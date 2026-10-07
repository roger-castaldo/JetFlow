using JetFlow.Interfaces;
using JetFlow.UI.Interfaces;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;

namespace JetFlow.UI.Services.Hosted;

internal abstract class AObserverService(IConfigService configService, NatsOpts natsOpts) : IHostedService
{
    private IObservationConnection? observationConnection = null;

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsOpts));
        configService.RegisterAddNamespaceCallback(async namespaceName =>
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                await observationConnection.AddDefaultNamespaceAsync();
            else
                await observationConnection.AddNamespaceAsync(namespaceName);
        });
        configService.RegisterRemoveNamespaceCallback(async namespaceName =>
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                await observationConnection.RemoveDefaultNamespaceAsync();
            else
                await observationConnection.RemoveNamespaceAsync(namespaceName);
        });
        var currentNamespaces = await configService.GetCurrentNamespacesAsync();
        await observationConnection.AddNamespacesAsync(currentNamespaces.Where(ns => !string.IsNullOrWhiteSpace(ns)).OfType<string>());
        if (currentNamespaces.Any(ns => string.IsNullOrWhiteSpace(ns)))
            await observationConnection.AddDefaultNamespaceAsync();
        await RegisterActionsAsync(observationConnection);
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
        => await (observationConnection?.DisposeAsync() ?? ValueTask.CompletedTask);

    protected abstract Task RegisterActionsAsync(IObservationConnection observationConnection);
}
