using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowActivitySubscriptionWithReturn<TWorkflowActivity, TOutput>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscription<TWorkflowActivity>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
{
    protected override sealed async ValueTask HandleActivityRunAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken)
    {
        var result = await HandleActivityRunWithReturnAsync(workflowState, message, cancellationToken);
        await ActivityHelper.EndActivityAsync<TOutput>(message, result, MessageSerializer, Connection, cancellationToken);
    }
    protected abstract ValueTask<TOutput> HandleActivityRunWithReturnAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellation);
}
