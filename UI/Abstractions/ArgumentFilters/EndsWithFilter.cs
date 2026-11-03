using JetFlow.UI.Interfaces;
using System.Text.Json;

namespace JetFlow.UI.ArgumentFilters;

public class EndsWithFilter : IArgumentFilter
{
    public required string PropertyName { get; init; }
    public required string Value { get; init; }
    bool IArgumentFilter.IsMatch(JsonElement argument)
    {
        var arg = ArgumentFilterHelper.GetValue(argument, PropertyName);
        return arg.ValueKind switch
        {
            JsonValueKind.String => arg.GetString()?.EndsWith(Value, StringComparison.InvariantCulture)??false,
            JsonValueKind.Array => arg.EnumerateArray().Any(v => v.GetString()?.EndsWith(Value, StringComparison.InvariantCulture)??false),
            _ => false
        };
    }
}
