using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;
internal class WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput>
    (TWorkflowActivity instance,
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, MetricsHelper metricsHelper, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithReturn<TWorkflowActivity, TOutput>(instance, serviceConnection, subjectMapper, messageSerializer, consumer, metricsHelper, cancellationToken)
     where TWorkflowActivity : IActivityWithReturn<TOutput>
{
    protected override Task<TOutput> HandleActivityRunWithReturnAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken)
        => Instance.ExecuteAsync(workflowState, cancellationToken);
}

internal class WorkflowSubscriptionActivityWithReturn<TWorkflowActivity, TOutput, TInput>
    (TWorkflowActivity instance,
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, MetricsHelper metricsHelper, CancellationToken cancellationToken)
    : AWorkflowActivitySubscriptionWithReturn<TWorkflowActivity, TOutput>(instance, serviceConnection, subjectMapper, messageSerializer, consumer, metricsHelper, cancellationToken)
     where TWorkflowActivity : IActivityWithReturn<TOutput, TInput>
{
    protected async override Task<TOutput> HandleActivityRunWithReturnAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken)
        => await Instance.ExecuteAsync((await MessageSerializer.DecodeAsync<TInput>(message.Data, message.Headers)), workflowState, cancellationToken);
}