using NATS.Client.Core;
using NATS.Client.JetStream;

namespace JetFlow.Helpers;

internal static class ConnectionHelper
{
    private const string TTLHeader = "Nats-TTL";
    private const string ScheduleDelayHeader = "Nats-Schedule";
    private const string ScheduleTargetHeader = "Nats-Schedule-Target";
    private const string ScheduledTargetTTL = "Nats-Schedule-TTL";
    private const string MessageIdHeader = "Nats-Msg-Id";
    private const string ActivityIdHeader = "JetFlow-Activity-Id";

    public static async ValueTask PublishMessageAsync(INatsConnection connection, byte[] data, MessageInfo messageInfo, CancellationToken cancellationToken)
        => await connection.PublishAsync<byte[]>(messageInfo.Subject, data, TraceHelper.InjectCurrentActivity(messageInfo.Headers), cancellationToken: cancellationToken);
    public static async ValueTask PublishMessageAsync(INatsJSContext connection, byte[] data, MessageInfo messageInfo, CancellationToken cancellationToken)
        => await connection.PublishAsync<byte[]>(messageInfo.Subject, data, headers: TraceHelper.InjectCurrentActivity(messageInfo.Headers), cancellationToken: cancellationToken);

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

    internal static void AddTTL(NatsHeaders natsHeaders, TimeSpan timeout)
        => natsHeaders.Add(TTLHeader, CreateTTLString(timeout));

    internal static void ScheduleDelayedSend(NatsHeaders natsHeaders, TimeSpan delay, string destinationSubject)
    {
        natsHeaders.Add(ScheduleDelayHeader, DateTime.UtcNow.Add(delay).ToString("'@at 'yyyy-MM-dd'T'HH:mm:ss'Z'"));
        natsHeaders.Add(ScheduleTargetHeader, destinationSubject);
    }

    internal static void AddMessageIds(NatsHeaders natsHeaders, string messageId, Guid? activityId = null)
    {
        natsHeaders.Add(MessageIdHeader, messageId);
        if (activityId!=null)
            natsHeaders.Add(ActivityIdHeader, activityId.ToString());
    }

    public static Guid GetActivityID(INatsJSMsg<byte[]> msg)
        => (msg.Headers?.TryGetValue(ActivityIdHeader, out var id) == true && Guid.TryParse(id, out var guid)) ? guid : Guid.Empty;

    internal static string GetMessageID(INatsJSMsg<byte[]> msg)
        => (msg.Headers?.TryGetValue(MessageIdHeader, out var id)==true ? id.ToString() : string.Empty);
}
