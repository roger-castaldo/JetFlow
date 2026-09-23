using JetFlow.Helpers;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions.Default;

internal class PurgeWorkflowSubscription(MetricsHelper metricsHelper, ServiceConnection serviceConnection, INatsJSConsumer consumer, CancellationToken cancellationToken)
    : ACoreSubscription(serviceConnection, consumer, cancellationToken)
{
    protected override async ValueTask ProcessMessageAsync(EventMessage message)
    {
        await ServiceConnection.PurgeWorkflowAsync(message, CancellationToken);
        metricsHelper.PurgeWorkflow(message.WorkflowName);
        await message.AckAsync(CancellationToken);
    }
}
