using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.Core;

namespace JetFlow;

internal partial class ServiceConnection
{
    public async ValueTask<Guid> StartWorkflowAsync(string cleanedName, string rawName, Guid id, byte[] data, NatsHeaders? headers, Dictionary<string, string[]>? metaData, byte[]? configData, CancellationToken cancellationToken)
    {
        headers ??= new();
        headers.TryAdd(Constants.WorkflowNameHeader, rawName);
        using var activity = TraceHelper.StartWorkflow(rawName, id.ToString());
        configData ??= InternalsSerializer.SerializeWorkflowOptions(await GetWorkflowOptions(cleanedName));
        await connection.PublishMessagesAsync([
            new InternalNatsConnection.PublishMessage(
                configData,
                subjectMapper.WorkflowConfigure(cleanedName, id.ToString()),
                new(headers.ToDictionary()),
                $"{cleanedName}-{id}-configure"
            ),
            new InternalNatsConnection.PublishMessage(
                data,
                subjectMapper.WorkflowStart(cleanedName, id.ToString()),
                new(MetaDataHelper.EncodeMetaData(metaData, headers).ToDictionary()),
                $"{cleanedName}-{id}-start"
            )
        ], cancellationToken);
        return id;
    }
    private async ValueTask<Guid> StartWorkflowAsync<TWorkflow>(Guid id, byte[] data, WorkflowExecutionRequest? executionRequest, NatsHeaders? headers, CancellationToken cancellationToken)
    {
        var (cleanedName, rawName) = NameHelper.GetWorkflowName<TWorkflow>();
        var configData = InternalsSerializer.SerializeWorkflowOptions(executionRequest?.Options??await GetWorkflowOptions(cleanedName));
        return await StartWorkflowAsync(cleanedName, rawName, id, data, headers, executionRequest?.MetaData, configData, cancellationToken);
    }
    public ValueTask<Guid> StartWorkflowAsync<TWorkflow>(WorkflowExecutionRequest? executionRequest, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
        => StartWorkflowAsync<TWorkflow>(Guid.CreateVersion7(), [], executionRequest, null, cancellationToken);
    public async ValueTask<Guid> StartWorkflowAsync<TWorkflow, TInput>(WorkflowExecutionRequest<TInput> executionRequest, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow<TInput>
    {
        var id = Guid.CreateVersion7();
        var (cleanedName, rawName) = NameHelper.GetWorkflowName<TWorkflow>();
        var (data, headers) = await EncodeMessageAsync<TInput>(executionRequest.Input, cleanedName, id.ToString(), cancellationToken);
        return await StartWorkflowAsync<TWorkflow>(id, data, executionRequest, headers, cancellationToken);
    }

    public async ValueTask ResumeWorkflowAsync<TWorkflow>(Guid id,string? message, CancellationToken cancellationToken)
    {
        var (cleanedName, rawName) = NameHelper.GetWorkflowName<TWorkflow>();
        var (data, headers) = await EncodeMessageAsync<string?>(message, cleanedName, id.ToString(), cancellationToken);
        headers.Add(Constants.WorkflowNameHeader, rawName);
        await connection.PublishMessageAsync(new(
            data,
            subjectMapper.WorkflowResumed(cleanedName, id.ToString()),
            headers,
            $"{cleanedName}-{id}-resume-{Guid.CreateVersion7()}"
            ), 
            cancellationToken);
    }

    public async ValueTask EndWorkflowAsync(EventMessage message, Data.WorkflowEnd workflowEnd, CancellationToken cancellationToken)
    {
        var (data, headers) = await EncodeMessageAsync<Data.WorkflowEnd>(workflowEnd, message.WorkflowSubjectName, message.WorkflowId, cancellationToken);
        await connection.PublishMessageAsync(new(
                data,
                subjectMapper.WorkflowEnd(message.WorkflowSubjectName, message.WorkflowId),
                message.InjectHeaders(headers),
                $"{message.WorkflowSubjectName}-{message.WorkflowId}-end"
            ), cancellationToken: cancellationToken);
    }
    public async ValueTask StartWorkflowDelayAsync(EventMessage message, TimeSpan delay, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        using var activity = TraceHelper.StartDelay(message);
        await connection.PublishMessagesAsync([
            new InternalNatsConnection.PublishMessage(
                [],
                subjectMapper.WorkflowDelayStart(message.WorkflowSubjectName, message.WorkflowId),
                message.InjectHeaders(null),
                $"{message.WorkflowSubjectName}-{message.WorkflowId}-{id}-delaystart"
            ),
            InternalNatsConnection.ScheduledPublishMessage.CreateDelayedMessage(
                [],
                subjectMapper.WorkflowTimer(message.WorkflowSubjectName, message.WorkflowId),
                message.InjectHeaders(null),
                $"{message.WorkflowSubjectName}-{message.WorkflowId}-{id}-delaytimer",
                delay,
                subjectMapper.WorkflowDelayEnd(message.WorkflowSubjectName, message.WorkflowId)
            )
        ], cancellationToken);
    }
    public ValueTask SuspendWorkflowAsync(EventMessage message,int index, CancellationToken cancellationToken)
        => connection.PublishMessageAsync(new(
                [],
                subjectMapper.WorkflowSuspended(message.WorkflowSubjectName, message.WorkflowId),
                message.InjectHeaders(null),
                $"{message.WorkflowSubjectName}-{message.WorkflowId}-suspended-{index}"
            ), cancellationToken: cancellationToken
        );
    public ValueTask MarkWorkflowArchived(EventMessage message, CancellationToken cancellationToken)
        => connection.PublishMessageAsync(new(
                [],
                subjectMapper.WorkflowArchived(message.WorkflowSubjectName, message.WorkflowId),
                message.InjectHeaders(null),
                $"{message.WorkflowSubjectName}-{message.WorkflowId}-archived"
            ), cancellationToken: cancellationToken
        );
    public ValueTask MarkWorkflowForPurge(EventMessage message, TimeSpan? purgeDelay, CancellationToken cancellationToken)
        => (purgeDelay.HasValue ?
           connection.PublishScheduledMessageAsync(InternalNatsConnection.ScheduledPublishMessage.CreateDelayedMessage(
                    [],
                    subjectMapper.DelayWorkflowPurge(message.WorkflowSubjectName, message.WorkflowId),
                    message.InjectHeaders(null),
                    $"{message.WorkflowSubjectName}-{message.WorkflowId}-purge", 
                   purgeDelay.Value,
                   subjectMapper.WorkflowPurge(message.WorkflowSubjectName, message.WorkflowId)
               ),
               cancellationToken: cancellationToken)
           : connection.PublishMessageAsync(new(
                   [],
                   subjectMapper.WorkflowPurge(message.WorkflowSubjectName, message.WorkflowId),
                   message.InjectHeaders(null),
                   $"{message.WorkflowSubjectName}-{message.WorkflowId}-purge"
               ), 
               cancellationToken: cancellationToken)
        );
}
