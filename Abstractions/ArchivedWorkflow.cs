using JetFlow.Configs;
using System.Text.Json.Serialization;

namespace JetFlow;

public enum WorkflowStepTypes
{
    Action,
    Delay
}

public enum WorkflowStepStatuses
{
    Success,
    Failure,
    Timeout
}

public enum RetryTypes
{
    Timeout,
    Error
}

public record struct WorkflowStepRetry(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    RetryTypes RetryType,
    DateTime Timestamp
);

public record struct WorkflowStep(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    WorkflowStepTypes Type,
    Guid? ID,
    string? Name,
    DateTime StartTime,
    DateTime EndTime,
    WorkflowStepRetry[]? Retries,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    WorkflowStepStatuses Status,
    string? ErrorMessage,
    object? Result
);

public record struct ArchivedWorkflow(
    Guid ID,
    string Name,
    WorkflowOptions Options,
    DateTime StartedAt,
    DateTime FinishedAt,
    bool IsSuccessful,
    string? ErrorMessage,
    object? Arguments,
    WorkflowStep[] Steps
);
