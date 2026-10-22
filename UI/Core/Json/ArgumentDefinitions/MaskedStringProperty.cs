using JetFlow.UI.Interfaces;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JetFlow.UI.Json.ArgumentDefinitions;

internal class MaskedStringProperty : IArgumentDefinition
{
    private const char MaskChar = '*';
    public enum MaskedType
    {
        Start,
        End
    };

    [JsonPropertyName("unmaskedLength")]
    public uint UnmaskedLength { get; init; } = 4;
    [JsonPropertyName("maskedType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MaskedType MaskType { get; init; } = MaskedType.Start;

    JsonNode IArgumentDefinition.ApplyDefinition(JsonNode input)
    {
        var content = input.GetValueKind() == System.Text.Json.JsonValueKind.String ? input.GetValue<string>() : JsonNode.Parse(input.ToJsonString())!.GetValue<string>();
        return JsonValue.Create((MaskType, UnmaskedLength) switch {
            (_, 0) => new string(MaskChar, content.Length),
            (_, _) when (int)UnmaskedLength>content.Length => content,
            (MaskedType.Start, _) => $"{new string(MaskChar, content.Length-(int)UnmaskedLength)}{content[^((int)UnmaskedLength)..]}",
            _ => $"{content[..(int)UnmaskedLength]}{new string(MaskChar, content.Length-(int)UnmaskedLength)}"
        });
    }
}
