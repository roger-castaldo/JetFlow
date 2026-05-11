namespace JetFlow;

internal class SubjectMapper
{
    private readonly string streamNamespace;
    private readonly string subjectNamespace;

    public SubjectMapper(string? instanceNamespace)
    {
        if (!string.IsNullOrWhiteSpace(instanceNamespace))
        {
            instanceNamespace = new string([.. instanceNamespace.Where(c => char.IsLetterOrDigit(c))]);
            if (instanceNamespace.Length>32)
                throw new ArgumentException("Namespace must be less than or equal to 32 characters after removing non-alphanumeric characters.", nameof(instanceNamespace));
        }
        else
            instanceNamespace=null;
        this.streamNamespace = (instanceNamespace==null ? "" : $"{instanceNamespace.ToUpper()}_");
        this.subjectNamespace = (instanceNamespace==null ? "" : $"{instanceNamespace.ToLower()}.");
    }

    public string WorkflowEventsStreamsName
        => $"JETFLOW_{streamNamespace}WORKFLOW_EVENTS";
    public string WorkflowEventsFilter
        => $"jetflow.{subjectNamespace}wkf.>";
    public string WorkflowConfigure(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.config";
    public string WorkflowStart(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.start";
    public string WorkflowEnd(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.end";
    public string WorkflowArchived(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.archived";
    public string WorkflowPurge(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.purge";
    public string WorkflowDelayStart(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.delaystart";
    public string WorkflowDelayEnd(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.delayend";
    public string WorkflowTimer(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.timer";
    public string WorkflowStepStart(string workflowName, string instance, string stepName)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.{stepName}.stepstart";
    public string WorkflowStepEnd(string workflowName, string instance, string stepName)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.{stepName}.stepend";
    public string WorkflowStepError(string workflowName, string instance, string stepName)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.{stepName}.steperror";
    public string WorkflowStepTimeout(string workflowName, string instance, string stepName)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.{stepName}.steptimeout";
    public string WorkflowStepRetry(string workflowName, string instance, string stepName)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.{stepName}.stepretry";
    public string WorkflowPurgeFilter(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.>";

    public string ActivityQueueStream
        => $"JETFLOW_{streamNamespace}ACTIVITY_QUEUE";
    public string ActivityEventsFilter
        => $"jetflow.{subjectNamespace}act.>";
    public string ActivityStart(string activityName, string workflowName, string workflowInstance)
        => $"jetflow.{subjectNamespace}act.{activityName}.{workflowName}.{workflowInstance}.start";
    public string ActivityTimer(string activityName, string workflowName, string workflowInstance)
        => $"jetflow.{subjectNamespace}act.{activityName}.{workflowName}.{workflowInstance}.timer";
    public string ActivityTimeout(string activityName, string workflowName, string workflowInstance)
        => $"jetflow.{subjectNamespace}act.{activityName}.{workflowName}.{workflowInstance}.timeout";
    public string WorkflowActivityPurgeFilter(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}act.*.{workflowName}.{instance}.>";

    public string ActivityLocksKeystore
        => $"JETFLOW_{streamNamespace}ACTIVITY_LOCKS";

    public string WorkflowConfigKeystore
        => $"JETFLOW_{streamNamespace}WORKFLOW_CONFIGS";
    
    public string WorkflowArchiveKeystore
        => $"JETFLOW_{streamNamespace}WORKFLOW_ARCHIVES";

    public string ScheduledWorkflowStreamsName
        => $"JETFLOW_{streamNamespace}SCHEDULED_WORKFLOWS";
    public string ScheduledWorkflowsFilter
        => $"jetflow.{subjectNamespace}swf.>";
    public string ScheduledWorkflowConfigure(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}swf.{workflowName}.{instance}.config";
    public string ScheduledWorkflowStart(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}swf.{workflowName}.{instance}.start";
    public string ScheduledWorkflowTimer(string workflowName, string instance)
        => $"jetflow.{subjectNamespace}swf.{workflowName}.{instance}.timer";
}
