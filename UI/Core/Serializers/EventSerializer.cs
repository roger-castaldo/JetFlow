using JetFlow.UI.Messages;
using NATS.Client.Core;
using System.Buffers;
using System.Text.Json;

namespace JetFlow.UI.Serializers;

internal class EventSerializer : INatsDeserialize<Event>, INatsSerialize<Event>
{
    Event? INatsDeserialize<Event>.Deserialize(in ReadOnlySequence<byte> buffer)
        => JsonSerializer.Deserialize<Event>(buffer.ToArray());

    void INatsSerialize<Event>.Serialize(IBufferWriter<byte> bufferWriter, Event value)
        => bufferWriter.Write(JsonSerializer.SerializeToUtf8Bytes<Event>(value));
}
