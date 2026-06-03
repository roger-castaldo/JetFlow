using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Messages;
using JetFlow.Serializers;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Net;
using System.Diagnostics;
using System.Text.Json;

namespace JetFlow.Testing;

[TestClass]
public class ScheduledWorkflowTests
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
    private sealed class CronWorkflowNoInput : IWorkflow
    {
        public static long? ExecutionTimestamp { get; private set; } = null;

        ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            ExecutionTimestamp ??= Stopwatch.GetTimestamp();
            return ValueTask.CompletedTask;
        }
    }

    [TestMethod]
    public async Task ExecuteCronWorkflowWithNoInput()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var cnt = 0;
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<CronWorkflowNoInput>(cancellationToken: TestContext.CancellationToken);
        
        //Act
        _ = Task.Run(async () =>
        {
            await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<CronWorkflowNoInput>(), "*"), cancellationToken: TestContext.CancellationToken))
            {
                cnt++;
                if (cnt>1)
                    completion.TrySetResult(msg);
            }
        }, TestContext.CancellationToken);
        await connection.ScheduleWorkflowAsync<CronWorkflowNoInput>(new WorkflowScheduleBuilder().Build(), cancellationToken: TestContext.CancellationToken);
        var result = await completion.Task;

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);
        Assert.IsTrue(CronWorkflowNoInput.ExecutionTimestamp.HasValue);
        Assert.IsGreaterThanOrEqualTo(1, Math.Ceiling(Stopwatch.GetElapsedTime(CronWorkflowNoInput.ExecutionTimestamp.Value).TotalMinutes));
    }

    private sealed class CronWorkflowWithInput : IWorkflow<string>
    {
        public static long? ExecutionTimestamp { get; private set; } = null;
        public static string? ProvidedInput { get; private set; } = null;

        ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string? input)
        {
            ExecutionTimestamp ??= Stopwatch.GetTimestamp();
            ProvidedInput = input;
            return ValueTask.CompletedTask;
        }
    }

    [TestMethod]
    public async Task ExecuteCronWorkflowWithInput()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var cnt = 0;
        var input = TestsHelper.GenerateRandomString(32);
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<CronWorkflowWithInput, string>(cancellationToken: TestContext.CancellationToken);

        //Act
        _ = Task.Run(async () =>
        {
            await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<CronWorkflowWithInput>(), "*"), cancellationToken: TestContext.CancellationToken))
            {
                cnt++;
                if (cnt>1)
                    completion.TrySetResult(msg);
            }
        }, TestContext.CancellationToken);
        await connection.ScheduleWorkflowAsync<CronWorkflowWithInput, string>(input, new WorkflowScheduleBuilder().Build(), cancellationToken: TestContext.CancellationToken);
        var result = await completion.Task;

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);
        Assert.IsTrue(CronWorkflowWithInput.ExecutionTimestamp.HasValue);
        Assert.IsGreaterThanOrEqualTo(1, Math.Ceiling(Stopwatch.GetElapsedTime(CronWorkflowWithInput.ExecutionTimestamp.Value).TotalMinutes));
        Assert.AreEqual(input, CronWorkflowWithInput.ProvidedInput);
    }

    private sealed class DelayedWorkflowNoInput : IWorkflow
    {
        public static long? ExecutionTimestamp { get; private set; } = null;

        ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            ExecutionTimestamp ??= Stopwatch.GetTimestamp();
            return ValueTask.CompletedTask;
        }
    }

    [TestMethod]
    public async Task ExecuteDelayedWorkflowWithNoInput()
    {
        Assert.IsNotNull(natsTestHarness);
        await Task.Delay(TimeSpan.FromSeconds(30), TestContext.CancellationToken);
        //Arrange
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<DelayedWorkflowNoInput>(cancellationToken: TestContext.CancellationToken);

        //Act
        var startTime = long.MinValue;
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<DelayedWorkflowNoInput>(
            natsConnection,
            subjectMapper,
            async () => {
                startTime = Stopwatch.GetTimestamp();
                _ = await connection.DelayStartWorkflowAsync<DelayedWorkflowNoInput>(TimeSpan.FromSeconds(60), cancellationToken: TestContext.CancellationToken);
                return null;
            }
        );
        var endTime = Stopwatch.GetElapsedTime(startTime);

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);
        Assert.IsTrue(DelayedWorkflowNoInput.ExecutionTimestamp.HasValue);
        Assert.IsGreaterThanOrEqualTo(60, endTime.TotalSeconds);
    }

    private sealed class DelayedWorkflowWithInput : IWorkflow<string>
    {
        public static long? ExecutionTimestamp { get; private set; } = null;
        public static string? ProvidedInput { get; private set; } = null;

        ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string? input)
        {
            ExecutionTimestamp ??= Stopwatch.GetTimestamp();
            ProvidedInput = input;
            return ValueTask.CompletedTask;
        }
    }

    [TestMethod]
    public async Task ExecuteDelayedWorkflowWithInput()
    {
        Assert.IsNotNull(natsTestHarness);
        await Task.Delay(TimeSpan.FromSeconds(30), TestContext.CancellationToken);
        //Arrange
        var input = TestsHelper.GenerateRandomString(32);
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<DelayedWorkflowWithInput, string>(cancellationToken: TestContext.CancellationToken);

        //Act
        var startTime = long.MinValue;
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<DelayedWorkflowWithInput>(
            natsConnection,
            subjectMapper,
            async () => {
                startTime = Stopwatch.GetTimestamp();
                _ = await connection.DelayStartWorkflowAsync<DelayedWorkflowWithInput, string>(input, TimeSpan.FromSeconds(60), cancellationToken: TestContext.CancellationToken);
                return null;
            }
        );
        var endTime = Stopwatch.GetElapsedTime(startTime);

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);
        Assert.IsTrue(DelayedWorkflowWithInput.ExecutionTimestamp.HasValue);
        Assert.IsGreaterThanOrEqualTo(60, endTime.TotalSeconds);
        Assert.AreEqual(input, DelayedWorkflowWithInput.ProvidedInput);
    }

    private sealed class ArchivableDelayedWorkflowWithInput : IWorkflow<string>
    {
        public static string? ProvidedInput { get; private set; } = null;

        ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string? input)
        {
            ProvidedInput = input;
            return ValueTask.CompletedTask;
        }
    }
    [TestMethod]
    public async Task ArchiveDelayedWorkflow()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var input = TestsHelper.GenerateRandomString(32);
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var objContext = jsContext.CreateObjectStoreContext();
        var connectionOptions = new ConnectionOptions(natsConnection, jsContext);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<DelayedWorkflowWithInput, string>(options: new() { CompletionAction = WorkflowCompletionActions.None}, TestContext.CancellationToken);

        //Act
        _ = Task.Run(async () =>
        {
            await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowArchived(NameHelper.GetWorkflowName<DelayedWorkflowWithInput>(), "*"), cancellationToken: TestContext.CancellationToken))
            {
                completion.TrySetResult(msg);
            }
        }, TestContext.CancellationToken);
        var scheduleId = await connection.DelayStartWorkflowAsync<DelayedWorkflowWithInput, string>(input, TimeSpan.FromSeconds(30), options: new() { CompletionAction = WorkflowCompletionActions.ArchiveThenPurge}, TestContext.CancellationToken);
        var result = await completion.Task;
        
        // Assert
        await Task.Delay(TimeSpan.FromSeconds(30), TestContext.CancellationToken); // wait for the workflow to be purged
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var runId = result.Value.Subject.Split('.')[3];

        var archiveStore = await objContext.GetObjectStoreAsync(subjectMapper.WorkflowArchiveObjectstore, TestContext.CancellationToken);
        var archiveData = await archiveStore.GetBytesAsync($"{NameHelper.GetWorkflowName<DelayedWorkflowWithInput>()}/{runId}", TestContext.CancellationToken);
        var archive = JsonSerializer.Deserialize<ArchivedWorkflow>(archiveData, Constants.JsonOptions);
        Assert.AreEqual(Guid.Parse(runId), archive.ID);
        Assert.AreEqual(scheduleId, archive.SchedulerId);
        Assert.IsTrue(archive.IsSuccessful);
        Assert.AreEqual(NameHelper.GetWorkflowName<DelayedWorkflowWithInput>(), archive.Name);
        Assert.AreEqual(WorkflowCompletionActions.ArchiveThenPurge, archive.Options.CompletionAction);
        Assert.AreNotEqual(archive.StartedAt.ToString(), archive.FinishedAt.ToString());
    }

    public TestContext TestContext { get; set; }
}
