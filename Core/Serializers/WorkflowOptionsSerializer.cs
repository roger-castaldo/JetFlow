using JetFlow.Configs;
using NATS.Client.Core;
using System.Buffers;

namespace JetFlow.Serializers;

internal class WorkflowOptionsSerializer : INatsSerializer<WorkflowOptions>
{
    INatsSerializer<WorkflowOptions> INatsSerializer<WorkflowOptions>.CombineWith(INatsSerializer<WorkflowOptions> next)
        => next;

    WorkflowOptions? INatsDeserialize<WorkflowOptions>.Deserialize(in ReadOnlySequence<byte> buffer)
        => InternalsSerializer.DeserializeWorkflowOptions(buffer.ToArray());

    void INatsSerialize<WorkflowOptions>.Serialize(IBufferWriter<byte> bufferWriter, WorkflowOptions value)
        => bufferWriter.Write(InternalsSerializer.SerializeWorkflowOptions(value));
}
