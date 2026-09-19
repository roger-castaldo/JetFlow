using JetFlow.Data;

namespace JetFlow.UI.Interfaces;

public interface IDbConnection
{
    ValueTask InitAsync();
    ValueTask RegisterNamespaceAsync(string? namespaceName);
    ValueTask UnregisterNamespaceAsync(string? namespaceName);
    ValueTask<IEnumerable<string?>> ListNamespacesAsync();
    ValueTask StoreArchiveAsync(ArchivedWorkflow archive, string? namespaceName);
    ValueTask StoreActivityPerformanceRecordAsync(string? namespaceName, ActivityPerformanceRecord record);
    ValueTask StoreWorkflowPerformanceRecordAsync(string? namespaceName, WorkflowPerformanceRecord record);
    ValueTask<IEnumerable<ActivityPerformanceRecord>> PeakActivityPerformanceDataAsync(string? namespaceName);
    ValueTask<IEnumerable<WorkflowPerformanceRecord>> PeakWorkflowPerformanceDataAsync(string? namespaceName);
}
