namespace JetFlow;

/// <summary>
/// Represents the result of an activity execution, including its status, index, and an optional error message.
/// </summary>
/// <param name="Index">The zero-based index of the activity within the workflow or sequence.</param>
/// <param name="Status">The status indicating the outcome of the activity execution.</param>
/// <param name="ErrorMessage">An optional error message describing the reason for failure if the activity did not succeed; otherwise, null.</param>
public record ActivityResult(ulong Index, ActivityResultStatus Status, string? ErrorMessage = null);

/// <summary>
/// Represents the result of an activity execution, including its status, optional error message, and an output value of
/// the specified type.
/// </summary>
/// <typeparam name="TOutput">The type of the output value returned by the activity. May be null or default if the activity did not produce a
/// result.</typeparam>
/// <param name="Index">The zero-based index of the activity execution within a sequence or workflow.</param>
/// <param name="Status">The status indicating whether the activity succeeded, failed, or was skipped.</param>
/// <param name="ErrorMessage">An optional error message describing the reason for failure if the activity did not succeed; otherwise, null.</param>
/// <param name="Output">The output value produced by the activity, or the default value for the type if no output was generated.</param>
public record ActivityResult<TOutput>(ulong Index, ActivityResultStatus Status, string? ErrorMessage = null, TOutput? Output = default)
    : ActivityResult(Index, Status, ErrorMessage);
