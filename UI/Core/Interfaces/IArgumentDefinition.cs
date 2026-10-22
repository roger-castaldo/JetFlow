using JetFlow.UI.Json.ArgumentDefinitions;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JetFlow.UI.Interfaces;

[JsonDerivedType(typeof(StringProperty), "string")]
[JsonDerivedType(typeof(ObjectProperty), "object")]
[JsonDerivedType(typeof(ArrayProperty), "array")]
[JsonDerivedType(typeof(MaskedStringProperty),"maskedString")]
[JsonDerivedType(typeof(BooleanProperty), "boolean")]
[JsonDerivedType(typeof(NumberProperty), "number")]
[JsonDerivedType(typeof(DateTimeProperty), "datetime")]
internal interface IArgumentDefinition
{
    JsonNode ApplyDefinition(JsonNode input);
}
