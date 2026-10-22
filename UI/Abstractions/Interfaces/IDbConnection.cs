using JetFlow.Data;
using System.Text.Json;

namespace JetFlow.UI.Interfaces;

/// <summary>
/// Abstraction over a persistence connection used by the UI to store archives, performance
/// records and to query available namespaces and performance samples.
/// </summary>
public interface IDbConnection
{
    /// <summary>
    /// Initializes the underlying database connection and any required schema or resources.
    /// </summary>
    ValueTask InitAsync();

    /// <summary>
    /// Registers a new workflow namespace for tracking.
    /// </summary>
    /// <param name="namespaceName">The namespace to register, or null for the default namespace.</param>
    ValueTask RegisterNamespaceAsync(string? namespaceName);

    /// <summary>
    /// Unregisters a workflow namespace.
    /// </summary>
    /// <param name="namespaceName">The namespace to unregister, or null for the default namespace.</param>
    ValueTask UnregisterNamespaceAsync(string? namespaceName);

    /// <summary>
    /// Lists the currently registered namespaces.
    /// </summary>
    /// <returns>A collection of namespace names (null values represent the default namespace).</returns>
    ValueTask<IEnumerable<string?>> ListNamespacesAsync();

    ValueTask StoreWorkflowArgumentDefinitionAsync(string? namespaceName, string workflowName, JsonDocument definition);
    ValueTask<JsonDocument?> GetWorkflowArgumentDefinitionAsync(string? namespaceName, string workflowName);
    ValueTask StoreActivityArgumentDefinitionAsync(string? namespaceName, string activityName, JsonDocument definition);
    ValueTask<JsonDocument?> GetActivityArgumentDefinitionAsync(string? namespaceName, string activityName);

    /// <summary>
    /// Stores an archived workflow record for the specified namespace.
    /// </summary>
    /// <param name="archive">The archived workflow to store.</param>
    /// <param name="namespaceName">The namespace to associate the archive with.</param>
    ValueTask StoreArchiveAsync(ArchivedWorkflow archive, string? namespaceName);
    ValueTask<IEnumerable<ArchivedWorkflow>> FindArchivedWorkflowAsync(string? namespaceName, string workflowName, IEnumerable<IArgumentFilter>? argumentFilters,
        Dictionary<string, string?>? metaData, int offset, int pageSize);

    /// <summary>
    /// Stores an activity performance record for a namespace.
    /// </summary>
    /// <param name="namespaceName">The namespace the record belongs to.</param>
    /// <param name="record">The activity performance record to store.</param>
    ValueTask StoreActivityPerformanceRecordAsync(string? namespaceName, ActivityPerformanceRecord record);

    /// <summary>
    /// Stores a workflow performance record for a namespace.
    /// </summary>
    /// <param name="namespaceName">The namespace the record belongs to.</param>
    /// <param name="record">The workflow performance record to store.</param>
    ValueTask StoreWorkflowPerformanceRecordAsync(string? namespaceName, WorkflowPerformanceRecord record);

    /// <summary>
    /// Returns recent activity performance records for the specified namespace.
    /// </summary>
    /// <param name="namespaceName">The namespace to query.</param>
    /// <returns>A collection of activity performance records.</returns>
    ValueTask<IEnumerable<ActivityPerformanceRecord>> PeakActivityPerformanceDataAsync(string? namespaceName);

    /// <summary>
    /// Returns recent workflow performance records for the specified namespace.
    /// </summary>
    /// <param name="namespaceName">The namespace to query.</param>
    /// <returns>A collection of workflow performance records.</returns>
    ValueTask<IEnumerable<WorkflowPerformanceRecord>> PeakWorkflowPerformanceDataAsync(string? namespaceName);

    /// <summary>
    /// Extracts workflow performance samples for a specific workflow over the provided time range.
    /// </summary>
    /// <param name="namespaceName">The namespace to query.</param>
    /// <param name="workflowName">The workflow name to filter by.</param>
    /// <param name="startTime">Start time (UTC) for the query.</param>
    /// <param name="endTime">End time (UTC) for the query.</param>
    /// <param name="aggregationType">The aggregation window to use when sampling.</param>
    /// <returns>A collection of workflow performance samples.</returns>
    ValueTask<IEnumerable<WorkflowPerformanceSample>> ExtractWorkflowPerformanceDataAsync(string? namespaceName, string workflowName, DateTime startTime, DateTime endTime);

    /// <summary>
    /// Extracts workflow performance samples across all workflows in a namespace for the provided time range.
    /// </summary>
    /// <param name="namespaceName">The namespace to query.</param>
    /// <param name="startTime">Start time (UTC) for the query.</param>
    /// <param name="endTime">End time (UTC) for the query.</param>
    /// <param name="aggregationType">The aggregation window to use when sampling.</param>
    /// <returns>A collection of named workflow performance samples.</returns>
    ValueTask<IEnumerable<NamedWorkflowPerformanceSample>> ExtractWorkflowPerformanceDataAsync(string? namespaceName, DateTime startTime, DateTime endTime);

    /// <summary>
    /// Extracts activity performance samples for a specific activity over the provided time range.
    /// </summary>
    /// <param name="namespaceName">The namespace to query.</param>
    /// <param name="activityName">The activity name to filter by.</param>
    /// <param name="startTime">Start time (UTC) for the query.</param>
    /// <param name="endTime">End time (UTC) for the query.</param>
    /// <param name="aggregationType">The aggregation window to use when sampling.</param>
    /// <returns>A collection of activity performance samples.</returns>
    ValueTask<IEnumerable<ActivityPerformanceSample>> ExtractActivityPerformanceDataAsync(string? namespaceName, string activityName, DateTime startTime, DateTime endTime);

    /// <summary>
    /// Extracts activity performance samples across all activities in a namespace for the provided time range.
    /// </summary>
    /// <param name="namespaceName">The namespace to query.</param>
    /// <param name="startTime">Start time (UTC) for the query.</param>
    /// <param name="endTime">End time (UTC) for the query.</param>
    /// <param name="aggregationType">The aggregation window to use when sampling.</param>
    /// <returns>A collection of named activity performance samples.</returns>
    ValueTask<IEnumerable<NamedActivityPerformanceSample>> ExtractActivityPerformanceDataAsync(string? namespaceName, DateTime startTime, DateTime endTime);
}
