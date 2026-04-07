namespace JetFlow;

public interface IActivityWithReturn<TOutput>
{
    ValueTask<TOutput> ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken);
}

public interface IActivityWithReturn<TOutput, TInput>
{
    ValueTask<TOutput> ExecuteAsync(IWorkflowState state, TInput? input, CancellationToken cancellationToken);
}

public interface IActivityWithReturn<TOutput, TInput1, TInput2>
{
    ValueTask<TOutput> ExecuteAsync(IWorkflowState state, TInput1? input1, TInput2? input2, CancellationToken cancellationToken);
}

public interface IActivityWithReturn<TOutput, TInput1, TInput2, TInput3>
{
    ValueTask<TOutput> ExecuteAsync(IWorkflowState state, TInput1? input1, TInput2? input2, TInput3? input3, CancellationToken cancellationToken);
}

public interface IActivityWithReturn<TOutput, TInput1, TInput2, TInput3, TInput4>
{
    ValueTask<TOutput> ExecuteAsync(IWorkflowState state, TInput1? input, TInput2? input2, TInput3? input3, TInput4? input4, CancellationToken cancellationToken);
}
