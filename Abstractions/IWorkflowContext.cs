namespace JetFlow;

public interface IWorkflowContext
{
    ValueTask WaitAsync(TimeSpan delay);
    ValueTask<ActivityResult> ExecuteActivityAsync<TActivity>(ActivityExecutionRequest executionRequest)
        where TActivity : IActivity;
    ValueTask<ActivityResult> ExecuteActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest)
        where TActivity : IActivity<TInput>;
    ValueTask<ActivityResult<TOutput>> ExecuteActivityAsync<TActivity, TOutput>(ActivityExecutionRequest executionRequest)
        where TActivity : IActivityWithReturn<TOutput>;
    ValueTask<ActivityResult<TOutput>> ExecuteActivityAsync<TActivity, TOutput, TInput>(ActivityExecutionRequest<TInput> executionRequest)
        where TActivity : IActivityWithReturn<TOutput, TInput>;
}
