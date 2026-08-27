using JetFlow.Configs;

namespace JetFlow;

public record WorkflowExecutionRequest
{
    public WorkflowOptions? Options { get; init; } = null;

    public Dictionary<string, string[]>? MetaData { get; init; } = null;
}

public record WorkflowExecutionRequest<TInput>(TInput Input) : WorkflowExecutionRequest;
