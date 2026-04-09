namespace JetFlow;

public interface IWorkflow
{
    ValueTask ExecuteAsync(IWorkflowContext context);
}

public interface IWorkflow<TInput>
{
    ValueTask ExecuteAsync(IWorkflowContext context, TInput? input);
}