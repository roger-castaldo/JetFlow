using JetFlow.Interfaces;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowActivitySubscriptionWithoutReturn<TWorkflowActivity>
    (TWorkflowActivity instance,
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscription<TWorkflowActivity>(instance, serviceConnection, subjectMapper, messageSerializer, consumer, cancellationToken)
{
    protected override sealed async Task HandleActivityRunAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken)
    {
        await HandleActivityRunWithoutReturnAsync(workflowState, message, cancellationToken);
        await ServiceConnection.EndActivityAsync(message, cancellationToken);
    }
    protected abstract Task HandleActivityRunWithoutReturnAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken);
}
