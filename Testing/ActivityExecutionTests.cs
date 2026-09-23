using JetFlow.Configs;
using JetFlow.Interfaces;
using JetFlow.Data;
using JetFlow.Serializers;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using JetFlow.Attributes;

namespace JetFlow.Testing;

[TestClass]
public class ActivityExecutionTests
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

    [ActivityName("Basic Activity")]
    private sealed class BasicActivity : IActivity
    {
        public int InvokeCount { get; private set; } = 0;
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            InvokeCount++;
            return Task.CompletedTask;
        }
    }
    [ActivityName("Basic Activity With Input")]
    private sealed class BasicActivityWithInput : IActivity<string>
    {
        public string? InvokedMessage { get; private set; } = string.Empty;

        Task IActivity<string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            InvokedMessage = input;
            return Task.CompletedTask;
        }
    }
    [ActivityName("Basic Activity With Return")]
    private sealed class BasicActivityWithReturn : IActivityWithReturn<string>
    {
        public string ReturnedMessage { get; private set; } = string.Empty;

        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            ReturnedMessage = TestsHelper.GenerateRandomString(32);
            return Task.FromResult(ReturnedMessage);
        }
    }
    [ActivityName("Basic Activity With Input And Return")]
    private sealed class BasicActivityWithInputAndReturn : IActivityWithReturn<string, string>
    {
        public string? InvokedMessage { get; private set; } = string.Empty;
        public string ReturnedMessage { get; private set; } = string.Empty;
        Task<string> IActivityWithReturn<string, string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            InvokedMessage = input;
            ReturnedMessage = TestsHelper.GenerateRandomString(32);
            return Task.FromResult(ReturnedMessage);
        }
    }
    [WorkflowName("Basic Workflow")]
    private sealed class BasicWorkflow : IWorkflow<string>
    {
        public static string? FinalResult { get; private set; } = string.Empty;

        async ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string input)
        {
            _ = await context.ExecuteActivityAsync<BasicActivity>(new());
            _ = await context.ExecuteActivityAsync<BasicActivityWithInput, string>(new(input));
            var result = await context.ExecuteActivityAsync<BasicActivityWithReturn, string>(new());
            result = await context.ExecuteActivityAsync<BasicActivityWithInputAndReturn, string, string>(new(result.Output));
            FinalResult = result.Output;
        }
    }

    [TestMethod]
    public async Task TestActivityVariablesPassThrough()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var basicActivity = new BasicActivity();
        var basicActivityWithInput = new BasicActivityWithInput();
        var basicActivityWithReturn = new BasicActivityWithReturn();
        var basicActivityWithInputAndReturn = new BasicActivityWithInputAndReturn();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<BasicWorkflow, string>(cancellationToken: TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityAsync<BasicActivity>(basicActivity, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<BasicActivityWithInput, string>(basicActivityWithInput, CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<BasicActivityWithReturn, string>(basicActivityWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<BasicActivityWithInputAndReturn, string, string>(basicActivityWithInputAndReturn, CancellationToken.None);
        var input = TestsHelper.GenerateRandomString(32);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<BasicWorkflow>(natsConnection, subjectMapper,
            async () => await connection.StartWorkflowAsync<BasicWorkflow, string>(new(input), CancellationToken.None)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endResult = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);

        //Verify
        Assert.IsNotNull(endResult);
        Assert.IsTrue(endResult.IsSuccess);
        Assert.AreEqual(1, basicActivity.InvokeCount);
        Assert.AreEqual(input, basicActivityWithInput.InvokedMessage);
        Assert.AreEqual(basicActivityWithReturn.ReturnedMessage, basicActivityWithInputAndReturn.InvokedMessage);
        Assert.IsFalse(string.IsNullOrWhiteSpace(BasicWorkflow.FinalResult));
        Assert.AreEqual(BasicWorkflow.FinalResult, basicActivityWithInputAndReturn.ReturnedMessage);
    }

    [ActivityName("Generate Random String")]
    private sealed class GenerateRandomString : IActivityWithReturn<string>
    {
        public string GeneratedString { get; private set; } = string.Empty;

        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            GeneratedString = TestsHelper.GenerateRandomString(32);
            return Task.FromResult(GeneratedString);
        }
    }
    [ActivityName("Recieve Random String From Context By Class")]
    private sealed class RecieveRandomStringFromContextByClass : IActivity
    {
        public string? RecievedString { get; private set; } = string.Empty;

        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            RecievedString = (await state.GetActivityResultValueAsync<GenerateRandomString, string>())?.FirstOrDefault();

        }
    }
    [ActivityName("Recieve Random String From Context By Name")]
    private sealed class RecieveRandomStringFromContextByName : IActivity
    {
        public string? RecievedString { get; private set; } = string.Empty;

        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            RecievedString = (await state.GetActivityResultValueAsync<string>("Generate Random String"))?.FirstOrDefault();
        }
    }
    [WorkflowName("Activity Context Workflow")]
    private sealed class ActivityContextWorkflow : IWorkflow
    {
        public static string? GeneratedString { get; private set; } = string.Empty;

        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            GeneratedString = (await context.ExecuteActivityAsync<GenerateRandomString, string>(new())).Output;
            _ = await context.ExecuteActivityAsync<RecieveRandomStringFromContextByClass>(new());
            _ = await context.ExecuteActivityAsync<RecieveRandomStringFromContextByName>(new());
        }
    }

    [TestMethod]
    public async Task TestActivityContextVariables()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var generateRandomString = new GenerateRandomString();
        var recieveRandomStringByClass = new RecieveRandomStringFromContextByClass();
        var recieveRandomStringByName = new RecieveRandomStringFromContextByName();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<ActivityContextWorkflow>(cancellationToken: TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityWithReturnAsync<GenerateRandomString, string>(generateRandomString, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<RecieveRandomStringFromContextByClass>(recieveRandomStringByClass, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<RecieveRandomStringFromContextByName>(recieveRandomStringByName, CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<ActivityContextWorkflow>(natsConnection, subjectMapper,
            async () => await connection.StartWorkflowAsync<ActivityContextWorkflow>()
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endResult = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);

        //Verify
        Assert.IsNotNull(endResult);
        Assert.IsTrue(endResult.IsSuccess);
        Assert.IsFalse(string.IsNullOrWhiteSpace(ActivityContextWorkflow.GeneratedString));
        Assert.AreEqual(ActivityContextWorkflow.GeneratedString, recieveRandomStringByClass.RecievedString);
        Assert.AreEqual(ActivityContextWorkflow.GeneratedString, recieveRandomStringByName.RecievedString);
    }

    [ActivityName("Generate Random String At Length")]
    private sealed class GenerateRandomStringAtLength : IActivityWithReturn<string, int>
    {
        Task<string> IActivityWithReturn<string, int>.ExecuteAsync(int input, IWorkflowState state, CancellationToken cancellationToken)
            =>Task.FromResult(TestsHelper.GenerateRandomString(input));
    }
    [ActivityName("Recieve Random Strings From Context By Class")]
    private sealed class RecieveRandomStringsFromContextByClass : IActivity
    {
        public IEnumerable<string?>? RecievedStrings { get; private set; } = null;

        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            RecievedStrings = await state.GetActivityResultValueAsync<GenerateRandomStringAtLength, string>();

        }
    }
    [ActivityName("Recieve Random Strings From Context By Name")]
    private sealed class RecieveRandomStringsFromContextByName : IActivity
    {
        public IEnumerable<string?>? RecievedStrings { get; private set; } = null;

        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            RecievedStrings = await state.GetActivityResultValueAsync<string>("Generate Random String At Length");
        }
    }
    [WorkflowName("Parallel Activity Context Workflow")]
    private sealed class ParallelActivityContextWorkflow : IWorkflow
    {
        public static IEnumerable<string?>? GeneratedStrings { get; private set; } = null;

        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            GeneratedStrings = (await context.ExecuteActivitiesAsync<GenerateRandomStringAtLength, string, int>(new([ 16, 32, 64]))).Select(r => r.Output);
            _ = await context.ExecuteActivityAsync<RecieveRandomStringsFromContextByClass>(new());
            _ = await context.ExecuteActivityAsync<RecieveRandomStringsFromContextByName>(new());
        }
    }

    [TestMethod]
    public async Task TestParallelActivityContextVariables()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var generateRandomString = new GenerateRandomStringAtLength();
        var recieveRandomStringByClass = new RecieveRandomStringsFromContextByClass();
        var recieveRandomStringByName = new RecieveRandomStringsFromContextByName();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<ParallelActivityContextWorkflow>(cancellationToken: TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityWithReturnAsync<GenerateRandomStringAtLength, string, int>(generateRandomString, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<RecieveRandomStringsFromContextByClass>(recieveRandomStringByClass, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<RecieveRandomStringsFromContextByName>(recieveRandomStringByName, CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<ParallelActivityContextWorkflow>(natsConnection, subjectMapper,
            async () => await connection.StartWorkflowAsync<ParallelActivityContextWorkflow>()
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endResult = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);

        //Verify
        Assert.IsNotNull(endResult);
        Assert.IsTrue(endResult.IsSuccess);
        Assert.IsNotNull(ParallelActivityContextWorkflow.GeneratedStrings);
        Assert.IsNotNull(recieveRandomStringByClass.RecievedStrings);
        Assert.IsNotNull(recieveRandomStringByName.RecievedStrings);
        CollectionAssert.AreEqual(ParallelActivityContextWorkflow.GeneratedStrings.ToArray(), recieveRandomStringByClass.RecievedStrings.ToArray());
        CollectionAssert.AreEqual(ParallelActivityContextWorkflow.GeneratedStrings.ToArray(), recieveRandomStringByName.RecievedStrings.ToArray());
    }

    public TestContext TestContext { get; set; }
}
