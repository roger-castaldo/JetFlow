using NATS.Client.JetStream;
using NATS.Client.ObjectStore;
using System.Text.Json;

namespace JetFlow.Subscriptions;

internal class ArchiveSubscription(INatsJSConsumer consumer, JsonSerializerOptions jsonSerializerOptions,
    string workflowNamespace, INatsObjStore archiveStore, Func<ArchivedWorkflowEvent, ValueTask<bool>> archiveCallback, CancellationToken cancellationToken)
        : ASubscription(consumer, cancellationToken)
{
    protected override async ValueTask ProcessMessageAsync(INatsJSMsg<byte[]> msg)
    {
        if (msg.Data==null)
            await msg.NakAsync();
        var path = System.Text.UTF8Encoding.UTF8.GetString(msg.Data!);
        if (string.IsNullOrWhiteSpace(path))
            await msg.NakAsync();
        var archiveData = await archiveStore.GetBytesAsync(path, CancellationToken);
        var archive = JsonSerializer.Deserialize<ArchivedWorkflow>(archiveData, jsonSerializerOptions);
        var delete = await archiveCallback(new(string.IsNullOrWhiteSpace(workflowNamespace) ? null : workflowNamespace, archive));
        if (delete)
            await archiveStore.DeleteAsync(path, CancellationToken);
        await msg.AckAsync();
    }
}
