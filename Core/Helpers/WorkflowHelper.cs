using JetFlow.Messages;
using NATS.Client.Core;

namespace JetFlow.Helpers;

internal static class WorkflowHelper
{
    private record struct MessageInfo(string Subject, NatsHeaders Headers);

    private static async ValueTask PublishMessageAsync(NatsConnection connection, byte[] data, MessageInfo messageInfo, CancellationToken cancellationToken)
        => await connection.PublishAsync<byte[]>(messageInfo.Subject, data, messageInfo.Headers, cancellationToken: cancellationToken);

    private static MessageInfo StartWorkflow<TWorkflow>(NatsHeaders? headers = null)
    {
        var id = Guid.NewGuid();
        var subject = SubjectHelper.WorkflowStart(typeof(TWorkflow).Name, id.ToString());
        var natsHeaders = headers ?? new NatsHeaders();
        natsHeaders.Add(Constants.MessageIdHeader, $"{typeof(TWorkflow).Name}-{id}-start");
        return new(subject, natsHeaders);
    }

    public static ValueTask StartWorkflowAsync<TWorkflow>(NatsConnection connection, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
        => PublishMessageAsync(connection, Array.Empty<byte>(), StartWorkflow<TWorkflow>(), cancellationToken);

    public static async ValueTask StartWorkflowAsync<TWorkflow, TInput>(NatsConnection connection, MessageSerializer messageSerializer, TInput input, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow<TInput>
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput>(input);
        await PublishMessageAsync(connection, data, StartWorkflow<TWorkflow>(headers), cancellationToken);
    }

    public static async ValueTask StartWorkflowAsync<TWorkflow, TInput1, TInput2>(NatsConnection connection, MessageSerializer messageSerializer, TInput1 input1, TInput2 input2, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow<TInput1, TInput2>
    {
        var (data, headers) = await messageSerializer.EncodeAsync<(TInput1, TInput2)>((input1, input2));
        await PublishMessageAsync(connection, data, StartWorkflow<TWorkflow>(headers), cancellationToken);
    }

    public static async ValueTask StartWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3>(NatsConnection connection, MessageSerializer messageSerializer, TInput1 input1, TInput2 input2, TInput3 input3, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow<TInput1, TInput2, TInput3>
    {
        var (data, headers) = await messageSerializer.EncodeAsync<(TInput1, TInput2, TInput3)>((input1, input2, input3));
        await PublishMessageAsync(connection, data, StartWorkflow<TWorkflow>(headers), cancellationToken);
    }

    public static async ValueTask StartWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3, TInput4>(NatsConnection connection, MessageSerializer messageSerializer, TInput1 input1, TInput2 input2, TInput3 input3, TInput4 input4, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow<TInput1, TInput2, TInput3, TInput4>
    {
        var (data, headers) = await messageSerializer.EncodeAsync<(TInput1, TInput2, TInput3, TInput4)>((input1, input2, input3, input4));
        await PublishMessageAsync(connection, data, StartWorkflow<TWorkflow>(headers), cancellationToken);
    }

    private static MessageInfo EndWorkflow<TWorkflow>(string instanceId, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowEnd(typeof(TWorkflow).Name, instanceId);
        var natsHeaders = headers ?? new NatsHeaders();
        natsHeaders.Add(Constants.MessageIdHeader, $"{typeof(TWorkflow).Name}-{instanceId}-end");
        return new(subject, natsHeaders);
    }

    public static async ValueTask EndWorkflowAsync<TWorkflow>(NatsConnection connection, MessageSerializer messageSerializer,string instanceId, Messages.WorkflowEnd workflowEnd, CancellationToken cancellationToken)
        where TWorkflow : class
    {
        var (data, headers) = await messageSerializer.EncodeAsync<Messages.WorkflowEnd>(workflowEnd);
        await PublishMessageAsync(connection, data, EndWorkflow<TWorkflow>(instanceId, headers), cancellationToken);
    }
}
