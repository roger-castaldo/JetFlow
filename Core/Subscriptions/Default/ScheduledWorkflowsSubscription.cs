using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions.Default
{
    internal class ScheduledWorkflowsSubscription(SubjectMapper subjectMapper, ServiceConnection serviceConnection, INatsJSConsumer consumer, CancellationToken cancellationToken)
    : ACoreSubscription(serviceConnection, consumer, cancellationToken)
    {
        protected override async ValueTask ProcessMessageAsync(EventMessage message)
        {
            INatsJSMsg<byte[]>? configMessage = null;
            await using var query = await ServiceConnection.QueryStreamAsync(subjectMapper.ScheduledWorkflowStreamsName, false, subjectMapper.ScheduledWorkflowConfigure(message.WorkflowName, message.WorkflowId));
            await foreach (var msg in query) { 
                configMessage = msg;
            }
            var headers = new NatsHeaders(message.Headers?.Where(pair => pair.Key.StartsWith(Constants.HeaderBase)).ToDictionary() ?? []);
            var id = Guid.CreateVersion7();
            var data = await MessagesHelper.EncodeLargeMessageAsync(ServiceConnection.MaxMessagePayload, ServiceConnection.LargeMessageStore, message.Data?? [], message.WorkflowName, id.ToString(), CancellationToken);
            headers.Add(Constants.SchedulerSourceID, message.WorkflowId);
            _ = await ServiceConnection.StartWorkflowAsync(
                message.WorkflowName,
                id,
                data,
                headers,
                null,
                configMessage?.Data, 
                CancellationToken);
            await message.AckAsync(CancellationToken);
        }
    }
}
