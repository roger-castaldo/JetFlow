using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Data;
using JetFlow.Serializers;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Net;
using System.Text.Json;

namespace JetFlow.Testing;

[TestClass]
public class ParallelActivityTests
{
    private static readonly InvalidDataException UnexpectedActivityResultCount = new("Unexpected activity result count");
    private static readonly InvalidDataException UnexpectedActivityStatus = new("Unexpected activity failure");

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

    private sealed class EmptyActivityWithInput : IActivity<string>
    {
        private readonly List<string?> inputs = [];
        public List<string?> Inputs => inputs;
        Task IActivity<string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            inputs.Add(input);
            return Task.CompletedTask;
        }
    }
    private sealed class ParallelActivityWorkflowWithoutOutput : IWorkflow
    {
        private const int ParallelActivityCount = 10;
        private static readonly List<string> inputs = [];
        public static List<string> Inputs => inputs;
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            if (inputs.Count==0)
            {
                for (var x = 0; x<ParallelActivityCount; x++)
                    inputs.Add(TestsHelper.GenerateRandomString(32));
            }
            var results = await context.ExecuteActivitiesAsync<EmptyActivityWithInput, string>(new(inputs));
            if (results.Count()!=ParallelActivityCount)
                throw UnexpectedActivityResultCount;
            else if (results.Any(r => !Equals(r.Status, ActivityResultStatus.Success)))
                throw UnexpectedActivityStatus;
        }
    }

    [TestMethod]
    public async Task ExecuteParalleActivitiesWithNoOutput()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var emptyActivityWithInput = new EmptyActivityWithInput();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<ParallelActivityWorkflowWithoutOutput>(cancellationToken: TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityAsync<EmptyActivityWithInput, string>(emptyActivityWithInput, TestContext.CancellationToken);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<ParallelActivityWorkflowWithoutOutput>(
            natsConnection,
            subjectMapper,
            async () => await connection.StartWorkflowAsync<ParallelActivityWorkflowWithoutOutput>(cancellationToken:TestContext.CancellationToken)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);

        //Verify
        Assert.HasCount(ParallelActivityWorkflowWithoutOutput.Inputs.Count, emptyActivityWithInput.Inputs);
        foreach (var input in ParallelActivityWorkflowWithoutOutput.Inputs)
            Assert.Contains(input, emptyActivityWithInput.Inputs);
    }

    private sealed class EmptyActivityWithInputAndOutput : IActivityWithReturn<string, string>
    {
        private readonly List<string?> inputs = [];
        private readonly List<string> outputs = [];
        public List<string?> Inputs => inputs;
        public List<string> Outputs => outputs;


        Task<string> IActivityWithReturn<string, string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            inputs.Add(input);
            var result = TestsHelper.GenerateRandomString(32);
            outputs.Add(result);
            return Task.FromResult(result);
        }
    }
    private sealed class ParallelActivityWorkflowWithOutput : IWorkflow
    {
        private const int ParallelActivityCount = 10;
        private static readonly List<string> inputs = [];
        private static readonly List<string> outputs = [];
        public static List<string> Inputs => inputs;
        public static List<string> Outputs => outputs;
        public static void Reset()
        {
            inputs.Clear();
            outputs.Clear();
        }
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            if (inputs.Count==0)
            {
                for (var x = 0; x<ParallelActivityCount; x++)
                    inputs.Add(TestsHelper.GenerateRandomString(32));
            }
            var results = await context.ExecuteActivitiesAsync<EmptyActivityWithInputAndOutput, string, string>(new(inputs));
            if (results.Count()!=ParallelActivityCount)
                throw UnexpectedActivityResultCount;
            else if (results.Any(r => !Equals(r.Status, ActivityResultStatus.Success)))
                throw UnexpectedActivityStatus;
            outputs.AddRange(results.Select(r => r.Output!));
        }
    }

    [TestMethod]
    public async Task ExecuteParalleActivitiesWithOutput()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        ParallelActivityWorkflowWithOutput.Reset();
        var emptyActivityWithInputAndOutput = new EmptyActivityWithInputAndOutput();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<ParallelActivityWorkflowWithOutput>(cancellationToken: TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityWithReturnAsync<EmptyActivityWithInputAndOutput, string, string>(emptyActivityWithInputAndOutput, TestContext.CancellationToken);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<ParallelActivityWorkflowWithOutput>(
            natsConnection,
            subjectMapper,
            async () => await connection.StartWorkflowAsync<ParallelActivityWorkflowWithOutput>(cancellationToken: TestContext.CancellationToken)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);

        //Verify
        Assert.HasCount(ParallelActivityWorkflowWithOutput.Inputs.Count, emptyActivityWithInputAndOutput.Inputs);
        foreach (var input in ParallelActivityWorkflowWithOutput.Inputs)
            Assert.Contains(input, emptyActivityWithInputAndOutput.Inputs);

        Assert.HasCount(ParallelActivityWorkflowWithOutput.Outputs.Count, emptyActivityWithInputAndOutput.Outputs);
        foreach (var output in ParallelActivityWorkflowWithOutput.Outputs)
            Assert.Contains(output, emptyActivityWithInputAndOutput.Outputs);
    }

    [TestMethod]
    public async Task ExecuteParalleActivitiesWithOutputToArchive()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        ParallelActivityWorkflowWithOutput.Reset();
        var runId = Guid.Empty;
        var emptyActivityWithInputAndOutput = new EmptyActivityWithInputAndOutput();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var objContext = jsContext.CreateObjectStoreContext();
        var connectionOptions = new ConnectionOptions(natsConnection, jsContext);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<ParallelActivityWorkflowWithOutput>(new() { CompletionAction = WorkflowCompletionActions.ArchiveThenNothing }, TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityWithReturnAsync<EmptyActivityWithInputAndOutput, string, string>(emptyActivityWithInputAndOutput, TestContext.CancellationToken);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForArchive<ParallelActivityWorkflowWithOutput>(
            natsConnection,
            subjectMapper,
            async () =>
            {
                runId = await connection.StartWorkflowAsync<ParallelActivityWorkflowWithOutput>(cancellationToken:TestContext.CancellationToken);
                return runId;
            }
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        
        //Verify
        Assert.HasCount(ParallelActivityWorkflowWithOutput.Inputs.Count, emptyActivityWithInputAndOutput.Inputs);
        foreach (var input in ParallelActivityWorkflowWithOutput.Inputs)
            Assert.Contains(input, emptyActivityWithInputAndOutput.Inputs);

        Assert.HasCount(ParallelActivityWorkflowWithOutput.Outputs.Count, emptyActivityWithInputAndOutput.Outputs);
        foreach (var output in ParallelActivityWorkflowWithOutput.Outputs)
            Assert.Contains(output, emptyActivityWithInputAndOutput.Outputs);

        var archiveStore = await objContext.GetObjectStoreAsync(subjectMapper.WorkflowArchiveObjectstore, TestContext.CancellationToken);
        var archiveData = await archiveStore.GetBytesAsync($"{NameHelper.GetWorkflowName<ParallelActivityWorkflowWithOutput>()}/{runId}", TestContext.CancellationToken);
        var archive = JsonSerializer.Deserialize<ArchivedWorkflow>(archiveData, Constants.JsonOptions);
        Assert.AreEqual(runId, archive.ID);
        Assert.IsNull(archive.SchedulerId);
        Assert.IsTrue(archive.IsSuccessful);
        Assert.AreEqual(NameHelper.GetWorkflowName<ParallelActivityWorkflowWithOutput>(), archive.Name);
        Assert.AreEqual(WorkflowCompletionActions.ArchiveThenNothing, archive.Options.CompletionAction);
        Assert.AreNotEqual(archive.StartedAt.ToString(), archive.FinishedAt.ToString());
        Assert.IsNotEmpty(archive.Steps);
        Assert.HasCount(emptyActivityWithInputAndOutput.Inputs.Count, archive.Steps);
        var stepIndex = archive.Steps[0].Index;
        Assert.IsTrue(archive.Steps.All(s => 
            Equals(s.Name, NameHelper.GetActivityName<EmptyActivityWithInputAndOutput>())
            && Equals(s.Status, ActivityResultStatus.Success)
            && Equals(stepIndex, s.Index)
            && emptyActivityWithInputAndOutput.Inputs.Contains(s.Input?.ToString())
            && emptyActivityWithInputAndOutput.Outputs.Contains(s.Result?.ToString()??string.Empty)
        ));
    }

    private sealed record EmptyActivityInput(int Index, string Input);

    private sealed class EmptyActivityWithProblems : IActivity<EmptyActivityInput>
    {
        private readonly List<string?> inputs = [];
        public List<string?> Inputs => inputs;
        async Task IActivity<EmptyActivityInput>.ExecuteAsync(EmptyActivityInput? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            inputs.Add(input?.Input);
            if (input?.Index == 3 || input?.Index==5)
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            else if (input?.Index==4 || input?.Index==6)
                throw new InvalidDataException("Simulated error occured");
        }
    }
    private sealed class ParallelActivityWorkflowWithProblems : IWorkflow
    {
        private const int ParallelActivityCount = 10;
        private static readonly List<EmptyActivityInput> inputs = [];
        public static IEnumerable<string> Inputs => inputs.Select(i=>i.Input);
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            if (inputs.Count==0)
            {
                for (var x = 0; x<ParallelActivityCount; x++)
                    inputs.Add(new(x+1,TestsHelper.GenerateRandomString(32)));
            }
            var results = await context.ExecuteActivitiesAsync<EmptyActivityWithProblems, EmptyActivityInput>(new(inputs)
            {
                Timeouts = new(AttemptTimeout: TimeSpan.FromSeconds(3))
            });
            if (results.Count()!=ParallelActivityCount)
                throw UnexpectedActivityResultCount;
        }
    }

    [TestMethod]
    [DataRow(true, false, DisplayName = "Error on activity timeout enabled, error on activity failure disabled")]
    [DataRow(false, true, DisplayName = "Error on activity timeout disabled, error on activity failure enabled")]
    [DataRow(true, true, DisplayName = "Error on activity timeout enabled, error on activity failure enabled")]
    public async Task ExecuteParalleActivitiesWithProblems(bool errorOnTimeout, bool errorOnFailure)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var emptyActivityToTimeout = new EmptyActivityWithProblems();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<ParallelActivityWorkflowWithProblems>(new()
        {
            ErrorOnActivityTimeout=errorOnTimeout,
            ErrorOnActivityFailure=errorOnFailure
        }, TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityAsync<EmptyActivityWithProblems, EmptyActivityInput>(emptyActivityToTimeout, TestContext.CancellationToken);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<ParallelActivityWorkflowWithProblems>(
            natsConnection,
            subjectMapper,
            async () => await connection.StartWorkflowAsync<ParallelActivityWorkflowWithProblems>(cancellationToken: TestContext.CancellationToken)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsFalse(endMessage.IsSuccess);
        if (errorOnFailure)
            Assert.AreEqual($"Activity {NameHelper.GetActivityName<EmptyActivityWithProblems>()} has failed with error: 3: Simulated error occured; 5: Simulated error occured; 2: Activity timed out; 4: Activity timed out", endMessage.ErrorMessage);
        else
            Assert.AreEqual($"Activity {NameHelper.GetActivityName<EmptyActivityWithProblems>()} has timed out: 2: Activity timed out; 4: Activity timed out", endMessage.ErrorMessage);

        //Verify
        Assert.HasCount(ParallelActivityWorkflowWithProblems.Inputs.Count(), emptyActivityToTimeout.Inputs);
        foreach (var input in ParallelActivityWorkflowWithProblems.Inputs)
            Assert.Contains(input, emptyActivityToTimeout.Inputs);
    }

    private sealed class ParallelActivityWorkflowWithLargeNumberOfCalls : IWorkflow
    {
        private const int ParallelActivityCount = 1002;
        private static readonly List<string> inputs = [];
        public static List<string> Inputs => inputs;
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            if (inputs.Count==0)
            {
                for (var x = 0; x<ParallelActivityCount; x++)
                    inputs.Add(TestsHelper.GenerateRandomString(32));
            }
            var results = await context.ExecuteActivitiesAsync<EmptyActivityWithInput, string>(new(inputs));
            if (results.Count()!=ParallelActivityCount)
                throw UnexpectedActivityResultCount;
            else if (results.Any(r => !Equals(r.Status, ActivityResultStatus.Success)))
                throw UnexpectedActivityStatus;
        }
    }

    [TestMethod]
    public async Task ExecuteParalleActivitiesWithLargeNumberOfCalls()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var emptyActivityWithInput = new EmptyActivityWithInput();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<ParallelActivityWorkflowWithLargeNumberOfCalls>(cancellationToken: TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityAsync<EmptyActivityWithInput, string>(emptyActivityWithInput, TestContext.CancellationToken);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<ParallelActivityWorkflowWithLargeNumberOfCalls>(
            natsConnection,
            subjectMapper,
            async () => await connection.StartWorkflowAsync<ParallelActivityWorkflowWithLargeNumberOfCalls>(cancellationToken: TestContext.CancellationToken)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);

        //Verify
        Assert.HasCount(ParallelActivityWorkflowWithLargeNumberOfCalls.Inputs.Count, emptyActivityWithInput.Inputs);
        foreach (var input in ParallelActivityWorkflowWithLargeNumberOfCalls.Inputs)
            Assert.Contains(input, emptyActivityWithInput.Inputs);
    }

    public TestContext TestContext { get; set; }
}
