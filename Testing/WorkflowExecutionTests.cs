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
public class WorkflowExecutionTests
{
    private const string TestNamespace = "WorkflowExecutionTests";

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

    private sealed class EmptyActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class EmptyActivityWorkflow : IWorkflow
    {
        public static readonly TaskCompletionSource DelayStartTask = new();

        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await DelayStartTask.Task;
            await context.ExecuteActivityAsync<EmptyActivity>(new());
        }
    }


    [TestMethod]
    [DataRow(null, DisplayName = "Default Namespace")]
    [DataRow(TestNamespace, DisplayName ="Supplied Namespace")]
    public async Task ExecuteStepPostWorkflowEndFails(string? namespaceValue)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var subjectMapper = new SubjectMapper(namespaceValue);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection)
        {
            Namespace=namespaceValue
        };
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<EmptyActivityWorkflow>(cancellationToken: TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityAsync<EmptyActivity>(new(), CancellationToken.None);

        //Act
        var id = await connection.StartWorkflowAsync<EmptyActivityWorkflow>();
        var (data, headers) = await messageSerializer.EncodeAsync<WorkflowEnd>(new(DateTime.UtcNow, null));
        await natsConnection.PublishAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<EmptyActivityWorkflow>(), id.ToString()), data, headers: headers, cancellationToken: TestContext.CancellationToken);
        _ = Task.Run(async () =>
        {
            await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<EmptyActivityWorkflow>(), "*"), cancellationToken: TestContext.CancellationToken))
            {
                if (Equals(msg.Subject, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<EmptyActivityWorkflow>(), id.ToString())))
                {
                    completion.TrySetResult(msg);
                    break;
                }
            }
        }, TestContext.CancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
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

    private sealed class DelayedWorkflow : IWorkflow
    {
        public static readonly TaskCompletionSource DelayStartTask = new();

        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await DelayStartTask.Task;
            await context.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default Namespace")]
    [DataRow(TestNamespace, DisplayName = "Supplied Namespace")]
    public async Task ExecuteWaitPostWorkflowEndFails(string? namespaceValue)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var subjectMapper = new SubjectMapper(namespaceValue);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection)
        {
            Namespace=namespaceValue
        };
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<DelayedWorkflow>(cancellationToken: TestContext.CancellationToken);

        //Act
        var id = await connection.StartWorkflowAsync<DelayedWorkflow>();
        var (data, headers) = await messageSerializer.EncodeAsync<WorkflowEnd>(new(DateTime.UtcNow, null));
        await natsConnection.PublishAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<DelayedWorkflow>(), id.ToString()), data, headers: headers, cancellationToken: TestContext.CancellationToken);
        _ = Task.Run(async () =>
        {
            await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<DelayedWorkflow>(), "*"), cancellationToken: TestContext.CancellationToken))
            {
                if (Equals(msg.Subject, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<DelayedWorkflow>(), id.ToString())))
                {
                    completion.TrySetResult(msg);
                    break;
                }
            }
        }, TestContext.CancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
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

    private sealed class OtherEmptyActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class InvokeMismatchedActivityWorkflow : IWorkflow
    {
        private bool firstRun = true;
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
    [DataRow(null, DisplayName = "Default Namespace")]
    [DataRow(TestNamespace, DisplayName = "Supplied Namespace")]
    public async Task ExecuteActivitiesInDifferentOrderFails(string? namespaceValue)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var subjectMapper = new SubjectMapper(namespaceValue);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection)
        {
            Namespace=namespaceValue
        };
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<InvokeMismatchedActivityWorkflow>(cancellationToken: TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityAsync<EmptyActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<OtherEmptyActivity>(new(), CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<InvokeMismatchedActivityWorkflow>(
            natsConnection,
            subjectMapper,
            async () => await connection.StartWorkflowAsync<InvokeMismatchedActivityWorkflow>()
        );
        
        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsFalse(endMessage.IsSuccess);
        Assert.AreEqual($"Expected step name {NameHelper.GetActivityName<OtherEmptyActivity>()} but got {NameHelper.GetActivityName<EmptyActivity>()}", endMessage.ErrorMessage);
    }

    private sealed class InvalidDelayStepWorkflow : IWorkflow
    {
        public static readonly TaskCompletionSource DelayStartTask = new();

        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await DelayStartTask.Task;
            await context.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default Namespace")]
    [DataRow(TestNamespace, DisplayName = "Supplied Namespace")]
    public async Task ExecuteWaitWithInvalidStepFails(string? namespaceValue)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var subjectMapper = new SubjectMapper(namespaceValue);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection)
        {
            Namespace=namespaceValue
        };
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<InvalidDelayStepWorkflow>(cancellationToken: TestContext.CancellationToken);

        //Act
        var id = await connection.StartWorkflowAsync<InvalidDelayStepWorkflow>();
        await natsConnection.PublishAsync<byte[]>(subjectMapper.WorkflowStepEnd(NameHelper.GetWorkflowName<InvalidDelayStepWorkflow>(), id.ToString(), NameHelper.GetActivityName<EmptyActivity>()), [], cancellationToken: TestContext.CancellationToken);
        _ = Task.Run(async () =>
        {
            await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<InvalidDelayStepWorkflow>(), "*"), cancellationToken: TestContext.CancellationToken))
            {
                if (Equals(msg.Subject, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<InvalidDelayStepWorkflow>(), id.ToString())))
                {
                    completion.TrySetResult(msg);
                    break;
                }
            }
        }, TestContext.CancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
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

    private sealed class NoActionActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
    private sealed class NoActionActivityWithReturn : IActivityWithReturn<string>
    {
        public string? ResultMessage { get; private set; } = string.Empty;
        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            ResultMessage = TestsHelper.GenerateRandomString(32);
            return Task.FromResult(ResultMessage);
        }
    }
    private sealed class NoActionActivityWithInput : IActivity<string>
    {
        public string? InputMessage { get; private set; } = string.Empty;
        Task IActivity<string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            InputMessage = input;
            return Task.CompletedTask;
        }
    }
    private sealed class NoActionActivityWithInputWithReturn : IActivityWithReturn<string, string>
    {
        public string? InputMessage { get; private set; } = string.Empty;
        public string? ResultMessage { get; private set; } = string.Empty;
        Task<string> IActivityWithReturn<string, string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            InputMessage = input;
            ResultMessage = TestsHelper.GenerateRandomString(32);
            return Task.FromResult(ResultMessage);
        }
    }
    private sealed class ErrorActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    private sealed class ErrorActivityWithReturn : IActivityWithReturn<string>
    {
        public string? ResultMessage { get; private set; } = string.Empty;
        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            ResultMessage = TestsHelper.GenerateRandomString(32);
            throw new NotImplementedException();
        }
    }
    private sealed class TimeoutActivity : IActivity
    {
        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
    private sealed class TimeoutActivityWithReturn : IActivityWithReturn<string>
    {
        public string? ResultMessage { get; private set; } = string.Empty;
        async Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            ResultMessage = TestsHelper.GenerateRandomString(32);
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return ResultMessage;
        }
    }

    private sealed class AllActivityResultsWorkflow : IWorkflow
    {
        public static ActivityResult? NoActResult { get; private set; }
        public static ActivityResult<string>? NoActReturnResult { get; private set; }
        public static string? StringInput { get; private set; }
        public static ActivityResult? NoActWithInputResult { get; private set; }
        public static ActivityResult<string>? NoActWithInputReturnResult { get; private set; }
        public static ActivityResult? ErrorActResult { get; private set; }
        public static ActivityResult<string>? ErrorActWithResult { get; private set; }
        public static ActivityResult? TimeoutActResult { get; private set; }
        public static ActivityResult<string>? TimeoutActWithResult { get; private set; }
        public static bool RanWait { get; private set; } = false;

        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            NoActResult = await context.ExecuteActivityAsync<NoActionActivity>(new());
            NoActReturnResult = await context.ExecuteActivityAsync<NoActionActivityWithReturn, string>(new());

            StringInput??= TestsHelper.GenerateRandomString(32);
            NoActWithInputResult = await context.ExecuteActivityAsync<NoActionActivityWithInput, string>(new(StringInput!));
            NoActWithInputReturnResult = await context.ExecuteActivityAsync<NoActionActivityWithInputWithReturn, string, string>(new(StringInput!));

            ErrorActResult = await context.ExecuteActivityAsync<ErrorActivity>(new());
            ErrorActWithResult = await context.ExecuteActivityAsync<ErrorActivityWithReturn, string>(new());

            TimeoutActResult = await context.ExecuteActivityAsync<TimeoutActivity>(new() { Timeouts=new(AttemptTimeout: TimeSpan.FromSeconds(2)) });
            TimeoutActWithResult = await context.ExecuteActivityAsync<TimeoutActivityWithReturn, string>(new() { Timeouts=new(AttemptTimeout: TimeSpan.FromSeconds(2)) });

            await context.WaitAsync(TimeSpan.FromSeconds(2));
            RanWait=true;
        }
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default Namespace")]
    [DataRow(TestNamespace, DisplayName = "Supplied Namespace")]
    public async Task ExecuteWorkflowWithAllActivityTypes(string? namespaceValue)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var noActWithReturn = new NoActionActivityWithReturn();
        var errorActWithReturn = new ErrorActivityWithReturn();
        var noActWithInput = new NoActionActivityWithInput();
        var noActWithInputWithReturn = new NoActionActivityWithInputWithReturn();
        var timeoutActWithReturn = new TimeoutActivityWithReturn();
        var subjectMapper = new SubjectMapper(namespaceValue);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection)
        {
            Namespace=namespaceValue
        };
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<AllActivityResultsWorkflow>(cancellationToken: TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityAsync<NoActionActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<NoActionActivityWithReturn, string>(noActWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<NoActionActivityWithInput, string>(noActWithInput, CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<NoActionActivityWithInputWithReturn, string, string>(noActWithInputWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<ErrorActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<ErrorActivityWithReturn, string>(errorActWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<TimeoutActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<TimeoutActivityWithReturn, string>(timeoutActWithReturn, CancellationToken.None);
        

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<AllActivityResultsWorkflow>(
            natsConnection,
            subjectMapper,
            async () => await connection.StartWorkflowAsync<AllActivityResultsWorkflow>()
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);

        //Verify
        Assert.IsNotNull(AllActivityResultsWorkflow.NoActResult);
        Assert.AreEqual(ActivityResultStatus.Success, AllActivityResultsWorkflow.NoActResult.Status);
        Assert.IsFalse(string.IsNullOrWhiteSpace(noActWithReturn.ResultMessage));
        Assert.IsNotNull(AllActivityResultsWorkflow.NoActReturnResult);
        Assert.AreEqual(ActivityResultStatus.Success, AllActivityResultsWorkflow.NoActReturnResult.Status);
        Assert.AreEqual(noActWithReturn.ResultMessage, AllActivityResultsWorkflow.NoActReturnResult.Output);
        Assert.IsNotNull(AllActivityResultsWorkflow.NoActWithInputResult);
        Assert.AreEqual(ActivityResultStatus.Success, AllActivityResultsWorkflow.NoActWithInputResult.Status);
        Assert.IsNotNull(AllActivityResultsWorkflow.NoActWithInputResult);
        Assert.AreEqual(ActivityResultStatus.Success, AllActivityResultsWorkflow.NoActWithInputResult.Status);
        Assert.AreEqual(noActWithInput.InputMessage, AllActivityResultsWorkflow.StringInput);
        Assert.IsNotNull(AllActivityResultsWorkflow.NoActWithInputReturnResult);
        Assert.AreEqual(ActivityResultStatus.Success, AllActivityResultsWorkflow.NoActWithInputReturnResult.Status);
        Assert.AreEqual(noActWithInputWithReturn.InputMessage, AllActivityResultsWorkflow.StringInput);
        Assert.AreEqual(noActWithInputWithReturn.ResultMessage, AllActivityResultsWorkflow.NoActWithInputReturnResult.Output);
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
    [DataRow(WorkflowCompletionActions.Purge, null)]
    [DataRow(WorkflowCompletionActions.Purge, TestNamespace)]
    [DataRow(WorkflowCompletionActions.ArchiveThenPurge, null)]
    [DataRow(WorkflowCompletionActions.ArchiveThenPurge, TestNamespace)]
    public async Task ExecuteWorkflowWithPurgeOnCompletion(WorkflowCompletionActions action, string? namespaceValue)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var runId = Guid.Empty;
        var noActWithReturn = new NoActionActivityWithReturn();
        var noActWithInput = new NoActionActivityWithInput();
        var noActWithInputWithReturn = new NoActionActivityWithInputWithReturn();
        var errorActWithReturn = new ErrorActivityWithReturn();
        var timeoutActWithReturn = new TimeoutActivityWithReturn();
        var subjectMapper = new SubjectMapper(namespaceValue);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var connectionOptions = new ConnectionOptions(natsConnection, jsContext)
        {
            Namespace=namespaceValue
        };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<AllActivityResultsWorkflow>(options: new()
        {
            CompletionAction = action
        }, TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityAsync<NoActionActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<NoActionActivityWithReturn, string>(noActWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<NoActionActivityWithInput, string>(noActWithInput, CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<NoActionActivityWithInputWithReturn, string, string>(noActWithInputWithReturn, CancellationToken.None);
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
                runId = await connection.StartWorkflowAsync<AllActivityResultsWorkflow>();
                return runId;
            }
        );

        // Assert
        Assert.IsNotNull(result);
        await Task.Delay(TimeSpan.FromMinutes(1), TestContext.CancellationToken);

        //Verify
        var workflowName = NameHelper.GetWorkflowName<AllActivityResultsWorkflow>();
        var messages = await TestJetStreamHelper.QueryStreamAsync(jsContext, subjectMapper.WorkflowEventsStreamsName, false,
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
            subjectMapper.WorkflowStepRetry(workflowName, runId.ToString(), "*")
        );
        Assert.IsEmpty(messages);

        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
    }

    [TestMethod]
    [DataRow(WorkflowCompletionActions.ArchiveThenNothing, null)]
    [DataRow(WorkflowCompletionActions.ArchiveThenNothing, TestNamespace)]
    [DataRow(WorkflowCompletionActions.ArchiveThenPurge, null)]
    [DataRow(WorkflowCompletionActions.ArchiveThenPurge, TestNamespace)]
    public async Task ExecuteWorkflowWithArchiveOnCompletion(WorkflowCompletionActions action, string? namespaceValue)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var runId = Guid.Empty;
        var metaData = new Dictionary<string, string[]>(){
            { "key1",["value1","value2"] },
            {"key2",["value3"] }
        };
        var noActWithReturn = new NoActionActivityWithReturn();
        var errorActWithReturn = new ErrorActivityWithReturn();
        var noActWithInput = new NoActionActivityWithInput();
        var noActWithInputWithReturn = new NoActionActivityWithInputWithReturn();
        var timeoutActWithReturn = new TimeoutActivityWithReturn();
        var subjectMapper = new SubjectMapper(namespaceValue);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var objContext = jsContext.CreateObjectStoreContext();
        var connectionOptions = new ConnectionOptions(natsConnection, jsContext)
        {
            Namespace = namespaceValue
        };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<AllActivityResultsWorkflow>(options: new()
        {
            CompletionAction = action
        }, TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityAsync<NoActionActivity>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<NoActionActivityWithReturn, string>(noActWithReturn, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<NoActionActivityWithInput, string>(noActWithInput, CancellationToken.None);
        await connection.RegisterWorkflowActivityWithReturnAsync<NoActionActivityWithInputWithReturn, string, string>(noActWithInputWithReturn, CancellationToken.None);
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
                runId = await connection.StartWorkflowAsync<AllActivityResultsWorkflow>(exectionRequest: new() { MetaData=metaData});
                return runId;
            }
        );

        // Assert
        Assert.IsNotNull(result);

        //Verify
        var archiveStore = await objContext.GetObjectStoreAsync(subjectMapper.WorkflowArchiveObjectstore, TestContext.CancellationToken);
        var archiveData = await archiveStore.GetBytesAsync($"{NameHelper.GetWorkflowName<AllActivityResultsWorkflow>()}/{runId}", TestContext.CancellationToken);
        var archive = JsonSerializer.Deserialize<ArchivedWorkflow>(archiveData, Constants.JsonOptions);
        Assert.AreEqual(runId, archive.ID);
        Assert.IsNull(archive.SchedulerId);
        Assert.IsTrue(archive.IsSuccessful);
        Assert.AreEqual(NameHelper.GetWorkflowName<AllActivityResultsWorkflow>(), archive.Name);
        Assert.AreEqual(action, archive.Options.CompletionAction);
        Assert.AreNotEqual(archive.StartedAt.ToString(), archive.FinishedAt.ToString());
        Assert.IsNotNull(archive.MetaData);
        Assert.IsTrue(metaData.All(pair => archive.MetaData.TryGetValue(pair.Key, out var value) && pair.Value.SequenceEqual(value)));
        Assert.AreEqual(metaData.Count, archive.MetaData.Count);
        Assert.IsNotEmpty(archive.Steps);
        AssertStepMatch(archive.Steps, 0, NameHelper.GetActivityName<NoActionActivity>(), ActivityResultStatus.Success, WorkflowStepTypes.Action);
        AssertStepMatch(archive.Steps, 1, NameHelper.GetActivityName<NoActionActivityWithReturn>(), ActivityResultStatus.Success, WorkflowStepTypes.Action);
        Assert.AreEqual(noActWithReturn.ResultMessage, archive.Steps[1].Result?.ToString());
        AssertStepMatch(archive.Steps, 2, NameHelper.GetActivityName<NoActionActivityWithInput>(), ActivityResultStatus.Success, WorkflowStepTypes.Action);
        Assert.AreEqual(noActWithInput.InputMessage, archive.Steps[2].Input?.ToString());
        AssertStepMatch(archive.Steps, 3, NameHelper.GetActivityName<NoActionActivityWithInputWithReturn>(), ActivityResultStatus.Success, WorkflowStepTypes.Action);
        Assert.AreEqual(noActWithInputWithReturn.InputMessage, archive.Steps[3].Input?.ToString());
        Assert.AreEqual(noActWithInputWithReturn.ResultMessage, archive.Steps[3].Result?.ToString());
        AssertStepMatch(archive.Steps, 4, NameHelper.GetActivityName<ErrorActivity>(), ActivityResultStatus.Failure, WorkflowStepTypes.Action);
        Assert.AreEqual(new NotImplementedException().Message, archive.Steps[4].ErrorMessage);
        AssertStepMatch(archive.Steps, 5, NameHelper.GetActivityName<ErrorActivityWithReturn>(), ActivityResultStatus.Failure, WorkflowStepTypes.Action);
        Assert.AreEqual(new NotImplementedException().Message, archive.Steps[5].ErrorMessage);
        AssertStepMatch(archive.Steps, 6, NameHelper.GetActivityName<TimeoutActivity>(), ActivityResultStatus.Timeout, WorkflowStepTypes.Action);
        AssertStepMatch(archive.Steps, 7, NameHelper.GetActivityName<TimeoutActivityWithReturn>(), ActivityResultStatus.Timeout, WorkflowStepTypes.Action);
        AssertStepMatch(archive.Steps, 8, null, null, WorkflowStepTypes.Delay);
        
        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
    }

    private static void AssertStepMatch(WorkflowStep[] steps, int index, string? name, ActivityResultStatus? status, WorkflowStepTypes type)
    {
        var step = steps[index];
        Assert.AreEqual(name, step.Name);
        Assert.IsNull(step.Retries);
        Assert.AreEqual(status, step.Status);
        Assert.AreEqual(type, step.Type);
    }

    public TestContext TestContext { get; set; }
}
