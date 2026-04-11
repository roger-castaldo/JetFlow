using Microsoft.Extensions.Options;

namespace JetFlow.Helpers;

internal static class SubjectHelper
{
    public static string WorkflowEventsStreamsName(string? instanceNamespace)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace.ToUpper()}_")}JETFLOW_WORKFLOW_EVENTS";
    public static string WorkflowStart(string? instanceNamespace, string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.start";
    public static string WorkflowEnd(string? instanceNamespace, string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.end";
    public static string WorkflowDelayStart(string? instanceNamespace, string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.delaystart";
    public static string WorkflowDelayEnd(string? instanceNamespace, string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.delayend";
    public static string WorkflowDelayTimer(string? instanceNamespace, string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.delaytimer";
    public static string WorkflowStepStart(string? instanceNamespace, string workflowName, string instance, string stepName)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.{stepName}.stepstart";
    public static string WorkflowStepEnd(string? instanceNamespace, string workflowName, string instance, string stepName)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.{stepName}.stepend";
    public static string WorkflowStepError(string? instanceNamespace, string workflowName, string instance, string stepName)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.{stepName}.steperror";
    public static string WorkflowStepTimeout(string? instanceNamespace, string workflowName, string instance, string stepName)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.{stepName}.steptimeout";

    public static string ActivityQueueStream(string? instanceNamespace)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace.ToUpper()}_")}JETFLOW_ACTIVITY_QUEUE";
    public static string ActivityStart(string? instanceNamespace, string activityName, string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}act.{activityName}.{workflowName}.{instance}.start";

    public static string ActivityTimersStream(string? instanceNamespace)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace.ToUpper()}_")}JETFLOW_ACTIVITY_TIMERS";
    public static string ActivityTimer(string? instanceNamespace, string activityName, string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}act.{activityName}.{workflowName}.{instance}.timer";
    public static string ActivityTimeout(string? instanceNamespace, string activityName, string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}act.{activityName}.{workflowName}.{instance}.timeout";

    public static string ActivityLocksKeystore(string? instanceNamespace)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace.ToUpper()}_")}JETFLOW_ACTIVITY_LOCKS";

    public static string WorkflowConfigKeytore(string? instanceNamespace)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace.ToUpper()}_")}JETFLOW_WORKFLOW_CONFIGS";
}
