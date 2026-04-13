using JetFlow.Configs;
using JetFlow.Serializers;
using NATS.Client.JetStream;

namespace JetFlow;

internal class WorkflowContext 
    : IWorkflowContext
{
    private readonly ServiceConnection serviceConnection;
    private readonly SubjectMapper subjectMapper;
    private readonly MessageSerializer messageSerializer;
    private readonly EventMessage message;
    private readonly IReadOnlyCollection<INatsJSMsg<byte[]>> messages = [];
    private int index = 0;
    private readonly CancellationToken cancellationToken = CancellationToken.None;
    public INatsJSMsg<byte[]> StartMessage {  get; private init; }
    public WorkflowOptions Options { get; private init; }

    private WorkflowContext(ServiceConnection serviceConnection, SubjectMapper subjectMapper, 
        MessageSerializer messageSerializer, EventMessage message, IReadOnlyCollection<INatsJSMsg<byte[]>> messages, INatsJSMsg<byte[]> startMessage, WorkflowOptions options)
    {
        this.serviceConnection=serviceConnection;
        this.subjectMapper=subjectMapper;
        this.messageSerializer=messageSerializer;
        this.message=message;
        this.messages=messages;
        StartMessage=startMessage;
        Options=options;
    }

    internal static async ValueTask<WorkflowContext> LoadAsync(ServiceConnection serviceConnection, SubjectMapper subjectMapper,
        MessageSerializer messageSerializer, EventMessage message)
    {
        await using var enumerable = await serviceConnection.QueryStream(subjectMapper.WorkflowEventsStreamsName,
                false,
                subjectMapper.WorkflowConfigure(message.WorkflowName, message.WorkflowId),
                subjectMapper.WorkflowStart(message.WorkflowName, message.WorkflowId),
                subjectMapper.WorkflowEnd(message.WorkflowName, message.WorkflowId),
                subjectMapper.WorkflowDelayEnd(message.WorkflowName, message.WorkflowId),
                subjectMapper.WorkflowStepEnd(message.WorkflowName, message.WorkflowId, "*"),
                subjectMapper.WorkflowStepError(message.WorkflowName, message.WorkflowId, "*"),
                subjectMapper.WorkflowStepTimeout(message.WorkflowName, message.WorkflowId, "*")
            );
        var msgs = new List<INatsJSMsg<byte[]>>();
        INatsJSMsg<byte[]>? startMessage = null;
        WorkflowOptions? options = null;
        await foreach(var msg in enumerable)
        {
            if (Equals(msg.Subject, subjectMapper.WorkflowConfigure(message.WorkflowName, message.WorkflowId)))
            {
                options = InternalsSerializer.DeserializeWorkflowOptions(msg.Data!);
                continue;
            }
            if (Equals(subjectMapper.WorkflowStart(message.WorkflowName, message.WorkflowId), msg.Subject))
                startMessage=msg;
            else
                msgs.Add(msg);
            if (Equals(msg.Metadata?.Sequence, message.Message.Metadata?.Sequence))
                break;
        }
        return new(serviceConnection, subjectMapper, messageSerializer,
            message, msgs.ToArray(), startMessage!, options!);
    }

    private INatsJSMsg<byte[]>? GetNextMessage()
    {
        if (index >= messages.Count)
            return null;
        var result = messages.ElementAt(index);
        index++;
        if (Equals(result.Subject, subjectMapper.WorkflowEnd(message.WorkflowName, message.WorkflowId)))
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
            _ => throw new InvalidWorkflowEventMessage(nextActivityMsg.Message.Subject, ServiceConnection.GetMessageID(nextActivityMsg.Message))
        };
    }

    private async ValueTask<ActivityResult<TOutput>?> GetNextActivityAsync<TActivity, TOutput>()
    {
        var nextActivityMsg = GetNextActivityMessage<TActivity>();
        return nextActivityMsg?.WorkflowEventType switch
        {
            null => null,
            WorkflowEventTypes.StepEnd => new(nextActivityMsg.ActivityID??Guid.Empty, ActivityResultStatus.Success, Output: await messageSerializer.DecodeAsync<TOutput>(nextActivityMsg.Message)),
            WorkflowEventTypes.StepError => new(nextActivityMsg.ActivityID??Guid.Empty, ActivityResultStatus.Failure, nextActivityMsg.Message.Data != null ? System.Text.Encoding.UTF8.GetString(nextActivityMsg.Message.Data) : null),
            WorkflowEventTypes.StepTimeout => new(nextActivityMsg.ActivityID??Guid.Empty, ActivityResultStatus.Timeout),
            _ => throw new InvalidWorkflowEventMessage(nextActivityMsg.Message.Subject, ServiceConnection.GetMessageID(nextActivityMsg.Message))
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
        => HandleNextActivity<TActivity>(() => serviceConnection.StartActivityAsync<TActivity>(executionRequest, message, cancellationToken));

    ValueTask<ActivityResult> IWorkflowContext.ExecuteActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest)
        => HandleNextActivity<TActivity>(() => serviceConnection.StartActivityAsync<TActivity, TInput>(executionRequest, message, cancellationToken));

    private async ValueTask<ActivityResult<TOutput>> HandleNextActivity<TActivity, TOutput>(Func<ValueTask> invokeCall)
    {
        var result = await GetNextActivityAsync<TActivity, TOutput>();
        if (result!=null)
            return result;
        await invokeCall();
        throw new WorkflowSuspendedException();
    }

    ValueTask<ActivityResult<TOutput>> IWorkflowContext.ExecuteActivityAsync<TActivity, TOutput>(ActivityExecutionRequest executionRequest)
        => HandleNextActivity<TActivity, TOutput>(() => serviceConnection.StartActivityAsync<TActivity>(executionRequest, message, cancellationToken));

    ValueTask<ActivityResult<TOutput>> IWorkflowContext.ExecuteActivityAsync<TActivity, TOutput, TInput>(ActivityExecutionRequest<TInput> executionRequest)
        => HandleNextActivity<TActivity, TOutput>(() => serviceConnection.StartActivityAsync<TActivity, TInput>(executionRequest, message, cancellationToken));

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
        await serviceConnection.StartWorkflowDelayAsync(message, delay, cancellationToken);
        throw new WorkflowSuspendedException();
    }
}
