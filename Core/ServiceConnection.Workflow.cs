using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.Core;
using System.Text;
using System.Xml.Linq;

namespace JetFlow;

internal partial class ServiceConnection
{
    public async ValueTask<Guid> StartWorkflowAsync(string cleanedName, string rawName, Guid id, byte[] data, NatsHeaders? headers, Dictionary<string, string[]>? metaData, byte[]? configData, CancellationToken cancellationToken)
    {
        using var activity = TraceHelper.StartWorkflow(rawName, id.ToString());
        await connection.PublishMessagesAsync(await ProduceStartWorkflowMessagesAsync(cleanedName, rawName, id, data, headers, metaData, configData),
            cancellationToken);
        return id;
    }
    private async ValueTask<IEnumerable<InternalNatsConnection.PublishMessage>> ProduceStartWorkflowMessagesAsync(string cleanedName, string rawName, Guid id, byte[] data, NatsHeaders? headers, Dictionary<string, string[]>? metaData, byte[]? configData)
    {
        headers ??= new();
        headers.TryAdd(Constants.WorkflowNameHeader, rawName);
        configData ??= InternalsSerializer.SerializeWorkflowOptions(await GetWorkflowOptions(cleanedName));
        return [
            new InternalNatsConnection.PublishMessage(
                data,
                subjectMapper.WorkflowStart(cleanedName, id.ToString()),
                new(MetaDataHelper.EncodeMetaData(metaData, headers).ToDictionary()),
                $"{cleanedName}-{id}-start"
            ),
            new InternalNatsConnection.PublishMessage(
                configData,
                subjectMapper.WorkflowConfigure(cleanedName, id.ToString()),
                new(headers.ToDictionary()),
                $"{cleanedName}-{id}-configure"
            )
        ];
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

    private async ValueTask StartChildWorkflowAsync<TWorkflow>(uint activityIndex, EventMessage message, Guid id, byte[] data, WorkflowExecutionRequest? executionRequest, NatsHeaders? headers, CancellationToken cancellationToken)
    {
        var (cleanedName, rawName) = NameHelper.GetWorkflowName<TWorkflow>();
        var (activityCleanedName, activityRawName) = NameHelper.GetChildWorkflowName<TWorkflow>();
        var configData = InternalsSerializer.SerializeWorkflowOptions(executionRequest?.Options??await GetWorkflowOptions(cleanedName));
        using var activity = TraceHelper.StartWorkflow(rawName, id.ToString());
        var messages = await BuildStartChildMessages(activityIndex, message, id, data, headers, cleanedName, rawName, activityCleanedName, activityRawName, configData);
        await connection.PublishMessagesAsync(messages, cancellationToken);
    }

    private async ValueTask<IEnumerable<InternalNatsConnection.PublishMessage>> BuildStartChildMessages(uint activityIndex, EventMessage message, Guid id, byte[] data, NatsHeaders? headers, string cleanedName, string rawName, string activityCleanedName, string activityRawName, byte[] configData)
    {
        var workflowHeaders = new NatsHeaders((headers ?? []).ToDictionary());
        workflowHeaders.TryAdd(Constants.ParentWorkflowIdHeader, message.WorkflowId);
        workflowHeaders.TryAdd(Constants.ParentWorkflowSubjectHeader, message.WorkflowSubjectName);
        workflowHeaders.TryAdd(Constants.ParentWorkflowActivitySubjectHeader, activityCleanedName);
        workflowHeaders.TryAdd(Constants.ParentWorkflowActivityNameHeader, activityRawName);
        workflowHeaders.TryAdd(Constants.ActivityIDHeader, activityIndex.ToString());
        var messages = await ProduceStartWorkflowMessagesAsync(cleanedName, rawName, id, data, workflowHeaders, null, configData);
        headers??= [];
        headers.TryAdd(Constants.ActivityNameHeader, activityRawName);
        headers.TryAdd(Constants.ChildWorkflowNameHeader, rawName);
        headers.TryAdd(Constants.ChildWorkflowIdHeader, id.ToString());
        headers.TryAdd(Constants.ActivityIDHeader, activityIndex.ToString());
        messages = messages.Append(new(
            data,
            subjectMapper.WorkflowStepStart(message.WorkflowSubjectName, message.WorkflowId, activityCleanedName),
            headers,
            $"{message.WorkflowSubjectName}-{message.WorkflowId}-{activityRawName}-{activityIndex}-start{id}"
        ));
        return messages;
    }

    public ValueTask StartChildWorkflowAsync<TWorkflow>(uint activityIndex, EventMessage message, WorkflowExecutionRequest? executionRequest, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
        => StartChildWorkflowAsync<TWorkflow>(activityIndex, message, Guid.CreateVersion7(), [], executionRequest, null, cancellationToken);
    public async ValueTask StartChildWorkflowAsync<TWorkflow, TInput>(uint activityIndex, EventMessage message, WorkflowExecutionRequest<TInput> executionRequest, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow<TInput>
    {
        var id = Guid.CreateVersion7();
        var (cleanedName, rawName) = NameHelper.GetWorkflowName<TWorkflow>();
        var (data, headers) = await EncodeMessageAsync<TInput>(executionRequest.Input, cleanedName, id.ToString(), cancellationToken);
        await StartChildWorkflowAsync<TWorkflow>(activityIndex, message, id, data, executionRequest, headers, cancellationToken);
    }

    public async ValueTask StartChildWorkflowsAsync<TWorkflow, TInput>(uint activityIndex, EventMessage message, WorkflowExecutionRequest<IEnumerable<TInput>> executionRequest, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow<TInput>
    {
        var (cleanedName, rawName) = NameHelper.GetWorkflowName<TWorkflow>();
        var (activityCleanedName, activityRawName) = NameHelper.GetChildWorkflowName<TWorkflow>();
        var configData = InternalsSerializer.SerializeWorkflowOptions(executionRequest?.Options??await GetWorkflowOptions(cleanedName));
        var messages = new List<InternalNatsConnection.PublishMessage>();
        uint idx = 0;
        var cnt = executionRequest?.Input?.Count()??0;
        foreach (var input in executionRequest?.Input?? [])
        {
            var id = Guid.CreateVersion7();
            var (data, headers) = await EncodeMessageAsync<TInput>(input, cleanedName, id.ToString(), cancellationToken);
            headers.Add(Constants.ParalellActivityIndexHeader, idx.ToString());
            headers.Add(Constants.ParallelActivityCountHeader, cnt.ToString());
            messages.AddRange(await BuildStartChildMessages(activityIndex, message, id, data, headers, cleanedName, rawName, activityCleanedName, activityRawName, configData));
            idx++;
        }
        await connection.PublishMessagesAsync(messages, cancellationToken);
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

    public async ValueTask EndWorkflowAsync(EventMessage? startMessage, EventMessage message, Data.WorkflowEnd workflowEnd, CancellationToken cancellationToken)
    {
        var (data, headers) = await EncodeMessageAsync<Data.WorkflowEnd>(workflowEnd, message.WorkflowSubjectName, message.WorkflowId, cancellationToken);
        IEnumerable<InternalNatsConnection.PublishMessage> messages = [
            new(
                data,
                subjectMapper.WorkflowEnd(message.WorkflowSubjectName, message.WorkflowId),
                message.InjectHeaders(headers),
                $"{message.WorkflowSubjectName}-{message.WorkflowId}-end"
            )
        ];
        if (!string.IsNullOrWhiteSpace(startMessage?.ParentWorkflowId) && !string.IsNullOrEmpty(startMessage?.ParentWorkflowSubject))
        {
            NatsHeaders endHeaders = [
                new(Constants.ActivityNameHeader, startMessage.ParentWorkflowActivityName),
                new(Constants.ActivityIDHeader, startMessage.ActivityID?.ToString())
            ];
            if (startMessage.ParallelActivityIndex.HasValue)
            {
                endHeaders.Add(Constants.ParalellActivityIndexHeader, startMessage.ParallelActivityIndex?.ToString());
                endHeaders.Add(Constants.ParallelActivityCountHeader, startMessage.ParallelActivityCount?.ToString());
            }
            messages = messages.Append(new(
                UTF8Encoding.UTF8.GetBytes((workflowEnd.IsSuccess ? WorkflowResult.Success : WorkflowResult.Failure).ToString()),
                subjectMapper.WorkflowStepEnd(startMessage.ParentWorkflowSubject, startMessage.ParentWorkflowId, startMessage.ParentWorkflowActivitySubject!),
                endHeaders,
                $"child-{message.WorkflowSubjectName}-{message.WorkflowId}-end"
            ));
        }
        await connection.PublishMessagesAsync(messages, cancellationToken: cancellationToken);
    }
    public async ValueTask StartWorkflowDelayAsync(EventMessage message, TimeSpan delay, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        using var activity = TraceHelper.StartDelay(message);
        await connection.PublishMessagesAsync([
            new InternalNatsConnection.PublishMessage(
                System.Text.UTF8Encoding.UTF8.GetBytes(delay.ToString()),
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
                System.Text.UTF8Encoding.UTF8.GetBytes(WorkflowHelper.WorkflowArchivePath(message)),
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
