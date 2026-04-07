namespace JetFlow;

public interface IWorkflowContext
{
    ValueTask WaitAsync(TimeSpan delay);
    ValueTask<ActivityResult> ExecuteActivityAsync<TActivity>(ActivityExecutionRequest executionRequest)
        where TActivity : IActivity;
    ValueTask<ActivityResult> ExecuteActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest)
        where TActivity : IActivity<TInput>;
    ValueTask<ActivityResult> ExecuteActivityAsync<TActivity, TInput1, TInput2>(ActivityExecutionRequest<TInput1, TInput2> executionRequest)
        where TActivity : IActivity<TInput1, TInput2>;
    ValueTask<ActivityResult> ExecuteActivityAsync<TActivity, TInput1, TInput2, TInput3>(ActivityExecutionRequest<TInput1, TInput2, TInput3> executionRequest)
        where TActivity : IActivity<TInput1, TInput2, TInput3>;
    ValueTask<ActivityResult> ExecuteActivityAsync<TActivity, TInput1, TInput2, TInput3, TInput4>(ActivityExecutionRequest<TInput1, TInput2, TInput3, TInput4> executionRequest)
        where TActivity : IActivity<TInput1, TInput2, TInput3, TInput4>;
    ValueTask<ActivityResult<TOutput>> ExecuteActivityAsync<TActivity, TOutput>(ActivityExecutionRequest executionRequest)
        where TActivity : IActivityWithReturn<TOutput>;
    ValueTask<ActivityResult<TOutput>> ExecuteActivityAsync<TActivity, TOutput, TInput>(ActivityExecutionRequest<TInput> executionRequest)
        where TActivity : IActivityWithReturn<TOutput, TInput>;
    ValueTask<ActivityResult<TOutput>> ExecuteActivityAsync<TActivity, TOutput, TInput1, TInput2>(ActivityExecutionRequest<TInput1, TInput2> executionRequest)
        where TActivity : IActivityWithReturn<TOutput, TInput1, TInput2>;
    ValueTask<ActivityResult<TOutput>> ExecuteActivityAsync<TActivity, TOutput, TInput1, TInput2, TInput3>(ActivityExecutionRequest<TInput1, TInput2, TInput3> executionRequest)
        where TActivity : IActivityWithReturn<TOutput, TInput1, TInput2, TInput3>;
    ValueTask<ActivityResult<TOutput>> ExecuteActivityAsync<TActivity, TOutput, TInput1, TInput2, TInput3, TInput4>(ActivityExecutionRequest<TInput1, TInput2, TInput3, TInput4> executionRequest)
        where TActivity : IActivityWithReturn<TOutput, TInput1, TInput2, TInput3, TInput4>;
}
