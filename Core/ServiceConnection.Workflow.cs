using JetFlow.Helpers;
using JetFlow.Serializers;
using NATS.Client.Core;

namespace JetFlow;

internal partial class ServiceConnection
{
    private async ValueTask StartWorkflowAsync<TWorkflow>(byte[] data, NatsHeaders? headers, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var name = NameHelper.GetWorkflowName<TWorkflow>();
        headers ??= new();
        using var activity = TraceHelper.StartWorkflow(name, id.ToString());
        await PublishMessageAsync(InternalsSerializer.SerializeWorkflowOptions(await GetWorkflowOptions(name)), 
            subjectMapper.WorkflowConfigure(name, id.ToString()), 
            new(headers.ToDictionary()), 
            $"{name}-{id}-configure", cancellationToken: cancellationToken);
        await PublishMessageAsync(data, 
            subjectMapper.WorkflowStart(name, id.ToString()),
            new(headers.ToDictionary()), 
            $"{name}-{id}-start", cancellationToken: cancellationToken);
    }
    public ValueTask StartWorkflowAsync<TWorkflow>(CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
        => StartWorkflowAsync<TWorkflow>([], null, cancellationToken);
    public async ValueTask StartWorkflowAsync<TWorkflow, TInput>(TInput input, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow<TInput>
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput>(input);
        await StartWorkflowAsync<TWorkflow>(data, headers, cancellationToken);
    }
    public async ValueTask EndWorkflowAsync(EventMessage message, Messages.WorkflowEnd workflowEnd, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<Messages.WorkflowEnd>(workflowEnd);
        await PublishMessageAsync(data, 
                subjectMapper.WorkflowEnd(message.WorkflowName, message.WorkflowId),
                message.InjectHeaders(headers), 
                $"{message.WorkflowName}-{message.WorkflowId}-end", cancellationToken: cancellationToken);
    }
    public async ValueTask StartWorkflowDelayAsync(EventMessage message, TimeSpan delay, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        using var activity = TraceHelper.StartDelay(message);
        await PublishMessageAsync([], 
            subjectMapper.WorkflowDelayStart(message.WorkflowId, message.WorkflowId), 
            message.InjectHeaders(null), 
            $"{message.WorkflowName}-{message.WorkflowId}-{id}-delaystart", cancellationToken: cancellationToken);
        await PublishDelayedMessageAsync([],
                subjectMapper.WorkflowDelayTimer(message.WorkflowName, message.WorkflowId),
                message.InjectHeaders(null),
                delay, 
                subjectMapper.WorkflowDelayEnd(message.WorkflowName, message.WorkflowId),
                $"{message.WorkflowName}-{message.WorkflowId}-{id}-delaytimer", cancellationToken: cancellationToken);
    }
}
