using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.KeyValueStore;
using System.Text;

namespace JetFlow.Helpers;

internal static class ActivityHelper
{
    private static MessageInfo StartWorkflowStep<TActivity>(EventMessage message, Guid activityId, NatsHeaders? headers = null)
    {
        var activityName = NameHelper.GetActivityName<TActivity>();
        var subject = SubjectHelper.WorkflowStepStart(message.Namespace, message.WorkflowName, message.WorkflowId, activityName);
        var natsHeaders = message.InjectHeaders(headers);   
        ConnectionHelper.AddMessageIds(natsHeaders, $"{message.WorkflowName}-{message.WorkflowId}-{activityName}-{activityId}-start", activityId);
        return new(subject, natsHeaders);
    }

    private static MessageInfo StartActivityMessage<TActivity>(EventMessage message, Guid id, TimeSpan? timeout, NatsHeaders? headers = null)
    {
        var activityName = NameHelper.GetActivityName<TActivity>();
        var subject = SubjectHelper.ActivityStart(message.Namespace, activityName, message.WorkflowName, message.WorkflowId);
        var natsHeaders = message.InjectHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{activityName}-{message.WorkflowId}-start", id);
        if (timeout!=null)
            ConnectionHelper.AddTTL(natsHeaders, timeout.Value);
        return new(subject, natsHeaders);
    }

    private static MessageInfo TimerActivityMessage<TActivity>(EventMessage message, Guid id, TimeSpan timeout, NatsHeaders? headers = null)
    {
        var activityName = NameHelper.GetActivityName<TActivity>();
        var subject = SubjectHelper.ActivityTimer(message.Namespace, activityName, message.WorkflowName, message.WorkflowId);
        var natsHeaders = message.InjectHeaders(headers);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{activityName}-{message.WorkflowId}-timer", id);
        ConnectionHelper.ScheduleDelayedSend(natsHeaders, timeout, SubjectHelper.ActivityTimeout(message.Namespace, activityName, message.WorkflowName, message.WorkflowId));
        return new(subject, natsHeaders);
    }

    private static string GetTimerKey(EventMessage message)
        => $"{message.WorkflowName}/{message.WorkflowId}/{message.ActivityName}";
    private static async ValueTask TransmitStartActivityMessages<TActivity>(byte[] data, NatsHeaders? headers, INatsConnection connection, INatsJSContext jsContext, EventMessage message, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        var activityId = Guid.NewGuid();
        using var activity = TraceHelper.StartWorkflowStep(message, NameHelper.GetActivityName<TActivity>(), activityId.ToString());
        await ConnectionHelper.PublishMessageAsync(connection, [], StartWorkflowStep<TActivity>(message, activityId, headers), cancellationToken);
        await ConnectionHelper.PublishMessageAsync(connection, data, StartActivityMessage<TActivity>(message, activityId, timeout, headers), cancellationToken);
        if (timeout.HasValue)
        {
            TraceHelper.AddActivityTimeout(timeout.Value);
            await ConnectionHelper.PublishMessageAsync(jsContext, [], TimerActivityMessage<TActivity>(message, activityId, timeout.Value, headers), cancellationToken);
        }
    }

    public static async ValueTask MarkActivityDoneInStore(INatsKVStore timerStore, EventMessage message, CancellationToken cancellationToken)
        => await timerStore.PutAsync<byte[]>($"{GetTimerKey(message)}/done", [], cancellationToken: cancellationToken);
    public static async ValueTask<(bool canRun, string activeKey)> CanActivityRun(INatsKVStore timerStore, INatsJSContext jsContext, EventMessage message, CancellationToken cancellationToken)
    {
        var keyBase = GetTimerKey(message);
        var doneValue = await timerStore.TryGetEntryAsync<byte[]>($"{keyBase}/done", cancellationToken: cancellationToken);
        if (doneValue.Success)
            return (false, string.Empty);
        var key = $"{keyBase}/active";
        var value = await timerStore.TryCreateAsync<byte[]>(key, [], cancellationToken: cancellationToken);
        if (value.Success)
        {
            var consumer = await jsContext.CreateOrUpdateConsumerAsync(
                SubjectHelper.WorkflowEventsStreamsName(message.Namespace),
                new ConsumerConfig
                {
                    Name = Guid.NewGuid().ToString(), // ephemeral identity
                    DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                    AckPolicy = ConsumerConfigAckPolicy.None,
                    FilterSubjects = [
                        SubjectHelper.WorkflowStepEnd(message.Namespace, message.WorkflowName, message.WorkflowId, message.ActivityName!),
                        SubjectHelper.WorkflowStepError(message.Namespace, message.WorkflowName, message.WorkflowId, message.ActivityName!),
                        SubjectHelper.WorkflowStepTimeout(message.Namespace, message.WorkflowName, message.WorkflowId, message.ActivityName!)
                    ],
                    HeadersOnly = true,
                    InactiveThreshold = TimeSpan.FromSeconds(1)
                }
            );
            var isDone = false;
            var hasAny = true;
            while (hasAny && !isDone)
            {
                hasAny = false;
                await foreach(var msg in consumer.FetchAsync<byte[]>(new() { MaxMsgs=5, Expires=TimeSpan.FromSeconds(1) }, cancellationToken: cancellationToken))
                {
                    hasAny=true;
                    if (Equals(message.ActivityID, ConnectionHelper.GetActivityID(msg)))
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

    public static async ValueTask<ulong> KeepActivityAlive(INatsKVStore timerStore, EventMessage message, ulong revision, CancellationToken cancellationToken)
        => await timerStore.UpdateAsync<byte[]>($"{GetTimerKey(message)}-active", [], revision, cancellationToken: cancellationToken);


    public static ValueTask StartActivityAsync<TActivity>(ActivityExecutionRequest executionRequest, INatsConnection connection, INatsJSContext jsContext, EventMessage message, CancellationToken cancellationToken)
        => TransmitStartActivityMessages<TActivity>(Array.Empty<byte>(), null, connection, jsContext, message, executionRequest.OverallTimeout, cancellationToken);

    public static async ValueTask StartActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest, INatsConnection connection, INatsJSContext jsContext, MessageSerializer messageSerializer, EventMessage message, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TInput>(executionRequest.Input);
        await TransmitStartActivityMessages<TActivity>(data, headers, connection, jsContext, message, executionRequest.OverallTimeout, cancellationToken);
    }

    public static async ValueTask TimeoutActivityAsync(EventMessage message, INatsConnection connection, CancellationToken cancellationToken)
    {
        var subject = SubjectHelper.WorkflowStepTimeout(message.Namespace, message.WorkflowName, message.WorkflowId, message.ActivityName!);
        var natsHeaders = message.InjectHeaders(null);
        ConnectionHelper.AddMessageIds(natsHeaders, $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-timeout", message.ActivityID);
        await ConnectionHelper.PublishMessageAsync(connection, Array.Empty<byte>(), new MessageInfo(subject, natsHeaders), cancellationToken);
    }

    public static async ValueTask ErrorActivityAsync(EventMessage message, Exception error, INatsConnection connection, CancellationToken cancellationToken)
    {
        var subject = SubjectHelper.WorkflowStepError(message.Namespace, message.WorkflowName, message.WorkflowId, message.ActivityName!);
        var natsHeaders = new NatsHeaders();
        ConnectionHelper.AddMessageIds(natsHeaders, $"{message.WorkflowName}-{message.WorkflowId}-{message.ActivityName}-{message.ActivityID}-error", message.ActivityID);
        await ConnectionHelper.PublishMessageAsync(connection, UTF8Encoding.UTF8.GetBytes(error.Message), new MessageInfo(subject, natsHeaders), cancellationToken);
    }

    private static MessageInfo EndActivity(EventMessage message, NatsHeaders? headers = null)
    {
        var subject = SubjectHelper.WorkflowStepEnd(message.Namespace, message.WorkflowName, message.WorkflowId, message.ActivityName!);
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