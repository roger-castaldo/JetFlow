namespace JetFlow.Interfaces;

/// <summary>
/// Used to execute activities and wait for a specified amount of time within a workflow. This interface is typically passed as a parameter to the workflow's main method, allowing the workflow to interact with the execution environment and manage its activities effectively.
/// </summary>
public interface IWorkflowContext
{
    /// <summary>
    /// Asynchronously waits for the specified time interval before completing.
    /// </summary>
    /// <param name="delay">The amount of time to wait before the operation completes. Must be a non-negative time span.</param>
    /// <returns>A ValueTask that represents the asynchronous wait operation.</returns>
    ValueTask WaitAsync(TimeSpan delay);
    /// <summary>
    /// Executes the specified activity asynchronously using the provided execution request and returns the result of
    /// the activity.
    /// </summary>
    /// <typeparam name="TActivity">The type of activity to execute. Must implement the IActivity interface.</typeparam>
    /// <param name="executionRequest">The request containing all information required to execute the activity. Cannot be null.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains the outcome of the executed
    /// activity.</returns>
    ValueTask<ActivityResult> ExecuteActivityAsync<TActivity>(ActivityExecutionRequest executionRequest)
        where TActivity : IActivity;
    /// <summary>
    /// Executes the specified activity asynchronously using the provided execution request.
    /// </summary>
    /// <typeparam name="TActivity">The type of activity to execute. Must implement IActivity&lt;TInput&gt;.</typeparam>
    /// <typeparam name="TInput">The type of input required by the activity.</typeparam>
    /// <param name="executionRequest">The request containing the input and context information for the activity execution. Cannot be null.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains the outcome of the activity
    /// execution.</returns>
    ValueTask<ActivityResult> ExecuteActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest)
        where TActivity : IActivity<TInput>;
    /// <summary>
    /// Executes the specified activity asynchronously using the provided execution request and returns the result of
    /// the activity.
    /// </summary>
    /// <typeparam name="TActivity">The type of activity to execute. Must implement IActivityWithReturn&lt;TOutput&gt;.</typeparam>
    /// <typeparam name="TOutput">The type of the value returned by the activity.</typeparam>
    /// <param name="executionRequest">The request containing all information required to execute the activity. Cannot be null.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains the outcome of the executed
    /// activity.</returns>
    ValueTask<ActivityResult<TOutput>> ExecuteActivityAsync<TActivity, TOutput>(ActivityExecutionRequest executionRequest)
        where TActivity : IActivityWithReturn<TOutput>;
    /// <summary>
    /// Executes the specified activity asynchronously using the provided execution request and returns the result of
    /// the activity
    /// </summary>
    /// <typeparam name="TActivity">The type of activity to execute. Must implement IActivityWithReturn&lt;TOutput, TInput&gt;.</typeparam>
    /// <typeparam name="TOutput">The type of the value returned by the activity.</typeparam>
    /// <typeparam name="TInput">The type of input required by the activity.</typeparam>
    /// <param name="executionRequest">The request containing the input and context information for the activity execution. Cannot be null.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains the outcome of the activity
    /// execution.</returns>
    ValueTask<ActivityResult<TOutput>> ExecuteActivityAsync<TActivity, TOutput, TInput>(ActivityExecutionRequest<TInput> executionRequest)
        where TActivity : IActivityWithReturn<TOutput, TInput>;
}
