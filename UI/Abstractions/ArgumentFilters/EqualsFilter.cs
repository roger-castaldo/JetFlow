using JetFlow.UI.Interfaces;
using System.Text.Json;

namespace JetFlow.UI.ArgumentFilters;

public class EqualsFilter : IArgumentFilter
{
    public required string PropertyName { get; init; }
    public required JsonElement Value { get; init; }
    bool IArgumentFilter.IsMatch(JsonElement argument)
    {
        var arg = ArgumentFilterHelper.GetValue(argument, PropertyName);
        return (arg.ValueKind, Value.ValueKind) switch
        {
            (JsonValueKind.String, JsonValueKind.String) when Value.TryGetDateTimeOffset(out var dateTimeValue) => arg.TryGetDateTimeOffset(out var vDateTimeValue) && Equals(dateTimeValue, vDateTimeValue),
            (JsonValueKind.String, JsonValueKind.String) when Value.GetString() is not null => arg.GetString()?.Equals(Value.GetString()!, StringComparison.InvariantCulture)??false,
            (JsonValueKind.Array, JsonValueKind.String) when Value.TryGetDateTimeOffset(out var dateTimeValue) => arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.String && v.TryGetDateTimeOffset(out var vdateTimeValue) && Equals(dateTimeValue, vdateTimeValue)),
            (JsonValueKind.Array, JsonValueKind.String) when Value.GetString() is not null => arg.EnumerateArray().Any(v=>v.GetString()?.Equals(Value.GetString()!, StringComparison.InvariantCulture)??false),
            (JsonValueKind.Array, JsonValueKind.Number) when Value.TryGetInt64(out var longValue) => arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetInt64(out var vlongValue) && Equals(longValue, vlongValue)),
            (JsonValueKind.Array, JsonValueKind.Number) when Value.TryGetUInt64(out var ulongValue) => arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetUInt64(out var vulongValue) && Equals(ulongValue, vulongValue)),
            (JsonValueKind.Array, JsonValueKind.Number) when Value.TryGetDecimal(out var decimalValue)=> arg.EnumerateArray().Any(v=>v.ValueKind==JsonValueKind.Number && v.TryGetDecimal(out var vdecimalValue) && Equals(decimalValue, vdecimalValue)),
            (JsonValueKind.Array, JsonValueKind.Number) when Value.TryGetDouble(out var doubleValue) => arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetDouble(out var vdoubleValue) && Equals(doubleValue, vdoubleValue)),
            (JsonValueKind.Array, JsonValueKind.Number) when Value.TryGetSingle(out var floatValue) => arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetSingle(out var vfloatValue) && Equals(floatValue, vfloatValue)),
            (JsonValueKind.Array, JsonValueKind.True) => arg.EnumerateArray().Any(v=>v.ValueKind == JsonValueKind.True),
            (JsonValueKind.Array, JsonValueKind.False) => arg.EnumerateArray().Any(v => v.ValueKind == JsonValueKind.False),
            _ => false 
        };
    }
}
