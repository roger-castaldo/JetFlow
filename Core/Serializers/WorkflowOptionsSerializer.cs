using JetFlow.Configs;
using NATS.Client.Core;
using System.Buffers;
using System.Text.Json;

namespace JetFlow.Serializers;

internal class WorkflowOptionsSerializer : INatsSerializer<WorkflowOptions>
{
    INatsSerializer<WorkflowOptions> INatsSerializer<WorkflowOptions>.CombineWith(INatsSerializer<WorkflowOptions> next)
        => next;

    WorkflowOptions? INatsDeserialize<WorkflowOptions>.Deserialize(in ReadOnlySequence<byte> buffer)
        => JsonSerializer.Deserialize<WorkflowOptions>(buffer.ToArray(), Constants.JsonOptions);

    void INatsSerialize<WorkflowOptions>.Serialize(IBufferWriter<byte> bufferWriter, WorkflowOptions value)
        => bufferWriter.Write(JsonSerializer.SerializeToUtf8Bytes<WorkflowOptions>(value, Constants.JsonOptions));
}
