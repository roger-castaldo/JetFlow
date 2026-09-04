namespace JetFlow.Interfaces;

/// <summary>
/// Represents an asynchronous, disposable query over active workflows. The query
/// can be enumerated using await foreach and can be materialized to a list.
/// </summary>
public interface IWorkflowQuery : IAsyncEnumerable<ActiveWorkflow>, IAsyncDisposable
{
    /// <summary>
    /// Materializes the query results to a collection of <see cref="ActiveWorkflow"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token used to cancel the operation.</param>
    /// <returns>A collection containing the matching <see cref="ActiveWorkflow"/> instances.</returns>
    ValueTask<IEnumerable<ActiveWorkflow>> ToListAsync(CancellationToken cancellationToken = default);
}
