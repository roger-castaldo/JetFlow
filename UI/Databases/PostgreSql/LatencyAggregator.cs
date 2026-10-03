using JetFlow.UI.dto;
using JetFlow.UI.Helpers;
using Npgsql;
using System.Text.Json;

namespace JetFlow.UI;

internal class LatencyAggregator(string connectionString)
    : IAsyncDisposable
{
    private const string aggLock = "SELECT pg_try_advisory_lock(36942);";
    private const string aggUnlock = "SELECT pg_advisory_unlock(36942);";

    private sealed record BucketDefinition(DateTime BucketStart, DateTime BucketEnd);

    private readonly CancellationTokenSource tokenSource = new();
    private readonly QueryHelper queryHelper = new(connectionString);
    private Task? runningTask=null;
    public void Start() {
        runningTask = RunBackground();
    }

    private async Task RunBackground()
    {
        while (!tokenSource.IsCancellationRequested)
        {
            await using var dataSource = NpgsqlDataSource.Create(connectionString);
            await using var connection = await dataSource.OpenConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandText = aggLock;
            var lockAcquired = (bool)(await command.ExecuteScalarAsync())!;
            if (lockAcquired)
            {
                try
                {
                    await AggregateRawWorkflows(connection);
                    await AggregateRawActivities(connection);
                    await queryHelper.ExecuteCommand("CALL cleanup_performance_data()", []);
                }
                catch
                {
                }
                finally
                {
                    command.CommandText = aggUnlock;
                    await command.ExecuteScalarAsync();
                }
            }
            await Task.Delay(TimeSpan.FromMinutes(1), tokenSource.Token);
        }
    }

    private async Task AggregateRawWorkflows(NpgsqlConnection connection)
    {
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var results = await queryHelper.ExecuteReader(connection, transaction, new("SELECT * FROM \"workflow_performance_raw\";", []));
            var rawPerformances = results?.Select(r => new WorkflowPerformanceRaw(
                (Guid)r["id"],
                (DateTime)r["window"],
                (Guid)r["workflow_id"],
                (Guid)r["namespace_id"],
                (long)r["started"],
                (long)r["completed"],
                (long)r["failed"],
                (long)r["purged"],
                JsonSerializer.Deserialize<IEnumerable<TimeSpan>>(r["queue_latencies"].ToString()!)!
            ))
                .ToArray()?? [];
            var performances = rawPerformances
                .GroupBy(r => new { r.Window, r.WorkflowID, r.NamespaceID })
                .Select(grp =>
                {
                    var latencySketch = new TimespanSketch();
                    latencySketch.Add(grp.SelectMany(r => r.QueueLatencies));
                    return new WorkflowPerformance(
                        grp.Key.Window,
                        grp.Key.WorkflowID,
                        grp.Key.NamespaceID,
                        PerformanceAggregationType.OneMinute,
                        grp.Sum(r=>r.Started),
                        grp.Sum(r=>r.Completed),
                        grp.Sum(r=>r.Failed),
                        grp.Sum(r=>r.Purged),
                        latencySketch
                    );
                })
                .ToArray();
            var commands = rawPerformances
                .Select(perf=>new SqlCommand(
                    "DELETE FROM \"workflow_performance_raw\" WHERE \"id\" = @id",
                    [new("@id", perf.ID)]
                ))
                .ToList();
            foreach (var performance in performances)
            {
                var currentValue = (await queryHelper.ExecuteReader(connection, transaction, new(
                    "SELECT * FROM \"workflow_performance\" WHERE \"bucket_start\" = @bucketStart AND \"workflow_id\" = @workflowID AND \"namespace_id\" = @namespaceID AND \"aggregation_type\" = 'OneMinute';",
                    [
                        new("@bucketStart", performance.BucketStart),
                        new("@workflowID", performance.WorkflowID),
                        new("@namespaceID", performance.NamespaceID)
                    ]
                )))?.FirstOrDefault();
                var insertableObject = performance;
                if (currentValue!=null)
                    insertableObject = performance.Merge(LoadWorkflowPerformance(currentValue));
                commands.AddRange(InsertWorkflowPerformance(insertableObject, PerformanceAggregationType.OneMinute));
            }
            await queryHelper.ExecuteCommands(connection, transaction, commands);
            foreach(var grp in performances.GroupBy(p => new { p.WorkflowID, p.NamespaceID }))
                await RebuildWorkflowAggregates(connection, transaction, grp.Min(p => p.BucketStart), grp.Max(p => p.BucketStart), grp.Key.WorkflowID, grp.Key.NamespaceID);
        }
        catch
        {
            await transaction.RollbackAsync();
        }
        await transaction.CommitAsync();
    }

    private async Task RebuildWorkflowAggregates(NpgsqlConnection connection, NpgsqlTransaction transaction, DateTime startDate, DateTime endDate, Guid workflowID, Guid namespaceID)
    {
        startDate = startDate.AddMinutes(0 - (startDate.Minute%5)).AddMinutes(-5);
        await RebuildWorkflowAggregate(connection, transaction, startDate, endDate, workflowID, namespaceID, PerformanceAggregationType.OneMinute, PerformanceAggregationType.FiveMinute, (date) => new(date.AddMinutes(5), date.AddMinutes(10)));
        startDate = startDate.AddMinutes(0 - (startDate.Minute%60)).AddHours(-1);
        await RebuildWorkflowAggregate(connection, transaction, startDate, endDate, workflowID, namespaceID, PerformanceAggregationType.FiveMinute, PerformanceAggregationType.OneHour, (date) => new(date.AddHours(1), date.AddHours(2)));
        startDate = startDate.AddHours(0 - startDate.Hour%23).AddDays(-1);
        await RebuildWorkflowAggregate(connection, transaction, startDate, endDate, workflowID, namespaceID, PerformanceAggregationType.OneHour, PerformanceAggregationType.OneDay, (date) => new(date.AddDays(1), date.AddDays(2)));
    }

    private async Task RebuildWorkflowAggregate(NpgsqlConnection connection, NpgsqlTransaction transaction, DateTime startDate, DateTime endDate, Guid workflowID, Guid namespaceID, PerformanceAggregationType sourceType, PerformanceAggregationType destinationType, Func<DateTime, BucketDefinition> nextBucket)
    {
        var commands = new List<SqlCommand>();
        while(startDate.Ticks < endDate.Ticks)
        {
            var bucket = nextBucket(startDate);
            var aggs = (await queryHelper.ExecuteReader(connection, transaction, new(
                "SELECT * from \"workflow_performance\" WHERE \"bucket_start\" >= @bucketStart AND \"bucket_start\" < @bucketEnd AND \"aggregation_type\" = @aggregationType::performance_aggregation_types AND \"workflow_id\" = @workflowID AND \"namespace_id\" = @namespaceID;",
                [
                    new("@bucketStart",bucket.BucketStart),
                    new("@bucketEnd",bucket.BucketEnd),
                    new("@aggregationType", sourceType.ToString()),
                    new("@workflowID", workflowID),
                    new("@namespaceID", namespaceID)
                ])))?.Select(res=>LoadWorkflowPerformance(res));
            if (aggs!=null)
            {
                var performance = new WorkflowPerformance(
                    bucket.BucketStart,
                    workflowID,
                    namespaceID,
                    destinationType,
                    0,
                    0,
                    0,
                    0,
                    new()
                );
                foreach (var p in aggs)
                    performance = performance.Merge(p);
                commands.AddRange(InsertWorkflowPerformance(performance, destinationType));
            }
            startDate = bucket.BucketStart;
        }
        await queryHelper.ExecuteCommands(connection, transaction, commands);
    }

    private static IEnumerable<SqlCommand> InsertWorkflowPerformance(WorkflowPerformance performance, PerformanceAggregationType aggregationType)
        => [
                new("DELETE FROM \"workflow_performance\" WHERE \"bucket_start\" = @bucketStart AND \"workflow_id\" = @workflowID AND \"namespace_id\" = @namespaceID AND \"aggregation_type\" = @aggregationType::performance_aggregation_types;",
                [
                    new("@bucketStart", performance.BucketStart),
                    new("@workflowID", performance.WorkflowID),
                    new("@namespaceID", performance.NamespaceID),
                    new("@aggregationType", aggregationType.ToString())
                ]),
                new(@"INSERT INTO ""workflow_performance""(""bucket_start"",""workflow_id"",""namespace_id"",""aggregation_type"",""started"",""completed"",""failed"",""purged"",""queue_latency_digest"")
VALUES(@bucketStart, @workflowID, @namespaceID, @aggregationType::performance_aggregation_types, @started, @completed, @failed, @purged, @queueLatencies);",
                [
                        new("@bucketStart", performance.BucketStart),
                        new("@workflowID", performance.WorkflowID),
                        new("@namespaceID", performance.NamespaceID),
                        new("@aggregationType", aggregationType.ToString()),
                        new("@started", performance.Started),
                        new("@completed", performance.Completed),
                        new("@failed", performance.Failed),
                        new("@purged", performance.Purged),
                        new("@queueLatencies", performance.QueueLatencies.AsBinary())
                ])
            ];

    private static WorkflowPerformance LoadWorkflowPerformance(Dictionary<string, object> currentValue)
        => new(
                (DateTime)currentValue["bucket_start"],
                (Guid)currentValue["workflow_id"],
                (Guid)currentValue["namespace_id"],
                PerformanceAggregationType.OneMinute,
                (long)currentValue["started"],
                (long)currentValue["completed"],
                (long)currentValue["failed"],
                (long)currentValue["purged"],
                TimespanSketch.ReadFromByte((byte[])currentValue["queue_latency_digest"])
            );

    private async Task AggregateRawActivities(NpgsqlConnection connection)
    {
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var results = await queryHelper.ExecuteReader(connection, transaction, new("SELECT * FROM \"activity_performance_raw\";", []));
            var rawPerformances = results?.Select(r => new ActivityPerformanceRaw(
                (Guid)r["id"],
                (DateTime)r["window"],
                (Guid)r["activity_id"],
                (Guid)r["namespace_id"],
                (long)r["started"],
                (long)r["completed"],
                (long)r["failed"],
                (long)r["timed_out"],
                JsonSerializer.Deserialize<IEnumerable<TimeSpan>>(r["queue_latencies"].ToString()!)!,
                JsonSerializer.Deserialize<IEnumerable<TimeSpan>>(r["durations"].ToString()!)!
            ))
                .ToArray()?? [];
            var performances = rawPerformances
                .GroupBy(r => new { r.Window, r.ActivityID, r.NamespaceID })
                .Select(grp =>
                {
                    var latencySketch = new TimespanSketch();
                    latencySketch.Add(grp.SelectMany(r => r.QueueLatencies));
                    var durationSketch = new TimespanSketch();
                    durationSketch.Add(grp.SelectMany(r => r.Duration));
                    return new ActivityPerformance(
                        grp.Key.Window,
                        grp.Key.ActivityID,
                        grp.Key.NamespaceID,
                        PerformanceAggregationType.OneMinute,
                        grp.Sum(r => r.Started),
                        grp.Sum(r => r.Completed),
                        grp.Sum(r => r.Failed),
                        grp.Sum(r => r.TimedOut),
                        latencySketch,
                        durationSketch
                    );
                })
                .ToArray();
            var commands = rawPerformances
                .Select(perf => new SqlCommand(
                    "DELETE FROM \"activity_performance_raw\" WHERE \"id\" = @id",
                    [new("@id", perf.ID)]
                ))
                .ToList();
            foreach (var performance in performances)
            {
                var currentValue = (await queryHelper.ExecuteReader(connection, transaction, new(
                    "SELECT * FROM \"activity_performance\" WHERE \"bucket_start\" = @bucketStart AND \"activity_id\" = @activityID AND \"namespace_id\" = @namespaceID AND \"aggregation_type\" = 'OneMinute';",
                    [
                        new("@bucketStart", performance.BucketStart),
                        new("@activityID", performance.ActivityID),
                        new("@namespaceID", performance.NamespaceID)
                    ]
                )))?.FirstOrDefault();
                var insertableObject = performance;
                if (currentValue!=null)
                {
                    insertableObject = performance.Merge(LoadActivityPerformance(currentValue));
                }
                commands.AddRange(InsertActivityPerformance(insertableObject, PerformanceAggregationType.OneMinute));
            }
            await queryHelper.ExecuteCommands(connection, transaction, commands);
            foreach (var grp in performances.GroupBy(p => new { p.ActivityID, p.NamespaceID }))
                await RebuildActivityAggregates(connection, transaction, grp.Min(p => p.BucketStart), grp.Max(p => p.BucketStart), grp.Key.ActivityID, grp.Key.NamespaceID);
        }
        catch
        {
            await transaction.RollbackAsync();
        }
        await transaction.CommitAsync();
    }

    private async Task RebuildActivityAggregates(NpgsqlConnection connection, NpgsqlTransaction transaction, DateTime startDate, DateTime endDate, Guid activityID, Guid namespaceID)
    {
        startDate = startDate.AddMinutes(0 - (startDate.Minute%5)).AddMinutes(-5);
        await RebuildActivityAggregate(connection, transaction, startDate, endDate, activityID, namespaceID, PerformanceAggregationType.OneMinute, PerformanceAggregationType.FiveMinute, (date) => new(date.AddMinutes(5), date.AddMinutes(10)));
        startDate = startDate.AddMinutes(0 - (startDate.Minute%60)).AddHours(-1);
        await RebuildActivityAggregate(connection, transaction, startDate, endDate, activityID, namespaceID, PerformanceAggregationType.FiveMinute, PerformanceAggregationType.OneHour, (date) => new(date.AddHours(1), date.AddHours(2)));
        startDate = startDate.AddHours(0 - startDate.Hour%23).AddDays(-1);
        await RebuildActivityAggregate(connection, transaction, startDate, endDate, activityID, namespaceID, PerformanceAggregationType.OneHour, PerformanceAggregationType.OneDay, (date) => new(date.AddDays(1), date.AddDays(2)));
    }

    private async Task RebuildActivityAggregate(NpgsqlConnection connection, NpgsqlTransaction transaction, DateTime startDate, DateTime endDate, Guid activityID, Guid namespaceID, PerformanceAggregationType sourceType, PerformanceAggregationType destinationType, Func<DateTime, BucketDefinition> nextBucket)
    {
        var commands = new List<SqlCommand>();
        while (startDate.Ticks < endDate.Ticks)
        {
            var bucket = nextBucket(startDate);
            var aggs = (await queryHelper.ExecuteReader(connection, transaction, new(
                "SELECT * from \"activity_performance\" WHERE \"bucket_start\" >= @bucketStart AND \"bucket_start\" < @bucketEnd AND \"aggregation_type\" = @aggregationType::performance_aggregation_types AND \"activity_id\" = @activityID AND \"namespace_id\" = @namespaceID;",
                [
                    new("@bucketStart",bucket.BucketStart),
                    new("@bucketEnd",bucket.BucketEnd),
                    new("@aggregationType", sourceType.ToString()),
                    new("@activityID", activityID),
                    new("@namespaceID", namespaceID)
                ])))?.Select(res => LoadActivityPerformance(res));
            if (aggs!=null)
            {
                var performance = new ActivityPerformance(
                    bucket.BucketStart,
                    activityID,
                    namespaceID,
                    destinationType,
                    0,
                    0,
                    0,
                    0,
                    new(),
                    new()
                );
                foreach (var p in aggs)
                    performance = performance.Merge(p);
                commands.AddRange(InsertActivityPerformance(performance, destinationType));
            }
            startDate = bucket.BucketStart;
        }
        await queryHelper.ExecuteCommands(connection, transaction, commands);
    }

    private static IEnumerable<SqlCommand> InsertActivityPerformance(ActivityPerformance performance, PerformanceAggregationType aggregationType)
        => [
                new("DELETE FROM \"activity_performance\" WHERE \"bucket_start\" = @bucketStart AND \"activity_id\" = @activityID AND \"namespace_id\" = @namespaceID AND \"aggregation_type\" = @aggregationType::performance_aggregation_types;",
                [
                    new("@bucketStart", performance.BucketStart),
                    new("@activityID", performance.ActivityID),
                    new("@namespaceID", performance.NamespaceID),
                    new("@aggregationType", aggregationType.ToString())
                ]),
                new(@"INSERT INTO ""activity_performance""(""bucket_start"",""activity_id"",""namespace_id"",""aggregation_type"",""started"",""completed"",""failed"",""timed_out"",""queue_latency_digest"", ""duration_digest"")
VALUES(@bucketStart, @activityID, @namespaceID, @aggregationType::performance_aggregation_types, @started, @completed, @failed, @timedOut, @queueLatencies, @durations);",
                [
                        new("@bucketStart", performance.BucketStart),
                        new("@activityID", performance.ActivityID),
                        new("@namespaceID", performance.NamespaceID),
                        new("@aggregationType", aggregationType.ToString()),
                        new("@started", performance.Started),
                        new("@completed", performance.Completed),
                        new("@failed", performance.Failed),
                        new("@timedOut", performance.TimedOut),
                        new("@queueLatencies", performance.QueueLatencies.AsBinary()),
                        new("@durations", performance.Duration.AsBinary())
                ])
            ];

    private static ActivityPerformance LoadActivityPerformance(Dictionary<string, object> currentValue)
        => new(
                (DateTime)currentValue["bucket_start"],
                (Guid)currentValue["activity_id"],
                (Guid)currentValue["namespace_id"],
                PerformanceAggregationType.OneMinute,
                (long)currentValue["started"],
                (long)currentValue["completed"],
                (long)currentValue["failed"],
                (long)currentValue["timed_out"],
                TimespanSketch.ReadFromByte((byte[])currentValue["queue_latency_digest"]),
                TimespanSketch.ReadFromByte((byte[])currentValue["duration_digest"])
            );

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await tokenSource.CancelAsync();
        try
        {
            await (runningTask??Task.CompletedTask);
        }
        catch { }
    }
}
