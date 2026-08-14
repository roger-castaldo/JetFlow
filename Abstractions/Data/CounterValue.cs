using System.Text.Json.Serialization;

namespace JetFlow.Data;

internal record CounterValue(
    [property: JsonPropertyName("val")]
    string? Val
);
