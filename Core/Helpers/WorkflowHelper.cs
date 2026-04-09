using NATS.Client.Core;
using NATS.Client.JetStream;

namespace JetFlow.Helpers;

internal static class WorkflowHelper
{
    

    private static MessageInfo StartWorkflow<TWorkflow>(NatsHeaders? headers = null)
    {
        var id = Guid.NewGuid();
        var subject = SubjectHelper.WorkflowStart(NameHelper.GetWorkflowName<TWorkflow>(), id.ToString());
        headers??=new();
        ConnectionHelper.AddMessageIds(headers, $"{NameHelper.GetWorkflowName<TWorkflow>()}-{id}-start");
        return new(subject, headers);
    }

    public static ValueTask StartWorkflowAsync<TWorkflow>(INatsConnection connection, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
        => ConnectionHelper.PublishMessageAsync(connection, Array.Empty<byte>(), StartWorkflow<TWorkflow>(), cancellationToken);

    public static async ValueTask StartWorkflowAsync<TWorkflow, TInput>(INatsConnection connection, MessageSerializer messageSerializer, TInput input, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow<TInput>
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput>(input);
        await ConnectionHelper.PublishMessageAsync(connection, data, StartWorkflow<TWorkflow>(headers), cancellationToken);
    }

    private static MessageInfo EndWorkflow(EventMessage message, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowEnd(message.WorkflowName, message.WorkflowId);
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
        var subject = SubjectHelper.WorkflowDelayStart(message.WorkflowName, message.WorkflowId);
        var natsHeaders = message.InjectHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{message.WorkflowName}-{message.WorkflowId}-{id}-delaystart");
        return new(subject, natsHeaders);
    }

    private static MessageInfo WorkflowDelayTimerMessage(EventMessage message, Guid id, TimeSpan delay, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowDelayTimer(message.WorkflowName, message.WorkflowId);
        var natsHeaders = message.InjectHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{message.WorkflowName}-{message.WorkflowId}-{id}-delaytimer");
        ConnectionHelper.ScheduleDelayedSend(natsHeaders, delay, SubjectHelper.WorkflowDelayEnd(message.WorkflowName, message.WorkflowId));
        return new(subject, natsHeaders);
    }

    internal static async Task StartWorkflowDelayAsync(EventMessage message, TimeSpan delay, INatsConnection connection, INatsJSContext jsContext, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        await ConnectionHelper.PublishMessageAsync(connection, [], WorkflowDelayStartMessage(message, id), cancellationToken);
        await ConnectionHelper.PublishMessageAsync(jsContext, [], WorkflowDelayTimerMessage(message, id, delay), cancellationToken);
    }
}
