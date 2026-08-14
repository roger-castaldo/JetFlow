using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.JetStream;

namespace JetFlow;

internal class WorkflowContext 
    : IWorkflowContext
{
    private readonly ServiceConnection serviceConnection;
    private readonly SubjectMapper subjectMapper;
    private readonly MessageSerializer messageSerializer;
    private readonly MetricsHelper metricsHelper;
    private readonly EventMessage message;
    private readonly IReadOnlyCollection<INatsJSMsg<byte[]>> messages = [];
    private int index = 0;
    private uint activityIndex = 0;
    public INatsJSMsg<byte[]> StartMessage {  get; private init; }
    public WorkflowOptions Options { get; private init; }

    private WorkflowContext(ServiceConnection serviceConnection, SubjectMapper subjectMapper, 
        MessageSerializer messageSerializer, MetricsHelper metricsHelper, EventMessage message, IReadOnlyCollection<INatsJSMsg<byte[]>> messages, INatsJSMsg<byte[]> startMessage, WorkflowOptions options)
    {
        this.serviceConnection=serviceConnection;
        this.subjectMapper=subjectMapper;
        this.messageSerializer=messageSerializer;
        this.metricsHelper=metricsHelper;
        this.message=message;
        this.messages=messages;
        StartMessage=startMessage;
        Options=options;
    }

    internal static async ValueTask<WorkflowContext> LoadAsync(ServiceConnection serviceConnection, SubjectMapper subjectMapper,
        MessageSerializer messageSerializer, MetricsHelper metricsHelper, EventMessage message)
    {
        await using var enumerable = await serviceConnection.QueryStreamAsync(subjectMapper.WorkflowEventsStreamsName,
                false,
                subjectMapper.WorkflowConfigure(message.WorkflowName, message.WorkflowId),
                subjectMapper.WorkflowStart(message.WorkflowName, message.WorkflowId),
                subjectMapper.WorkflowEnd(message.WorkflowName, message.WorkflowId),
                subjectMapper.WorkflowDelayEnd(message.WorkflowName, message.WorkflowId),
                subjectMapper.WorkflowStepEnd(message.WorkflowName, message.WorkflowId, "*")
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
            if (Equals(msg.Metadata?.Sequence, message.Metadata?.Sequence))
                break;
        }
        return new(serviceConnection, subjectMapper, messageSerializer, metricsHelper,
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

    private async ValueTask<EventMessage?> GetNextActivityMessageAsync<TActivity>(CancellationToken cancellationToken)
    {
        var name = typeof(TActivity).Name;
        var msg = GetNextMessage();
        if (msg == null)
            return null;
        var result = await EventMessage.CreateMessageAsync(serviceConnection, msg, cancellationToken);
        if (!Equals(result.ActivityName, name))    
            throw new InvalidStepException(name, result.ActivityName??string.Empty);
        activityIndex++;
        return result;
    }

    private async ValueTask<(ActivityResult? result, uint? progressIndex, uint? progressCount)> GetNextActivityAsync<TActivity>(CancellationToken cancellationToken)
    {
        var nextActivityMsg = await GetNextActivityMessageAsync<TActivity>(cancellationToken);
        return (
            nextActivityMsg?.WorkflowStepResultStatus switch
            {
                null => null,
                ActivityResultStatus.Success => new(nextActivityMsg.ActivityID??0, ActivityResultStatus.Success),
                ActivityResultStatus.Failure => new(nextActivityMsg.ActivityID??0, ActivityResultStatus.Failure, nextActivityMsg.Data != null ? System.Text.Encoding.UTF8.GetString(nextActivityMsg.Data) : null),
                ActivityResultStatus.Timeout => new(nextActivityMsg.ActivityID??0, ActivityResultStatus.Timeout),
                _ => throw new InvalidWorkflowEventMessage(nextActivityMsg.Subject, InternalNatsConnection.GetMessageID(nextActivityMsg.Headers))
            }, 
            nextActivityMsg?.ParallelActivityIndex,
            nextActivityMsg?.ParallelActivityCount
        );
    }

    private async ValueTask<(ActivityResult<TOutput>?, uint? progressIndex, uint? progressCount)> GetNextActivityAsync<TActivity, TOutput>(CancellationToken cancellationToken)
    {
        var nextActivityMsg = await GetNextActivityMessageAsync<TActivity>(cancellationToken);
        return (
            nextActivityMsg?.WorkflowStepResultStatus switch
            {
                null => null,
                ActivityResultStatus.Success => new(nextActivityMsg.ActivityID??0, ActivityResultStatus.Success, Output: await messageSerializer.DecodeAsync<TOutput>(nextActivityMsg.Data, nextActivityMsg.Headers)),
                ActivityResultStatus.Failure => new(nextActivityMsg.ActivityID??0, ActivityResultStatus.Failure, nextActivityMsg.Data != null ? System.Text.Encoding.UTF8.GetString(nextActivityMsg.Data) : null),
                ActivityResultStatus.Timeout => new(nextActivityMsg.ActivityID??0, ActivityResultStatus.Timeout),
                _ => throw new InvalidWorkflowEventMessage(nextActivityMsg.Subject, InternalNatsConnection.GetMessageID(nextActivityMsg.Headers))
            },
            nextActivityMsg?.ParallelActivityIndex,
            nextActivityMsg?.ParallelActivityCount
        );
    }

    private async ValueTask<ActivityResult> HandleNextActivityAsync<TActivity>(Func<ValueTask> invokeCall, CancellationToken cancellationToken)
    {
        var (result,_,_) = await GetNextActivityAsync<TActivity>(cancellationToken);
        if (result!=null)
            return result;
        await invokeCall();
        throw new WorkflowSuspendedException();
    }

    ValueTask<ActivityResult> IWorkflowContext.ExecuteActivityAsync<TActivity>(ActivityExecutionRequest executionRequest, CancellationToken cancellationToken)
        => HandleNextActivityAsync<TActivity>(() => serviceConnection.StartActivityAsync<TActivity>(activityIndex, executionRequest, message, cancellationToken), cancellationToken);

    ValueTask<ActivityResult> IWorkflowContext.ExecuteActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest, CancellationToken cancellationToken)
        => HandleNextActivityAsync<TActivity>(() => serviceConnection.StartActivityAsync<TActivity, TInput>(activityIndex, executionRequest, message, cancellationToken), cancellationToken);

    private async ValueTask<ActivityResult<TOutput>> HandleNextActivity<TActivity, TOutput>(Func<ValueTask> invokeCall, CancellationToken cancellationToken)
    {
        var (result,_,_) = await GetNextActivityAsync<TActivity, TOutput>(cancellationToken);
        if (result!=null)
            return result;
        await invokeCall();
        throw new WorkflowSuspendedException();
    }

    ValueTask<ActivityResult<TOutput>> IWorkflowContext.ExecuteActivityAsync<TActivity, TOutput>(ActivityExecutionRequest executionRequest, CancellationToken cancellationToken)
        => HandleNextActivity<TActivity, TOutput>(() => serviceConnection.StartActivityAsync<TActivity>(activityIndex, executionRequest, message, cancellationToken), cancellationToken);

    ValueTask<ActivityResult<TOutput>> IWorkflowContext.ExecuteActivityAsync<TActivity, TOutput, TInput>(ActivityExecutionRequest<TInput> executionRequest, CancellationToken cancellationToken)
        => HandleNextActivity<TActivity, TOutput>(() => serviceConnection.StartActivityAsync<TActivity, TInput>(activityIndex, executionRequest, message, cancellationToken), cancellationToken);

    async ValueTask<IEnumerable<ActivityResult>> IWorkflowContext.ExecuteActivitiesAsync<TActivity, TInput>(ActivityExecutionRequest<IEnumerable<TInput>> executionRequest, CancellationToken cancellationToken)
    {
        var (result, progressIndex, progressCount) = await GetNextActivityAsync<TActivity>(cancellationToken);
        if (result!=null)
        {
            var results = new List<(uint index, ActivityResult result)>();
            results.Add((progressIndex!.Value, result!));
            while(results.Count<progressCount)
            {
                (result, progressIndex, progressCount) = await GetNextActivityAsync<TActivity>(cancellationToken);
                results.Add((progressIndex!.Value, result!));
            }
            return results.OrderBy(x=>x.index).Select(x=>x.result);
        }
        await serviceConnection.StartActivitiesAsync<TActivity, TInput>(activityIndex, executionRequest, message, cancellationToken);
        throw new WorkflowSuspendedException();
    }

    async ValueTask<IEnumerable<ActivityResult<TOutput>>> IWorkflowContext.ExecuteActivitiesAsync<TActivity, TOutput, TInput>(ActivityExecutionRequest<IEnumerable<TInput>> executionRequest, CancellationToken cancellationToken)
    {
        var (result, progressIndex, progressCount) = await GetNextActivityAsync<TActivity, TOutput>(cancellationToken);
        if (result!=null)
        {
            var results = new List<(uint index, ActivityResult<TOutput> result)>();
            results.Add((progressIndex!.Value, result!));
            while (results.Count < progressCount)
            {
                (result, progressIndex, progressCount) = await GetNextActivityAsync<TActivity, TOutput>(cancellationToken);
                results.Add((progressIndex!.Value, result!));
            }
            return results.OrderBy(x => x.index).Select(x => x.result);
        }
        await serviceConnection.StartActivitiesAsync<TActivity, TInput>(activityIndex, executionRequest, message, cancellationToken);
        throw new WorkflowSuspendedException();
    }

    async ValueTask IWorkflowContext.WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        var msg = GetNextMessage();
        if (msg!=null)
        {
            var eventMessage = await EventMessage.CreateMessageAsync(serviceConnection, msg, cancellationToken);
            if (!Equals(eventMessage.WorkflowEventType, WorkflowEventTypes.DelayEnd))
                throw new InvalidDelayStepException(eventMessage.Subject);
            return;
        }
        await serviceConnection.StartWorkflowDelayAsync(message, delay, cancellationToken);
        await metricsHelper.SuspendWorkflowAsync(cancellationToken);
        throw new WorkflowSuspendedException();
    }

    internal (bool isComplete, ActivityResultStatus status, string? errorMessage, string? timeoutMessage) ExtractParallelActivityStatus()
    {
        List<string> errors = [];
        List<string> timeouts = [];
        uint cnt = 0;
        foreach(var msg in messages.Where(m=>Equals(m.Subject, message.Subject) 
        && (m.Headers?.TryGetValue(Constants.ActivityIDHeader, out var activityId)??false)
        && Equals(activityId.ToString(), message.ActivityID.ToString())))
        {
            cnt++;
            var idx = ((msg.Headers?.TryGetValue(Constants.ParalellActivityIndexHeader, out var paindex)??false) ? uint.Parse(paindex.ToString()!) : 0);
            if ((msg.Headers?.TryGetValue(Constants.ActivityResultHeader, out var status)??false) && Enum.TryParse<ActivityResultStatus>(status.ToString(), out var resultStatus))
            {
                if (resultStatus == ActivityResultStatus.Timeout)
                    timeouts.Add($"{idx}: Activity timed out");
                else if (resultStatus == ActivityResultStatus.Failure)
                    errors.Add($"{idx}: {(msg.Data==null ? "Activity failed" : System.Text.Encoding.UTF8.GetString(msg.Data))}");
            }
        }
        return (
            Equals(message.ParallelActivityCount, cnt),
            (errors.Count>0, timeouts.Count>0) switch
            {
                (true, false) => ActivityResultStatus.Failure,
                (true, true)=> ActivityResultStatus.Failure | ActivityResultStatus.Timeout,
                (false, true) => ActivityResultStatus.Timeout,
                _ => ActivityResultStatus.Success
            },
            errors.Count > 0 ? string.Join("; ", errors) : null,
            timeouts.Count > 0 ? string.Join("; ", timeouts) : null
        );
    }
}
