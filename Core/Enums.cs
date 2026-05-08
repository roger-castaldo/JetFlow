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
    StepRetry,
    Archived,
    Purge,
    Config
}

internal enum ActivityEventTypes
{
    Start,
    Timer,
    Timeout
}