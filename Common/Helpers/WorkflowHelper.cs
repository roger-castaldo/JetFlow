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

    private static async ValueTask<ExtractedWorkflow> ExtraceWorkflowAsync(SubjectMapper subjectMapper, INatsJSContext jsContext, INatsObjStore largeMessageStore, MessageSerializer messageSerializer,
        string workflowName, string workflowId, CancellationToken cancellationToken)
    {
        Guid? schedulerId = null;
        WorkflowOptions? options = null;
        DateTimeOffset? start = null;
        DateTimeOffset? end = null;
        WorkflowEnd? workflowEnd = null;
        List<EventMessage> events = [];
        object? arguments = null;
        List<WorkflowStep> steps = [];
        Dictionary<string, string[]>? metaData = null;
        await using var query = await JetStreamHelper.QueryStreamAsync(
            jsContext,
            subjectMapper.WorkflowEventsStreamsName,
            false,
            subjectMapper.WorkflowPurgeFilter(workflowName, workflowId)
        );
        await foreach (var msg in query)
        {
            var eventMessage = await EventMessage.CreateMessageAsync(largeMessageStore, msg, cancellationToken);
            switch (eventMessage.WorkflowEventType!)
            {
                case WorkflowEventTypes.Config:
                    options = InternalsSerializer.DeserializeWorkflowOptions(eventMessage.Data!);
                    break;
                case WorkflowEventTypes.Start:
                    start = eventMessage.Metadata?.Timestamp;
                    arguments = await messageSerializer.DecodeAsync(eventMessage.Data, eventMessage.Headers);
                    if ((eventMessage.Headers?.TryGetValue(Constants.SchedulerSourceID, out var scheduleIdString)??false) && Guid.TryParse(scheduleIdString.ToString(), out var schedId))
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
                    steps.Add(new(
                        WorkflowStepTypes.Delay,
                        null,
                        null,
                        startMessage!.Metadata!.Value.Timestamp,
                        eventMessage!.Metadata!.Value.Timestamp,
                        null,
                        null,
                        null,
                        null,
                        null
                    ));
                    break;
                case WorkflowEventTypes.Resumed:
                    var suspendMessage = FindMatchingMessages(eventMessage, ref events).FirstOrDefault(e => Equals(e.WorkflowEventType, WorkflowEventTypes.Suspended));
                    steps.Add(new(
                        WorkflowStepTypes.Suspended,
                        null,
                        null,
                        suspendMessage!.Metadata!.Value.Timestamp,
                        suspendMessage!.Metadata!.Value.Timestamp,
                        null,
                        null,
                        null,
                        null,
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
                case WorkflowEventTypes.Suspended:
                    steps.Add(new(
                        (eventMessage.WorkflowEventType==WorkflowEventTypes.DelayStart ? WorkflowStepTypes.Delay : WorkflowStepTypes.Suspended),
                        null,
                        null,
                        eventMessage!.Metadata!.Value.Timestamp,
                        null,
                        null,
                        null,
                        null,
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
        string workflowName, string workflowId, CancellationToken cancellationToken)
    {
        var extractedData = await ExtraceWorkflowAsync(subjectMapper, jsContext, largeMessageStore, messageSerializer, workflowName, workflowId, cancellationToken);
        return new ArchivedWorkflow(
            Guid.Parse(workflowId),
            extractedData.SchedulerId,
            workflowName,
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
        string workflowName, string workflowId, CancellationToken cancellationToken)
    {
        var extractedData = await ExtraceWorkflowAsync(subjectMapper, jsContext, largeMessageStore, messageSerializer, workflowName, workflowId, cancellationToken);
        return new ActiveWorkflow(
            Guid.Parse(workflowId),
            extractedData.SchedulerId,
            workflowName,
            extractedData.Options,
            extractedData.Start,
            extractedData.Arguments,
            extractedData.MetaData,
            extractedData.Steps
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

    private static async Task<WorkflowStep> ProduceActionAsync(MessageSerializer messageSerializer, EventMessage? endMessage, EventMessage startMessage, IEnumerable<WorkflowStepRetry> retries)
    {
        return new(
            WorkflowStepTypes.Action,
            startMessage.ActivityID,
            startMessage.ActivityName,
            startMessage!.Metadata!.Value.Timestamp,
            endMessage?.Metadata?.Timestamp,
            (retries.Any() ? retries.ToArray() : null),
            ((startMessage.Data?.Length??0)>0 ? await messageSerializer.DecodeAsync(startMessage.Data, startMessage.Headers) : null),
            endMessage?.WorkflowStepResultStatus,
            endMessage?.WorkflowStepResultStatus == ActivityResultStatus.Failure ? System.Text.UTF8Encoding.UTF8.GetString(endMessage!.Data!) : null,
            endMessage?.WorkflowStepResultStatus == ActivityResultStatus.Success && (endMessage?.Data?.Length??0)>0 ? await messageSerializer.DecodeAsync(endMessage!.Data, endMessage!.Headers) : null
        );
    }
}
