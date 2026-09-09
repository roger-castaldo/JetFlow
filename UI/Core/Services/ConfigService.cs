using JetFlow.UI.Interfaces;
using System.Collections.Concurrent;

namespace JetFlow.UI.Services;

internal class ConfigService(IDbConnection dbConnection)
    : IConfigService
{
    private readonly ConcurrentBag<Func<string?, Task>> addNamespaceCallbacks = [];
    private readonly ConcurrentBag<Func<string?, Task>> removeNamespaceCallbacks = [];
    async ValueTask IConfigService.RegisterNamespaceAsync(string? namespaceName)
    {
        await dbConnection.RegisterNamespaceAsync(namespaceName);
        await Task.WhenAll(addNamespaceCallbacks.Select(callback => callback(namespaceName)));
    }

    void IConfigService.RegisterAddNamespaceCallback(Func<string?, Task> callback)
        => addNamespaceCallbacks.Add(callback);

    void IConfigService.RegisterRemoveNamespaceCallback(Func<string?, Task> callback)
        => removeNamespaceCallbacks.Add(callback);

    async ValueTask IConfigService.UnregisterNamespaceAsync(string? namespaceName)
    {
        await dbConnection.UnregisterNamespaceAsync(namespaceName);
        await Task.WhenAll(removeNamespaceCallbacks.Select(callback => callback(namespaceName)));
    }
}
