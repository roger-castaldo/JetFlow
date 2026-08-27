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
    StepRetry,
    Archived,
    Config,
    Suspended,
    Resumed
}

internal enum ActivityEventTypes
{
    Start,
    Timer,
    Timeout
}