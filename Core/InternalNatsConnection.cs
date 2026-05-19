using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace JetFlow;

internal class InternalNatsConnection(INatsConnection connection, INatsJSContext jsContext, Version? serverVersion)
{
    public record PublishMessage(byte[] Data, string Subject, NatsHeaders Headers, string Id, TimeSpan? Timeout = null);
    public record DelayedPublishMessage(byte[] Data, string Subject, NatsHeaders Headers, string Id, TimeSpan Delay, string DestinationSubject, TimeSpan? Timeout = null)
        : PublishMessage(Data, Subject, Headers, Id, Timeout);

    private const string TTLHeader = "Nats-TTL";
    private const string ScheduleDelayHeader = "Nats-Schedule";
    private const string ScheduleTargetHeader = "Nats-Schedule-Target";
    private const string ScheduledTargetTTL = "Nats-Schedule-TTL";
    private const string MessageIdHeader = "Nats-Msg-Id";
    private const string BatchIdHeader = "Nats-Batch-Id";
    private const string BatchSequenceHeader = "Nats-Batch-Sequence";
    private const string BatchCommitHeader = "Nats-Batch-Commit";

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

    public static string CreateScheduledString(TimeSpan delay)
        => DateTime.UtcNow.Add(delay).ToString("'@at 'yyyy-MM-dd'T'HH:mm:ss'Z'");

    private static NatsHeaders AppendDefaultHeaders(NatsHeaders headers, string messageId, TimeSpan? timeout)
    {
        if (timeout.HasValue)
            headers.Add(TTLHeader, CreateTTLString(timeout.Value));
        headers.Add(MessageIdHeader, messageId);
        return TraceHelper.InjectCurrentActivity(headers);
    }
    public ValueTask PublishMessageAsync(PublishMessage message, CancellationToken cancellationToken = default)
        => PublishMessageAsync(message.Subject, message.Data, AppendDefaultHeaders(message.Headers, message.Id, message.Timeout), cancellationToken);
    public ValueTask PublishDelayedMessageAsync(DelayedPublishMessage message, CancellationToken cancellationToken = default)
        => PublishScheduledMessageAsync(message, CreateScheduledString(message.Delay), message.DestinationSubject, cancellationToken);
    public async ValueTask PublishScheduledMessageAsync(PublishMessage message, string cronString, string destinationSubject, CancellationToken cancellationToken = default)
    {
        var headers = AppendDefaultHeaders(message.Headers, message.Id, null);
        headers.Add(ScheduleDelayHeader, cronString);
        headers.Add(ScheduleTargetHeader, destinationSubject);
        if (message.Timeout.HasValue)
            headers.Add(ScheduledTargetTTL, CreateTTLString(message.Timeout.Value));
        await PublishMessageAsync(message.Subject, message.Data, headers, cancellationToken);
    }
    private async ValueTask PublishMessageAsync(string subject, byte[] data, NatsHeaders headers, CancellationToken cancellationToken)
    {
        await jsContext.PublishAsync<byte[]>(subject, data, headers: headers, cancellationToken: cancellationToken);
        TraceHelper.AddPublishEvent(subject);
    }
    public static string GetMessageID(INatsJSMsg<byte[]> msg)
        => (msg.Headers?.TryGetValue(MessageIdHeader, out var id)==true ? id.ToString() : string.Empty);
    public ValueTask<INatsJSConsumer> CreateOrUpdateConsumerAsync(
        string stream,
        ConsumerConfig config,
        CancellationToken cancellationToken = default)
        => jsContext.CreateOrUpdateConsumerAsync(stream, config, cancellationToken);

    internal ValueTask<StreamPurgeResponse> PurgeStreamAsync(string stream, StreamPurgeRequest request, CancellationToken cancellationToken)
        => jsContext.PurgeStreamAsync(stream, request, cancellationToken);

    internal ValueTask<bool> DeleteConsumerAsync(string streamName, string consumerName)
        => jsContext.DeleteConsumerAsync(streamName, consumerName);
}
