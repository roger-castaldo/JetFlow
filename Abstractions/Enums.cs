using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace JetFlow;

/// <summary>
/// Represents the status of an activity result, indicating whether it was successful, failed, or timed out.
/// </summary>
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter<ActivityResultStatus>))]
public enum ActivityResultStatus
{
    /// <summary>
    /// Indicates that the operation completed successfully.
    /// </summary>
    Success,
    /// <summary>
    /// Represents a failure result or state.
    /// </summary>
    Failure,
    /// <summary>
    /// Gets or sets the timeout interval for the operation.
    /// </summary>
    /// <remarks>Specify the duration to wait before the operation is canceled due to timeout. The value is
    /// typically expressed in milliseconds unless otherwise noted by the implementation.</remarks>
    Timeout
}

/// <summary>
/// Specifies the type of workflow step, which can be either an action (Activity) or a delay (a pause in the workflow).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WorkflowStepTypes>))]
public enum WorkflowStepTypes
{
    /// <summary>
    /// Represents an action step, typically an activity or task to be executed.
    /// </summary>
    Action,
    /// <summary>
    /// Represents a Delay called within the workflow
    /// </summary>
    Delay,
    /// <summary>
    /// Represents a suspension of the workflow
    /// </summary>
    Suspended,
    /// <summary>
    /// Represents a child workflow execution
    /// </summary>
    ChildWorkflow
}

/// <summary>
/// Specifies the type of retry that occurred during a workflow step, which can be either a timeout or an error.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RetryTypes>))]
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