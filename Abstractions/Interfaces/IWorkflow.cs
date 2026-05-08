namespace JetFlow.Interfaces;

public interface IWorkflow
{
    ValueTask ExecuteAsync(IWorkflowContext context);
}

public interface IWorkflow<in TInput>
{
    ValueTask ExecuteAsync(IWorkflowContext context, TInput? input);
}