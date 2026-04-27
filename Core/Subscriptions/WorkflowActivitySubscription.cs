using JetFlow.Interfaces;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal class WorkflowActivitySubscription<TWorkflowActivity>
    (TWorkflowActivity instance,
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithoutReturn<TWorkflowActivity>(instance, serviceConnection, subjectMapper, messageSerializer, consumer, cancellationToken)
     where TWorkflowActivity : IActivity
{
    protected override Task HandleActivityRunWithoutReturnAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken)
        => Instance.ExecuteAsync(workflowState, cancellationToken);
}

internal class WorkflowActivitySubscription<TWorkflowActivity, TInput>
    (TWorkflowActivity instance,
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithoutReturn<TWorkflowActivity>(instance, serviceConnection, subjectMapper, messageSerializer, consumer, cancellationToken)
     where TWorkflowActivity : IActivity<TInput>
{
    protected async override Task HandleActivityRunWithoutReturnAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken)
        => await Instance.ExecuteAsync((await MessageSerializer.DecodeAsync<TInput>(message.Message.Data, message.Message.Headers)), workflowState, cancellationToken);
}
