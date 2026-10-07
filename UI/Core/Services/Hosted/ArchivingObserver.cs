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
            try
            {
                await dbConnection.StoreArchiveAsync(archiveEvent.Archive, archiveEvent.WorkflowNamespace);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to store archive for workflow namespace: {archiveEvent.WorkflowNamespace}, Error: {ex.Message}");
            }
            return true;
        });
    }
}
