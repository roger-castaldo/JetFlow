using System.Text.Json.Serialization;

namespace JetFlow.Configs;

/// <summary>
/// Specifies the actions to take when a workflow completes, such as archiving or purging the workflow data. This allows for automated cleanup and management of workflow records based on the desired retention policies.
/// </summary>
public enum WorkflowCompletionActions
{
    /// <summary>
    /// Default behavior where no automatic action is taken upon workflow completion. The workflow data will remain in the system until manually archived or purged. This option allows for maximum flexibility in managing workflow records, but may require manual intervention to clean up old or completed workflows.
    /// </summary>
    None,
    /// <summary>
    /// Specifies that the item should be archived and no further action should be taken.
    /// </summary>
    ArchiveThenNothing,
    /// <summary>
    /// Specifies that items should be archived before being purged.
    /// </summary>
    /// <remarks>Use this option when it is necessary to retain a backup of items prior to permanent deletion.
    /// Archiving ensures that data can be restored if needed after the purge operation.</remarks>
    ArchiveThenPurge,
    /// <summary>
    /// Removes all items or data from the collection or resource, resetting it to an empty state.
    /// </summary>
    /// <remarks>Use this method to clear all contents. After calling this method, the collection or resource
    /// will contain no items. Any references to previously stored items will be released if applicable.</remarks>
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
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WorkflowCompletionActions CompletionAction { get; init; } = WorkflowCompletionActions.None;
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
