namespace JetFlow.Data;

public record struct ActivityPerformanceRecord(DateTimeOffset Window, string Name, long Started, long Completed, long Failed, long TimedOut, IEnumerable<TimeSpan> QueueLatencies, IEnumerable<TimeSpan> Durations);
public record struct WorkflowPerformanceRecord(DateTimeOffset Window, string Name, long Started, long Completed, long Failed, long Purged, IEnumerable<TimeSpan> QueueLatencies);