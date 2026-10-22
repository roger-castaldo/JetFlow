using JetFlow.UI.Interfaces;

namespace JetFlow.UI.Extensions;

internal record WorkflowQuery(
    string? WorkflowNamespace,
    string WorkflowName,
    Dictionary<string,string?>? MetaData,
    IEnumerable<IArgumentFilter>? Argument,
    ulong Offset=0,
    uint PageSize=10
);
