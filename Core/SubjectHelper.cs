using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetFlow;

internal static class SubjectHelper
{
    public const string WorkflowEventsStreamsName = "WORKFLOW_EVENTS";
    public static string WorkflowStart(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.start";
    public static string WorkflowEnd(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.end";
    public static string WorkflowStepStart(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.start";
    public static string WorkflowStepEnd(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.end";
    public static string WorkflowStepError(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.error";
    public static string WorkflowStepTimeout(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.timeout";

    public const string ActivityEventsStreamsName = "ACTIVITY_EVENTS";
    public static string ActivityStart(string activityName, string instance)
        => $"act.{activityName}.{instance}.start";
    public static string ActivityTimer(string activityName, string instance)
        => $"act.{activityName}.{instance}.timer";
}
