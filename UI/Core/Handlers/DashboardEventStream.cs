using JetFlow.UI.Interfaces;
using System.Net.ServerSentEvents;

namespace JetFlow.UI.Handlers;

internal class DashboardEventStream(string? namespaceName, IActiveFlowService activeFlowService, IDbConnection dbConnection) : IAsyncEnumerable<SseItem<object>>
{
    private record struct ActivityPerformanceRecordSummary(DateTimeOffset Window, string Name, long Started, long Completed, long Failed, long TimedOut, TimeSpan AverageQueueLatencies, TimeSpan AverageDurations);
    private record struct WorkflowPerformanceRecordSummary(DateTimeOffset Window, string Name, long Started, long Completed, long Failed, long Purged, TimeSpan AverageQueueLatencies);

    async IAsyncEnumerator<SseItem<object>> IAsyncEnumerable<SseItem<object>>.GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        yield return new SseItem<object>(
            string.Empty,
            "established"
        );
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return new SseItem<object>(
                await activeFlowService.GetPerformanceCountersAsync(namespaceName),
                "performanceCounters"
            );
            yield return new SseItem<object>(
                (await dbConnection.PeakActivityPerformanceDataAsync(namespaceName)).Take(10)
                .Select(perf=> new ActivityPerformanceRecordSummary(perf.Window, perf.Name, perf.Started, perf.Completed, perf.Failed, perf.TimedOut, 
                    new((long)Math.Floor(perf.QueueLatencies.Select(t=>t.Ticks).Average())),
                    new((long)Math.Floor(perf.Durations.Select(t=>t.Ticks).Average()))
                    )
                ),
                "activityPerformance"
            );
            yield return new SseItem<object>(
                (await dbConnection.PeakWorkflowPerformanceDataAsync(namespaceName)).Take(10).Select(perf => new WorkflowPerformanceRecordSummary(perf.Window, perf.Name, perf.Started, perf.Completed, perf.Failed, perf.Purged,
                    new((long)Math.Floor(perf.QueueLatencies.Select(t => t.Ticks).Average()))
                    )
                ),
                "workflowPerformance"
            );
            yield return new SseItem<object>(
                await activeFlowService.GetWorkflowServiceability(namespaceName),
                "workflowServiceability"
            );
            yield return new SseItem<object>(
                await activeFlowService.GetActivityServiceability(namespaceName),
                "activityServiceability"
            );
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }
        yield break;
    }
}
