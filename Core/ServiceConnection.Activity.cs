using JetFlow.Helpers;
using NATS.Client.Core;
using System.Text;
using static JetFlow.InternalNatsConnection;

namespace JetFlow;

internal partial class ServiceConnection
{
    private NatsHeaders CreateWorkflowActivityStartHeaders(string activityName, uint stepIndex, ActivityExecutionRequest options, byte[] data, NatsHeaders? headers, EventMessage message, TimeSpan? timeout)
    {
        headers??= [];
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
        return headers;
    }

    private PublishMessage CreateWorkflowActivityStartMessage(string activityName, uint stepIndex, byte[] data, NatsHeaders headers, EventMessage message,uint? idx=null)
        => new(
                data,
                subjectMapper.WorkflowStepStart(message.WorkflowName, message.WorkflowId, activityName),
                message.InjectHeaders(headers),
                $"{message.WorkflowName}-{message.WorkflowId}-{activityName}-{stepIndex}-start{idx}"
            );

    private IEnumerable<PublishMessage> CreateActivityStartMessages(string activityName, uint stepIndex, ActivityExecutionRequest options, byte[] data, NatsHeaders headers, EventMessage message, TimeSpan? timeout, uint? idx = null)
    {
        var activityInstanceId = Guid.NewGuid().ToString();
        IEnumerable<PublishMessage> messages = [new InternalNatsConnection.PublishMessage(
                data,
                subjectMapper.ActivityStart(activityName, message.WorkflowName, message.WorkflowId, activityInstanceId),
                message.InjectHeaders(headers),
                $"{message.WorkflowName}-{message.WorkflowId}-{activityName}-{stepIndex}-start{idx}"
            )
        ];
        if (timeout.HasValue)
            messages = messages.Append(InternalNatsConnection.ScheduledPublishMessage.CreateDelayedMessage(
                    data,
                    subjectMapper.ActivityTimer(activityName, message.WorkflowName, message.WorkflowId, activityInstanceId),
                    message.InjectHeaders(headers),
                    $"{message.WorkflowName}-{message.WorkflowId}-{activityName}-{stepIndex}-timer{idx}",
                    timeout.Value,
                    subjectMapper.ActivityTimeout(activityName, message.WorkflowName, message.WorkflowId, activityInstanceId),
                    timeout.Value.Add(options.Timeouts?.AttemptTimeout ?? TimeSpan.Zero)
                ));
        return messages;
    }

    private async ValueTask TransmitStartActivityMessages<TActivity>(uint stepIndex, ActivityExecutionRequest options, byte[] data, NatsHeaders? headers, EventMessage message, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        var activityName = NameHelper.GetActivityName<TActivity>();
        using var activity = TraceHelper.StartWorkflowStep(message, NameHelper.GetActivityName<TActivity>(), stepIndex.ToString());
        headers = CreateWorkflowActivityStartHeaders(activityName, stepIndex, options, data, headers, message, timeout);
        await connection.PublishMessageAsync(CreateWorkflowActivityStartMessage(activityName, stepIndex, data, headers, message), cancellationToken: cancellationToken);
        await connection.PublishMessagesAsync(CreateActivityStartMessages(activityName, stepIndex, options, data, headers, message, timeout), cancellationToken);
    }

    public async ValueTask RetryActivityAsync(RetryTypes retryType, EventMessage message, CancellationToken cancellationToken)
    {
        await connection.PurgeStreamAsync(
            subjectMapper.ActivityQueueStream,
            new()
            {
                Filter=subjectMapper.ActivityTimer(message.ActivityName!, message.WorkflowName, message.WorkflowId, message.ActivityInstanceID!),
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
        List<PublishMessage> messages = [];
        var activityInstanceId = Guid.NewGuid().ToString();
        if (message.RetryConfiguration?.DelayBetween!=null)
            messages.Add(InternalNatsConnection.ScheduledPublishMessage.CreateDelayedMessage(
                        message.Message.Data?? [],
                        subjectMapper.ActivityTimer(message.ActivityName!, message.WorkflowName, message.WorkflowId, activityInstanceId),
                        new(headers.ToDictionary()),
                        $"{message.ActivityName}-{message.WorkflowId}-start-attempt{message.ActivityAttempt}", 
                        message.RetryConfiguration.DelayBetween.Value,
                        subjectMapper.ActivityStart(message.ActivityName!, message.WorkflowName, message.WorkflowId, activityInstanceId),
                        (timeout.HasValue ? timeout.Value.Add(message.RetryConfiguration.DelayBetween.Value) : null)
                    ));
        else
            messages.Add(new InternalNatsConnection.PublishMessage(
                    message.Message.Data?? [],
                    subjectMapper.ActivityStart(message.ActivityName!, message.WorkflowName, message.WorkflowId, activityInstanceId),
                    new(headers.ToDictionary()),
                    $"{message.ActivityName}-{message.WorkflowId}-start-attempt{message.ActivityAttempt}",
                    timeout
                ));
        if (timeout.HasValue)
            messages.Add(InternalNatsConnection.ScheduledPublishMessage.CreateDelayedMessage(
                        message.Message.Data?? [],
                        subjectMapper.ActivityTimer(message.ActivityName!, message.WorkflowName, message.WorkflowId, activityInstanceId),
                        new(headers.ToDictionary()),
                        $"{message.ActivityName}-{message.WorkflowId}-timer-attempt{message.ActivityAttempt}",
                        timeout.Value.Add(message.RetryConfiguration?.DelayBetween.HasValue==true ? message.RetryConfiguration.DelayBetween.Value : TimeSpan.Zero),
                        subjectMapper.ActivityTimeout(message.ActivityName!, message.WorkflowName, message.WorkflowId, activityInstanceId)
                    ));
        await connection.PublishMessagesAsync(messages, cancellationToken);
    }
    public ValueTask StartActivityAsync<TActivity>(uint stepIndex, ActivityExecutionRequest executionRequest, EventMessage message, CancellationToken cancellationToken)
        => TransmitStartActivityMessages<TActivity>(stepIndex, executionRequest, Array.Empty<byte>(), null, message, executionRequest.Timeouts?.OverallTimeout, cancellationToken);
    public async ValueTask StartActivityAsync<TActivity, TInput>(uint stepIndex, ActivityExecutionRequest<TInput> executionRequest, EventMessage message, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput>(executionRequest.Input);
        await TransmitStartActivityMessages<TActivity>(stepIndex, executionRequest, data, headers, message, executionRequest.Timeouts?.OverallTimeout, cancellationToken);
    }

    public async ValueTask StartActivitiesAsync<TActivity, TInput>(uint stepIndex, ActivityExecutionRequest<IEnumerable<TInput>> executionRequest, EventMessage message, CancellationToken cancellationToken)
    {
        var activityName = NameHelper.GetActivityName<TActivity>();
        uint idx = 0;
        var cnt = executionRequest.Input?.Count()??0;
        var workflowMessages = new List<InternalNatsConnection.PublishMessage>();
        var activityMessages = new List<InternalNatsConnection.PublishMessage>();
        foreach(var input in executionRequest.Input??Array.Empty<TInput>())
        {
            var (data, headers) = await messageSerializer.EncodeAsync<TInput>(input);
            headers.Add(Constants.ParalellActivityIndexHeader, idx.ToString());
            headers.Add(Constants.ParallelActivityCountHeader, cnt.ToString());
            headers = CreateWorkflowActivityStartHeaders(activityName, stepIndex, executionRequest, data, headers, message, executionRequest.Timeouts?.OverallTimeout);
            workflowMessages.Add(CreateWorkflowActivityStartMessage(activityName, stepIndex, data, headers, message, idx));
            activityMessages.AddRange(CreateActivityStartMessages(activityName, stepIndex, executionRequest, data, headers, message, executionRequest.Timeouts?.OverallTimeout, idx));
            idx++;
        }
        using var activity = TraceHelper.StartWorkflowStep(message, NameHelper.GetActivityName<TActivity>(), stepIndex.ToString());
        await connection.PublishMessagesAsync(workflowMessages, cancellationToken);
        await connection.PublishMessagesAsync(activityMessages, cancellationToken);
    }

    private static NatsHeaders AppendActivityId(NatsHeaders? headers, EventMessage message)
    {
        headers??=new();
        headers.Add(Constants.ActivityIDHeader, message.ActivityID?.ToString());
        return headers;
    }

    private async ValueTask PublishActivityEndingAsync(EventMessage message, ActivityResultStatus status, byte[] data, NatsHeaders headers, CancellationToken cancellationToken)
    {
        headers.Add(Constants.ActivityResultHeader, status.ToString());
        if (message.ParallelActivityIndex.HasValue)
        {
            headers.Add(Constants.ParalellActivityIndexHeader, message.ParallelActivityIndex.ToString());
            headers.Add(Constants.ParallelActivityCountHeader, message.ParallelActivityCount.ToString());
        }
        await connection.PurgeStreamAsync(
            subjectMapper.ActivityQueueStream,
            new()
            {
                Filter=subjectMapper.ActivityTimer(message.ActivityName!, message.WorkflowName, message.WorkflowId, message.ActivityInstanceID!),
            }, cancellationToken: cancellationToken);
        await connection.PublishMessageAsync(new(
                data,
                subjectMapper.WorkflowStepEnd(message.WorkflowName, message.WorkflowId, message.ActivityName!),
                headers,
                $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-end{message.ParallelActivityIndex}"
            ), cancellationToken);
    }

    public ValueTask TimeoutActivityAsync(EventMessage message, CancellationToken cancellationToken)
        => PublishActivityEndingAsync(
            message,
            ActivityResultStatus.Timeout,
            [],
            message.InjectHeaders(AppendActivityId(null, message)),
            cancellationToken
        );
    public ValueTask ErrorActivityAsync(EventMessage message, Exception error, CancellationToken cancellationToken)
        => PublishActivityEndingAsync(
            message,
            ActivityResultStatus.Failure,
            UTF8Encoding.UTF8.GetBytes(error.Message),
            message.InjectHeaders(AppendActivityId(null, message)),
            cancellationToken
        );
            
    private ValueTask EndActivityAsync(EventMessage message, byte[] data, NatsHeaders? headers, CancellationToken cancellationToken)
    => PublishActivityEndingAsync(
            message,
            ActivityResultStatus.Success,
            data,
            message.InjectHeaders(AppendActivityId(headers, message)),
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
        => $"{message.WorkflowName}/{message.WorkflowId}/{message.ActivityName}/{message.ActivityInstanceID}/attempt{message.ActivityAttempt}";
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
                subjectMapper.WorkflowStepEnd(message.WorkflowName, message.WorkflowId, message.ActivityName!)
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
                        if (message.ParallelActivityIndex.HasValue)
                        {
                            if ((msg.Headers?.TryGetValue(Constants.ParalellActivityIndexHeader, out var parallelIdx)??false) && Equals(message.ParallelActivityIndex.ToString(), parallelIdx.ToString()))
                            {
                                isDone=true;
                                break;
                            }
                        }
                        else
                        {
                            isDone=true;
                            break;
                        }
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
