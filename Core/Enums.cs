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
    Config
}

internal enum ActivityEventTypes
{
    Start,
    Timer,
    Timeout
}