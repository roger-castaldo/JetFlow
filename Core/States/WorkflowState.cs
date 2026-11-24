using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;

namespace JetFlow.States;

internal class WorkflowState 
    : AContext,IWorkflowState
{
    private readonly Dictionary<string, IEnumerable<EventMessage>> messages;
    private readonly ushort activityAttempt;
    private readonly Func<string, ValueTask<IEnumerable<EventMessage>>> loadPreviousActivities;
    private WorkflowState(EventMessage startMessage, Dictionary<string, IEnumerable<EventMessage>> messages, MessageSerializer messageSerializer, 
        ushort activityAttempt, Func<string,ValueTask<IEnumerable<EventMessage>>> loadPreviousActivities)
        : base(startMessage, messageSerializer)
    {
        this.messages = messages;
        this.activityAttempt = activityAttempt;
        this.loadPreviousActivities = loadPreviousActivities;
    }

    public static async ValueTask<IWorkflowState> CreateAsync(ServiceConnection serviceConnection, MessageSerializer messageSerializer, SubjectMapper subjectMapper, EventMessage message) 
    {
        EventMessage? start = null;
        var messages = new Dictionary<string, IEnumerable<EventMessage>>();
        await using var query = await serviceConnection.QueryStreamAsync(
            subjectMapper.WorkflowEventsStreamsName,
            false,
            subjectMapper.WorkflowStart(message.WorkflowSubjectName, message.WorkflowId)
        );
        var startMessage = await query.FirstOrDefaultAsync();
        if (startMessage!=null)
            start = await EventMessage.CreateMessageAsync(serviceConnection.LargeMessageStore, startMessage, CancellationToken.None);
        if (start==null)
            throw new UnableToLocateStartMessageException(message.ActivityName??message.ActivitySubjectName!, message.WorkflowName, message.WorkflowId);
        return new WorkflowState(start!, messages, messageSerializer, message.ActivityAttempt, async (activityName) =>
        {
            List<EventMessage> currentMessages = [];
            await using var query = await serviceConnection.QueryStreamAsync(
                subjectMapper.WorkflowEventsStreamsName,
                false,
                subjectMapper.WorkflowStepEnd(message.WorkflowSubjectName, message.WorkflowId, NameHelper.CleanName(activityName),"*"),
                subjectMapper.WorkflowStepEnd(message.WorkflowSubjectName, message.WorkflowId, NameHelper.CleanName(activityName), "*","*")
            );
            await foreach (var msg in query)
            {
                var eventMessage = await EventMessage.CreateMessageAsync(serviceConnection.LargeMessageStore, msg, CancellationToken.None);
                if (eventMessage.ActivityID.HasValue && message.ActivityID.HasValue && eventMessage.ActivityID>=message.ActivityID)
                    break;
                if (currentMessages.Count!=0 && currentMessages[0].ActivityID!=eventMessage.ActivityID)
                    currentMessages.Clear();
                currentMessages.Add(eventMessage);
            }
            return currentMessages;
        });
    }

    private async ValueTask LoadActivityMessages(string activityName)
    {
        if (!messages.ContainsKey(activityName) && !messages.ContainsKey(NameHelper.CleanName(activityName)))
        {
            var activityMessages = await loadPreviousActivities(activityName);
            messages.TryAdd(activityName, activityMessages);
        }
    }

    ushort IWorkflowState.ActivityAttempt => activityAttempt;

    ValueTask<IEnumerable<TValue?>?> IWorkflowState.GetActivityResultValueAsync<TWorkflowActivity, TValue>() 
        where TValue : default
        => ((IWorkflowState)this).GetActivityResultValueAsync<TValue>(NameHelper.GetActivityName<TWorkflowActivity>().cleanedName);

    async ValueTask<IEnumerable<TValue?>?> IWorkflowState.GetActivityResultValueAsync<TValue>(string activityName) where TValue : default
    {
        await LoadActivityMessages(activityName);
        if (messages.TryGetValue(activityName, out var msgs))
            return await Task.WhenAll(msgs.Select(msg=>MessageSerializer.DecodeAsync<TValue>(msg.Data, msg.Headers).AsTask()));
        else if (messages.TryGetValue(NameHelper.CleanName(activityName), out msgs))
            return await Task.WhenAll(msgs.Select(msg => MessageSerializer.DecodeAsync<TValue>(msg.Data, msg.Headers).AsTask()));
        return default;
    }
}
