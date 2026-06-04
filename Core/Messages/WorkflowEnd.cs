namespace JetFlow.Messages;

/// <summary>
/// Houses the information about the end of a workflow, including the time it ended and any error message if it failed.
/// </summary>
/// <param name="EndTime">The time the workflow ended.</param>
/// <param name="ErrorMessage">The error message if the workflow failed, otherwise null.</param>
public record WorkflowEnd(DateTime EndTime, string? ErrorMessage)
{
    /// <summary>
    /// Indicates whether the workflow ended successfully, which is true if there is no error message.
    /// </summary>
    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
