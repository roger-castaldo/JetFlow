using JetFlow.Configs;
using JetFlow.Messages;
using JetFlow.Serializers;

namespace JetFlow;

internal partial class ServiceConnection
{
    public async Task ArchiveWorkflowAsync(EventMessage message, CancellationToken cancellationToken)
    {
        WorkflowOptions? options=null;
        DateTime? start=null;
        DateTime? end=null;
        WorkflowEnd? workflowEnd=null;
        EventMessage? previousMessage=null;
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
            subjectMapper.WorkflowStepError(message.WorkflowName, message.WorkflowId, "*"),
            subjectMapper.WorkflowStepTimeout(message.WorkflowName, message.WorkflowId, "*")
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
                    start = eventMessage.Message.Metadata?.Timestamp.UtcDateTime;
                    arguments = await messageSerializer.DecodeAsync(eventMessage.Message);
                    break;
                case WorkflowEventTypes.End:
                    end = eventMessage.Message.Metadata?.Timestamp.UtcDateTime;
                    workflowEnd = await messageSerializer.DecodeAsync<WorkflowEnd>(eventMessage.Message);
                    break;
                case WorkflowEventTypes.DelayStart:
                case WorkflowEventTypes.StepStart:
                    previousMessage = eventMessage;
                    break;
                case WorkflowEventTypes.DelayEnd:
                    steps.Add(new(
                        WorkflowStepTypes.Delay,
                        null,
                        null,
                        previousMessage!.Message.Metadata.Value.Timestamp.UtcDateTime,
                        eventMessage.Message.Metadata.Value.Timestamp.UtcDateTime,
                        WorkflowStepStatuses.Success,
                        null,
                        null
                    ));
                    break;
                case WorkflowEventTypes.StepEnd:
                case WorkflowEventTypes.StepError:
                case WorkflowEventTypes.StepTimeout:
                    steps.Add(new(
                        WorkflowStepTypes.Action,
                        eventMessage.ActivityID,
                        eventMessage.ActivityName,
                        previousMessage!.Message.Metadata.Value.Timestamp.UtcDateTime,
                        eventMessage.Message.Metadata.Value.Timestamp.UtcDateTime,
                        (eventMessage.WorkflowEventType) switch { 
                            WorkflowEventTypes.StepEnd => WorkflowStepStatuses.Success, 
                            WorkflowEventTypes.StepError => WorkflowStepStatuses.Failure, 
                            WorkflowEventTypes.StepTimeout => WorkflowStepStatuses.Timeout, 
                            _ => throw new InvalidOperationException() 
                        },
                        eventMessage.WorkflowEventType == WorkflowEventTypes.StepError ? System.Text.UTF8Encoding.UTF8.GetString(eventMessage.Message.Data!) : null,
                        eventMessage.WorkflowEventType == WorkflowEventTypes.StepEnd && (eventMessage.Message.Data?.Length??0)>0 ? await messageSerializer.DecodeAsync(eventMessage.Message)  : null
                    ));
                    break;
            }
        }
        await archiveStore.PutAsync(
            $"{message.WorkflowName}/{message.WorkflowId}",
            InternalsSerializer.SerializeWorkflowArchive(new(
                Guid.Parse(message.WorkflowId),
                message.WorkflowName,
                options!,
                start!.Value,
                end!.Value,
                workflowEnd!.IsSuccess,
                workflowEnd!.ErrorMessage,
                arguments,
                steps.ToArray()
            )),
            cancellationToken
        );
    }
}
