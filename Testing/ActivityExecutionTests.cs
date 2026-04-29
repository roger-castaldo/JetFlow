using JetFlow.Configs;
using JetFlow.Interfaces;
using JetFlow.Messages;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    private class BasicActivity : IActivity
    {
        public int InvokeCount { get; private set; } = 0;
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            InvokeCount++;
            return Task.CompletedTask;
        }
    }
    private class BasicActivityWithInput : IActivity<string>
    {
        public string? InvokedMessage { get; private set; } = string.Empty;

        Task IActivity<string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            InvokedMessage = input;
            return Task.CompletedTask;
        }
    }
    private class BasicActivityWithReturn : IActivityWithReturn<string>
    {
        public string ReturnedMessage { get; private set; } = string.Empty;

        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            ReturnedMessage = TestsHelper.GenerateRandomString(32);
            return Task.FromResult(ReturnedMessage);
        }
    }
    private class BasicActivityWithInputAndReturn : IActivityWithReturn<string, string>
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
    private class BasicWorkflow : IWorkflow<string>
    {
        public static string? FinalResult { get; private set; } = string.Empty;

        async ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string? input)
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
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<BasicWorkflow, string>();
        await connection.RegisterWorkflowActivityAsync<BasicActivity>(basicActivity, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<BasicActivityWithInput, string>(basicActivityWithInput, CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<BasicActivityWithReturn, string>(basicActivityWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<BasicActivityWithInputAndReturn, string, string>(basicActivityWithInputAndReturn, CancellationToken.None);
        var input = TestsHelper.GenerateRandomString(32);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<BasicWorkflow>(natsConnection, subjectMapper,
            () => connection.StartWorkflowAsync<BasicWorkflow, string>(input, CancellationToken.None)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsTrue(result.HasValue);
        var endResult = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);

        //Verify
        Assert.IsNotNull(endResult);
        Assert.IsTrue(endResult.IsSuccess);
        Assert.AreEqual(1, basicActivity.InvokeCount);
        Assert.AreEqual(input, basicActivityWithInput.InvokedMessage);
        Assert.AreEqual(basicActivityWithReturn.ReturnedMessage, basicActivityWithInputAndReturn.InvokedMessage);
        Assert.IsFalse(string.IsNullOrWhiteSpace(BasicWorkflow.FinalResult));
        Assert.AreEqual(BasicWorkflow.FinalResult, basicActivityWithInputAndReturn.ReturnedMessage);
    }

    private class GenerateRandomString : IActivityWithReturn<string>
    {
        public string GeneratedString { get; private set; } = string.Empty;

        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            GeneratedString = TestsHelper.GenerateRandomString(32);
            return Task.FromResult(GeneratedString);
        }
    }
    private class RecieveRandomStringFromContextByClass : IActivity
    {
        public string? RecievedString { get; private set; } = string.Empty;

        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            RecievedString = await state.GetActivityResultValueAsync<GenerateRandomString, string>();

        }
    }
    private class RecieveRandomStringFromContextByName : IActivity
    {
        public string? RecievedString { get; private set; } = string.Empty;

        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            RecievedString = await state.GetActivityResultValueAsync<string>("GenerateRandomString");
        }
    }
    private class ActivityContextWorkflow : IWorkflow
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
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<ActivityContextWorkflow>();
        await connection.RegisterWorkflowActivityWithReturnAsync<GenerateRandomString, string>(generateRandomString, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<RecieveRandomStringFromContextByClass>(recieveRandomStringByClass, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<RecieveRandomStringFromContextByName>(recieveRandomStringByName, CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<ActivityContextWorkflow>(natsConnection, subjectMapper,
            () => connection.StartWorkflowAsync<ActivityContextWorkflow>(CancellationToken.None)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsTrue(result.HasValue);
        var endResult = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);

        //Verify
        Assert.IsNotNull(endResult);
        Assert.IsTrue(endResult.IsSuccess);
        Assert.IsFalse(string.IsNullOrWhiteSpace(ActivityContextWorkflow.GeneratedString));
        Assert.AreEqual(ActivityContextWorkflow.GeneratedString, recieveRandomStringByClass.RecievedString);
        Assert.AreEqual(ActivityContextWorkflow.GeneratedString, recieveRandomStringByName.RecievedString);
    }
}
