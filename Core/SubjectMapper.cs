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
        => $"{streamNamespace}JETFLOW_WORKFLOW_EVENTS";
    public string WorkflowConfigure(string workflowName, string instance)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.config";
    public string WorkflowStart(string workflowName, string instance)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.start";
    public string WorkflowEnd(string workflowName, string instance)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.end";
    public string WorkflowArchived(string workflowName, string instance)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.archived";
    public string WorkflowPurge(string workflowName, string instance)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.purge";
    public string WorkflowDelayStart(string workflowName, string instance)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.delaystart";
    public string WorkflowDelayEnd(string workflowName, string instance)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.delayend";
    public string WorkflowTimer(string workflowName, string instance)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.timer";
    public string WorkflowStepStart(string workflowName, string instance, string stepName)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.{stepName}.stepstart";
    public string WorkflowStepEnd(string workflowName, string instance, string stepName)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.{stepName}.stepend";
    public string WorkflowStepError(string workflowName, string instance, string stepName)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.{stepName}.steperror";
    public string WorkflowStepTimeout(string workflowName, string instance, string stepName)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.{stepName}.steptimeout";
    public string WorkflowStepRetry(string workflowName, string instance, string stepName)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.{stepName}.stepretry";
    public string WorkflowPurgeFilter(string workflowName, string instance)
        => $"{subjectNamespace}wf.{workflowName}.{instance}.>";

    public string ActivityQueueStream
        => $"{streamNamespace}JETFLOW_ACTIVITY_QUEUE";
    public string ActivityStart(string activityName, string workflowName, string instance)
        => $"{subjectNamespace}act.{activityName}.{workflowName}.{instance}.start";
    public string ActivityTimer(string activityName, string workflowName, string instance)
        => $"{subjectNamespace}act.{activityName}.{workflowName}.{instance}.timer";
    public string ActivityTimeout(string activityName, string workflowName, string instance)
        => $"{subjectNamespace}act.{activityName}.{workflowName}.{instance}.timeout";
    public string WorkflowActivityPurgeFilter(string workflowName, string instance)
        => $"{subjectNamespace}act.*.{workflowName}.{instance}.>";

    public string ActivityLocksKeystore
        => $"{streamNamespace}JETFLOW_ACTIVITY_LOCKS";

    public string WorkflowConfigKeystore
        => $"{streamNamespace}JETFLOW_WORKFLOW_CONFIGS";
    
    public string WorkflowArchiveKeystore
        => $"{streamNamespace}JETFLOW_WORKFLOW_ARCHIVES";
}
