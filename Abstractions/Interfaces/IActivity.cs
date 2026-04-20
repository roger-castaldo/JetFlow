namespace JetFlow.Interfaces;

public interface IActivity
{
    Task ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken);
}

public interface IActivity<TInput>
{
    Task ExecuteAsync(IWorkflowState state, TInput? input, CancellationToken cancellationToken);
}