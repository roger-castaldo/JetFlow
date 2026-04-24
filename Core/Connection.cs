using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using JetFlow.Subscriptions;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.KeyValueStore;
using NATS.Client.ObjectStore;
using NATS.Net;
using System.Collections.Concurrent;

namespace JetFlow;

public static class Connection 
{
    public const string TraceProviderName = "JetFlow";
    public const string MetricsMeterName = "JetFlow.Runtime";
    public static ValueTask<IConnection> CreateInstanceAsync(ConnectionOptions options)
        => ConnectionInstance.CreateAsync(options);

    internal class ConnectionInstance : IConnection, IAsyncDisposable
    {
        private readonly MessageSerializer messageSerializer;
        private readonly ServiceConnection serviceConnection;
        private readonly SubjectMapper subjectMapper;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly ConcurrentBag<ASubscription> subscriptions = new();

        private ConnectionInstance(INatsConnection connection, INatsJSContext natsJSContext, MessageSerializer messageSerializer, 
            SubjectMapper subjectMapper, INatsKVStore timerStore, INatsKVStore configurationStore, INatsObjStore archiveStore,
            INatsJSConsumer activityTimeoutsConsumer)
        {
            this.messageSerializer = messageSerializer;
            this.subjectMapper = subjectMapper;
            serviceConnection = new(connection, natsJSContext, timerStore, configurationStore, archiveStore, subjectMapper, messageSerializer);
            subscriptions.Add(new ActivityTimeoutsSubscription(serviceConnection, activityTimeoutsConsumer, cancellationTokenSource.Token));
        }

        public static async ValueTask<IConnection> CreateAsync(ConnectionOptions options)
        {
            var connection = options.Connection;
            var jsContext = options.NatsJSContext;
            try
            {
                await connection.ConnectAsync();
            }
            catch
            {
                //burying connection errors
            }
            if (connection.ConnectionState != NatsConnectionState.Open)
                throw new UnableToConnectException();
            var subjectMapper = new SubjectMapper(options.Namespace);
            await jsContext.CreateOrUpdateStreamAsync(new(subjectMapper.WorkflowEventsStreamsName, [
                subjectMapper.WorkflowConfigure("*", "*"),
                subjectMapper.WorkflowStart("*", "*"),
                subjectMapper.WorkflowEnd("*", "*"),
                subjectMapper.WorkflowArchived("*", "*"),
                subjectMapper.WorkflowPurge("*", "*"),
                subjectMapper.WorkflowDelayStart("*", "*"),
                subjectMapper.WorkflowDelayEnd("*", "*"),
                subjectMapper.WorkflowTimer("*", "*"),
                subjectMapper.WorkflowStepStart("*", "*", "*"),
                subjectMapper.WorkflowStepEnd("*", "*", "*"),
                subjectMapper.WorkflowStepError("*", "*", "*"),
                subjectMapper.WorkflowStepTimeout("*", "*", "*"),
                subjectMapper.WorkflowStepRetry("*", "*", "*")
            ])
            {
                DuplicateWindow = TimeSpan.FromMinutes(10),
                AllowDirect = true,
                AllowMsgSchedules = true,
                AllowMsgTTL=true
            });
            await jsContext.CreateOrUpdateStreamAsync(new(subjectMapper.ActivityQueueStream, [
                subjectMapper.ActivityStart("*","*", "*"),
                subjectMapper.ActivityTimeout("*", "*", "*"),
                subjectMapper.ActivityTimer("*", "*", "*")
            ])
            {
                DuplicateWindow = TimeSpan.FromMinutes(10),
                AllowDirect = true,
                AllowMsgSchedules = true,
                AllowMsgTTL=true,
                Retention = StreamConfigRetention.Workqueue
            });
            var kc = jsContext.CreateKeyValueStoreContext();
            var timerStore = await kc.CreateOrUpdateStoreAsync(new(subjectMapper.ActivityLocksKeystore)
            {
                Description = "KeyValue store for workflow timers",
                History = 1, 
                MaxAge = TimeSpan.FromMinutes(5)
            });
            var configurationStore = await kc.CreateOrUpdateStoreAsync(new(subjectMapper.WorkflowConfigKeystore)
            {
                Description = "KeyValue store for workflow configurations",
                History = 1,
                LimitMarkerTTL = TimeSpan.FromMinutes(1)
            });
            await configurationStore.PutAsync<WorkflowOptions>(ServiceConnection.DefaultConfigKey, options.DefaultWorkflowOptions, serializer: new WorkflowOptionsSerializer());
            var objContext = jsContext.CreateObjectStoreContext();
            var archiveStore = await objContext.CreateObjectStoreAsync(subjectMapper.WorkflowArchiveKeystore);
            var activityTimeoutsConsumer = await jsContext.CreateOrUpdateConsumerAsync(
                    subjectMapper.ActivityQueueStream,
                    new($"jetflow_activity_timeouts")
                    {
                        DurableName = $"jetflow_activity_timeouts",
                        FilterSubject= subjectMapper.ActivityTimeout("*", "*", "*"),
                        AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.Explicit
                    },
                    CancellationToken.None
                );
            return new ConnectionInstance(connection, jsContext, new(options), subjectMapper, timerStore, configurationStore, archiveStore, activityTimeoutsConsumer);
        }

        private async Task StartActivityTimeoutRunner()
        {
            var consumer = await serviceConnection.CreateOrUpdateConsumerAsync(
                    subjectMapper.ActivityQueueStream,
                    new($"jetflow_activity_timeouts")
                    {
                        DurableName = $"jetflow_activity_timeouts",
                        FilterSubject= subjectMapper.ActivityTimeout("*", "*", "*"),
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
                            var (canRun, _) = await serviceConnection.CanActivityRun(message, cancellationTokenSource.Token);
                            if (canRun)
                            {
                                await serviceConnection.MarkActivityDoneInStore(message, cancellationTokenSource.Token);
                                await RetryHelper.ProcessActivityRetryAsync(RetryTypes.Timeout, message, serviceConnection, cancellationTokenSource.Token);
                                await message.Message.AckAsync(cancellationToken: cancellationTokenSource.Token);
                            }
                        }
                        else
                            await message.Message.NakAsync(cancellationToken: cancellationTokenSource.Token);
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
                catch(OperationCanceledException)
                {
                    // expected on cancellation, ignore
                }
            }
        }

        private ValueTask<INatsJSConsumer> CreateWorkflowActivityConsumerAsync<TWorkflowActivity>(CancellationToken cancellationToken)
            => serviceConnection.CreateOrUpdateConsumerAsync(
                    subjectMapper.ActivityQueueStream,
                    new($"act_{NameHelper.GetActivityName<TWorkflowActivity>()}")
                    {
                        DurableName= $"act_{NameHelper.GetActivityName<TWorkflowActivity>()}",
                        FilterSubject = subjectMapper.ActivityStart(NameHelper.GetActivityName<TWorkflowActivity>(), "*", "*"),
                        AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.Explicit
                    },
                    cancellationToken
                );

        async ValueTask IConnection.RegisterWorkflowActivityAsync<TWorkflowActivity>(TWorkflowActivity activity, CancellationToken cancellationToken)
            => subscriptions.Add(new WorkflowActivitySubscription<TWorkflowActivity>(activity, serviceConnection, subjectMapper, messageSerializer, await CreateWorkflowActivityConsumerAsync<TWorkflowActivity>(cancellationToken), cancellationTokenSource.Token));

        async ValueTask IConnection.RegisterWorkflowActivityAsync<TWorkflowActivity, TInput>(TWorkflowActivity activity, CancellationToken cancellationToken)
            => subscriptions.Add(new WorkflowActivitySubscription<TWorkflowActivity, TInput>(activity, serviceConnection, subjectMapper, messageSerializer, await CreateWorkflowActivityConsumerAsync<TWorkflowActivity>(cancellationToken), cancellationTokenSource.Token));

        async ValueTask IConnection.RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput>(TWorkflowActivity activity, CancellationToken cancellationToken)
            => subscriptions.Add(new WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput>(activity, serviceConnection, subjectMapper, messageSerializer, await CreateWorkflowActivityConsumerAsync<TWorkflowActivity>(cancellationToken), cancellationTokenSource.Token));

        async ValueTask IConnection.RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput, TInput>(TWorkflowActivity activity, CancellationToken cancellationToken)
            => subscriptions.Add(new WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput, TInput>(activity, serviceConnection, subjectMapper, messageSerializer, await CreateWorkflowActivityConsumerAsync<TWorkflowActivity>(cancellationToken), cancellationTokenSource.Token));

        private ValueTask<INatsJSConsumer> CreateWorkflowConsumerAsync<TWorkflow>(CancellationToken cancellationToken)
            => serviceConnection.CreateOrUpdateConsumerAsync(
                    subjectMapper.WorkflowEventsStreamsName,
                    new($"wfr_{NameHelper.GetWorkflowName<TWorkflow>()}")
                    {
                        DurableName = $"wfr_{NameHelper.GetWorkflowName<TWorkflow>()}",
                        FilterSubjects= [
                            subjectMapper.WorkflowStart(NameHelper.GetWorkflowName<TWorkflow>(), "*"),
                            subjectMapper.WorkflowPurge(NameHelper.GetWorkflowName<TWorkflow>(), "*"),
                            subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<TWorkflow>(), "*"),
                            subjectMapper.WorkflowDelayEnd(NameHelper.GetWorkflowName<TWorkflow>(), "*"),
                            subjectMapper.WorkflowStepEnd(NameHelper.GetWorkflowName<TWorkflow>(), "*", "*"),
                            subjectMapper.WorkflowStepError(NameHelper.GetWorkflowName<TWorkflow>(), "*", "*"),
                            subjectMapper.WorkflowStepTimeout(NameHelper.GetWorkflowName<TWorkflow>(), "*", "*")
                        ],
                        DeliverPolicy = NATS.Client.JetStream.Models.ConsumerConfigDeliverPolicy.New,
                        AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.Explicit
                    },
                    cancellationToken
                );

        async ValueTask IConnection.RegisterWorkflowAsync<TWorkflow>(WorkflowOptions? options, CancellationToken cancellationToken)
        {
            await serviceConnection.RegisterWorkflowConfigAsync<TWorkflow>(options);
            subscriptions.Add(new WorkflowSubscription<TWorkflow>(serviceConnection, subjectMapper, messageSerializer, await CreateWorkflowConsumerAsync<TWorkflow>(cancellationToken), cancellationTokenSource.Token));
        }

        async ValueTask IConnection.RegisterWorkflowAsync<TWorkflow, TInput>(WorkflowOptions? options, CancellationToken cancellationToken)
        {
            await serviceConnection.RegisterWorkflowConfigAsync<TWorkflow>(options);
            subscriptions.Add(new WorkflowSubscription<TWorkflow, TInput>(serviceConnection, subjectMapper, messageSerializer, await CreateWorkflowConsumerAsync<TWorkflow>(cancellationToken), cancellationTokenSource.Token));
        }
        ValueTask<Guid> IConnection.StartWorkflowAsync<TWorkflow>(CancellationToken cancellationToken)
            => serviceConnection.StartWorkflowAsync<TWorkflow>(cancellationToken);

        ValueTask<Guid> IConnection.StartWorkflowAsync<TWorkflow, TInput>(TInput input, CancellationToken cancellationToken)
            => serviceConnection.StartWorkflowAsync<TWorkflow, TInput>(input, cancellationToken);

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                await cancellationTokenSource.CancelAsync();
                await Task.WhenAll(
                    subscriptions.Select(s => s.AwaitClose())
                );
                subscriptions.Clear();
            }
        }
    }
}
