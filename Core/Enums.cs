namespace JetFlow;

internal enum WorkflowEventTypes 
{
    Start,
    End,
    DelayStart,
    DelayEnd,
    DelayTimer,
    StepStart,
    StepEnd,
    StepError,
    StepTimeout
}

internal enum ActivityEventTypes
{
    Start,
    Timer,
    Timeout
}