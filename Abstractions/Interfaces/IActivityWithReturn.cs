namespace JetFlow.Interfaces;

public interface IActivityWithReturn<TOutput>
{
    Task<TOutput> ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken);
}

public interface IActivityWithReturn<TOutput, TInput>
{
    Task<TOutput> ExecuteAsync(IWorkflowState state, TInput? input, CancellationToken cancellationToken);
}
