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
    public WorkflowCompletionActions CompletionAction { get; init; } = WorkflowCompletionActions.None;
    public TimeSpan? PurgeDelay { get; init;  } = null;
}
