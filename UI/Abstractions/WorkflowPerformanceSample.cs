namespace JetFlow.UI;

/// <summary>
/// Aggregated workflow performance metrics for a single sampling window.
/// </summary>
/// <param name="SampleWindow">The time window these metrics were aggregated over.</param>
/// <param name="Started">Number of workflow starts during the window.</param>
/// <param name="Completed">Number of workflow completions during the window.</param>
/// <param name="Failed">Number of workflow failures during the window.</param>
/// <param name="Purged">Number of workflows purged during the window.</param>
/// <param name="QueueLatency">Distribution metrics for queue latency observed.</param>
public record WorkflowPerformanceSample(DateTimeOffset SampleWindow, long Started, long Completed, long Failed, long Purged, PMetrics QueueLatency);

/// <summary>
/// Named workflow performance sample including the workflow name and aggregated metrics.
/// </summary>
/// <param name="Name">The workflow name these metrics belong to.</param>
/// <param name="SampleWindow">The time window these metrics were aggregated over.</param>
/// <param name="Started">Number of workflow starts during the window.</param>
/// <param name="Completed">Number of workflow completions during the window.</param>
/// <param name="Failed">Number of workflow failures during the window.</param>
/// <param name="Purged">Number of workflows purged during the window.</param>
/// <param name="QueueLatency">Distribution metrics for queue latency observed.</param>
public record NamedWorkflowPerformanceSample(string Name, DateTimeOffset SampleWindow, long Started, long Completed, long Failed, long Purged, PMetrics QueueLatency)
    : WorkflowPerformanceSample(SampleWindow, Started, Completed, Failed, Purged, QueueLatency);
