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
    /// <param name="workflowNamespace">The workflow namespace to filter by, or <c>null</c> to use the default namespace.</param>
    /// <returns>The number of active activities for the specified workflow.</returns>
    ValueTask<BigInteger> GetActiveActivityCountAsync(string? workflowNamespace);

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
    /// <summary>
    /// Creates a query that enumerates active workflows of the specified workflow type within the
    /// optionally provided namespace. The returned <see cref="IWorkflowQuery"/> can be consumed
    /// asynchronously to iterate matching <see cref="ActiveWorkflow"/> instances.
    /// </summary>
    /// <typeparam name="TWorkflow">The workflow type to filter by. Must implement <see cref="IWorkflow"/>.</typeparam>
    /// <param name="workflowNamespace">Optional namespace to scope the query. Use <c>null</c> to query the default namespace.</param>
    /// <param name="checkMetaData">Optional predicate to filter workflows based on their metadata. The predicate receives the metadata dictionary or <c>null</c>.</param>
    /// <returns>An <see cref="IWorkflowQuery"/> that yields matching <see cref="ActiveWorkflow"/> instances.</returns>
    ValueTask<IWorkflowQuery> QueryWorkflowAsync<TWorkflow>(string? workflowNamespace, Func<Dictionary<string, string[]>?, bool>? checkMetaData = null)
        where TWorkflow : class, IWorkflow;

    /// <summary>
    /// Creates a query that enumerates active workflows of the specified workflow type with a strongly-typed input.
    /// </summary>
    /// <typeparam name="TWorkflow">The workflow type to filter by. Must implement <see cref="IWorkflow{TInput}"/>.</typeparam>
    /// <typeparam name="TInput">The workflow input type used to apply an optional argument filter.</typeparam>
    /// <param name="workflowNamespace">Optional namespace to scope the query. Use <c>null</c> to query the default namespace.</param>
    /// <param name="checkMetaData">Optional predicate to filter workflows based on their metadata. The predicate receives the metadata dictionary or <c>null</c>.</param>
    /// <param name="checkArguement">Optional predicate to filter the workflow input. If provided, only workflows whose input satisfies the predicate are returned.</param>
    /// <returns>An <see cref="IWorkflowQuery"/> that yields matching <see cref="ActiveWorkflow"/> instances.</returns>
    ValueTask<IWorkflowQuery> QueryWorkflowAsync<TWorkflow, TInput>(string? workflowNamespace, Func<Dictionary<string, string[]>?, bool>? checkMetaData = null, Func<TInput, bool>? checkArguement = null)
        where TWorkflow : class, IWorkflow<TInput>;

    /// <summary>
    /// Loads all active workflows of the specified workflow type in the optional namespace into a materialized collection.
    /// </summary>
    /// <typeparam name="TWorkflow">The workflow type to filter by. Must implement <see cref="IWorkflow"/>.</typeparam>
    /// <param name="workflowNamespace">Optional namespace to scope the load. Use <c>null</c> to load from the default namespace.</param>
    /// <param name="checkMetaData">Optional predicate to filter workflows based on their metadata. The predicate receives the metadata dictionary or <c>null</c>.</param>
    /// <returns>A collection of <see cref="ActiveWorkflow"/> instances that match the filters.</returns>
    ValueTask<IEnumerable<ActiveWorkflow>> LoadWorkflowsAsync<TWorkflow>(string? workflowNamespace, Func<Dictionary<string, string[]>?, bool>? checkMetaData = null)
        where TWorkflow : class, IWorkflow;

    /// <summary>
    /// Loads all active workflows of the specified workflow type with a strongly-typed input into a materialized collection.
    /// </summary>
    /// <typeparam name="TWorkflow">The workflow type to filter by. Must implement <see cref="IWorkflow{TInput}"/>.</typeparam>
    /// <typeparam name="TInput">The workflow input type used to apply an optional argument filter.</typeparam>
    /// <param name="workflowNamespace">Optional namespace to scope the load. Use <c>null</c> to load from the default namespace.</param>
    /// <param name="checkMetaData">Optional predicate to filter workflows based on their metadata. The predicate receives the metadata dictionary or <c>null</c>.</param>
    /// <param name="checkArguement">Optional predicate to filter the workflow input. If provided, only workflows whose input satisfies the predicate are returned.</param>
    /// <returns>A collection of <see cref="ActiveWorkflow"/> instances that match the filters.</returns>
    ValueTask<IEnumerable<ActiveWorkflow>> LoadWorkflowsAsync<TWorkflow, TInput>(string? workflowNamespace, Func<Dictionary<string, string[]>?, bool>? checkMetaData = null, Func<TInput, bool>? checkArguement = null)
        where TWorkflow : class, IWorkflow<TInput>;

    /// <summary>
    /// Loads a single active workflow by its identifier, scoped to the given workflow type and optional namespace.
    /// </summary>
    /// <typeparam name="TWorkflow">The workflow type to filter by. Must implement <see cref="IWorkflow"/>.</typeparam>
    /// <param name="workflowNamespace">Optional namespace to scope the lookup. Use <c>null</c> to search the default namespace.</param>
    /// <param name="workflowId">The unique identifier of the workflow instance to load.</param>
    /// <returns>The matching <see cref="ActiveWorkflow"/> if found; otherwise <c>null</c>.</returns>
    ValueTask<ActiveWorkflow?> LoadWorkflowAsync<TWorkflow>(string? workflowNamespace, Guid workflowId)
        where TWorkflow : class, IWorkflow;

    /// <summary>
    /// Loads a single active workflow by its identifier for workflows that accept a strongly-typed input.
    /// </summary>
    /// <typeparam name="TWorkflow">The workflow type to filter by. Must implement <see cref="IWorkflow{TInput}"/>.</typeparam>
    /// <typeparam name="TInput">The workflow input type.</typeparam>
    /// <param name="workflowNamespace">Optional namespace to scope the lookup. Use <c>null</c> to search the default namespace.</param>
    /// <param name="workflowId">The unique identifier of the workflow instance to load.</param>
    /// <returns>The matching <see cref="ActiveWorkflow"/> if found; otherwise <c>null</c>.</returns>
    ValueTask<ActiveWorkflow?> LoadWorkflowAsync<TWorkflow, TInput>(string? workflowNamespace, Guid workflowId)
        where TWorkflow : class, IWorkflow<TInput>;
}
