using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace JetFlow.Testing.Observation;

[TestClass]
public class ArchiveListenerTests
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

    private sealed class EmptyActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
    private sealed class EmptyActivityWorkflow : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await context.ExecuteActivityAsync<EmptyActivity>(new());
        }
    }

    [TestMethod]
    [DataRow(null, false, DisplayName = "Default namespace no delete")]
    [DataRow(null, true, DisplayName = "Default namespace and delete")]
    [DataRow("archiveObservations", false, DisplayName = "Custom namespace no delete")]
    [DataRow("archiveObservations", true, DisplayName = "Custom namespace and delete")]
    public async Task TestArchiveObservations(string? instanceNamespace, bool delete)
    {
        Assert.IsNotNull(natsTestHarness);
        // Arrange
        var subjectMapper = new SubjectMapper(instanceNamespace);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var events = new List<ArchivedWorkflowEvent>();
        var taskSource = new TaskCompletionSource();
        var objContext = jsContext.CreateObjectStoreContext();
        // Act
        var connection = await Connection.CreateInstanceAsync(new(natsConnection, jsContext)
        {
            Namespace = instanceNamespace
        });
        await connection.RegisterWorkflowAsync<EmptyActivityWorkflow>(new() { CompletionAction = Configs.WorkflowCompletionActions.Archive});
        await connection.RegisterWorkflowActivityAsync<EmptyActivity>();

        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection));
        await Task.Delay(TimeSpan.FromSeconds(1)); // small delay to ensure streams are created
        if (instanceNamespace is null)
            await observationConnection.AddDefaultNamespaceAsync();
        else
            await observationConnection.AddNamespaceAsync(instanceNamespace);
        await observationConnection.AddArchivingListenerAsync((archiveEvent) =>
        {
            events.Add(archiveEvent);
            if (events.Count==2)
                taskSource.TrySetResult();
            return ValueTask.FromResult(delete);
        });

        // Assert
        var run1 = await connection.StartWorkflowAsync<EmptyActivityWorkflow>();
        var run2 = await connection.StartWorkflowAsync<EmptyActivityWorkflow>();

        await taskSource.Task;

        // Verify
        Assert.HasCount(2, events);
        Assert.IsTrue(events.All(archive => Equals(instanceNamespace, archive.WorkflowNamespace)
        && archive.Archive.Steps.Length==1
        && Equals(archive.Archive.Steps[0].Name, NameHelper.GetActivityName<EmptyActivity>().rawName)));
        Assert.IsTrue(events.Any(archive => Equals(archive.Archive.ID, run1)));
        Assert.IsTrue(events.Any(archive => Equals(archive.Archive.ID, run2)));

        var archiveStore = await objContext.GetObjectStoreAsync(subjectMapper.WorkflowArchiveObjectstore);
        var objEntry = await archiveStore.GetInfoAsync($"{NameHelper.GetWorkflowName<EmptyActivityWorkflow>().cleanedName}/{run1}", showDeleted: true);
        Assert.IsNotNull(objEntry);
        Assert.AreEqual(delete, objEntry.Deleted);
        objEntry = await archiveStore.GetInfoAsync($"{NameHelper.GetWorkflowName<EmptyActivityWorkflow>().cleanedName}/{run2}", showDeleted: true);
        Assert.IsNotNull(objEntry);
        Assert.AreEqual(delete, objEntry.Deleted);

        //cleanup
        await ((IAsyncDisposable)connection).DisposeAsync();
        await ((IAsyncDisposable)observationConnection).DisposeAsync();
    }
}
