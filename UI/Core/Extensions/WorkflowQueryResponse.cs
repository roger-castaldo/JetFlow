namespace JetFlow.UI.Extensions;

internal record WorkflowQueryResponse<T>(
    IEnumerable<T> Data,
    ulong LastOffset
);
