using JetFlow.Interfaces;
using JetFlow.UI.Data;
using System.Text.Json;

namespace JetFlow.UI.Interfaces;

internal interface IActiveFlowService
{
    ValueTask<PerformanceCounters> GetPerformanceCountersAsync(string? workflowNamespace);
    ValueTask<IEnumerable<NamedServiceabilityDetails>> GetWorkflowServiceability(string? workflowNamespace);
    ValueTask<IEnumerable<NamedServiceabilityDetails>> GetActivityServiceability(string? workflowNamespace);
    ValueTask<IPagedWorkflowQuery> QueryActiveFlowsAsync(string? workflowNamespace, string workflowName, uint take, ulong lastOffset, Func<Dictionary<string, string[]>?, bool>? checkMetaData, Func<JsonElement, bool>? checkArguement);
    ValueTask<IEnumerable<ScheduledWorkflow<object>>> ListScheduledWorkflowsAsync(string? workflowNamespace);
}
