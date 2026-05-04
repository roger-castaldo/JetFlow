using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Messages;
using JetFlow.Serializers;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JetFlow.Testing;

[TestClass]
public class WorkflowExecutionTests
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

    private class EmptyActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private class EmptyActivityWorkflow : IWorkflow
    {
        public static readonly TaskCompletionSource DelayStartTask = new();

        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await DelayStartTask.Task;
            await context.ExecuteActivityAsync<EmptyActivity>(new());
        }
    }


    [TestMethod]
    public async Task ExecuteStepPostWorkflowEndFails()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<EmptyActivityWorkflow>();
        await connection.RegisterWorkflowActivityAsync<EmptyActivity>(new(), CancellationToken.None);

        //Act
        var id = await connection.StartWorkflowAsync<EmptyActivityWorkflow>(CancellationToken.None);
        var (data, headers) = await messageSerializer.EncodeAsync<WorkflowEnd>(new(DateTime.UtcNow, null));
        await natsConnection.PublishAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<EmptyActivityWorkflow>(), id.ToString()), data, headers: headers);
        _ = Task.Run(async () =>
        {
            await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<EmptyActivityWorkflow>(), "*")))
            {
                if (Equals(msg.Subject, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<EmptyActivityWorkflow>(), id.ToString())))
                {
                    completion.TrySetResult(msg);
                    break;
                }
            }
        });
        await Task.Delay(TimeSpan.FromSeconds(10));
        EmptyActivityWorkflow.DelayStartTask.TrySetResult();
        var result = await completion.Task;

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsFalse(endMessage.IsSuccess);
        Assert.AreEqual("You are unable to execute an activity inside a completed workflow", endMessage.ErrorMessage);
    }

    private class DelayedWorkflow : IWorkflow
    {
        public static readonly TaskCompletionSource DelayStartTask = new();

        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await DelayStartTask.Task;
            await context.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    [TestMethod]
    public async Task ExecuteWaitPostWorkflowEndFails()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<DelayedWorkflow>();

        //Act
        var id = await connection.StartWorkflowAsync<DelayedWorkflow>(CancellationToken.None);
        var (data, headers) = await messageSerializer.EncodeAsync<WorkflowEnd>(new(DateTime.UtcNow, null));
        await natsConnection.PublishAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<DelayedWorkflow>(), id.ToString()), data, headers: headers);
        _ = Task.Run(async () =>
        {
            await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<DelayedWorkflow>(), "*")))
            {
                if (Equals(msg.Subject, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<DelayedWorkflow>(), id.ToString())))
                {
                    completion.TrySetResult(msg);
                    break;
                }
            }
        });
        await Task.Delay(TimeSpan.FromSeconds(10));
        DelayedWorkflow.DelayStartTask.TrySetResult();
        var result = await completion.Task;

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsFalse(endMessage.IsSuccess);
        Assert.AreEqual("You are unable to execute an activity inside a completed workflow", endMessage.ErrorMessage);
    }

    private class OtherEmptyActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private class InvokeMismatchedActivityWorkflow : IWorkflow
    {
        private static bool firstRun = true;
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            if (firstRun)
            {
                firstRun=false;
                await context.ExecuteActivityAsync<EmptyActivity>(new());
            }
            else
                await context.ExecuteActivityAsync<OtherEmptyActivity>(new());
        }
    }

    [TestMethod]
    public async Task ExecuteActivitiesInDifferentOrderFails()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<InvokeMismatchedActivityWorkflow>();
        await connection.RegisterWorkflowActivityAsync<EmptyActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<OtherEmptyActivity>(new(), CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<InvokeMismatchedActivityWorkflow>(
            natsConnection,
            subjectMapper,
            ()=> connection.StartWorkflowAsync<InvokeMismatchedActivityWorkflow>(CancellationToken.None)
        );
        
        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsFalse(endMessage.IsSuccess);
        Assert.AreEqual($"Expected step name {NameHelper.GetActivityName<OtherEmptyActivity>()} but got {NameHelper.GetActivityName<EmptyActivity>()}", endMessage.ErrorMessage);
    }

    private class InvalidDelayStepWorkflow : IWorkflow
    {
        public static readonly TaskCompletionSource DelayStartTask = new();

        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await DelayStartTask.Task;
            await context.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    [TestMethod]
    public async Task ExecuteWaitWithInvalidStepFails()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<InvalidDelayStepWorkflow>();

        //Act
        var id = await connection.StartWorkflowAsync<InvalidDelayStepWorkflow>(CancellationToken.None);
        await natsConnection.PublishAsync<byte[]>(subjectMapper.WorkflowStepEnd(NameHelper.GetWorkflowName<InvalidDelayStepWorkflow>(), id.ToString(), NameHelper.GetActivityName<EmptyActivity>()), []);
        _ = Task.Run(async () =>
        {
            await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<InvalidDelayStepWorkflow>(), "*")))
            {
                if (Equals(msg.Subject, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<InvalidDelayStepWorkflow>(), id.ToString())))
                {
                    completion.TrySetResult(msg);
                    break;
                }
            }
        });
        await Task.Delay(TimeSpan.FromSeconds(10));
        InvalidDelayStepWorkflow.DelayStartTask.TrySetResult();
        var result = await completion.Task;

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsFalse(endMessage.IsSuccess);
        Assert.AreEqual($"Expected delay finished event but recieved {subjectMapper.WorkflowStepEnd(NameHelper.GetWorkflowName<InvalidDelayStepWorkflow>(), id.ToString(), NameHelper.GetActivityName<EmptyActivity>())}", endMessage.ErrorMessage);
    }

    private class NoActionActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
    private class NoActionActivityWithReturn : IActivityWithReturn<string>
    {
        public string? ResultMessage { get; private set; } = string.Empty;
        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            ResultMessage = TestsHelper.GenerateRandomString(32);
            return Task.FromResult(ResultMessage);
        }
    }
    private class ErrorActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    private class ErrorActivityWithReturn : IActivityWithReturn<string>
    {
        public string? ResultMessage { get; private set; } = string.Empty;
        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            ResultMessage = TestsHelper.GenerateRandomString(32);
            throw new NotImplementedException();
        }
    }
    private class TimeoutActivity : IActivity
    {
        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
    private class TimeoutActivityWithReturn : IActivityWithReturn<string>
    {
        public string? ResultMessage { get; private set; } = string.Empty;
        async Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            ResultMessage = TestsHelper.GenerateRandomString(32);
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return ResultMessage;
        }
    }

    private class AllActivityResultsWorkflow : IWorkflow
    {
        public static ActivityResult? NoActResult { get; private set; }
        public static ActivityResult<string>? NoActReturnResult { get; private set; }
        public static ActivityResult? ErrorActResult { get; private set; }
        public static ActivityResult<string>? ErrorActWithResult { get; private set; }
        public static ActivityResult? TimeoutActResult { get; private set; }
        public static ActivityResult<string>? TimeoutActWithResult { get; private set; }
        public static bool RanWait { get; private set; } = false;

        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            NoActResult = await context.ExecuteActivityAsync<NoActionActivity>(new());
            NoActReturnResult = await context.ExecuteActivityAsync<NoActionActivityWithReturn, string>(new());

            ErrorActResult = await context.ExecuteActivityAsync<ErrorActivity>(new());
            ErrorActWithResult = await context.ExecuteActivityAsync<ErrorActivityWithReturn, string>(new());

            TimeoutActResult = await context.ExecuteActivityAsync<TimeoutActivity>(new() { Timeouts=new(AttemptTimeout: TimeSpan.FromSeconds(2)) });
            TimeoutActWithResult = await context.ExecuteActivityAsync<TimeoutActivityWithReturn, string>(new() { Timeouts=new(AttemptTimeout: TimeSpan.FromSeconds(2)) });

            await context.WaitAsync(TimeSpan.FromSeconds(2));
            RanWait=true;
        }
    }

    [TestMethod]
    public async Task ExecuteWorkflowWithAllActivityTypes()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var noActWithReturn = new NoActionActivityWithReturn();
        var errorActWithReturn = new ErrorActivityWithReturn();
        var timeoutActWithReturn = new TimeoutActivityWithReturn();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<AllActivityResultsWorkflow>();
        await connection.RegisterWorkflowActivityAsync<NoActionActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<NoActionActivityWithReturn, string>(noActWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<ErrorActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<ErrorActivityWithReturn, string>(errorActWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<TimeoutActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<TimeoutActivityWithReturn, string>(timeoutActWithReturn, CancellationToken.None);
        

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<AllActivityResultsWorkflow>(
            natsConnection,
            subjectMapper,
            () => connection.StartWorkflowAsync<AllActivityResultsWorkflow>(CancellationToken.None)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);

        //Verify
        Assert.IsNotNull(AllActivityResultsWorkflow.NoActResult);
        Assert.AreEqual(ActivityResultStatus.Success, AllActivityResultsWorkflow.NoActResult.Status);
        Assert.IsFalse(string.IsNullOrWhiteSpace(noActWithReturn.ResultMessage));
        Assert.IsNotNull(AllActivityResultsWorkflow.NoActReturnResult);
        Assert.AreEqual(ActivityResultStatus.Success, AllActivityResultsWorkflow.NoActReturnResult.Status);
        Assert.AreEqual(noActWithReturn.ResultMessage, AllActivityResultsWorkflow.NoActReturnResult.Output);
        Assert.IsNotNull(AllActivityResultsWorkflow.ErrorActResult);
        Assert.AreEqual(ActivityResultStatus.Failure, AllActivityResultsWorkflow.ErrorActResult.Status);
        Assert.AreEqual(new NotImplementedException().Message, AllActivityResultsWorkflow.ErrorActResult.ErrorMessage);
        Assert.IsFalse(string.IsNullOrWhiteSpace(errorActWithReturn.ResultMessage));
        Assert.IsNotNull(AllActivityResultsWorkflow.ErrorActWithResult);
        Assert.AreEqual(ActivityResultStatus.Failure, AllActivityResultsWorkflow.ErrorActWithResult.Status);
        Assert.AreEqual(new NotImplementedException().Message, AllActivityResultsWorkflow.ErrorActWithResult.ErrorMessage);
        Assert.IsNull(AllActivityResultsWorkflow.ErrorActWithResult.Output);
        Assert.IsNotNull(AllActivityResultsWorkflow.TimeoutActResult);
        Assert.AreEqual(ActivityResultStatus.Timeout, AllActivityResultsWorkflow.TimeoutActResult.Status);
        Assert.IsFalse(string.IsNullOrWhiteSpace(timeoutActWithReturn.ResultMessage));
        Assert.IsNotNull(AllActivityResultsWorkflow.TimeoutActWithResult);
        Assert.AreEqual(ActivityResultStatus.Timeout, AllActivityResultsWorkflow.TimeoutActWithResult.Status);
        Assert.IsNull(AllActivityResultsWorkflow.TimeoutActWithResult.Output);
        Assert.IsTrue(AllActivityResultsWorkflow.RanWait);
    }

    [TestMethod]
    [DataRow(WorkflowCompletionActions.Purge)]
    [DataRow(WorkflowCompletionActions.ArchiveThenPurge)]
    public async Task ExecuteWorkflowWithPurgeOnCompletion(WorkflowCompletionActions action)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var runId = Guid.Empty;
        var purgeRecieved = new TaskCompletionSource();
        var noActWithReturn = new NoActionActivityWithReturn();
        var errorActWithReturn = new ErrorActivityWithReturn();
        var timeoutActWithReturn = new TimeoutActivityWithReturn();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var connectionOptions = new ConnectionOptions(natsConnection, jsContext);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<AllActivityResultsWorkflow>(options: new()
        {
            CompletionAction = action
        });
        await connection.RegisterWorkflowActivityAsync<NoActionActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<NoActionActivityWithReturn, string>(noActWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<ErrorActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<ErrorActivityWithReturn, string>(errorActWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<TimeoutActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<TimeoutActivityWithReturn, string>(timeoutActWithReturn, CancellationToken.None);


        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForPurge<AllActivityResultsWorkflow>(
            natsConnection,
            subjectMapper,
            async () =>
            {
                runId = await connection.StartWorkflowAsync<AllActivityResultsWorkflow>(CancellationToken.None);
                return runId;
            }
        );

        // Assert
        Assert.IsNotNull(result);
        await Task.Delay(TimeSpan.FromSeconds(5));

        //Verify
        var workflowName = NameHelper.GetWorkflowName<AllActivityResultsWorkflow>();
        var messages = await JetStreamHelper.QueryStreamAsync(jsContext, subjectMapper.WorkflowEventsStreamsName, false,
            subjectMapper.WorkflowConfigure(workflowName, runId.ToString()),
            subjectMapper.WorkflowStart(workflowName, runId.ToString()),
            subjectMapper.WorkflowEnd(workflowName, runId.ToString()),
            subjectMapper.WorkflowArchived(workflowName, runId.ToString()),
            subjectMapper.WorkflowPurge(workflowName, runId.ToString()),
            subjectMapper.WorkflowDelayStart(workflowName, runId.ToString()),
            subjectMapper.WorkflowDelayEnd(workflowName, runId.ToString()),
            subjectMapper.WorkflowTimer(workflowName, runId.ToString()),
            subjectMapper.WorkflowStepStart(workflowName, runId.ToString(), "*"),
            subjectMapper.WorkflowStepEnd(workflowName, runId.ToString(), "*"),
            subjectMapper.WorkflowStepError(workflowName, runId.ToString(), "*"),
            subjectMapper.WorkflowStepTimeout(workflowName, runId.ToString(), "*"),
            subjectMapper.WorkflowStepRetry(workflowName, runId.ToString(), "*")
        );
        Assert.IsFalse(messages.Any());

        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
    }

    [TestMethod]
    [DataRow(WorkflowCompletionActions.ArchiveThenNothing)]
    [DataRow(WorkflowCompletionActions.ArchiveThenPurge)]
    public async Task ExecuteWorkflowWithArchiveOnCompletion(WorkflowCompletionActions action)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var runId = Guid.Empty;
        var purgeRecieved = new TaskCompletionSource();
        var noActWithReturn = new NoActionActivityWithReturn();
        var errorActWithReturn = new ErrorActivityWithReturn();
        var timeoutActWithReturn = new TimeoutActivityWithReturn();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var objContext = jsContext.CreateObjectStoreContext();
        var connectionOptions = new ConnectionOptions(natsConnection, jsContext);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<AllActivityResultsWorkflow>(options: new()
        {
            CompletionAction = action
        });
        await connection.RegisterWorkflowActivityAsync<NoActionActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<NoActionActivityWithReturn, string>(noActWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<ErrorActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<ErrorActivityWithReturn, string>(errorActWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<TimeoutActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<TimeoutActivityWithReturn, string>(timeoutActWithReturn, CancellationToken.None);


        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForArchive<AllActivityResultsWorkflow>(
            natsConnection,
            subjectMapper,
            async () =>
            {
                runId = await connection.StartWorkflowAsync<AllActivityResultsWorkflow>(CancellationToken.None);
                return runId;
            }
        );

        // Assert
        Assert.IsNotNull(result);

        //Verify
        var archiveStore = await objContext.GetObjectStoreAsync(subjectMapper.WorkflowArchiveKeystore);
        var archiveData = await archiveStore.GetBytesAsync($"{NameHelper.GetWorkflowName<AllActivityResultsWorkflow>()}/{runId}");
        var archive = JsonSerializer.Deserialize<ArchivedWorkflow>(archiveData, JetFlow.Serializers.Constants.JsonOptions);
        Assert.AreEqual(runId, archive.ID);
        Assert.IsTrue(archive.IsSuccessful);
        Assert.AreEqual(NameHelper.GetWorkflowName<AllActivityResultsWorkflow>(), archive.Name);
        Assert.AreEqual(action, archive.Options.CompletionAction);
        Assert.AreNotEqual(archive.StartedAt.ToString(), archive.FinishedAt.ToString());
        Assert.IsTrue(archive.Steps.Any());
        Assert.AreEqual(1, archive.Steps.Count(s =>
            Equals(NameHelper.GetActivityName<NoActionActivity>(), s.Name) &&
            s.Retries==null &&
            Equals(WorkflowStepStatuses.Success, s.Status) &&
            Equals(WorkflowStepTypes.Action, s.Type)
        ));
        Assert.AreEqual(1, archive.Steps.Count(s =>
            Equals(NameHelper.GetActivityName<NoActionActivityWithReturn>(), s.Name) &&
            s.Retries==null &&
            Equals(WorkflowStepStatuses.Success, s.Status) &&
            Equals(WorkflowStepTypes.Action, s.Type) &&
            Equals(noActWithReturn.ResultMessage, s.Result.ToString())
        ));
        Assert.AreEqual(1, archive.Steps.Count(s =>
            Equals(NameHelper.GetActivityName<ErrorActivity>(), s.Name) &&
            s.Retries==null &&
            Equals(WorkflowStepStatuses.Failure, s.Status) &&
            Equals(WorkflowStepTypes.Action, s.Type) &&
            Equals(new NotImplementedException().Message, s.ErrorMessage)
        ));
        Assert.AreEqual(1, archive.Steps.Count(s =>
            Equals(NameHelper.GetActivityName<ErrorActivityWithReturn>(), s.Name) &&
            s.Retries==null &&
            Equals(WorkflowStepStatuses.Failure, s.Status) &&
            Equals(WorkflowStepTypes.Action, s.Type) &&
            Equals(new NotImplementedException().Message, s.ErrorMessage)
        ));
        Assert.AreEqual(1, archive.Steps.Count(s =>
            Equals(NameHelper.GetActivityName<TimeoutActivity>(), s.Name) &&
            s.Retries==null &&
            Equals(WorkflowStepStatuses.Timeout, s.Status) &&
            Equals(WorkflowStepTypes.Action, s.Type)
        ));
        Assert.AreEqual(1, archive.Steps.Count(s =>
            Equals(NameHelper.GetActivityName<TimeoutActivityWithReturn>(), s.Name) &&
            s.Retries==null &&
            Equals(WorkflowStepStatuses.Timeout, s.Status) &&
            Equals(WorkflowStepTypes.Action, s.Type)
        ));
        Assert.AreEqual(1, archive.Steps.Count(s =>
            s.Name==null &&
            s.Retries==null &&
            Equals(WorkflowStepStatuses.Success, s.Status) &&
            Equals(WorkflowStepTypes.Delay, s.Type)
        ));
        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
    }
}
