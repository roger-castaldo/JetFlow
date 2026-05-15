using NATS.Client.Core;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions
{
    internal class ScheduledWorkflowsSubscription(SubjectMapper subjectMapper, ServiceConnection serviceConnection, INatsJSConsumer consumer, CancellationToken cancellationToken)
    : ASubscription(serviceConnection, consumer, cancellationToken)
    {
        protected override async ValueTask ProcessMessageAsync(EventMessage message)
        {
            INatsJSMsg<byte[]>? configMessage = null;
            await using var query = await ServiceConnection.QueryStreamAsync(subjectMapper.ScheduledWorkflowStreamsName, false, subjectMapper.ScheduledWorkflowConfigure(message.WorkflowName, message.WorkflowId));
            await foreach (var msg in query) { 
                configMessage = msg;
            }
            var headers = new NatsHeaders(message.Message.Headers?.Where(pair => pair.Key.StartsWith(Constants.HeaderBase)).ToDictionary() ?? []);
            headers.Add(Constants.SchedulerSourceID, message.WorkflowId);
            _ = await ServiceConnection.StartWorkflowAsync(
                message.WorkflowName, 
                message.Message.Data?? [],
                headers, 
                configMessage?.Data, 
                CancellationToken);
            await message.Message.AckAsync();
        }
    }
}
