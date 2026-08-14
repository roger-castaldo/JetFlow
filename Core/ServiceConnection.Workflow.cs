using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.Core;

namespace JetFlow;

internal partial class ServiceConnection
{
    public async ValueTask<Guid> StartWorkflowAsync(string name, Guid id, byte[] data, NatsHeaders? headers, byte[]? configData, CancellationToken cancellationToken)
    {
        headers ??= new();
        using var activity = TraceHelper.StartWorkflow(name, id.ToString());
        configData ??= InternalsSerializer.SerializeWorkflowOptions(await GetWorkflowOptions(name));
        await connection.PublishMessagesAsync([
            new InternalNatsConnection.PublishMessage(
                configData,
                subjectMapper.WorkflowConfigure(name, id.ToString()),
                new(headers.ToDictionary()),
                $"{name}-{id}-configure"
            ),
            new InternalNatsConnection.PublishMessage(
                data,
                subjectMapper.WorkflowStart(name, id.ToString()),
                new(headers.ToDictionary()),
                $"{name}-{id}-start"
            )
        ], cancellationToken);
        return id;
    }
    private async ValueTask<Guid> StartWorkflowAsync<TWorkflow>(Guid id, byte[] data, NatsHeaders? headers, CancellationToken cancellationToken)
    {
        var name = NameHelper.GetWorkflowName<TWorkflow>();
        var configData = InternalsSerializer.SerializeWorkflowOptions(await GetWorkflowOptions(name));
        return await StartWorkflowAsync(name, id, data, headers, configData, cancellationToken);
    }
    public ValueTask<Guid> StartWorkflowAsync<TWorkflow>(CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
        => StartWorkflowAsync<TWorkflow>(Guid.NewGuid(), [], null, cancellationToken);
    public async ValueTask<Guid> StartWorkflowAsync<TWorkflow, TInput>(TInput input, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow<TInput>
    {
        var id = Guid.NewGuid();
        var (data, headers) = await EncodeMessageAsync<TInput>(input, NameHelper.GetWorkflowName<TWorkflow>(), id.ToString(), cancellationToken);
        return await StartWorkflowAsync<TWorkflow>(id, data, headers, cancellationToken);
    }
    public async ValueTask EndWorkflowAsync(EventMessage message, Messages.WorkflowEnd workflowEnd, CancellationToken cancellationToken)
    {
        var (data, headers) = await EncodeMessageAsync<Messages.WorkflowEnd>(workflowEnd, message.WorkflowName, message.WorkflowId, cancellationToken);
        await connection.PublishMessageAsync(new(
                data,
                subjectMapper.WorkflowEnd(message.WorkflowName, message.WorkflowId),
                message.InjectHeaders(headers),
                $"{message.WorkflowName}-{message.WorkflowId}-end"
            ), cancellationToken: cancellationToken);
    }
    public async ValueTask StartWorkflowDelayAsync(EventMessage message, TimeSpan delay, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        using var activity = TraceHelper.StartDelay(message);
        await connection.PublishMessagesAsync([
            new InternalNatsConnection.PublishMessage(
                [],
                subjectMapper.WorkflowDelayStart(message.WorkflowName, message.WorkflowId),
                message.InjectHeaders(null),
                $"{message.WorkflowName}-{message.WorkflowId}-{id}-delaystart"
            ),
            InternalNatsConnection.ScheduledPublishMessage.CreateDelayedMessage(
                [],
                subjectMapper.WorkflowTimer(message.WorkflowName, message.WorkflowId),
                message.InjectHeaders(null),
                $"{message.WorkflowName}-{message.WorkflowId}-{id}-delaytimer",
                delay,
                subjectMapper.WorkflowDelayEnd(message.WorkflowName, message.WorkflowId)
            )
        ], cancellationToken);
    }
    public ValueTask MarkWorkflowArchived(EventMessage message, CancellationToken cancellationToken)
        => connection.PublishMessageAsync(new(
                [],
                subjectMapper.WorkflowArchived(message.WorkflowName, message.WorkflowId),
                message.InjectHeaders(null),
                $"{message.WorkflowName}-{message.WorkflowId}-archived"
            ), cancellationToken: cancellationToken
        );
    public ValueTask MarkWorkflowForPurge(EventMessage message, TimeSpan? purgeDelay, CancellationToken cancellationToken)
        => (purgeDelay.HasValue ?
           connection.PublishScheduledMessageAsync(InternalNatsConnection.ScheduledPublishMessage.CreateDelayedMessage(
                    [],
                    subjectMapper.DelayWorkflowPurge(message.WorkflowName, message.WorkflowId),
                    message.InjectHeaders(null),
                    $"{message.WorkflowName}-{message.WorkflowId}-purge", 
                   purgeDelay.Value,
                   subjectMapper.WorkflowPurge(message.WorkflowName, message.WorkflowId)
               ),
               cancellationToken: cancellationToken)
           : connection.PublishMessageAsync(new(
                   [],
                   subjectMapper.WorkflowPurge(message.WorkflowName, message.WorkflowId),
                   message.InjectHeaders(null),
                   $"{message.WorkflowName}-{message.WorkflowId}-purge"
               ), 
               cancellationToken: cancellationToken)
        );
}
