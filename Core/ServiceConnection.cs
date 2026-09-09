using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using NATS.Client.ObjectStore;

namespace JetFlow;

internal partial class ServiceConnection(InternalNatsConnection connection, 
    INatsKVStore timerStore, INatsKVStore configurationStore, INatsObjStore archiveStore, INatsObjStore largeMessageStore,
    SubjectMapper subjectMapper, MessageSerializer messageSerializer)
{
    public INatsJSContext JSContext => connection.JSContext;
    public int MaxMessagePayload => connection.MaxMessagePayload;
    public INatsObjStore LargeMessageStore => largeMessageStore;
    public INatsObjStore ArchiveStore => archiveStore;
    public ValueTask<IJetstreamQuery> QueryStreamAsync(string streamName, bool headersOnly, params string[] filterSubjects)
        => JetStreamHelper.QueryStreamAsync(connection.JSContext, streamName, headersOnly, filterSubjects);

    private ValueTask<(byte[] data, NatsHeaders headers)> EncodeMessageAsync<TMessage>(TMessage? message, string workflowName, string workflowInstanceId, CancellationToken cancellationToken)
        => MessagesHelper.EncodeMessageAsync<TMessage>(MaxMessagePayload, LargeMessageStore, messageSerializer, message, workflowName, workflowInstanceId, cancellationToken);

    public async ValueTask PurgeWorkflowAsync(EventMessage message, CancellationToken cancellationToken)
    {
        await using var query = await QueryStreamAsync(subjectMapper.WorkflowEventsStreamsName, false, subjectMapper.WorkflowPurgeFilter(message.WorkflowName, message.WorkflowId));
        var tasks = new List<Task>();
        await foreach (var msg in query)
        {
            if (MessagesHelper.IsLargeMessage(msg.Data))
                tasks.Add(MessagesHelper.DeleteLargeMessageAsync(largeMessageStore, msg.Data!, cancellationToken));
        }
        await Task.WhenAll(
        [
            .. tasks,
            connection.PurgeStreamAsync(subjectMapper.ActivityQueueStream, new() { Filter = subjectMapper.WorkflowActivityPurgeFilter(message.WorkflowName, message.WorkflowId) }, cancellationToken),
            connection.PurgeStreamAsync(subjectMapper.WorkflowEventsStreamsName, new() { Filter = subjectMapper.WorkflowPurgeFilter(message.WorkflowName, message.WorkflowId) }, cancellationToken)
        ]);
        await connection.PublishMessageAsync(
            new(Array.Empty<byte>(), subjectMapper.WorkflowPurged(message.WorkflowName, message.WorkflowId), new(), $"{message.WorkflowName}-{message.WorkflowId}-purged", Timeout: TimeSpan.FromHours(6)),
            cancellationToken: cancellationToken
        );
    }
}
