using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowActivitySubscriptionWithoutReturn<TWorkflowActivity>
    (TWorkflowActivity instance, INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscription<TWorkflowActivity>(instance, connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
{
    protected override sealed async ValueTask HandleActivityRunAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
    {
        await HandleActivityRunWithoutReturnAsync(workflowState, workflowName, workflowId, activityId, msg, cancellationToken);
        await ActivityHelper.EndActivityAsync(workflowName, workflowId, NameHelper.GetActivityName<TWorkflowActivity>(), activityId, Connection, cancellationToken);
    }
    protected abstract ValueTask HandleActivityRunWithoutReturnAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellation);
}
