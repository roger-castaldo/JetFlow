namespace JetFlow.UI;

/// <summary>
/// Aggregated workflow performance metrics for a single sampling window.
/// </summary>
/// <param name="Started">Number of workflow starts during the window.</param>
/// <param name="Completed">Number of workflow completions during the window.</param>
/// <param name="Failed">Number of workflow failures during the window.</param>
/// <param name="Purged">Number of workflows purged during the window.</param>
/// <param name="QueueLatency">Distribution metrics for queue latency observed.</param>
public record WorkflowPerformanceSample(long Started,long Completed,long Failed,long Purged, PMetrics QueueLatency);

/// <summary>
/// Named workflow performance sample including the workflow name and aggregated metrics.
/// </summary>
/// <param name="Name">The workflow name these metrics belong to.</param>
/// <param name="Started">Number of workflow starts during the window.</param>
/// <param name="Completed">Number of workflow completions during the window.</param>
/// <param name="Failed">Number of workflow failures during the window.</param>
/// <param name="Purged">Number of workflows purged during the window.</param>
/// <param name="QueueLatency">Distribution metrics for queue latency observed.</param>
public record NamedWorkflowPerformanceSample(string Name, long Started, long Completed, long Failed, long Purged, PMetrics QueueLatency)
    : WorkflowPerformanceSample(Started, Completed, Failed, Purged, QueueLatency);
