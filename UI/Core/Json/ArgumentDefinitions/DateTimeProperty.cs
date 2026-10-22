using JetFlow.UI.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JetFlow.UI.Json.ArgumentDefinitions;

internal class DateTimeProperty : IArgumentDefinition
{
    JsonNode IArgumentDefinition.ApplyDefinition(JsonNode input)
        => input.GetValueKind() switch {
            JsonValueKind.String when DateTime.TryParse(input.GetValue<string>(),out var result) => JsonValue.Create(result),
            JsonValueKind.Null => input,
            _ => throw new InvalidCastException()
        };
}
