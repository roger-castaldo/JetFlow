namespace JetFlow;

public interface IActivity
{
    ValueTask ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken);
}

public interface IActivity<TInput>
{
    ValueTask ExecuteAsync(IWorkflowState state, TInput? input, CancellationToken cancellationToken);
}