using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace JetFlow.Testing.Observation;

[TestClass]
public class QueryTests
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

    private sealed class AwaitableActivity : IActivity
    {
        private TaskCompletionSource completionSource = new();
        public Task Task => completionSource.Task;

        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            completionSource.TrySetResult();
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
    private sealed class ObservableWorkflowWithNoInput : IWorkflow
    {
        private static TaskCompletionSource delayCompletionSource = new();
        private static TaskCompletionSource suspendCompletionSource = new();
        public static Task DelayTask => delayCompletionSource.Task;
        public static Task SuspendTask => suspendCompletionSource.Task;
        public static void Reset()
        {
            delayCompletionSource = new();
            suspendCompletionSource = new();
        }
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await context.ExecuteActivityAsync<AwaitableActivity>(new ActivityExecutionRequest());
            delayCompletionSource.TrySetResult();
            await context.WaitAsync(TimeSpan.FromMinutes(1));
            suspendCompletionSource.TrySetResult();
            await context.SuspendAsync();
        }
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("workflowWithoutInput", DisplayName = "Custom namespace")]
    public async Task QueryWorkflowWithoutInput(string? instanceNamespace)
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        ObservableWorkflowWithNoInput.Reset();
        var awaitableActivity = new AwaitableActivity();
        var subjectMapper = new SubjectMapper(instanceNamespace);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        // Act
        var connection = await Connection.CreateInstanceAsync(new(natsConnection, jsContext)
        {
            Namespace = instanceNamespace
        });
        await connection.RegisterWorkflowAsync<ObservableWorkflowWithNoInput>(new() { CompletionAction = Configs.WorkflowCompletionActions.Purge});
        await connection.RegisterWorkflowActivityAsync<AwaitableActivity>(awaitableActivity);
        
        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection));
        await Task.Delay(TimeSpan.FromSeconds(1)); // small delay to ensure streams are created
        if (instanceNamespace is null)
            await observationConnection.AddDefaultNamespaceAsync();
        else
            await observationConnection.AddNamespaceAsync(instanceNamespace);
        
        // Assert
        Assert.IsNotNull(connection);

        var instanceId = await connection.StartWorkflowAsync<ObservableWorkflowWithNoInput>(new());
        //validate activities are being observed
        await awaitableActivity.Task;
        var workflowQuery = await observationConnection.QueryWorkflowAsync<ObservableWorkflowWithNoInput>(instanceNamespace);
        await foreach (var workflow in workflowQuery)
        {
            Assert.AreEqual(instanceId, workflow.ID);
            Assert.HasCount(1, workflow.Steps);
            Assert.AreEqual(WorkflowStepTypes.Action, workflow.Steps[0].Type);
            Assert.AreEqual(NameHelper.GetActivityName<AwaitableActivity>(), workflow.Steps[0].Name);
            Assert.IsNull(workflow.Steps[0].EndTime);
        }
        var workflowList = await observationConnection.LoadWorkflowsAsync<ObservableWorkflowWithNoInput>(instanceNamespace);
        Assert.HasCount(1, workflowList);
        var workflowInstance = workflowList.First();
        Assert.AreEqual(instanceId, workflowInstance.ID);
        Assert.HasCount(1, workflowInstance.Steps);
        Assert.AreEqual(WorkflowStepTypes.Action, workflowInstance.Steps[0].Type);
        Assert.AreEqual(NameHelper.GetActivityName<AwaitableActivity>(), workflowInstance.Steps[0].Name);
        Assert.IsNull(workflowInstance.Steps[0].EndTime);
        //validate delays are being observed
        await ObservableWorkflowWithNoInput.DelayTask;
        workflowQuery = await observationConnection.QueryWorkflowAsync<ObservableWorkflowWithNoInput>(instanceNamespace);
        await foreach (var workflow in workflowQuery)
        {
            Assert.AreEqual(instanceId, workflow.ID);
            Assert.HasCount(2, workflow.Steps);
            Assert.AreEqual(WorkflowStepTypes.Delay, workflow.Steps[1].Type);
            Assert.IsNull(workflow.Steps[1].Name);
            Assert.IsNull(workflow.Steps[1].EndTime);
        }
        workflowList = await observationConnection.LoadWorkflowsAsync<ObservableWorkflowWithNoInput>(instanceNamespace);
        Assert.HasCount(1, workflowList);
        workflowInstance = workflowList.First();
        Assert.AreEqual(instanceId, workflowInstance.ID);
        Assert.HasCount(2, workflowInstance.Steps);
        Assert.AreEqual(WorkflowStepTypes.Delay, workflowInstance.Steps[1].Type);
        Assert.IsNull(workflowInstance.Steps[1].Name);
        Assert.IsNull(workflowInstance.Steps[1].EndTime);
        //validate suspends are being observed
        await ObservableWorkflowWithNoInput.SuspendTask;
        await Task.Delay(TimeSpan.FromMinutes(1));
        workflowQuery = await observationConnection.QueryWorkflowAsync<ObservableWorkflowWithNoInput>(instanceNamespace);
        await foreach (var workflow in workflowQuery)
        {
            Assert.AreEqual(instanceId, workflow.ID);
            Assert.HasCount(3, workflow.Steps);
            Assert.AreEqual(WorkflowStepTypes.Suspended, workflow.Steps[2].Type);
            Assert.IsNull(workflow.Steps[2].Name);
            Assert.IsNull(workflow.Steps[2].EndTime);
        }
        workflowList = await observationConnection.LoadWorkflowsAsync<ObservableWorkflowWithNoInput>(instanceNamespace);
        Assert.HasCount(1, workflowList);
        workflowInstance = workflowList.First();
        Assert.AreEqual(instanceId, workflowInstance.ID);
        Assert.HasCount(3, workflowInstance.Steps);
        Assert.AreEqual(WorkflowStepTypes.Suspended, workflowInstance.Steps[2].Type);
        Assert.IsNull(workflowInstance.Steps[2].Name);
        Assert.IsNull(workflowInstance.Steps[2].EndTime);

        await connection.ResumeWorkflowAsync<ObservableWorkflowWithNoInput>(instanceId);
        await Task.Delay(TimeSpan.FromSeconds(30));
        workflowList = await observationConnection.LoadWorkflowsAsync<ObservableWorkflowWithNoInput>(instanceNamespace);
        Assert.HasCount(0, workflowList);

        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }


    private sealed class SuspendingWorkflowWithoutInput : IWorkflow
    {
        public SuspendingWorkflowWithoutInput()
        {
        }

        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await context.SuspendAsync();
        }
    }
    [TestMethod]
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("metaWorkflowWithoutInput", DisplayName = "Custom namespace")]
    public async Task QueryWorkflowMetaDataWithoutInput(string? instanceNamespace)
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        var firstMeta = new Dictionary<string, string[]>()
        {
            {"key", new string[] {"value1"} }
        };
        var secondMeta = new Dictionary<string, string[]>()
        {
            {"key", new string[] {"value2"} }
        };
        var thirdMeta = new Dictionary<string, string[]>()
        {
            {"key", new string[] {"value3"} }
        };
        ObservableWorkflowWithNoInput.Reset();
        var subjectMapper = new SubjectMapper(instanceNamespace);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        // Act
        var connection = await Connection.CreateInstanceAsync(new(natsConnection, jsContext)
        {
            Namespace = instanceNamespace
        });
        await connection.RegisterWorkflowAsync<SuspendingWorkflowWithoutInput>(new() { CompletionAction = Configs.WorkflowCompletionActions.Purge });

        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection));
        await Task.Delay(TimeSpan.FromSeconds(1)); // small delay to ensure streams are created
        if (instanceNamespace is null)
            await observationConnection.AddDefaultNamespaceAsync();
        else
            await observationConnection.AddNamespaceAsync(instanceNamespace);

        // Assert
        Assert.IsNotNull(connection);

        var firstInstance = await connection.StartWorkflowAsync<SuspendingWorkflowWithoutInput>(new() { MetaData = firstMeta });
        var secondInstance = await connection.StartWorkflowAsync<SuspendingWorkflowWithoutInput>(new() { MetaData = secondMeta });
        var thirdInstance = await connection.StartWorkflowAsync<SuspendingWorkflowWithoutInput>(new() { MetaData = thirdMeta });

        //delay for suspends
        await Task.Delay(TimeSpan.FromSeconds(30));

        //validate delays are being observed
        var cnt = 0;
        var workflowQuery = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithoutInput>(instanceNamespace,
            (meta) => MetaDataAreEqual(meta, firstMeta));
        await foreach (var workflow in workflowQuery)
            cnt++;
        Assert.AreEqual(1, cnt);
        var workflowList = await observationConnection.LoadWorkflowsAsync<SuspendingWorkflowWithoutInput>(instanceNamespace,
            (meta) => MetaDataAreEqual(meta, firstMeta));
        Assert.HasCount(1, workflowList);
        var workflowInstance = workflowList.First();
        Assert.AreEqual(firstInstance, workflowInstance.ID);
        cnt = 0;
        workflowQuery = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithoutInput>(instanceNamespace,
            (meta) => MetaDataAreEqual(meta, secondMeta));
        await foreach (var workflow in workflowQuery)
            cnt++;
        Assert.AreEqual(1, cnt);
        workflowList = await observationConnection.LoadWorkflowsAsync<SuspendingWorkflowWithoutInput>(instanceNamespace,
            (meta) => MetaDataAreEqual(meta, secondMeta));
        Assert.HasCount(1, workflowList);
        workflowInstance = workflowList.First();
        Assert.AreEqual(secondInstance, workflowInstance.ID);
        cnt = 0;
        workflowQuery = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithoutInput>(instanceNamespace,
            (meta) => MetaDataAreEqual(meta, thirdMeta));
        await foreach (var workflow in workflowQuery)
            cnt++;
        Assert.AreEqual(1, cnt);
        workflowList = await observationConnection.LoadWorkflowsAsync<SuspendingWorkflowWithoutInput>(instanceNamespace,
            (meta) => MetaDataAreEqual(meta, thirdMeta));
        Assert.HasCount(1, workflowList);
        workflowInstance = workflowList.First();
        Assert.AreEqual(thirdInstance, workflowInstance.ID);

        await connection.ResumeWorkflowAsync<SuspendingWorkflowWithoutInput>(firstInstance);
        await connection.ResumeWorkflowAsync<SuspendingWorkflowWithoutInput>(secondInstance);
        await connection.ResumeWorkflowAsync<SuspendingWorkflowWithoutInput>(thirdInstance);

        await Task.Delay(TimeSpan.FromSeconds(30));

        cnt = 0;
        workflowQuery = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithoutInput>(instanceNamespace);
        await foreach (var workflow in workflowQuery)
            cnt++;
        Assert.AreEqual(0, cnt);
        workflowList = await observationConnection.LoadWorkflowsAsync<SuspendingWorkflowWithoutInput>(instanceNamespace);
        Assert.HasCount(0, workflowList);

        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }

    private sealed class SuspendingWorkflowWithInput : IWorkflow<string>
    {
        async ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string input)
        {
            await context.SuspendAsync();
        }
    }
    [TestMethod]
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("metaWorkflowWithInput", DisplayName = "Custom namespace")]
    public async Task QueryWorkflowMetaDataWithInput(string? instanceNamespace)
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        var firstMeta = new Dictionary<string, string[]>()
        {
            {"key", new string[] {"value1"} }
        };
        var firstArg = "input1";
        var secondMeta = new Dictionary<string, string[]>()
        {
            {"key", new string[] {"value2"} }
        };
        var secondArg = "input2";
        var thirdMeta = new Dictionary<string, string[]>()
        {
            {"key", new string[] {"value3"} }
        };
        var thirdArg = "input3";
        ObservableWorkflowWithNoInput.Reset();
        var subjectMapper = new SubjectMapper(instanceNamespace);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        // Act
        var connection = await Connection.CreateInstanceAsync(new(natsConnection, jsContext)
        {
            Namespace = instanceNamespace
        });
        await connection.RegisterWorkflowAsync<SuspendingWorkflowWithInput,string>(new() { CompletionAction = Configs.WorkflowCompletionActions.Purge });

        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection));
        await Task.Delay(TimeSpan.FromSeconds(1)); // small delay to ensure streams are created
        if (instanceNamespace is null)
            await observationConnection.AddDefaultNamespaceAsync();
        else
            await observationConnection.AddNamespaceAsync(instanceNamespace);

        // Assert
        Assert.IsNotNull(connection);

        var firstInstance = await connection.StartWorkflowAsync<SuspendingWorkflowWithInput,string>(new(firstArg) { MetaData = firstMeta });
        var secondInstance = await connection.StartWorkflowAsync<SuspendingWorkflowWithInput, string>(new(secondArg) { MetaData = secondMeta });
        var thirdInstance = await connection.StartWorkflowAsync<SuspendingWorkflowWithInput, string>(new(thirdArg) { MetaData = thirdMeta });

        //delay for suspends
        await Task.Delay(TimeSpan.FromSeconds(30));

        //validate delays are being observed
        var cnt = 0;
        var workflowQuery = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithInput, string>(instanceNamespace,
            (meta) => MetaDataAreEqual(meta, firstMeta),
            (input) => firstArg.Equals(input));
        await foreach (var workflow in workflowQuery)
            cnt++;
        Assert.AreEqual(1, cnt);
        var workflowList = await observationConnection.LoadWorkflowsAsync<SuspendingWorkflowWithInput,string>(instanceNamespace,
            (meta) => MetaDataAreEqual(meta, firstMeta),
            (input) => firstArg.Equals(input));
        Assert.HasCount(1, workflowList);
        var workflowInstance = workflowList.First();
        Assert.AreEqual(firstInstance, workflowInstance.ID);
        cnt = 0;
        workflowQuery = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithInput, string>(instanceNamespace,
            (meta) => MetaDataAreEqual(meta, secondMeta),
            (input) => secondArg.Equals(input));
        await foreach (var workflow in workflowQuery)
            cnt++;
        Assert.AreEqual(1, cnt);
        workflowList = await observationConnection.LoadWorkflowsAsync<SuspendingWorkflowWithInput, string>(instanceNamespace,
            (meta) => MetaDataAreEqual(meta, secondMeta),
            (input) => secondArg.Equals(input));
        Assert.HasCount(1, workflowList);
        workflowInstance = workflowList.First();
        Assert.AreEqual(secondInstance, workflowInstance.ID);
        cnt = 0;
        workflowQuery = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithInput, string>(instanceNamespace,
            (meta) => MetaDataAreEqual(meta, thirdMeta),
            (input) => thirdArg.Equals(input));
        await foreach (var workflow in workflowQuery)
            cnt++;
        Assert.AreEqual(1, cnt);
        workflowList = await observationConnection.LoadWorkflowsAsync<SuspendingWorkflowWithInput, string>(instanceNamespace,
            (meta) => MetaDataAreEqual(meta, thirdMeta),
            (input) => thirdArg.Equals(input));
        Assert.HasCount(1, workflowList);
        workflowInstance = workflowList.First();
        Assert.AreEqual(thirdInstance, workflowInstance.ID);

        await connection.ResumeWorkflowAsync<SuspendingWorkflowWithInput, string>(firstInstance);
        await connection.ResumeWorkflowAsync<SuspendingWorkflowWithInput, string>(secondInstance);
        await connection.ResumeWorkflowAsync<SuspendingWorkflowWithInput, string>(thirdInstance);

        await Task.Delay(TimeSpan.FromSeconds(30));

        cnt = 0;
        workflowQuery = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithInput, string>(instanceNamespace);
        await foreach (var workflow in workflowQuery)
            cnt++;
        Assert.AreEqual(0, cnt);
        workflowList = await observationConnection.LoadWorkflowsAsync<SuspendingWorkflowWithInput, string>(instanceNamespace);
        Assert.HasCount(0, workflowList);

        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("loadworkflow", DisplayName = "Custom namespace")]
    public async Task LoadWorkflowById(string? instanceNamespace)
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        ObservableWorkflowWithNoInput.Reset();
        var subjectMapper = new SubjectMapper(instanceNamespace);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        // Act
        var connection = await Connection.CreateInstanceAsync(new(natsConnection, jsContext)
        {
            Namespace = instanceNamespace
        });
        await connection.RegisterWorkflowAsync<SuspendingWorkflowWithoutInput>(new() { CompletionAction = Configs.WorkflowCompletionActions.Purge });
        await connection.RegisterWorkflowAsync<SuspendingWorkflowWithInput, string>(new() { CompletionAction = Configs.WorkflowCompletionActions.Purge });

        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection));
        await Task.Delay(TimeSpan.FromSeconds(1)); // small delay to ensure streams are created
        if (instanceNamespace is null)
            await observationConnection.AddDefaultNamespaceAsync();
        else
            await observationConnection.AddNamespaceAsync(instanceNamespace);

        // Assert
        Assert.IsNotNull(connection);

        var withInput = await connection.StartWorkflowAsync<SuspendingWorkflowWithInput, string>(new("firstRun") {});
        var withoutInput = await connection.StartWorkflowAsync<SuspendingWorkflowWithoutInput>(new() { });

        //delay for suspends
        await Task.Delay(TimeSpan.FromSeconds(30));

        //validate delays are being observed
        var workflow = await observationConnection.LoadWorkflowAsync<SuspendingWorkflowWithInput, string>(instanceNamespace, withInput);
        Assert.IsTrue(workflow.HasValue);
        Assert.AreEqual(withInput, workflow.Value.ID);
        workflow = await observationConnection.LoadWorkflowAsync<SuspendingWorkflowWithoutInput>(instanceNamespace, withoutInput);
        Assert.IsTrue(workflow.HasValue);
        Assert.AreEqual(withoutInput, workflow.Value.ID);

        await connection.ResumeWorkflowAsync<SuspendingWorkflowWithoutInput>(withoutInput);
        await connection.ResumeWorkflowAsync<SuspendingWorkflowWithInput, string>(withInput);

        await Task.Delay(TimeSpan.FromSeconds(30));

        workflow = await observationConnection.LoadWorkflowAsync<SuspendingWorkflowWithInput, string>(instanceNamespace, withInput);
        Assert.IsFalse(workflow.HasValue);
        workflow = await observationConnection.LoadWorkflowAsync<SuspendingWorkflowWithoutInput>(instanceNamespace, withoutInput);
        Assert.IsFalse(workflow.HasValue);

        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }

    private static bool MetaDataAreEqual(Dictionary<string, string[]>? meta, Dictionary<string, string[]> supplied)
        => meta!=null
        && meta.Count==supplied.Count
        && meta.All(kvp => supplied.TryGetValue(kvp.Key, out var value) && value.SequenceEqual(kvp.Value));
}
