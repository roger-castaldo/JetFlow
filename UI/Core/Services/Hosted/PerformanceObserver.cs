using JetFlow.Interfaces;
using JetFlow.UI.Interfaces;
using NATS.Client.Core;

namespace JetFlow.UI.Services.Hosted;

internal class PerformanceObserver(IDbConnection dbConnection, IConfigService configService, NatsOpts natsOpts)
    : AObserverService(configService, natsOpts)
{
    protected override async Task RegisterActionsAsync(IObservationConnection observationConnection)
    {
        await observationConnection.AddPerformanceMonitoringAsync(1,
            async workflowEvent =>
            {
                await dbConnection.StoreWorkflowPerformanceRecordAsync(workflowEvent.namespaceName, workflowEvent.PerformanceRecord);
            },
            async activityEvent =>
            {
                await dbConnection.StoreActivityPerformanceRecordAsync(activityEvent.namespaceName, activityEvent.PerformanceRecord);
            }
        );
    }
}
