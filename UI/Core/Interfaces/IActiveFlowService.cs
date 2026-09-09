using JetFlow.UI.Data;

namespace JetFlow.UI.Interfaces;

internal interface IActiveFlowService
{
    ValueTask<PerformanceCounters> GetPerformanceCountersAsync(string workflowNamespace);
}
