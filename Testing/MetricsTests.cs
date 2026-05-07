using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace JetFlow.Testing;

[TestClass]
public class MetricsTests
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

    private class MetricsDelayedActivity : IActivity
    {
        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
    private class MetricsWorkflow : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            _ = await context.ExecuteActivityAsync<MetricsDelayedActivity>(new());
        }
    }

    [TestMethod]
    public async Task TestMetricsGathering()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var name = NameHelper.GetWorkflowName<MetricsWorkflow>();
        var captured = new ConcurrentBag<(string name, double? dmeasurement, long? lmeasurement, KeyValuePair<string, object?>[] tags)>();
        using var listener = new MeterListener();

        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == Connection.MetricsMeterName &&
            (instrument.Name == TraceConstants.ActivityDuration
             || instrument.Name == TraceConstants.ActivityQueueLatency
             || instrument.Name == TraceConstants.WorkflowsStarted
             || instrument.Name == TraceConstants.WorkflowsCompleted
             || instrument.Name == TraceConstants.WorkflowQueueLatency))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (tags.ToArray().Any(t => Equals(TraceConstants.WorkflowNameTag, t.Key) && Equals(name, t.Value)))
                captured.Add((instrument.Name, measurement, null, tags.ToArray()));
        });
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (tags.ToArray().Any(t => Equals(TraceConstants.WorkflowNameTag, t.Key) && Equals(name, t.Value)))
                captured.Add((instrument.Name, null, measurement, tags.ToArray()));
        });

        listener.Start();


        var activityName = NameHelper.GetActivityName<MetricsDelayedActivity>();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<MetricsWorkflow>();
        await connection.RegisterWorkflowActivityAsync<MetricsDelayedActivity>(new(), CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<MetricsWorkflow>(
            natsConnection,
            subjectMapper,
            () => connection.StartWorkflowAsync<MetricsWorkflow>(CancellationToken.None)
        );

        // Assert
        Assert.IsNotNull(result);
        await ((IAsyncDisposable)connection).DisposeAsync();
        //delay for metrics completion
        await Task.Delay(TimeSpan.FromSeconds(5));

        var data = captured.ToArray();
        Assert.HasCount(6, data);
        Assert.ContainsSingle(r =>
            Equals(TraceConstants.ActivityDuration, r.name)
            && r.tags.Any(t => Equals(TraceConstants.ActivityNameTag, t.Key) && Equals(activityName, t.Value))
            && r.dmeasurement!=null
            && r.dmeasurement>=1000
, data);
        Assert.ContainsSingle(r =>
            Equals(TraceConstants.ActivityQueueLatency, r.name)
            && r.tags.Any(t => Equals(TraceConstants.ActivityNameTag, t.Key) && Equals(activityName, t.Value))
            && r.dmeasurement!=null
, data);
        Assert.ContainsSingle(r =>
            Equals(TraceConstants.WorkflowQueueLatency, r.name)
            && r.tags.Any(t => Equals(TraceConstants.ActivityNameTag, t.Key) && Equals(activityName, t.Value))
            && r.tags.Any(t => Equals(TraceConstants.WorkflowNameTag, t.Key) && Equals(name, t.Value))
            && r.dmeasurement!=null
, data);
        Assert.AreEqual(2, data.Count(r =>
            Equals(TraceConstants.WorkflowQueueLatency, r.name)
            && r.tags.Any(t => Equals(TraceConstants.WorkflowNameTag, t.Key) && Equals(name, t.Value))
            && r.dmeasurement!=null
        ));
        Assert.ContainsSingle(r =>
            Equals(TraceConstants.WorkflowsStarted, r.name)
            && r.tags.Any(t => Equals(TraceConstants.WorkflowNameTag, t.Key) && Equals(name, t.Value))
            && r.lmeasurement==1
, data);
        Assert.ContainsSingle(r =>
            Equals(TraceConstants.WorkflowsCompleted, r.name)
            && r.tags.Any(t => Equals(TraceConstants.WorkflowNameTag, t.Key) && Equals(name, t.Value))
            && r.lmeasurement==1
, data);
    }
}
