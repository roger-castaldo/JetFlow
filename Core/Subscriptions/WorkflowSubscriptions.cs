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

internal class WorkflowSubscription<TWorkflow, TInput1, TInput2>
    (INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflow : class, IWorkflow<TInput1, TInput2>
{
    protected override async ValueTask HandleWorkflowEventAsync(WorkflowContext context)
    {
        var (input1, input2) = await MessageSerializer.DecodeAsync<TInput1, TInput2>(context.StartMessage);
        await Workflow.ExecuteAsync(context, input1, input2);
    }
}

internal class WorkflowSubscription<TWorkflow, TInput1, TInput2, TInput3>
    (INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflow : class, IWorkflow<TInput1, TInput2, TInput3>
{
    protected override async ValueTask HandleWorkflowEventAsync(WorkflowContext context)
    {
        var (input1, input2, input3) = await MessageSerializer.DecodeAsync<TInput1, TInput2, TInput3>(context.StartMessage);
        await Workflow.ExecuteAsync(context, input1, input2, input3);
    }
}

internal class WorkflowSubscription<TWorkflow, TInput1, TInput2, TInput3, TInput4>
    (INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(connection, natsJSContext, timerStore, consumer, messageSerializer, cancellationToken)
     where TWorkflow : class, IWorkflow<TInput1, TInput2, TInput3, TInput4>
{
    protected override async ValueTask HandleWorkflowEventAsync(WorkflowContext context)
    {
        var (input1, input2, input3, input4) = await MessageSerializer.DecodeAsync<TInput1, TInput2, TInput3, TInput4>(context.StartMessage);
        await Workflow.ExecuteAsync(context, input1, input2, input3, input4);
    }
}
