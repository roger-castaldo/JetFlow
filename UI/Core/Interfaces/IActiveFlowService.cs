using JetFlow.UI.Data;

namespace JetFlow.UI.Interfaces;

internal interface IActiveFlowService
{
    ValueTask<PerformanceCounters> GetPerformanceCountersAsync(string? workflowNamespace);
    ValueTask<IEnumerable<NamedServiceabilityDetails>> GetWorkflowServiceability(string? workflowNamespace);
    ValueTask<IEnumerable<NamedServiceabilityDetails>> GetActivityServiceability(string? workflowNamespace);
}
