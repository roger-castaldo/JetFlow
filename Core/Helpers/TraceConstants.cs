namespace JetFlow.Helpers;

internal static class TraceConstants
{
    private const string BaseTag = "jetflow";
    public const string BaseWorkflowTag = $"{BaseTag}.workflow";
    public const string WorkflowNameTag = $"{BaseWorkflowTag}.name";
    public const string WorkflowIdTag = $"{BaseWorkflowTag}.id";
    public const string BaseActivityTag = $"{BaseTag}.workflowactivity";
    public const string ActivityNameTag = $"{BaseActivityTag}.name";
    public const string ActivityIdTag = $"{BaseActivityTag}.id";
    public const string MessageBaseTag = $"{BaseTag}.message";
}
