using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.KeyValueStore;
using System.Diagnostics;
using System.Xml.Linq;

namespace JetFlow;

internal class WorkflowContext(INatsConnection connection, INatsJSContext jsContext, MessageSerializer messageSerializer, INatsKVStore timerStore, 
    EventMessage message) 
    : IWorkflowContext
{
    private IReadOnlyCollection<INatsJSMsg<byte[]>> messages = [];
    private int index = 0;
    private readonly CancellationToken cancellationToken = CancellationToken.None;
    public INatsJSMsg<byte[]> StartMessage {  get; set; }
    internal async ValueTask<WorkflowContext> LoadAsync()
    {
        var consumer = await jsContext.CreateOrUpdateConsumerAsync(
            SubjectHelper.WorkflowEventsStreamsName,
            new ConsumerConfig
            {
                Name = Guid.NewGuid().ToString(), // ephemeral identity
                DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                AckPolicy = ConsumerConfigAckPolicy.None,
                FilterSubjects = [
                    SubjectHelper.WorkflowStart(message.WorkflowName, message.WorkflowId),
                    SubjectHelper.WorkflowEnd(message.WorkflowName, message.WorkflowId),
                    SubjectHelper.WorkflowDelayEnd(message.WorkflowName, message.WorkflowId),
                    SubjectHelper.WorkflowStepEnd(message.WorkflowName, message.WorkflowId, "*"),
                    SubjectHelper.WorkflowStepError(message.WorkflowName, message.WorkflowId, "*"),
                    SubjectHelper.WorkflowStepTimeout(message.WorkflowName, message.WorkflowId, "*")
                ]
            }
        );
        var msgs = new List<INatsJSMsg<byte[]>>();
        await foreach(var msg in consumer.FetchAsync<byte[]>(new() { MaxMsgs=5, Expires=TimeSpan.FromSeconds(1)  }))
        {
            if (Equals(SubjectHelper.WorkflowStart(message.WorkflowName, message.WorkflowId), msg.Subject))
                StartMessage=msg;
            else
                msgs.Add(msg);
            if (Equals(msg.Metadata?.Sequence, message.Message.Metadata?.Sequence))
                break;
        }
        await jsContext.DeleteConsumerAsync(SubjectHelper.WorkflowEventsStreamsName,consumer.Info.Name);
        messages = msgs;
        return this;
    }

    private INatsJSMsg<byte[]>? GetNextMessage()
    {
        if (index >= messages.Count)
            return null;
        var result = messages.ElementAt(index);
        index++;
        if (Equals(result.Subject, SubjectHelper.WorkflowEnd(message.WorkflowName, message.WorkflowId)))
            throw new WorkflowEndedException();
        return result;
    }

    private EventMessage? GetNextActivityMessage<TActivity>()
    {
        var name = typeof(TActivity).Name;
        var msg = GetNextMessage();
        if (msg == null)
            return null;
        var result = new EventMessage(msg);
        if (!Equals(result.ActivityName, name))    
            throw new InvalidStepException(name, result.ActivityName??string.Empty);
        return result;
    }

    private ActivityResult? GetNextActivity<TActivity>()
    {
        var nextActivityMsg = GetNextActivityMessage<TActivity>();
        return nextActivityMsg?.WorkflowEventType switch
        {
            null => null,
            WorkflowEventTypes.StepEnd => new(nextActivityMsg.ActivityID??Guid.Empty, ActivityResultStatus.Success),
            WorkflowEventTypes.StepError => new(nextActivityMsg.ActivityID??Guid.Empty, ActivityResultStatus.Failure, nextActivityMsg.Message.Data != null ? System.Text.Encoding.UTF8.GetString(nextActivityMsg.Message.Data) : null),
            WorkflowEventTypes.StepTimeout => new(nextActivityMsg.ActivityID??Guid.Empty, ActivityResultStatus.Timeout),
            _ => throw new InvalidWorkflowEventMessage(nextActivityMsg.Message.Subject, ConnectionHelper.GetMessageID(nextActivityMsg.Message))
        };
    }

    private async ValueTask<ActivityResult<TOutput>?> GetNextActivityAsync<TActivity, TOutput>(MessageSerializer messageSerializer)
    {
        var nextActivityMsg = GetNextActivityMessage<TActivity>();
        return nextActivityMsg?.WorkflowEventType switch
        {
            null => null,
            WorkflowEventTypes.StepEnd => new(nextActivityMsg.ActivityID??Guid.Empty, ActivityResultStatus.Success, Output: await messageSerializer.DecodeAsync<TOutput>(nextActivityMsg.Message)),
            WorkflowEventTypes.StepError => new(nextActivityMsg.ActivityID??Guid.Empty, ActivityResultStatus.Failure, nextActivityMsg.Message.Data != null ? System.Text.Encoding.UTF8.GetString(nextActivityMsg.Message.Data) : null),
            WorkflowEventTypes.StepTimeout => new(nextActivityMsg.ActivityID??Guid.Empty, ActivityResultStatus.Timeout),
            _ => throw new InvalidWorkflowEventMessage(nextActivityMsg.Message.Subject, ConnectionHelper.GetMessageID(nextActivityMsg.Message))
        };
    }

    private async ValueTask<ActivityResult> HandleNextActivity<TActivity>(Func<ValueTask> invokeCall)
    {
        var result = GetNextActivity<TActivity>();
        if (result!=null)
            return result;
        await invokeCall();
        throw new WorkflowSuspendedException();
    }

    ValueTask<ActivityResult> IWorkflowContext.ExecuteActivityAsync<TActivity>(ActivityExecutionRequest executionRequest)
        => HandleNextActivity<TActivity>(() => ActivityHelper.StartActivityAsync<TActivity>(executionRequest, connection, jsContext, timerStore, message, cancellationToken));

    ValueTask<ActivityResult> IWorkflowContext.ExecuteActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest)
        => HandleNextActivity<TActivity>(() => ActivityHelper.StartActivityAsync<TActivity, TInput>(executionRequest, connection, jsContext, messageSerializer, timerStore, message, cancellationToken));

    private async ValueTask<ActivityResult<TOutput>> HandleNextActivity<TActivity, TOutput>(Func<ValueTask> invokeCall)
    {
        var result = await GetNextActivityAsync<TActivity, TOutput>(messageSerializer);
        if (result!=null)
            return result;
        await invokeCall();
        throw new WorkflowSuspendedException();
    }

    ValueTask<ActivityResult<TOutput>> IWorkflowContext.ExecuteActivityAsync<TActivity, TOutput>(ActivityExecutionRequest executionRequest)
        => HandleNextActivity<TActivity, TOutput>(() => ActivityHelper.StartActivityAsync<TActivity>(executionRequest, connection, jsContext, timerStore, message, cancellationToken));

    ValueTask<ActivityResult<TOutput>> IWorkflowContext.ExecuteActivityAsync<TActivity, TOutput, TInput>(ActivityExecutionRequest<TInput> executionRequest)
        => HandleNextActivity<TActivity, TOutput>(() => ActivityHelper.StartActivityAsync<TActivity, TInput>(executionRequest, connection, jsContext, messageSerializer, timerStore, message, cancellationToken));

    async ValueTask IWorkflowContext.WaitAsync(TimeSpan delay)
    {
        var msg = GetNextMessage();
        if (msg!=null)
        {
            var eventMessage = new EventMessage(msg);
            if (!Equals(eventMessage.WorkflowEventType, WorkflowEventTypes.DelayEnd))
                throw new InvalidDelayStepException(eventMessage.Message.Subject);
            return;
        }
        await WorkflowHelper.StartWorkflowDelayAsync(message, delay, connection, jsContext, cancellationToken);
        throw new WorkflowSuspendedException();
    }
}
