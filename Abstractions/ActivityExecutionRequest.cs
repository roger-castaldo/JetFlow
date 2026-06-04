namespace JetFlow;

/// <summary>
/// Houses the Retry configuration for an activity execution. This includes the maximum number of attempts, the delay between attempts, and which errors to retry on or block on.
/// </summary>
/// <param name="MaximumAttempts">The maximum number of retry attempts.</param>
/// <param name="DelayBetween">The delay between retry attempts.</param>
/// <param name="RetryOnTimeout">Indicates whether to retry on timeout.</param>
/// <param name="RetryOnError">Indicates whether to retry on error.</param>
/// <param name="BlockedErrors">A list of errors that should block retries.</param>
public record ActivityRetryConfiguration(
    ushort MaximumAttempts = 0,
    TimeSpan? DelayBetween = null,
    bool RetryOnTimeout = true,
    bool RetryOnError = true,
    string[]? BlockedErrors = null
);

/// <summary>
/// Represents the timeout configuration for an activity, including overall and per-attempt timeouts.
/// </summary>
/// <param name="OverallTimeout">The maximum duration allowed for the entire activity to complete. Specify <see langword="null"/> to indicate no
/// overall timeout.</param>
/// <param name="AttemptTimeout">The maximum duration allowed for a single attempt of the activity. Specify <see langword="null"/> to indicate no
/// per-attempt timeout.</param>
public record ActivityTimeoutConfiguration(
    TimeSpan? OverallTimeout = null,
    TimeSpan? AttemptTimeout = null
);

/// <summary>
/// Represents a request to execute an activity, including optional timeout and retry configurations.
/// </summary>
/// <remarks>Use this type to specify execution parameters for an activity, such as custom timeouts or retry
/// behavior. Both configuration properties are optional; if not set, default execution settings will be used.</remarks>
public record ActivityExecutionRequest()
{
    /// <summary>
    /// Gets the timeout configuration for activity execution.
    /// </summary>
    /// <remarks>Use this property to specify custom timeout settings for activities. If not set, default
    /// timeouts may apply.</remarks>
    public ActivityTimeoutConfiguration? Timeouts { get; init; } = null;
    /// <summary>
    /// Gets the retry configuration to use for the activity, or null if no retries are configured.
    /// </summary>
    public ActivityRetryConfiguration? Retries { get; init; } = null;
};

/// <summary>
/// Represents a request to execute an activity with a strongly typed input parameter.
/// </summary>
/// <typeparam name="TInput">The type of the input parameter provided to the activity. Can be any serializable type appropriate for the
/// activity's requirements.</typeparam>
/// <param name="Input">The input value to pass to the activity. May be null if the activity does not require input.</param>
public record ActivityExecutionRequest<TInput>(TInput? Input) : ActivityExecutionRequest;