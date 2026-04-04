namespace JetFlow.Messages;

public record WorkflowEnd(DateTime EndTime, string? ErrorMessage)
{
    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
