using JetFlow.Configs;

namespace JetFlow.Interfaces
{
    public interface IConnection
    {
        ValueTask RegisterWorkflowAsync<TWorkflow>(WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : class,IWorkflow;
        ValueTask RegisterWorkflowAsync<TWorkflow,TInput>(WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : class, IWorkflow<TInput>;
        ValueTask StartWorkflowAsync<TWorkflow>(CancellationToken cancellationToken)
            where TWorkflow : IWorkflow;
        ValueTask StartWorkflowAsync<TWorkflow,TInput>(TInput input,CancellationToken cancellationToken)
            where TWorkflow : IWorkflow<TInput>;
        ValueTask RegisterWorkflowActivityAsync<TWorkflowActivity>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivity;
        ValueTask RegisterWorkflowActivityAsync<TWorkflowActivity, TInput>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivity<TInput>;
        ValueTask RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivityWithReturn<TOutput>;
        ValueTask RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput, TInput>(TWorkflowActivity activity, CancellationToken cancellationToken)
            where TWorkflowActivity : class, IActivityWithReturn<TOutput, TInput>;
    }
}
