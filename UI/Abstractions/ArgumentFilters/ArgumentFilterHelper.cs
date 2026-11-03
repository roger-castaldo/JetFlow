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
            JsonValueKind.Array => GetValues(argument.EnumerateArray(), prop),
            _ => JsonValue.Create((string?)null).Deserialize<JsonElement>()
        };

    private static JsonElement GetValues(JsonElement.ArrayEnumerator arrayEnumerator, string prop)
    {
        var result = arrayEnumerator.Select(val => GetValue(val, prop));
        if (result.Any(e => e.ValueKind==JsonValueKind.Null || e.ValueKind==JsonValueKind.Undefined))
            return JsonValue.Create((string?)null).Deserialize<JsonElement>();
        return JsonValue.Create(new JsonArray(result.Select(e=>e.Deserialize<JsonNode>()!).ToArray())).Deserialize<JsonElement>();
    }
}
