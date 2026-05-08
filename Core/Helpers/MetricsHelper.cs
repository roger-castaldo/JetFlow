using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace JetFlow.Helpers;

internal static class MetricsHelper
{
    private static readonly Meter Meter = new Meter(Connection.MetricsMeterName, "1.0.0");
    private static readonly Histogram<double> ActivityDuration = Meter.CreateHistogram<double>(TraceConstants.ActivityDuration, unit: "ms", description: "Activity execution duration");
    private static readonly Histogram<double> ActivityQueueLatency = Meter.CreateHistogram<double>(TraceConstants.ActivityQueueLatency, unit: "ms", description: "Time spend waiting before execution");
    private static readonly Histogram<double> WorkflowQueueLatency = Meter.CreateHistogram<double>(TraceConstants.WorkflowQueueLatency, unit: "ms", description: "Time spend waiting before execution");
    private static readonly Counter<long> WorkflowStarted = Meter.CreateCounter<long>(TraceConstants.WorkflowsStarted);
    private static readonly Counter<long> WorkflowCompleted = Meter.CreateCounter<long>(TraceConstants.WorkflowsCompleted);

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
                message.RecievedTimestamp.Subtract(message.Message.Metadata.Value.Timestamp).TotalMilliseconds,
                new(TraceConstants.WorkflowNameTag, message.WorkflowName),
                new(TraceConstants.ActivityNameTag, message.ActivityName)
            );
        return Stopwatch.GetTimestamp();
    }

    public static void ProcessWorkflowMessage(EventMessage message)
    {
        if (message.Message.Metadata!=null)
            WorkflowQueueLatency.Record(
                message.RecievedTimestamp.Subtract(message.Message.Metadata.Value.Timestamp).TotalMilliseconds,
                new(TraceConstants.WorkflowNameTag, message.WorkflowName),
                new(TraceConstants.ActivityNameTag, message.ActivityName)
            );
        if (Equals(WorkflowEventTypes.Start, message.WorkflowEventType))
            WorkflowStarted.Add(1, new KeyValuePair<string, object?>(TraceConstants.WorkflowNameTag, message.WorkflowName));
    }

    public static void EndWorkflow(string name)
        => WorkflowCompleted.Add(1, new KeyValuePair<string, object?>(TraceConstants.WorkflowNameTag, name));
}
