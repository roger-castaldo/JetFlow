using JetFlow.Interfaces;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace JetFlow.Helpers;

internal static class JetStreamHelper
{
    private sealed class JetstreamQuery(INatsJSConsumer consumer, Func<INatsJSConsumer, ValueTask> deleteCallback) : IJetstreamQuery
    {
        private static readonly NatsJSConsumeOpts consumeOpts = new()
        {
            MaxMsgs = 1000,
            ThresholdMsgs = 750
        };
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
            await consumer.RefreshAsync(cancellationToken: cancellationToken);
            if (consumer.Info.NumPending == 0 || cancellationToken.IsCancellationRequested)
                yield break;
            await foreach (var msg in consumer.ConsumeAsync<byte[]>(opts: consumeOpts, cancellationToken: cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                yield return msg;
                if (msg.Metadata?.NumPending == 0 || cancellationToken.IsCancellationRequested)
                {
                    // No more messages in the stream, end the query.
                    yield break;
                }
            }
        }
    }

    public static ValueTask<IJetstreamQuery> QueryStreamAsync(INatsJSContext jsContext, string streamName, bool headersOnly, params string[] filterSubjects)
        => QueryStreamAsync(jsContext, streamName, headersOnly, 0, filterSubjects);

    public static async ValueTask<IJetstreamQuery> QueryStreamAsync(INatsJSContext jsContext, string streamName, bool headersOnly, ulong previousLastMessageOffset, params string[] filterSubjects)
    {
        var config = new ConsumerConfig
        {
            Name = Guid.CreateVersion7().ToString(), // ephemeral identity
            DeliverPolicy = (previousLastMessageOffset==0 ? ConsumerConfigDeliverPolicy.All : ConsumerConfigDeliverPolicy.ByStartSequence),
            AckPolicy = ConsumerConfigAckPolicy.None,
            FilterSubjects = filterSubjects,
            HeadersOnly = headersOnly,
            InactiveThreshold = TimeSpan.FromSeconds(10)
        };
        if (previousLastMessageOffset!=0)
            config.OptStartSeq = previousLastMessageOffset+1;
        var consumer = await jsContext.CreateOrUpdateConsumerAsync(
            streamName,
            config
        );
        return new JetstreamQuery(consumer, async (consumer) => _ = await jsContext.DeleteConsumerAsync(consumer.Info.StreamName, consumer.Info.Name));
    }
}
