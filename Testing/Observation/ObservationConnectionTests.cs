using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

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
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("ensurecreation", DisplayName = "Custom namespace")]
    public async Task EnsureObservationStreamsCreated(string instanceNamespace)
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
        await observationConnection.AddPerformanceMonitoringAsync(1, async (workflowRecord) =>
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

    public TestContext TestContext { get; set; }
}
