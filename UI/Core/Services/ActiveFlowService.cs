using JetFlow.Interfaces;
using JetFlow.UI.Data;
using JetFlow.UI.Interfaces;

namespace JetFlow.UI.Services;

internal class ActiveFlowService(
    IDbConnection dbConnection,
    IConfigService configService, 
    IObservationConnection observationConnection
    )
    : IActiveFlowService
{
    public async ValueTask InitAsync()
    {
        configService.RegisterAddNamespaceCallback(async (namespaceName) =>
        {
            if (namespaceName is not null)
                await observationConnection.AddNamespaceAsync(namespaceName);
            else
                await observationConnection.AddDefaultNamespaceAsync();
        });
        configService.RegisterRemoveNamespaceCallback(async (namespaceName) =>
        {
            if (namespaceName is not null)
                await observationConnection.RemoveNamespaceAsync(namespaceName);
            else
                await observationConnection.RemoveDefaultNamespaceAsync();
        });
        var namespaces = await dbConnection.ListNamespacesAsync();
        await observationConnection.AddNamespacesAsync(namespaces.OfType<string>());
        if (namespaces.Any(n=>string.IsNullOrEmpty(n)))
            await observationConnection.AddDefaultNamespaceAsync();
    }

    async ValueTask<PerformanceCounters> IActiveFlowService.GetPerformanceCountersAsync(string workflowNamespace)
        => new(
            await observationConnection.GetActiveWorkflowCountAsync(workflowNamespace),
            await observationConnection.GetSuspendedWorkflowCountAsync(workflowNamespace),
            await observationConnection.GetActiveActivityCountAsync(workflowNamespace)
        );
}
