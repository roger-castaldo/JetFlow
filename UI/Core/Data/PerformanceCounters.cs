using System.Numerics;

namespace JetFlow.UI.Data;

/// <summary>
/// Snapshot of various performance counters exposed by the UI for display or monitoring.
/// </summary>
/// <param name="ActiveWorkflows">The number of active workflows currently running.</param>
/// <param name="SuspendedWorkflows">The number of workflows that are currently suspended.</param>
/// <param name="ActiveActivities">The number of active activities currently executing.</param>
public record PerformanceCounters(
    BigInteger ActiveWorkflows,
    BigInteger SuspendedWorkflows,
    BigInteger ActiveActivities
);
