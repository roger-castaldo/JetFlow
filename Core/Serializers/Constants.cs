using System.Text.Json;
using System.Text.Json.Serialization;

namespace JetFlow.Serializers;

internal static class Constants
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented=false,
        AllowTrailingCommas=true,
        PropertyNameCaseInsensitive=true,
        ReadCommentHandling=JsonCommentHandling.Skip,
        DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingNull
    };
}
