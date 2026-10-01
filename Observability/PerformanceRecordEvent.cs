using JetFlow.Data;

namespace JetFlow;

/// <summary>
/// Event containing aggregated activity performance metrics for a sampling window.
/// </summary>
/// <param name="namespaceName">The namespace the record belongs to, or null for the default namespace.</param>
/// <param name="PerformanceRecord">The aggregated activity performance record.</param>
public record ActivityPerformanceRecordEvent(string? namespaceName, ActivityPerformanceRecord PerformanceRecord);

/// <summary>
/// Event containing aggregated workflow performance metrics for a sampling window.
/// </summary>
/// <param name="namespaceName">The namespace the record belongs to, or null for the default namespace.</param>
/// <param name="PerformanceRecord">The aggregated workflow performance record.</param>
public record WorkflowPerformanceRecordEvent(string? namespaceName, WorkflowPerformanceRecord PerformanceRecord);
