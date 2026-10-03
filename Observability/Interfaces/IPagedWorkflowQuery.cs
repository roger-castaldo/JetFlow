namespace JetFlow.Interfaces;

/// <summary>
/// Represents a materialized, paged collection of active workflows with a marker
/// indicating the last observed offset in the underlying event stream.
/// </summary>
public interface IPagedWorkflowQuery : IEnumerable<ActiveWorkflow>
{
    /// <summary>
    /// The last stream offset that was included in this page of results.
    /// </summary>
    ulong LastOffset { get; }
}
