using JetFlow.Data;
using JetFlow.UI.dto;
using JetFlow.UI.Helpers;
using JetFlow.UI.Interfaces;
using Npgsql;
using System.Text.Json;

namespace JetFlow.UI;

/// <summary>
/// Represents a PostgreSQL database connection for JetFlow UI, implementing the IDbConnection and IAsyncDisposable interfaces.
/// </summary>
/// <param name="connectionString">The connection string to the PostgreSQL database</param>
public class PostgresqlDbConnection(string connectionString) 
    : IDbConnection, IAsyncDisposable
{
    private readonly LatencyAggregator latencyAggregator = new(connectionString);
    private readonly QueryHelper queryHelper = new(connectionString);

    async ValueTask IDbConnection.InitAsync()
    {
        var script = await new StreamReader(GetType().Assembly.GetManifestResourceStream("JetFlow.UI.init.sql")!).ReadToEndAsync();
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = script;
        cmd.Transaction = transaction;

        try
        {
            await cmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        latencyAggregator.Start();
    }

    async ValueTask<IEnumerable<string?>> IDbConnection.ListNamespacesAsync()
    {
        var queryResults = await queryHelper.ExecuteReader(new(@"SELECT ""name"" FROM ""namespaces"" WHERE ""enabled"" = @enabled;", [new("@enabled", true)]));
        return queryResults?.Select(row => (string?)row["name"])?? [];
    }

    ValueTask IDbConnection.RegisterNamespaceAsync(string? namespaceName)
    => queryHelper.ExecuteCommand("CALL register_namespace(@namespace)", new NpgsqlParameter("@namespace", namespaceName));

    ValueTask IDbConnection.StoreArchiveAsync(ArchivedWorkflow archive, string? namespaceName)
    => queryHelper.ExecuteCommands([
        new("CALL create_archived_workflow(@id, @namespace, @workflow, @scheduler_id, @started_at, @finished_at, @is_successful, @error_message, @arguments)",[
            QueryHelper.CreateParameter("@id", archive.ID),
            QueryHelper.CreateParameter("@namespace", namespaceName),
            QueryHelper.CreateParameter("@workflow", archive.Name),
            QueryHelper.CreateParameter("@scheduler_id", archive.SchedulerId),
            QueryHelper.CreateParameter("@started_at", archive.StartedAt),
            QueryHelper.CreateParameter("@finished_at", archive.FinishedAt),
            QueryHelper.CreateParameter("@is_successful", archive.IsSuccessful),
            QueryHelper.CreateParameter("@error_message", archive.ErrorMessage),
            QueryHelper.CreateJsonParameter("@arguments", archive.Arguments)
        ]),
        new(@"CALL set_archived_workflow_options(@id, @namespace, @workflow, @completion_action::workflow_completion_actions, @purge_delay, @error_on_activity_timeout, @error_on_activity_failure)", [
            QueryHelper.CreateParameter("@id", archive.ID),
            QueryHelper.CreateParameter("@namespace", namespaceName),
            QueryHelper.CreateParameter("@workflow", archive.Name),
            QueryHelper.CreateParameter("@completion_action", archive.Options.CompletionAction),
            QueryHelper.CreateParameter("@purge_delay", archive.Options.PurgeDelay?.ToString()),
            QueryHelper.CreateParameter("@error_on_activity_timeout", archive.Options.ErrorOnActivityFailure),
            QueryHelper.CreateParameter("@error_on_activity_failure", archive.Options.ErrorOnActivityTimeout)
        ]),
        .. (archive.MetaData?.SelectMany(pair=>
            pair.Value.Select((value,index)=> new SqlCommand("CALL add_archived_workflow_metadata_entry(@id, @namespace, @workflow, @key_name, @value_index, @value)",[
                    QueryHelper.CreateParameter("@id", archive.ID),
                    QueryHelper.CreateParameter("@namespace", namespaceName),
                    QueryHelper.CreateParameter("@workflow", archive.Name),
                    QueryHelper.CreateParameter("@key_name", pair.Key),
                    QueryHelper.CreateParameter("@value_index", index),
                    QueryHelper.CreateParameter("@value", value)
                ])
            ))??[]),
        .. archive.Steps.SelectMany<WorkflowStep, SqlCommand>((step, index)=>
            [
                new SqlCommand("CALL add_archived_workflow_step(@id, @namespace, @workflow, @step_id, @step_type::workflow_step_types, @step_index, @step_name, @start_time, @end_time, @input, @result_status::workflow_completion_actions, @error_message, @result)", [
                    QueryHelper.CreateParameter("@id", archive.ID),
                    QueryHelper.CreateParameter("@namespace", namespaceName),
                    QueryHelper.CreateParameter("@workflow", archive.Name),
                    QueryHelper.CreateParameter("@step_id", index),
                    QueryHelper.CreateParameter("@step_type", step.Type),
                    QueryHelper.CreateParameter("@step_index", step.Index),
                    QueryHelper.CreateParameter("@step_name", step.Name),
                    QueryHelper.CreateParameter("@start_time", step.StartTime),
                    QueryHelper.CreateParameter("@end_time", step.EndTime),
                    QueryHelper.CreateJsonParameter("@input", step.Input),
                    QueryHelper.CreateParameter("@result_status", step.Status),
                    QueryHelper.CreateParameter("@error_message", step.ErrorMessage),
                    QueryHelper.CreateJsonParameter("@result", step.Result)
                ]),
                .. (step.Retries?.Select((retry, rindex)=>new SqlCommand("CALL add_archived_workflow_step_retry(@id, @namespace, @workflow, @step_id, @retry_index, @retry_type::workflow_retry_types, @time_stamp)",[
                        QueryHelper.CreateParameter("@id", archive.ID),
                        QueryHelper.CreateParameter("@namespace", namespaceName),
                        QueryHelper.CreateParameter("@workflow", archive.Name),
                        QueryHelper.CreateParameter("@step_id", index),
                        QueryHelper.CreateParameter("@retry_index", rindex),
                        QueryHelper.CreateParameter("@retry_type", retry.RetryType),
                        QueryHelper.CreateParameter("@time_stamp", retry.Timestamp)
                    ]))
                    ??[])
            ]
        )
    ]);

    ValueTask IDbConnection.UnregisterNamespaceAsync(string? namespaceName)
        => queryHelper.ExecuteCommand("CALL unregister_namespace(@namespace)", new NpgsqlParameter("@namespace", namespaceName));

    ValueTask IDbConnection.StoreActivityPerformanceRecordAsync(string? namespaceName, ActivityPerformanceRecord record)
        => queryHelper.ExecuteCommand("CALL add_activity_performance_entry(@window, @namespace,@activity,@started,@completed,@failed,@timed_out,@queue_latencies,@durations)", [
                QueryHelper.CreateParameter("@window", record.Window),
                QueryHelper.CreateParameter("@namespace", namespaceName),
                QueryHelper.CreateParameter("@activity", record.Name),
                QueryHelper.CreateParameter("@started", record.Started),
                QueryHelper.CreateParameter("@completed", record.Completed),
                QueryHelper.CreateParameter("@failed", record.Failed),
                QueryHelper.CreateParameter("@timed_out", record.TimedOut),
                QueryHelper.CreateJsonParameter("@queue_latencies", record.QueueLatencies),
                QueryHelper.CreateJsonParameter("@durations", record.Durations)
            ]);
    ValueTask IDbConnection.StoreWorkflowPerformanceRecordAsync(string? namespaceName, WorkflowPerformanceRecord record)
        => queryHelper.ExecuteCommand("CALL add_workflow_performance_entry(@window, @namespace,@workflow,@started,@completed,@failed,@purged,@queue_latencies)", [
                QueryHelper.CreateParameter("@window", record.Window),
                QueryHelper.CreateParameter("@namespace", namespaceName),
                QueryHelper.CreateParameter("@workflow", record.Name),
                QueryHelper.CreateParameter("@started", record.Started),
                QueryHelper.CreateParameter("@completed", record.Completed),
                QueryHelper.CreateParameter("@failed", record.Failed),
                QueryHelper.CreateParameter("@purged", record.Purged),
                QueryHelper.CreateJsonParameter("@queue_latencies", record.QueueLatencies)
            ]);
    async ValueTask<IEnumerable<ActivityPerformanceRecord>> IDbConnection.PeakActivityPerformanceDataAsync(string? namespaceName)
        => (await queryHelper.ExecuteReader(new(
                @"
SELECT 
    apr.window, 
    a.name AS ""activity_name"",
    SUM(apr.started)::bigint AS ""started"",
    SUM(apr.completed)::bigint AS ""completed"",
    SUM(apr.failed)::bigint AS ""failed"",
    SUM(apr.timed_out)::bigint AS ""timed_out"",

    jsonb_path_query_array(
        jsonb_agg(apr.queue_latencies),
        '$[*][*]'
    ) AS ""queue_latencies"",

    jsonb_path_query_array(
        jsonb_agg(apr.durations),
        '$[*][*]'
    ) AS ""durations""

FROM ""activity_performance_raw"" apr
INNER JOIN ""activities"" a 
    ON apr.activity_id = a.id
    AND apr.namespace_id = a.namespace_id
INNER JOIN ""namespaces"" n
    ON apr.namespace_id = n.id
WHERE n.name = @namespaceName
GROUP BY apr.window, a.name
ORDER BY apr.window DESC
                ", [QueryHelper.CreateParameter("@namespaceName", namespaceName)]
            )))?.Select(row => new ActivityPerformanceRecord(
                DateTimeOffset.Parse(row["window"].ToString()!),
                (string)row["activity_name"],
                (long)row["started"],
                (long)row["completed"],
                (long)row["failed"],
                (long)row["timed_out"],
                JsonSerializer.Deserialize<IEnumerable<TimeSpan>>((string)row["queue_latencies"])!,
                JsonSerializer.Deserialize<IEnumerable<TimeSpan>>((string)row["durations"])!)
            )?? [];
    async ValueTask<IEnumerable<WorkflowPerformanceRecord>> IDbConnection.PeakWorkflowPerformanceDataAsync(string? namespaceName)
    => (await queryHelper.ExecuteReader(new(
                @"
SELECT wpr.window, 
    w.name AS ""workflow_name"",
    SUM(wpr.started)::bigint AS ""started"",
    SUM(wpr.completed)::bigint AS ""completed"",
    SUM(wpr.failed)::bigint AS ""failed"",
    SUM(wpr.purged)::bigint AS ""purged"",
    jsonb_path_query_array(
        jsonb_agg(wpr.queue_latencies),
        '$[*][*]'
    ) AS ""queue_latencies""
FROM ""workflow_performance_raw"" wpr
INNER JOIN ""workflows"" w
    ON wpr.workflow_id = w.id
    AND wpr.namespace_id = w.namespace_id
INNER JOIN ""namespaces"" n
    ON wpr.namespace_id = n.id
WHERE n.name = @namespaceName
GROUP BY wpr.window, w.name
ORDER BY wpr.window DESC
                ", [QueryHelper.CreateParameter("@namespaceName", namespaceName)]
            )))?.Select(row => new WorkflowPerformanceRecord(
                DateTimeOffset.Parse(row["window"].ToString()!),
                (string)row["workflow_name"],
                (long)row["started"],
                (long)row["completed"],
                (long)row["failed"],
                (long)row["purged"],
                JsonSerializer.Deserialize<IEnumerable<TimeSpan>>((string)row["queue_latencies"])!)
            )?? [];

    private PerformanceAggregationType CalculateAggregationType(DateTime startTime, DateTime endTime)
        => (DateTime.Now.Subtract(startTime).TotalDays, endTime.Subtract(startTime).TotalDays) switch {
            var (historyDays, diffDays) when historyDays <= 3 && diffDays <= 0.5 => PerformanceAggregationType.OneMinute,
            var (historyDays, diffDays) when historyDays <= 3 && diffDays <= 1 => PerformanceAggregationType.FiveMinute,
            var (historyDays, _) when historyDays <= 3 => PerformanceAggregationType.OneHour,
            var (historyDays, diffDays) when historyDays <= 15 && diffDays <= 1 => PerformanceAggregationType.FiveMinute,
            var (historyDays, diffDays) when historyDays <= 15 && diffDays <= 3 => PerformanceAggregationType.OneHour,
            var (historyDays, _) when historyDays <= 15 => PerformanceAggregationType.OneDay,
            var (historyDays, diffDays) when historyDays <= 30 && diffDays <= 3 => PerformanceAggregationType.OneHour,
            _ => PerformanceAggregationType.OneDay
        };

async ValueTask<IEnumerable<WorkflowPerformanceSample>> IDbConnection.ExtractWorkflowPerformanceDataAsync(string? namespaceName, string workflowName, DateTime startTime, DateTime endTime)
        => (await ExtractWorkflowPerformanceAsync(namespaceName, startTime, endTime, workflowName))
                .Cast<WorkflowPerformanceSample>();

    ValueTask<IEnumerable<NamedWorkflowPerformanceSample>> IDbConnection.ExtractWorkflowPerformanceDataAsync(string? namespaceName, DateTime startTime, DateTime endTime)
        => ExtractWorkflowPerformanceAsync(namespaceName, startTime, endTime);

    private async ValueTask<IEnumerable<NamedWorkflowPerformanceSample>> ExtractWorkflowPerformanceAsync(string? namespaceName, DateTime startTime, DateTime endTime, string? workflowName = null)
        => (await queryHelper.ExecuteReader(new(
                @"SELECT wf.name, wp.started, wp.completed, wp.failed, wp.purged, wp.queue_latency_digest 
FROM ""workflow_performance"" wp 
INNER JOIN ""workflows"" wf 
    ON wp.namespace_id = wf.namespace_id 
    AND wp.workflow_id = wf.id 
INNER JOIN ""namespaces"" n
    ON wp.namespace_id = n.id
WHERE wp.bucket_start >= @startTime
AND wp.bucket_start <= @endTime 
AND wp.aggregation_type = @aggregationType::performance_aggregation_types
AND (n.name = @namespace OR (@namespace IS NULL AND n.name IS NULL))
AND (@workflowName IS NULL OR wf.name = @workflowName)
ORDER BY wp.bucket_start ASC", 
                [
                    QueryHelper.CreateParameter("@startTime", startTime),
                    QueryHelper.CreateParameter("@endTime", endTime),
                    QueryHelper.CreateParameter("@aggregationType", CalculateAggregationType(startTime, endTime)),
                    QueryHelper.CreateParameter("@namespace", namespaceName),
                    QueryHelper.CreateParameter("@workflowName", workflowName)
                ]
            )))?.Select(result=> {
                var sketch = TimespanSketch.ReadFromByte((byte[])result["queue_latency_digest"]);
                return new NamedWorkflowPerformanceSample(
                    (string)result["name"],
                    (long)result["started"],
                    (long)result["completed"],
                    (long)result["failed"],
                    (long)result["purged"],
                    new(sketch.GetQuantile(0.50), sketch.GetQuantile(0.95), sketch.GetQuantile(0.99))
                );
            })?? [];

    async ValueTask<IEnumerable<ActivityPerformanceSample>> IDbConnection.ExtractActivityPerformanceDataAsync(string? namespaceName, string activityName, DateTime startTime, DateTime endTime)
        => (await ExtractActivityPerformanceAsync(namespaceName, startTime, endTime, activityName))
            .Cast<ActivityPerformanceSample>();

    ValueTask<IEnumerable<NamedActivityPerformanceSample>> IDbConnection.ExtractActivityPerformanceDataAsync(string? namespaceName, DateTime startTime, DateTime endTime)
        => ExtractActivityPerformanceAsync(namespaceName, startTime, endTime);

    private async ValueTask<IEnumerable<NamedActivityPerformanceSample>> ExtractActivityPerformanceAsync(string? namespaceName, DateTime startTime, DateTime endTime, string? activityName = null)
        => (await queryHelper.ExecuteReader(new(
                @"SELECT a.name, ap.started, ap.completed, ap.failed, ap.timed_out, ap.queue_latency_digest, ap.duration_digest
FROM ""activity_performance"" ap 
INNER JOIN ""activities"" a
    ON ap.namespace_id = a.namespace_id 
    AND ap.activity_id = a.id 
INNER JOIN ""namespaces"" n
    ON ap.namespace_id = n.id
WHERE ap.bucket_start >= @startTime
AND ap.bucket_start <= @endTime 
AND ap.aggregation_type = @aggregationType::performance_aggregation_types
AND (n.name = @namespace OR (@namespace IS NULL AND n.name IS NULL))
AND (@activityName IS NULL OR a.name = @activityName)
ORDER BY ap.bucket_start ASC",
                [
                    QueryHelper.CreateParameter("@startTime", startTime),
                    QueryHelper.CreateParameter("@endTime", endTime),
                    QueryHelper.CreateParameter("@aggregationType", CalculateAggregationType(startTime, endTime)),
                    QueryHelper.CreateParameter("@namespace", namespaceName),
                    QueryHelper.CreateParameter("@activityName", activityName)
                ]
            )))?.Select(result => {
                var queueSketch = TimespanSketch.ReadFromByte((byte[])result["queue_latency_digest"]);
                var durationSketch = TimespanSketch.ReadFromByte((byte[])result["duration_digest"]);
                return new NamedActivityPerformanceSample(
                    (string)result["name"],
                    (long)result["started"],
                    (long)result["completed"],
                    (long)result["failed"],
                    (long)result["timed_out"],
                    new(queueSketch.GetQuantile(0.50), queueSketch.GetQuantile(0.95), queueSketch.GetQuantile(0.99)),
                    new(durationSketch.GetQuantile(0.50), durationSketch.GetQuantile(0.95), durationSketch.GetQuantile(0.99))
                );
            })?? [];

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await ((IAsyncDisposable)latencyAggregator).DisposeAsync();
    }
}
