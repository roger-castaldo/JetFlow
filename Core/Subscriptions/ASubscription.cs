using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal abstract class ASubscription(ServiceConnection serviceConnection, INatsJSConsumer consumer, CancellationToken cancellationToken)
{
    protected CancellationToken CancellationToken => cancellationToken;
    protected ServiceConnection ServiceConnection => serviceConnection;

    private Task? runningTask;
    public void Start()
        => runningTask = StartStream();

    protected async Task StartStream()
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await consumer.RefreshAsync(cancellationToken); // or try to recreate consumer

                await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationToken))
                {
                    var message = new EventMessage(msg);
                    await ProcessMessageAsync(new(msg));
                }
            }
            catch (NatsJSProtocolException e)
            {
                //bury error
            }
            catch (NatsJSException e)
            {
                // log exception
                await Task.Delay(1000, cancellationToken); // backoff
            }
        }
    }

    protected abstract ValueTask ProcessMessageAsync(EventMessage message);
}
