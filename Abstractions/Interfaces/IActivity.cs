namespace JetFlow.Interfaces;

/// <summary>
/// Represents an activity that can be executed as part of a workflow. Activities are the building blocks of workflows and can perform various tasks, such as data processing, API calls, or any custom logic defined by the user. The IActivity interface defines a contract for executing an activity asynchronously, allowing for flexible and scalable workflow implementations.
/// </summary>
public interface IActivity
{
    /// <summary>
    /// Executes the workflow operation asynchronously using the specified workflow state.
    /// </summary>
    /// <param name="state">The workflow state to use during execution. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken);
}

/// <summary>
/// Represents an activity that can be executed as part of a workflow, with a specific input type. This interface extends the IActivity interface and allows for activities that require input data to be executed within a workflow. The generic type parameter TInput specifies the type of input that the activity expects, enabling type safety and flexibility in workflow design.
/// </summary>
/// <typeparam name="TInput">The type of input that the activity expects.</typeparam>
public interface IActivity<in TInput>
{
    /// <summary>
    /// Executes the workflow operation asynchronously using the specified input and workflow state.
    /// </summary>
    /// <param name="input">The input data for the activity. Can be null if the activity does not require input.</param>
    /// <param name="state">The workflow state to use during execution. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task ExecuteAsync(TInput? input, IWorkflowState state, CancellationToken cancellationToken);
}