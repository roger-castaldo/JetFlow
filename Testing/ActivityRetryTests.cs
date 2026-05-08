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
public class ActivityRetryTests
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

    private class TimeoutActivity : IActivity
    {
        public int CallAttempts { get; private set; } = 0;
        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            CallAttempts++;
            if (state.ActivityAttempt<2)
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return;
        }
    }
    private class WorkflowWithActivityTimeout : IWorkflow
    {
        public static ActivityResult? RunResult { get; private set; } = null;
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            RunResult = await context.ExecuteActivityAsync<TimeoutActivity>(new()
            {
                Retries=new()
                {
                    RetryOnTimeout=true,
                    MaximumAttempts=5
                },
                Timeouts=new(AttemptTimeout: TimeSpan.FromSeconds(5))
            });
        }
    }

    [TestMethod]
    public async Task RetryActivityOnTimeout()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var timeoutActivity = new TimeoutActivity();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<WorkflowWithActivityTimeout>();
        await connection.RegisterWorkflowActivityAsync<TimeoutActivity>(timeoutActivity, CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<WorkflowWithActivityTimeout>(
            natsConnection,
            subjectMapper,
            () => connection.StartWorkflowAsync<WorkflowWithActivityTimeout>(CancellationToken.None)
        );

        // Assert
        Assert.IsNotNull(result);
        await ((IAsyncDisposable)connection).DisposeAsync();

        //Verify
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);
        Assert.AreEqual(3, timeoutActivity.CallAttempts);
        Assert.IsNotNull(WorkflowWithActivityTimeout.RunResult);
        Assert.AreEqual(ActivityResultStatus.Success, WorkflowWithActivityTimeout.RunResult.Status);
    }

    private class UnimplementedActivity : IActivity
    {
        public int CallAttempts { get; private set; } = 0;
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            CallAttempts++;
            if (state.ActivityAttempt<2)
                throw new NotImplementedException();
            return Task.CompletedTask;
        }
    }
    public class WorkflowWithUnimplmentedActivity : IWorkflow
    {
        public static ActivityResult? FirstCallResult { get; private set; } = null;
        public static ActivityResult? SecondCallResult { get; private set; } = null;
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            FirstCallResult = await context.ExecuteActivityAsync<UnimplementedActivity>(new()
            {
                Retries=new()
                {
                    RetryOnError=true,
                    RetryOnTimeout=false,
                    MaximumAttempts=5
                },
                Timeouts=new(AttemptTimeout:TimeSpan.FromSeconds(5))
            });
            SecondCallResult = await context.ExecuteActivityAsync<UnimplementedActivity>(new()
            {
                Retries = new()
                {
                    RetryOnError=true,
                    RetryOnTimeout=false,
                    MaximumAttempts=5,
                    BlockedErrors= [new NotImplementedException().Message]
                },
                Timeouts=new(AttemptTimeout:TimeSpan.FromSeconds(5))
            });
        }
    }

    [TestMethod]
    public async Task RetryActivityOnError()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var unimplementedActivity = new UnimplementedActivity();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<WorkflowWithUnimplmentedActivity>();
        await connection.RegisterWorkflowActivityAsync<UnimplementedActivity>(unimplementedActivity, CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<WorkflowWithUnimplmentedActivity>(
            natsConnection,
            subjectMapper,
            () => connection.StartWorkflowAsync<WorkflowWithUnimplmentedActivity>(CancellationToken.None)
        );

        // Assert
        Assert.IsNotNull(result);
        await ((IAsyncDisposable)connection).DisposeAsync();

        //Verify
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);
        Assert.AreEqual(4, unimplementedActivity.CallAttempts);
        Assert.IsNotNull(WorkflowWithUnimplmentedActivity.FirstCallResult);
        Assert.AreEqual(ActivityResultStatus.Success, WorkflowWithUnimplmentedActivity.FirstCallResult.Status);
        Assert.IsNotNull(WorkflowWithUnimplmentedActivity.SecondCallResult);
        Assert.AreEqual(ActivityResultStatus.Failure, WorkflowWithUnimplmentedActivity.SecondCallResult.Status);
        Assert.AreEqual(new NotImplementedException().Message, WorkflowWithUnimplmentedActivity.SecondCallResult.ErrorMessage);
    }

    private class UnimplementedActivityWithTimers : IActivity
    {
        public List<long> TimeStamps { get; private set; } = [];

        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            TimeStamps.Add(Stopwatch.GetTimestamp());
            throw new NotImplementedException();
        }
    }
    public class WorkflowWithUnimplementedActivityWithTimers : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            _ = await context.ExecuteActivityAsync<UnimplementedActivityWithTimers>(new()
            {
                Retries = new()
                {
                    MaximumAttempts=3,
                    RetryOnError=true,
                    RetryOnTimeout=false,
                    DelayBetween=TimeSpan.FromSeconds(3)
                }
            });
        }
    }
    [TestMethod]
    public async Task RetryActivityOnWithDelay()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var unimplementedActivityWithTimers = new UnimplementedActivityWithTimers();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<WorkflowWithUnimplementedActivityWithTimers>();
        await connection.RegisterWorkflowActivityAsync<UnimplementedActivityWithTimers>(unimplementedActivityWithTimers, CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<WorkflowWithUnimplementedActivityWithTimers>(
            natsConnection,
            subjectMapper,
            () => connection.StartWorkflowAsync<WorkflowWithUnimplementedActivityWithTimers>(CancellationToken.None)
        );

        // Assert
        Assert.IsNotNull(result);
        await ((IAsyncDisposable)connection).DisposeAsync();

        //Verify
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);
        Assert.HasCount(4, unimplementedActivityWithTimers.TimeStamps);
        for(var x = unimplementedActivityWithTimers.TimeStamps.Count-1; x>0; x--)
        {
            var diff = Math.Floor(Stopwatch.GetElapsedTime(unimplementedActivityWithTimers.TimeStamps[x-1])
                .Subtract(Stopwatch.GetElapsedTime(unimplementedActivityWithTimers.TimeStamps[x])).TotalSeconds);
            Assert.IsGreaterThanOrEqualTo(3, diff);
            Assert.IsLessThanOrEqualTo(5, diff);
        }
    }

    private class MultiRetryActivity : IActivity
    {
        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            switch (state.ActivityAttempt)
            {
                case 0:
                case 2:
                    throw new NotImplementedException();
                case 1: await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken); break;
            }
        }
    }
    private class WorkflowWithRetryForArchiving : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            _ = await context.ExecuteActivityAsync<MultiRetryActivity>(new()
            {
                Retries=new()
                {
                    MaximumAttempts=4,
                    DelayBetween=TimeSpan.FromSeconds(3),
                    RetryOnError=true,
                    RetryOnTimeout=true
                },
                Timeouts=new(AttemptTimeout: TimeSpan.FromSeconds(2))
            });
        }
    }
    [TestMethod]
    [DataRow(WorkflowCompletionActions.ArchiveThenNothing)]
    [DataRow(WorkflowCompletionActions.ArchiveThenPurge)]
    public async Task RetryActivitiesWithArchiving(WorkflowCompletionActions action)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var runId = Guid.Empty;
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var objContext = jsContext.CreateObjectStoreContext();
        var connectionOptions = new ConnectionOptions(natsConnection, jsContext);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<WorkflowWithRetryForArchiving>(options: new()
        {
            CompletionAction = action
        });
        await connection.RegisterWorkflowActivityAsync<MultiRetryActivity>(new());


        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForArchive<WorkflowWithRetryForArchiving>(
            natsConnection,
            subjectMapper,
            async () =>
            {
                runId = await connection.StartWorkflowAsync<WorkflowWithRetryForArchiving>(CancellationToken.None);
                return runId;
            }
        );

        // Assert
        Assert.IsNotNull(result);

        //Verify
        var archiveStore = await objContext.GetObjectStoreAsync(subjectMapper.WorkflowArchiveKeystore);
        var archiveData = await archiveStore.GetBytesAsync($"{NameHelper.GetWorkflowName<WorkflowWithRetryForArchiving>()}/{runId}");
        var archive = JsonSerializer.Deserialize<ArchivedWorkflow>(archiveData, JetFlow.Serializers.Constants.JsonOptions);
        Assert.AreEqual(runId, archive.ID);
        Assert.IsTrue(archive.IsSuccessful);
        Assert.AreEqual(NameHelper.GetWorkflowName<WorkflowWithRetryForArchiving>(), archive.Name);
        Assert.AreEqual(action, archive.Options.CompletionAction);
        Assert.AreNotEqual(archive.StartedAt.ToString(), archive.FinishedAt.ToString());
        Assert.HasCount(1, archive.Steps);
        var step = archive.Steps[0];
        Assert.AreEqual(NameHelper.GetActivityName<MultiRetryActivity>(), step.Name);
        Assert.AreEqual(WorkflowStepStatuses.Success, step.Status);
        Assert.IsNotNull(step.Retries);
        Assert.HasCount(3, step.Retries);
        Assert.AreEqual(RetryTypes.Error, step.Retries[0].RetryType);
        Assert.AreEqual(RetryTypes.Timeout, step.Retries[1].RetryType);
        Assert.AreEqual(RetryTypes.Error, step.Retries[2].RetryType);

        var diff = Math.Floor(step.Retries[1].Timestamp.Subtract(step.Retries[0].Timestamp).TotalSeconds);
        Assert.IsGreaterThanOrEqualTo(4, diff);
        Assert.IsLessThanOrEqualTo(7, diff);
        diff = Math.Floor(step.Retries[2].Timestamp.Subtract(step.Retries[1].Timestamp).TotalSeconds);
        Assert.IsGreaterThanOrEqualTo(3, diff);
        Assert.IsLessThanOrEqualTo(5, diff);

        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
    }
}
