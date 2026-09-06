using JetFlow.Data;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace JetFlow.Testing.Observation;

[TestClass]
public class PerformanceRecordTests
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

    private sealed class TestRecordedActivity : IActivity
    {
        private int counter = 0;

        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            counter++;
            if (counter==1)
                return;
            if (counter==2)
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            else
            {
                counter=0;
                throw new NotImplementedException();
            }
        }
    }
    private sealed class TestRecordedWorkflowSuccess : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            _ = await context.ExecuteActivityAsync<TestRecordedActivity>(new() { Timeouts = new(AttemptTimeout: TimeSpan.FromSeconds(30)) });
            _ = await context.ExecuteActivityAsync<TestRecordedActivity>(new() { Timeouts = new(AttemptTimeout: TimeSpan.FromSeconds(30)) });
            _ = await context.ExecuteActivityAsync<TestRecordedActivity>(new() { Timeouts = new(AttemptTimeout: TimeSpan.FromSeconds(30)) });
        }
    }
    private sealed class TestRecordedWorkflowFailure: IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            _ = await context.ExecuteActivityAsync<TestRecordedActivity>(new() { Timeouts = new(AttemptTimeout: TimeSpan.FromSeconds(30)) });
            _ = await context.ExecuteActivityAsync<TestRecordedActivity>(new() { Timeouts = new(AttemptTimeout: TimeSpan.FromSeconds(30)) });
            _ = await context.ExecuteActivityAsync<TestRecordedActivity>(new() { Timeouts = new(AttemptTimeout: TimeSpan.FromSeconds(30)) });
        }
    }

    [TestMethod]
    public async Task ValidatePerformanceData()
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        var start = DateTimeOffset.UtcNow;
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var connection = await Connection.CreateInstanceAsync(new(natsConnection, jsContext));
        await connection.RegisterWorkflowAsync<TestRecordedWorkflowSuccess>(options: new() { }, TestContext.CancellationToken);
        await connection.RegisterWorkflowAsync<TestRecordedWorkflowFailure>(options: new() { ErrorOnActivityFailure = true, ErrorOnActivityTimeout=true }, TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityAsync<TestRecordedActivity>(cancellationToken: TestContext.CancellationToken);
        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection));
        await observationConnection.AddDefaultNamespaceAsync();

        var workflowRecords = new List<WorkflowPerformanceRecord>();
        var activityRecords = new List<ActivityPerformanceRecord>();

        await observationConnection.AddPerformanceMonitoringAsync(1, async (workflowRecord) =>
        {
            workflowRecords.Add(workflowRecord);
            await Task.CompletedTask;
        }, async (activityRecord) =>
        {
            activityRecords.Add(activityRecord);
            await Task.CompletedTask;
        });
        // Act
        _ = await WorkflowsHelper.StartWorkflowAndWaitForPurge<TestRecordedWorkflowSuccess>(natsConnection, subjectMapper, async () => await connection.StartWorkflowAsync<TestRecordedWorkflowSuccess>(cancellationToken: TestContext.CancellationToken));
        _ = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<TestRecordedWorkflowFailure>(natsConnection, subjectMapper, async () => await connection.StartWorkflowAsync<TestRecordedWorkflowFailure>(cancellationToken: TestContext.CancellationToken));
        _ = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<TestRecordedWorkflowFailure>(natsConnection, subjectMapper, async () => await connection.StartWorkflowAsync<TestRecordedWorkflowFailure>(cancellationToken: TestContext.CancellationToken));

        // Assert
        await Task.Delay(TimeSpan.FromMinutes(1), TestContext.CancellationToken);
        await ((IAsyncDisposable)connection).DisposeAsync();
        //ensure all performance data is collection
        await Task.Delay(TimeSpan.FromMinutes(3), TestContext.CancellationToken);
        await ((IAsyncDisposable)observationConnection).DisposeAsync();

        // Verify
        var successRecords = workflowRecords.Where(wr => Equals(NameHelper.GetWorkflowName<TestRecordedWorkflowSuccess>(), wr.Name));
        var errorRecords = workflowRecords.Where(wr=>Equals(NameHelper.GetWorkflowName<TestRecordedWorkflowFailure>(), wr.Name));

        Assert.AreEqual(1, successRecords.Sum(wr => wr.Started));
        Assert.AreEqual(1, successRecords.Sum(wr => wr.Completed));
        Assert.AreEqual(0, successRecords.Sum(wr => wr.Failed));
        Assert.AreEqual(1, successRecords.Sum(wr => wr.Purged));
        Assert.AreEqual(4, successRecords.Sum(wr => wr.QueueLatencies.Count()));

        Assert.AreEqual(2, errorRecords.Sum(wr => wr.Started));
        Assert.AreEqual(0, errorRecords.Sum(wr => wr.Completed));
        Assert.AreEqual(2, errorRecords.Sum(wr => wr.Failed));
        Assert.AreEqual(2, errorRecords.Sum(wr => wr.Purged));
        Assert.AreEqual(5, errorRecords.Sum(wr => wr.QueueLatencies.Count()));

        Assert.IsTrue(activityRecords.All(ar => Equals(ar.Name, NameHelper.GetActivityName<TestRecordedActivity>())));
        Assert.AreEqual(6, activityRecords.Sum(ar => ar.Started));
        Assert.AreEqual(2, activityRecords.Sum(ar => ar.Completed));
        Assert.AreEqual(2, activityRecords.Sum(ar => ar.Failed));
        Assert.AreEqual(2, activityRecords.Sum(ar => ar.TimedOut));
        Assert.AreEqual(6, activityRecords.Sum(ar => ar.QueueLatencies.Count()));
        Assert.AreEqual(2, activityRecords.Sum(ar => ar.Durations.Count()));

        Assert.IsTrue(workflowRecords.All(wr => wr.Window>=start && wr.Window.Second==0));
        Assert.IsTrue(activityRecords.All(wr => wr.Window>=start && wr.Window.Second==0));
    }

    public TestContext TestContext { get; set; }
}
