using JetFlow.UI.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JetFlow.UI.Json.ArgumentDefinitions;

internal class ArrayProperty : IArgumentDefinition
{
    [JsonPropertyName("ofType")]
    public required IArgumentDefinition OfType { get; init; }

    JsonNode IArgumentDefinition.ApplyDefinition(JsonNode input)
    {
        if (input.GetValueKind()!=System.Text.Json.JsonValueKind.Array)
            throw new InvalidCastException();
        return new JsonArray(
            input.AsArray()
            .Where(n=>n is not null)
            .Select(node=>OfType.ApplyDefinition(JsonSerializer.Deserialize<JsonNode>(node!)!))
            .ToArray()
        );
    }
}
