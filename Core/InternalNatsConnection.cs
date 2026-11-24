using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using System.Diagnostics;
using System.Text.Json;

namespace JetFlow;

internal class InternalNatsConnection(INatsConnection connection, INatsJSContext jsContext, Version? serverVersion)
{
    public Version? ServerVersion = serverVersion;
    public int MaxMessagePayload => connection.ServerInfo?.MaxPayload??1_048_576;

    public INatsJSContext JSContext => jsContext;

    public record PublishMessage(byte[] Data, string Subject, NatsHeaders Headers, string Id, TimeSpan? Timeout = null);

    public record ScheduledPublishMessage(byte[] Data, string Subject, NatsHeaders Headers, string Id, string DelayString, string DestinationSubject, TimeSpan? Timeout = null)
        : PublishMessage(Data, Subject, Headers, Id, Timeout)
    {
        public static ScheduledPublishMessage CreateDelayedMessage(byte[] data, string subject, NatsHeaders headers, string id, TimeSpan delay, string destinationSubject, TimeSpan? timeout = null)
            => new(data, subject, headers, id, CreateScheduledString(delay), destinationSubject, timeout);
    }

    private const int DefaultChunkSize = 1000;
    private const int MaxBatchRetries = 8;
    private const int BatchIncompleteError = 10176;
    private const int BatchTooLargeError = 10199;
    private const int TooManyBatchesError = 10210;
    private int maxChunkSize = DefaultChunkSize;
    private const string TTLHeader = "Nats-TTL";
    private const string ScheduleTargetHeader = "Nats-Schedule-Target";
    private const string ScheduledTargetTTL = "Nats-Schedule-TTL";
    private const string MessageIdHeader = "Nats-Msg-Id";
    private const string BatchIdHeader = "Nats-Batch-Id";
    private const string BatchSequenceHeader = "Nats-Batch-Sequence";
    private const string BatchCommitHeader = "Nats-Batch-Commit";
    private const string NatsCounterHeader = "Nats-Incr";

    private readonly bool allowsBatching = (serverVersion??new Version("0.0.0.0"))>=new Version("2.12");

    private void ReduceChunkSize(int attemptedSize)
    {
        var newSize = Math.Max(1, attemptedSize / 2);

        while (true)
        {
            var currentSize = Volatile.Read(ref maxChunkSize);

            if (currentSize <= newSize)
                return;

            if (Interlocked.CompareExchange(
                    ref maxChunkSize,
                    newSize,
                    currentSize) == currentSize)
                return;
        }
    }

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
    private static NatsHeaders AppendScheduleHeaders(NatsHeaders headers, string cronString, string destinationSubject, TimeSpan? timeout)
    {
        headers.Add(Constants.ScheduleDelayHeader, cronString);
        headers.Add(ScheduleTargetHeader, destinationSubject);
        if (timeout.HasValue)
            headers.Add(ScheduledTargetTTL, CreateTTLString(timeout.Value));
        return headers;
    }
    public ValueTask PublishMessageAsync(PublishMessage message, CancellationToken cancellationToken = default)
        => PublishMessageAsync(message.Subject, message.Data, AppendDefaultHeaders(message.Headers, message.Id, message.Timeout), cancellationToken);
    public ValueTask IncrementCounterAsync(string counterSubject, CancellationToken cancellationToken = default)
        => PublishMessageAsync(counterSubject, Array.Empty<byte>(), new NatsHeaders() { { NatsCounterHeader, "+1" } }, cancellationToken);
    public ValueTask DecrementCounterAsync(string counterSubject, CancellationToken cancellationToken = default)
        => PublishMessageAsync(counterSubject, Array.Empty<byte>(), new NatsHeaders() { { NatsCounterHeader, "-1" } }, cancellationToken);
    public ValueTask PublishScheduledMessageAsync(ScheduledPublishMessage message, CancellationToken cancellationToken = default)
        => PublishMessageAsync(message.Subject,
            message.Data,
            AppendScheduleHeaders(AppendDefaultHeaders(message.Headers, message.Id, message.Timeout), message.DelayString, message.DestinationSubject, message.Timeout),
            cancellationToken);

    private static TimeSpan GetBatchRetryDelay(int attempt)
    {
        var delay = Math.Min(
            2000,
            50 * Math.Pow(2, attempt - 1));

        var jitter = Random.Shared.Next(10, 100);

        return TimeSpan.FromMilliseconds(delay + jitter);
    }

    public async ValueTask PublishMessagesAsync(IEnumerable<PublishMessage> messages, CancellationToken cancellationToken = default)
    {
        var items = messages as PublishMessage[]
            ?? messages.ToArray();

        var offset = 0;
        var retries = 0;

        while (offset < items.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var count = Math.Min(
                Volatile.Read(ref maxChunkSize),
                items.Length - offset);

            try
            {
                await PublishMessagesChunkAsync(items.AsMemory(offset, count), cancellationToken);

                offset += count;
                retries = 0;
            }
            catch (NatsJSApiException ex) when (
                allowsBatching &&
                count > 1 &&
                retries < MaxBatchRetries &&
                ex.Error.ErrCode is
                    BatchTooLargeError or
                    TooManyBatchesError or
                    BatchIncompleteError)
            {
                Trace.WriteLine($"Atomic batching error {ex.Error.ErrCode}");

                retries++;

                if (ex.Error.ErrCode == BatchTooLargeError)
                {
                    ReduceChunkSize(count);
                }

                await Task.Delay(GetBatchRetryDelay(retries), cancellationToken);
            }
        }
    }

    private static NatsHeaders CloneHeaders(NatsHeaders source)
    {
        var result = new NatsHeaders();

        foreach (var header in source)
            result[header.Key] = header.Value;

        return result;
    }

    private async ValueTask BeginBatchAsync(string subject, byte[] data, NatsHeaders headers, CancellationToken cancellationToken)
    {
        var response = await connection.RequestAsync<byte[], byte[]>(
            subject,
            data,
            headers: headers,
            cancellationToken: cancellationToken);

        response.EnsureSuccess();

        // NATS.Net represents an empty payload as null.
        // Either representation indicates batch acceptance.
        if (response.Data is null or { Length: 0 })
        {
            TraceHelper.AddPublishEvent(subject);
            return;
        }

        var ack = JsonSerializer.Deserialize<PubAckResponse>(response.Data);

        if (ack?.Error is { } error)
            throw new NatsJSApiException(error);

        throw new InvalidOperationException(
            "Unexpected atomic batch acknowledgement.");
    }

    private async ValueTask PublishMessagesChunkAsync(ReadOnlyMemory<PublishMessage> messages, CancellationToken cancellationToken)
    {
        var total = messages.Length;

        var atomicBatch = allowsBatching && total > 1;

        var batchId = atomicBatch
            ? Guid.CreateVersion7().ToString()
            : null;

        for (var index = 0; index < total; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = messages.Span[index];

            var headers = AppendDefaultHeaders(CloneHeaders(message.Headers), message.Id, message.Timeout);

            if (message is ScheduledPublishMessage scheduled)
                headers = AppendScheduleHeaders(headers, scheduled.DelayString, scheduled.DestinationSubject, scheduled.Timeout);

            var isLast = index == total - 1;

            if (atomicBatch)
            {
                headers[BatchIdHeader] = batchId!;
                headers[BatchSequenceHeader] = (index + 1).ToString();

                if (isLast)
                    headers[BatchCommitHeader] = "1";
            }

            if (atomicBatch && index == 0)
                await BeginBatchAsync(message.Subject, message.Data, headers, cancellationToken);
            else
                await PublishMessageAsync(message.Subject, message.Data, headers, cancellationToken, ensureSuccess: !atomicBatch || isLast);
        }
    }

    private async ValueTask PublishMessageAsync(string subject, byte[] data, NatsHeaders headers, CancellationToken cancellationToken, bool ensureSuccess=true)
    {
        if (ensureSuccess)
        {
            var result = await jsContext.PublishAsync<byte[]>(subject, data, headers: headers, cancellationToken: cancellationToken);
            result.EnsureSuccess();
        }else
            await connection.PublishAsync<byte[]>(subject, data, headers:headers, cancellationToken: cancellationToken);
        TraceHelper.AddPublishEvent(subject);
    }
    public static string GetMessageID(NatsHeaders? headers)
        => (headers?.TryGetValue(MessageIdHeader, out var id)==true ? id.ToString() : string.Empty);
    public ValueTask<INatsJSConsumer> CreateOrUpdateConsumerAsync(
        string stream,
        ConsumerConfig config,
        CancellationToken cancellationToken = default)
        => jsContext.CreateOrUpdateConsumerAsync(stream, config, cancellationToken);

    internal ValueTask<StreamPurgeResponse> PurgeStreamAsync(string stream, StreamPurgeRequest request, CancellationToken cancellationToken)
        => jsContext.PurgeStreamAsync(stream, request, cancellationToken);

    internal ValueTask<bool> DeleteConsumerAsync(string streamName, string consumerName)
        => jsContext.DeleteConsumerAsync(streamName, consumerName);

    public ValueTask DisposeAsync()
        => connection.DisposeAsync();
}
