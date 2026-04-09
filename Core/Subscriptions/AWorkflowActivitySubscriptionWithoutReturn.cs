using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowActivitySubscriptionWithoutReturn<TWorkflowActivity>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscription<TWorkflowActivity>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
{
    protected override sealed async ValueTask HandleActivityRunAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken)
    {
        await HandleActivityRunWithoutReturnAsync(workflowState, message, cancellationToken);
        await ActivityHelper.EndActivityAsync(message, Connection, cancellationToken);
    }
    protected abstract ValueTask HandleActivityRunWithoutReturnAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken);
}
