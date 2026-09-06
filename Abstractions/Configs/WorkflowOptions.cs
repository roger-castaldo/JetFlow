using System.Text.Json.Serialization;

namespace JetFlow.Configs;

/// <summary>
/// Specifies the actions to take when a workflow completes, such as archiving or purging the workflow data. This allows for automated cleanup and management of workflow records based on the desired retention policies.
/// </summary>
public enum WorkflowCompletionActions
{
    /// <summary>
    /// Default behavior specifies that the workflow should be archived before being purged. This ensures that a backup of the workflow data is retained for future reference or auditing purposes, while still allowing for eventual cleanup of completed workflows.
    /// </summary>
    Archive,
    /// <summary>
    /// Specifies that the workflow should be purged upon completion. This will permanently remove the workflow data from the system, and there will be no archive or backup retained. Use this option when you want to immediately clean up completed workflows without retaining any historical data.
    /// </summary>
    Purge
}

/// <summary>
/// Represents configuration options for workflow execution, including completion actions, purge behavior, and error
/// handling settings.
/// </summary>
/// <remarks>Use this type to specify how a workflow should behave upon completion, how long to retain workflow
/// data, and whether to treat activity timeouts or failures as errors. All properties are immutable and must be set at
/// initialization.</remarks>
public sealed record WorkflowOptions
{
    /// <summary>
    /// Gets the action to perform when the workflow completes.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<WorkflowCompletionActions>))]
    public WorkflowCompletionActions CompletionAction { get; init; } = WorkflowCompletionActions.Archive;
    /// <summary>
    /// Gets the optional delay before purging items.
    /// </summary>
    /// <remarks>If set, items will not be purged until the specified delay has elapsed. If null, items may be
    /// purged immediately according to the default policy.</remarks>
    public TimeSpan? PurgeDelay { get; init;  } = null;
    /// <summary>
    /// Gets a value indicating whether an exception is thrown when an activity times out.
    /// </summary>
    /// <remarks>When set to <see langword="true"/>, the operation will throw an exception if an activity
    /// exceeds its allowed timeout period. When set to <see langword="false"/>, the operation may complete without
    /// throwing, even if a timeout occurs.</remarks>
    public bool ErrorOnActivityTimeout { get; init; } = false;
    /// <summary>
    /// Gets a value indicating whether an error should be thrown when an activity fails.
    /// </summary>
    public bool ErrorOnActivityFailure { get; init; } = false;
}
