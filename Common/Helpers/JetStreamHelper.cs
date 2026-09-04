using JetFlow.Interfaces;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace JetFlow.Helpers;

internal static class JetStreamHelper
{
    private sealed class JetstreamQuery(INatsJSConsumer consumer, Func<INatsJSConsumer, ValueTask> deleteCallback) : IJetstreamQuery
    {
        private const int MaxMessages = 64;
        private bool disposed = false;

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (!disposed)
            {
                disposed=true;
                try
                {
                    await deleteCallback(consumer);
                }
                catch { /*bury error*/ }
            }
        }

        IAsyncEnumerator<INatsJSMsg<byte[]>> IAsyncEnumerable<INatsJSMsg<byte[]>>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => GetAllMessagesAsync(cancellationToken);

        private async IAsyncEnumerator<INatsJSMsg<byte[]>> GetAllMessagesAsync(CancellationToken cancellationToken)
        {
            // Fetch batches from the consumer and yield all available messages.
            // If a fetch returns no messages, treat the query as complete and exit the enumerator.
            while (!cancellationToken.IsCancellationRequested)
            {
                var cnt = 0;
                ulong numPending = ulong.MaxValue;
                await foreach (var msg in consumer.FetchAsync<byte[]>(new() { MaxMsgs = MaxMessages, Expires = TimeSpan.FromSeconds(1) }, cancellationToken: cancellationToken))
                {
                    cnt++;
                    yield return msg;
                    numPending = msg.Metadata?.NumPending ?? ulong.MaxValue;
                }

                if (cnt!=MaxMessages || numPending == 0)
                {
                    // No messages in this fetch, end the query.
                    yield break;
                }
            }
        }
    }

    public static async ValueTask<IJetstreamQuery> QueryStreamAsync(INatsJSContext jsContext, string streamName, bool headersOnly, params string[] filterSubjects)
    {
        var consumer = await jsContext.CreateOrUpdateConsumerAsync(
            streamName,
            new ConsumerConfig
            {
                Name = Guid.CreateVersion7().ToString(), // ephemeral identity
                DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                AckPolicy = ConsumerConfigAckPolicy.None,
                FilterSubjects = filterSubjects,
                HeadersOnly = headersOnly,
                InactiveThreshold = TimeSpan.FromSeconds(10)
            }
        );
        return new JetstreamQuery(consumer, async(consumer)=>_ = await jsContext.DeleteConsumerAsync(consumer.Info.StreamName, consumer.Info.Name));
    }
}
