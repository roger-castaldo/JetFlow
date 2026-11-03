using System.Text.Json;

namespace JetFlow.UI.Messages;

internal record Event(string Type, JsonElement? Data);

