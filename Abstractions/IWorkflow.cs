namespace JetFlow;

public interface IWorkflow
{
    ValueTask ExecuteAsync(IWorkflowContext context);
}

public interface IWorkflow<TInput>
{
    ValueTask ExecuteAsync(IWorkflowContext context, TInput? input);
}

public interface IWorkflow<TInput1, TInput2>
{
    ValueTask ExecuteAsync(IWorkflowContext context, TInput1? input1, TInput2? input2);
}

public interface IWorkflow<TInput1, TInput2, TInput3>
{
    ValueTask ExecuteAsync(IWorkflowContext context, TInput1? input1, TInput2? input2, TInput3? input3);
}

public interface IWorkflow<TInput1, TInput2, TInput3, TInput4>
{
    ValueTask ExecuteAsync(IWorkflowContext context, TInput1? input, TInput2? input2, TInput3? input3, TInput4? input4);
}
