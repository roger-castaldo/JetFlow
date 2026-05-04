using JetFlow.Helpers;
using NATS.Client.Core;

namespace JetFlow.Testing.Helpers;

internal static class WorkflowsHelper
{
    public static async Task<NatsMsg<byte[]>?> StartWorkflowAndWaitForCompletion<TWorkflow>(INatsConnection natsConnection, SubjectMapper subjectMapper, Func<ValueTask<Guid>> startCall)
    {
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var runId = Guid.Empty;
        _ = Task.Run(async () =>
        {
            await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<TWorkflow>(), "*")))
            {
                if (Equals(msg.Subject, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<TWorkflow>(), runId.ToString())))
                {
                    completion.TrySetResult(msg);
                    break;
                }
            }
        });
        runId = await startCall();
        return await completion.Task;
    }

    public static async Task<NatsMsg<byte[]>?> StartWorkflowAndWaitForPurge<TWorkflow>(INatsConnection natsConnection, SubjectMapper subjectMapper, Func<ValueTask<Guid>> startCall)
    {
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var runId = Guid.Empty;
        _ = Task.Run(async () =>
        {
            await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowPurge(NameHelper.GetWorkflowName<TWorkflow>(), "*")))
            {
                if (Equals(msg.Subject, subjectMapper.WorkflowPurge(NameHelper.GetWorkflowName<TWorkflow>(), runId.ToString())))
                {
                    completion.TrySetResult(msg);
                    break;
                }
            }
        });
        runId = await startCall();
        return await completion.Task;
    }

    public static async Task<NatsMsg<byte[]>?> StartWorkflowAndWaitForArchive<TWorkflow>(INatsConnection natsConnection, SubjectMapper subjectMapper, Func<ValueTask<Guid>> startCall)
    {
        var completion = new TaskCompletionSource<NatsMsg<byte[]>?>();
        var runId = Guid.Empty;
        _ = Task.Run(async () =>
        {
            await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowArchived(NameHelper.GetWorkflowName<TWorkflow>(), "*")))
            {
                if (Equals(msg.Subject, subjectMapper.WorkflowArchived(NameHelper.GetWorkflowName<TWorkflow>(), runId.ToString())))
                {
                    completion.TrySetResult(msg);
                    break;
                }
            }
        });
        runId = await startCall();
        return await completion.Task;
    }
}
