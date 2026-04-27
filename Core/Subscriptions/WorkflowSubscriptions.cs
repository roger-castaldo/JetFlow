using JetFlow.Interfaces;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal class WorkflowSubscription<TWorkflow>
    (ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(serviceConnection, subjectMapper, messageSerializer, consumer, cancellationToken)
     where TWorkflow : class, IWorkflow
{
    protected override ValueTask HandleWorkflowEventAsync(WorkflowContext context)
        => Workflow.ExecuteAsync(context);
}

internal class WorkflowSubscription<TWorkflow, TInput>
    (ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(serviceConnection, subjectMapper, messageSerializer, consumer, cancellationToken)
     where TWorkflow : class, IWorkflow<TInput>
{
    protected override async ValueTask HandleWorkflowEventAsync(WorkflowContext context)
        => await Workflow.ExecuteAsync(context, (await MessageSerializer.DecodeAsync<TInput>(context.StartMessage.Data, context.StartMessage.Headers)));
}