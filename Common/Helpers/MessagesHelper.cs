using JetFlow.Serializers;
using NATS.Client.Core;
using NATS.Client.ObjectStore;
using System.Text;

namespace JetFlow.Helpers;

internal static class MessagesHelper
{
    private static readonly byte[] LargeMessageMagicByte = [0x4C, 0x4D, 0x53, 0x47]; // "LMSG" in ASCII

    public static async ValueTask<byte[]> EncodeLargeMessageAsync(int maxMessagePayload, INatsObjStore largeMessageStore, byte[] data, string workflowName, string workflowInstanceId, CancellationToken cancellationToken)
    {
        if (data.Length<maxMessagePayload)
            return data;
        var messageId = $"{workflowName}/{workflowInstanceId}/{Guid.CreateVersion7()}";
        await largeMessageStore.PutAsync(messageId, data, cancellationToken);
        return [.. LargeMessageMagicByte, .. UTF8Encoding.UTF8.GetBytes(messageId)];
    }

    public static async ValueTask<(byte[] data, NatsHeaders headers)> EncodeMessageAsync<TMessage>(int maxMessagePayload, INatsObjStore largeMessageStore, MessageSerializer messageSerializer,TMessage? message, string workflowName, string workflowInstanceId, CancellationToken cancellationToken)
    {
        var (data, headers) = await messageSerializer.EncodeAsync<TMessage>(message);
        data = await EncodeLargeMessageAsync(maxMessagePayload, largeMessageStore, data, workflowName, workflowInstanceId, cancellationToken);
        return (data, headers);
    }

    public static async ValueTask<byte[]?> RetrieveMessageDataAsync(INatsObjStore largeMessageStore, byte[]? messageData, CancellationToken cancellationToken)
    {
        if (IsLargeMessage(messageData))
        {
            var messageId = GetLargeMessageId(messageData!);
            var info = await largeMessageStore.GetInfoAsync(messageId, cancellationToken: cancellationToken);
            if (info!=null && !info.Deleted)
                return await largeMessageStore.GetBytesAsync(messageId, cancellationToken: cancellationToken);
            throw new ArgumentNullException($"Large message with id {messageId} not found in storage.");
        }
        return messageData;
    }

    public static bool IsLargeMessage(byte[]? messageData)
        => messageData!=null && messageData.Length>LargeMessageMagicByte.Length && messageData.Take(LargeMessageMagicByte.Length).SequenceEqual(LargeMessageMagicByte);

    private static string GetLargeMessageId(byte[] messageData)
        => UTF8Encoding.UTF8.GetString([.. messageData!.Skip(LargeMessageMagicByte.Length)]);

    public static async Task DeleteLargeMessageAsync(INatsObjStore largeMessageStore, byte[] messageData, CancellationToken cancellationToken)
        => await largeMessageStore.DeleteAsync(GetLargeMessageId(messageData), cancellationToken);
}
