using JetFlow.UI.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JetFlow.UI.Helpers;

internal static class WorkflowsHelper
{
    private static async Task<WorkflowStep> FixStepInputAsync(IConfigService configService, string? namespaceName, WorkflowStep step)
    {
        if (step.Input==null || step.Type!= WorkflowStepTypes.Action)
            return step;
        var masker = await configService.GetActivityArgumentDefinitionAsync(namespaceName, step.Name!);
        if (masker!=null)
            return step with
            {
                Input = masker.ApplyDefinition(JsonNode.Parse(JsonSerializer.Serialize(step.Input))!)
            };
        return step;
    }

    private static async ValueTask<object?> FixWorkflowArgumentsAsync(IConfigService configService, string? namespaceName, string flowName, object? arguments)
    {
        if (arguments==null)
            return arguments;
        var masker = await configService.GetWorkflowArgumentDefinitionAsync(namespaceName, flowName);
        if (masker!=null)
            return masker.ApplyDefinition(JsonNode.Parse(JsonSerializer.Serialize(arguments))!);
        return arguments;
    }

    public static async ValueTask<IEnumerable<ActiveWorkflow>> MaskArgumentsAsync(IConfigService configService, string? namespaceName, IEnumerable<ActiveWorkflow> workflows)
        => await Task.WhenAll(workflows
                .Select(async flow =>
                {
                    return flow with
                    {
                        Arguments = await FixWorkflowArgumentsAsync(configService, namespaceName, flow.Name, flow.Arguments),
                        Steps = await Task.WhenAll(flow.Steps.Select(async step => await FixStepInputAsync(configService, namespaceName, step)))
                    };
                })
            );

    public static async ValueTask<IEnumerable<ArchivedWorkflow>> MaskArgumentsAsync(IConfigService configService, string? namespaceName, IEnumerable<ArchivedWorkflow> workflows)
        => await Task.WhenAll(workflows
                .Select(async flow =>
                {
                    return flow with
                    {
                        Arguments = await FixWorkflowArgumentsAsync(configService, namespaceName, flow.Name, flow.Arguments),
                        Steps = await Task.WhenAll(flow.Steps.Select(async step => await FixStepInputAsync(configService, namespaceName, step)))
                    };
                })
            );
}
