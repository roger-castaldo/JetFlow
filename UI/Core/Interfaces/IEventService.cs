using JetFlow.UI.Messages;
using System.Text.Json;

namespace JetFlow.UI.Interfaces;

internal interface IEventService
{
    void RegisterEventListener(string type, Func<JsonElement?, ValueTask> callBack);
    ValueTask PublishEvent(Event @event);
}
