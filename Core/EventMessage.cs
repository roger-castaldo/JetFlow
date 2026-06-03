using JetFlow.Helpers;
using NATS.Client.Core;
using System.Text.RegularExpressions;
using NATS.Client.JetStream;

namespace JetFlow;

internal record EventMessage
{
    private static readonly string[] SharedHeaders = [
        TraceHelper.WorkflowTraceHeaderKey,
        TraceHelper.WorkflowTraceSpanHeaderKey
    ];
    private static readonly Regex workflowSubjectRegex = new(@"^(?<namespace>[^.]+\.)?(wkf|swf)\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)(?:\.(?<stepName>[^.]+))?\.(?<eventType>start|end|delaystart|delayend|timer|archived|purge|config|stepstart|stepend|stepretry)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));
    private static readonly Regex activitySubjectRegex = new(@"^(?<namespace>[^.]+\.)?act\.(?<activityName>[^.]+)\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)\.(?<activityInstance>[^.]+)\.(?<eventType>start|timer|timeout)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    public static async ValueTask<EventMessage> CreateMessageAsync(ServiceConnection connection, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
        => new(
            msg.Subject, 
            msg.Headers, 
            await connection.RetrieveMessageDataAsync(msg.Data, cancellationToken), 
            msg.Metadata,
            async(token)=>await msg.AckAsync(cancellationToken: token),
            async (token) => await msg.NakAsync(cancellationToken: token)
        );

    private readonly Func<CancellationToken, ValueTask> ack;
    private readonly Func<CancellationToken, ValueTask> nak;

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
            if (!match.Success)
                throw new ArgumentException($"Invalid event subject {subject}");
            ActivityName = match.Groups["activityName"].Value;
            ActivityEventType = Enum.Parse<ActivityEventTypes>(match.Groups["eventType"].Value, true);
            ActivityInstanceID = match.Groups["activityInstance"].Value;
            if (headers!=null)
            {
                if (headers.TryGetValue(Constants.ActivityTimeoutHeader, out var timeoutValue) && TimeSpan.TryParse(timeoutValue, out var timeSpan))
                    ActivityTimeout = timeSpan;
                if (headers.TryGetValue(Constants.ActivityAttemptHeader, out var attemptValue) && ushort.TryParse(attemptValue, out var attempt))
                    ActivityAttempt = attempt;
                if (headers.TryGetValue(Constants.ActivityMaximumAttemptsHeader, out var maxAttemptValue) && ushort.TryParse(maxAttemptValue, out var maxAttempt)) 
                    RetryConfiguration = new(
                        maxAttempt,
                        headers.TryGetValue(Constants.ActiviyRetryDelayBetweenHeader, out var delayValue) && TimeSpan.TryParse(delayValue, out var delay) ? delay : (TimeSpan?)null,
                        headers.TryGetValue(Constants.ActivityRetryOnTimeoutHeader, out var retryOnTimeoutValue) && bool.TryParse(retryOnTimeoutValue, out var retryOnTimeout) ? retryOnTimeout : true,
                        headers.TryGetValue(Constants.ActivityRetryOnErrorHeader, out var retryOnErrorValue) && bool.TryParse(retryOnErrorValue, out var retryOnError) ? retryOnError : true,
                        headers.TryGetValue(Constants.ActivityRetryBlockedErrorsHeader, out var blockedErrorsValue) ? [..blockedErrorsValue.ToArray().Where(s => !string.IsNullOrWhiteSpace(s)).OfType<string>()] : null
                    );
            }
        }
        if (headers?.TryGetValue(Constants.ActivityIDHeader, out var activityId)??false)
            ActivityID = uint.Parse(activityId.ToString());
        if (headers?.TryGetValue(Constants.ActivityResultHeader, out var resultValue)??false)
            WorkflowStepResultStatus = Enum.Parse<ActivityResultStatus>(resultValue.ToString(), true);
        if (headers?.TryGetValue(Constants.ParalellActivityIndexHeader, out var parallelIndexValue)??false)
            ParallelActivityIndex = uint.Parse(parallelIndexValue.ToString());
        if (headers?.TryGetValue(Constants.ParallelActivityCountHeader, out var parallelCountValue)??false)
            ParallelActivityCount = uint.Parse(parallelCountValue.ToString());
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
