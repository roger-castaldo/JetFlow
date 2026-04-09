using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace JetFlow.Subscriptions;

internal class WorkflowSubscription<TWorkflow>
    (INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflow : class, IWorkflow
{
    protected override ValueTask HandleWorkflowEventAsync(WorkflowContext context)
        => Workflow.ExecuteAsync(context);
}

internal class WorkflowSubscription<TWorkflow, TInput>
    (INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflow : class, IWorkflow<TInput>
{
    protected override async ValueTask HandleWorkflowEventAsync(WorkflowContext context)
        => await Workflow.ExecuteAsync(context, (await MessageSerializer.DecodeAsync<TInput>(context.StartMessage)));
}