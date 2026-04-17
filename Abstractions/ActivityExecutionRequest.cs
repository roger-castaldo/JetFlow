namespace JetFlow;

public record ActivityRetryConfiguration(
    ushort MaximumAttempts = 0,
    TimeSpan? DelayBetween = null,
    bool RetryOnTimeout = true,
    bool RetryOnError = true,
    string[]? BlockedErrors = null
);

public record ActivityTimeoutConfiguration(
    TimeSpan? OverallTimeout = null,
    TimeSpan? AttemptTimeout = null
);

public record ActivityOptions()
{
    public ActivityTimeoutConfiguration? Timeouts { get; init; } = null;
    public ActivityRetryConfiguration? Retries { get; init; } = null;
};

public record ActivityExecutionRequest() : ActivityOptions;

public record ActivityExecutionRequest<TInput>(TInput? Input) : ActivityOptions;