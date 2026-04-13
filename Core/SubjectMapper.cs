namespace JetFlow;

internal class SubjectMapper(string? instanceNamespace)
{
    public string WorkflowEventsStreamsName
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace.ToUpper()}_")}JETFLOW_WORKFLOW_EVENTS";
    public string WorkflowConfigure(string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.config";
    public string WorkflowStart(string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.start";
    public string WorkflowEnd(string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.end";
    public string WorkflowDelayStart(string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.delaystart";
    public string WorkflowDelayEnd(string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.delayend";
    public string WorkflowDelayTimer(string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.delaytimer";
    public string WorkflowStepStart(string workflowName, string instance, string stepName)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.{stepName}.stepstart";
    public string WorkflowStepEnd(string workflowName, string instance, string stepName)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.{stepName}.stepend";
    public string WorkflowStepError(string workflowName, string instance, string stepName)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.{stepName}.steperror";
    public string WorkflowStepTimeout(string workflowName, string instance, string stepName)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}wf.{workflowName}.{instance}.{stepName}.steptimeout";

    public string ActivityQueueStream
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace.ToUpper()}_")}JETFLOW_ACTIVITY_QUEUE";
    public string ActivityStart(string activityName, string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}act.{activityName}.{workflowName}.{instance}.start";

    public string ActivityTimersStream
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace.ToUpper()}_")}JETFLOW_ACTIVITY_TIMERS";
    public string ActivityTimer(string activityName, string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}act.{activityName}.{workflowName}.{instance}.timer";
    public string ActivityTimeout(string activityName, string workflowName, string instance)
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace}.")}act.{activityName}.{workflowName}.{instance}.timeout";

    public string ActivityLocksKeystore
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace.ToUpper()}_")}JETFLOW_ACTIVITY_LOCKS";

    public string WorkflowConfigKeytore
        => $"{(instanceNamespace==null ? "" : $"{instanceNamespace.ToUpper()}_")}JETFLOW_WORKFLOW_CONFIGS";
}
