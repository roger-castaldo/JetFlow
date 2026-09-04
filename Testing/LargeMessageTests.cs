using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Data;
using JetFlow.Serializers;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Net;
using System.IO.Compression;
using System.Text.Json;

namespace JetFlow.Testing;

[TestClass]
public class LargeMessageTests
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

    private sealed class LargeIOActivity(INatsServerInfo? natsServerInfo) : IActivityWithReturn<string, string>
    {
        public string? IncomingMessage { get; private set; }
        public string? OutgoingMessage { get; private set; }

        Task<string> IActivityWithReturn<string, string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            IncomingMessage= input;
            OutgoingMessage = TestsHelper.GenerateRandomString((natsServerInfo?.MaxPayload??1_048_576)*3);
            return Task.FromResult(OutgoingMessage);
        }
    }
    private sealed class LargeMessageWorkflow : IWorkflow<string>
    {
        public static string? InputMessage { get; private set; }
        public static string? IncomingMessage { get; private set; }
        public static string? OutgoingMessage { get; private set; }
        public static void Reset()
        {
            IncomingMessage = null;
            OutgoingMessage = null;
        }

        async ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string? input)
        {
            InputMessage = input;
            IncomingMessage??=TestsHelper.GenerateRandomString(input?.Length ?? 0);
            var result = await context.ExecuteActivityAsync<LargeIOActivity, string, string>(new ActivityExecutionRequest<string>(IncomingMessage));
            OutgoingMessage = result.Output;
        }
    }

    [TestMethod]
    public async Task ExecuteLargeIOActivities()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        LargeMessageWorkflow.Reset();
        var runId = Guid.Empty;
        var start = string.Empty;
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        await natsConnection.ConnectAsync();
        var jsContext = new NatsJSContext(natsConnection);
        var objContext = jsContext.CreateObjectStoreContext();
        var connectionOptions = new ConnectionOptions(natsConnection, jsContext);
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        var largeIOActivity = new LargeIOActivity(natsConnection.ServerInfo);
        await connection.RegisterWorkflowAsync<LargeMessageWorkflow,string>(cancellationToken: TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityWithReturnAsync<LargeIOActivity, string, string>(largeIOActivity, TestContext.CancellationToken);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<LargeMessageWorkflow>(
            natsConnection,
            subjectMapper,
            async () =>
            {
                start = TestsHelper.GenerateRandomString((natsConnection.ServerInfo?.MaxPayload??1_048_576)*3);
                runId = await connection.StartWorkflowAsync<LargeMessageWorkflow, string>(new(start), TestContext.CancellationToken);
                return runId;
            }
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);

        //Verify
        Assert.IsNotNull(LargeMessageWorkflow.InputMessage);
        Assert.AreEqual(LargeMessageWorkflow.InputMessage, start);
        Assert.IsNotNull(LargeMessageWorkflow.IncomingMessage);
        Assert.AreEqual(LargeMessageWorkflow.IncomingMessage, largeIOActivity.IncomingMessage);
        Assert.IsNotNull(LargeMessageWorkflow.OutgoingMessage);
        Assert.AreEqual(LargeMessageWorkflow.OutgoingMessage, largeIOActivity.OutgoingMessage);
        var largeStore = await objContext.GetObjectStoreAsync(subjectMapper.LargeMessageObjectstore, TestContext.CancellationToken);
        var messageCount = 0;
        await foreach(var file in largeStore.ListAsync(cancellationToken: TestContext.CancellationToken))
        {
            if (file.Name.StartsWith($"{NameHelper.GetWorkflowName<LargeMessageWorkflow>()}/{runId}/"))
            {
                messageCount++;
                var content = await JsonSerializer.DeserializeAsync<string>(new BrotliStream(new MemoryStream(await largeStore.GetBytesAsync(file.Name, TestContext.CancellationToken)), CompressionMode.Decompress), cancellationToken: TestContext.CancellationToken);
                Assert.IsTrue(
                    Equals(content, LargeMessageWorkflow.InputMessage)
                    || Equals(content, LargeMessageWorkflow.IncomingMessage)
                    || Equals(content, LargeMessageWorkflow.OutgoingMessage)
                );
            }
        }
        Assert.AreEqual(3, messageCount);
    }

    [TestMethod]
    public async Task ArchiveLargeIOActivities()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        LargeMessageWorkflow.Reset();
        var runId = Guid.Empty;
        var start = string.Empty;
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        await natsConnection.ConnectAsync();
        var jsContext = new NatsJSContext(natsConnection);
        var objContext = jsContext.CreateObjectStoreContext();
        var connectionOptions = new ConnectionOptions(natsConnection, jsContext);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        var largeIOActivity = new LargeIOActivity(natsConnection.ServerInfo);
        await connection.RegisterWorkflowAsync<LargeMessageWorkflow, string>(new() { CompletionAction = WorkflowCompletionActions.ArchiveThenNothing}, TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityWithReturnAsync<LargeIOActivity, string, string>(largeIOActivity, TestContext.CancellationToken);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForArchive<LargeMessageWorkflow>(
            natsConnection,
            subjectMapper,
            async () =>
            {
                start = TestsHelper.GenerateRandomString((natsConnection.ServerInfo?.MaxPayload??1_048_576)*3);
                runId = await connection.StartWorkflowAsync<LargeMessageWorkflow, string>(new(start), TestContext.CancellationToken);
                return runId;
            }
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);

        //Verify
        Assert.IsNotNull(LargeMessageWorkflow.InputMessage);
        Assert.AreEqual(LargeMessageWorkflow.InputMessage, start);
        Assert.IsNotNull(LargeMessageWorkflow.IncomingMessage);
        Assert.AreEqual(LargeMessageWorkflow.IncomingMessage, largeIOActivity.IncomingMessage);
        Assert.IsNotNull(LargeMessageWorkflow.OutgoingMessage);
        Assert.AreEqual(LargeMessageWorkflow.OutgoingMessage, largeIOActivity.OutgoingMessage);
        var archiveStore = await objContext.GetObjectStoreAsync(subjectMapper.WorkflowArchiveObjectstore, TestContext.CancellationToken);
        var archiveData = await archiveStore.GetBytesAsync($"{NameHelper.GetWorkflowName<LargeMessageWorkflow>()}/{runId}", TestContext.CancellationToken);
        var archive = JsonSerializer.Deserialize<ArchivedWorkflow>(archiveData, Constants.JsonOptions);
        Assert.AreEqual(runId, archive.ID);
        Assert.IsNull(archive.SchedulerId);
        Assert.IsTrue(archive.IsSuccessful);
        Assert.AreEqual(NameHelper.GetWorkflowName<LargeMessageWorkflow>(), archive.Name);
        Assert.AreEqual(WorkflowCompletionActions.ArchiveThenNothing, archive.Options.CompletionAction);
        Assert.AreNotEqual(archive.StartedAt.ToString(), archive.FinishedAt.ToString());
        Assert.IsNotEmpty(archive.Steps);
        Assert.AreEqual(start, archive.Arguments?.ToString());
        Assert.HasCount(1, archive.Steps);
        Assert.AreEqual(NameHelper.GetActivityName<LargeIOActivity>(), archive.Steps[0].Name);
        Assert.AreEqual(LargeMessageWorkflow.IncomingMessage, archive.Steps[0].Input?.ToString());
        Assert.AreEqual(LargeMessageWorkflow.OutgoingMessage, archive.Steps[0].Result?.ToString());
    }

    [TestMethod]
    [DataRow(WorkflowCompletionActions.ArchiveThenPurge)]
    [DataRow(WorkflowCompletionActions.Purge)]
    public async Task ExecuteLargeIOActivitiesThenPurge(WorkflowCompletionActions completionAction)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        LargeMessageWorkflow.Reset();
        var runId = Guid.Empty;
        var start = string.Empty;
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        await natsConnection.ConnectAsync();
        var jsContext = new NatsJSContext(natsConnection);
        var objContext = jsContext.CreateObjectStoreContext();
        var connectionOptions = new ConnectionOptions(natsConnection, jsContext);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        var largeIOActivity = new LargeIOActivity(natsConnection.ServerInfo);
        await connection.RegisterWorkflowAsync<LargeMessageWorkflow, string>(new() { CompletionAction = completionAction }, TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityWithReturnAsync<LargeIOActivity, string, string>(largeIOActivity, TestContext.CancellationToken);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForPurge<LargeMessageWorkflow>(
            natsConnection,
            subjectMapper,
            async () =>
            {
                start = TestsHelper.GenerateRandomString((natsConnection.ServerInfo?.MaxPayload??1_048_576)*3);
                runId = await connection.StartWorkflowAsync<LargeMessageWorkflow, string>(new(start), TestContext.CancellationToken);
                return runId;
            }
        );

        // Assert
        Assert.IsNotNull(result);
        await Task.Delay(TimeSpan.FromMinutes(2), TestContext.CancellationToken); //delaying to ensure purge large messages has run
        await ((IAsyncDisposable)connection).DisposeAsync();

        //Verify
        Assert.IsNotNull(LargeMessageWorkflow.InputMessage);
        Assert.AreEqual(LargeMessageWorkflow.InputMessage, start);
        Assert.IsNotNull(LargeMessageWorkflow.IncomingMessage);
        Assert.AreEqual(LargeMessageWorkflow.IncomingMessage, largeIOActivity.IncomingMessage);
        Assert.IsNotNull(LargeMessageWorkflow.OutgoingMessage);
        Assert.AreEqual(LargeMessageWorkflow.OutgoingMessage, largeIOActivity.OutgoingMessage);
        var largeStore = await objContext.GetObjectStoreAsync(subjectMapper.LargeMessageObjectstore, TestContext.CancellationToken);
        var messageCount = 0;
        await foreach (var file in largeStore.ListAsync(cancellationToken: TestContext.CancellationToken))
        {
            if (file.Name.StartsWith($"{NameHelper.GetWorkflowName<LargeMessageWorkflow>()}/{runId}/"))
                messageCount++;
        }
        Assert.AreEqual(0, messageCount);
    }

    public TestContext TestContext { get; set; }
}
