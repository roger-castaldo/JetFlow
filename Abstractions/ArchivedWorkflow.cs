using JetFlow.Configs;
using System.Text.Json.Serialization;

namespace JetFlow;

/// <summary>
/// Specifies the type of workflow step, which can be either an action (Activity) or a delay (a pause in the workflow).
/// </summary>
public enum WorkflowStepTypes
{
    /// <summary>
    /// Represents an action step, typically an activity or task to be executed.
    /// </summary>
    Action,
    /// <summary>
    /// Gets or sets the delay interval before the operation is executed.
    /// </summary>
    Delay
}

/// <summary>
/// Specifies the type of retry that occurred during a workflow step, which can be either a timeout or an error.
/// </summary>
public enum RetryTypes
{
    /// <summary>
    /// Specifies that the retry was triggered due to a timeout, indicating that the operation took longer than expected to complete.
    /// </summary>
    Timeout,
    /// <summary>
    /// Specifies that the retry was triggered due to an error, indicating that an exception or failure occurred during the execution of the operation.
    /// </summary>
    Error
}

/// <summary>
/// Represents a retry action for a workflow step, including the retry type and the time the retry occurred.
/// </summary>
/// <param name="RetryType">The type of retry performed for the workflow step.</param>
/// <param name="Timestamp">The date and time, in UTC, when the retry action was executed.</param>
public record struct WorkflowStepRetry(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    RetryTypes RetryType,
    DateTimeOffset Timestamp
);

/// <summary>
/// Represents a single step within a workflow, including its type, timing, status, and associated data.
/// </summary>
/// <param name="Type">The type of the workflow step. Specifies the operation or action performed at this step.</param>
/// <param name="Index">The zero-based index of the step within the workflow sequence, or null if not specified.</param>
/// <param name="Name">The name of the workflow step, or null if unnamed.</param>
/// <param name="StartTime">The date and time when the step started.</param>
/// <param name="EndTime">The date and time when the step ended.</param>
/// <param name="Retries">An array of retry attempts for this step, or null if no retries occurred.</param>
/// <param name="Status">The result status of the step, or null if the status is not set.</param>
/// <param name="ErrorMessage">The error message associated with the step if it failed, or null if no error occurred.</param>
/// <param name="Result">The result produced by the step, or null if there is no result.</param>
public record struct WorkflowStep(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    WorkflowStepTypes Type,
    uint? Index,
    string? Name,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    WorkflowStepRetry[]? Retries,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ActivityResultStatus? Status,
    string? ErrorMessage,
    object? Result
);

/// <summary>
/// Represents a workflow instance that has completed execution and has been archived, including its metadata, execution
/// details, and results.
/// </summary>
/// <param name="ID">The unique identifier of the archived workflow instance.</param>
/// <param name="SchedulerId">The identifier of the scheduler that executed the workflow, or null if not applicable.</param>
/// <param name="Name">The name of the workflow definition associated with this instance.</param>
/// <param name="Options">The options used to configure the workflow execution.</param>
/// <param name="StartedAt">The date and time, in UTC, when the workflow execution started.</param>
/// <param name="FinishedAt">The date and time, in UTC, when the workflow execution finished.</param>
/// <param name="IsSuccessful">true if the workflow completed successfully; otherwise, false.</param>
/// <param name="ErrorMessage">The error message if the workflow failed; otherwise, null.</param>
/// <param name="Arguments">The arguments provided to the workflow at the time of execution, or null if none.</param>
/// <param name="Steps">An array containing the steps executed as part of the workflow, in order.</param>
public record struct ArchivedWorkflow(
    Guid ID,
    Guid? SchedulerId,
    string Name,
    WorkflowOptions Options,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    bool IsSuccessful,
    string? ErrorMessage,
    object? Arguments,
    WorkflowStep[] Steps
);
