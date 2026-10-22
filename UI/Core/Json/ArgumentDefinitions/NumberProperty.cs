using JetFlow.UI.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JetFlow.UI.Json.ArgumentDefinitions;

internal class NumberProperty : IArgumentDefinition
{
    JsonNode IArgumentDefinition.ApplyDefinition(JsonNode input)
        => input.GetValueKind() switch {
            JsonValueKind.Number => input,
            JsonValueKind.String when decimal.TryParse(input.GetValue<string>(),out var result) => JsonValue.Create(result),
            JsonValueKind.Null => input,
            _ => throw new InvalidCastException()
        };
}
