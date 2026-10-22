using JetFlow.UI.Interfaces;
using System.Text.Json.Nodes;

namespace JetFlow.UI.Json.ArgumentDefinitions;

internal class StringProperty : IArgumentDefinition
{
    JsonNode IArgumentDefinition.ApplyDefinition(JsonNode input)
        => input.GetValueKind() == System.Text.Json.JsonValueKind.String ? input : JsonNode.Parse(input.ToJsonString())!;
}
