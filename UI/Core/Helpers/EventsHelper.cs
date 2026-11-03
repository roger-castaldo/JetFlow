using JetFlow.UI.Messages;
using System.Text.Json;

namespace JetFlow.UI.Helpers;

internal static class EventsHelper
{
    public const string NamespaceEvent = "NamespaceConfigChange";
    private record struct NamespaceChangeEvent(bool Add, string? NamespaceName);
    public static Event ProduceNamespaceChange(bool add, string? namespaceName)
        => new(NamespaceEvent, JsonSerializer.SerializeToElement<NamespaceChangeEvent>(new(add, namespaceName)));
    public static (bool add, string? namespaceName) ExtractNamespaceChangeDetails(JsonElement element)
    {
        var evnt = JsonSerializer.Deserialize<NamespaceChangeEvent>(element)!;
        return (evnt.Add, evnt.NamespaceName);
    }

    public const string WorkflowArgumentDefinitionEvent = "WorkflowArgumentDefinitionChange";
    private record struct ArgumentChangeEvent(string? NamespaceName, string ElementName);
    public static Event ProduceWorkflowArgumentChange(string? namespaceName, string workflowName)
        => new(WorkflowArgumentDefinitionEvent, JsonSerializer.SerializeToElement<ArgumentChangeEvent>(new(namespaceName, workflowName)));
    public static (string? namespaceName, string workflowName) ExtractWorkflowArgumentChangeDetials(JsonElement element)
    {
        var evnt = JsonSerializer.Deserialize<ArgumentChangeEvent>(element)!;
        return (evnt.NamespaceName, evnt.ElementName);
    }
    public const string ActivityArgumentDefinitionEvent = "ActivityArgumentDefinitionChange";
    public static Event ProduceActivityArgumentChange(string? namespaceName, string workflowName)
        => new(ActivityArgumentDefinitionEvent, JsonSerializer.SerializeToElement<ArgumentChangeEvent>(new(namespaceName, workflowName)));
    public static (string? namespaceName, string workflowName) ExtractActivityArgumentChangeDetials(JsonElement element)
    {
        var evnt = JsonSerializer.Deserialize<ArgumentChangeEvent>(element)!;
        return (evnt.NamespaceName, evnt.ElementName);
    }
}
