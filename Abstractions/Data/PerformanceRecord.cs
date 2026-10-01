namespace JetFlow.Data;

/// <summary>
/// Aggregated activity-level performance metrics for a sampling window.
/// </summary>
/// <param name="Window">The window timestamp for the sampled metrics.</param>
/// <param name="Name">The name of the activity these metrics relate to.</param>
/// <param name="Started">Number of activity starts during the window.</param>
/// <param name="Completed">Number of successful completions during the window.</param>
/// <param name="Failed">Number of failures during the window.</param>
/// <param name="TimedOut">Number of timeouts during the window.</param>
/// <param name="QueueLatencies">Observed queue latencies during the window.</param>
/// <param name="Durations">Observed activity durations during the window.</param>
public record struct ActivityPerformanceRecord(DateTimeOffset Window, string Name, long Started, long Completed, long Failed, long TimedOut, IEnumerable<TimeSpan> QueueLatencies, IEnumerable<TimeSpan> Durations);

/// <summary>
/// Aggregated workflow-level performance metrics for a sampling window.
/// </summary>
/// <param name="Window">The window timestamp for the sampled metrics.</param>
/// <param name="Name">The name of the workflow these metrics relate to.</param>
/// <param name="Started">Number of workflow starts during the window.</param>
/// <param name="Completed">Number of successful completions during the window.</param>
/// <param name="Failed">Number of failures during the window.</param>
/// <param name="Purged">Number of purged workflow records during the window.</param>
/// <param name="QueueLatencies">Observed queue latencies during the window.</param>
public record struct WorkflowPerformanceRecord(DateTimeOffset Window, string Name, long Started, long Completed, long Failed, long Purged, IEnumerable<TimeSpan> QueueLatencies);
