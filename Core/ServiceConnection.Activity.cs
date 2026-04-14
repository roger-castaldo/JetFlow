using JetFlow.Helpers;
using NATS.Client.Core;
using System.Text;

namespace JetFlow;

internal partial class ServiceConnection
{
    private async ValueTask TransmitStartActivityMessages<TActivity>(byte[] data, NatsHeaders? headers, EventMessage message, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        var activityId = Guid.NewGuid();
        var activityName = NameHelper.GetActivityName<TActivity>();
        using var activity = TraceHelper.StartWorkflowStep(message, NameHelper.GetActivityName<TActivity>(), activityId.ToString());

        await PublishMessageAsync(data,
                subjectMapper.WorkflowStepStart(message.WorkflowName, message.WorkflowId, activityName),
                message.InjectHeaders(headers), 
                $"{message.WorkflowName}-{message.WorkflowId}-{activityName}-{activityId}-start", activityId, cancellationToken: cancellationToken);
        await PublishMessageAsync(data,
                subjectMapper.ActivityStart(activityName, message.WorkflowName, message.WorkflowId),
                message.InjectHeaders(headers),
                $"{activityName}-{message.WorkflowId}-start", activityId, timeout, cancellationToken);
        if (timeout.HasValue)
            await PublishDelayedMessageAsync(data,
                    subjectMapper.ActivityTimer(activityName, message.WorkflowName, message.WorkflowId),
                    message.InjectHeaders(headers),
                    timeout.Value,
                    subjectMapper.ActivityTimeout(activityName, message.WorkflowName, message.WorkflowId),
                    $"{activityName}-{message.WorkflowId}-timer", activityId, cancellationToken: cancellationToken);
    }
    public ValueTask StartActivityAsync<TActivity>(ActivityExecutionRequest executionRequest, EventMessage message, CancellationToken cancellationToken)
        => TransmitStartActivityMessages<TActivity>(Array.Empty<byte>(), null, message, executionRequest.OverallTimeout, cancellationToken);
    public async ValueTask StartActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest, EventMessage message, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput>(executionRequest.Input);
        await TransmitStartActivityMessages<TActivity>(data, headers, message, executionRequest.OverallTimeout, cancellationToken);
    }
    public ValueTask TimeoutActivityAsync(EventMessage message, CancellationToken cancellationToken)
        => PublishMessageAsync([], 
            subjectMapper.WorkflowStepTimeout(message.WorkflowName, message.WorkflowId, message.ActivityName!),
            message.InjectHeaders(null),
            $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-timeout", message.ActivityID, cancellationToken: cancellationToken);
    public ValueTask ErrorActivityAsync(EventMessage message, Exception error, CancellationToken cancellationToken)
        => PublishMessageAsync(UTF8Encoding.UTF8.GetBytes(error.Message), 
            subjectMapper.WorkflowStepError(message.WorkflowName, message.WorkflowId, message.ActivityName!), 
            message.InjectHeaders(null), 
            $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-error", message.ActivityID, cancellationToken: cancellationToken);
    private ValueTask EndActivityAsync(EventMessage message, byte[] data, NatsHeaders? headers, CancellationToken cancellationToken)
        => PublishMessageAsync(data, 
                subjectMapper.WorkflowStepEnd(message.WorkflowName, message.WorkflowId, message.ActivityName!),
                message.InjectHeaders(headers), 
                $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-end", message.ActivityID, cancellationToken: cancellationToken);

    public ValueTask EndActivityAsync(EventMessage message, CancellationToken cancellationToken)
        => EndActivityAsync(message, [], null, cancellationToken);

    public async ValueTask EndActivityAsync<TOutput>(EventMessage message, TOutput output, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TOutput>(output);
        await EndActivityAsync(message, data, headers, cancellationToken);
    }

    #region locks
    private static string GetTimerKey(EventMessage message)
        => $"{message.WorkflowName}/{message.WorkflowId}/{message.ActivityName}";
    public async ValueTask MarkActivityDoneInStore(EventMessage message, CancellationToken cancellationToken)
        => await timerStore.PutAsync<byte[]>($"{GetTimerKey(message)}/done", [], cancellationToken: cancellationToken);
    public async ValueTask<(bool canRun, string activeKey)> CanActivityRun(EventMessage message, CancellationToken cancellationToken)
    {
        var keyBase = GetTimerKey(message);
        var doneValue = await timerStore.TryGetEntryAsync<byte[]>($"{keyBase}/done", cancellationToken: cancellationToken);
        if (doneValue.Success)
            return (false, string.Empty);
        var key = $"{keyBase}/active";
        var value = await timerStore.TryCreateAsync<byte[]>(key, [], cancellationToken: cancellationToken);
        if (value.Success)
        {
            await using var query = await QueryStreamAsync(
                subjectMapper.WorkflowEventsStreamsName,
                true,
                subjectMapper.WorkflowStepEnd(message.WorkflowName, message.WorkflowId, message.ActivityName!),
                subjectMapper.WorkflowStepError(message.WorkflowName, message.WorkflowId, message.ActivityName!),
                subjectMapper.WorkflowStepTimeout(message.WorkflowName, message.WorkflowId, message.ActivityName!)
            );
            var isDone = false;
            var hasAny = true;
            while (hasAny && !isDone)
            {
                hasAny = false;
                await foreach (var msg in query)
                {
                    hasAny=true;
                    if (Equals(message.ActivityID, ServiceConnection.GetActivityID(msg)))
                    {
                        isDone=true;
                        break;
                    }
                }
            }
            return (!isDone, key);
        }
        return (false, key);
    }

    public async ValueTask<ulong> KeepActivityAlive(EventMessage message, ulong revision, CancellationToken cancellationToken)
        => await timerStore.UpdateAsync<byte[]>($"{GetTimerKey(message)}-active", [], revision, cancellationToken: cancellationToken);
    #endregion
}
