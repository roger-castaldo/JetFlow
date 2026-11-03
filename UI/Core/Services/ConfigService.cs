using JetFlow.UI.Helpers;
using JetFlow.UI.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace JetFlow.UI.Services;

internal class ConfigService
    : IConfigService
{
    private record struct NamespacedItem(string? namespaceName, string itemName);
    private readonly ConcurrentBag<Func<string?, Task>> addNamespaceCallbacks = [];
    private readonly ConcurrentBag<Func<string?, Task>> removeNamespaceCallbacks = [];
    private readonly ConcurrentDictionary<NamespacedItem, IArgumentDefinition?> workflowArguments = [];
    private readonly ConcurrentDictionary<NamespacedItem, IArgumentDefinition?> activityArguments = [];
    private readonly IDbConnection dbConnection;
    private readonly IEventService eventService;

    public ConfigService(IDbConnection dbConnection, IEventService eventService)
    {
        this.dbConnection = dbConnection;
        this.eventService = eventService;
        eventService.RegisterEventListener(EventsHelper.NamespaceEvent,async (element) =>
        {
            if (element.HasValue)
            {
                var (add, namespaceName) = EventsHelper.ExtractNamespaceChangeDetails(element.Value);
                if (add)
                    await Task.WhenAll(addNamespaceCallbacks.Select(callback => callback(namespaceName)));
                else
                    await Task.WhenAll(removeNamespaceCallbacks.Select(callback => callback(namespaceName)));
            }
        });
        eventService.RegisterEventListener(EventsHelper.WorkflowArgumentDefinitionEvent, (element) =>
        {
            if (element.HasValue) {
                var (namespaceName, workflowName) = EventsHelper.ExtractWorkflowArgumentChangeDetials(element.Value);
                workflowArguments.Remove(new(namespaceName, workflowName), out _);
            }
            return ValueTask.CompletedTask;
        });
        eventService.RegisterEventListener(EventsHelper.ActivityArgumentDefinitionEvent, (element) =>
        {
            if (element.HasValue)
            {
                var (namespaceName, activityName) = EventsHelper.ExtractActivityArgumentChangeDetials(element.Value);
                activityArguments.Remove(new(namespaceName, activityName), out _);
            }
            return ValueTask.CompletedTask;
        });
    }


    async ValueTask IConfigService.RegisterNamespaceAsync(string? namespaceName)
    {
        await dbConnection.RegisterNamespaceAsync(namespaceName);
        //await Task.WhenAll(addNamespaceCallbacks.Select(callback => callback(namespaceName)));
        await eventService.PublishEvent(EventsHelper.ProduceNamespaceChange(true, namespaceName));
    }

    void IConfigService.RegisterAddNamespaceCallback(Func<string?, Task> callback)
        => addNamespaceCallbacks.Add(callback);

    void IConfigService.RegisterRemoveNamespaceCallback(Func<string?, Task> callback)
        => removeNamespaceCallbacks.Add(callback);

    async ValueTask IConfigService.UnregisterNamespaceAsync(string? namespaceName)
    {
        await dbConnection.UnregisterNamespaceAsync(namespaceName);
        //await Task.WhenAll(removeNamespaceCallbacks.Select(callback => callback(namespaceName)));
        await eventService.PublishEvent(EventsHelper.ProduceNamespaceChange(false, namespaceName));
    }

    ValueTask<IEnumerable<string?>> IConfigService.GetCurrentNamespacesAsync()
        => dbConnection.ListNamespacesAsync();

    async ValueTask<IArgumentDefinition?> IConfigService.GetWorkflowArgumentDefinitionAsync(string? namespaceName, string workflowName)
    {
        var key = new NamespacedItem(namespaceName, workflowName);
        if (!workflowArguments.TryGetValue(key, out var result))
        {
            var doc = await dbConnection.GetWorkflowArgumentDefinitionAsync(namespaceName, workflowName);
            if (doc!=null)
                result = JsonSerializer.Deserialize<IArgumentDefinition>(doc);
            workflowArguments.TryAdd(key, result);
        }
        return result;
    }

    async ValueTask<IArgumentDefinition?> IConfigService.GetActivityArgumentDefinitionAsync(string? namespaceName, string activityName)
    {
        var key = new NamespacedItem(namespaceName, activityName);
        if (!activityArguments.TryGetValue(key, out var result))
        {
            var doc = await dbConnection.GetActivityArgumentDefinitionAsync(namespaceName, activityName);
            if (doc!=null)
                result = JsonSerializer.Deserialize<IArgumentDefinition>(doc);
            activityArguments.TryAdd(key, result);
        }
        return result;
    }
}
