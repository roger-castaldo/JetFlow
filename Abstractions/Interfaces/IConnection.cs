using JetFlow.Configs;

namespace JetFlow.Interfaces
{
    public interface IConnection
    {
        ValueTask RegisterWorkflowAsync<TWorkflow>(WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : class,IWorkflow;
        ValueTask RegisterWorkflowAsync<TWorkflow,TInput>(WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : class, IWorkflow<TInput>;
        ValueTask<Guid> StartWorkflowAsync<TWorkflow>(CancellationToken cancellationToken = default)
            where TWorkflow : IWorkflow;
        ValueTask<Guid> StartWorkflowAsync<TWorkflow,TInput>(TInput input,CancellationToken cancellationToken = default)
            where TWorkflow : IWorkflow<TInput>;
        ValueTask RegisterWorkflowActivityAsync<TWorkflowActivity>(TWorkflowActivity activity, CancellationToken cancellationToken = default)
            where TWorkflowActivity : class, IActivity;
        ValueTask RegisterWorkflowActivityAsync<TWorkflowActivity, TInput>(TWorkflowActivity activity, CancellationToken cancellationToken = default)
            where TWorkflowActivity : class, IActivity<TInput>;
        ValueTask RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput>(TWorkflowActivity activity, CancellationToken cancellationToken = default)
            where TWorkflowActivity : class, IActivityWithReturn<TOutput>;
        ValueTask RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput, TInput>(TWorkflowActivity activity, CancellationToken cancellationToken = default)
            where TWorkflowActivity : class, IActivityWithReturn<TOutput, TInput>;
        ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow>(IWorkflowSchedule schedule, WorkflowOptions? options = default, CancellationToken cancellationToken = default)
           where TWorkflow : IWorkflow;
        ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow, TInput>(TInput input, IWorkflowSchedule schedule, WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : IWorkflow<TInput>;
        ValueTask<Guid> DelayStartWorkflowAsync<TWorkflow>(TimeSpan delay, WorkflowOptions? options = default, CancellationToken cancellationToken = default)
           where TWorkflow : IWorkflow;
        ValueTask<Guid> DelayStartWorkflowAsync<TWorkflow, TInput>(TInput input, TimeSpan delay, WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : IWorkflow<TInput>;
    }
}
