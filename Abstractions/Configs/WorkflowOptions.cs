using System.Text.Json.Serialization;

namespace JetFlow.Configs;

public enum WorkflowCompletionActions
{
    None,
    ArchiveThenNothing,
    ArchiveThenPurge,
    Purge
}

public sealed record WorkflowOptions
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WorkflowCompletionActions CompletionAction { get; init; } = WorkflowCompletionActions.None;
    public TimeSpan? PurgeDelay { get; init;  } = null;
    public bool ErrorOnActivityTimeout { get; init; } = false;
    public bool ErrorOnActivityFailure { get; init; } = false;
}
