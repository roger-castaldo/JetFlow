namespace JetFlow;

public enum ActivityResultStatus
{
    Success,
    Failure,
    Timeout
}

public record ActivityResult(ulong Index, ActivityResultStatus Status, string? ErrorMessage = null);

public record ActivityResult<TOutput>(ulong Index, ActivityResultStatus Status, string? ErrorMessage = null, TOutput? Output = default)
    : ActivityResult(Index, Status, ErrorMessage);
