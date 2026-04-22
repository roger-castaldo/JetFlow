using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.KeyValueStore;
using NATS.Client.ObjectStore;
using NATS.Net;

namespace JetFlow.Testing;

[TestClass]
public class ConnectionTests
{
    private static NatsTestHarness? natsTestHarness;

    [ClassInitialize]
    public static async Task Init(TestContext testContext)
    {
        natsTestHarness = new NatsTestHarness();
        await natsTestHarness.StartAsync();
    }

    [ClassCleanup]
    public static async Task Cleanup()
        => await (natsTestHarness?.DisposeAsync()??ValueTask.CompletedTask);

    [TestMethod]
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("mydomain", DisplayName = "Custom namespace")]
    public async Task EnsureAllRequiredStreamsCreated(string instanceNamespace)
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        var subjectMapper = new SubjectMapper(instanceNamespace);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var kvStoreContext = jsContext.CreateKeyValueStoreContext();
        var objectStoreContext = jsContext.CreateObjectStoreContext();
        // Act
        var connection = await Connection.CreateInstanceAsync(new(natsConnection, jsContext)
        {
            Namespace = instanceNamespace
        });
        await Task.Delay(TimeSpan.FromSeconds(1)); // small delay to ensure streams are created

        // Assert
        Assert.IsNotNull(connection);
        await ((IAsyncDisposable)connection).DisposeAsync();

        //Verify
        var workFlowStream = await jsContext.GetStreamAsync(subjectMapper.WorkflowEventsStreamsName);
        Assert.IsNotNull(workFlowStream);
        Assert.IsNotNull(workFlowStream.Info.Config.Subjects);
        Assert.IsTrue(CollectionsHelper.CollectionsMatchIgnoreOrder<string>(workFlowStream.Info.Config.Subjects, new string[]{
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
        }));
        Assert.IsTrue(workFlowStream.Info.Config.AllowDirect);
        Assert.IsTrue(workFlowStream.Info.Config.AllowMsgSchedules);
        Assert.IsTrue(workFlowStream.Info.Config.AllowMsgTTL);
        Assert.AreEqual(TimeSpan.FromMinutes(10), workFlowStream.Info.Config.DuplicateWindow);
        var activityStream = await jsContext.GetStreamAsync(subjectMapper.ActivityQueueStream);
        Assert.IsNotNull(activityStream);
        Assert.IsNotNull(activityStream.Info.Config.Subjects);
        Assert.IsTrue(CollectionsHelper.CollectionsMatchIgnoreOrder<string>(activityStream.Info.Config.Subjects, new string[]{
            subjectMapper.ActivityStart("*","*", "*"),
            subjectMapper.ActivityTimeout("*", "*", "*"),
            subjectMapper.ActivityTimer("*", "*", "*")
        }));
        Assert.IsTrue(activityStream.Info.Config.AllowDirect);
        Assert.IsTrue(activityStream.Info.Config.AllowMsgSchedules);
        Assert.IsTrue(activityStream.Info.Config.AllowMsgTTL);
        Assert.AreEqual(TimeSpan.FromMinutes(10), activityStream.Info.Config.DuplicateWindow);
        Assert.AreEqual(StreamConfigRetention.Workqueue, activityStream.Info.Config.Retention);
        var activityLocksStore = await jsContext.GetStreamAsync($"KV_{subjectMapper.ActivityLocksKeystore}");
        Assert.IsNotNull(activityLocksStore);
        Assert.IsNotNull(activityLocksStore.Info.Config);
        Assert.AreEqual("KeyValue store for workflow timers", activityLocksStore.Info.Config.Description);
        Assert.AreEqual(TimeSpan.FromMinutes(5), activityLocksStore.Info.Config.MaxAge);
        Assert.AreEqual(1, activityLocksStore.Info.Config.MaxMsgsPerSubject);
        var workflowConfigStore = await jsContext.GetStreamAsync($"KV_{subjectMapper.WorkflowConfigKeystore}");
        Assert.IsNotNull(workflowConfigStore);
        Assert.IsNotNull(workflowConfigStore.Info.Config);
        Assert.AreEqual("KeyValue store for workflow configurations", workflowConfigStore.Info.Config.Description);
        Assert.AreEqual(1, workflowConfigStore.Info.Config.MaxMsgsPerSubject);
        var configStorage = await kvStoreContext.GetStoreAsync(subjectMapper.WorkflowConfigKeystore);
        Assert.IsNotNull(configStorage);
        var defaultConfig = await configStorage.GetEntryAsync<WorkflowOptions>(ServiceConnection.DefaultConfigKey, serializer: new WorkflowOptionsSerializer());
        Assert.IsNull(defaultConfig.Error);
        Assert.AreEqual(new WorkflowOptions(), defaultConfig.Value);
        var archiveStore = await objectStoreContext.GetObjectStoreAsync(subjectMapper.WorkflowArchiveKeystore);
        Assert.IsNotNull(archiveStore);
        var activityTimeoutConsumer = await jsContext.GetConsumerAsync(subjectMapper.ActivityQueueStream, "jetflow_activity_timeouts");
        Assert.IsNotNull(activityTimeoutConsumer);
        Assert.IsNotNull(activityTimeoutConsumer.Info.Config);
        Assert.AreEqual(subjectMapper.ActivityTimeout("*", "*", "*"), activityTimeoutConsumer.Info.Config.FilterSubject);
        Assert.AreEqual("jetflow_activity_timeouts", activityTimeoutConsumer.Info.Config.DurableName);
        Assert.AreEqual(ConsumerConfigAckPolicy.Explicit, activityTimeoutConsumer.Info.Config.AckPolicy);
    }

    [TestMethod()]
    public async Task EnsureConnectionFailureAborts()
    {
        // Arrange
        var options = new NatsOpts()
        {
            Url = "nats://localhost:4223" // assuming nothing is running on this port
        };
        // Act & Assert
        await Assert.ThrowsAsync<UnableToConnectException>(async () => await Connection.CreateInstanceAsync(new(options)));
    }

    private class WorkflowActivityNoInputNoReturn : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
    private class WorkflowActivityWithInputNoReturn : IActivity<string>
    {
        Task IActivity<string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
    private class WorkflowActivityNoInputWithReturn : IActivityWithReturn<string>
    {
        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
            => Task.FromResult(string.Empty);
    }
    private class WorkflowActivityWithInputWithReturn : IActivityWithReturn<string, string>
    {
        Task<string> IActivityWithReturn<string, string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
            => Task.FromResult(string.Empty);
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("mydomain", DisplayName = "Custom namespace")]
    public async Task EnsureWorkflowActivityRegistrations(string instanceNamespace)
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        var subjectMapper = new SubjectMapper(instanceNamespace);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        // Act
        var connection = await Connection.CreateInstanceAsync(new(natsConnection, jsContext)
        {
            Namespace = instanceNamespace
        });
        await connection.RegisterWorkflowActivityAsync<WorkflowActivityNoInputNoReturn>(new WorkflowActivityNoInputNoReturn(), CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<WorkflowActivityWithInputNoReturn, string>(new WorkflowActivityWithInputNoReturn(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<WorkflowActivityNoInputWithReturn, string>(new WorkflowActivityNoInputWithReturn(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<WorkflowActivityWithInputWithReturn, string, string>(new WorkflowActivityWithInputWithReturn(), CancellationToken.None);

        // Assert
        Assert.IsNotNull(connection);
        await ((IAsyncDisposable)connection).DisposeAsync();

        //Verify
        var activityConsumer = await jsContext.GetConsumerAsync(subjectMapper.ActivityQueueStream, $"act_{NameHelper.GetActivityName<WorkflowActivityNoInputNoReturn>()}");
        Assert.IsNotNull(activityConsumer);
        Assert.AreEqual($"act_{NameHelper.GetActivityName<WorkflowActivityNoInputNoReturn>()}", activityConsumer.Info.Config.DurableName);
        Assert.AreEqual(subjectMapper.ActivityStart(NameHelper.GetActivityName<WorkflowActivityNoInputNoReturn>(), "*", "*"), activityConsumer.Info.Config.FilterSubject);
        Assert.AreEqual(ConsumerConfigAckPolicy.Explicit, activityConsumer.Info.Config.AckPolicy);

        var activityWithInputConsumer = await jsContext.GetConsumerAsync(subjectMapper.ActivityQueueStream, $"act_{NameHelper.GetActivityName<WorkflowActivityWithInputNoReturn>()}");
        Assert.IsNotNull(activityWithInputConsumer);
        Assert.AreEqual($"act_{NameHelper.GetActivityName<WorkflowActivityWithInputNoReturn>()}", activityWithInputConsumer.Info.Config.DurableName);
        Assert.AreEqual(subjectMapper.ActivityStart(NameHelper.GetActivityName<WorkflowActivityWithInputNoReturn>(), "*", "*"), activityWithInputConsumer.Info.Config.FilterSubject);
        Assert.AreEqual(ConsumerConfigAckPolicy.Explicit, activityWithInputConsumer.Info.Config.AckPolicy);

        var activityWithReturnConsumer = await jsContext.GetConsumerAsync(subjectMapper.ActivityQueueStream, $"act_{NameHelper.GetActivityName<WorkflowActivityNoInputWithReturn>()}");
        Assert.IsNotNull(activityWithReturnConsumer);
        Assert.AreEqual($"act_{NameHelper.GetActivityName<WorkflowActivityNoInputWithReturn>()}", activityWithReturnConsumer.Info.Config.DurableName);
        Assert.AreEqual(subjectMapper.ActivityStart(NameHelper.GetActivityName<WorkflowActivityNoInputWithReturn>(), "*", "*"), activityWithReturnConsumer.Info.Config.FilterSubject);
        Assert.AreEqual(ConsumerConfigAckPolicy.Explicit, activityWithReturnConsumer.Info.Config.AckPolicy);

        var activityWithInputWithReturnConsumer = await jsContext.GetConsumerAsync(subjectMapper.ActivityQueueStream, $"act_{NameHelper.GetActivityName<WorkflowActivityWithInputWithReturn>()}");
        Assert.IsNotNull(activityWithInputWithReturnConsumer);
        Assert.AreEqual($"act_{NameHelper.GetActivityName<WorkflowActivityWithInputWithReturn>()}", activityWithInputWithReturnConsumer.Info.Config.DurableName);
        Assert.AreEqual(subjectMapper.ActivityStart(NameHelper.GetActivityName<WorkflowActivityWithInputWithReturn>(), "*", "*"), activityWithInputWithReturnConsumer.Info.Config.FilterSubject);
        Assert.AreEqual(ConsumerConfigAckPolicy.Explicit, activityWithInputWithReturnConsumer.Info.Config.AckPolicy);
    }

    private class WorkflowWithNoInput : IWorkflow
    {
        ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
            => ValueTask.CompletedTask;
    }
    private class WorkflowWithInput : IWorkflow<string>
    {
        ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string? input)
            => ValueTask.CompletedTask;
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("mydomain", DisplayName = "Custom namespace")]
    public async Task EnsureWorkflowRegistrations(string instanceNamespace)
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        var subjectMapper = new SubjectMapper(instanceNamespace);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var kvStoreContext = jsContext.CreateKeyValueStoreContext();
        var withInputConfig = new WorkflowOptions()
        {
            CompletionAction = WorkflowCompletionActions.ArchiveThenNothing
        };
        // Act
        var connection = await Connection.CreateInstanceAsync(new(natsConnection, jsContext)
        {
            Namespace = instanceNamespace
        });
        await connection.RegisterWorkflowAsync<WorkflowWithNoInput>(null, CancellationToken.None);
        await connection.RegisterWorkflowAsync<WorkflowWithInput, string>(withInputConfig, CancellationToken.None);

        // Assert
        Assert.IsNotNull(connection);
        await ((IAsyncDisposable)connection).DisposeAsync();

        //Verify
        var workflowConsumer = await jsContext.GetConsumerAsync(subjectMapper.WorkflowEventsStreamsName, $"wfr_{NameHelper.GetWorkflowName<WorkflowWithNoInput>()}");
        Assert.IsNotNull(workflowConsumer);
        Assert.AreEqual($"wfr_{NameHelper.GetWorkflowName<WorkflowWithNoInput>()}", workflowConsumer.Info.Config.DurableName);
        Assert.IsNotNull(workflowConsumer.Info.Config.FilterSubjects);
        Assert.IsTrue(CollectionsHelper.CollectionsMatchIgnoreOrder<string>(workflowConsumer.Info.Config.FilterSubjects, new string[]
        {
            subjectMapper.WorkflowStart(NameHelper.GetWorkflowName<WorkflowWithNoInput>(), "*"),
            subjectMapper.WorkflowPurge(NameHelper.GetWorkflowName<WorkflowWithNoInput>(), "*"),
            subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<WorkflowWithNoInput>(), "*"),
            subjectMapper.WorkflowDelayEnd(NameHelper.GetWorkflowName<WorkflowWithNoInput>(), "*"),
            subjectMapper.WorkflowStepEnd(NameHelper.GetWorkflowName<WorkflowWithNoInput>(), "*", "*"),
            subjectMapper.WorkflowStepError(NameHelper.GetWorkflowName<WorkflowWithNoInput>(), "*", "*"),
            subjectMapper.WorkflowStepTimeout(NameHelper.GetWorkflowName<WorkflowWithNoInput>(), "*", "*")
        }));
        Assert.AreEqual(ConsumerConfigAckPolicy.Explicit, workflowConsumer.Info.Config.AckPolicy);
        Assert.AreEqual(ConsumerConfigDeliverPolicy.New, workflowConsumer.Info.Config.DeliverPolicy);

        var workflowWithInputConsumer = await jsContext.GetConsumerAsync(subjectMapper.WorkflowEventsStreamsName, $"wfr_{NameHelper.GetWorkflowName<WorkflowWithInput>()}");
        Assert.IsNotNull(workflowWithInputConsumer);
        Assert.AreEqual($"wfr_{NameHelper.GetWorkflowName<WorkflowWithInput>()}", workflowWithInputConsumer.Info.Config.DurableName);
        Assert.IsNotNull(workflowWithInputConsumer.Info.Config.FilterSubjects);
        Assert.IsTrue(CollectionsHelper.CollectionsMatchIgnoreOrder<string>(workflowWithInputConsumer.Info.Config.FilterSubjects, new string[]
        {
            subjectMapper.WorkflowStart(NameHelper.GetWorkflowName<WorkflowWithInput>(), "*"),
            subjectMapper.WorkflowPurge(NameHelper.GetWorkflowName<WorkflowWithInput>(), "*"),
            subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<WorkflowWithInput>(), "*"),
            subjectMapper.WorkflowDelayEnd(NameHelper.GetWorkflowName<WorkflowWithInput>(), "*"),
            subjectMapper.WorkflowStepEnd(NameHelper.GetWorkflowName<WorkflowWithInput>(), "*", "*"),
            subjectMapper.WorkflowStepError(NameHelper.GetWorkflowName<WorkflowWithInput>(), "*", "*"),
            subjectMapper.WorkflowStepTimeout(NameHelper.GetWorkflowName<WorkflowWithInput>(), "*", "*")
        }));
        Assert.AreEqual(ConsumerConfigAckPolicy.Explicit, workflowWithInputConsumer.Info.Config.AckPolicy);
        Assert.AreEqual(ConsumerConfigDeliverPolicy.New, workflowWithInputConsumer.Info.Config.DeliverPolicy);

        var configStorage = await kvStoreContext.GetStoreAsync(subjectMapper.WorkflowConfigKeystore);
        Assert.IsNotNull(configStorage);
        var noInputConfig = await configStorage.TryGetEntryAsync<WorkflowOptions>(NameHelper.GetWorkflowName<WorkflowWithNoInput>(), serializer: new WorkflowOptionsSerializer());
        Assert.IsFalse(noInputConfig.Success);
        var inputConfig = await configStorage.TryGetEntryAsync<WorkflowOptions>(NameHelper.GetWorkflowName<WorkflowWithInput>(), serializer: new WorkflowOptionsSerializer());
        Assert.IsTrue(inputConfig.Success);
        Assert.AreEqual(withInputConfig, inputConfig.Value.Value);
    }
}
