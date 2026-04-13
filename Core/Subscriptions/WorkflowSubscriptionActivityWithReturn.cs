using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;
internal class WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput>
    (TWorkflowActivity instance,
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithReturn<TWorkflowActivity, TOutput>(instance, serviceConnection, subjectMapper, messageSerializer, consumer, cancellationToken)
     where TWorkflowActivity : IActivityWithReturn<TOutput>
{
    protected override ValueTask<TOutput> HandleActivityRunWithReturnAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken)
        => Instance.ExecuteAsync(workflowState, cancellationToken);
}

internal class WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput, TInput>
    (TWorkflowActivity instance,
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithReturn<TWorkflowActivity, TOutput>(instance, serviceConnection, subjectMapper, messageSerializer, consumer, cancellationToken)
     where TWorkflowActivity : IActivityWithReturn<TOutput, TInput>
{
    protected async override ValueTask<TOutput> HandleActivityRunWithReturnAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken)
        => await Instance.ExecuteAsync(workflowState, (await MessageSerializer.DecodeAsync<TInput>(message.Message)), cancellationToken);
}