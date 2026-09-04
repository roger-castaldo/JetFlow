using JetFlow.Data;
using NATS.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JetFlow.Helpers;

internal class ObservationMetricsCollector : IAsyncDisposable
{
    private enum PerformanceEntryType
    {
        Activity,
        Workflow
    }
    private enum PerformanceEntryEvent
    {
        Started,
        TimedOut,
        Failed, 
        Completed, 
        Purged,
        NonRecording
    }
    private record struct PerformanceEntry(string Name, PerformanceEntryType Type, PerformanceEntryEvent Event, TimeSpan? QueueLatency = null, TimeSpan? ProcessingTime = null);

    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly Task runningTask;
    private readonly InternalNatsConnection natsConnection;
    private readonly SubjectMapper subjectMapper;
    private readonly List<PerformanceEntry> performanceEntries = new();
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented=false,
        AllowTrailingCommas=true,
        PropertyNameCaseInsensitive=true,
        ReadCommentHandling=JsonCommentHandling.Skip,
        DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = ObservationJsonContext.Default
    };
    private short? interval = null;

    public ObservationMetricsCollector(InternalNatsConnection natsConnection, SubjectMapper subjectMapper)
    {
        this.natsConnection = natsConnection;
        this.subjectMapper = subjectMapper;
        runningTask = Task.Run(RunAsync);
    }

    private async Task RunAsync()
    {
        var kc = natsConnection.JSContext.CreateKeyValueStoreContext();
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                var configStore = await kc.GetStoreAsync(subjectMapper.WorkflowConfigKeystore, cancellationToken: cancellationTokenSource.Token);
                var getEntryResult = await configStore.TryGetEntryAsync<short>(subjectMapper.PerformanceSamplingKey, cancellationToken: cancellationTokenSource.Token);
                if (getEntryResult.Success)
                    interval = getEntryResult.Value.Value;
                var next = GetNextOccurrence(DateTimeOffset.Now, interval??5);
                await Task.Delay(next - DateTimeOffset.Now, cancellationTokenSource.Token);
                await semaphoreSlim.WaitAsync();
                var entries = performanceEntries.ToArray();
                performanceEntries.Clear();
                semaphoreSlim.Release();
                if (interval==null)
                {
                    getEntryResult = await configStore.TryGetEntryAsync<short>(subjectMapper.PerformanceSamplingKey, cancellationToken: cancellationTokenSource.Token);
                    if (getEntryResult.Success)
                        interval = getEntryResult.Value.Value;
                }
                if (interval!=null)
                    await TransmitPerformanceEntriesAsync(next.ToUniversalTime(), entries, cancellationTokenSource.Token);
            }catch(OperationCanceledException)
            {
                //ignore cancellation exception
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    private async Task TransmitPerformanceEntriesAsync(DateTimeOffset window, PerformanceEntry[] performanceEntries, CancellationToken cancellationToken)
    {
        foreach(var typeGroup in performanceEntries.GroupBy(e => e.Type))
        {
            foreach(var nameGroup in typeGroup.GroupBy(e => e.Name))
            {
                var name = nameGroup.Key;
                if (typeGroup.Key == PerformanceEntryType.Activity)
                    await natsConnection.PublishMessageAsync(
                        new(JsonSerializer.SerializeToUtf8Bytes<ActivityPerformanceRecord>(new(
                            window,
                            nameGroup.Key,
                            nameGroup.Count(e => e.Event == PerformanceEntryEvent.Started),
                            nameGroup.Count(e => e.Event == PerformanceEntryEvent.Completed),
                            nameGroup.Count(e => e.Event == PerformanceEntryEvent.Failed),
                            nameGroup.Count(e => e.Event == PerformanceEntryEvent.TimedOut),
                            nameGroup.Where(e => e.QueueLatency.HasValue).Select(e => e.QueueLatency!.Value),
                            nameGroup.Where(e => e.ProcessingTime.HasValue).Select(e => e.ProcessingTime!.Value)
                        ), options: jsonOptions),
                        subjectMapper.ActivityPerformanceSubject,
                        new(),
                        Guid.CreateVersion7().ToString()
                    ), cancellationToken: cancellationToken);
                else
                    await natsConnection.PublishMessageAsync(
                        new(JsonSerializer.SerializeToUtf8Bytes<WorkflowPerformanceRecord>(new(
                            window,
                            nameGroup.Key,
                            nameGroup.Count(e => e.Event == PerformanceEntryEvent.Started),
                            nameGroup.Count(e => e.Event == PerformanceEntryEvent.Completed),
                            nameGroup.Count(e => e.Event == PerformanceEntryEvent.Failed),
                            nameGroup.Count(e => e.Event == PerformanceEntryEvent.Purged),
                            nameGroup.Where(e => e.QueueLatency.HasValue).Select(e => e.QueueLatency!.Value)
                        ), options: jsonOptions),
                        subjectMapper.WorkflowPerformanceSubject,
                        new(),
                        Guid.CreateVersion7().ToString()
                    ), cancellationToken: cancellationToken);
            }
        }
    }

    private DateTimeOffset GetNextOccurrence(DateTimeOffset now, short interval)
    {
        var minute = now.Minute;

        var nextMinute = ((minute / interval) + 1) * interval;

        if (nextMinute >= 60)
        {
            return new DateTimeOffset(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                0,
                0,
                now.Offset)
                .AddHours(1);
        }

        return new DateTimeOffset(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            nextMinute,
            0,
            now.Offset);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (!cancellationTokenSource.IsCancellationRequested)
        {
            await cancellationTokenSource.CancelAsync();
            await (runningTask.IsCompleted ? Task.CompletedTask : runningTask);
            semaphoreSlim.Dispose();
            if (performanceEntries.Count>0)
            {
                try
                {
                    await TransmitPerformanceEntriesAsync(GetNextOccurrence(DateTimeOffset.Now, interval??5).ToUniversalTime(), [.. performanceEntries], CancellationToken.None);
                }
                catch (Exception)
                {
                    //burying error as this is an attempt to transmit the last batch of performance entries before shutdown, and we don't want to throw an exception during disposal
                }
            }
        }
    }

    private void AddEntry(PerformanceEntry entry)
    {
        semaphoreSlim.Wait();
        performanceEntries.Add(entry);
        semaphoreSlim.Release();
    }

    internal void StartActivity(string? activityName, TimeSpan queueLatency)
        => AddEntry(new(activityName ?? "Unknown", PerformanceEntryType.Activity, PerformanceEntryEvent.Started, QueueLatency: queueLatency));
    internal void CompleteActivity(string? activityName, TimeSpan duration)
        => AddEntry(new(activityName ?? "Unknown", PerformanceEntryType.Activity, PerformanceEntryEvent.Completed, ProcessingTime: duration));
    internal void FailActivity(string? activityName)
        => AddEntry(new(activityName ?? "Unknown", PerformanceEntryType.Activity, PerformanceEntryEvent.Failed));
    internal void TimeoutActivity(string? activityName)
        => AddEntry(new(activityName ?? "Unknown", PerformanceEntryType.Activity, PerformanceEntryEvent.TimedOut));
    internal void StartWorkflow(string workflowName, TimeSpan timeSpan)
        => AddEntry(new(workflowName, PerformanceEntryType.Workflow, PerformanceEntryEvent.Started, QueueLatency: timeSpan));
    internal void RecordWorkflowLatency(string workflowName, TimeSpan timeSpan)
        => AddEntry(new(workflowName, PerformanceEntryType.Workflow, PerformanceEntryEvent.NonRecording, QueueLatency: timeSpan));
    internal void CompleteWorkflow(string workflowName)
        => AddEntry(new(workflowName, PerformanceEntryType.Workflow, PerformanceEntryEvent.Completed));
    internal void FailWorkflow(string workflowName)
        => AddEntry(new(workflowName, PerformanceEntryType.Workflow, PerformanceEntryEvent.Failed));
    internal void PurgeWorkflow(string workflowName)
        => AddEntry(new(workflowName, PerformanceEntryType.Workflow, PerformanceEntryEvent.Purged));
}
