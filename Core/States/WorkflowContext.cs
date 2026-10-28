using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.JetStream;
using System.Diagnostics;
using System.Text;

namespace JetFlow.States;

internal class WorkflowContext 
    : AContext, IWorkflowContext
{
    private readonly ServiceConnection serviceConnection;
    private readonly SubjectMapper subjectMapper;
    private readonly MetricsHelper metricsHelper;
    private readonly EventMessage message;
    private readonly IReadOnlyCollection<INatsJSMsg<byte[]>> messages = [];
    private int index = 0;
    private uint activityIndex = 0;
    public WorkflowOptions Options { get; private init; }

    private WorkflowContext(ServiceConnection serviceConnection, SubjectMapper subjectMapper, 
        MessageSerializer messageSerializer, MetricsHelper metricsHelper, EventMessage message, IReadOnlyCollection<INatsJSMsg<byte[]>> messages, EventMessage startMessage, WorkflowOptions options)
        : base(startMessage, messageSerializer)
    {
        this.serviceConnection=serviceConnection;
        this.subjectMapper=subjectMapper;
        this.metricsHelper=metricsHelper;
        this.message=message;
        this.messages=messages;
        Options=options;
    }

    internal static async ValueTask<WorkflowContext> LoadAsync(ServiceConnection serviceConnection, SubjectMapper subjectMapper,
        MessageSerializer messageSerializer, MetricsHelper metricsHelper, EventMessage message)
    {
        await using var enumerable = await serviceConnection.QueryStreamAsync(subjectMapper.WorkflowEventsStreamsName,
                false,
                subjectMapper.WorkflowConfigure(message.WorkflowSubjectName, message.WorkflowId),
                subjectMapper.WorkflowStart(message.WorkflowSubjectName, message.WorkflowId),
                subjectMapper.WorkflowEnd(message.WorkflowSubjectName, message.WorkflowId),
                subjectMapper.WorkflowDelayEnd(message.WorkflowSubjectName, message.WorkflowId),
                subjectMapper.WorkflowStepEnd(message.WorkflowSubjectName, message.WorkflowId, "*")
            );
        var msgs = new List<INatsJSMsg<byte[]>>();
        INatsJSMsg<byte[]>? startMessage = null;
        WorkflowOptions? options = null;
        var add = true;
        await foreach(var msg in enumerable)
        {
            if (Equals(msg.Subject, subjectMapper.WorkflowConfigure(message.WorkflowSubjectName, message.WorkflowId)))
            {
                options = InternalsSerializer.DeserializeWorkflowOptions(msg.Data!);
                continue;
            }
            if (Equals(subjectMapper.WorkflowStart(message.WorkflowSubjectName, message.WorkflowId), msg.Subject))
                startMessage=msg;
            else if (add)
                msgs.Add(msg);
            if (Equals(msg.Metadata?.Sequence, message.Metadata?.Sequence))
                add=false;
            if (!add && startMessage!=null && options!=null)
                break;
        }
        if (startMessage==null)
            throw new UnableToLocateStartMessageException(message.ActivityName??message.ActivitySubjectName!, message.WorkflowName, message.WorkflowId);
        return new(serviceConnection, subjectMapper, messageSerializer, metricsHelper,
            message, msgs.ToArray(), await EventMessage.CreateMessageAsync(serviceConnection.LargeMessageStore, startMessage!, CancellationToken.None), options!);
    }

    private async ValueTask<EventMessage?> GetNextMessageAsync(CancellationToken cancellationToken)
    {
        if (index >= messages.Count)
            return null;
        var result = messages.ElementAt(index);
        index++;
        if (Equals(result.Subject, subjectMapper.WorkflowEnd(message.WorkflowSubjectName, message.WorkflowId)))
            throw new WorkflowEndedException();
        return await EventMessage.CreateMessageAsync(serviceConnection.LargeMessageStore, result, cancellationToken);
    }

    private async ValueTask<EventMessage?> GetNextActivityMessageAsync<TActivity>(CancellationToken cancellationToken)
    {
        var name = NameHelper.GetActivityName<TActivity>();
        var result = await GetNextMessageAsync(cancellationToken);
        if (result == null)
            return null;
        if (!Equals(result.ActivityName, name.rawName))    
            throw new InvalidStepException(name.rawName, result.ActivityName??string.Empty);
        activityIndex++;
        return result;
    }

    private async ValueTask<(StepResult? result, uint? progressIndex, uint? progressCount)> GetNextActivityAsync<TActivity>(CancellationToken cancellationToken)
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

    private async ValueTask<(StepResult<TOutput>?, uint? progressIndex, uint? progressCount)> GetNextActivityAsync<TActivity, TOutput>(CancellationToken cancellationToken)
    {
        var nextActivityMsg = await GetNextActivityMessageAsync<TActivity>(cancellationToken);
        return (
            nextActivityMsg?.WorkflowStepResultStatus switch
            {
                null => null,
                ActivityResultStatus.Success => new(nextActivityMsg.ActivityID??0, ActivityResultStatus.Success, Output: await MessageSerializer.DecodeAsync<TOutput>(nextActivityMsg.Data, nextActivityMsg.Headers)),
                ActivityResultStatus.Failure => new(nextActivityMsg.ActivityID??0, ActivityResultStatus.Failure, nextActivityMsg.Data != null ? System.Text.Encoding.UTF8.GetString(nextActivityMsg.Data) : null),
                ActivityResultStatus.Timeout => new(nextActivityMsg.ActivityID??0, ActivityResultStatus.Timeout),
                _ => throw new InvalidWorkflowEventMessage(nextActivityMsg.Subject, InternalNatsConnection.GetMessageID(nextActivityMsg.Headers))
            },
            nextActivityMsg?.ParallelActivityIndex,
            nextActivityMsg?.ParallelActivityCount
        );
    }

    private async ValueTask<StepResult> HandleNextActivityAsync<TActivity>(Func<ValueTask> invokeCall, CancellationToken cancellationToken)
    {
        var (result,_,_) = await GetNextActivityAsync<TActivity>(cancellationToken);
        if (result!=null)
            return result;
        await invokeCall();
        throw new WorkflowSuspendedException();
    }

    ValueTask<StepResult> IWorkflowContext.ExecuteActivityAsync<TActivity>(ActivityExecutionRequest executionRequest, CancellationToken cancellationToken)
        => HandleNextActivityAsync<TActivity>(() => serviceConnection.StartActivityAsync<TActivity>(activityIndex, executionRequest, message, cancellationToken), cancellationToken);

    ValueTask<StepResult> IWorkflowContext.ExecuteActivityAsync<TActivity, TInput>(ActivityExecutionRequest<TInput> executionRequest, CancellationToken cancellationToken)
        => HandleNextActivityAsync<TActivity>(() => serviceConnection.StartActivityAsync<TActivity, TInput>(activityIndex, executionRequest, message, cancellationToken), cancellationToken);

    private async ValueTask<StepResult<TOutput>> HandleNextActivity<TActivity, TOutput>(Func<ValueTask> invokeCall, CancellationToken cancellationToken)
    {
        var (result,_,_) = await GetNextActivityAsync<TActivity, TOutput>(cancellationToken);
        if (result!=null)
            return result;
        await invokeCall();
        throw new WorkflowSuspendedException();
    }

    ValueTask<StepResult<TOutput>> IWorkflowContext.ExecuteActivityAsync<TActivity, TOutput>(ActivityExecutionRequest executionRequest, CancellationToken cancellationToken)
        => HandleNextActivity<TActivity, TOutput>(() => serviceConnection.StartActivityAsync<TActivity>(activityIndex, executionRequest, message, cancellationToken), cancellationToken);

    ValueTask<StepResult<TOutput>> IWorkflowContext.ExecuteActivityAsync<TActivity, TOutput, TInput>(ActivityExecutionRequest<TInput> executionRequest, CancellationToken cancellationToken)
        => HandleNextActivity<TActivity, TOutput>(() => serviceConnection.StartActivityAsync<TActivity, TInput>(activityIndex, executionRequest, message, cancellationToken), cancellationToken);

    async ValueTask<IEnumerable<StepResult>> IWorkflowContext.ExecuteActivitiesAsync<TActivity, TInput>(ActivityExecutionRequest<IEnumerable<TInput>> executionRequest, CancellationToken cancellationToken)
    {
        var (result, progressIndex, progressCount) = await GetNextActivityAsync<TActivity>(cancellationToken);
        if (result!=null)
        {
            var results = new List<(uint index, StepResult result)>();
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

    async ValueTask<IEnumerable<StepResult<TOutput>>> IWorkflowContext.ExecuteActivitiesAsync<TActivity, TOutput, TInput>(ActivityExecutionRequest<IEnumerable<TInput>> executionRequest, CancellationToken cancellationToken)
    {
        var (result, progressIndex, progressCount) = await GetNextActivityAsync<TActivity, TOutput>(cancellationToken);
        if (result!=null)
        {
            var results = new List<(uint index, StepResult<TOutput> result)>();
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
        var msg = await GetNextMessageAsync(cancellationToken);
        if (msg!=null)
        {
            if (!Equals(msg.WorkflowEventType, WorkflowEventTypes.DelayEnd))
                throw new InvalidDelayStepException(msg.Subject);
            return;
        }
        await serviceConnection.StartWorkflowDelayAsync(message, delay, cancellationToken);
        await metricsHelper.SuspendWorkflowAsync(cancellationToken);
        throw new WorkflowSuspendedException();
    }

    async ValueTask IWorkflowContext.SuspendAsync(CancellationToken cancellationToken)
    {
        var msg = await GetNextMessageAsync(cancellationToken);
        if (msg!=null)
        {
            
            if (!Equals(msg.WorkflowEventType, WorkflowEventTypes.Suspended))
                throw new Exception();
            msg = await GetNextMessageAsync(cancellationToken);
            if (msg!=null)
            {
                if (!Equals(msg.WorkflowEventType, WorkflowEventTypes.Resumed))
                    throw new Exception();
            }
        }
        await serviceConnection.SuspendWorkflowAsync(message, index, cancellationToken);
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

    private async ValueTask<(StepResult? result, uint? progressIndex, uint? progressCount)> GetNextChildWorkflowMessage<TWorkflow>(CancellationToken cancellationToken)
    {
        var (_, rawName) = NameHelper.GetChildWorkflowName<TWorkflow>();
        var msg = await GetNextMessageAsync(cancellationToken);
        if (msg!=null)
        {
            if (!Equals(msg.WorkflowEventType, WorkflowEventTypes.StepEnd))
                throw new Exception();
            if (!Equals(msg.ActivityName, rawName))
                throw new InvalidStepException(rawName, msg.ActivityName??string.Empty);
            return (
                msg.WorkflowStepResultStatus switch
                {
                    null => null,
                    ActivityResultStatus.Success => new(msg.ActivityID??0, ActivityResultStatus.Success, null),
                    ActivityResultStatus.Failure => new(msg.ActivityID??0, ActivityResultStatus.Failure, msg.Data != null ? System.Text.Encoding.UTF8.GetString(msg.Data) : null),
                    _ => throw new InvalidWorkflowEventMessage(msg.Subject, InternalNatsConnection.GetMessageID(msg.Headers))
                },
                msg.ParallelActivityIndex,
                msg.ParallelActivityCount
            );
        }
        return (null, null, null);
    }

    private async ValueTask<StepResult> HandleNextChildWorkflow<TWorkflow>(Func<ValueTask> invokeCall, CancellationToken cancellationToken)
    {
        var (result, _, _) = await GetNextChildWorkflowMessage<TWorkflow>(cancellationToken);
        if (result!=null)
            return result;
        await invokeCall();
        throw new WorkflowSuspendedException();
    }

    ValueTask<StepResult> IWorkflowContext.ExecuteChildWorkflowAsync<TWorkflow>(WorkflowExecutionRequest? executionRequest, CancellationToken cancellationToken)
        => HandleNextChildWorkflow<TWorkflow>(()=>serviceConnection.StartChildWorkflowAsync<TWorkflow>(activityIndex, message, executionRequest, cancellationToken), cancellationToken);
    ValueTask<StepResult> IWorkflowContext.ExecuteChildWorkflowAsync<TWorkflow, TInput>(WorkflowExecutionRequest<TInput> executionRequest, CancellationToken cancellationToken)
        => HandleNextChildWorkflow<TWorkflow>(() => serviceConnection.StartChildWorkflowAsync<TWorkflow,TInput>(activityIndex, message, executionRequest, cancellationToken), cancellationToken);
    async ValueTask<IEnumerable<StepResult>> IWorkflowContext.ExecuteChildWorkflowsAsync<TWorkflow, TInput>(WorkflowExecutionRequest<IEnumerable<TInput>> executionRequest, CancellationToken cancellationToken)
    {
        var(result, index, count) = await GetNextChildWorkflowMessage<TWorkflow>(cancellationToken);
        if (result!=null)
        {
            var results = new List<(uint index, StepResult result)>();
            results.Add((index!.Value, result));
            while (results.Count<count!.Value)
            {
                (result, index, _) = await GetNextChildWorkflowMessage<TWorkflow>(cancellationToken);
                results.Add((index!.Value,result!));
            }
            return results.OrderBy(x => x.index).Select(x => x.result);
        }
        await serviceConnection.StartChildWorkflowsAsync<TWorkflow, TInput>(activityIndex, message, executionRequest, cancellationToken);
        throw new WorkflowSuspendedException();
    }
}
