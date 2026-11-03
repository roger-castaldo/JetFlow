using JetFlow.UI.Interfaces;
using System.Text.Json;

namespace JetFlow.UI.ArgumentFilters;

public class LessThanFilter : IArgumentFilter
{
    public required string PropertyName { get; init; }
    public required JsonElement Value { get; init; }
    public required bool OrEqualTo { get; init; } = false;
    bool IArgumentFilter.IsMatch(JsonElement argument)
    {
        var arg = ArgumentFilterHelper.GetValue(argument, PropertyName);
        return (arg.ValueKind, Value.ValueKind) switch
        {
            (JsonValueKind.String, JsonValueKind.String) 
                when Value.TryGetDateTimeOffset(out var datetimeValue) => 
                    arg.TryGetDateTimeOffset(out var argDatetimeValue) && (datetimeValue>argDatetimeValue || (OrEqualTo && datetimeValue.Equals(argDatetimeValue))),
            (JsonValueKind.Array, JsonValueKind.String) 
                when Value.TryGetDateTimeOffset(out var datetimeValue) => 
                    arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.String && v.TryGetDateTimeOffset(out var vdatetimeValue) && (datetimeValue>vdatetimeValue || (OrEqualTo && datetimeValue.Equals(vdatetimeValue)))),
            (JsonValueKind.Array, JsonValueKind.Number)
                when Value.TryGetInt64(out var longValue) =>
                    arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetInt64(out var vlongValue) && (longValue>vlongValue ||(OrEqualTo && Equals(vlongValue, longValue)))),
            (JsonValueKind.Array, JsonValueKind.Number)
                when Value.TryGetUInt64(out var ulongValue) =>
                    arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetUInt64(out var vulongValue) && (ulongValue>vulongValue ||(OrEqualTo && Equals(vulongValue, ulongValue)))),
            (JsonValueKind.Array, JsonValueKind.Number)
                when Value.TryGetDecimal(out var decimalValue) =>
                    arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetDecimal(out var vdecimalValue) && (decimalValue>vdecimalValue ||(OrEqualTo && Equals(vdecimalValue, decimalValue)))),
            (JsonValueKind.Array, JsonValueKind.Number)
                when Value.TryGetDouble(out var doubleValue) =>
                    arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetDouble(out var vdoubleValue) && (doubleValue>vdoubleValue ||(OrEqualTo && Equals(vdoubleValue, doubleValue)))),
            (JsonValueKind.Array, JsonValueKind.Number)
                when Value.TryGetSingle(out var floatValue) =>
                    arg.EnumerateArray().Any(v => v.ValueKind==JsonValueKind.Number && v.TryGetSingle(out var vfloatValue) && (floatValue>vfloatValue ||(OrEqualTo && Equals(vfloatValue, floatValue)))),
            _ => false 
        };
    }
}
