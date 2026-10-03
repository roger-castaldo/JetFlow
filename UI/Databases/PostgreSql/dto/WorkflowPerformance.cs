namespace JetFlow.UI.dto;

internal record WorkflowPerformance(
    DateTime BucketStart,
    Guid WorkflowID,
    Guid NamespaceID,
    PerformanceAggregationType AggregationType,
    long Started,
    long Completed,
    long Failed,
    long Purged,
    TimespanSketch QueueLatencies
)
{
    public WorkflowPerformance Merge(WorkflowPerformance workflowPerformance)
    {
        QueueLatencies.Merge(workflowPerformance.QueueLatencies);
        return new(
            BucketStart,
            WorkflowID,
            NamespaceID,
            AggregationType,
            Started+workflowPerformance.Started,
            Completed+workflowPerformance.Completed,
            Failed+workflowPerformance.Failed,
            Purged+workflowPerformance.Purged,
            QueueLatencies
        );
    }
}

internal record WorkflowPerformanceRaw(
    Guid ID,
    DateTime Window,
    Guid WorkflowID,
    Guid NamespaceID,
    long Started,
    long Completed,
    long Failed,
    long Purged,
    IEnumerable<TimeSpan> QueueLatencies
);
