using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace JetFlow.Subscriptions;

internal abstract class ASubscription(INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore,
    MessageSerializer messageSerializer, CancellationToken cancellationToken)
{
    protected INatsConnection Connection => connection;
    protected INatsJSContext NatsJSContext => natsJSContext;
    protected INatsKVStore TimerStore => timerStore;
    protected MessageSerializer MessageSerializer => messageSerializer;

    private Task? runningTask;
    public void Start()
        => runningTask = StartStream(cancellationToken);

    protected abstract Task StartStream(CancellationToken cancellationToken);
}
