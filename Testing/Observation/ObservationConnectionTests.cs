using JetFlow.Interfaces;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;
using System.Numerics;

namespace JetFlow.Testing.Observation;

[TestClass]
public class ObservationConnectionTests
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
    [DataRow(null, 3, DisplayName = "Default namespace with 3 minutes")]
    [DataRow(null, 9, DisplayName = "Default namespace with 9 minutes")]
    [DataRow("ensurecreation", 2, DisplayName = "Custom namespace with 2 minutes")]
    [DataRow("ensurecreation", 7, DisplayName = "Custom namespace with 7 minutes")]
    public async Task EnsureObservationStreamsCreated(string instanceNamespace, int performanceMinutes)
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
        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection));
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken); // small delay to ensure streams are created
        if (instanceNamespace is null)
            await observationConnection.AddDefaultNamespaceAsync();
        else
            await observationConnection.AddNamespaceAsync(instanceNamespace);
        await observationConnection.AddPerformanceMonitoringAsync((byte)performanceMinutes, async (workflowRecord) =>
        {
            // handle workflow record
            await Task.CompletedTask;
        }, async (activityRecord) =>
        {
            // handle activity record
            await Task.CompletedTask;
        });
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken); // small delay to ensure streams are created

        // Assert
        Assert.IsNotNull(connection);
        await ((IAsyncDisposable)connection).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();

        //verify
        var performanceStream = await jsContext.GetStreamAsync(subjectMapper.PerformanceStreamName, cancellationToken: TestContext.CancellationToken);
        Assert.IsNotNull(performanceStream);
        Assert.IsNotNull(performanceStream.Info.Config.Subjects);
        Assert.IsTrue(CollectionsHelper.CollectionsMatchIgnoreOrder<string>(performanceStream.Info.Config.Subjects, [
            subjectMapper.PerformanceFilter
        ]));
        Assert.AreEqual(StreamConfigRetention.Limits, performanceStream.Info.Config.Retention);
        Assert.AreEqual(StreamConfigDiscard.Old, performanceStream.Info.Config.Discard);
        Assert.AreEqual(TimeSpan.FromDays(1), performanceStream.Info.Config.MaxAge);
        var kc = jsContext.CreateKeyValueStoreContext();
        var configStore = await kc.GetStoreAsync(subjectMapper.WorkflowConfigKeystore);
        var getEntryResult = await configStore.TryGetEntryAsync<short>(subjectMapper.PerformanceSamplingKey);
        Assert.IsTrue(getEntryResult.Success);
        Assert.AreEqual(getEntryResult.Value.Value, (short)performanceMinutes);
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
        await Assert.ThrowsAsync<ObservationConnectionFailedException>(async () => await ObservationConnection.CreateInstanceAsync(new(options)));
    }

    [TestMethod()]
    public async Task EnsureLimitationsForPerformanceSettingsBlockItems()
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        // Act
        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection));

        // Assert
        Assert.IsNotNull(observationConnection);
        var rangeError = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => await observationConnection.AddPerformanceMonitoringAsync(0,
            (rec)=>ValueTask.CompletedTask,
            (rec)=>ValueTask.CompletedTask
        ));
        Assert.IsNotNull(rangeError);
        Assert.AreEqual("sampleDurationMinutes", rangeError.ParamName);
        Assert.StartsWith("The sampling minutes must be between 1 and 10", rangeError.Message);
        rangeError = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => await observationConnection.AddPerformanceMonitoringAsync(11,
            (rec) => ValueTask.CompletedTask,
            (rec) => ValueTask.CompletedTask
        ));
        Assert.IsNotNull(rangeError);
        Assert.AreEqual("sampleDurationMinutes", rangeError.ParamName);
        Assert.StartsWith("The sampling minutes must be between 1 and 10", rangeError.Message);
        await observationConnection.AddPerformanceMonitoringAsync(10,
            (rec) => ValueTask.CompletedTask,
            (rec) => ValueTask.CompletedTask
        );
        var alreadySetError = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await observationConnection.AddPerformanceMonitoringAsync(10,
            (rec) => ValueTask.CompletedTask,
            (rec) => ValueTask.CompletedTask
        ));
        Assert.IsNotNull(alreadySetError);
        Assert.AreEqual("Performance monitoring has already been added.", alreadySetError.Message);

        // Cleanup
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }

    [TestMethod]
    public async Task EnsureObservationStreamSettingsPropegate()
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        string instanceNamespace = "PropegationSettingsTest";
        string instanceNamespace2 = "PropegationSettingsTest2";
        string groupName = "PropegationSettingsTest";
        var performanceMinutes = 4;
        var performanceMinutes2 = 8;
        var subjectMapperInstance = new SubjectMapper(instanceNamespace);
        var subjectMapperInstance2 = new SubjectMapper(instanceNamespace2);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        // Act
        var connectionInstance = await Connection.CreateInstanceAsync(new(options)
        {
            Namespace = instanceNamespace
        });
        var connectionDefault = await Connection.CreateInstanceAsync(new(options){
            Namespace = instanceNamespace2
        });
        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection)
        {
            GroupName = groupName
        });
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken); // small delay to ensure streams are created
        await observationConnection.AddNamespaceAsync(instanceNamespace);
        await observationConnection.AddPerformanceMonitoringAsync((byte)performanceMinutes, async (workflowRecord) =>
        {
            // handle workflow record
            await Task.CompletedTask;
        }, async (activityRecord) =>
        {
            // handle activity record
            await Task.CompletedTask;
        });
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken); // small delay to ensure streams are created

        var performanceStream = await jsContext.GetStreamAsync(subjectMapperInstance.PerformanceStreamName, cancellationToken: TestContext.CancellationToken);
        var consumer = await performanceStream.GetConsumerAsync(groupName);
        Assert.IsNotNull(consumer);
        Assert.AreEqual(1, consumer.Info.NumWaiting);
        var error = await Assert.ThrowsExactlyAsync<NatsJSApiException>(async()=>_ = await jsContext.GetStreamAsync(subjectMapperInstance2.PerformanceStreamName, cancellationToken: TestContext.CancellationToken));
        Assert.IsNotNull(error);
        Assert.AreEqual("stream not found", error.Message);
        var kc = jsContext.CreateKeyValueStoreContext();
        var configStore = await kc.GetStoreAsync(subjectMapperInstance.WorkflowConfigKeystore);
        var getEntryResult = await configStore.TryGetEntryAsync<short>(subjectMapperInstance.PerformanceSamplingKey);
        Assert.IsTrue(getEntryResult.Success);
        Assert.AreEqual(getEntryResult.Value.Value, (short)performanceMinutes);
        configStore = await kc.GetStoreAsync(subjectMapperInstance2.WorkflowConfigKeystore);
        getEntryResult = await configStore.TryGetEntryAsync<short>(subjectMapperInstance2.PerformanceSamplingKey);
        Assert.IsFalse(getEntryResult.Success);

        await ((IAsyncDisposable)observationConnection).DisposeAsync();
        await natsConnection.DisposeAsync();
        natsConnection = new NatsConnection(options);
        jsContext = new NatsJSContext(natsConnection);

        observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection)
        {
            GroupName = groupName
        });
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken); // small delay to ensure streams are created
        await observationConnection.AddPerformanceMonitoringAsync((byte)performanceMinutes2, async (workflowRecord) =>
        {
            // handle workflow record
            await Task.CompletedTask;
        }, async (activityRecord) =>
        {
            // handle activity record
            await Task.CompletedTask;
        });
        await observationConnection.AddNamespaceAsync(instanceNamespace2);
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken); // small delay to ensure streams are created
        

        //verify
        performanceStream = await jsContext.GetStreamAsync(subjectMapperInstance.PerformanceStreamName, cancellationToken: TestContext.CancellationToken);
        consumer = await performanceStream.GetConsumerAsync(groupName);
        Assert.IsNotNull(consumer);
        Assert.AreEqual(0, consumer.Info.NumWaiting);
        performanceStream = await jsContext.GetStreamAsync(subjectMapperInstance2.PerformanceStreamName, cancellationToken: TestContext.CancellationToken);
        consumer = await performanceStream.GetConsumerAsync(groupName);
        Assert.IsNotNull(consumer);
        Assert.AreEqual(1, consumer.Info.NumWaiting);
        kc = jsContext.CreateKeyValueStoreContext();
        configStore = await kc.GetStoreAsync(subjectMapperInstance.WorkflowConfigKeystore);
        getEntryResult = await configStore.TryGetEntryAsync<short>(subjectMapperInstance.PerformanceSamplingKey);
        Assert.IsTrue(getEntryResult.Success);
        Assert.AreEqual(getEntryResult.Value.Value, (short)performanceMinutes);
        configStore = await kc.GetStoreAsync(subjectMapperInstance2.WorkflowConfigKeystore);
        getEntryResult = await configStore.TryGetEntryAsync<short>(subjectMapperInstance2.PerformanceSamplingKey);
        Assert.IsTrue(getEntryResult.Success);
        Assert.AreEqual(getEntryResult.Value.Value, (short)performanceMinutes2);

        // Assert
        Assert.IsNotNull(connectionInstance);
        await ((IAsyncDisposable)connectionInstance).DisposeAsync();
        Assert.IsNotNull(connectionDefault);
        await ((IAsyncDisposable)connectionDefault).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }

    private sealed class EmptyWorkflow : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await context.WaitAsync(TimeSpan.FromMinutes(1));
        }
    }

    [TestMethod]
    public async Task EnsureObservationNamespaceChangesPropegate()
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        var performanceMinutes = 5;
        string instanceNamespace = "NamespaceChangesTest";
        string instanceNamespace2 = "NamespaceChangesTest2";
        string groupName = "NamespaceChangesTest";
        var subjectMapperInstance = new SubjectMapper(instanceNamespace);
        var subjectMapperInstance2 = new SubjectMapper(instanceNamespace2);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        // Act
        var connectionInstance = await Connection.CreateInstanceAsync(new(natsConnection)
        {
            Namespace = instanceNamespace
        });
        var connectionInstance2 = await Connection.CreateInstanceAsync(new(options)
        {
            Namespace = instanceNamespace2
        });
        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(options)
        {
            GroupName = groupName
        });
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken); // small delay to ensure streams are created
        await observationConnection.AddNamespacesAsync([instanceNamespace, instanceNamespace2]);
        await observationConnection.AddPerformanceMonitoringAsync((byte)performanceMinutes, async (workflowRecord) =>
        {
            // handle workflow record
            await Task.CompletedTask;
        }, async (activityRecord) =>
        {
            // handle activity record
            await Task.CompletedTask;
        });
        await connectionInstance2.RegisterWorkflowAsync<EmptyWorkflow>();
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken); // small delay to ensure streams are created

        var performanceStream = await jsContext.GetStreamAsync(subjectMapperInstance.PerformanceStreamName, cancellationToken: TestContext.CancellationToken);
        var consumer = await performanceStream.GetConsumerAsync(groupName);
        Assert.IsNotNull(consumer);
        Assert.AreEqual(1, consumer.Info.NumWaiting);
        performanceStream = await jsContext.GetStreamAsync(subjectMapperInstance2.PerformanceStreamName, cancellationToken: TestContext.CancellationToken);
        consumer = await performanceStream.GetConsumerAsync(groupName);
        Assert.IsNotNull(consumer);
        Assert.AreEqual(1, consumer.Info.NumWaiting);

        await observationConnection.RemoveNamespaceAsync(instanceNamespace2);
        await Task.Delay(TimeSpan.FromSeconds(30));
        await connectionInstance2.StartWorkflowAsync<EmptyWorkflow>();

        //verify
        Assert.AreEqual(BigInteger.Zero, await observationConnection.GetSuspendedWorkflowCountAsync(instanceNamespace2));
        Assert.AreEqual(BigInteger.Zero, await observationConnection.GetActiveActivityCountAsync(instanceNamespace2));
        Assert.AreEqual(BigInteger.Zero, await observationConnection.GetActiveWorkflowCountAsync(instanceNamespace2));
        performanceStream = await jsContext.GetStreamAsync(subjectMapperInstance2.PerformanceStreamName, cancellationToken: TestContext.CancellationToken);
        consumer = await performanceStream.GetConsumerAsync(groupName);
        Assert.IsNotNull(consumer);
        Assert.AreEqual(0, consumer.Info.NumWaiting);

        //cleanup
        await ((IAsyncDisposable)connectionInstance).DisposeAsync();
        await ((IAsyncDisposable)connectionInstance2).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }

    public TestContext TestContext { get; set; }
}
