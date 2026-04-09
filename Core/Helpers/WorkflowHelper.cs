using NATS.Client.Core;
using NATS.Client.JetStream;

namespace JetFlow.Helpers;

internal static class WorkflowHelper
{
    

    private static MessageInfo StartWorkflow<TWorkflow>(NatsHeaders? headers = null)
    {
        var id = Guid.NewGuid();
        var subject = SubjectHelper.WorkflowStart(NameHelper.GetWorkflowName<TWorkflow>(), id.ToString());
        var natsHeaders = ConnectionHelper.CloneHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{NameHelper.GetWorkflowName<TWorkflow>()}-{id}-start");
        return new(subject, natsHeaders);
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

    private static MessageInfo EndWorkflow<TWorkflow>(string instanceId, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowEnd(NameHelper.GetWorkflowName<TWorkflow>(), instanceId);
        var natsHeaders = ConnectionHelper.CloneHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{NameHelper.GetWorkflowName<TWorkflow>()}-{instanceId}-end");
        return new(subject, natsHeaders);
    }

    public static async ValueTask EndWorkflowAsync<TWorkflow>(INatsConnection connection, MessageSerializer messageSerializer,string instanceId, Messages.WorkflowEnd workflowEnd, CancellationToken cancellationToken)
        where TWorkflow : class
    {
        var (data, headers) = await messageSerializer.EncodeAsync<Messages.WorkflowEnd>(workflowEnd);
        await ConnectionHelper.PublishMessageAsync(connection, data, EndWorkflow<TWorkflow>(instanceId, headers), cancellationToken);
    }

    private static MessageInfo WorkflowDelayStartMessage(string workflowName, string workflowId, Guid id, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowDelayStart(workflowName, workflowId);
        var natsHeaders = ConnectionHelper.CloneHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{workflowName}-{workflowId}-{id}-delaystart");
        return new(subject, natsHeaders);
    }

    private static MessageInfo WorkflowDelayTimerMessage(string workflowName, string workflowId, Guid id, TimeSpan delay, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowDelayTimer(workflowName, workflowId);
        var natsHeaders = ConnectionHelper.CloneHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{workflowName}-{workflowId}-{id}-delaytimer");
        ConnectionHelper.ScheduleDelayedSend(natsHeaders, delay, SubjectHelper.WorkflowDelayEnd(workflowName, workflowId));
        return new(subject, natsHeaders);
    }

    internal static async Task StartWorkflowDelayAsync(string workflowName, string workflowId, TimeSpan delay, INatsConnection connection, INatsJSContext jsContext, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        await ConnectionHelper.PublishMessageAsync(connection, [], WorkflowDelayStartMessage(workflowName, workflowId, id), cancellationToken);
        await ConnectionHelper.PublishMessageAsync(jsContext, [], WorkflowDelayTimerMessage(workflowName, workflowId, id, delay), cancellationToken);
    }
}
