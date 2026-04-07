using JetFlow.Configs;

namespace JetFlow
{
    public interface IConnection
    {
        ValueTask RegisterWorkflowAsync<TWorkflow>(WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : class,IWorkflow;
        ValueTask RegisterWorkflowAsync<TWorkflow,TInput>(WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : class, IWorkflow<TInput>;
        ValueTask RegisterWorkflowAsync<TWorkflow, TInput1, TInput2>(WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : class, IWorkflow<TInput1,TInput2>;
        ValueTask RegisterWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3>(WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : class, IWorkflow<TInput1, TInput2, TInput3>;
        ValueTask RegisterWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3, TInput4>(WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : class, IWorkflow<TInput1, TInput2, TInput3, TInput4>;
        ValueTask StartWorkflowAsync<TWorkflow>(CancellationToken cancellationToken)
            where TWorkflow : IWorkflow;
        ValueTask StartWorkflowAsync<TWorkflow,TInput>(TInput input,CancellationToken cancellationToken)
            where TWorkflow : IWorkflow<TInput>;
        ValueTask StartWorkflowAsync<TWorkflow, TInput1, TInput2>(TInput1 input1, TInput2 input2, CancellationToken cancellationToken)
            where TWorkflow : IWorkflow<TInput1, TInput2>;
        ValueTask StartWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3>(TInput1 input1, TInput2 input2, TInput3 input3, CancellationToken cancellationToken)
            where TWorkflow : IWorkflow<TInput1, TInput2, TInput3>;
        ValueTask StartWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3, TInput4>(TInput1 input, TInput2 input2, TInput3 input3, TInput4 input4, CancellationToken cancellationToken)
            where TWorkflow : IWorkflow<TInput1, TInput2, TInput3, TInput4>;

        ValueTask RegisterWorkflowActivityAsync<TWorkflowActivity>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivity;
        ValueTask RegisterWorkflowActivityAsync<TWorkflowActivity, TInput>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivity<TInput>;
        ValueTask RegisterWorkflowActivityAsync<TWorkflowActivity, TInput1, TInput2>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivity<TInput1, TInput2>;
        ValueTask RegisterWorkflowActivityAsync<TWorkflowActivity, TInput1, TInput2, TInput3>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivity<TInput1, TInput2, TInput3>;
        ValueTask RegisterWorkflowActivityAsync<TWorkflowActivity, TInput1, TInput2, TInput3, TInput4>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivity<TInput1, TInput2, TInput3, TInput4>;

        ValueTask RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivityWithReturn<TOutput>;
        ValueTask RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput, TInput>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivityWithReturn<TOutput, TInput>;
        ValueTask RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput, TInput1, TInput2>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivityWithReturn<TOutput, TInput1, TInput2>;
        ValueTask RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput, TInput1, TInput2, TInput3>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivityWithReturn<TOutput, TInput1, TInput2, TInput3>;
        ValueTask RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput, TInput1, TInput2, TInput3, TInput4>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivityWithReturn<TOutput, TInput1, TInput2, TInput3, TInput4>;
    }
}
