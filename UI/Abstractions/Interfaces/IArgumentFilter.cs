using JetFlow.UI.ArgumentFilters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JetFlow.UI.Interfaces;

[JsonDerivedType(typeof(StartsWithFilter),"startsWith")]
[JsonDerivedType(typeof(EndsWithFilter), "endsWith")]
public interface IArgumentFilter
{
    bool IsMatch(JsonElement argument);
}
