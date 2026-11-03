namespace JetFlow.UI.Messages;

internal record WorkflowQueryResponse<T>(
    IEnumerable<T> Data,
    ulong LastOffset
);
