using JetFlow.Attributes;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace JetFlow.Testing.Observation;

[TestClass]
public class ScheduledWorkflowTests
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

    [WorkflowName("Scheduled Workflow With No Input")]
    private sealed class ScheduledWorkflowWithNoInput : IWorkflow
    {
        ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
            => ValueTask.CompletedTask;
    }
    [WorkflowName("Scheduled Workflow With Input")]
    private sealed class ScheduledWorkflowWithInput : IWorkflow<string>
    {
        ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string input)
            => ValueTask.CompletedTask;
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("workflowWithoutInput", DisplayName = "Custom namespace")]
    public async Task ListAllScheduledWorkflows(string? instanceNamespace)
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        var cronInput = "Cron Test";
        var delayedInput = "Delayed Test";
        var metaData = new Dictionary<string, string[]>()
        {
            {"key1",["value1"]}
        };
        var delay = TimeSpan.FromHours(1);
        var subjectMapper = new SubjectMapper(instanceNamespace);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        // Act
        var connection = await Connection.CreateInstanceAsync(new(natsConnection, jsContext)
        {
            Namespace = instanceNamespace
        });
        await connection.RegisterWorkflowAsync<ScheduledWorkflowWithNoInput>(new());
        await connection.RegisterWorkflowAsync<ScheduledWorkflowWithInput, string>(new());

        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection));
        await Task.Delay(TimeSpan.FromSeconds(1)); // small delay to ensure streams are created
        if (instanceNamespace is null)
            await observationConnection.AddDefaultNamespaceAsync();
        else
            await observationConnection.AddNamespaceAsync(instanceNamespace);
        
        // Assert
        Assert.IsNotNull(connection);

        var cronSchedule = new WorkflowScheduleBuilder()
            .DailyAt(9, 15)
            .Build();
        var expectedDelayMin = DateTimeOffset.UtcNow.AddMinutes(59);
        var expectedDelayMax = DateTimeOffset.UtcNow.AddMinutes(62);

        var cronNoInputId = await connection.ScheduleWorkflowAsync<ScheduledWorkflowWithNoInput>(cronSchedule, new() { MetaData = metaData});
        var cronWithInputId = await connection.ScheduleWorkflowAsync<ScheduledWorkflowWithInput, string>(new(cronInput), cronSchedule);
        var delayedNoInputId = await connection.DelayStartWorkflowAsync<ScheduledWorkflowWithNoInput>(delay);
        var delayedWithInputId = await connection.DelayStartWorkflowAsync<ScheduledWorkflowWithInput, string>(new(delayedInput), delay);

        // Verify
        var allWorkflows = await observationConnection.ListScheduledWorkflowsAsync(instanceNamespace);
        Assert.HasCount(4, allWorkflows);
        Assert.IsTrue(allWorkflows.All(wf =>
        (Equals(wf.ID, cronNoInputId.ToString()) && wf.Arguments==null && Equals(wf.Name, NameHelper.GetWorkflowName<ScheduledWorkflowWithNoInput>().rawName) && Equals(wf.CronString, cronSchedule.AsString) && wf.RunsAt==null)
        ||(Equals(wf.ID, cronWithInputId.ToString()) && Equals(wf.Arguments?.ToString(),cronInput) && Equals(wf.Name, NameHelper.GetWorkflowName<ScheduledWorkflowWithInput>().rawName) && Equals(wf.CronString, cronSchedule.AsString) && wf.RunsAt==null && wf.MetaData==null)
        ||(Equals(wf.ID, delayedNoInputId.ToString()) && wf.Arguments==null && Equals(wf.Name, NameHelper.GetWorkflowName<ScheduledWorkflowWithNoInput>().rawName) && wf.CronString==null && wf.RunsAt.HasValue && wf.RunsAt.Value>=expectedDelayMin && wf.RunsAt.Value<=expectedDelayMax && wf.MetaData==null)
        ||(Equals(wf.ID, delayedWithInputId.ToString()) && Equals(wf.Arguments?.ToString(),delayedInput) && Equals(wf.Name, NameHelper.GetWorkflowName<ScheduledWorkflowWithInput>().rawName) && wf.CronString==null && wf.RunsAt.HasValue && wf.RunsAt.Value>=expectedDelayMin && wf.RunsAt.Value<=expectedDelayMax && wf.MetaData==null)
        ));
        var metaDataWorkflow = allWorkflows.First(wf => Equals(wf.ID, cronNoInputId.ToString()));
        Assert.IsNotNull(metaDataWorkflow.MetaData);
        Assert.AreEqual(metaData.Count, metaDataWorkflow.MetaData.Count);
        Assert.IsTrue(metaData.All(pair=>metaDataWorkflow.MetaData.ContainsKey(pair.Key) &&
        pair.Value.SequenceEqual(metaDataWorkflow.MetaData[pair.Key])));
        var noInputWorkflows = await observationConnection.ListScheduledWorkflowsAsync<ScheduledWorkflowWithNoInput>(instanceNamespace);
        Assert.HasCount(2, noInputWorkflows);
        Assert.IsTrue(noInputWorkflows.All(wf =>
        (Equals(wf.ID, cronNoInputId.ToString()) && wf.Arguments==null && Equals(wf.Name, NameHelper.GetWorkflowName<ScheduledWorkflowWithNoInput>().rawName) && Equals(wf.CronString, cronSchedule.AsString) && wf.RunsAt==null)
        ||(Equals(wf.ID, delayedNoInputId.ToString()) && wf.Arguments==null && Equals(wf.Name, NameHelper.GetWorkflowName<ScheduledWorkflowWithNoInput>().rawName) && wf.CronString==null && wf.RunsAt.HasValue && wf.RunsAt.Value>=expectedDelayMin && wf.RunsAt.Value<=expectedDelayMax && wf.MetaData==null)
        ));
        metaDataWorkflow = noInputWorkflows.First(wf => Equals(wf.ID, cronNoInputId.ToString()));
        Assert.IsNotNull(metaDataWorkflow.MetaData);
        Assert.AreEqual(metaData.Count, metaDataWorkflow.MetaData.Count);
        Assert.IsTrue(metaData.All(pair => metaDataWorkflow.MetaData.ContainsKey(pair.Key) &&
        pair.Value.SequenceEqual(metaDataWorkflow.MetaData[pair.Key])));
        var inputWorkflows = await observationConnection.ListScheduledWorkflowsAsync<ScheduledWorkflowWithInput, string>(instanceNamespace);
        Assert.HasCount(2, inputWorkflows);
        Assert.IsTrue(inputWorkflows.All(wf =>
        (Equals(wf.ID, cronWithInputId.ToString()) && Equals(wf.Arguments, cronInput) && Equals(wf.Name, NameHelper.GetWorkflowName<ScheduledWorkflowWithInput>().rawName) && Equals(wf.CronString, cronSchedule.AsString) && wf.RunsAt==null && wf.MetaData==null)
        ||(Equals(wf.ID, delayedWithInputId.ToString()) && Equals(wf.Arguments, delayedInput) && Equals(wf.Name, NameHelper.GetWorkflowName<ScheduledWorkflowWithInput>().rawName) && wf.CronString==null && wf.RunsAt.HasValue && wf.RunsAt.Value>=expectedDelayMin && wf.RunsAt.Value<=expectedDelayMax && wf.MetaData==null)
        ));

        Assert.IsTrue(await connection.RemoveScheduledWorkflowAsync<ScheduledWorkflowWithNoInput>(cronNoInputId));
        Assert.IsTrue(await connection.RemoveScheduledWorkflowAsync<ScheduledWorkflowWithInput, string>(cronWithInputId));
        Assert.IsTrue(await connection.RemoveDelayedWorkflowAsync<ScheduledWorkflowWithNoInput>(delayedNoInputId));
        Assert.IsTrue(await connection.RemoveDelayedWorkflowAsync<ScheduledWorkflowWithInput, string>(delayedWithInputId));

        allWorkflows = await observationConnection.ListScheduledWorkflowsAsync(instanceNamespace);
        Assert.HasCount(0, allWorkflows);
        noInputWorkflows = await observationConnection.ListScheduledWorkflowsAsync<ScheduledWorkflowWithNoInput>(instanceNamespace);
        Assert.HasCount(0, noInputWorkflows);
        inputWorkflows = await observationConnection.ListScheduledWorkflowsAsync<ScheduledWorkflowWithInput, string>(instanceNamespace);
        Assert.HasCount(0, inputWorkflows);

        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }
}
