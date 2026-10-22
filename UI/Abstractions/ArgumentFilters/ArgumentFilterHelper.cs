using System.Text.Json;
using System.Text.Json.Nodes;

namespace JetFlow.UI.ArgumentFilters;

internal static class ArgumentFilterHelper
{
    public static JsonElement GetValue(JsonElement argument, string prop)
        => argument.ValueKind switch {
            JsonValueKind.Null => argument,
            JsonValueKind.Undefined => argument,
            JsonValueKind.Object when prop.Contains('.') => GetValue(GetValue(argument, prop[..prop.IndexOf('.')]), prop[(prop.IndexOf('.')+1)..]),
            JsonValueKind.Object when argument.TryGetProperty(prop, out var result) => result,
            _ => JsonValue.Create((string?)null).Deserialize<JsonElement>()
        };
}
