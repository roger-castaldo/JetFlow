using JetFlow.Helpers;
using JetFlow.Interfaces;
using NATS.Client.JetStream;

namespace JetFlow;

internal class WorkflowState : IWorkflowState
{
    private readonly Dictionary<string, INatsJSMsg<byte[]>> messages;
    private readonly MessageSerializer messageSerializer;

    private WorkflowState(Dictionary<string, INatsJSMsg<byte[]>> messages, MessageSerializer messageSerializer)
    {
        this.messages = messages;
        this.messageSerializer = messageSerializer;
    }

    public static async ValueTask<IWorkflowState> CreateAsync(ServiceConnection serviceConnection, MessageSerializer messageSerializer, SubjectMapper subjectMapper, EventMessage message) 
    {
        var messages = new Dictionary<string, INatsJSMsg<byte[]>>();
        await using var query = await serviceConnection.QueryStreamAsync(
            subjectMapper.WorkflowEventsStreamsName,
            false,
            subjectMapper.WorkflowStepStart(message.WorkflowName, message.WorkflowId, "*"),
            subjectMapper.WorkflowStepEnd(message.WorkflowName, message.WorkflowId, "*"),
            subjectMapper.WorkflowStepError(message.WorkflowName, message.WorkflowId, "*"),
            subjectMapper.WorkflowStepTimeout(message.WorkflowName, message.WorkflowId, "*")
        );
        await foreach (var msg in query)
        {
            var eventMessage = new EventMessage(msg);
            messages.Remove(eventMessage.ActivityName!);
            if (Equals(eventMessage.WorkflowEventType, WorkflowEventTypes.StepStart) && Equals(message.ActivityID, eventMessage.ActivityID))
                break;
            if (Equals(eventMessage.WorkflowEventType, WorkflowEventTypes.StepEnd))
                messages.Add(eventMessage.ActivityName!, msg);
        }
        return new WorkflowState(messages, messageSerializer);
    }

    ValueTask<TValue?> IWorkflowState.GetActivityResultValueAsync<TWorkflowActivity, TValue>() 
        where TValue : default
        => ((IWorkflowState)this).GetActivityResultValueAsync<TValue>(NameHelper.GetActivityName<TWorkflowActivity>());

    async ValueTask<TValue?> IWorkflowState.GetActivityResultValueAsync<TValue>(string activityName) where TValue : default
    {
        if (messages.TryGetValue(activityName, out var msg))
            return await messageSerializer.DecodeAsync<TValue>(msg.Data, msg.Headers);
        return default;
    }
}
