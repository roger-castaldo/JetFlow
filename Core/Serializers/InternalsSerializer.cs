using JetFlow.Configs;
using System.Text.Json;

namespace JetFlow.Serializers;

internal static class InternalsSerializer
{
    public static byte[] SerializeWorkflowOptions(WorkflowOptions options)
        => JsonSerializer.SerializeToUtf8Bytes<WorkflowOptions>(options, Constants.JsonOptions);

    public static WorkflowOptions? DeserializeWorkflowOptions(byte[] data)
        => JsonSerializer.Deserialize<WorkflowOptions>(data, Constants.JsonOptions);
}
