using JetFlow.Data;
using System.Numerics;

namespace JetFlow.Interfaces;

/// <summary>
/// Represents a connection used to observe runtime metrics and performance
/// for workflows and activities. Implementations provide methods to query
/// counters, manage observed namespaces, and subscribe to periodic performance
/// records.
/// </summary>
public interface IObservationConnection
{
    /// <summary>
    /// Gets the current active workflow counts grouped by a performance counter key.
    /// </summary>
    /// <returns>A collection of <see cref="PerformanceCounter"/> instances representing active workflow counts.</returns>
    ValueTask<IEnumerable<PerformanceCounter>> GetActiveWorkflowCountAsync();

    /// <summary>
    /// Gets the current active workflow count for the specified namespace.
    /// </summary>
    /// <param name="workflowNamespace">The workflow namespace to filter by, or <c>null</c> to use the default namespace.</param>
    /// <returns>The number of active workflows in the requested namespace.</returns>
    ValueTask<BigInteger> GetActiveWorkflowCountAsync(string? workflowNamespace);

    /// <summary>
    /// Gets the current suspended workflow counts grouped by a performance counter key.
    /// </summary>
    /// <returns>A collection of <see cref="PerformanceCounter"/> instances representing suspended workflow counts.</returns>
    ValueTask<IEnumerable<PerformanceCounter>> GetSuspendedWorkflowCountAsync();

    /// <summary>
    /// Gets the current suspended workflow count for the specified namespace.
    /// </summary>
    /// <param name="workflowNamespace">The workflow namespace to filter by, or <c>null</c> to use the default namespace.</param>
    /// <returns>The number of suspended workflows in the requested namespace.</returns>
    ValueTask<BigInteger> GetSuspendedWorkflowCountAsync(string? workflowNamespace);

    /// <summary>
    /// Gets the current active activity counts grouped by a performance counter key.
    /// </summary>
    /// <returns>A collection of <see cref="PerformanceCounter"/> instances representing active activity counts.</returns>
    ValueTask<IEnumerable<PerformanceCounter>> GetActiveActivityCountAsync();

    /// <summary>
    /// Gets the current active activity count for the specified workflow name.
    /// </summary>
    /// <param name="workflowName">The workflow name to filter activity counts by, or <c>null</c> to use all workflows.</param>
    /// <returns>The number of active activities for the specified workflow.</returns>
    ValueTask<BigInteger> GetActiveActivityCountAsync(string? workflowName);

    /// <summary>
    /// Adds the default namespace to the set of namespaces being observed.
    /// </summary>
    ValueTask AddDefaultNamespaceAsync();

    /// <summary>
    /// Removes the default namespace from the set of namespaces being observed.
    /// </summary>
    ValueTask RemoveDefaultNamespaceAsync();

    /// <summary>
    /// Adds a namespace to the set of namespaces being observed.
    /// </summary>
    /// <param name="workflowNamespace">The namespace to add.</param>
    ValueTask AddNamespaceAsync(string workflowNamespace);

    /// <summary>
    /// Removes a namespace from the set of namespaces being observed.
    /// </summary>
    /// <param name="workflowNamespace">The namespace to remove.</param>
    ValueTask RemoveNamespaceAsync(string workflowNamespace);

    /// <summary>
    /// Adds multiple namespaces to the set of namespaces being observed.
    /// </summary>
    /// <param name="workflowNamespaces">The namespaces to add.</param>
    ValueTask AddNamespacesAsync(IEnumerable<string> workflowNamespaces);

    /// <summary>
    /// Removes multiple namespaces from the set of namespaces being observed.
    /// </summary>
    /// <param name="workflowNamespaces">The namespaces to remove.</param>
    ValueTask RemoveNamespacesAsync(IEnumerable<string> workflowNamespaces);

    /// <summary>
    /// Starts periodic performance monitoring. The connection will sample
    /// performance data at the specified interval and invoke the provided
    /// callbacks for workflow and activity performance records.
    /// </summary>
    /// <param name="sampleDurationMinutes">Sampling interval in minutes. A small positive value is expected.</param>
    /// <param name="workflowRecordReceived">Callback invoked when a <see cref="WorkflowPerformanceRecord"/> is available.</param>
    /// <param name="activityRecordReceived">Callback invoked when an <see cref="ActivityPerformanceRecord"/> is available.</param>
    ValueTask AddPerformanceMonitoringAsync(byte sampleDurationMinutes, Func<WorkflowPerformanceRecord, ValueTask> workflowRecordReceived, Func<ActivityPerformanceRecord, ValueTask> activityRecordReceived);
}
