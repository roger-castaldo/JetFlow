namespace JetFlow
{
    public interface IConnection
    {
        ValueTask RegisterWorkflowAsync<TWorkflow>(CancellationToken cancellationToken)
            where TWorkflow : class,IWorkflow;
        ValueTask RegisterWorkflowAsync<TWorkflow,TInput>(CancellationToken cancellationToken)
            where TWorkflow : class, IWorkflow<TInput>;
        ValueTask RegisterWorkflowAsync<TWorkflow, TInput1, TInput2>(CancellationToken cancellationToken)
            where TWorkflow : class, IWorkflow<TInput1,TInput2>;
        ValueTask RegisterWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3>(CancellationToken cancellationToken)
            where TWorkflow : class, IWorkflow<TInput1, TInput2, TInput3>;
        ValueTask RegisterWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3, TInput4>(CancellationToken cancellationToken)
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
    }
}
