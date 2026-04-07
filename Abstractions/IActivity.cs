namespace JetFlow;

public interface IActivity
{
    ValueTask ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken);
}

public interface IActivity<TInput>
{
    ValueTask ExecuteAsync(IWorkflowState state, TInput? input, CancellationToken cancellationToken);
}

public interface IActivity<TInput1, TInput2>
{
    ValueTask ExecuteAsync(IWorkflowState state, TInput1? input1, TInput2? input2, CancellationToken cancellationToken);
}

public interface IActivity<TInput1, TInput2, TInput3>
{
    ValueTask ExecuteAsync(IWorkflowState state, TInput1? input1, TInput2? input2, TInput3? input3, CancellationToken cancellationToken);
}

public interface IActivity<TInput1, TInput2, TInput3, TInput4>
{
    ValueTask ExecuteAsync(IWorkflowState state, TInput1? input, TInput2? input2, TInput3? input3, TInput4? input4, CancellationToken cancellationToken);
}