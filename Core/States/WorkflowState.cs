using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;

namespace JetFlow;

internal class WorkflowState : IWorkflowState
{
    private readonly Dictionary<string, IEnumerable<EventMessage>> messages;
    private readonly MessageSerializer messageSerializer;
    private readonly ushort activityAttempt;

    private WorkflowState(Dictionary<string, IEnumerable<EventMessage>> messages, MessageSerializer messageSerializer, ushort activityAttempt)
    {
        this.messages = messages;
        this.messageSerializer = messageSerializer;
        this.activityAttempt = activityAttempt;
    }

    public static async ValueTask<IWorkflowState> CreateAsync(ServiceConnection serviceConnection, MessageSerializer messageSerializer, SubjectMapper subjectMapper, EventMessage message) 
    {
        var messages = new Dictionary<string, IEnumerable<EventMessage>>();
        await using var query = await serviceConnection.QueryStreamAsync(
            subjectMapper.WorkflowEventsStreamsName,
            false,
            subjectMapper.WorkflowStepStart(message.WorkflowName, message.WorkflowId, "*"),
            subjectMapper.WorkflowStepEnd(message.WorkflowName, message.WorkflowId, "*")
        );
        await foreach (var msg in query)
        {
            var eventMessage = await EventMessage.CreateMessageAsync(serviceConnection, msg, CancellationToken.None);
            if (Equals(eventMessage.WorkflowEventType, WorkflowEventTypes.StepStart) && Equals(message.ActivityID, eventMessage.ActivityID)) 
                break;
            if (Equals(eventMessage.WorkflowEventType, WorkflowEventTypes.StepEnd))
            {
                if (!messages.TryGetValue(eventMessage.ActivityName!, out var msgs))
                    messages.Add(eventMessage.ActivityName!, [eventMessage]);
                else
                {
                    messages.Remove(eventMessage.ActivityName!);
                    if (Equals(msgs.First().ActivityID, eventMessage.ActivityID))
                        messages.Add(eventMessage.ActivityName!, msgs.Append(eventMessage).OrderBy(m => m.ParallelActivityIndex));
                    else
                        messages.Add(eventMessage.ActivityName!, [eventMessage]);
                }
            }
        }
        return new WorkflowState(messages, messageSerializer, message.ActivityAttempt);
    }

    ushort IWorkflowState.ActivityAttempt => activityAttempt;

    ValueTask<IEnumerable<TValue?>?> IWorkflowState.GetActivityResultValueAsync<TWorkflowActivity, TValue>() 
        where TValue : default
        => ((IWorkflowState)this).GetActivityResultValueAsync<TValue>(NameHelper.GetActivityName<TWorkflowActivity>());

    async ValueTask<IEnumerable<TValue?>?> IWorkflowState.GetActivityResultValueAsync<TValue>(string activityName) where TValue : default
    {
        if (messages.TryGetValue(activityName, out var msgs))
            return await Task.WhenAll(msgs.Select(msg=>messageSerializer.DecodeAsync<TValue>(msg.Data, msg.Headers).AsTask()));
        return default;
    }
}
