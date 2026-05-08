using JetFlow.Helpers;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal class ActivityTimeoutsSubscription(ServiceConnection serviceConnection, INatsJSConsumer consumer, CancellationToken cancellationToken)
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
                await RetryHelper.ProcessActivityRetryAsync(RetryTypes.Timeout, message, ServiceConnection, CancellationToken);
                await message.Message.AckAsync(cancellationToken: CancellationToken);
            }
        }
        else
            await message.Message.NakAsync(cancellationToken: CancellationToken);
    }
}
