using JetFlow.Helpers;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions.Default;

internal class ActivityTimeoutsSubscription(MetricsHelper metricsHelper, ServiceConnection serviceConnection, INatsJSConsumer consumer, CancellationToken cancellationToken)
    : ASubscription(serviceConnection, consumer, cancellationToken)
{
    protected override async ValueTask ProcessMessageAsync(EventMessage message)
    {
        if (Equals(message.ActivityEventType, ActivityEventTypes.Timeout))
        {
            var (canRun, _) = await ServiceConnection.CanActivityRun(message, CancellationToken);
            if (canRun)
            {
                await ServiceConnection.MarkActivityDoneInStore(message, CancellationToken);
                await metricsHelper.TimeoutActivityAsync(message, CancellationToken);
                await RetryHelper.ProcessActivityRetryAsync(RetryTypes.Timeout, message, ServiceConnection, CancellationToken);
                await message.AckAsync(CancellationToken);
            }
        }
        else
            await message.NakAsync(CancellationToken);
    }
}
