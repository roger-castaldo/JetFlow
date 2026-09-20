using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Testing.Helpers;

namespace JetFlow.Testing.Observation;

[TestClass]
public class ServiceabilityTests
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

    private sealed class WorkflowWithNoAction : IWorkflow
    {
        public static TaskCompletionSource TaskCompletionSource { get; set; }

        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await TaskCompletionSource.Task;
        }
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("idleWorkflows", DisplayName = "Custom namespace")]
    public async Task TestWorkflowInstances(string? instanceNamespace)
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        WorkflowWithNoAction.TaskCompletionSource = new();
        var options = natsTestHarness.Options;

        // Act
        var connection = await Connection.CreateInstanceAsync(new(options)
        {
            Namespace = instanceNamespace
        });
        await connection.RegisterWorkflowAsync<WorkflowWithNoAction>(new());

        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(options));
        await Task.Delay(TimeSpan.FromSeconds(1)); // small delay to ensure streams are created
        if (instanceNamespace is null)
            await observationConnection.AddDefaultNamespaceAsync();
        else
            await observationConnection.AddNamespaceAsync(instanceNamespace);

        //Assert
        var workflowDetail = await observationConnection.GetWorkflowServiceabilityAsync<WorkflowWithNoAction>(instanceNamespace);
        Assert.IsNotNull(workflowDetail);
        ConfirmInstanceValues(workflowDetail, 1, 0, 0);
        var workflows = await observationConnection.GetWorkflowServiceabilityAsync(instanceNamespace);
        Assert.IsGreaterThanOrEqualTo(1, workflows.Count());
        var workflow = workflows.FirstOrDefault(wf => Equals(NameHelper.GetWorkflowName<WorkflowWithNoAction>().rawName, wf.Name));
        Assert.IsNotNull(workflow);
        ConfirmInstanceValues(workflow, 1, 0, 0);

        _ = await connection.StartWorkflowAsync<WorkflowWithNoAction>();
        _ = await connection.StartWorkflowAsync<WorkflowWithNoAction>();
        await Task.Delay(TimeSpan.FromSeconds(10));

        workflowDetail = await observationConnection.GetWorkflowServiceabilityAsync<WorkflowWithNoAction>(instanceNamespace);
        Assert.IsNotNull(workflowDetail);
        ConfirmInstanceValues(workflowDetail, 1, 2, 0);
        workflows = await observationConnection.GetWorkflowServiceabilityAsync(instanceNamespace);
        Assert.IsGreaterThanOrEqualTo(1, workflows.Count());
        workflow = workflows.FirstOrDefault(wf => Equals(NameHelper.GetWorkflowName<WorkflowWithNoAction>().rawName, wf.Name));
        Assert.IsNotNull(workflow);
        ConfirmInstanceValues(workflow, 1, 2, 0);

        WorkflowWithNoAction.TaskCompletionSource.TrySetResult();
        await Task.Delay(TimeSpan.FromSeconds(30));

        workflowDetail = await observationConnection.GetWorkflowServiceabilityAsync<WorkflowWithNoAction>(instanceNamespace);
        Assert.IsNotNull(workflowDetail);
        ConfirmInstanceValues(workflowDetail, 1, 0, 0);
        workflows = await observationConnection.GetWorkflowServiceabilityAsync(instanceNamespace);
        Assert.IsGreaterThanOrEqualTo(1, workflows.Count());
        workflow = workflows.FirstOrDefault(wf => Equals(NameHelper.GetWorkflowName<WorkflowWithNoAction>().rawName, wf.Name));
        Assert.IsNotNull(workflow);
        ConfirmInstanceValues(workflow, 1, 0, 0);

        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }

    private sealed class ActivityWithNoAction : IActivity
    {
        public static TaskCompletionSource TaskCompletionSource { get; set; }

        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            await TaskCompletionSource.Task;
        }
    }
    private sealed class WorkflowWithSingleAction : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await context.ExecuteActivityAsync<ActivityWithNoAction>(new());
        }
    }
    [TestMethod]
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("idleActivities", DisplayName = "Custom namespace")]
    public async Task TestActivityInstances(string? instanceNamespace)
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        ActivityWithNoAction.TaskCompletionSource = new();
        var options = natsTestHarness.Options;

        // Act
        var connection = await Connection.CreateInstanceAsync(new(options)
        {
            Namespace = instanceNamespace
        });
        await connection.RegisterWorkflowAsync<WorkflowWithSingleAction>(new());
        await connection.RegisterWorkflowActivityAsync<ActivityWithNoAction>();

        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(options));
        await Task.Delay(TimeSpan.FromSeconds(1)); // small delay to ensure streams are created
        if (instanceNamespace is null)
            await observationConnection.AddDefaultNamespaceAsync();
        else
            await observationConnection.AddNamespaceAsync(instanceNamespace);

        //Assert
        var activityDetail = await observationConnection.GetActivityServiceabilityAsync<ActivityWithNoAction>(instanceNamespace);
        Assert.IsNotNull(activityDetail);
        ConfirmInstanceValues(activityDetail, 1, 0, 0);
        var activities = await observationConnection.GetActivityServiceabilityAsync(instanceNamespace);
        Assert.HasCount(1, activities);
        var activity = activities.First();
        Assert.AreEqual(NameHelper.GetWorkflowName<ActivityWithNoAction>().rawName, activity.Name);
        ConfirmInstanceValues(activity, 1, 0, 0);

        _ = await connection.StartWorkflowAsync<WorkflowWithSingleAction>();
        _ = await connection.StartWorkflowAsync<WorkflowWithSingleAction>();
        await Task.Delay(TimeSpan.FromSeconds(10));

        activityDetail = await observationConnection.GetActivityServiceabilityAsync<ActivityWithNoAction>(instanceNamespace);
        Assert.IsNotNull(activityDetail);
        ConfirmInstanceValues(activityDetail, 1, 2, 0);
        activities = await observationConnection.GetActivityServiceabilityAsync(instanceNamespace);
        Assert.HasCount(1, activities);
        activity = activities.First();
        Assert.AreEqual(NameHelper.GetWorkflowName<ActivityWithNoAction>().rawName, activity.Name);
        ConfirmInstanceValues(activity, 1, 2, 0);

        ActivityWithNoAction.TaskCompletionSource.TrySetResult();
        await Task.Delay(TimeSpan.FromSeconds(30));

        activityDetail = await observationConnection.GetActivityServiceabilityAsync<ActivityWithNoAction>(instanceNamespace);
        Assert.IsNotNull(activityDetail);
        ConfirmInstanceValues(activityDetail, 1, 0, 0);
        activities = await observationConnection.GetActivityServiceabilityAsync(instanceNamespace);
        Assert.HasCount(1, activities);
        activity = activities.First();
        Assert.AreEqual(NameHelper.GetWorkflowName<ActivityWithNoAction>().rawName, activity.Name);
        ConfirmInstanceValues(activity, 1, 0, 0);

        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }

    private static void ConfirmInstanceValues(ServiceabilityDetails instance, int idle, int active, ulong messagesWaiting)
    {
        Assert.AreEqual(idle, instance.IdleInstances);
        Assert.AreEqual(active, instance.ActiveInstances);
        Assert.AreEqual(messagesWaiting, instance.MessagesWaiting);
    }
}
