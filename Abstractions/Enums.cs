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
/// Represents the status of a child workflow result, indicating if it was successful or failed.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WorkflowResult>))]
public enum WorkflowResult
{
    /// <summary>
    /// Indicates that the operation completed successfully.
    /// </summary>
    Success,
    /// <summary>
    /// Represents a failure result or state.
    /// </summary>
    Failure
}
