using NATS.Client.Core;
using NATS.Client.JetStream;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JetFlow.Helpers;

internal static class TraceHelper
{
    public const string WorkflowTraceHeaderKey = "jetflow-workflow-traceParentId";
    public const string WorkflowTraceSpanHeaderKey = "jetflow-workflow-traceParentSpanId";
    private const string WorkflowStepTraceParentHeaderKey = "jetflow-workflowstep-traceParentId";
    private const string WorkflowStepTraceParentSpanHeaderKey = "jetflow-workflowstep-traceParentSpanId";

    private const string BaseTag = "jetflow";
    private const string BaseWorkflowTag = $"{BaseTag}.workflow";
    private const string WorkflowNameTag = $"{BaseWorkflowTag}.name";
    private const string WorkflowIdTag = $"{BaseWorkflowTag}.id";
    private const string BaseActivityTag = $"{BaseTag}.workflowactivity";
    private const string ActivityNameTag = $"{BaseActivityTag}.name";
    private const string ActivityIdTag = $"{BaseActivityTag}.id";

    private const string WorkflowStepStartActivityName = "WorkflowStepStart";
    private const string WorkflowStartName = "WorkflowStart";

    private static readonly ActivitySource activitySource = new(Connection.TraceProviderName);

    public static void AddActivityTimeout(TimeSpan timeout)
        => Activity.Current?.AddTag($"{BaseActivityTag}.timeout", timeout);

    public static Activity? StartWorkflow(string name, string id)
        => activitySource.StartActivity(WorkflowStartName, ActivityKind.Internal, default(ActivityContext), new[] { new KeyValuePair<string, object?>(WorkflowNameTag, name), new KeyValuePair<string, object?>(WorkflowIdTag, id) }, null);
    public static Activity? StartWorkflowStep(EventMessage message, string activityName, string id)
        => activitySource.StartActivity(WorkflowStepStartActivityName, ActivityKind.Producer, default(ActivityContext), ExtractWorkflowTags(message), ExtractWorkflowLink(message));
    public static Activity? StartDelay(EventMessage message)
        => activitySource.StartActivity("WorkflowDelayStart", ActivityKind.Producer, default(ActivityContext), ExtractWorkflowTags(message), ExtractWorkflowLink(message));
    public static Activity? StartActivity(EventMessage message)
        => activitySource.StartActivity("WorkflowActivityStart", ActivityKind.Consumer, default(ActivityContext), ExtractWorkflowActivityTags(message), ExtractWorkflowActivityLink(message));

    public static NatsHeaders? InjectCurrentActivity(NatsHeaders? headers)
        => (Activity.Current?.OperationName) switch
        {
            WorkflowStartName => new(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>([
                new(WorkflowTraceHeaderKey,Activity.Current.TraceId.ToString()), 
                new(WorkflowTraceSpanHeaderKey, Activity.Current.SpanId.ToString()),
                ..headers??[]])),
            WorkflowStepStartActivityName => new(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>([
                new(WorkflowStepTraceParentHeaderKey,Activity.Current.TraceId.ToString()),
                new(WorkflowStepTraceParentSpanHeaderKey, Activity.Current.SpanId.ToString()),
                ..headers??[]])),
            _ => headers
        };

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
            new(WorkflowNameTag, message.WorkflowName),
            new(WorkflowIdTag, message.WorkflowId)
        ];

    private static IEnumerable<KeyValuePair<string, object?>> ExtractWorkflowActivityTags(EventMessage message)
        => [ .. ExtractWorkflowTags(message),
            new(ActivityNameTag, message.ActivityName),
            new(ActivityIdTag, message.ActivityID)
        ];
}
