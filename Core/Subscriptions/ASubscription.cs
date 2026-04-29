using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal abstract class ASubscription
{
    protected CancellationToken CancellationToken { get; private init; }
    protected ServiceConnection ServiceConnection { get; private init; }
    private readonly INatsJSConsumer consumer;
    private readonly Task runningTask;

    protected ASubscription(ServiceConnection serviceConnection, INatsJSConsumer consumer, CancellationToken cancellationToken)
    {
        ServiceConnection = serviceConnection;
        this.consumer = consumer;
        CancellationToken = cancellationToken;
        runningTask = StartStream();
    }

    private async Task StartStream()
    {
        while (!CancellationToken.IsCancellationRequested)
        {
            try
            {
                await consumer.RefreshAsync(CancellationToken); // or try to recreate consumer
                await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: CancellationToken))
                {
                    var message = new EventMessage(msg);
                    await ProcessMessageAsync(new(msg));
                }
            }
            catch (NatsJSProtocolException)
            {
                //bury error
            }
            catch (NatsJSException)
            {
                // log exception
                await Task.Delay(1000, CancellationToken); // backoff
            }
            catch (OperationCanceledException)
            {
                // expected on cancellation, ignore
            }
        }
    }

    protected abstract ValueTask ProcessMessageAsync(EventMessage message);

    public async Task AwaitClose()
        => await (runningTask.IsCompleted ? Task.CompletedTask : runningTask);
}
