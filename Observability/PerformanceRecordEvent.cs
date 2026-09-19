using JetFlow.Data;

namespace JetFlow;

public record ActivityPerformanceRecordEvent(string? namespaceName, ActivityPerformanceRecord PerformanceRecord);
public record WorkflowPerformanceRecordEvent(string? namespaceName, WorkflowPerformanceRecord PerformanceRecord);
