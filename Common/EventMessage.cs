using JetFlow.Helpers;
using NATS.Client.Core;
using System.Text.RegularExpressions;
using NATS.Client.JetStream;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using NATS.Client.ObjectStore;

namespace JetFlow;

internal record EventMessage
{
    private static readonly string[] SharedHeaders = [
        Constants.WorkflowTraceHeaderKey,
        Constants.WorkflowTraceSpanHeaderKey
    ];
    private static readonly Regex workflowSubjectRegex = new(@"^jetflow\.(?<namespace>[^.]+\.)?(wkf|swf)\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)(?:\.(?<stepName>[^.]+))?\.(?<eventType>start|end|delaystart|delayend|timer|archived|config|stepstart|stepend|stepretry|suspended|resumed)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));
    private static readonly Regex activitySubjectRegex = new(@"^jetflow\.(?<namespace>[^.]+\.)?act\.(?<activityName>[^.]+)\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)\.(?<activityInstance>[^.]+)\.(?<eventType>start|timer|timeout)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));
    private static readonly Regex purgeWorkflowSubjectRegex = new(@"^jetflow\.(?<namespace>[^.]+\.)?purge\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    public static (string workflowName, string instance) ExtractWorkflowFromSubject(string subject)
    {
        var match = workflowSubjectRegex.Match(subject);
        if (match.Success)
            return (match.Groups["workflowName"].Value, match.Groups["instance"].Value);
        throw new ArgumentException(nameof(subject));
    }

    public static async ValueTask<EventMessage> CreateMessageAsync(INatsObjStore largeMessageStore, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
        => new(
            msg.Subject, 
            msg.Headers, 
            await MessagesHelper.RetrieveMessageDataAsync(largeMessageStore, msg.Data, cancellationToken), 
            msg.Metadata,
            async(token)=>await msg.AckAsync(cancellationToken: token),
            async (token) => await msg.NakAsync(cancellationToken: token)
        );

    private readonly Func<CancellationToken, ValueTask> ack;
    private readonly Func<CancellationToken, ValueTask> nak;

    private static T? ExtractHeader<T>(NatsHeaders? headers, string headerKey, Func<string, T?> converter)
        where T : struct
    {
        if (headers?.TryGetValue(headerKey, out var value)??false)
            return converter(value.ToString());
        return null;
    }

    private static string[]? ExtractHeaders(NatsHeaders? headers, string headerKey, Func<StringValues, string[]> converter)
    {
        if (headers?.TryGetValue(headerKey, out var value)??false)
            return converter(value);
        return null;
    }

    private EventMessage(string subject, NatsHeaders? headers, byte[]? data, NatsJSMsgMetadata? metadata, Func<CancellationToken,ValueTask> ack, Func<CancellationToken,ValueTask> nak)
    {
        RecievedTimestamp = DateTimeOffset.Now;
        var match = workflowSubjectRegex.Match(subject);
        if (match.Success)
        {
            WorkflowEventType = Enum.Parse<WorkflowEventTypes>(match.Groups["eventType"].Value, true);
            ActivityName = match.Groups["stepName"].Success ? match.Groups["stepName"].Value : null;       
        }
        else
        {
            match = activitySubjectRegex.Match(subject);
            if (match.Success)
            {

                ActivityName = match.Groups["activityName"].Value;
                ActivityEventType = Enum.Parse<ActivityEventTypes>(match.Groups["eventType"].Value, true);
                ActivityInstanceID = match.Groups["activityInstance"].Value;
                ActivityTimeout = ExtractHeader<TimeSpan>(headers, Constants.ActivityTimeoutHeader, value => (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var timeSpan) ? timeSpan : (TimeSpan?)null));
                ActivityAttempt = ExtractHeader<ushort>(headers, Constants.ActivityAttemptHeader, value => ushort.TryParse(value, out var attempt) ? attempt : (ushort)0)??0;
                if (headers?.TryGetValue(Constants.ActivityMaximumAttemptsHeader, out _)??false)
                    RetryConfiguration = new(
                        ExtractHeader<ushort>(headers, Constants.ActivityMaximumAttemptsHeader, value => ushort.TryParse(value, out var maxAttempt) ? maxAttempt : (ushort)0)??0,
                        ExtractHeader<TimeSpan>(headers, Constants.ActiviyRetryDelayBetweenHeader, value => (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var timeSpan) ? timeSpan : (TimeSpan?)null)),
                        ExtractHeader<bool>(headers, Constants.ActivityRetryOnTimeoutHeader, value => !bool.TryParse(value, out var retryOnTimeout)||retryOnTimeout)??false,
                        ExtractHeader<bool>(headers, Constants.ActivityRetryOnErrorHeader, value => !bool.TryParse(value, out var retryOnError)||retryOnError)??false,
                        ExtractHeaders(headers, Constants.ActivityRetryBlockedErrorsHeader, value => [.. value.Where(s => !string.IsNullOrWhiteSpace(s)).OfType<string>()])
                    );
            }
            else
            {
                match = purgeWorkflowSubjectRegex.Match(subject);
                if (!match.Success)
                    throw new ArgumentException($"Invalid subject format: {subject}", nameof(subject));
            }
        }
        ActivityID = ExtractHeader<uint>(headers, Constants.ActivityIDHeader, value => uint.Parse(value));
        WorkflowStepResultStatus = ExtractHeader<ActivityResultStatus>(headers, Constants.ActivityResultHeader, value => Enum.Parse<ActivityResultStatus>(value, true));
        ParallelActivityIndex = ExtractHeader<uint>(headers, Constants.ParalellActivityIndexHeader, value => uint.Parse(value));
        ParallelActivityCount = ExtractHeader<uint>(headers, Constants.ParallelActivityCountHeader, value => uint.Parse(value));
        Namespace = match.Groups["namespace"].Success ? match.Groups["namespace"].Value : null;
        WorkflowName = match.Groups["workflowName"].Value;
        WorkflowId = match.Groups["instance"].Value;
        Subject = subject;
        Data = data?? [];
        Headers = headers;
        Metadata = metadata;
        this.ack = ack;
        this.nak = nak;
    }

    public DateTimeOffset RecievedTimestamp { get; private init; }
    public string? Namespace { get; private init; }
    public string WorkflowName { get; private init; }
    public string WorkflowId { get; private init; }
    public WorkflowEventTypes? WorkflowEventType { get; private init; } = null;
    public ActivityResultStatus? WorkflowStepResultStatus { get; private init; } = null;
    public uint? ParallelActivityIndex { get; private init; } = null;
    public uint? ParallelActivityCount { get; private init; } = null;
    public string? ActivityName { get; private init; }
    public ActivityEventTypes? ActivityEventType { get; private init; } = null;
    public uint? ActivityID { get; private init; } = null;
    public string? ActivityInstanceID { get; private init; } = null;
    public TimeSpan? ActivityTimeout { get; private init; } = null;
    public ushort ActivityAttempt { get; private init; } = 0;
    public ActivityRetryConfiguration? RetryConfiguration { get; private init; } = null;
    public string Subject { get; private init; }
    public byte[] Data { get; private init; }
    public NatsJSMsgMetadata? Metadata { get; private init; }
    public NatsHeaders? Headers { get; private init; }
    public NatsHeaders InjectHeaders(NatsHeaders? headers)
    {
        var result = new NatsHeaders(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(
            Headers==null ? [] : Headers.Where(pair=>SharedHeaders.Contains(pair.Key))
        ));
        if (headers!=null)
        {
            foreach(var pair in headers)
            {
                if (!result.ContainsKey(pair.Key))
                    result.Add(pair.Key, pair.Value);
            }
        }
        return result;
    }
    public ValueTask AckAsync(CancellationToken cancellationToken = default) => ack(cancellationToken);
    public ValueTask NakAsync(CancellationToken cancellationToken = default) => nak(cancellationToken);
}
