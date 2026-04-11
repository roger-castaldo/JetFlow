using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Subscriptions;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.KeyValueStore;
using NATS.Net;

namespace JetFlow;

public static class Connection 
{
    public const string TraceProviderName = "JetFlow";
    public const string MetricsMeterName = "JetFlow.Runtime";
    public static async ValueTask<IConnection> CreateInstanceAsync(ConnectionOptions options)
    {
        var result = new ConnectionInstance(options);
        return await result.OpenAsync();
    }

    internal class ConnectionInstance(ConnectionOptions options) : IConnection
    {
        private readonly INatsConnection connection = options.Connection;
        private readonly INatsJSContext jsContext = options.NatsJSContext;
        private readonly MessageSerializer messageSerializer = new(options);
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private INatsKVStore timerStore;
        private WorkflowConfigurationContainer workflowConfigurationContainer;
        private Task? timeoutRunner;

        public async ValueTask<IConnection> OpenAsync()
        {
            await connection.ConnectAsync();
            if (connection.ConnectionState != NatsConnectionState.Open)
                throw new UnableToConnectException();
            await jsContext.CreateOrUpdateStreamAsync(new(SubjectHelper.WorkflowEventsStreamsName(options.Namespace), [
                SubjectHelper.WorkflowStart(options.Namespace, "*", "*"),
                SubjectHelper.WorkflowEnd(options.Namespace, "*", "*"),
                SubjectHelper.WorkflowDelayStart(options.Namespace, "*", "*"),
                SubjectHelper.WorkflowDelayEnd(options.Namespace, "*", "*"),
                SubjectHelper.WorkflowDelayTimer(options.Namespace, "*", "*"),
                SubjectHelper.WorkflowStepStart(options.Namespace, "*", "*", "*"),
                SubjectHelper.WorkflowStepEnd(options.Namespace, "*", "*", "*"),
                SubjectHelper.WorkflowStepError(options.Namespace, "*", "*", "*"),
                SubjectHelper.WorkflowStepTimeout(options.Namespace, "*", "*", "*")
            ])
            {
                DuplicateWindow = TimeSpan.FromMinutes(10),
                AllowDirect = true,
                AllowMsgSchedules = true,
                AllowMsgTTL=true
            });
            await jsContext.CreateOrUpdateStreamAsync(new(SubjectHelper.ActivityQueueStream(options.Namespace), [
                SubjectHelper.ActivityStart(options.Namespace, "*","*", "*")
            ])
            {
                DuplicateWindow = TimeSpan.FromMinutes(10),
                AllowDirect = true,
                AllowMsgSchedules = true,
                AllowMsgTTL=true
            });
            await jsContext.CreateOrUpdateStreamAsync(new(SubjectHelper.ActivityTimersStream(options.Namespace), [
                SubjectHelper.ActivityTimeout(options.Namespace, "*", "*", "*"),
                SubjectHelper.ActivityTimer(options.Namespace, "*", "*", "*")
            ])
            {
                DuplicateWindow = TimeSpan.FromMinutes(10),
                AllowDirect = true,
                AllowMsgSchedules = true
            });
            var kc = jsContext.CreateKeyValueStoreContext();
            timerStore = await kc.CreateOrUpdateStoreAsync(new(SubjectHelper.ActivityLocksKeystore(options.Namespace))
            {
                Description = "KeyValue store for workflow timers",
                History = 1, 
                MaxAge = TimeSpan.FromMinutes(5)
            });
            workflowConfigurationContainer = await WorkflowConfigurationContainer.CreateAsync(kc, options);
            
            timeoutRunner = StartActivityTimeoutRunner();
            return this;
        }

        private async Task StartActivityTimeoutRunner()
        {
            var consumer = await jsContext.CreateOrUpdateConsumerAsync(
                    SubjectHelper.ActivityTimersStream(options.Namespace),
                    new($"jetflow_activity_timeouts")
                    {
                        FilterSubjects= [
                            SubjectHelper.ActivityTimeout(options.Namespace, "*", "*", "*")
                        ],
                        DeliverPolicy = NATS.Client.JetStream.Models.ConsumerConfigDeliverPolicy.New,
                        AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.Explicit
                    },
                    cancellationTokenSource.Token
                );
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await consumer.RefreshAsync(cancellationTokenSource.Token); // or try to recreate consumer

                    await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationTokenSource.Token))
                    {
                        var message = new EventMessage(msg);
                        if (Equals(message.ActivityEventType, ActivityEventTypes.Timeout))
                        {
                            var (canRun, _) = await ActivityHelper.CanActivityRun(timerStore, jsContext, message, cancellationTokenSource.Token);
                            if (canRun)
                            {
                                await ActivityHelper.MarkActivityDoneInStore(timerStore, message, cancellationTokenSource.Token);
                                await ActivityHelper.TimeoutActivityAsync(message, connection, cancellationTokenSource.Token);
                                await msg.AckAsync(cancellationToken: cancellationTokenSource.Token);
                            }
                        }
                        else
                            await msg.NakAsync(cancellationToken: cancellationTokenSource.Token);
                    }
                }
                catch (NatsJSProtocolException e)
                {
                    //bury error
                }
                catch (NatsJSException e)
                {
                    // log exception
                    await Task.Delay(1000, cancellationTokenSource.Token); // backoff
                }
            }
        }

        private ValueTask<INatsJSConsumer> CreateWorkflowActivityConsumerAsync<TWorkflowActivity>(CancellationToken cancellationToken)
            => jsContext.CreateOrUpdateConsumerAsync(
                    SubjectHelper.ActivityQueueStream(options.Namespace),
                    new($"act_{NameHelper.GetActivityName<TWorkflowActivity>()}")
                    {
                        FilterSubjects= [
                            SubjectHelper.ActivityStart(options.Namespace, NameHelper.GetActivityName<TWorkflowActivity>(), "*", "*")
                        ],
                        DeliverPolicy = NATS.Client.JetStream.Models.ConsumerConfigDeliverPolicy.New,
                        AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.Explicit
                    },
                    cancellationToken
                );

        async ValueTask IConnection.RegisterWorkflowActivityAsync<TWorkflowActivity>(TWorkflowActivity activity, CancellationToken cancellationToken)
        {
            var sub = new WorkflowActivitySubscription<TWorkflowActivity>(activity, connection, jsContext, timerStore, await CreateWorkflowActivityConsumerAsync<TWorkflowActivity>(cancellationToken), messageSerializer, cancellationToken);
            sub.Start();
        }

        async ValueTask IConnection.RegisterWorkflowActivityAsync<TWorkflowActivity, TInput>(TWorkflowActivity activity, CancellationToken cancellationToken)
        {
            var sub = new WorkflowActivitySubscription<TWorkflowActivity, TInput>(activity, connection, jsContext, timerStore, await CreateWorkflowActivityConsumerAsync<TWorkflowActivity>(cancellationToken), messageSerializer, cancellationToken);
            sub.Start();
        }

        async ValueTask IConnection.RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput>(TWorkflowActivity activity, CancellationToken cancellationToken)
        {
            var sub = new WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput>(activity, connection, jsContext, timerStore, await CreateWorkflowActivityConsumerAsync<TWorkflowActivity>(cancellationToken), messageSerializer, cancellationToken);
            sub.Start();
        }

        async ValueTask IConnection.RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput, TInput>(TWorkflowActivity activity, CancellationToken cancellationToken)
        {
            var sub = new WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput, TInput>(activity, connection, jsContext, timerStore, await CreateWorkflowActivityConsumerAsync<TWorkflowActivity>(cancellationToken), messageSerializer, cancellationToken);
            sub.Start();
        }

        private ValueTask<INatsJSConsumer> CreateWorkflowConsumerAsync<TWorkflow>(CancellationToken cancellationToken)
            => jsContext.CreateOrUpdateConsumerAsync(
                    SubjectHelper.WorkflowEventsStreamsName(options.Namespace),
                    new($"wfr_{NameHelper.GetWorkflowName<TWorkflow>()}")
                    {
                        FilterSubjects= [
                            SubjectHelper.WorkflowStart(options.Namespace, NameHelper.GetWorkflowName<TWorkflow>(), "*"),
                            SubjectHelper.WorkflowDelayEnd(options.Namespace, NameHelper.GetWorkflowName<TWorkflow>(), "*"),
                            SubjectHelper.WorkflowStepEnd(options.Namespace, NameHelper.GetWorkflowName<TWorkflow>(), "*", "*"),
                            SubjectHelper.WorkflowStepError(options.Namespace, NameHelper.GetWorkflowName<TWorkflow>(), "*", "*"),
                            SubjectHelper.WorkflowStepTimeout(options.Namespace, NameHelper.GetWorkflowName<TWorkflow>(), "*", "*")
                        ],
                        DeliverPolicy = NATS.Client.JetStream.Models.ConsumerConfigDeliverPolicy.New,
                        AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.Explicit
                    },
                    cancellationToken
                );

        async ValueTask IConnection.RegisterWorkflowAsync<TWorkflow>(WorkflowOptions? options, CancellationToken cancellationToken)
        {
            await workflowConfigurationContainer.RegisterWorkflowConfigAsync<TWorkflow>(options);
            var sub = new WorkflowSubscription<TWorkflow>(connection, jsContext, timerStore, workflowConfigurationContainer, await CreateWorkflowConsumerAsync<TWorkflow>(cancellationToken), messageSerializer, cancellationTokenSource.Token);
            sub.Start();
        }

        async ValueTask IConnection.RegisterWorkflowAsync<TWorkflow, TInput>(WorkflowOptions? options, CancellationToken cancellationToken)
        {
            await workflowConfigurationContainer.RegisterWorkflowConfigAsync<TWorkflow>(options);
            var sub = new WorkflowSubscription<TWorkflow, TInput>(connection, jsContext, timerStore, workflowConfigurationContainer, await CreateWorkflowConsumerAsync<TWorkflow>(cancellationToken), messageSerializer, cancellationTokenSource.Token);
            sub.Start();
        }
        ValueTask IConnection.StartWorkflowAsync<TWorkflow>(CancellationToken cancellationToken)
            => WorkflowHelper.StartWorkflowAsync<TWorkflow>(options.Namespace, connection, cancellationToken);

        ValueTask IConnection.StartWorkflowAsync<TWorkflow, TInput>(TInput input, CancellationToken cancellationToken)
            => WorkflowHelper.StartWorkflowAsync<TWorkflow, TInput>(options.Namespace, connection, messageSerializer, input, cancellationToken);

    }
}
