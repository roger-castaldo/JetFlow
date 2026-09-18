namespace JetFlow;

/// <summary>
/// Houses an event to indicate that a workflow was archived
/// </summary>
/// <param name="WorkflowNamespace">The namespace that the workflow belongs to, null if default</param>
/// <param name="Archive">The content of the archived workflow</param>
public record ArchivedWorkflowEvent(
    string? WorkflowNamespace,
    ArchivedWorkflow Archive
);
