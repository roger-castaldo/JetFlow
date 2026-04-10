namespace JetFlow.Helpers;

internal static class SubjectHelper
{
    public const string WorkflowEventsStreamsName = "JETFLOW_WORKFLOW_EVENTS";
    public static string WorkflowStart(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.start";
    public static string WorkflowEnd(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.end";
    public static string WorkflowDelayStart(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.delaystart";
    public static string WorkflowDelayEnd(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.delayend";
    public static string WorkflowDelayTimer(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.delaytimer";
    public static string WorkflowStepStart(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.stepstart";
    public static string WorkflowStepEnd(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.stepend";
    public static string WorkflowStepError(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.steperror";
    public static string WorkflowStepTimeout(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.steptimeout";

    public const string ActivityEventsStreamsName = "JETFLOW_ACTIVITY_EVENTS";
    public static string ActivityStart(string activityName, string workflowName, string instance)
        => $"act.{activityName}.{workflowName}.{instance}.start";
    public static string ActivityTimer(string activityName, string workflowName, string instance)
        => $"act.{activityName}.{workflowName}.{instance}.timer";
    public static string ActivityTimeout(string activityName, string workflowName, string instance)
        => $"act.{activityName}.{workflowName}.{instance}.timeout";
}
