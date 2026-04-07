using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using System.Text;

namespace JetFlow.Helpers;

internal static class ActivityHelper
{
    private static MessageInfo StartWorkflowStep<TActivity>(string workflowName, string workflowId, Guid activityId, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowStepStart(workflowName, workflowId, NameHelper.GetActivityName<TActivity>());
        var natsHeaders = ConnectionHelper.CloneHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{workflowName}-{workflowId}-{NameHelper.GetActivityName<TActivity>()}-{activityId}-start", activityId);
        return new(subject, natsHeaders);
    }

    private static MessageInfo StartActivityMessage<TActivity>(string workflowName, string workflowId, Guid id, TimeSpan? timeout, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.ActivityStart(NameHelper.GetActivityName<TActivity>(), workflowName, workflowId);
        var natsHeaders = ConnectionHelper.CloneHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{NameHelper.GetActivityName<TActivity>()}-{workflowId}-start", id);
        if (timeout!=null)
            ConnectionHelper.AddTTL(natsHeaders, timeout.Value);
        return new(subject, natsHeaders);
    }

    private static MessageInfo TimerActivityMessage<TActivity>(string workflowName, string workflowId, Guid id, TimeSpan timeout, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.ActivityTimer(NameHelper.GetActivityName<TActivity>(), workflowName, workflowId);
        var natsHeaders = ConnectionHelper.CloneHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{NameHelper.GetActivityName<TActivity>()}-{workflowId}-timer", id);
        ConnectionHelper.ScheduleDelayedSend(natsHeaders, timeout, SubjectHelper.ActivityTimeout(NameHelper.GetActivityName<TActivity>(), workflowName, workflowId));
        return new(subject, natsHeaders);
    }

    private static string GetTimerKey<TActivity>(string workflowName,string workflowId)
        => $"{workflowName}-{workflowId}-{NameHelper.GetActivityName<TActivity>()}";
    private static async ValueTask TransmitStartActivityMessages<TActivity>(byte[] data, NatsHeaders? headers, INatsConnection connection, INatsJSContext jsContext, INatsKVStore timerStore, string workflowName, string workflowId, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        var activityId = Guid.NewGuid();
        await timerStore.PutAsync(GetTimerKey<TActivity>(workflowName, workflowId), Array.Empty<byte>(), cancellationToken: cancellationToken);
        await ConnectionHelper.PublishMessageAsync(connection, [], StartWorkflowStep<TActivity>(workflowName, workflowId, activityId, headers), cancellationToken);
        await ConnectionHelper.PublishMessageAsync(connection, data, StartActivityMessage<TActivity>(workflowName, workflowId, activityId, timeout, headers), cancellationToken);
        if (timeout.HasValue)
            await ConnectionHelper.PublishMessageAsync(jsContext, [], TimerActivityMessage<TActivity>(workflowName, workflowId, activityId, timeout.Value, headers), cancellationToken);
    }

    public static async ValueTask<bool> CanActivityRun<TActivity>(INatsKVStore timerStore, string workflowName, string workflowId, CancellationToken cancellationToken)
    {
        var key = GetTimerKey<TActivity>(workflowName, workflowId);
        var value = await timerStore.TryGetEntryAsync<byte[]>(key, cancellationToken: cancellationToken);
        if (value.Success)
        {
            await timerStore.PurgeAsync(key, cancellationToken: cancellationToken);
            return true;
        }
        return false;
    }

    public static ValueTask StartActivityAsync<TActivity>(ActivityExecutionRequest executionRequest, INatsConnection connection, INatsJSContext jsContext, INatsKVStore timerStore, string workflowName, string workflowId, CancellationToken cancellationToken)
        => TransmitStartActivityMessages<TActivity>(Array.Empty<byte>(), null, connection, jsContext, timerStore, workflowName, workflowId, executionRequest.OverallTimeout, cancellationToken);

    public static async ValueTask StartActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest, INatsConnection connection, INatsJSContext jsContext, MessageSerializer messageSerializer, INatsKVStore timerStore, string workflowName, string workflowId, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput>(executionRequest.Input);
        await TransmitStartActivityMessages<TActivity>(data, headers, connection, jsContext, timerStore, workflowName, workflowId, executionRequest.OverallTimeout, cancellationToken);
    }

    public static async ValueTask StartActivityAsync<TActivity, TInput1, TInput2>(ActivityExecutionRequest<TInput1, TInput2> executionRequest, INatsConnection connection, INatsJSContext jsContext, MessageSerializer messageSerializer, INatsKVStore timerStore, string workflowName, string workflowId, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput1, TInput2>(executionRequest.Input1, executionRequest.Input2);
        await TransmitStartActivityMessages<TActivity>(data, headers, connection, jsContext, timerStore, workflowName, workflowId, executionRequest.OverallTimeout, cancellationToken);
    }

    public static async ValueTask StartActivityAsync<TActivity, TInput1, TInput2, TInput3>(ActivityExecutionRequest<TInput1, TInput2, TInput3> executionRequest, INatsConnection connection, INatsJSContext jsContext, MessageSerializer messageSerializer, INatsKVStore timerStore, string workflowName, string workflowId, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput1, TInput2, TInput3>(executionRequest.Input1, executionRequest.Input2, executionRequest.Input3);
        await TransmitStartActivityMessages<TActivity>(data, headers, connection, jsContext, timerStore, workflowName, workflowId, executionRequest.OverallTimeout, cancellationToken);
    }

    public static async ValueTask StartActivityAsync<TActivity, TInput1, TInput2, TInput3, TInput4>(ActivityExecutionRequest<TInput1, TInput2, TInput3, TInput4> executionRequest, INatsConnection connection, INatsJSContext jsContext, MessageSerializer messageSerializer, INatsKVStore timerStore, string workflowName, string workflowId, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput1, TInput2, TInput3, TInput4>(executionRequest.Input1, executionRequest.Input2, executionRequest.Input3, executionRequest.Input4);
        await TransmitStartActivityMessages<TActivity>(data, headers, connection, jsContext, timerStore, workflowName, workflowId, executionRequest.OverallTimeout, cancellationToken);
    }

    public static async ValueTask TimeoutActivityAsync(string workflowName, string workflowId, string activityName, Guid activityId, INatsConnection connection, CancellationToken cancellationToken)
    {
        var subject = SubjectHelper.WorkflowStepTimeout(workflowName, workflowId, activityName);
        var natsHeaders = new NatsHeaders();
        ConnectionHelper.AddMessageIds(natsHeaders, $"{workflowName}-{workflowId}-{activityName}-{activityId}-timeout", activityId);
        await ConnectionHelper.PublishMessageAsync(connection, Array.Empty<byte>(), new MessageInfo(subject, natsHeaders), cancellationToken);
    }

    public static async ValueTask ErrorActivityAsync(string workflowName, string workflowId, string activityName, Guid activityId, Exception error, INatsConnection connection, CancellationToken cancellationToken)
    {
        var subject = SubjectHelper.WorkflowStepError(workflowName, workflowId, activityName);
        var natsHeaders = new NatsHeaders();
        ConnectionHelper.AddMessageIds(natsHeaders, $"{workflowName}-{workflowId}-{activityName}-{activityId}-error", activityId);
        await ConnectionHelper.PublishMessageAsync(connection, UTF8Encoding.UTF8.GetBytes(error.Message), new MessageInfo(subject, natsHeaders), cancellationToken);
    }

    private static MessageInfo EndActivity(string workflowName, string workflowId, string activityName, Guid activityId, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowStepEnd(workflowName, workflowId, activityName);
        var natsHeaders = ConnectionHelper.CloneHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{workflowName}-{workflowId}-{activityName}-{activityId}-end", activityId);
        return new(subject, natsHeaders);
    }

    public static ValueTask EndActivityAsync(string workflowName, string workflowId, string activityName, Guid activityId, INatsConnection connection, CancellationToken cancellationToken)
        => ConnectionHelper.PublishMessageAsync(connection, Array.Empty<byte>(), EndActivity(workflowName, workflowId, activityName, activityId), cancellationToken);

    public static async ValueTask EndActivityAsync<TOutput>(string workflowName, string workflowId, string activityName, Guid activityId, TOutput output, MessageSerializer messageSerializer, INatsConnection connection, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TOutput>(output);
        await ConnectionHelper.PublishMessageAsync(connection, data, EndActivity(workflowName, workflowId, activityName, activityId, headers), cancellationToken);
    }
}