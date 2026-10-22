namespace JetFlow.UI.Interfaces;

internal interface IConfigService
{
    void RegisterAddNamespaceCallback(Func<string?, Task> callback);
    void RegisterRemoveNamespaceCallback(Func<string?, Task> callback);
    ValueTask RegisterNamespaceAsync(string? namespaceName);
    ValueTask UnregisterNamespaceAsync(string? namespaceName);
    ValueTask<IEnumerable<string?>> GetCurrentNamespacesAsync();
    ValueTask<IArgumentDefinition?> GetWorkflowArgumentDefinitionAsync(string? namespaceName, string workflowName);
    ValueTask<IArgumentDefinition?> GetActivityArgumentDefinitionAsync(string? namespaceName, string activityName);
}
