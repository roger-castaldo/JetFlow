using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.Core;

namespace JetFlow;

internal partial class ServiceConnection
{
    private async ValueTask<Guid> StartWorkflowAsync<TWorkflow>(byte[] data, NatsHeaders? headers, CancellationToken cancellationToken)
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
        return id;
    }
    public ValueTask<Guid> StartWorkflowAsync<TWorkflow>(CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
        => StartWorkflowAsync<TWorkflow>([], null, cancellationToken);
    public async ValueTask<Guid> StartWorkflowAsync<TWorkflow, TInput>(TInput input, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow<TInput>
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput>(input);
        return await StartWorkflowAsync<TWorkflow>(data, headers, cancellationToken);
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
            subjectMapper.WorkflowDelayStart(message.WorkflowName, message.WorkflowId), 
            message.InjectHeaders(null), 
            $"{message.WorkflowName}-{message.WorkflowId}-{id}-delaystart", cancellationToken: cancellationToken);
        await PublishDelayedMessageAsync([],
                subjectMapper.WorkflowTimer(message.WorkflowName, message.WorkflowId),
                message.InjectHeaders(null),
                delay, 
                subjectMapper.WorkflowDelayEnd(message.WorkflowName, message.WorkflowId),
                $"{message.WorkflowName}-{message.WorkflowId}-{id}-delaytimer", cancellationToken: cancellationToken);
    }
    public ValueTask MarkWorkflowArchived(EventMessage message, CancellationToken cancellationToken)
        => PublishMessageAsync([],
            subjectMapper.WorkflowArchived(message.WorkflowName, message.WorkflowId),
            message.InjectHeaders(null),
            $"{message.WorkflowName}-{message.WorkflowId}-archived",
            cancellationToken: cancellationToken
        );
    public ValueTask MarkWorkflowForPurge(EventMessage message, TimeSpan? purgeDelay, CancellationToken cancellationToken)
        => (purgeDelay.HasValue ?
           PublishDelayedMessageAsync([],
                subjectMapper.WorkflowTimer(message.WorkflowName, message.WorkflowId),
                message.InjectHeaders(null),
                purgeDelay.Value,
                subjectMapper.WorkflowPurge(message.WorkflowName, message.WorkflowId),
                $"{message.WorkflowName}-{message.WorkflowId}-purge",
                cancellationToken: cancellationToken)
           : PublishMessageAsync([],
                subjectMapper.WorkflowPurge(message.WorkflowName, message.WorkflowId),
                message.InjectHeaders(null),
                $"{message.WorkflowName}-{message.WorkflowId}-purge",
                cancellationToken: cancellationToken)
        );
}
