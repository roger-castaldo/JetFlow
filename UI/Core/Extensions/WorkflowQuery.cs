using System.Text.Json;

namespace JetFlow.UI.Extensions;

internal record WorkflowQuery(
    string? WorkflowNamespace,
    string WorkflowName,
    Dictionary<string,string?>? MetaData,
    JsonElement? Argument,
    ulong Offset=0,
    uint PageSize=10
);
