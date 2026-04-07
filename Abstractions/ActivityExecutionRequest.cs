namespace JetFlow;

public record ActivityOptions()
{
    public TimeSpan? OverallTimeout { get; init; } = null;
};

public record ActivityExecutionRequest() : ActivityOptions;

public record ActivityExecutionRequest<TInput>(TInput? Input) : ActivityOptions;

public record ActivityExecutionRequest<TInput1, TInput2>(TInput1? Input1, TInput2? Input2) : ActivityOptions;

public record ActivityExecutionRequest<TInput1, TInput2, TInput3>(TInput1? Input1, TInput2? Input2, TInput3? Input3) : ActivityOptions;

public record ActivityExecutionRequest<TInput1, TInput2, TInput3, TInput4>(TInput1? Input1, TInput2? Input2, TInput3? Input3, TInput4? Input4) : ActivityOptions;