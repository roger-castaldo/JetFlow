using NATS.Client.Core;
using NATS.Client.JetStream;

namespace JetFlow.Helpers;

internal static class WorkflowHelper
{

    private static async ValueTask StartWorkflowAsync<TWorkflow>(string? instanceNamespace, INatsConnection connection, byte[] data, NatsHeaders? headers, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var name = NameHelper.GetWorkflowName<TWorkflow>();
        using var activity = TraceHelper.StartWorkflow(name, id.ToString());
        var subject = SubjectHelper.WorkflowStart(instanceNamespace, name, id.ToString());
        headers??=new();
        ConnectionHelper.AddMessageIds(headers, $"{name}-{id}-start");
        await ConnectionHelper.PublishMessageAsync(connection, data, new(subject, headers), cancellationToken);
    }

    public static ValueTask StartWorkflowAsync<TWorkflow>(string? instanceNamespace, INatsConnection connection, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
        => StartWorkflowAsync<TWorkflow>(instanceNamespace, connection, [], null, cancellationToken);

    public static async ValueTask StartWorkflowAsync<TWorkflow, TInput>(string? instanceNamespace, INatsConnection connection, MessageSerializer messageSerializer, TInput input, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow<TInput>
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput>(input);
        await StartWorkflowAsync<TWorkflow>(instanceNamespace, connection, data, headers, cancellationToken);
    }

    private static MessageInfo EndWorkflow(EventMessage message, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowEnd(message.Namespace, message.WorkflowName, message.WorkflowId);
        var natsHeaders = message.InjectHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{message.WorkflowName}-{message.WorkflowId}-end");
        return new(subject, natsHeaders);
    }

    public static async ValueTask EndWorkflowAsync(INatsConnection connection, MessageSerializer messageSerializer,EventMessage message, Messages.WorkflowEnd workflowEnd, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<Messages.WorkflowEnd>(workflowEnd);
        await ConnectionHelper.PublishMessageAsync(connection, data, EndWorkflow(message, headers), cancellationToken);
    }

    private static MessageInfo WorkflowDelayStartMessage(EventMessage message, Guid id, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowDelayStart(message.Namespace, message.WorkflowName, message.WorkflowId);
        var natsHeaders = message.InjectHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{message.WorkflowName}-{message.WorkflowId}-{id}-delaystart");
        return new(subject, natsHeaders);
    }

    private static MessageInfo WorkflowDelayTimerMessage(EventMessage message, Guid id, TimeSpan delay, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowDelayTimer(message.Namespace, message.WorkflowName, message.WorkflowId);
        var natsHeaders = message.InjectHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{message.WorkflowName}-{message.WorkflowId}-{id}-delaytimer");
        ConnectionHelper.ScheduleDelayedSend(natsHeaders, delay, SubjectHelper.WorkflowDelayEnd(message.Namespace, message.WorkflowName, message.WorkflowId));
        return new(subject, natsHeaders);
    }

    internal static async Task StartWorkflowDelayAsync(EventMessage message, TimeSpan delay, INatsConnection connection, INatsJSContext jsContext, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        using var activity = TraceHelper.StartDelay(message);
        await ConnectionHelper.PublishMessageAsync(connection, [], WorkflowDelayStartMessage(message, id), cancellationToken);
        await ConnectionHelper.PublishMessageAsync(jsContext, [], WorkflowDelayTimerMessage(message, id, delay), cancellationToken);
    }
}
