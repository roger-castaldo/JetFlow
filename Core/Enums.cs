namespace JetFlow;

internal enum ActivityRunStatus
{
    Acquired, 
    AlreadyCompleted,
    CurrentlyRunning
}

internal enum WorkflowExitType
{
    Activity,
    ChildWorkflow,
    Delay,
    ExternalEvent,
    Complete
}
