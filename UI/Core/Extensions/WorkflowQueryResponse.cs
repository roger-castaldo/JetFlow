namespace JetFlow.UI.Extensions;

internal record WorkflowQueryResponse(
    IEnumerable<ActiveWorkflow> Data,
    ulong LastOffset
);
