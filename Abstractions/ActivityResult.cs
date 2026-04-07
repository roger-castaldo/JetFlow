namespace JetFlow;

public enum ActivityResultStatus
{
    Success,
    Failure,
    Timeout
}

public record ActivityResult(Guid ID, ActivityResultStatus Status, string? ErrorMessage = null);

public record ActivityResult<TOutput>(Guid ID, ActivityResultStatus Status, string? ErrorMessage = null, TOutput? Output = default)
    : ActivityResult(ID, Status, ErrorMessage);
