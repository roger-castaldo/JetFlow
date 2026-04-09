using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using System.Text;

namespace JetFlow.Helpers;

internal static class ActivityHelper
{
    private static MessageInfo StartWorkflowStep<TActivity>(EventMessage message, Guid activityId, NatsHeaders? headers = null)
    {
        var activityName = NameHelper.GetActivityName<TActivity>();
        var subject = SubjectHelper.WorkflowStepStart(message.WorkflowName, message.WorkflowId, activityName);
        var natsHeaders = message.InjectHeaders(headers);   
        ConnectionHelper.AddMessageIds(natsHeaders, $"{message.WorkflowName}-{message.WorkflowId}-{activityName}-{activityId}-start", activityId);
        return new(subject, natsHeaders);
    }

    private static MessageInfo StartActivityMessage<TActivity>(EventMessage message, Guid id, TimeSpan? timeout, NatsHeaders? headers = null)
    {
        var activityName = NameHelper.GetActivityName<TActivity>();
        var subject = SubjectHelper.ActivityStart(activityName, message.WorkflowName, message.WorkflowId);
        var natsHeaders = message.InjectHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{activityName}-{message.WorkflowId}-start", id);
        if (timeout!=null)
            ConnectionHelper.AddTTL(natsHeaders, timeout.Value);
        return new(subject, natsHeaders);
    }

    private static MessageInfo TimerActivityMessage<TActivity>(EventMessage message, Guid id, TimeSpan timeout, NatsHeaders? headers = null)
    {
        var activityName = NameHelper.GetActivityName<TActivity>();
        var subject = SubjectHelper.ActivityTimer(activityName, message.WorkflowName, message.WorkflowId);
        var natsHeaders = message.InjectHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{activityName}-{message.WorkflowId}-timer", id);
        ConnectionHelper.ScheduleDelayedSend(natsHeaders, timeout, SubjectHelper.ActivityTimeout(activityName, message.WorkflowName, message.WorkflowId));
        return new(subject, natsHeaders);
    }

    private static string GetTimerKey<TActivity>(string workflowName,string workflowId)
        => $"{workflowName}-{workflowId}-{NameHelper.GetActivityName<TActivity>()}";
    private static async ValueTask TransmitStartActivityMessages<TActivity>(byte[] data, NatsHeaders? headers, INatsConnection connection, INatsJSContext jsContext, INatsKVStore timerStore, EventMessage message, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        var activityId = Guid.NewGuid();
        await timerStore.PutAsync(GetTimerKey<TActivity>(message.WorkflowName, message.WorkflowId), Array.Empty<byte>(), cancellationToken: cancellationToken);
        await ConnectionHelper.PublishMessageAsync(connection, [], StartWorkflowStep<TActivity>(message, activityId, headers), cancellationToken);
        await ConnectionHelper.PublishMessageAsync(connection, data, StartActivityMessage<TActivity>(message, activityId, timeout, headers), cancellationToken);
        if (timeout.HasValue)
            await ConnectionHelper.PublishMessageAsync(jsContext, [], TimerActivityMessage<TActivity>(message, activityId, timeout.Value, headers), cancellationToken);
    }

    public static async ValueTask<bool> CanActivityRun<TActivity>(INatsKVStore timerStore, EventMessage message, CancellationToken cancellationToken)
    {
        var key = GetTimerKey<TActivity>(message.WorkflowName, message.WorkflowId);
        var value = await timerStore.TryGetEntryAsync<byte[]>(key, cancellationToken: cancellationToken);
        if (value.Success)
        {
            await timerStore.PurgeAsync(key, cancellationToken: cancellationToken);
            return true;
        }
        return false;
    }

    public static ValueTask StartActivityAsync<TActivity>(ActivityExecutionRequest executionRequest, INatsConnection connection, INatsJSContext jsContext, INatsKVStore timerStore, EventMessage message, CancellationToken cancellationToken)
        => TransmitStartActivityMessages<TActivity>(Array.Empty<byte>(), null, connection, jsContext, timerStore, message, executionRequest.OverallTimeout, cancellationToken);

    public static async ValueTask StartActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest, INatsConnection connection, INatsJSContext jsContext, MessageSerializer messageSerializer, INatsKVStore timerStore, EventMessage message, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput>(executionRequest.Input);
        await TransmitStartActivityMessages<TActivity>(data, headers, connection, jsContext, timerStore, message, executionRequest.OverallTimeout, cancellationToken);
    }

    public static async ValueTask TimeoutActivityAsync(string workflowName, string workflowId, string activityName, Guid activityId, INatsConnection connection, CancellationToken cancellationToken)
    {
        var subject = SubjectHelper.WorkflowStepTimeout(workflowName, workflowId, activityName);
        var natsHeaders = new NatsHeaders();
        ConnectionHelper.AddMessageIds(natsHeaders, $"{workflowName}-{workflowId}-{activityName}-{activityId}-timeout", activityId);
        await ConnectionHelper.PublishMessageAsync(connection, Array.Empty<byte>(), new MessageInfo(subject, natsHeaders), cancellationToken);
    }

    public static async ValueTask ErrorActivityAsync(EventMessage message, Exception error, INatsConnection connection, CancellationToken cancellationToken)
    {
        var subject = SubjectHelper.WorkflowStepError(message.WorkflowName, message.WorkflowId, message.ActivityName!);
        var natsHeaders = new NatsHeaders();
        ConnectionHelper.AddMessageIds(natsHeaders, $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-error", message.ActivityID);
        await ConnectionHelper.PublishMessageAsync(connection, UTF8Encoding.UTF8.GetBytes(error.Message), new MessageInfo(subject, natsHeaders), cancellationToken);
    }

    private static MessageInfo EndActivity(EventMessage message, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowStepEnd(message.WorkflowName, message.WorkflowId, message.ActivityName!);
        var natsHeaders = message.InjectHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-end", message.ActivityID);
        return new(subject, natsHeaders);
    }

    public static ValueTask EndActivityAsync(EventMessage message, INatsConnection connection, CancellationToken cancellationToken)
        => ConnectionHelper.PublishMessageAsync(connection, [], EndActivity(message), cancellationToken);

    public static async ValueTask EndActivityAsync<TOutput>(EventMessage message, TOutput output, MessageSerializer messageSerializer, INatsConnection connection, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TOutput>(output);
        await ConnectionHelper.PublishMessageAsync(connection, data, EndActivity(message, headers), cancellationToken);
    }
}