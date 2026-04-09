using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.KeyValueStore;
using System.Xml.Linq;

namespace JetFlow;

internal class WorkflowContext(INatsConnection connection, INatsJSContext jsContext, MessageSerializer messageSerializer, INatsKVStore timerStore, 
    string workflowName, string workflowId, NatsJSSequencePair? sequence) 
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
                    SubjectHelper.WorkflowStart(workflowName, workflowId),
                    SubjectHelper.WorkflowEnd(workflowName, workflowId),
                    SubjectHelper.WorkflowDelayEnd(workflowName, workflowId),
                    SubjectHelper.WorkflowStepEnd(workflowName, workflowId, "*"),
                    SubjectHelper.WorkflowStepError(workflowName, workflowId, "*"),
                    SubjectHelper.WorkflowStepTimeout(workflowName, workflowId, "*")
                ]
            }
        );
        var msgs = new List<INatsJSMsg<byte[]>>();
        await foreach(var msg in consumer.FetchAsync<byte[]>(new() { MaxMsgs=5, Expires=TimeSpan.FromSeconds(1)  }))
        {
            if (Equals(SubjectHelper.WorkflowStart(workflowName, workflowId), msg.Subject))
                StartMessage=msg;
            else
                msgs.Add(msg);
            if (Equals(msg.Metadata?.Sequence, sequence))
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
        if (Equals(result.Subject, SubjectHelper.WorkflowEnd(workflowName, workflowId)))
            throw new WorkflowEndedException();
        return result;
    }

    private (Guid activityID, WorkflowEventTypes eventType, INatsJSMsg<byte[]> msg)? GetNextActivityMessage<TActivity>()
    {
        var name = typeof(TActivity).Name;
        var msg = GetNextMessage();
        if (msg == null)
            return null;
        (_ ,_ , var stepName, var eventType) = SubjectHelper.ExtractWorkflowEventInfo(msg.Subject);
        if (!Equals(stepName, name))    
            throw new InvalidStepException(name, stepName!);
        var activityID = ConnectionHelper.GetActivityID(msg);
        return (activityID, eventType, msg);
    }

    private ActivityResult? GetNextActivity<TActivity>()
    {
        var nextActivityMsg = GetNextActivityMessage<TActivity>();
        return nextActivityMsg?.eventType switch
        {
            null => null,
            WorkflowEventTypes.StepEnd => new(nextActivityMsg.Value.activityID, ActivityResultStatus.Success),
            WorkflowEventTypes.StepError => new(nextActivityMsg.Value.activityID, ActivityResultStatus.Failure, nextActivityMsg.Value.msg.Data != null ? System.Text.Encoding.UTF8.GetString(nextActivityMsg.Value.msg.Data) : null),
            WorkflowEventTypes.StepTimeout => new(nextActivityMsg.Value.activityID, ActivityResultStatus.Timeout),
            _ => throw new InvalidWorkflowEventMessage(nextActivityMsg.Value.msg.Subject, ConnectionHelper.GetMessageID(nextActivityMsg.Value.msg))
        };
    }

    private async ValueTask<ActivityResult<TOutput>?> GetNextActivityAsync<TActivity, TOutput>(MessageSerializer messageSerializer)
    {
        var nextActivityMsg = GetNextActivityMessage<TActivity>();
        return nextActivityMsg?.eventType switch
        {
            null => null,
            WorkflowEventTypes.StepEnd => new(nextActivityMsg.Value.activityID, ActivityResultStatus.Success, Output: await messageSerializer.DecodeAsync<TOutput>(nextActivityMsg.Value.msg)),
            WorkflowEventTypes.StepError => new(nextActivityMsg.Value.activityID, ActivityResultStatus.Failure, nextActivityMsg.Value.msg.Data != null ? System.Text.Encoding.UTF8.GetString(nextActivityMsg.Value.msg.Data) : null),
            WorkflowEventTypes.StepTimeout => new(nextActivityMsg.Value.activityID, ActivityResultStatus.Timeout),
            _ => throw new InvalidWorkflowEventMessage(nextActivityMsg.Value.msg.Subject, ConnectionHelper.GetMessageID(nextActivityMsg.Value.msg))
        };
    }

    async ValueTask<ActivityResult> IWorkflowContext.ExecuteActivityAsync<TActivity>(ActivityExecutionRequest executionRequest)
    {
        var result = GetNextActivity<TActivity>();
        if (result!=null)
            return result;
        await ActivityHelper.StartActivityAsync<TActivity>(executionRequest, connection, jsContext, timerStore, workflowName, workflowId, cancellationToken);
        throw new WorkflowSuspendedException();
    }

    async ValueTask<ActivityResult> IWorkflowContext.ExecuteActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest)
    {
        var result = GetNextActivity<TActivity>();
        if (result!=null)
            return result;
        await ActivityHelper.StartActivityAsync<TActivity, TInput>(executionRequest, connection, jsContext, messageSerializer, timerStore, workflowName, workflowId, cancellationToken);
        throw new WorkflowSuspendedException();
    }

    async ValueTask<ActivityResult<TOutput>> IWorkflowContext.ExecuteActivityAsync<TActivity, TOutput>(ActivityExecutionRequest executionRequest)
    {
        var result = await GetNextActivityAsync<TActivity, TOutput>(messageSerializer);
        if (result!=null)
            return result;
        await ActivityHelper.StartActivityAsync<TActivity>(executionRequest, connection, jsContext, timerStore, workflowName, workflowId, cancellationToken);
        throw new WorkflowSuspendedException();
    }

    async ValueTask<ActivityResult<TOutput>> IWorkflowContext.ExecuteActivityAsync<TActivity, TOutput, TInput>(ActivityExecutionRequest<TInput> executionRequest)
    {
        var result = await GetNextActivityAsync<TActivity, TOutput>(messageSerializer);
        if (result!=null)
            return result;
        await ActivityHelper.StartActivityAsync<TActivity, TInput>(executionRequest, connection, jsContext, messageSerializer, timerStore, workflowName, workflowId, cancellationToken);
        throw new WorkflowSuspendedException();
    }

    async ValueTask IWorkflowContext.WaitAsync(TimeSpan delay)
    {
        var msg = GetNextMessage();
        if (msg!=null)
        {
            (_, _, _, var eventType) = SubjectHelper.ExtractWorkflowEventInfo(msg.Subject);
            if (!Equals(eventType, WorkflowEventTypes.DelayEnd))
                throw new InvalidDelayStepException(msg.Subject);
            return;
        }
        await WorkflowHelper.StartWorkflowDelayAsync(workflowName, workflowId, delay, connection, jsContext, cancellationToken);
        throw new WorkflowSuspendedException();
    }
}
