using JetFlow.Configs;

namespace JetFlow;

/// <summary>
/// Represents optional parameters and metadata used when starting, scheduling,
/// or delaying the execution of a workflow. Use <see cref="Options"/> to
/// control execution behavior and <see cref="MetaData"/> to attach arbitrary
/// key/value metadata to the execution request.
/// </summary>
public record WorkflowExecutionRequest
{
    /// <summary>
    /// Optional workflow execution options such as priority, timeout, or retry settings.
    /// </summary>
    public WorkflowOptions? Options { get; init; } = null;

    /// <summary>
    /// Arbitrary metadata to associate with the workflow execution. Keys map to one or more values.
    /// </summary>
    public Dictionary<string, string[]>? MetaData { get; init; } = null;
}

/// <summary>
/// A workflow execution request that carries a strongly-typed input payload.
/// </summary>
/// <typeparam name="TInput">The type of the input payload to pass to the workflow.</typeparam>
/// <param name="Input">The input value to provide to the workflow at start time.</param>
public record WorkflowExecutionRequest<TInput>(TInput Input) : WorkflowExecutionRequest;
