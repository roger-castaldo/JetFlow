namespace JetFlow.UI;

/// <summary>
/// Aggregated activity performance metrics for a single sampling window.
/// </summary>
/// <param name="Started">Number of activity starts during the window.</param>
/// <param name="Completed">Number of activity completions during the window.</param>
/// <param name="Failed">Number of activity failures during the window.</param>
/// <param name="TimedOut">Number of activity timeouts during the window.</param>
/// <param name="QueueLatency">Distribution metrics for queue latency observed.</param>
/// <param name="Duration">Distribution metrics for activity duration observed.</param>
public record ActivityPerformanceSample(long Started, long Completed, long Failed, long TimedOut, PMetrics QueueLatency, PMetrics Duration);

/// <summary>
/// Named activity performance sample including the activity name and aggregated metrics.
/// </summary>
/// <param name="Name">The activity name these metrics belong to.</param>
/// <param name="Started">Number of activity starts during the window.</param>
/// <param name="Completed">Number of activity completions during the window.</param>
/// <param name="Failed">Number of activity failures during the window.</param>
/// <param name="TimedOut">Number of activity timeouts during the window.</param>
/// <param name="QueueLatency">Distribution metrics for queue latency observed.</param>
/// <param name="Duration">Distribution metrics for activity duration observed.</param>
public record NamedActivityPerformanceSample(string Name, long Started, long Completed, long Failed, long TimedOut, PMetrics QueueLatency, PMetrics Duration)
    : ActivityPerformanceSample(Started, Completed, Failed, TimedOut, QueueLatency, Duration);

