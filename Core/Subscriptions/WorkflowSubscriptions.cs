using NATS.Client.Core;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal class WorkflowSubscription<TWorkflow>
    (NatsConnection connection,INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(connection, consumer, messageSerializer, cancellationToken)
     where TWorkflow : class, IWorkflow
{
    protected override ValueTask HandleStepEventAsync(IWorkflowContext context, INatsJSMsg<byte[]> msg)
    {
        throw new NotImplementedException();
    }

    protected override ValueTask HandleWorkflowStartAsync(IWorkflowContext context, INatsJSMsg<byte[]> msg)
        => Workflow.ExecuteAsync(context);
}

internal class WorkflowSubscription<TWorkflow, TInput>
    (NatsConnection connection, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(connection, consumer, messageSerializer, cancellationToken)
     where TWorkflow : class, IWorkflow<TInput>
{
    protected override ValueTask HandleStepEventAsync(IWorkflowContext context, INatsJSMsg<byte[]> msg)
    {
        throw new NotImplementedException();
    }

    protected override async ValueTask HandleWorkflowStartAsync(IWorkflowContext context, INatsJSMsg<byte[]> msg)
        => await Workflow.ExecuteAsync(context, (await MessageSerializer.DecodeAsync<TInput>(msg.Data!,msg.Headers!)));
}

internal class WorkflowSubscription<TWorkflow, TInput1, TInput2>
    (NatsConnection connection, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(connection, consumer, messageSerializer, cancellationToken)
     where TWorkflow : class, IWorkflow<TInput1, TInput2>
{
    protected override ValueTask HandleStepEventAsync(IWorkflowContext context, INatsJSMsg<byte[]> msg)
    {
        throw new NotImplementedException();
    }

    protected override async ValueTask HandleWorkflowStartAsync(IWorkflowContext context, INatsJSMsg<byte[]> msg)
    {
        var (input1, input2) = await MessageSerializer.DecodeAsync<(TInput1, TInput2)>(msg.Data!, msg.Headers!);
        await Workflow.ExecuteAsync(context, input1, input2);
    }
}

internal class WorkflowSubscription<TWorkflow, TInput1, TInput2, TInput3>
    (NatsConnection connection, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(connection, consumer, messageSerializer, cancellationToken)
     where TWorkflow : class, IWorkflow<TInput1, TInput2, TInput3>
{
    protected override ValueTask HandleStepEventAsync(IWorkflowContext context, INatsJSMsg<byte[]> msg)
    {
        throw new NotImplementedException();
    }

    protected override async ValueTask HandleWorkflowStartAsync(IWorkflowContext context, INatsJSMsg<byte[]> msg)
    {
        var (input1, input2, input3) = await MessageSerializer.DecodeAsync<(TInput1, TInput2, TInput3)>(msg.Data!, msg.Headers!);
        await Workflow.ExecuteAsync(context, input1, input2, input3);
    }
}

internal class WorkflowSubscription<TWorkflow, TInput1, TInput2, TInput3, TInput4>
    (NatsConnection connection, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : AWorkflowSubscription<TWorkflow>(connection, consumer, messageSerializer, cancellationToken)
     where TWorkflow : class, IWorkflow<TInput1, TInput2, TInput3, TInput4>
{
    protected override ValueTask HandleStepEventAsync(IWorkflowContext context, INatsJSMsg<byte[]> msg)
    {
        throw new NotImplementedException();
    }

    protected override async ValueTask HandleWorkflowStartAsync(IWorkflowContext context, INatsJSMsg<byte[]> msg)
    {
        var (input1, input2, input3, input4) = await MessageSerializer.DecodeAsync<(TInput1, TInput2, TInput3, TInput4)>(msg.Data!, msg.Headers!);
        await Workflow.ExecuteAsync(context, input1, input2, input3, input4);
    }
}
