namespace JetFlow;

public interface IActivityWithReturn<TOutput>
{
    ValueTask<TOutput> ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken);
}

public interface IActivityWithReturn<TOutput, TInput>
{
    ValueTask<TOutput> ExecuteAsync(IWorkflowState state, TInput? input, CancellationToken cancellationToken);
}
