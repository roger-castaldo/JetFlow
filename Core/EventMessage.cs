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
    private static readonly Regex workflowSubjectRegex = new(@"^(?<namespace>[^.]+\.)?wf\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)(?:\.(?<stepName>[^.]+))?\.(?<eventType>start|end|delaystart|delayend|timer|archived|purge|stepstart|stepend|steperror|steptimeout)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));
    private static readonly Regex activitySubjectRegex = new(@"^(?<namespace>[^.]+\.)?act\.(?<activityName>[^.]+)\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)\.(?<eventType>start|timer|timeout)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    public EventMessage(INatsJSMsg<byte[]> msg)
    {
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
            ActivityID = ServiceConnection.GetActivityID(msg);
        }
        Namespace = match.Groups["namespace"].Success ? match.Groups["namespace"].Value : null;
        WorkflowName = match.Groups["workflowName"].Value;
        WorkflowId = match.Groups["instance"].Value;
        Message=msg;
    }

    public string? Namespace { get; private init; }
    public string WorkflowName { get; private init; }
    public string WorkflowId { get; private init; }
    public WorkflowEventTypes? WorkflowEventType { get; private init; } = null;
    public string? ActivityName { get; private init; }
    public ActivityEventTypes? ActivityEventType { get; private init; } = null;
    public Guid? ActivityID { get; private init; } = null;
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
