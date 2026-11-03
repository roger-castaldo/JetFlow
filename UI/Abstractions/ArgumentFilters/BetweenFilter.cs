using JetFlow.UI.Interfaces;
using System.Text.Json;

namespace JetFlow.UI.ArgumentFilters;

public class BetweenFilter : IArgumentFilter
{
    public required string PropertyName { get; init; }
    public required JsonElement Min { get; init; }
    public required JsonElement Max { get; init; }
    bool IArgumentFilter.IsMatch(JsonElement argument)
    {
        var arg = ArgumentFilterHelper.GetValue(argument, PropertyName);
        return (arg.ValueKind, Min.ValueKind, Max.ValueKind) switch
        {
            (JsonValueKind.String, JsonValueKind.String, JsonValueKind.String) => CheckDateTime(arg),
            (JsonValueKind.Array, JsonValueKind.String, JsonValueKind.String) 
                when Min.TryGetDateTimeOffset(out var minDateTime) && Max.TryGetDateTimeOffset(out var maxDateTime) => 
                    arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.String && v.TryGetDateTimeOffset(out var vdateTimeValue) && vdateTimeValue>=minDateTime && vdateTimeValue<=maxDateTime),
            (JsonValueKind.Array, JsonValueKind.Number, JsonValueKind.Number)
                when Min.TryGetInt64(out var minLongValue) && Max.TryGetInt64(out var maxLongValue) =>
                    arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetInt64(out var vlongValue) && vlongValue>=minLongValue && vlongValue<=maxLongValue),
            (JsonValueKind.Array, JsonValueKind.Number, JsonValueKind.Number)
                when Min.TryGetUInt64(out var minULongValue) && Max.TryGetUInt64(out var maxULongValue) =>
                    arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetUInt64(out var vulongValue) && vulongValue>=minULongValue && vulongValue<=maxULongValue),
            (JsonValueKind.Array, JsonValueKind.Number, JsonValueKind.Number) 
                when Min.TryGetDecimal(out var minDecimalValue) && Max.TryGetDecimal(out var maxDecimalValue) => 
                    arg.EnumerateArray().Any(v=>v.ValueKind==JsonValueKind.Number && v.TryGetDecimal(out var vdecimalValue) && vdecimalValue>=minDecimalValue && vdecimalValue<=maxDecimalValue),
            (JsonValueKind.Array, JsonValueKind.Number, JsonValueKind.Number)
                when Min.TryGetDouble(out var minDoubleValue) && Max.TryGetDouble(out var maxDoubleValue) =>
                    arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetDouble(out var vdoubleValue) && vdoubleValue>=minDoubleValue && vdoubleValue<=maxDoubleValue),
            (JsonValueKind.Array, JsonValueKind.Number, JsonValueKind.Number)
                when Min.TryGetSingle(out var minFloatValue) && Max.TryGetSingle(out var maxFloatValue) =>
                    arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetSingle(out var vfloatvalue) && vfloatvalue>=minFloatValue && vfloatvalue<=maxFloatValue),
            _ => false 
        };
    }

    private bool CheckDateTime(JsonElement arg)
        => arg.TryGetDateTimeOffset(out var argDateTime)
        && Min.TryGetDateTimeOffset(out var minDateTime)
        && Max.TryGetDateTimeOffset(out var maxDateTime)
        && argDateTime>=minDateTime
        && argDateTime<=maxDateTime;
}
