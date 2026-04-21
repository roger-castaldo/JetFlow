using JetFlow.Configs;
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

        // Assert
        Assert.IsNotNull(connection);

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
}
