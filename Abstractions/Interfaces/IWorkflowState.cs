namespace JetFlow.Interfaces;

/// <summary>
/// Houses the state of a workflow, such as the current activity attempt and the results of completed activities. This interface allows workflow activities to access information about the workflow's execution state, enabling them to make informed decisions based on previous activity outcomes.
/// </summary>
public interface IWorkflowState
{
    /// <summary>
    /// The current attempt number for the activity being executed. This value is incremented each time an activity is retried, allowing activities to determine how many times they have been attempted and to implement retry logic accordingly.
    /// </summary>
    ushort ActivityAttempt { get; }
    /// <summary>
    /// Called by workflow activities to retrieve the result value of a previously completed activity. The method is generic, allowing the caller to specify the expected type of the result value. The method returns a nullable value, indicating that the result may not be present if the activity has not been completed or if it did not produce a result. This allows activities to safely access previous results without risking exceptions due to missing data.
    /// </summary>
    /// <typeparam name="TWorkflowActivity">The type of the workflow activity whose result is being retrieved.</typeparam>
    /// <typeparam name="TValue">The type of the value returned by the activity.</typeparam>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains the value of the completed activity, or null if the activity has not been completed or did not produce a result.</returns>
    ValueTask<TValue?> GetActivityResultValueAsync<TWorkflowActivity, TValue>();
    /// <summary>
    /// Called by workflow activities to retrieve the result value of a previously completed activity by specifying the activity's name. This method is also generic, allowing the caller to specify the expected type of the result value. Similar to the previous method, it returns a nullable value, indicating that the result may not be present if the activity has not been completed or if it did not produce a result. This provides flexibility for activities to access results based on activity names rather than types, which can be useful in scenarios where multiple activities of the same type are executed within a workflow.
    /// </summary>
    /// <typeparam name="TValue">The type of the value returned by the activity.</typeparam>
    /// <param name="activityName">The name of the activity whose result is being retrieved.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains the value of the completed activity, or null if the activity has not been completed or did not produce a result.</returns>
    ValueTask<TValue?> GetActivityResultValueAsync<TValue>(string activityName);
}
