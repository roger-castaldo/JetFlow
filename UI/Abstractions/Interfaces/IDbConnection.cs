namespace JetFlow.UI.Interfaces;

public interface IDbConnection
{
    ValueTask InitAsync();
    ValueTask RegisterNamespaceAsync(string? namespaceName);
    ValueTask UnregisterNamespaceAsync(string? namespaceName);
    ValueTask<IEnumerable<string?>> ListNamespacesAsync();
    ValueTask StoreArchiveAsync(ArchivedWorkflow archive, string? namespaceName);
}
