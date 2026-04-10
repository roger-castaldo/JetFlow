using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace JetFlow.Subscriptions;

internal class WorkflowActivitySubscription<TWorkflowActivity>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithoutReturn<TWorkflowActivity>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflowActivity : IActivity
{
    protected override ValueTask HandleActivityRunWithoutReturnAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken)
        => Instance.ExecuteAsync(workflowState, cancellationToken);
}

internal class WorkflowActivitySubscription<TWorkflowActivity, TInput>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithoutReturn<TWorkflowActivity>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflowActivity : IActivity<TInput>
{
    protected async override ValueTask HandleActivityRunWithoutReturnAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken)
        => await Instance.ExecuteAsync(workflowState, (await MessageSerializer.DecodeAsync<TInput>(message.Message)), cancellationToken);
}
