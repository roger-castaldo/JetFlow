using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace JetFlow.Subscriptions;

internal class WorkflowActivitySubscription<TWorkflowActivity>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithoutReturn<TWorkflowActivity>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflowActivity : IActivity
{
    protected override ValueTask HandleActivityRunWithoutReturnAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
        => Instance.ExecuteAsync(workflowState, cancellationToken);
}

internal class WorkflowActivitySubscription<TWorkflowActivity, TInput>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithoutReturn<TWorkflowActivity>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflowActivity : IActivity<TInput>
{
    protected async override ValueTask HandleActivityRunWithoutReturnAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
        => await Instance.ExecuteAsync(workflowState, (await MessageSerializer.DecodeAsync<TInput>(msg)), cancellationToken);
}

internal class WorkflowActivitySubscription<TWorkflowActivity, TInput1, TInput2>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithoutReturn<TWorkflowActivity>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflowActivity : IActivity<TInput1, TInput2>
{
    protected async override ValueTask HandleActivityRunWithoutReturnAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
    {
        var (input1, input2) = await MessageSerializer.DecodeAsync<TInput1, TInput2>(msg);
        await Instance.ExecuteAsync(workflowState, input1, input2, cancellationToken);
    }
}

internal class WorkflowActivitySubscription<TWorkflowActivity, TInput1, TInput2, TInput3>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithoutReturn<TWorkflowActivity>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflowActivity : IActivity<TInput1, TInput2, TInput3>
{
    protected async override ValueTask HandleActivityRunWithoutReturnAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
    {
        var (input1, input2, input3) = await MessageSerializer.DecodeAsync<TInput1, TInput2, TInput3>(msg);
        await Instance.ExecuteAsync(workflowState, input1, input2, input3, cancellationToken);
    }
}

internal class WorkflowActivitySubscription<TWorkflowActivity, TInput1, TInput2, TInput3, TInput4>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithoutReturn<TWorkflowActivity>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflowActivity : IActivity<TInput1, TInput2, TInput3, TInput4>
{
    protected async override ValueTask HandleActivityRunWithoutReturnAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
    {
        var (input1, input2, input3, input4) = await MessageSerializer.DecodeAsync<TInput1, TInput2, TInput3, TInput4>(msg);
        await Instance.ExecuteAsync(workflowState, input1, input2, input3, input4, cancellationToken);
    }
}
