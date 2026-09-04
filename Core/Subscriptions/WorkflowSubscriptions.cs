using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal class WorkflowSubscription<TWorkflow>
    (ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, MetricsHelper metricsHelper, IServiceProvider? serviceProvider, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(serviceConnection, subjectMapper, messageSerializer, consumer, metricsHelper, serviceProvider, cancellationToken)
     where TWorkflow : class, IWorkflow
{
    protected override ValueTask HandleWorkflowEventAsync(WorkflowContext context)
        => Workflow.ExecuteAsync(context);
}

internal class WorkflowSubscription<TWorkflow, TInput>
    (ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, MetricsHelper metricsHelper, IServiceProvider? serviceProvider, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(serviceConnection, subjectMapper, messageSerializer, consumer, metricsHelper, serviceProvider, cancellationToken)
     where TWorkflow : class, IWorkflow<TInput>
{
    protected override async ValueTask HandleWorkflowEventAsync(WorkflowContext context)
        => await Workflow.ExecuteAsync(context, (await MessageSerializer.DecodeAsync<TInput>(await MessagesHelper.RetrieveMessageDataAsync(ServiceConnection.LargeMessageStore, context.StartMessage.Data, CancellationToken), context.StartMessage.Headers)));
}