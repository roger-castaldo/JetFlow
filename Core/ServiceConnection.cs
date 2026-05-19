using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.KeyValueStore;
using NATS.Client.ObjectStore;

namespace JetFlow;

internal partial class ServiceConnection(InternalNatsConnection connection, 
    INatsKVStore timerStore, INatsKVStore configurationStore, INatsObjStore archiveStore,
    SubjectMapper subjectMapper, MessageSerializer messageSerializer)
{
    public async ValueTask<IJetstreamQuery> QueryStreamAsync(string streamName, bool headersOnly, params string[] filterSubjects)
        => new JetstreamQuery(await connection.CreateOrUpdateConsumerAsync(
                streamName,
                new ConsumerConfig
                {
                    Name = Guid.NewGuid().ToString(), // ephemeral identity
                    DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                    AckPolicy = ConsumerConfigAckPolicy.None,
                    FilterSubjects = filterSubjects,
                    HeadersOnly = headersOnly,
                    InactiveThreshold = TimeSpan.FromSeconds(10)
                }
            ), connection);

    public async ValueTask PurgeWorkflowAsync(EventMessage message, CancellationToken cancellationToken)
    {
        await connection.PurgeStreamAsync(subjectMapper.ActivityQueueStream, new() { Filter = subjectMapper.WorkflowActivityPurgeFilter(message.WorkflowName, message.WorkflowId) }, cancellationToken);
        await connection.PurgeStreamAsync(subjectMapper.WorkflowEventsStreamsName, new() { Filter = subjectMapper.WorkflowPurgeFilter(message.WorkflowName, message.WorkflowId) }, cancellationToken);
    }

    private sealed class JetstreamQuery(INatsJSConsumer consumer, InternalNatsConnection connection) : IJetstreamQuery
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
                    await connection.DeleteConsumerAsync(consumer.Info.StreamName, consumer.Info.Name);
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
                await foreach (var msg in consumer.FetchAsync<byte[]>(new() { MaxMsgs = MaxMessages, Expires = TimeSpan.FromSeconds(1) }, cancellationToken: cancellationToken))
                {
                    cnt++;
                    yield return msg;
                }

                if (cnt!=MaxMessages)
                {
                    // No messages in this fetch, end the query.
                    yield break;
                }
            }
        }
    }
}
