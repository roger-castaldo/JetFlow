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
    Purge,
    Config
}

internal enum ActivityEventTypes
{
    Start,
    Timer,
    Timeout
}

internal enum RetryTypes
{
    Timeout,
    Error
}