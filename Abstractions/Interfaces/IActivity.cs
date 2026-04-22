namespace JetFlow.Interfaces;

public interface IActivity
{
    Task ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken);
}

public interface IActivity<TInput>
{
    Task ExecuteAsync(TInput? input, IWorkflowState state, CancellationToken cancellationToken);
}