namespace JetFlow.Helpers;

internal static class TraceConstants
{
    private const string BaseTag = "jetflow";
    public const string BaseWorkflowTag = $"{BaseTag}.workflow";
    public const string WorkflowNameTag = $"{BaseWorkflowTag}.name";
    public const string WorkflowIdTag = $"{BaseWorkflowTag}.id";
    public const string WorkflowQueueLatency = $"{BaseWorkflowTag}.queue.latency";
    public const string WorkflowsStarted = $"{BaseWorkflowTag}.started";
    public const string WorkflowsCompleted= $"{BaseWorkflowTag}.completed";
    public const string BaseActivityTag = $"{BaseTag}.workflowactivity";
    public const string ActivityNameTag = $"{BaseActivityTag}.name";
    public const string ActivityIdTag = $"{BaseActivityTag}.id";
    public const string ActivityTimeoutIdTag = $"{BaseActivityTag}.timeout";
    public const string ActivityDuration = $"{BaseActivityTag}.duration";
    public const string ActivityQueueLatency = $"{BaseActivityTag}.queue.latency";
    public const string MessageBaseTag = $"{BaseTag}.message";
    public const string MessageSubjectTag = $"{MessageBaseTag}.subject";
    public const string MessageContentHeaderTag = $"{MessageBaseTag}.contentheader";

    public const string WorkflowStepStart = "WorkflowStepStart";
    public const string WorkflowStart = "WorkflowStart";
    public const string WorkflowDelayStart = "WorkflowDelayStart";
    public const string WorkflowActivityStart = "WorkflowActivityStart";
    public const string MessagePublished = "MessagePublished";
    public const string MessageDecoded = "MessageDecoded";
}
