namespace JetFlow.Interfaces;

/// <summary>
/// Houses the baseline capabilities that a Workflow State or Context should hold.
/// </summary>
public interface IContext
{
    /// <summary>
    /// Houses the workflow Instance ID
    /// </summary>
    string WorkflowID { get; }
    /// <summary>
    /// Houses the MetaData values that were supplied at the start of the workflow execution, if any.
    /// </summary>
    IReadOnlyDictionary<string, string[]>? MetaData { get; }
    /// <summary>
    /// Called to get the initial argument used to kick off the workflow if any.  If there is not one it will throw an error.
    /// </summary>
    /// <typeparam name="TInput">The type of the argument that was supplied</typeparam>
    /// <returns></returns>
    ValueTask<TInput> GetWorkflowArgumentAsync<TInput>();
}
