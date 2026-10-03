using JetFlow.Attributes;
using JetFlow.Interfaces;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace JetFlow.Testing.Observation;

[TestClass]
public class PagedQueryTests
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

    [ActivityName("Empty Activity")]
    private sealed class EmptyActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
    [WorkflowName("Observable Workflow With No Input")]
    private sealed class ObservableWorkflowWithNoInput : IWorkflow
    {
        private static TaskCompletionSource completionSource = new();
        public static TaskCompletionSource SuspendTask => completionSource;
        public static void Reset()
        {
            completionSource = new();
        }
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await context.ExecuteActivityAsync<EmptyActivity>(new ActivityExecutionRequest());
            await completionSource.Task;
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
        var awaitableActivity = new EmptyActivity();
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
        await connection.RegisterWorkflowActivityAsync<EmptyActivity>(awaitableActivity);
        
        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection));
        await Task.Delay(TimeSpan.FromSeconds(1)); // small delay to ensure streams are created
        if (instanceNamespace is null)
            await observationConnection.AddDefaultNamespaceAsync();
        else
            await observationConnection.AddNamespaceAsync(instanceNamespace);
        
        // Assert
        Assert.IsNotNull(connection);

        var instanceIDs = await Task.WhenAll([
                connection.StartWorkflowAsync<ObservableWorkflowWithNoInput>(new()).AsTask(),
                connection.StartWorkflowAsync<ObservableWorkflowWithNoInput>(new()).AsTask(),
                connection.StartWorkflowAsync<ObservableWorkflowWithNoInput>(new()).AsTask(),
                connection.StartWorkflowAsync<ObservableWorkflowWithNoInput>(new()).AsTask(),
                connection.StartWorkflowAsync<ObservableWorkflowWithNoInput>(new()).AsTask()
            ]);
        //validate activities are being observed
        await Task.Delay(TimeSpan.FromSeconds(30));
        var currentWorkflows = await observationConnection.QueryWorkflowAsync<ObservableWorkflowWithNoInput>(instanceNamespace, 3);
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(3, currentWorkflows);
        Assert.IsTrue(currentWorkflows.All(wf => instanceIDs.Contains(wf.ID) && wf.Steps.Length==1 && wf.Steps.All(s=>Equals(s.Name, "Empty Activity"))));
        currentWorkflows = await observationConnection.QueryWorkflowAsync<ObservableWorkflowWithNoInput>(instanceNamespace, 3, lastOffset: currentWorkflows.LastOffset);
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(2, currentWorkflows);
        Assert.IsTrue(currentWorkflows.All(wf => instanceIDs.Contains(wf.ID) && wf.Steps.Length==1 && wf.Steps.All(s => Equals(s.Name, "Empty Activity"))));
        currentWorkflows = await observationConnection.QueryWorkflowAsync(instanceNamespace, "Observable Workflow With No Input", 3);
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(3, currentWorkflows);
        Assert.IsTrue(currentWorkflows.All(wf => instanceIDs.Contains(wf.ID) && wf.Steps.Length==1 && wf.Steps.All(s => Equals(s.Name, "Empty Activity"))));
        currentWorkflows = await observationConnection.QueryWorkflowAsync(instanceNamespace, "Observable Workflow With No Input", 3, lastOffset: currentWorkflows.LastOffset);
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(2, currentWorkflows);
        Assert.IsTrue(currentWorkflows.All(wf => instanceIDs.Contains(wf.ID) && wf.Steps.Length==1 && wf.Steps.All(s => Equals(s.Name, "Empty Activity"))));
        ObservableWorkflowWithNoInput.SuspendTask.TrySetResult();
        await Task.Delay(TimeSpan.FromMinutes(1));
        currentWorkflows = await observationConnection.QueryWorkflowAsync<ObservableWorkflowWithNoInput>(instanceNamespace, 3);
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(0, currentWorkflows);
        currentWorkflows = await observationConnection.QueryWorkflowAsync(instanceNamespace, "Observable Workflow With No Input", 3);
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(0, currentWorkflows);
        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }

    [WorkflowName("Suspending Workflow Without Input")]
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
        var meta = new Dictionary<string, string[]>()
        {
            {"key", new string[] {"value1"} }
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

        var instanceIDs = await Task.WhenAll([
                connection.StartWorkflowAsync<SuspendingWorkflowWithoutInput>(new() { MetaData = meta }).AsTask(),
                connection.StartWorkflowAsync<SuspendingWorkflowWithoutInput>(new() { MetaData = meta }).AsTask(),
                connection.StartWorkflowAsync<SuspendingWorkflowWithoutInput>(new() { MetaData = meta }).AsTask(),
                connection.StartWorkflowAsync<SuspendingWorkflowWithoutInput>(new() { MetaData = meta }).AsTask(),
                connection.StartWorkflowAsync<SuspendingWorkflowWithoutInput>(new() { MetaData = meta }).AsTask()
            ]);
        var nonMetaID = await connection.StartWorkflowAsync<SuspendingWorkflowWithoutInput>(new());

        //delay for suspends
        await Task.Delay(TimeSpan.FromSeconds(30));

        //validate delays are being observed
        var currentWorkflows = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithoutInput>(instanceNamespace, 3,
            checkMetaData: (val)=>MetaDataAreEqual(val, meta));
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(3, currentWorkflows);
        Assert.IsTrue(currentWorkflows.All(wf => instanceIDs.Contains(wf.ID) && wf.Steps.Length==1 && wf.Steps.All(s => Equals(s.Type, WorkflowStepTypes.Suspended))));
        currentWorkflows = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithoutInput>(instanceNamespace, 3, lastOffset: currentWorkflows.LastOffset,
            checkMetaData: (val) => MetaDataAreEqual(val, meta));
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(2, currentWorkflows);
        Assert.IsTrue(currentWorkflows.All(wf => instanceIDs.Contains(wf.ID) && wf.Steps.Length==1 && wf.Steps.All(s => Equals(s.Type, WorkflowStepTypes.Suspended))));
        currentWorkflows = await observationConnection.QueryWorkflowAsync(instanceNamespace, "Suspending Workflow Without Input", 3,
            checkMetaData: (val) => MetaDataAreEqual(val, meta));
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(3, currentWorkflows);
        Assert.IsTrue(currentWorkflows.All(wf => instanceIDs.Contains(wf.ID) && wf.Steps.Length==1 && wf.Steps.All(s => Equals(s.Type, WorkflowStepTypes.Suspended))));
        currentWorkflows = await observationConnection.QueryWorkflowAsync(instanceNamespace, "Suspending Workflow Without Input", 3, lastOffset: currentWorkflows.LastOffset,
            checkMetaData: (val) => MetaDataAreEqual(val, meta));
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(2, currentWorkflows);
        Assert.IsTrue(currentWorkflows.All(wf => instanceIDs.Contains(wf.ID) && wf.Steps.Length==1 && wf.Steps.All(s => Equals(s.Type, WorkflowStepTypes.Suspended))));

        await Task.WhenAll(instanceIDs.Select(id => connection.ResumeWorkflowAsync<SuspendingWorkflowWithoutInput>(id).AsTask()));
        await Task.Delay(TimeSpan.FromMinutes(1));
        currentWorkflows = await observationConnection.QueryWorkflowAsync<ObservableWorkflowWithNoInput>(instanceNamespace, 3,
            checkMetaData: (val) => MetaDataAreEqual(val, meta));
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(0, currentWorkflows);
        currentWorkflows = await observationConnection.QueryWorkflowAsync(instanceNamespace, "Suspending Workflow Without Input", 3,
            checkMetaData: (val) => MetaDataAreEqual(val, meta));
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(0, currentWorkflows);



        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }

    [WorkflowName("Suspending Workflow With Input")]
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
        var meta = new Dictionary<string, string[]>()
        {
            {"key", new string[] {"value1"} }
        };
        var arg = "input1";
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

        var instanceIDs = await Task.WhenAll([
            connection.StartWorkflowAsync<SuspendingWorkflowWithInput,string>(new(arg) { MetaData = meta }).AsTask(),
            connection.StartWorkflowAsync<SuspendingWorkflowWithInput,string>(new(arg) { MetaData = meta }).AsTask(),
            connection.StartWorkflowAsync<SuspendingWorkflowWithInput,string>(new(arg) { MetaData = meta }).AsTask(),
            connection.StartWorkflowAsync<SuspendingWorkflowWithInput,string>(new(arg) { MetaData = meta }).AsTask(),
            connection.StartWorkflowAsync<SuspendingWorkflowWithInput,string>(new(arg) { MetaData = meta }).AsTask()
        ]);
        var nonMatchingID = await connection.StartWorkflowAsync<SuspendingWorkflowWithInput, string>(new($"{arg}{arg}"));

        //delay for suspends
        await Task.Delay(TimeSpan.FromSeconds(30));

        //validate delays are being observed
        var currentWorkflows = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithInput, string>(instanceNamespace, 3,
            checkMetaData: (val) => MetaDataAreEqual(val, meta),
            checkArguement: (val) => Equals(val, arg));
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(3, currentWorkflows);
        Assert.IsTrue(currentWorkflows.All(wf => instanceIDs.Contains(wf.ID) && wf.Steps.Length==1 && wf.Steps.All(s => Equals(s.Type, WorkflowStepTypes.Suspended))));
        currentWorkflows = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithInput, string>(instanceNamespace, 3, lastOffset: currentWorkflows.LastOffset,
            checkMetaData: (val) => MetaDataAreEqual(val, meta),
            checkArguement: (val) => Equals(val, arg));
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(2, currentWorkflows);
        Assert.IsTrue(currentWorkflows.All(wf => instanceIDs.Contains(wf.ID) && wf.Steps.Length==1 && wf.Steps.All(s => Equals(s.Type, WorkflowStepTypes.Suspended))));
        currentWorkflows = await observationConnection.QueryWorkflowAsync(instanceNamespace, "Suspending Workflow With Input", 3,
            checkMetaData: (val) => MetaDataAreEqual(val, meta),
            checkArguement: (val) => Equals(val.ToString(), arg));
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(3, currentWorkflows);
        Assert.IsTrue(currentWorkflows.All(wf => instanceIDs.Contains(wf.ID) && wf.Steps.Length==1 && wf.Steps.All(s => Equals(s.Type, WorkflowStepTypes.Suspended))));
        currentWorkflows = await observationConnection.QueryWorkflowAsync(instanceNamespace, "Suspending Workflow With Input", 3, lastOffset: currentWorkflows.LastOffset,
            checkMetaData: (val) => MetaDataAreEqual(val, meta),
            checkArguement: (val) => Equals(val.ToString(), arg));
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(2, currentWorkflows);
        Assert.IsTrue(currentWorkflows.All(wf => instanceIDs.Contains(wf.ID) && wf.Steps.Length==1 && wf.Steps.All(s => Equals(s.Type, WorkflowStepTypes.Suspended))));

        await Task.WhenAll(instanceIDs.Select(id => connection.ResumeWorkflowAsync<SuspendingWorkflowWithInput, string>(id).AsTask()));
        await Task.Delay(TimeSpan.FromMinutes(1));
        currentWorkflows = await observationConnection.QueryWorkflowAsync<SuspendingWorkflowWithInput, string>(instanceNamespace, 3,
            checkMetaData: (val) => MetaDataAreEqual(val, meta),
            checkArguement: (val) => Equals(val, arg));
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(0, currentWorkflows);
        currentWorkflows = await observationConnection.QueryWorkflowAsync(instanceNamespace, "Suspending Workflow With Input", 3,
            checkMetaData: (val) => MetaDataAreEqual(val, meta),
            checkArguement: (val) => Equals(val.ToString(), arg));
        Assert.IsNotNull(currentWorkflows);
        Assert.HasCount(0, currentWorkflows);

        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }

    private static bool MetaDataAreEqual(Dictionary<string, string[]>? meta, Dictionary<string, string[]> supplied)
        => meta!=null
        && meta.Count==supplied.Count
        && meta.All(kvp => supplied.TryGetValue(kvp.Key, out var value) && value.SequenceEqual(kvp.Value));
}
