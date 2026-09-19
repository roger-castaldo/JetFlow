using JetFlow.UI.Interfaces;
using System.Net.ServerSentEvents;

namespace JetFlow.UI.Handlers;

internal class DashboardEventStream(string? namespaceName, IActiveFlowService activeFlowService, IDbConnection dbConnection) : IAsyncEnumerable<SseItem<object>>
{
    async IAsyncEnumerator<SseItem<object>> IAsyncEnumerable<SseItem<object>>.GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return new SseItem<object>(
                await activeFlowService.GetPerformanceCountersAsync(namespaceName),
                "performanceCounters"
            );
            yield return new SseItem<object>(
                (await dbConnection.PeakActivityPerformanceDataAsync(namespaceName)).Take(10),
                "activityPerformance"
            );
            yield return new SseItem<object>(
                (await dbConnection.PeakWorkflowPerformanceDataAsync(namespaceName)).Take(10),
                "workflowPerformance"
            );
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
        yield break;
    }
}
