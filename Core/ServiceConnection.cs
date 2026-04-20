using JetFlow.Helpers;
using JetFlow.Interfaces;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.KeyValueStore;
using NATS.Client.ObjectStore;

namespace JetFlow;

internal partial class ServiceConnection(INatsConnection connection, INatsJSContext jsContext, 
    INatsKVStore timerStore, INatsKVStore configurationStore, INatsObjStore archiveStore,
    SubjectMapper subjectMapper, MessageSerializer messageSerializer)
{
    private const string TTLHeader = "Nats-TTL";
    private const string ScheduleDelayHeader = "Nats-Schedule";
    private const string ScheduleTargetHeader = "Nats-Schedule-Target";
    private const string ScheduledTargetTTL = "Nats-Schedule-TTL";
    private const string MessageIdHeader = "Nats-Msg-Id";
    private const string ActivityIdHeader = "x-jetflow-activity-id";

    private static string CreateTTLString(TimeSpan ttl)
    {
        if (ttl == TimeSpan.Zero)
        {
            return "0s";
        }

        // Determine sign and work with absolute value
        var negative = ttl < TimeSpan.Zero;

        // Safely get absolute ticks (avoid overflow with long.MinValue)
        long ticks = ttl.Ticks;
        ulong absTicks;
        if (ticks < 0)
        {
            // -(long.MinValue) would overflow; compute via unsigned arithmetic
            absTicks = (ulong)(-(ticks + 1)) + 1UL;
        }
        else
        {
            absTicks = (ulong)ticks;
        }

        // 1 tick = 100 nanoseconds
        ulong ns = absTicks * 100UL;

        const ulong NsPerSecond = 1_000_000_000UL;
        const ulong NsPerMinute = NsPerSecond * 60UL;
        const ulong NsPerHour = NsPerMinute * 60UL;

        var sb = new System.Text.StringBuilder();

        if (negative) sb.Append('-');

        var hours = ns / NsPerHour;
        ns %= NsPerHour;
        if (hours > 0)
        {
            sb.Append(hours).Append('h');
        }

        var minutes = ns / NsPerMinute;
        ns %= NsPerMinute;
        if (minutes > 0)
        {
            sb.Append(minutes).Append('m');
        }

        var seconds = ns / NsPerSecond;
        var remainderNs = ns % NsPerSecond;

        if (seconds > 0 || remainderNs > 0)
        {
            if (remainderNs > 0)
            {
                // fractional seconds with up to 9 digits (nanoseconds)
                var frac = remainderNs.ToString("D9").TrimEnd('0');
                sb.Append(seconds).Append('.').Append(frac).Append('s');
            }
            else
            {
                sb.Append(seconds).Append('s');
            }
        }

        return sb.ToString();
    }

    private static NatsHeaders AppendDefaultHeaders(NatsHeaders headers, string messageId, Guid? activityId, TimeSpan? timeout)
    {
        if (timeout.HasValue)
            headers.Add(TTLHeader, CreateTTLString(timeout.Value));
        headers.Add(MessageIdHeader, messageId);
        if (activityId!=null)
            headers.Add(ActivityIdHeader, activityId.ToString());
        return TraceHelper.InjectCurrentActivity(headers);
    }

    private async ValueTask PublishMessageAsync(byte[] data, string subject, NatsHeaders headers, string messageId, Guid? activityId = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        await connection.PublishAsync<byte[]>(subject, data, AppendDefaultHeaders(headers, messageId, activityId, timeout), cancellationToken: cancellationToken);
        TraceHelper.AddPublishEvent(subject);
    }

    private async ValueTask PublishDelayedMessageAsync(byte[] data, string subject, NatsHeaders headers, TimeSpan delay, string destinationSubject, string messageId, Guid? activityId = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        headers = AppendDefaultHeaders(headers, messageId, activityId, null);
        headers.Add(ScheduleDelayHeader, DateTime.UtcNow.Add(delay).ToString("'@at 'yyyy-MM-dd'T'HH:mm:ss'Z'"));
        headers.Add(ScheduleTargetHeader, destinationSubject);
        if (timeout.HasValue)
            headers.Add(ScheduledTargetTTL, CreateTTLString(timeout.Value));
        await jsContext.PublishAsync<byte[]>(subject, data, headers: headers, cancellationToken: cancellationToken);
        TraceHelper.AddPublishEvent(subject);
    }

    public static Guid GetActivityID(INatsJSMsg<byte[]> msg)
        => (msg.Headers?.TryGetValue(ActivityIdHeader, out var id) == true && Guid.TryParse(id, out var guid)) ? guid : Guid.Empty;

    internal static string GetMessageID(INatsJSMsg<byte[]> msg)
        => (msg.Headers?.TryGetValue(MessageIdHeader, out var id)==true ? id.ToString() : string.Empty);

    public ValueTask<INatsJSConsumer> CreateOrUpdateConsumerAsync(
        string stream,
        ConsumerConfig config,
        CancellationToken cancellationToken = default)
        => jsContext.CreateOrUpdateConsumerAsync(stream, config, cancellationToken);

    public async ValueTask<IJetstreamQuery> QueryStreamAsync(string streamName, bool headersOnly, params string[] filterSubjects)
        => new JetstreamQuery(await jsContext.CreateOrUpdateConsumerAsync(
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
            ), jsContext);

    public async ValueTask PurgeWorkflowAsync(EventMessage message, CancellationToken cancellationToken)
    {
        await jsContext.PurgeStreamAsync(subjectMapper.ActivityQueueStream, new() { Filter = subjectMapper.WorkflowActivityPurgeFilter(message.WorkflowName, message.WorkflowId) }, cancellationToken);
        await jsContext.PurgeStreamAsync(subjectMapper.WorkflowEventsStreamsName, new() { Filter = subjectMapper.WorkflowPurgeFilter(message.WorkflowName, message.WorkflowId) }, cancellationToken);
    }

    private class JetstreamQuery(INatsJSConsumer consumer, INatsJSContext jsContext) : IJetstreamQuery
    {
        private bool disposed = false;

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (!disposed)
            {
                disposed=true;
                try
                {
                    await jsContext.DeleteConsumerAsync(consumer.Info.StreamName, consumer.Info.Name);
                }
                catch
                {
                    //bury error
                }
            }
        }

        IAsyncEnumerator<INatsJSMsg<byte[]>> IAsyncEnumerable<INatsJSMsg<byte[]>>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => GetAllMessagesAsync(cancellationToken);

        private async IAsyncEnumerator<INatsJSMsg<byte[]>> GetAllMessagesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Fetch batches from the consumer and yield all available messages.
            // If a fetch returns no messages, treat the query as complete and exit the enumerator.
            while (!cancellationToken.IsCancellationRequested)
            {
                var cnt = 0;
                await foreach (var msg in consumer.FetchAsync<byte[]>(new() { MaxMsgs = 10, Expires = TimeSpan.FromSeconds(1) }, cancellationToken: cancellationToken))
                {
                    cnt++;
                    yield return msg;
                }

                if (cnt!=10)
                {
                    // No messages in this fetch, end the query.
                    yield break;
                }
            }
        }
    }
}
