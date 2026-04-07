using JetFlow.Helpers;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace JetFlow;

internal class WorkflowState(INatsJSContext jsContext, MessageSerializer messageSerializer,
    string workflowName, string workflowId, Guid activityId) : IWorkflowState
{
    private Dictionary<string, INatsJSMsg<byte[]>> messages = [];

    public async ValueTask<IWorkflowState> LoadAsync()
    {
        var consumer = await jsContext.CreateOrUpdateConsumerAsync(
            SubjectHelper.WorkflowEventsStreamsName,
            new ConsumerConfig
            {
                Name = Guid.NewGuid().ToString(), // ephemeral identity
                DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                AckPolicy = ConsumerConfigAckPolicy.None,
                FilterSubjects = [
                    SubjectHelper.WorkflowStepStart(workflowName, workflowId, "*"),
                    SubjectHelper.WorkflowStepEnd(workflowName, workflowId, "*"),
                    SubjectHelper.WorkflowStepError(workflowName, workflowId, "*"),
                    SubjectHelper.WorkflowStepTimeout(workflowName, workflowId, "*")
                ]
            }
        );
        await foreach (var msg in consumer.FetchAsync<byte[]>(new() { MaxMsgs=5, Expires=TimeSpan.FromSeconds(1) }))
        {
            (_, _, var stepName, var eventType) = SubjectHelper.ExtractWorkflowEventInfo(msg.Subject);
            messages.Remove(stepName!);
            if (Equals(eventType, WorkflowEventTypes.StepStart) && Equals(activityId, ConnectionHelper.GetActivityID(msg)))
                break;
            if (Equals(eventType, WorkflowEventTypes.StepEnd))
                messages.Add(stepName!, msg);
        }
        await jsContext.DeleteConsumerAsync(SubjectHelper.WorkflowEventsStreamsName, consumer.Info.Name);
        return this;
    }

    ValueTask<TValue?> IWorkflowState.GetActivityResultValueAsync<TWorkflowActivity, TValue>() 
        where TValue : default
        => ((IWorkflowState)this).GetActivityResultValueAsync<TValue>(NameHelper.GetActivityName<TWorkflowActivity>());

    async ValueTask<TValue?> IWorkflowState.GetActivityResultValueAsync<TValue>(string activityName) where TValue : default
    {
        if (messages.TryGetValue(activityName, out var msg))
            return await messageSerializer.DecodeAsync<TValue>(msg);
        return default;
    }
}
