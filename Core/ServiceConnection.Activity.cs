using JetFlow.Helpers;
using NATS.Client.Core;
using System.Text;

namespace JetFlow;

internal partial class ServiceConnection
{
    private async ValueTask TransmitStartActivityMessages<TActivity>(ActivityOptions options,byte[] data, NatsHeaders? headers, EventMessage message, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        var activityId = Guid.NewGuid();
        var activityName = NameHelper.GetActivityName<TActivity>();
        using var activity = TraceHelper.StartWorkflowStep(message, NameHelper.GetActivityName<TActivity>(), activityId.ToString());
        headers??=new();
        if (options.Retries!=null)
        {
            headers.Add(Constants.ActivityAttemptHeader, "0");
            headers.Add(Constants.ActivityMaximumAttemptsHeader, options.Retries.MaximumAttempts.ToString());
            headers.Add(Constants.ActivityRetryOnTimeoutHeader, options.Retries.RetryOnTimeout.ToString());
            headers.Add(Constants.ActivityRetryOnErrorHeader, options.Retries.RetryOnError.ToString());
            if (options.Retries.DelayBetween.HasValue)
                headers.Add(Constants.ActiviyRetryDelayBetweenHeader, options.Retries.DelayBetween.ToString());
            if (options.Retries.BlockedErrors!=null && options.Retries.BlockedErrors.Any())
                headers.Add(Constants.ActivityRetryBlockedErrorsHeader, options.Retries.BlockedErrors);
        }
        if (options.Timeouts?.AttemptTimeout!=null)
            headers.Add(Constants.ActivityTimeoutHeader, options.Timeouts.AttemptTimeout.ToString());
        if (timeout.HasValue)
            headers.Add(Constants.ActivityOverallTimeoutHeader, timeout.Value.ToString());
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

    public async ValueTask RetryActivityAsync(RetryTypes retryType, EventMessage message, CancellationToken cancellationToken)
    {
        await jsContext.PurgeStreamAsync(
            subjectMapper.ActivityQueueStream,
            new()
            {
                Filter=subjectMapper.ActivityTimer(message.ActivityName!, message.WorkflowName, message.WorkflowId),
            }, cancellationToken: cancellationToken);
        await PublishMessageAsync(UTF8Encoding.UTF8.GetBytes(retryType.ToString()), 
            subjectMapper.WorkflowStepRetry(message.WorkflowName, message.WorkflowId, message.ActivityName!), 
            message.InjectHeaders(null), 
            $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-retry-{message.ActivityAttempt}", message.ActivityID, cancellationToken: cancellationToken);
        var headers = message.Message.Headers!
            .Where(pair => !Equals(Constants.ActivityAttemptHeader, pair.Key)
            && pair.Key.Contains("-jetflow-"))
            .Append(new(Constants.ActivityAttemptHeader, (message.ActivityAttempt + 1).ToString()));
        var timeout = message.Message.Headers.TryGetValue(Constants.ActivityOverallTimeoutHeader, out var timeoutStr) && TimeSpan.TryParse(timeoutStr, out var timeoutVal) ? timeoutVal : (TimeSpan?)null;
        if(message.RetryConfiguration?.DelayBetween!=null)
            await PublishDelayedMessageAsync(message.Message.Data?? [],
                    subjectMapper.ActivityTimer(message.ActivityName!, message.WorkflowName, message.WorkflowId),
                    new(headers.ToDictionary()),
                    message.RetryConfiguration.DelayBetween.Value,
                    subjectMapper.ActivityStart(message.ActivityName!, message.WorkflowName, message.WorkflowId),
                    $"{message.ActivityName}-{message.WorkflowId}-start-attempt{message.ActivityAttempt}", timeout: message.RetryConfiguration.DelayBetween.Value.Add(timeout??TimeSpan.Zero), cancellationToken: cancellationToken);
        else
            await PublishMessageAsync(message.Message.Data?? [],
                    subjectMapper.ActivityStart(message.ActivityName!, message.WorkflowName, message.WorkflowId),
                    new(headers.ToDictionary()),
                    $"{message.ActivityName}-{message.WorkflowId}-start-attempt{message.ActivityAttempt}", timeout: timeout, cancellationToken: cancellationToken);
        if (timeout.HasValue)
            await PublishDelayedMessageAsync(message.Message.Data?? [],
                    subjectMapper.ActivityTimer(message.ActivityName!, message.WorkflowName, message.WorkflowId),
                    new(headers.ToDictionary()),
                    timeout.Value.Add(message.RetryConfiguration?.DelayBetween.HasValue==true ? message.RetryConfiguration.DelayBetween.Value : TimeSpan.Zero),
                    subjectMapper.ActivityTimeout(message.ActivityName!, message.WorkflowName, message.WorkflowId),
                    $"{message.ActivityName}-{message.WorkflowId}-timer-attempt{message.ActivityAttempt}", cancellationToken: cancellationToken);
    }
    public ValueTask StartActivityAsync<TActivity>(ActivityExecutionRequest executionRequest, EventMessage message, CancellationToken cancellationToken)
        => TransmitStartActivityMessages<TActivity>(executionRequest, Array.Empty<byte>(), null, message, executionRequest.Timeouts?.OverallTimeout, cancellationToken);
    public async ValueTask StartActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest, EventMessage message, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput>(executionRequest.Input);
        await TransmitStartActivityMessages<TActivity>(executionRequest, data, headers, message, executionRequest.Timeouts?.OverallTimeout, cancellationToken);
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
        => $"{message.WorkflowName}/{message.WorkflowId}/{message.ActivityName}/attempt{message.ActivityAttempt}";
    public async ValueTask MarkActivityDoneInStore(EventMessage message, CancellationToken cancellationToken)
    {
        await timerStore.PutAsync<byte[]>($"{GetTimerKey(message)}/done", [], cancellationToken: cancellationToken);
        await timerStore.DeleteAsync($"{GetTimerKey(message)}/active", cancellationToken: cancellationToken);
    }
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
