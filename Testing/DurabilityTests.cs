using JetFlow.Attributes;
using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Net;
using System.Diagnostics;
using System.Text.Json;

namespace JetFlow.Testing;

[TestClass]
public class DurabilityTests
{
    private static NatsTestHarness? natsTestHarness;
    public TestContext TestContext { get; set; }

    [TestInitialize]
    public async Task InitializeTest()
    {
        natsTestHarness = new NatsTestHarness();
        await natsTestHarness.StartAsync();
        GC.Collect(
            GC.MaxGeneration,
            GCCollectionMode.Forced,
            blocking: true,
            compacting: true);

        GC.WaitForPendingFinalizers();

        GC.Collect(
            GC.MaxGeneration,
            GCCollectionMode.Forced,
            blocking: true,
            compacting: true);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await (natsTestHarness?.DisposeAsync()??ValueTask.CompletedTask);
    }

    [ActivityName("Counting Activity With IO")]
    private sealed class CountingActivityWithIO : IActivityWithReturn<string, int>
    {
        Task<string> IActivityWithReturn<string, int>.ExecuteAsync(int input, IWorkflowState state, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Call count {input}");
        }
    }
    [WorkflowName("Large Activity Count Workflow")]
    private sealed class LargeActivityCountWorkflow : IWorkflow<int>
    {
        public static TimeSpan TotalDuration { get; private set; } = TimeSpan.Zero;
        async ValueTask IWorkflow<int>.ExecuteAsync(IWorkflowContext context, int input)
        {
            var start = Stopwatch.GetTimestamp();
            for (var x = 0; x<input; x++)
                _ = await context.ExecuteActivityAsync<CountingActivityWithIO, string, int>(new(x));
            TotalDuration = Stopwatch.GetElapsedTime(start);
        }
    }

    [TestMethod]
    [DataRow(2_000, 1)]
    [DataRow(1_000, 2)]
    [DataRow(500, 4)]
    [DataRow(200, 10)]
    [DataRow(100, 20)]
    [DataRow(100, 40)]
    [DataRow(100, 80)]
    [DataRow(50, 40)]
    public async Task ExecuteLargeActivityCountRun(int callCount, int workflowCount)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        TestContext.WriteLine($"Heap Before: {GC.GetTotalMemory(false) / 1048576.0:F2} MB");
        long allocatedBefore = GC.GetTotalAllocatedBytes();
        int gen0Before = GC.CollectionCount(0);
        int gen1Before = GC.CollectionCount(1);
        int gen2Before = GC.CollectionCount(2);
        Guid[] runIds = new Guid[workflowCount];
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var objContext = jsContext.CreateObjectStoreContext();
        var connectionOptions = new ConnectionOptions(natsConnection, jsContext);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<LargeActivityCountWorkflow,int>(new() { CompletionAction = WorkflowCompletionActions.Archive });
        await connection.RegisterWorkflowActivityWithReturnAsync<CountingActivityWithIO, string, int>(new());

        LogMemory(TestContext, "Before");

        //Act
        var results = await Task.WhenAll(
            Enumerable.Range(1,workflowCount)
            .Select((idx) =>
                WorkflowsHelper.StartWorkflowAndWaitForArchive<LargeActivityCountWorkflow>(
                natsConnection,
                subjectMapper,
                async () =>
                {
                    runIds[idx-1] = await connection.StartWorkflowAsync<LargeActivityCountWorkflow, int>(new(callCount));
                    return runIds[idx-1];
                })
            )
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsTrue(results.All(r => r!=null));

        LogMemory(TestContext, "After");

        //Verify
        TestContext.WriteLine($"Heap After: {GC.GetTotalMemory(false) / 1048576.0:F2} MB");
        var archiveStore = await objContext.GetObjectStoreAsync(subjectMapper.WorkflowArchiveObjectstore);
        foreach (var runId in runIds)
        {
            var archiveData = await archiveStore.GetBytesAsync($"{NameHelper.GetWorkflowName<LargeActivityCountWorkflow>().cleanedName}/{runId}");
            var archive = JsonSerializer.Deserialize<ArchivedWorkflow>(archiveData, Constants.JsonOptions);
            Assert.AreEqual(runId, archive.ID);
            Assert.IsNull(archive.SchedulerId);
            Assert.IsTrue(archive.IsSuccessful);
            Assert.AreEqual(NameHelper.GetWorkflowName<LargeActivityCountWorkflow>().rawName, archive.Name);
            Assert.AreEqual(WorkflowCompletionActions.Archive, archive.Options.CompletionAction);
            Assert.AreNotEqual(archive.StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"), archive.FinishedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            Assert.IsNotEmpty(archive.Steps);
            Assert.HasCount(callCount, archive.Steps);
            Assert.IsTrue(archive.Steps.All(s =>
                Equals(s.Name, NameHelper.GetActivityName<CountingActivityWithIO>().rawName)
                && Equals(s.Status, ActivityResultStatus.Success)
                && s.EndTime.HasValue
            ));
            var workflowDuration = archive.FinishedAt.Subtract(archive.StartedAt);
            var activityDurations = TimeSpan.FromTicks(archive.Steps.Sum(s => s.EndTime.Value.Subtract(s.StartTime).Ticks));
            TestContext.WriteLine($"Total duration of workflow {workflowDuration}");
            TestContext.WriteLine($"Total duration of activities {activityDurations}");
            TestContext.WriteLine($"Total overhead time {workflowDuration.Subtract(activityDurations)}");
        }
        TestContext.WriteLine($"Total duration of fully achieved workflow {LargeActivityCountWorkflow.TotalDuration}");
        long allocated = GC.GetTotalAllocatedBytes() - allocatedBefore;
        TestContext.WriteLine($"Allocated: {allocated / 1048576.0:F2} MB");
        TestContext.WriteLine($"Gen0 GCs: {GC.CollectionCount(0) - gen0Before}");
        TestContext.WriteLine($"Gen1 GCs: {GC.CollectionCount(1) - gen1Before}");
        TestContext.WriteLine($"Gen2 GCs: {GC.CollectionCount(2) - gen2Before}");
    }

    static void LogMemory(TestContext context, string label)
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();

        context.WriteLine(
            $"{label}: " +
            $"Managed={GC.GetTotalMemory(false) / 1048576.0:F2} MB, " +
            $"Private={process.PrivateMemorySize64 / 1048576.0:F2} MB, " +
            $"WorkingSet={process.WorkingSet64 / 1048576.0:F2} MB, " +
            $"Gen2={GC.CollectionCount(2)}");
    }
}
