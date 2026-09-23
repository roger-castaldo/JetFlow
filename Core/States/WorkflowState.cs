using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;

namespace JetFlow.States;

internal class WorkflowState 
    : AContext,IWorkflowState
{
    private readonly Dictionary<string, IEnumerable<EventMessage>> messages;
    private readonly ushort activityAttempt;

    private WorkflowState(EventMessage startMessage, Dictionary<string, IEnumerable<EventMessage>> messages, MessageSerializer messageSerializer, ushort activityAttempt)
        : base(startMessage, messageSerializer)
    {
        this.messages = messages;
        this.activityAttempt = activityAttempt;
    }

    public static async ValueTask<IWorkflowState> CreateAsync(ServiceConnection serviceConnection, MessageSerializer messageSerializer, SubjectMapper subjectMapper, EventMessage message) 
    {
        EventMessage? start = null;
        var messages = new Dictionary<string, IEnumerable<EventMessage>>();
        await using var query = await serviceConnection.QueryStreamAsync(
            subjectMapper.WorkflowEventsStreamsName,
            false,
            subjectMapper.WorkflowStart(message.WorkflowSubjectName, message.WorkflowId),
            subjectMapper.WorkflowStepStart(message.WorkflowSubjectName, message.WorkflowId, "*"),
            subjectMapper.WorkflowStepEnd(message.WorkflowSubjectName, message.WorkflowId, "*")
        );
        await foreach (var msg in query)
        {
            var eventMessage = await EventMessage.CreateMessageAsync(serviceConnection.LargeMessageStore, msg, CancellationToken.None);
            if (Equals(eventMessage.WorkflowEventType, WorkflowEventTypes.Start))
            {
                start = eventMessage;
                continue;
            }
            if (Equals(eventMessage.WorkflowEventType, WorkflowEventTypes.StepStart) && Equals(message.ActivityID, eventMessage.ActivityID)) 
                break;
            else if (Equals(eventMessage.WorkflowEventType, WorkflowEventTypes.StepEnd))
            {
                if (!messages.TryGetValue(eventMessage.ActivitySubjectName!, out var msgs))
                    messages.Add(eventMessage.ActivitySubjectName!, [eventMessage]);
                else
                {
                    messages.Remove(eventMessage.ActivitySubjectName!);
                    if (Equals(msgs.First().ActivityID, eventMessage.ActivityID))
                        messages.Add(eventMessage.ActivitySubjectName!, msgs.Append(eventMessage).OrderBy(m => m.ParallelActivityIndex));
                    else
                        messages.Add(eventMessage.ActivitySubjectName!, [eventMessage]);
                }
            }
        }
        return new WorkflowState(start!, messages, messageSerializer, message.ActivityAttempt);
    }
    ushort IWorkflowState.ActivityAttempt => activityAttempt;

    ValueTask<IEnumerable<TValue?>?> IWorkflowState.GetActivityResultValueAsync<TWorkflowActivity, TValue>() 
        where TValue : default
        => ((IWorkflowState)this).GetActivityResultValueAsync<TValue>(NameHelper.GetActivityName<TWorkflowActivity>().cleanedName);

    async ValueTask<IEnumerable<TValue?>?> IWorkflowState.GetActivityResultValueAsync<TValue>(string activityName) where TValue : default
    {
        if (messages.TryGetValue(activityName, out var msgs))
            return await Task.WhenAll(msgs.Select(msg=>MessageSerializer.DecodeAsync<TValue>(msg.Data, msg.Headers).AsTask()));
        else if (messages.TryGetValue(NameHelper.CleanName(activityName), out msgs))
            return await Task.WhenAll(msgs.Select(msg => MessageSerializer.DecodeAsync<TValue>(msg.Data, msg.Headers).AsTask()));
        return default;
    }
}
