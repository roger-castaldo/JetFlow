using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace JetFlow.Helpers;

internal class MetricsHelper(InternalNatsConnection natsConnection, SubjectMapper subjectMapper) : IAsyncDisposable
{
    private static readonly Meter Meter = new Meter(Connection.MetricsMeterName, "1.0.0");
    private static readonly Histogram<double> ActivityDuration = Meter.CreateHistogram<double>(TraceConstants.ActivityDuration, unit: "ms", description: "Activity execution duration");
    private static readonly Histogram<double> ActivityQueueLatency = Meter.CreateHistogram<double>(TraceConstants.ActivityQueueLatency, unit: "ms", description: "Time spend waiting before execution");
    private static readonly Histogram<double> WorkflowQueueLatency = Meter.CreateHistogram<double>(TraceConstants.WorkflowQueueLatency, unit: "ms", description: "Time spend waiting before execution");
    private static readonly Counter<long> WorkflowStarted = Meter.CreateCounter<long>(TraceConstants.WorkflowsStarted);
    private static readonly Counter<long> WorkflowCompleted = Meter.CreateCounter<long>(TraceConstants.WorkflowsCompleted);
    private readonly ObservationMetricsCollector observationMetricsCollector = new(natsConnection, subjectMapper);

    public async ValueTask CompleteActivityAsync(EventMessage message, long stopwatchStart, CancellationToken cancellationToken = default)
    {
        var duration = Stopwatch.GetElapsedTime(stopwatchStart);
        ActivityDuration.Record(
                duration.TotalMilliseconds,
                new(TraceConstants.WorkflowNameTag, message.WorkflowName),
                new(TraceConstants.ActivityNameTag, message.ActivityName)
        );
        observationMetricsCollector.CompleteActivity(message.ActivityName, duration);
        await natsConnection.DecrementCounterAsync(subjectMapper.ActiveActivitiesCounter, cancellationToken);
    }
    public async ValueTask TimeoutActivityAsync(EventMessage message, CancellationToken cancellationToken = default)
    {
        observationMetricsCollector.TimeoutActivity(message.ActivityName);
        if (!Equals(message.ActivityEventType, ActivityEventTypes.Timeout))
            await natsConnection.DecrementCounterAsync(subjectMapper.ActiveActivitiesCounter, cancellationToken);
    }
    public async ValueTask FailActivityAsync(EventMessage message, CancellationToken cancellationToken = default)
    {
        observationMetricsCollector.FailActivity(message.ActivityName);
        await natsConnection.DecrementCounterAsync(subjectMapper.ActiveActivitiesCounter, cancellationToken);
    }
    public async ValueTask<long> StartActivityAsync(EventMessage message, CancellationToken cancellationToken = default)
    {
        if (message.Metadata!=null)
        {
            ActivityQueueLatency.Record(
                message.RecievedTimestamp.Subtract(message.Metadata.Value.Timestamp).TotalMilliseconds,
                new(TraceConstants.WorkflowNameTag, message.WorkflowName),
                new(TraceConstants.ActivityNameTag, message.ActivityName)
            );
            observationMetricsCollector.StartActivity(message.ActivityName, message.RecievedTimestamp.Subtract(message.Metadata.Value.Timestamp));
        }
        await natsConnection.IncrementCounterAsync(subjectMapper.ActiveActivitiesCounter, cancellationToken);
        return Stopwatch.GetTimestamp();
    }
    public async ValueTask ProcessWorkflowMessageAsync(EventMessage message, CancellationToken cancellationToken = default)
    {
        if (message.Metadata!=null)
            WorkflowQueueLatency.Record(
                message.RecievedTimestamp.Subtract(message.Metadata.Value.Timestamp).TotalMilliseconds,
                new(TraceConstants.WorkflowNameTag, message.WorkflowName),
                new(TraceConstants.ActivityNameTag, message.ActivityName)
            );
        if (Equals(message.WorkflowEventType, WorkflowEventTypes.Start))
        {
            WorkflowStarted.Add(1, new KeyValuePair<string, object?>(TraceConstants.WorkflowNameTag, message.WorkflowName));
            observationMetricsCollector.StartWorkflow(message.WorkflowName, message.RecievedTimestamp.Subtract(message.Metadata?.Timestamp ?? message.RecievedTimestamp));
            await natsConnection.IncrementCounterAsync(subjectMapper.ActiveWorkflowsCounter, cancellationToken);
        }
        else
        {
            observationMetricsCollector.RecordWorkflowLatency(message.WorkflowName, message.RecievedTimestamp.Subtract(message.Metadata?.Timestamp ?? message.RecievedTimestamp));
            if (Equals(message.WorkflowEventType, WorkflowEventTypes.DelayEnd))
                await natsConnection.DecrementCounterAsync(subjectMapper.SuspendedWorkflowsCounter, cancellationToken);
        }
        
    }
    public ValueTask SuspendWorkflowAsync(CancellationToken cancellationToken = default)
        => natsConnection.IncrementCounterAsync(subjectMapper.SuspendedWorkflowsCounter, cancellationToken);

    public async Task EndWorkflowAsync(string workflowName, bool success, CancellationToken cancellationToken)
    {
        WorkflowCompleted.Add(1, new KeyValuePair<string, object?>(TraceConstants.WorkflowNameTag, workflowName));
        if (success)
            observationMetricsCollector.CompleteWorkflow(workflowName);
        else
            observationMetricsCollector.FailWorkflow(workflowName);
        await natsConnection.DecrementCounterAsync(subjectMapper.ActiveWorkflowsCounter, cancellationToken);
    }

    public void PurgeWorkflow(string workflowName)
        => observationMetricsCollector.PurgeWorkflow(workflowName);

    ValueTask IAsyncDisposable.DisposeAsync()
        => ((IAsyncDisposable)observationMetricsCollector).DisposeAsync();
}
