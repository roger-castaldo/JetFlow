namespace JetFlow.UI.Interfaces;

internal interface IConfigService
{
    void RegisterAddNamespaceCallback(Func<string?, Task> callback);
    void RegisterRemoveNamespaceCallback(Func<string?, Task> callback);
    ValueTask RegisterNamespaceAsync(string? namespaceName);
    ValueTask UnregisterNamespaceAsync(string? namespaceName);
}
