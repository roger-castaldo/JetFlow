using System.Text.Json.Serialization;

namespace JetFlow.UI;

public enum SkinTypes
{
    Cerulean, Cosmo, Cyborg, Dark, Darkly, Default, Flatly, Journal, Light,
    Litera, Lumen, Lux, Materia, Minty, Morph, Pulse, Quartz, Sandstone, Simplex,
    Sketchy, Slate, Solar, Spacelab, Superhero, United, Vapor, Yeti, Zephyr
};

public sealed record JetflowUIConfig
{
    public string? DefaultNamespace { get; init; } = null;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SkinTypes Skin { get; init; } = SkinTypes.Lumen;
}
