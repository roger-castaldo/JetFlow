using JetFlow.Interfaces;
using JetFlow.UI.Interfaces;
using NATS.Client.Core;

namespace JetFlow.UI.Services.Hosted;

internal class ArchivingObserver(IDbConnection dbConnection, IConfigService configService, NatsOpts natsOpts) 
    : AObserverService(configService, natsOpts)
{
    protected override async Task RegisterActionsAsync(IObservationConnection observationConnection)
    {
        await observationConnection.AddArchivingListenerAsync(async archiveEvent =>
        {
            await dbConnection.StoreArchiveAsync(archiveEvent.Archive, archiveEvent.WorkflowNamespace);
            return true;
        });
    }
}
