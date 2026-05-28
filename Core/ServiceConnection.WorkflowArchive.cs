using JetFlow.Configs;
using JetFlow.Messages;
using JetFlow.Serializers;
using System.Text;

namespace JetFlow;

internal partial class ServiceConnection
{
    public async Task ArchiveWorkflowAsync(EventMessage message, CancellationToken cancellationToken)
    {
        Guid? schedulerId = null;
        WorkflowOptions? options=null;
        DateTimeOffset? start=null;
        DateTimeOffset? end=null;
        WorkflowEnd? workflowEnd=null;
        List<EventMessage> events = [];
        object? arguments = null;
        List<WorkflowStep> steps = [];
        await using var query = await QueryStreamAsync(
            subjectMapper.WorkflowEventsStreamsName,
            false,
            subjectMapper.WorkflowConfigure(message.WorkflowName, message.WorkflowId),
            subjectMapper.WorkflowStart(message.WorkflowName, message.WorkflowId),
            subjectMapper.WorkflowEnd(message.WorkflowName, message.WorkflowId),
            subjectMapper.WorkflowDelayStart(message.WorkflowName, message.WorkflowId),
            subjectMapper.WorkflowDelayEnd(message.WorkflowName, message.WorkflowId),
            subjectMapper.WorkflowStepStart(message.WorkflowName, message.WorkflowId, "*"),
            subjectMapper.WorkflowStepEnd(message.WorkflowName, message.WorkflowId, "*"),
            subjectMapper.WorkflowStepRetry(message.WorkflowName, message.WorkflowId, "*")
        );
        await foreach(var msg in query)
        {
            var eventMessage = new EventMessage(msg);
            switch (eventMessage.WorkflowEventType!)
            {
                case WorkflowEventTypes.Config:
                    options = InternalsSerializer.DeserializeWorkflowOptions(eventMessage.Message.Data!);
                    break;
                case WorkflowEventTypes.Start:
                    start = eventMessage.Message.Metadata?.Timestamp;
                    arguments = await messageSerializer.DecodeAsync(eventMessage.Message.Data, eventMessage.Message.Headers);
                    if ((eventMessage.Message.Headers?.TryGetValue(Constants.SchedulerSourceID, out var scheduleIdString)??false) && Guid.TryParse(scheduleIdString.ToString(), out var schedId))
                        schedulerId = schedId;
                    break;
                case WorkflowEventTypes.End:
                    end = eventMessage.Message.Metadata?.Timestamp;
                    workflowEnd = await messageSerializer.DecodeAsync<WorkflowEnd>(eventMessage.Message.Data, eventMessage.Message.Headers);
                    break;
                case WorkflowEventTypes.DelayStart:
                case WorkflowEventTypes.StepStart:
                case WorkflowEventTypes.StepRetry:
                    events.Add(eventMessage);
                    break;
                case WorkflowEventTypes.DelayEnd:
                    var startMessage = FindMatchingMessages(eventMessage, ref events).FirstOrDefault(e => Equals(e.WorkflowEventType, WorkflowEventTypes.DelayStart));
                    steps.Add(new(
                        WorkflowStepTypes.Delay,
                        null,
                        null,
                        startMessage!.Message.Metadata!.Value.Timestamp,
                        eventMessage!.Message.Metadata!.Value.Timestamp,
                        null,
                        null,
                        null,
                        null,
                        null
                    ));
                    break;
                case WorkflowEventTypes.StepEnd:
                    var messages = FindMatchingMessages(eventMessage, ref events);
                    var previousMessage = messages.FirstOrDefault(e => Equals(e.WorkflowEventType, WorkflowEventTypes.StepStart));
                    var retries = messages.Where(e => Equals(e.WorkflowEventType, WorkflowEventTypes.StepRetry)).Select(e => new WorkflowStepRetry(Enum.Parse<RetryTypes>(UTF8Encoding.UTF8.GetString(e.Message.Data!)), e.Message.Metadata!.Value.Timestamp)).ToArray();
                    steps.Add(await ProduceActionAsync(eventMessage, previousMessage, retries));
                    break;
            }
        }
        await archiveStore.PutAsync(
            $"{message.WorkflowName}/{message.WorkflowId}",
            InternalsSerializer.SerializeWorkflowArchive(new(
                Guid.Parse(message.WorkflowId),
                schedulerId,
                message.WorkflowName,
                options!,
                start!.Value,
                end!.Value,
                workflowEnd!.IsSuccess,
                workflowEnd!.ErrorMessage,
                arguments,
                [.. steps]
            )),
            cancellationToken
        );
    }

    private static IEnumerable<EventMessage> FindMatchingMessages(EventMessage eventMessage, ref List<EventMessage> events)
    {
        var result = events.Where(e => Equals(e.ActivityID, eventMessage.ActivityID)
                    && Equals(e.ParallelActivityIndex, eventMessage.ParallelActivityIndex)).ToArray();
        events.RemoveAll(e => Equals(e.ActivityID, eventMessage.ActivityID)
                    && Equals(e.ParallelActivityIndex, eventMessage.ParallelActivityIndex));
        return result;
    }

    private async Task<WorkflowStep> ProduceActionAsync(EventMessage eventMessage, EventMessage? previousMessage, IEnumerable<WorkflowStepRetry> retries)
    {
        return new(
            WorkflowStepTypes.Action,
            eventMessage.ActivityID,
            eventMessage.ActivityName,
            previousMessage!.Message.Metadata!.Value.Timestamp,
            eventMessage!.Message.Metadata!.Value.Timestamp,
            (retries.Any() ? retries.ToArray() : null),
            ((previousMessage?.Message.Data?.Length??0)>0 ? await messageSerializer.DecodeAsync(previousMessage!.Message.Data, previousMessage!.Message.Headers) : null),
            eventMessage.WorkflowStepResultStatus,
            eventMessage.WorkflowStepResultStatus == ActivityResultStatus.Failure ? System.Text.UTF8Encoding.UTF8.GetString(eventMessage.Message.Data!) : null,
            eventMessage.WorkflowStepResultStatus == ActivityResultStatus.Success && (eventMessage.Message.Data?.Length??0)>0 ? await messageSerializer.DecodeAsync(eventMessage.Message.Data, eventMessage.Message.Headers) : null
        );
    }
}
