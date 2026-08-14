using JetFlow.Helpers;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal abstract class AMetricSubscription(ServiceConnection serviceConnection, INatsJSConsumer consumer, MetricsHelper metricsHelper, CancellationToken cancellationToken)
    : ASubscription(serviceConnection, consumer, cancellationToken)
{
    protected MetricsHelper MetricsHelper => metricsHelper;
}
