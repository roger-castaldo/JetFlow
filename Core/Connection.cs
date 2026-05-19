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

/// <summary>
/// Used to create a connection to the JetFlow runtime, and register workflows and activities. It also manages the lifecycle of the connection and all subscriptions created through it, so disposing it will close all connections and subscriptions created through it. You can have multiple instances of this class connected to the same NATS server, but it's recommended to reuse the same instance as much as possible to avoid unnecessary connections and subscriptions.
/// </summary>
public static class Connection 
{
    /// <summary>
    /// The name of the trace provider and metrics meter used by JetFlow, you can use it to correlate traces and metrics with the JetFlow runtime. It's recommended to use the same name for both traces and metrics to make it easier to correlate them.
    /// </summary>
    public const string TraceProviderName = "JetFlow";
    /// <summary>
    /// The name of the metrics meter used by JetFlow, you can use it to correlate metrics with the JetFlow runtime. It's recommended to use the same name for both traces and metrics to make it easier to correlate them.
    /// </summary>
    public const string MetricsMeterName = "JetFlow.Runtime";
    /// <summary>
    /// Called to create a new instance of the connection, which will be used to register workflows and activities, and manage the lifecycle of the connection and all subscriptions created through it. It's recommended to reuse the same instance as much as possible to avoid unnecessary connections and subscriptions. This method will attempt to connect to the NATS server, and if it fails, it will throw an exception. If the connection is successful, it will create the necessary streams, key value stores, object stores and consumers required for the JetFlow runtime to function properly. The connection will be left open until the instance is disposed, so you can use it to register workflows and activities at any time after it's created.
    /// </summary>
    /// <param name="options">The ConnectionOptions object containing the configuration options for the connection.</param>
    /// <returns>A ValueTask representing the asynchronous operation, with a result of type IConnection.</returns>
    public static ValueTask<IConnection> CreateInstanceAsync(ConnectionOptions options)
        => ConnectionInstance.CreateAsync(options);

    private sealed record ConnectionStores(INatsKVStore TimerStore, INatsKVStore ConfigurationStore, INatsObjStore ArchiveStore,
            INatsJSConsumer ActivityTimeoutsConsumer, INatsJSConsumer ScheduledWorkflowConsumer);

    internal class ConnectionInstance : IConnection, IAsyncDisposable
    {
        private readonly MessageSerializer messageSerializer;
        private readonly InternalNatsConnection internalConnection;
        private readonly ServiceConnection serviceConnection;
        private readonly Version? serverVersion;
        private readonly SubjectMapper subjectMapper;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly ConcurrentBag<ASubscription> subscriptions = new();

        private ConnectionInstance(INatsConnection connection, INatsJSContext natsJSContext, MessageSerializer messageSerializer, 
            SubjectMapper subjectMapper, ConnectionStores stores)
        {
            this.messageSerializer = messageSerializer;
            this.subjectMapper = subjectMapper;
            serverVersion = (connection.ServerInfo==null ? null : new Version(connection.ServerInfo.Version));
            internalConnection = new(connection, natsJSContext, serverVersion);
            serviceConnection = new(internalConnection, stores.TimerStore, stores.ConfigurationStore, stores.ArchiveStore, subjectMapper, messageSerializer);
            subscriptions.Add(new ActivityTimeoutsSubscription(serviceConnection, stores.ActivityTimeoutsConsumer, cancellationTokenSource.Token));
            subscriptions.Add(new ScheduledWorkflowsSubscription(subjectMapper, serviceConnection, stores.ScheduledWorkflowConsumer, cancellationTokenSource.Token));
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
            await jsContext.CreateOrUpdateStreamAsync(new(subjectMapper.WorkflowEventsStreamsName, [subjectMapper.WorkflowEventsFilter])
            {
                DuplicateWindow = TimeSpan.FromMinutes(10),
                AllowDirect = true,
                AllowMsgSchedules = true,
                AllowMsgTTL=true
            });
            await jsContext.CreateOrUpdateStreamAsync(new(subjectMapper.ActivityQueueStream, [subjectMapper.ActivityEventsFilter])
            {
                DuplicateWindow = TimeSpan.FromMinutes(10),
                AllowDirect = true,
                AllowMsgSchedules = true,
                AllowMsgTTL=true,
                Retention = StreamConfigRetention.Workqueue
            });
            await jsContext.CreateOrUpdateStreamAsync(new(subjectMapper.ScheduledWorkflowStreamsName, [subjectMapper.ScheduledWorkflowsFilter])
            {
                DuplicateWindow = TimeSpan.FromMinutes(10),
                AllowDirect = true,
                AllowMsgSchedules = true,
                AllowMsgTTL=true
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
            var scheduledWorkflowConsumer = await jsContext.CreateOrUpdateConsumerAsync(
                    subjectMapper.ScheduledWorkflowStreamsName,
                    new($"jetflow_scheduled_workflows")
                    {
                        DurableName = $"jetflow_scheduled_workflows",
                        FilterSubject= subjectMapper.ScheduledWorkflowStart("*", "*"),
                        AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.Explicit
                    },
                    CancellationToken.None
                );
            return new ConnectionInstance(connection, jsContext, new(options), subjectMapper, 
                new(timerStore, configurationStore, archiveStore, activityTimeoutsConsumer, scheduledWorkflowConsumer)
            );
        }

        private ValueTask<INatsJSConsumer> CreateWorkflowActivityConsumerAsync<TWorkflowActivity>(CancellationToken cancellationToken)
            => internalConnection.CreateOrUpdateConsumerAsync(
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
            => internalConnection.CreateOrUpdateConsumerAsync(
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

        private static readonly Version minScheduleVersionRequired = new("2.14");

        ValueTask<Guid> IConnection.ScheduleWorkflowAsync<TWorkflow>(IWorkflowSchedule schedule, WorkflowOptions? options, CancellationToken cancellationToken)
        {
            if (serverVersion!=null && serverVersion<minScheduleVersionRequired)
                throw new NotSupportedException($"Unable to support repeated schedules on nats version {serverVersion}, you must upgrade to at least {minScheduleVersionRequired}");
            return serviceConnection.ScheduleWorkflowAsync<TWorkflow>(schedule, options, cancellationToken);
        }

        ValueTask<Guid> IConnection.ScheduleWorkflowAsync<TWorkflow, TInput>(TInput input, IWorkflowSchedule schedule, WorkflowOptions? options, CancellationToken cancellationToken)
        {
            if (serverVersion!=null && serverVersion<minScheduleVersionRequired)
                throw new NotSupportedException($"Unable to support repeated schedules on nats version {serverVersion}, you must upgrade to at least {minScheduleVersionRequired}");
            return serviceConnection.ScheduleWorkflowAsync<TWorkflow, TInput>(input, schedule, options, cancellationToken);
        }

        ValueTask<Guid> IConnection.DelayStartWorkflowAsync<TWorkflow>(TimeSpan delay, WorkflowOptions? options, CancellationToken cancellationToken)
            => serviceConnection.DelayStartWorkflowAsync<TWorkflow>(delay, options, cancellationToken);

        ValueTask<Guid> IConnection.DelayStartWorkflowAsync<TWorkflow, TInput>(TInput input, TimeSpan delay, WorkflowOptions? options, CancellationToken cancellationToken)
            => serviceConnection.DelayStartWorkflowAsync<TWorkflow, TInput>(input, delay, options, cancellationToken);

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
