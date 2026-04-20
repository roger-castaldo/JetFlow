using JetFlow.Interfaces;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowActivitySubscriptionWithReturn<TWorkflowActivity, TOutput>
    (TWorkflowActivity instance,
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscription<TWorkflowActivity>(instance, serviceConnection, subjectMapper, messageSerializer, consumer, cancellationToken)
{
    protected override sealed async Task HandleActivityRunAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken)
    {
        var result = await HandleActivityRunWithReturnAsync(workflowState, message, cancellationToken);
        await ServiceConnection.EndActivityAsync<TOutput>(message, result, cancellationToken);
    }
    protected abstract Task<TOutput> HandleActivityRunWithReturnAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellation);
}
