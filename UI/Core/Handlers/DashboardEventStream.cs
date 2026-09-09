using JetFlow.UI.Interfaces;
using System.Net.ServerSentEvents;

namespace JetFlow.UI.Handlers;

internal class DashboardEventStream(string namespaceName, IActiveFlowService activeFlowService) : IAsyncEnumerable<SseItem<object>>
{
    async IAsyncEnumerator<SseItem<object>> IAsyncEnumerable<SseItem<object>>.GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return new SseItem<object>(
                await activeFlowService.GetPerformanceCountersAsync(namespaceName),
                "performanceCounters"
            );
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
        yield break;
    }
}
