using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace JetFlow.Subscriptions;
internal class WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithReturn<TWorkflowActivity, TOutput>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflowActivity : IActivityWithReturn<TOutput>
{
    protected override ValueTask<TOutput> HandleActivityRunWithReturnAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
        => Instance.ExecuteAsync(workflowState, cancellationToken);
}

internal class WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput, TInput>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithReturn<TWorkflowActivity, TOutput>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflowActivity : IActivityWithReturn<TOutput, TInput>
{
    protected async override ValueTask<TOutput> HandleActivityRunWithReturnAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
        => await Instance.ExecuteAsync(workflowState, (await MessageSerializer.DecodeAsync<TInput>(msg)), cancellationToken);
}

internal class WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput, TInput1, TInput2>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithReturn<TWorkflowActivity, TOutput>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflowActivity : IActivityWithReturn<TOutput, TInput1, TInput2>
{
    protected async override ValueTask<TOutput> HandleActivityRunWithReturnAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
    {
        var (input1, input2) = await MessageSerializer.DecodeAsync<TInput1, TInput2>(msg);
        return await Instance.ExecuteAsync(workflowState, input1, input2, cancellationToken);
    }
}

internal class WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput, TInput1, TInput2, TInput3>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithReturn<TWorkflowActivity, TOutput>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflowActivity : IActivityWithReturn<TOutput, TInput1, TInput2, TInput3>
{
    protected async override ValueTask<TOutput> HandleActivityRunWithReturnAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
    {
        var (input1, input2, input3) = await MessageSerializer.DecodeAsync<TInput1, TInput2, TInput3>(msg);
        return await Instance.ExecuteAsync(workflowState, input1, input2, input3, cancellationToken);
    }
}

internal class WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput, TInput1, TInput2, TInput3, TInput4>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithReturn<TWorkflowActivity, TOutput>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflowActivity : IActivityWithReturn<TOutput, TInput1, TInput2, TInput3, TInput4>
{
    protected async override ValueTask<TOutput> HandleActivityRunWithReturnAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
    {
        var (input1, input2, input3, input4) = await MessageSerializer.DecodeAsync<TInput1, TInput2, TInput3, TInput4>(msg);
        return await Instance.ExecuteAsync(workflowState, input1, input2, input3, input4, cancellationToken);
    }
}
