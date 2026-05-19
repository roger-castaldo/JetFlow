using JetFlow.Helpers;
using NATS.Client.Core;
using System.Text;

namespace JetFlow;

internal partial class ServiceConnection
{
    private async ValueTask TransmitStartActivityMessages<TActivity>(uint stepIndex, ActivityExecutionRequest options,byte[] data, NatsHeaders? headers, EventMessage message, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        var activityName = NameHelper.GetActivityName<TActivity>();
        using var activity = TraceHelper.StartWorkflowStep(message, NameHelper.GetActivityName<TActivity>(), stepIndex.ToString());
        headers??=[];
        headers.Add(Constants.ActivityIDHeader, stepIndex.ToString());
        if (options.Retries!=null)
        {
            headers.Add(Constants.ActivityAttemptHeader, "0");
            headers.Add(Constants.ActivityMaximumAttemptsHeader, options.Retries.MaximumAttempts.ToString());
            headers.Add(Constants.ActivityRetryOnTimeoutHeader, options.Retries.RetryOnTimeout.ToString());
            headers.Add(Constants.ActivityRetryOnErrorHeader, options.Retries.RetryOnError.ToString());
            if (options.Retries.DelayBetween.HasValue)
                headers.Add(Constants.ActiviyRetryDelayBetweenHeader, options.Retries.DelayBetween.ToString());
            if (options.Retries.BlockedErrors!=null && options.Retries.BlockedErrors.Length!=0)
                headers.Add(Constants.ActivityRetryBlockedErrorsHeader, options.Retries.BlockedErrors);
        }
        if (options.Timeouts?.AttemptTimeout!=null)
            headers.Add(Constants.ActivityTimeoutHeader, options.Timeouts.AttemptTimeout.ToString());
        if (timeout.HasValue)
            headers.Add(Constants.ActivityOverallTimeoutHeader, timeout.Value.ToString());
        await connection.PublishMessageAsync(new(
                data,
                subjectMapper.WorkflowStepStart(message.WorkflowName, message.WorkflowId, activityName),
                message.InjectHeaders(headers),
                $"{message.WorkflowName}-{message.WorkflowId}-{activityName}-{stepIndex}-start"
            ), cancellationToken: cancellationToken);
        await connection.PublishMessageAsync(new(
                data,
                subjectMapper.ActivityStart(activityName, message.WorkflowName, message.WorkflowId),
                message.InjectHeaders(headers),
                $"{message.WorkflowName}-{message.WorkflowId}-{activityName}-{stepIndex}-start"
            ), cancellationToken);
        if (timeout.HasValue)
            await connection.PublishDelayedMessageAsync(new(
                    data,
                    subjectMapper.ActivityTimer(activityName, message.WorkflowName, message.WorkflowId),
                    message.InjectHeaders(headers),
                    $"{message.WorkflowName}-{message.WorkflowId}-{activityName}-{stepIndex}-timer",
                    timeout.Value,
                    subjectMapper.ActivityTimeout(activityName, message.WorkflowName, message.WorkflowId)
                ),
                cancellationToken: cancellationToken);
    }

    public async ValueTask RetryActivityAsync(RetryTypes retryType, EventMessage message, CancellationToken cancellationToken)
    {
        await connection.PurgeStreamAsync(
            subjectMapper.ActivityQueueStream,
            new()
            {
                Filter=subjectMapper.ActivityTimer(message.ActivityName!, message.WorkflowName, message.WorkflowId),
            }, cancellationToken: cancellationToken);
        await connection.PublishMessageAsync(new(
                UTF8Encoding.UTF8.GetBytes(retryType.ToString()),
                subjectMapper.WorkflowStepRetry(message.WorkflowName, message.WorkflowId, message.ActivityName!),
                message.InjectHeaders(null),
                $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-retry-{message.ActivityAttempt}"
            ), cancellationToken: cancellationToken);
        var headers = message.Message.Headers!
            .Where(pair => !Equals(Constants.ActivityAttemptHeader, pair.Key)
            && pair.Key.Contains("-jetflow-"))
            .Append(new(Constants.ActivityAttemptHeader, (message.ActivityAttempt + 1).ToString()));
        var timeout = (message.Message.Headers?.TryGetValue(Constants.ActivityOverallTimeoutHeader, out var timeoutStr)??false) && TimeSpan.TryParse(timeoutStr, out var timeoutVal) ? timeoutVal : (TimeSpan?)null;
        if(message.RetryConfiguration?.DelayBetween!=null)
            await connection.PublishDelayedMessageAsync(new(
                        message.Message.Data?? [],
                        subjectMapper.ActivityTimer(message.ActivityName!, message.WorkflowName, message.WorkflowId),
                        new(headers.ToDictionary()),
                        $"{message.ActivityName}-{message.WorkflowId}-start-attempt{message.ActivityAttempt}", 
                        message.RetryConfiguration.DelayBetween.Value,
                        subjectMapper.ActivityStart(message.ActivityName!, message.WorkflowName, message.WorkflowId),
                        message.RetryConfiguration.DelayBetween.Value.Add(timeout??TimeSpan.Zero)
                    ),
                    cancellationToken: cancellationToken);
        else
            await connection.PublishMessageAsync(new(
                    message.Message.Data?? [],
                    subjectMapper.ActivityStart(message.ActivityName!, message.WorkflowName, message.WorkflowId),
                    new(headers.ToDictionary()),
                    $"{message.ActivityName}-{message.WorkflowId}-start-attempt{message.ActivityAttempt}",
                    timeout
                ), cancellationToken: cancellationToken);
        if (timeout.HasValue)
            await connection.PublishDelayedMessageAsync(new(
                        message.Message.Data?? [],
                        subjectMapper.ActivityTimer(message.ActivityName!, message.WorkflowName, message.WorkflowId),
                        new(headers.ToDictionary()),
                        $"{message.ActivityName}-{message.WorkflowId}-timer-attempt{message.ActivityAttempt}",
                        timeout.Value.Add(message.RetryConfiguration?.DelayBetween.HasValue==true ? message.RetryConfiguration.DelayBetween.Value : TimeSpan.Zero),
                        subjectMapper.ActivityTimeout(message.ActivityName!, message.WorkflowName, message.WorkflowId)
                    ),
                    cancellationToken: cancellationToken);
    }
    public ValueTask StartActivityAsync<TActivity>(uint stepIndex, ActivityExecutionRequest executionRequest, EventMessage message, CancellationToken cancellationToken)
        => TransmitStartActivityMessages<TActivity>(stepIndex, executionRequest, Array.Empty<byte>(), null, message, executionRequest.Timeouts?.OverallTimeout, cancellationToken);
    public async ValueTask StartActivityAsync<TActivity, TInput>(uint stepIndex, ActivityExecutionRequest<TInput> executionRequest, EventMessage message, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput>(executionRequest.Input);
        await TransmitStartActivityMessages<TActivity>(stepIndex, executionRequest, data, headers, message, executionRequest.Timeouts?.OverallTimeout, cancellationToken);
    }

    private static NatsHeaders AppendActivityId(NatsHeaders? headers, EventMessage message)
    {
        headers??=new();
        headers.Add(Constants.ActivityIDHeader, message.ActivityID?.ToString());
        return headers;
    }

    private async ValueTask PublishActivityEndingAsync(EventMessage message, Func<ValueTask> publishCall, CancellationToken cancellationToken)
    {
        await connection.PurgeStreamAsync(
            subjectMapper.ActivityQueueStream,
            new()
            {
                Filter=subjectMapper.ActivityTimer(message.ActivityName!, message.WorkflowName, message.WorkflowId),
            }, cancellationToken: cancellationToken);
        await publishCall();
    }

    public ValueTask TimeoutActivityAsync(EventMessage message, CancellationToken cancellationToken)
        => PublishActivityEndingAsync(
            message,
            ()=>connection.PublishMessageAsync(new(
                    [],
                    subjectMapper.WorkflowStepTimeout(message.WorkflowName, message.WorkflowId, message.ActivityName!),
                    message.InjectHeaders(AppendActivityId(null, message)),
                    $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-timeout"
                ), 
                cancellationToken: cancellationToken),
            cancellationToken
        );
    public ValueTask ErrorActivityAsync(EventMessage message, Exception error, CancellationToken cancellationToken)
        => PublishActivityEndingAsync(
            message,
            ()=>connection.PublishMessageAsync(new(
                    UTF8Encoding.UTF8.GetBytes(error.Message),
                    subjectMapper.WorkflowStepError(message.WorkflowName, message.WorkflowId, message.ActivityName!),
                    message.InjectHeaders(AppendActivityId(null, message)),
                    $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-error"
                ), 
                cancellationToken: cancellationToken),
            cancellationToken
        );
    private ValueTask EndActivityAsync(EventMessage message, byte[] data, NatsHeaders? headers, CancellationToken cancellationToken)
    => PublishActivityEndingAsync(
            message,
            () => connection.PublishMessageAsync(new(
                    data,
                    subjectMapper.WorkflowStepEnd(message.WorkflowName, message.WorkflowId, message.ActivityName!),
                    message.InjectHeaders(AppendActivityId(headers, message)),
                    $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-end"
                ), 
                cancellationToken: cancellationToken),
            cancellationToken
        );

    public ValueTask EndActivityAsync(EventMessage message, CancellationToken cancellationToken)
        => EndActivityAsync(message, [], null, cancellationToken);

    public async ValueTask EndActivityAsync<TOutput>(EventMessage message, TOutput output, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TOutput>(output);
        await EndActivityAsync(message, data, headers, cancellationToken);
    }

    #region locks
    private static string GetTimerKey(EventMessage message)
        => $"{message.WorkflowName}/{message.WorkflowId}/{message.ActivityName}/{message.ActivityID}/attempt{message.ActivityAttempt}";
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
                    if ((msg.Headers?.TryGetValue(Constants.ActivityIDHeader, out var activityId)??false) && Equals(message.ActivityID, uint.Parse(activityId.ToString())))
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
