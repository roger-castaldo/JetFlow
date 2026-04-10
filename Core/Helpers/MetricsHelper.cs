using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace JetFlow.Helpers;

internal static class MetricsHelper
{
    private const string TagBase = "jetflow";
    private const string WorkflowTagBase = "jetflow.workflow";

    private static readonly Meter Meter = new Meter(Connection.MetricsMeterName, "1.0.0");
    private static readonly Histogram<double> ActivityDuration = Meter.CreateHistogram<double>($"{TraceConstants.BaseActivityTag}.duration", unit: "ms", description: "Activity execution duration");
    private static readonly Histogram<double> ActivityQueueLatency = Meter.CreateHistogram<double>($"{TraceConstants.BaseActivityTag}.queue.latency", unit: "ms", description: "Time spend waiting before execution");
    private static readonly Histogram<double> WorkflowQueueLatency = Meter.CreateHistogram<double>($"{TraceConstants.BaseWorkflowTag}.queue.latency", unit: "ms", description: "Time spend waiting before execution");
    private static readonly Counter<long> WorkflowStarted = Meter.CreateCounter<long>($"{TraceConstants.BaseWorkflowTag}.started");
    private static readonly Counter<long> WorkflowCompleted = Meter.CreateCounter<long>($"{TraceConstants.BaseWorkflowTag}.completed");

    public static void CompleteActivity(EventMessage message, long stopwatchStart)
        => ActivityDuration.Record(
                Stopwatch.GetElapsedTime(stopwatchStart).TotalMilliseconds,
                new(TraceConstants.WorkflowNameTag, message.WorkflowName),
                new(TraceConstants.ActivityNameTag, message.ActivityName)
           );
    public static long StartActivity(EventMessage message)
    {
        if (message.Message.Metadata!=null)
            ActivityQueueLatency.Record(
                DateTime.UtcNow.Subtract(message.Message.Metadata.Value.Timestamp.UtcDateTime).TotalMilliseconds,
                new(TraceConstants.WorkflowNameTag, message.WorkflowName),
                new(TraceConstants.ActivityNameTag, message.ActivityName)
            );
        return Stopwatch.GetTimestamp();
    }

    public static void ProcessWorkflowMessage(EventMessage message)
    {
        if (message.Message.Metadata!=null)
            WorkflowQueueLatency.Record(
                DateTime.UtcNow.Subtract(message.Message.Metadata.Value.Timestamp.UtcDateTime).TotalMilliseconds,
                new(TraceConstants.WorkflowNameTag, message.WorkflowName),
                new(TraceConstants.ActivityNameTag, message.ActivityName)
            );
        if (Equals(WorkflowEventTypes.Start, message.WorkflowEventType))
            WorkflowStarted.Add(1, new KeyValuePair<string, object?>(TraceConstants.WorkflowNameTag, message.WorkflowName));
    }

    public static void EndWorkflow(string name)
        => WorkflowCompleted.Add(1, new KeyValuePair<string, object?>(TraceConstants.WorkflowNameTag, name));
}
