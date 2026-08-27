namespace JetFlow.Data;

internal record WorkflowEnd(DateTime EndTime, string? ErrorMessage)
{
    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
