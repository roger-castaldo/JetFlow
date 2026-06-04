namespace JetFlow.Interfaces;

/// <summary>
/// Represents a workflow that can be executed with a given context. This interface defines the contract for executing a workflow without any input parameters.
/// </summary>
public interface IWorkflow
{
    /// <summary>
    /// Executes the workflow operations asynchronously using the specified workflow context.
    /// </summary>
    /// <param name="context">The workflow context that provides data and services required for execution. Cannot be null.</param>
    /// <returns>A ValueTask that represents the asynchronous execution operation.</returns>
    ValueTask ExecuteAsync(IWorkflowContext context);
}

/// <summary>
/// Represents a workflow that can be executed with a given context and an input parameter. This interface defines the contract for executing a workflow that requires input data.
/// </summary>
/// <typeparam name="TInput"></typeparam>
public interface IWorkflow<in TInput>
{
    /// <summary>
    /// Executes the workflow operation asynchronously using the specified context and input.
    /// </summary>
    /// <param name="context">The workflow context that provides execution state and services for the operation. Cannot be null.</param>
    /// <param name="input">The input data for the workflow operation. May be null if the operation does not require input.</param>
    /// <returns>A ValueTask that represents the asynchronous execution of the workflow operation.</returns>
    ValueTask ExecuteAsync(IWorkflowContext context, TInput? input);
}