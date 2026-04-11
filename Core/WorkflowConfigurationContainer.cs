using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Serializers;
using NATS.Client.KeyValueStore;

namespace JetFlow;

internal class WorkflowConfigurationContainer
{
    private const string DefaultKey = "__DEFAULT";

    private static readonly WorkflowOptionsSerializer optionsSerializer = new();

    public static async ValueTask<WorkflowConfigurationContainer> CreateAsync(INatsKVContext kvContext, ConnectionOptions connectionOotions)
    {
        var kvStore = await kvContext.CreateOrUpdateStoreAsync(new("JETFLOW_WORKFLOW_CONFIGS")
        {
            Description = "KeyValue store for workflow configurations",
            History = 1,
            LimitMarkerTTL = TimeSpan.FromMinutes(1)
        });
        await kvStore.PutAsync<WorkflowOptions>(DefaultKey, connectionOotions.DefaultWorkflowOptions, serializer: optionsSerializer);
        return new(kvStore);
    }

    private readonly INatsKVStore kvStore;

    private WorkflowConfigurationContainer(INatsKVStore kvStore)
        => this.kvStore = kvStore;

    public async ValueTask RegisterWorkflowConfigAsync<TWorkflow>(WorkflowOptions? workflowOptions)
    {
        if (workflowOptions == null)
            await kvStore.TryDeleteAsync(NameHelper.GetWorkflowName<TWorkflow>());
        else
            await kvStore.PutAsync<WorkflowOptions>(NameHelper.GetWorkflowName<TWorkflow>(), workflowOptions, serializer: optionsSerializer);
    }

    public async ValueTask<WorkflowOptions> GetInstanceConfigAsync(EventMessage message)
    {
        var currentValue = await kvStore.TryGetEntryAsync<WorkflowOptions>($"{message.WorkflowName}_{message.WorkflowId}", serializer: optionsSerializer);
        if (currentValue.Success)
            return currentValue.Value.Value!;
        currentValue = await kvStore.TryGetEntryAsync<WorkflowOptions>(message.WorkflowName, serializer: optionsSerializer);
        if (!currentValue.Success)
            currentValue = await kvStore.TryGetEntryAsync<WorkflowOptions>(DefaultKey, serializer: optionsSerializer);
        if(currentValue.Success)
        {
            await kvStore.PutAsync<WorkflowOptions>($"{message.WorkflowName}_{message.WorkflowId}", currentValue.Value.Value!, serializer: optionsSerializer);
            return currentValue.Value.Value!;
        }
        return new();
    }
}
