using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Messages;
using JetFlow.Serializers;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using System.Text.Json;

namespace JetFlow.Testing;

[TestClass]
public class WorkflowOptionTests
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

    private class UnregisteredActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private class WorkflowWithUnregisteredActivity : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await context.ExecuteActivityAsync<UnregisteredActivity>(new() { Timeouts = new(OverallTimeout:TimeSpan.FromSeconds(5))});
        }
    }

    [TestMethod]
    public async Task TestErrorOnActivityStartTimeout()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<WorkflowWithUnregisteredActivity>(new() { ErrorOnActivityTimeout=true }, CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<WorkflowWithUnregisteredActivity>(natsConnection, subjectMapper,
            () => connection.StartWorkflowAsync<WorkflowWithUnregisteredActivity>(CancellationToken.None)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsTrue(result.HasValue);
        var endResult = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);

        //Verify
        Assert.IsNotNull(endResult);
        Assert.IsFalse(endResult.IsSuccess);
        Assert.AreEqual($"Activity {NameHelper.GetActivityName<UnregisteredActivity>()} has timed out", endResult.ErrorMessage);
    }

    private class SlowActivity : IActivity
    {
        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    private class WorkflowWithSlowActivity : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await context.ExecuteActivityAsync<SlowActivity>(new() { Timeouts=new(AttemptTimeout: TimeSpan.FromSeconds(5)) });
        }
    }

    [TestMethod]
    public async Task TestErrorOnActivityRunTimeout()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<WorkflowWithSlowActivity>(new() { ErrorOnActivityTimeout=true }, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<SlowActivity>(new(), CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<WorkflowWithSlowActivity>(natsConnection, subjectMapper,
            () => connection.StartWorkflowAsync<WorkflowWithSlowActivity>(CancellationToken.None)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsTrue(result.HasValue);
        var endResult = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);

        //Verify
        Assert.IsNotNull(endResult);
        Assert.IsFalse(endResult.IsSuccess);
        Assert.AreEqual($"Activity {NameHelper.GetActivityName<SlowActivity>()} has timed out", endResult.ErrorMessage);
    }
}
