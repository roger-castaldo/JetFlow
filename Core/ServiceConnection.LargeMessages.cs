using NATS.Client.Core;
using System.Text;

namespace JetFlow;

internal partial class ServiceConnection
{
    private readonly byte[] LargeMessageMagicByte = [0x4C, 0x4D, 0x53, 0x47]; // "LMSG" in ASCII

    public async ValueTask<byte[]> EncodeLargeMessageAsync(byte[] data, string workflowName, string workflowInstanceId, CancellationToken cancellationToken)
    {
        if (data.Length<connection.MaxMessagePayload)
            return data;
        var messageId = $"{workflowName}/{workflowInstanceId}/{Guid.NewGuid()}";
        await largeMessageStore.PutAsync(messageId, data, cancellationToken);
        return [.. LargeMessageMagicByte, .. UTF8Encoding.UTF8.GetBytes(messageId)];
    }

    private async ValueTask<(byte[] data, NatsHeaders headers)> EncodeMessageAsync<TMessage>(TMessage? message, string workflowName, string workflowInstanceId, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TMessage>(message);
        data = await EncodeLargeMessageAsync(data, workflowName, workflowInstanceId, cancellationToken);
        return (data, headers);
    }

    public async ValueTask<byte[]?> RetrieveMessageDataAsync(byte[]? messageData, CancellationToken cancellationToken)
    {
        if (messageData!=null && messageData.Length>LargeMessageMagicByte.Length && messageData.Take(LargeMessageMagicByte.Length).SequenceEqual(LargeMessageMagicByte))
        {
            var messageId = UTF8Encoding.UTF8.GetString([.. messageData.Skip(LargeMessageMagicByte.Length)]);
            var info = await largeMessageStore.GetInfoAsync(messageId, cancellationToken: cancellationToken);
            if (info!=null && !info.Deleted)
                return await largeMessageStore.GetBytesAsync(messageId, cancellationToken: cancellationToken);
            throw new ArgumentNullException($"Large message with id {messageId} not found in storage.");
        }
        return messageData;
    }
}
