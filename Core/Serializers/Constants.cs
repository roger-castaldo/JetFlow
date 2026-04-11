using System.Text.Json;

namespace JetFlow.Serializers;

internal static class Constants
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented=false,
        AllowTrailingCommas=true,
        PropertyNameCaseInsensitive=true,
        ReadCommentHandling=JsonCommentHandling.Skip
    };
}
