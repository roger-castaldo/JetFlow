using System.Numerics;

namespace JetFlow.UI.Data;

public record PerformanceCounters(
    BigInteger ActiveWorkflows,
    BigInteger SuspendedWorkflows,
    BigInteger ActiveActivities
);
