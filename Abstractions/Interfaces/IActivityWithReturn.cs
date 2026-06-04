namespace JetFlow.Interfaces;

/// <summary>
/// Represents an activity that can be executed as part of a workflow, with a specific output type. This interface extends the IActivity interface and allows for activities that produce a result upon execution. The generic type parameter TOutput specifies the type of output that the activity returns, enabling type safety and flexibility in workflow design. Activities implementing this interface can be used to perform operations that yield a result, which can then be utilized in subsequent steps of the workflow.
/// </summary>
/// <typeparam name="TOutput">The type of output that the activity returns.</typeparam>
public interface IActivityWithReturn<TOutput>
{
    /// <summary>
    /// Executes the workflow operation asynchronously using the specified state and cancellation token.
    /// </summary>
    /// <param name="state">The workflow state to use during execution. Provides context and data required by the workflow.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the output produced by the workflow.</returns>
    Task<TOutput> ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken);
}

/// <summary>
/// Represents an activity that can be executed as part of a workflow, with specific input and output types. This interface extends the IActivity interface and allows for activities that require input data and produce a result upon execution. The generic type parameters TOutput and TInput specify the types of output and input, respectively, enabling type safety and flexibility in workflow design. Activities implementing this interface can be used to perform operations that take input data, process it, and yield a result, which can then be utilized in subsequent steps of the workflow.
/// </summary>
/// <typeparam name="TOutput">The type of output that the activity returns.</typeparam>
/// <typeparam name="TInput">The type of input that the activity expects.</typeparam>
public interface IActivityWithReturn<TOutput, TInput>
{
    /// <summary>
    /// Executes the workflow operation asynchronously using the specified input and workflow state.
    /// </summary>
    /// <param name="input">The input data for the workflow execution. May be null if the workflow does not require input.</param>
    /// <param name="state">The current workflow state used to track execution progress and context. Cannot be null.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the output produced by the workflow.</returns>
    Task<TOutput> ExecuteAsync(TInput? input, IWorkflowState state, CancellationToken cancellationToken);
}
