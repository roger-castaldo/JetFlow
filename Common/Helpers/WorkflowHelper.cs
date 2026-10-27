using JetFlow.Configs;
using JetFlow.Data;
using JetFlow.Serializers;
using NATS.Client.JetStream;
using NATS.Client.ObjectStore;
using System.Text;

namespace JetFlow.Helpers;

internal static class WorkflowHelper
{
    private record ExtractedWorkflow(
        Guid? SchedulerId,
        WorkflowOptions Options,
        DateTimeOffset Start,
        DateTimeOffset? End,
        WorkflowEnd? WorkflowEnd,
        object? Arguments,
        Dictionary<string, string[]>? MetaData,
        WorkflowStep[] Steps
    );

    private static async ValueTask<ExtractedWorkflow> ExtractWorkflowAsync(SubjectMapper subjectMapper, INatsJSContext jsContext, INatsObjStore largeMessageStore, MessageSerializer messageSerializer,
        EventMessage sourceMessage, CancellationToken cancellationToken)
    {
        Guid? schedulerId = (sourceMessage.WorkflowEventType == WorkflowEventTypes.Start && sourceMessage.Headers?.TryGetValue(Constants.SchedulerSourceID, out var scheduleIdString) == true && Guid.TryParse(scheduleIdString.ToString(), out var schedId) ? schedId : null);
        WorkflowOptions? options = null;
        DateTimeOffset? start = (sourceMessage.WorkflowEventType == WorkflowEventTypes.Start ? sourceMessage.Metadata?.Timestamp : null);
        DateTimeOffset? end = null;
        WorkflowEnd? workflowEnd = null;
        List<EventMessage> events = [];
        object? arguments = (sourceMessage.WorkflowEventType == WorkflowEventTypes.Start ? await messageSerializer.DecodeAsync(sourceMessage.Data, sourceMessage.Headers) : null);
        List<WorkflowStep> steps = [];
        Dictionary<string, string[]>? metaData = (sourceMessage.WorkflowEventType == WorkflowEventTypes.Start ? MetaDataHelper.ExtractMetaData(sourceMessage.Headers) : null);
        await using var query = await JetStreamHelper.QueryStreamAsync(
            jsContext,
            subjectMapper.WorkflowEventsStreamsName,
            false,
            (sourceMessage.WorkflowEventType == WorkflowEventTypes.Start ? (sourceMessage.Metadata?.Sequence.Stream-1)??0 : 0),
            subjectMapper.WorkflowPurgeFilter(sourceMessage.WorkflowSubjectName, sourceMessage.WorkflowId)
        );
        await foreach (var msg in query)
        {
            if (Equals(msg.Subject, subjectMapper.WorkflowPurged(sourceMessage.WorkflowSubjectName, sourceMessage.WorkflowId)))
                continue;
            var eventMessage = await EventMessage.CreateMessageAsync(largeMessageStore, msg, cancellationToken);
            switch (eventMessage.WorkflowEventType!)
            {
                case WorkflowEventTypes.Config:
                    options = InternalsSerializer.DeserializeWorkflowOptions(eventMessage.Data!);
                    break;
                case WorkflowEventTypes.Start:
                    start = eventMessage.Metadata?.Timestamp;
                    arguments = await messageSerializer.DecodeAsync(eventMessage.Data, eventMessage.Headers);
                    if ((eventMessage.Headers?.TryGetValue(Constants.SchedulerSourceID, out scheduleIdString)??false) && Guid.TryParse(scheduleIdString.ToString(), out schedId))
                        schedulerId = schedId;
                    metaData = MetaDataHelper.ExtractMetaData(eventMessage.Headers);
                    break;
                case WorkflowEventTypes.End:
                    end = eventMessage.Metadata?.Timestamp;
                    workflowEnd = await messageSerializer.DecodeAsync<WorkflowEnd>(eventMessage.Data, eventMessage.Headers);
                    break;
                case WorkflowEventTypes.DelayStart:
                case WorkflowEventTypes.StepStart:
                case WorkflowEventTypes.StepRetry:
                case WorkflowEventTypes.Suspended:
                    events.Add(eventMessage);
                    break;
                case WorkflowEventTypes.DelayEnd:
                    var startMessage = FindMatchingMessages(eventMessage, ref events).FirstOrDefault(e => Equals(e.WorkflowEventType, WorkflowEventTypes.DelayStart));
                    steps.Add(WorkflowStep.CreateDelayStep(
                        startMessage!.Metadata!.Value.Timestamp,
                        eventMessage!.Metadata!.Value.Timestamp,
                        (startMessage.Data?.Length??0)>0 ? TimeSpan.Parse(UTF8Encoding.UTF8.GetString(startMessage.Data!)) : null)
                    );
                    break;
                case WorkflowEventTypes.Resumed:
                    var suspendMessage = FindMatchingMessages(eventMessage, ref events).FirstOrDefault(e => Equals(e.WorkflowEventType, WorkflowEventTypes.Suspended));
                    steps.Add(WorkflowStep.CreateSuspendStep(
                        suspendMessage!.Metadata!.Value.Timestamp,
                        eventMessage!.Metadata!.Value.Timestamp,
                        (eventMessage.Data?.Length??0)>0 ? await messageSerializer.DecodeAsync(eventMessage.Data, eventMessage.Headers) : null
                    ));
                    break;
                case WorkflowEventTypes.StepEnd:
                    var messages = FindMatchingMessages(eventMessage, ref events);
                    var previousMessage = messages.First(e => Equals(e.WorkflowEventType, WorkflowEventTypes.StepStart));
                    var retries = messages.Where(e => Equals(e.WorkflowEventType, WorkflowEventTypes.StepRetry)).Select(e => new WorkflowStepRetry(Enum.Parse<RetryTypes>(UTF8Encoding.UTF8.GetString(e.Data!)), e.Metadata!.Value.Timestamp)).ToArray();
                    steps.Add(await ProduceActionAsync(messageSerializer, eventMessage, previousMessage, retries));
                    break;
            }
        }
        steps.AddRange(await ProcessIncompleteEventsAsync(events, messageSerializer));
        return new(schedulerId, options!, start!.Value, end, workflowEnd, arguments, metaData, [.. steps]);
    }

    private static async Task<IEnumerable<WorkflowStep>> ProcessIncompleteEventsAsync(List<EventMessage> events, MessageSerializer messageSerializer)
    {
        if (events.Count==0)
            return [];
        var steps = new List<WorkflowStep>();
        var idx = 0;
        while (idx < events.Count)
        {
            var eventMessage = events[idx];
            switch (eventMessage.WorkflowEventType)
            {
                case WorkflowEventTypes.DelayStart:
                    steps.Add(WorkflowStep.CreateDelayStep(
                        eventMessage!.Metadata!.Value.Timestamp,
                        null,
                        (eventMessage.Data?.Length??0)>0 ? TimeSpan.Parse(UTF8Encoding.UTF8.GetString(eventMessage.Data!)) : null));
                    break;
                case WorkflowEventTypes.Suspended:
                    steps.Add(WorkflowStep.CreateSuspendStep(
                        eventMessage!.Metadata!.Value.Timestamp,
                        null,
                        (eventMessage.Data?.Length??0)>0 ? await messageSerializer.DecodeAsync(eventMessage.Data, eventMessage.Headers) : null
                    ));
                    break;
                case WorkflowEventTypes.StepStart:
                    var messages = FindMatchingMessages(eventMessage, ref events);
                    var retries = messages.Where(e => Equals(e.WorkflowEventType, WorkflowEventTypes.StepRetry)).Select(e => new WorkflowStepRetry(Enum.Parse<RetryTypes>(UTF8Encoding.UTF8.GetString(e.Data!)), e.Metadata!.Value.Timestamp)).ToArray();
                    steps.Add(await ProduceActionAsync(messageSerializer, null, eventMessage, retries));
                    break;
            }
            idx++;
        }
        return steps;
    }

    public static async ValueTask<ArchivedWorkflow> ProduceArchivedWorkflowAsync(SubjectMapper subjectMapper, INatsJSContext jsContext, INatsObjStore largeMessageStore, MessageSerializer messageSerializer,
        EventMessage message, CancellationToken cancellationToken)
    {
        var extractedData = await ExtractWorkflowAsync(subjectMapper, jsContext, largeMessageStore, messageSerializer, message, cancellationToken);
        return new ArchivedWorkflow(
            Guid.Parse(message.WorkflowId),
            extractedData.SchedulerId,
            message.WorkflowName,
            extractedData.Options,
            extractedData.Start,
            extractedData.End!.Value,
            extractedData.WorkflowEnd!.IsSuccess,
            extractedData.WorkflowEnd!.ErrorMessage,
            extractedData.Arguments,
            extractedData.MetaData,
            extractedData.Steps
        );
    }

    public static async ValueTask<ActiveWorkflow> ProduceActiveWorkflowAsync(SubjectMapper subjectMapper, INatsJSContext jsContext, INatsObjStore largeMessageStore, MessageSerializer messageSerializer,
        EventMessage message, CancellationToken cancellationToken)
    {
        var extractedData = await ExtractWorkflowAsync(subjectMapper, jsContext, largeMessageStore, messageSerializer, message, cancellationToken);
        return new ActiveWorkflow(
            Guid.Parse(message.WorkflowId),
            extractedData.SchedulerId,
            message.WorkflowName,
            extractedData.Options,
            extractedData.Start,
            extractedData.Arguments,
            extractedData.MetaData,
            extractedData.Steps
        );
    }

    public static string WorkflowArchivePath(EventMessage eventMessage)
        => $"{eventMessage.WorkflowSubjectName}/{eventMessage.WorkflowId}";

    private static IEnumerable<EventMessage> FindMatchingMessages(EventMessage eventMessage, ref List<EventMessage> events)
    {
        var result = events.Where(e => Equals(e.ActivityID, eventMessage.ActivityID)
                    && Equals(e.ParallelActivityIndex, eventMessage.ParallelActivityIndex)).ToArray();
        events.RemoveAll(e => Equals(e.ActivityID, eventMessage.ActivityID)
                    && Equals(e.ParallelActivityIndex, eventMessage.ParallelActivityIndex));
        return result;
    }

    private static async Task<WorkflowStep> ProduceActionAsync(MessageSerializer messageSerializer, EventMessage? endMessage, EventMessage startMessage, IEnumerable<WorkflowStepRetry> retries)
    {
        object? result = null;
        if (!string.IsNullOrWhiteSpace(startMessage.ChildWorkflowId) && (endMessage?.Data?.Length??0)>0)
            result = Enum.Parse<WorkflowResult>(UTF8Encoding.UTF8.GetString(endMessage!.Data));
        else if (endMessage?.WorkflowStepResultStatus == ActivityResultStatus.Success && (endMessage?.Data?.Length??0)>0)
            result = await messageSerializer.DecodeAsync(endMessage!.Data, endMessage!.Headers);
        return new(
            (!string.IsNullOrWhiteSpace(startMessage.ChildWorkflowId) ? WorkflowStepTypes.ChildWorkflow : WorkflowStepTypes.Action),
            startMessage.ActivityID,
            startMessage.ActivityName,
            startMessage!.Metadata!.Value.Timestamp,
            endMessage?.Metadata?.Timestamp,
            (retries.Any() ? retries.ToArray() : null),
            ((startMessage.Data?.Length??0)>0 ? await messageSerializer.DecodeAsync(startMessage.Data, startMessage.Headers) : null),
            endMessage?.WorkflowStepResultStatus,
            endMessage?.WorkflowStepResultStatus == ActivityResultStatus.Failure ? System.Text.UTF8Encoding.UTF8.GetString(endMessage!.Data!) : null,
            result,
            (!string.IsNullOrWhiteSpace(startMessage.ChildWorkflowId) ? Guid.Parse(startMessage.ChildWorkflowId) : null),
            startMessage.ChildWorkflowName
        );
    }
}
