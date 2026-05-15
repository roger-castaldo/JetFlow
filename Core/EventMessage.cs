using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using System.Text.RegularExpressions;

namespace JetFlow;

internal record EventMessage
{
    private static readonly string[] SharedHeaders = [
        TraceHelper.WorkflowTraceHeaderKey,
        TraceHelper.WorkflowTraceSpanHeaderKey
    ];
    private static readonly Regex workflowSubjectRegex = new(@"^(?<namespace>[^.]+\.)?(wkf|swf)\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)(?:\.(?<stepName>[^.]+))?\.(?<eventType>start|end|delaystart|delayend|timer|archived|purge|config|stepstart|stepend|steperror|steptimeout|stepretry)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));
    private static readonly Regex activitySubjectRegex = new(@"^(?<namespace>[^.]+\.)?act\.(?<activityName>[^.]+)\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)\.(?<eventType>start|timer|timeout)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    public EventMessage(INatsJSMsg<byte[]> msg)
    {
        RecievedTimestamp = DateTimeOffset.Now;
        var match = workflowSubjectRegex.Match(msg.Subject);
        if (match.Success)
        {
            WorkflowEventType = Enum.Parse<WorkflowEventTypes>(match.Groups["eventType"].Value, true);
            ActivityName = match.Groups["stepName"].Success ? match.Groups["stepName"].Value : null;       
        }
        else
        {
            match = activitySubjectRegex.Match(msg.Subject);
            if (!match.Success)
                throw new ArgumentException($"Invalid event subject {msg.Subject}");
            ActivityName = match.Groups["activityName"].Value;
            ActivityEventType = Enum.Parse<ActivityEventTypes>(match.Groups["eventType"].Value, true);
            if (msg.Headers!=null)
            {
                if (msg.Headers.TryGetValue(Constants.ActivityTimeoutHeader, out var timeoutValue) && TimeSpan.TryParse(timeoutValue, out var timeSpan))
                    ActivityTimeout = timeSpan;
                if (msg.Headers.TryGetValue(Constants.ActivityAttemptHeader, out var attemptValue) && ushort.TryParse(attemptValue, out var attempt))
                    ActivityAttempt = attempt;
                if (msg.Headers.TryGetValue(Constants.ActivityMaximumAttemptsHeader, out var maxAttemptValue) && ushort.TryParse(maxAttemptValue, out var maxAttempt)) 
                    RetryConfiguration = new(
                        maxAttempt,
                        msg.Headers.TryGetValue(Constants.ActiviyRetryDelayBetweenHeader, out var delayValue) && TimeSpan.TryParse(delayValue, out var delay) ? delay : (TimeSpan?)null,
                        msg.Headers.TryGetValue(Constants.ActivityRetryOnTimeoutHeader, out var retryOnTimeoutValue) && bool.TryParse(retryOnTimeoutValue, out var retryOnTimeout) ? retryOnTimeout : true,
                        msg.Headers.TryGetValue(Constants.ActivityRetryOnErrorHeader, out var retryOnErrorValue) && bool.TryParse(retryOnErrorValue, out var retryOnError) ? retryOnError : true,
                        msg.Headers.TryGetValue(Constants.ActivityRetryBlockedErrorsHeader, out var blockedErrorsValue) ? [..blockedErrorsValue.ToArray().Where(s => !string.IsNullOrWhiteSpace(s)).OfType<string>()] : null
                    );
            }
        }
        if (msg.Headers?.TryGetValue(Constants.ActivityIDHeader, out var activityId)??false)
            ActivityID = uint.Parse(activityId.ToString());
        Namespace = match.Groups["namespace"].Success ? match.Groups["namespace"].Value : null;
        WorkflowName = match.Groups["workflowName"].Value;
        WorkflowId = match.Groups["instance"].Value;
        Message=msg;
    }

    public DateTimeOffset RecievedTimestamp { get; private init; }
    public string? Namespace { get; private init; }
    public string WorkflowName { get; private init; }
    public string WorkflowId { get; private init; }
    public WorkflowEventTypes? WorkflowEventType { get; private init; } = null;
    public string? ActivityName { get; private init; }
    public ActivityEventTypes? ActivityEventType { get; private init; } = null;
    public uint? ActivityID { get; private init; } = null;
    public TimeSpan? ActivityTimeout { get; private init; } = null;
    public ushort ActivityAttempt { get; private init; } = 0;
    public ActivityRetryConfiguration? RetryConfiguration { get; private init; } = null;
    public INatsJSMsg<byte[]> Message { get; private init; }
    public NatsHeaders InjectHeaders(NatsHeaders? headers)
    {
        var result = new NatsHeaders(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(
            Message.Headers==null ? [] : Message.Headers.Where(pair=>SharedHeaders.Contains(pair.Key))
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
}
