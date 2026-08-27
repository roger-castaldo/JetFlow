using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal abstract class ACoreSubscription
    : ASubscription
{
    protected ServiceConnection ServiceConnection { get; private init; }
    protected ACoreSubscription(ServiceConnection serviceConnection, INatsJSConsumer consumer, CancellationToken cancellationToken)
        : base(consumer, cancellationToken)
    {
        ServiceConnection = serviceConnection;
    }

    protected override async ValueTask ProcessMessageAsync(INatsJSMsg<byte[]> msg)
    {
        var message = await EventMessage.CreateMessageAsync(ServiceConnection, msg, CancellationToken);
        await ProcessMessageAsync(message);
    }

    protected abstract ValueTask ProcessMessageAsync(EventMessage message);
}
