namespace JetFlow.UI.dto;

internal record ActivityPerformance(
    DateTime BucketStart,
    Guid ActivityID,
    Guid NamespaceID,
    PerformanceAggregationType AggregationType,
    long Started,
    long Completed,
    long Failed,
    long TimedOut,
    TimespanSketch QueueLatencies,
    TimespanSketch Duration
)
{
    public ActivityPerformance Merge(ActivityPerformance activityPerformance)
    {
        QueueLatencies.Merge(activityPerformance.QueueLatencies);
        Duration.Merge(activityPerformance.Duration);
        return new(
            BucketStart,
            ActivityID,
            NamespaceID,
            AggregationType,
            Started+activityPerformance.Started,
            Completed+activityPerformance.Completed,
            Failed+activityPerformance.Failed,
            TimedOut+activityPerformance.TimedOut,
            QueueLatencies,
            Duration
        );
    }
}

internal record ActivityPerformanceRaw(
    Guid ID,
    DateTime Window,
    Guid ActivityID,
    Guid NamespaceID,
    long Started,
    long Completed,
    long Failed,
    long TimedOut,
    IEnumerable<TimeSpan> QueueLatencies,
    IEnumerable<TimeSpan> Duration
);
