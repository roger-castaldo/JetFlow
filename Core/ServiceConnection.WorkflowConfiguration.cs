using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Serializers;

namespace JetFlow;

internal partial class ServiceConnection
{
    public const string DefaultConfigKey = "__DEFAULT";
    private static readonly WorkflowOptionsSerializer optionsSerializer = new();

    public async ValueTask RegisterWorkflowConfigAsync<TWorkflow>(WorkflowOptions? workflowOptions)
    {
        if (workflowOptions == null)
            await configurationStore.TryDeleteAsync(NameHelper.GetWorkflowName<TWorkflow>());
        else
            await configurationStore.PutAsync<WorkflowOptions>(NameHelper.GetWorkflowName<TWorkflow>(), workflowOptions, serializer: optionsSerializer);
    }

    public async ValueTask<WorkflowOptions> GetWorkflowOptions(string name)
    {
        var currentValue = await configurationStore.TryGetEntryAsync<WorkflowOptions>(name, serializer: optionsSerializer);
        if (!currentValue.Success)
            currentValue = await configurationStore.TryGetEntryAsync<WorkflowOptions>(DefaultConfigKey, serializer: optionsSerializer);
        return currentValue.Success ? currentValue.Value.Value! : new();
    }
}
