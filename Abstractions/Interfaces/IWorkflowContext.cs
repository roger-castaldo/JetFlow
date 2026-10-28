namespace JetFlow.Interfaces;

/// <summary>
/// Used to execute activities and wait for a specified amount of time within a workflow. This interface is typically passed as a parameter to the workflow's main method, allowing the workflow to interact with the execution environment and manage its activities effectively.
/// </summary>
public interface IWorkflowContext : IContext
{
    /// <summary>
    /// Called to suspend the current workflow and it will resume once a resume call has been received for this workflow
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the activity execution. Optional.</param>
    /// <returns>A ValueTask that represents the asynchronous wait operation.</returns>
    ValueTask SuspendAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Asynchronously waits for the specified time interval before completing.
    /// </summary>
    /// <param name="delay">The amount of time to wait before the operation completes. Must be a non-negative time span.</param>
    /// /// <param name="cancellationToken">A token that can be used to cancel the activity execution. Optional.</param>
    /// <returns>A ValueTask that represents the asynchronous wait operation.</returns>
    ValueTask WaitAsync(TimeSpan delay, CancellationToken cancellationToken = default);
    /// <summary>
    /// Executes the specified activity asynchronously using the provided execution request and returns the result of
    /// the activity.
    /// </summary>
    /// <typeparam name="TActivity">The type of activity to execute. Must implement the IActivity interface.</typeparam>
    /// <param name="executionRequest">The request containing all information required to execute the activity. Cannot be null.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the activity execution. Optional.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains the outcome of the executed
    /// activity.</returns>
    ValueTask<StepResult> ExecuteActivityAsync<TActivity>(ActivityExecutionRequest executionRequest, CancellationToken cancellationToken = default)
        where TActivity : IActivity;
    /// <summary>
    /// Executes the specified activity asynchronously using the provided execution request.
    /// </summary>
    /// <typeparam name="TActivity">The type of activity to execute. Must implement IActivity&lt;TInput&gt;.</typeparam>
    /// <typeparam name="TInput">The type of input required by the activity.</typeparam>
    /// <param name="executionRequest">The request containing the input and context information for the activity execution. Cannot be null.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the activity execution. Optional.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains the outcome of the activity
    /// execution.</returns>
    ValueTask<StepResult> ExecuteActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest, CancellationToken cancellationToken = default)
        where TActivity : IActivity<TInput>;
    /// <summary>
    /// Executes the specified activity asynchronously using the provided execution request and returns the result of
    /// the activity.
    /// </summary>
    /// <typeparam name="TActivity">The type of activity to execute. Must implement IActivityWithReturn&lt;TOutput&gt;.</typeparam>
    /// <typeparam name="TOutput">The type of the value returned by the activity.</typeparam>
    /// <param name="executionRequest">The request containing all information required to execute the activity. Cannot be null.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the activity execution. Optional.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains the outcome of the executed
    /// activity.</returns>
    ValueTask<StepResult<TOutput>> ExecuteActivityAsync<TActivity, TOutput>(ActivityExecutionRequest executionRequest, CancellationToken cancellationToken = default)
        where TActivity : IActivityWithReturn<TOutput>;
    /// <summary>
    /// Executes the specified activity asynchronously using the provided execution request and returns the result of
    /// the activity
    /// </summary>
    /// <typeparam name="TActivity">The type of activity to execute. Must implement IActivityWithReturn&lt;TOutput, TInput&gt;.</typeparam>
    /// <typeparam name="TOutput">The type of the value returned by the activity.</typeparam>
    /// <typeparam name="TInput">The type of input required by the activity.</typeparam>
    /// <param name="executionRequest">The request containing the input and context information for the activity execution. Cannot be null.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the activity execution. Optional.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains the outcome of the activity
    /// execution.</returns>
    ValueTask<StepResult<TOutput>> ExecuteActivityAsync<TActivity, TOutput, TInput>(ActivityExecutionRequest<TInput> executionRequest, CancellationToken cancellationToken = default)
        where TActivity : IActivityWithReturn<TOutput, TInput>;
    /// <summary>
    /// Executes the specified activity asynchronously multiple times using the provided inputs and execution request, and returns the results of the activities.
    /// </summary>
    /// <typeparam name="TActivity">The type of activity to execute. Must implement IActivity&lt;TInput&gt;.</typeparam>
    /// <typeparam name="TInput">The type of input required by the activity.</typeparam>
    /// <param name="executionRequest">The request containing the inputs and context information for the activity execution. Cannot be null.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the activity execution. Optional.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains the outcomes of the executed activities.</returns>
    ValueTask<IEnumerable<StepResult>> ExecuteActivitiesAsync<TActivity, TInput>(ActivityExecutionRequest<IEnumerable<TInput>> executionRequest, CancellationToken cancellationToken = default)
        where TActivity : IActivity<TInput>;
    /// <summary>
    /// Executes the specified activity asynchronously multiple times using the provided inputs and execution request, and returns the results of the activities.
    /// </summary>
    /// <typeparam name="TActivity">The type of activity to execute. Must implement IActivityWithReturn&lt;TOutput, TInput&gt;.</typeparam>
    /// <typeparam name="TOutput">The type of the value returned by the activity.</typeparam>
    /// <typeparam name="TInput">The type of input required by the activity.</typeparam>
    /// <param name="executionRequest">The request containing the inputs and context information for the activity execution. Cannot be null.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the activity execution. Optional.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains the outcomes of the executed activities.</returns>
    ValueTask<IEnumerable<StepResult<TOutput>>> ExecuteActivitiesAsync<TActivity, TOutput, TInput>(ActivityExecutionRequest<IEnumerable<TInput>> executionRequest, CancellationToken cancellationToken = default)
        where TActivity : IActivityWithReturn<TOutput, TInput>;
    /// <summary>
    /// Starts a child instance of the specified workflow type asynchronously.
    /// </summary>
    /// <typeparam name="TWorkflow">The type of workflow to start. Must implement the IWorkflow interface.</typeparam>
    /// <param name="executionRequest">The request containing additional information to execute the workflow with, if desired.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the workflow start operation.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The task result contains result status of the workflow.</returns>
    ValueTask<StepResult> ExecuteChildWorkflowAsync<TWorkflow>(WorkflowExecutionRequest? executionRequest = null, CancellationToken cancellationToken = default)
        where TWorkflow : IWorkflow;
    /// <summary>
    /// Starts a child instance of the specified workflow asynchronously.
    /// </summary>
    /// <typeparam name="TWorkflow">The type of workflow to start. Must implement IWorkflow&lt;TInput&gt;.</typeparam>
    /// <typeparam name="TInput">The type of input data provided to the workflow.</typeparam>
    /// <param name="executionRequest">The request containing additional information to execute the workflow with, as well as the input.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The task result contains result status of the workflow.</returns>
    ValueTask<StepResult> ExecuteChildWorkflowAsync<TWorkflow, TInput>(WorkflowExecutionRequest<TInput> executionRequest, CancellationToken cancellationToken = default)
        where TWorkflow : IWorkflow<TInput>;
    /// <summary>
    /// Starts multiple child workflow instance of the specified workflow asynchronously and returns the results of the workflows.
    /// </summary>
    /// <typeparam name="TWorkflow">The type of workflow to start. Must implement IWorkflow&lt;TInput&gt;.</typeparam>
    /// <typeparam name="TInput">The type of input data provided to the workflow.</typeparam>
    /// <param name="executionRequest">The request containing additional information to execute the workflow with, as well as the input.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The task result contains the results of the workflows.</returns>
    ValueTask<IEnumerable<StepResult>> ExecuteChildWorkflowsAsync<TWorkflow, TInput>(WorkflowExecutionRequest<IEnumerable<TInput>> executionRequest, CancellationToken cancellationToken = default)
        where TWorkflow : IWorkflow<TInput>;
}
