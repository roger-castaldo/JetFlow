using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal abstract class ASubscription
{
    protected CancellationToken CancellationToken { get; private init; }
    private readonly INatsJSConsumer consumer;
    private readonly Task runningTask;

    protected ASubscription(INatsJSConsumer consumer, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        this.consumer = consumer;
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
                    await ProcessMessageAsync(msg);
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

    protected abstract ValueTask ProcessMessageAsync(INatsJSMsg<byte[]> msg);
    public async Task AwaitClose()
        => await (runningTask.IsCompleted ? Task.CompletedTask : runningTask);
}
