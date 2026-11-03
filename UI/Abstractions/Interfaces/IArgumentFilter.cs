using JetFlow.UI.ArgumentFilters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JetFlow.UI.Interfaces;

[JsonDerivedType(typeof(StartsWithFilter),"startsWith")]
[JsonDerivedType(typeof(EndsWithFilter), "endsWith")]
[JsonDerivedType(typeof(BetweenFilter), "between")]
[JsonDerivedType(typeof(EqualsFilter), "equals")]
[JsonDerivedType(typeof(ContainsFilter), "contains")]
[JsonDerivedType(typeof(LessThanFilter), "lessThan")]
[JsonDerivedType(typeof(GreaterThanFilter), "greaterThan")]
public interface IArgumentFilter
{
    bool IsMatch(JsonElement argument);
}
