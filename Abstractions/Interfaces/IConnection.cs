using JetFlow.Configs;

namespace JetFlow.Interfaces
{
    /// <summary>
    /// Represents a connection to the workflow engine, allowing you to register workflows and activities, start workflows, and schedule workflows.
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Registers a workflow of the specified type for execution with the provided options.
        /// </summary>
        /// <typeparam name="TWorkflow">The type of workflow to register. Must implement the IWorkflow interface.</typeparam>
        /// <param name="options">The options to configure the workflow registration. If null, default options are used.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A ValueTask that represents the asynchronous registration operation.</returns>
        ValueTask RegisterWorkflowAsync<TWorkflow>(WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : class,IWorkflow;
        /// <summary>
        /// Registers a workflow type for execution with the specified options.
        /// </summary>
        /// <typeparam name="TWorkflow">The workflow type to register. Must implement IWorkflow&lt;TInput&gt;.</typeparam>
        /// <typeparam name="TInput">The type of input accepted by the workflow.</typeparam>
        /// <param name="options">The options to use when registering the workflow, or null to use default options.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A ValueTask that represents the asynchronous registration operation.</returns>
        ValueTask RegisterWorkflowAsync<TWorkflow,TInput>(WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : class, IWorkflow<TInput>;
        /// <summary>
        /// Starts a new instance of the specified workflow type asynchronously.
        /// </summary>
        /// <typeparam name="TWorkflow">The type of workflow to start. Must implement the IWorkflow interface.</typeparam>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the workflow start operation.</param>
        /// <returns>A ValueTask that represents the asynchronous operation. The result contains the unique identifier of the
        /// started workflow instance.</returns>
        ValueTask<Guid> StartWorkflowAsync<TWorkflow>(CancellationToken cancellationToken = default)
            where TWorkflow : IWorkflow;
        /// <summary>
        /// Starts a new instance of the specified workflow asynchronously.
        /// </summary>
        /// <typeparam name="TWorkflow">The type of workflow to start. Must implement IWorkflow&lt;TInput&gt;.</typeparam>
        /// <typeparam name="TInput">The type of input data provided to the workflow.</typeparam>
        /// <param name="input">The input data to pass to the workflow instance.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A ValueTask that represents the asynchronous operation. The task result contains the unique identifier of
        /// the started workflow instance.</returns>
        ValueTask<Guid> StartWorkflowAsync<TWorkflow,TInput>(TInput input,CancellationToken cancellationToken = default)
            where TWorkflow : IWorkflow<TInput>;
        /// <summary>
        /// Registers the specified workflow activity for execution within the workflow host.
        /// </summary>
        /// <typeparam name="TWorkflowActivity">The type of the workflow activity to register. Must implement the IActivity interface.</typeparam>
        /// <param name="activity">The workflow activity instance to register. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the registration operation.</param>
        /// <returns>A ValueTask that represents the asynchronous registration operation.</returns>
        ValueTask RegisterWorkflowActivityAsync<TWorkflowActivity>(TWorkflowActivity activity, CancellationToken cancellationToken = default)
            where TWorkflowActivity : class, IActivity;
        /// <summary>
        /// Registers a workflow activity for execution within the workflow host.
        /// </summary>
        /// <typeparam name="TWorkflowActivity">The type of the workflow activity to register. Must implement IActivity&lt;TInput&gt;.</typeparam>
        /// <typeparam name="TInput">The type of input accepted by the workflow activity.</typeparam>
        /// <param name="activity">The workflow activity instance to register. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the registration operation.</param>
        /// <returns>A ValueTask that represents the asynchronous registration operation.</returns>
        ValueTask RegisterWorkflowActivityAsync<TWorkflowActivity, TInput>(TWorkflowActivity activity, CancellationToken cancellationToken = default)
            where TWorkflowActivity : class, IActivity<TInput>;
        /// <summary>
        /// Registers a workflow activity that produces a return value for execution within the workflow runtime.
        /// </summary>
        /// <typeparam name="TWorkflowActivity">The type of the workflow activity to register. Must implement IActivityWithReturn&lt;TOutput&gt;.</typeparam>
        /// <typeparam name="TOutput">The type of the value returned by the workflow activity.</typeparam>
        /// <param name="activity">The workflow activity instance to register. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the registration operation.</param>
        /// <returns>A ValueTask that represents the asynchronous registration operation.</returns>
        ValueTask RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput>(TWorkflowActivity activity, CancellationToken cancellationToken = default)
            where TWorkflowActivity : class, IActivityWithReturn<TOutput>;
        /// <summary>
        /// Registers a workflow activity that produces a return value and accepts input for execution within the workflow runtime.
        /// </summary>
        /// <typeparam name="TWorkflowActivity">The type of the workflow activity to register. Must implement IActivityWithReturn&lt;TOutput, TInput&gt;.</typeparam>
        /// <typeparam name="TOutput">The type of the value returned by the workflow activity.</typeparam>
        /// <typeparam name="TInput">The type of input accepted by the workflow activity.</typeparam>
        /// <param name="activity">The workflow activity instance to register. Cannot be null.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask RegisterWorkflowActivityWithReturnAsync<TWorkflowActivity, TOutput, TInput>(TWorkflowActivity activity, CancellationToken cancellationToken = default)
            where TWorkflowActivity : class, IActivityWithReturn<TOutput, TInput>;
        /// <summary>
        /// Schedules a workflow of the specified type for execution according to the provided schedule.
        /// </summary>
        /// <typeparam name="TWorkflow">The type of workflow to schedule. Must implement the IWorkflow interface.</typeparam>
        /// <param name="schedule">The schedule that defines when and how the workflow should be executed.</param>
        /// <param name="options">Optional configuration options for the workflow execution. If null, default options are used.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation is canceled if the token is triggered.</param>
        /// <returns>A ValueTask that represents the asynchronous scheduling operation. The result contains the unique identifier
        /// of the scheduled workflow instance.</returns>
        ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow>(IWorkflowSchedule schedule, WorkflowOptions? options = default, CancellationToken cancellationToken = default)
           where TWorkflow : IWorkflow;
        /// <summary>
        /// Schedules a workflow of the specified type to run according to the provided schedule.
        /// </summary>
        /// <typeparam name="TWorkflow">The type of workflow to schedule. Must implement IWorkflow&lt;TInput&gt;.</typeparam>
        /// <typeparam name="TInput">The type of input data provided to the workflow.</typeparam>
        /// <param name="input">The input data to pass to the workflow when it is executed.</param>
        /// <param name="schedule">The schedule that determines when the workflow will be executed.</param>
        /// <param name="options">Optional configuration options for the workflow execution. If not specified, default options are used.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the scheduling operation.</param>
        /// <returns>A ValueTask that represents the asynchronous operation. The result contains the unique identifier of the
        /// scheduled workflow.</returns>
        ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow, TInput>(TInput input, IWorkflowSchedule schedule, WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : IWorkflow<TInput>;
        /// <summary>
        /// Schedules the specified workflow to start after the given delay.
        /// </summary>
        /// <typeparam name="TWorkflow">The type of workflow to start. Must implement the IWorkflow interface.</typeparam>
        /// <param name="delay">The amount of time to wait before starting the workflow. Must be a non-negative duration.</param>
        /// <param name="options">Optional settings that configure the workflow execution. If null, default options are used.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the scheduling operation.</param>
        /// <returns>A ValueTask that represents the asynchronous operation. The result contains the unique identifier of the
        /// scheduled workflow instance.</returns>
        ValueTask<Guid> DelayStartWorkflowAsync<TWorkflow>(TimeSpan delay, WorkflowOptions? options = default, CancellationToken cancellationToken = default)
           where TWorkflow : IWorkflow;
        /// <summary>
        /// Schedules the specified workflow to start after the given delay with the provided input.
        /// </summary>
        /// <typeparam name="TWorkflow">The type of workflow to start. Must implement IWorkflow&lt;TInput&gt;.</typeparam>
        /// <typeparam name="TInput">The type of input required by the workflow.</typeparam>
        /// <param name="input">The input data to pass to the workflow when it starts.</param>
        /// <param name="delay">The amount of time to wait before starting the workflow.</param>
        /// <param name="options">Optional configuration options for the workflow execution. If null, default options are used.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the scheduling operation.</param>
        /// <returns>A ValueTask that represents the asynchronous operation. The result contains the unique identifier of the
        /// scheduled workflow instance.</returns>
        ValueTask<Guid> DelayStartWorkflowAsync<TWorkflow, TInput>(TInput input, TimeSpan delay, WorkflowOptions? options = default, CancellationToken cancellationToken = default)
            where TWorkflow : IWorkflow<TInput>;
    }
}
