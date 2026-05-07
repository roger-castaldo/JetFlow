using NATS.Client.Core;
using System.Diagnostics;

namespace JetFlow.Helpers;

internal static class TraceHelper
{
    public const string WorkflowTraceHeaderKey = "x-jetflow-workflow-traceParentId";
    public const string WorkflowTraceSpanHeaderKey = "x-jetflow-workflow-traceParentSpanId";
    private const string WorkflowStepTraceParentHeaderKey = "x-jetflow-workflowstep-traceParentId";
    private const string WorkflowStepTraceParentSpanHeaderKey = "x-jetflow-workflowstep-traceParentSpanId";

    private static readonly ActivitySource activitySource = new(Connection.TraceProviderName);

    public static void AddActivityTimeout(TimeSpan timeout)
        => Activity.Current?.AddTag(TraceConstants.ActivityTimeoutIdTag, timeout);

    public static Activity? StartWorkflow(string name, string id)
        => activitySource.StartActivity(TraceConstants.WorkflowStart, ActivityKind.Internal, default(ActivityContext), new[] { new KeyValuePair<string, object?>(TraceConstants.WorkflowNameTag, name), new KeyValuePair<string, object?>(TraceConstants.WorkflowIdTag, id) }, null);
    public static Activity? StartWorkflowStep(EventMessage message, string activityName, string id)
        => activitySource.StartActivity(TraceConstants.WorkflowStepStart, ActivityKind.Producer, default(ActivityContext), ExtractWorkflowTags(message).Concat([
                new(TraceConstants.ActivityNameTag, activityName),
                new(TraceConstants.ActivityIdTag, id)
            ]), ExtractWorkflowLink(message));
    public static Activity? StartDelay(EventMessage message)
        => activitySource.StartActivity(TraceConstants.WorkflowDelayStart, ActivityKind.Producer, default(ActivityContext), ExtractWorkflowTags(message), ExtractWorkflowLink(message));
    public static Activity? StartActivity(EventMessage message)
        => activitySource.StartActivity(TraceConstants.WorkflowActivityStart, ActivityKind.Consumer, default(ActivityContext), ExtractWorkflowActivityTags(message), ExtractWorkflowActivityLink(message));

    public static NatsHeaders InjectCurrentActivity(NatsHeaders headers)
        => (Activity.Current?.OperationName) switch
        {
            TraceConstants.WorkflowStart => new(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>([
                new(WorkflowTraceHeaderKey,Activity.Current.TraceId.ToString()), 
                new(WorkflowTraceSpanHeaderKey, Activity.Current.SpanId.ToString()),
                ..headers??[]])),
            TraceConstants.WorkflowStepStart => new(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>([
                new(WorkflowStepTraceParentHeaderKey,Activity.Current.TraceId.ToString()),
                new(WorkflowStepTraceParentSpanHeaderKey, Activity.Current.SpanId.ToString()),
                ..headers??[]])),
            _ => headers
        };

    public static void AddPublishEvent(string subject)
        => Activity.Current?.AddEvent(new(TraceConstants.MessagePublished, tags: new([new(TraceConstants.MessageSubjectTag, subject)])));

    public static void AddMessageDecodedEvent(string contentHeader)
        => Activity.Current?.AddEvent(new(TraceConstants.MessageDecoded, tags: new([new(TraceConstants.MessageContentHeaderTag, contentHeader)])));

    private static IEnumerable<ActivityLink>? ExtractWorkflowLink(EventMessage message)
    {
        if (message.Message.Headers!=null
            && message.Message.Headers.TryGetValue(WorkflowTraceHeaderKey, out var traceKey)
            && message.Message.Headers.TryGetValue(WorkflowTraceSpanHeaderKey, out var spanKey))
            return [new(new(ActivityTraceId.CreateFromString(traceKey.ToString()), ActivitySpanId.CreateFromString(spanKey.ToString()), ActivityTraceFlags.Recorded, isRemote: true))];
        return null;
    }

    private static IEnumerable<ActivityLink>? ExtractWorkflowActivityLink(EventMessage message)
    {
        if (message.Message.Headers!=null
            && message.Message.Headers.TryGetValue(WorkflowStepTraceParentHeaderKey, out var traceKey)
            && message.Message.Headers.TryGetValue(WorkflowStepTraceParentSpanHeaderKey, out var spanKey))
            return [new(new(ActivityTraceId.CreateFromString(traceKey.ToString()), ActivitySpanId.CreateFromString(spanKey.ToString()), ActivityTraceFlags.Recorded, isRemote: true))];
        return null;
    }

    private static IEnumerable<KeyValuePair<string, object?>> ExtractWorkflowTags(EventMessage message)
        => [
            new(TraceConstants.WorkflowNameTag, message.WorkflowName),
            new(TraceConstants.WorkflowIdTag, message.WorkflowId)
        ];

    private static IEnumerable<KeyValuePair<string, object?>> ExtractWorkflowActivityTags(EventMessage message)
        => [ .. ExtractWorkflowTags(message),
            new(TraceConstants.ActivityNameTag, message.ActivityName),
            new(TraceConstants.ActivityIdTag, message.ActivityID)
        ];

    
}
