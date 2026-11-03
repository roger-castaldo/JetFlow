using JetFlow.UI.Interfaces;

namespace JetFlow.UI.Messages;

internal record WorkflowQuery(
    string? WorkflowNamespace,
    string WorkflowName,
    Dictionary<string,string?>? MetaData,
    IEnumerable<IArgumentFilter>? Argument,
    ulong Offset=0,
    uint PageSize=10
);

internal record ArchivedWorkflowQuery(
    string? WorkflowNamespace,
    string WorkflowName,
    DateRange? StartedAt, 
    DateRange? CompletedAt,
    Dictionary<string, string?>? MetaData,
    IEnumerable<IArgumentFilter>? Argument,
    ulong Offset = 0,
    uint PageSize = 10
) : WorkflowQuery(WorkflowNamespace, WorkflowName, MetaData, Argument, Offset, PageSize);