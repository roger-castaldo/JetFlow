using JetFlow.UI.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JetFlow.UI.Json.ArgumentDefinitions;

internal class BooleanProperty : IArgumentDefinition
{
    JsonNode IArgumentDefinition.ApplyDefinition(JsonNode input)
        => input.GetValueKind() switch {
            JsonValueKind.False => input,
            JsonValueKind.True => input,
            JsonValueKind.Number when input.GetValue<decimal>() < 1 => JsonValue.Create(false),
            JsonValueKind.Number when input.GetValue<decimal>() >= 1 => JsonValue.Create(true),
            JsonValueKind.String when bool.TryParse(input.GetValue<string>(),out var result) => JsonValue.Create(result),
            JsonValueKind.Null => JsonValue.Create(false),
            JsonValueKind.Undefined => JsonValue.Create(false),
            _ => throw new InvalidCastException()
        };
}
