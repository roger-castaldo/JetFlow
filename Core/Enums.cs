namespace JetFlow;

internal enum WorkflowEventTypes 
{
    Start,
    End,
    DelayStart,
    DelayEnd,
    Timer,
    StepStart,
    StepEnd,
    StepError,
    StepTimeout,
    Archived,
    Purge
}

internal enum ActivityEventTypes
{
    Start,
    Timer,
    Timeout
}