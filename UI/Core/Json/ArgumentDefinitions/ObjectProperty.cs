using JetFlow.UI.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JetFlow.UI.Json.ArgumentDefinitions;

internal class ObjectProperty : IArgumentDefinition
{
    [JsonPropertyName("properties")]
    public IEnumerable<KeyValuePair<string, IArgumentDefinition>> Properties { get; init; } = [];

    JsonNode IArgumentDefinition.ApplyDefinition(JsonNode input)
    {
        if (input.GetValueKind()!=System.Text.Json.JsonValueKind.Object)
            throw new InvalidCastException();
        var obj = input.AsObject();
        var result = new JsonObject();
        foreach(var prop in Properties)
        {
            if (obj.TryGetPropertyValue(prop.Key, out var node))
                result.Add(prop.Key, prop.Value.ApplyDefinition(JsonSerializer.Deserialize<JsonNode>(node!)!));
        }
        return result;
    }
}
