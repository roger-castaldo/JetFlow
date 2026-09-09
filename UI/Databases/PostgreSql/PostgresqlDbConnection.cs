using JetFlow.UI.Interfaces;
using Npgsql;
using System.Text.Json;

namespace JetFlow.UI;

public class PostgresqlDbConnection(string connectionString) 
    : IDbConnection
{
    private record SqlCommand(string Command, NpgsqlParameter[] Arguments);

    private async ValueTask ExecuteCommands(IEnumerable<SqlCommand> commands)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var batch = connection.CreateBatch();
        batch.Transaction = transaction;
        foreach(var comm in commands)
        {
            var command = new NpgsqlBatchCommand(comm.Command);
            foreach (var arg in comm.Arguments)
                command.Parameters.Add(arg);
            batch.BatchCommands.Add(command);
        }
        try
        {
            await batch.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private ValueTask ExecuteCommand(string sql, params NpgsqlParameter[] arguments)
        => ExecuteCommands([new SqlCommand(sql, arguments)]);

    private async ValueTask<IEnumerable<IEnumerable<Dictionary<string,object>>>> ExecuteReaders(IEnumerable<SqlCommand> commands)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        IEnumerable<Dictionary<string, object>>[] results = new IEnumerable<Dictionary<string, object>>[commands.Count()];
        var idx = 0;
        foreach(var comm in commands)
        {
            List<Dictionary<string, object>> rows = [];
            var command = dataSource.CreateCommand(comm.Command);
            foreach (var arg in comm.Arguments)
                command.Parameters.Add(arg);
            await using var reader = await command.ExecuteReaderAsync();
            while(await reader.NextResultAsync())
            {
                var row = new Dictionary<string, object>();
                for(var x = 0; x<reader.FieldCount; x++)
                {
                    if (!reader.IsDBNull(x))
                        row.Add(reader.GetName(x), reader.GetValue(x));
                }
                rows.Add(row);
            }
            results[idx] = rows;
            idx++;
        }
        return results;
    }

    private async ValueTask<IEnumerable<Dictionary<string, object>>> ExecuteReader(SqlCommand command)
        => (await ExecuteReaders([command])).First();

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
    }

    async ValueTask<IEnumerable<string?>> IDbConnection.ListNamespacesAsync()
    {
        var queryResults = await ExecuteReader(new(@"SELECT ""name"" FROM ""namespaces"" WHERE ""enabled"" = @enabled;", [new("@enabled", true)]));
        return queryResults.Select(row => (string?)row["name"]);
    }

    ValueTask IDbConnection.RegisterNamespaceAsync(string? namespaceName)
    => ExecuteCommand("CALL register_namespace(@namespace)", new NpgsqlParameter("@namespace", namespaceName));

    ValueTask IDbConnection.StoreArchiveAsync(ArchivedWorkflow archive, string? namespaceName)
    => ExecuteCommands([
        new("CALL create_archived_workflow(@id, @namespace, @workflow, @scheduler_id, @started_at, @finished_at, @is_successful, @error_message, @arguments)",[
            new("@id", archive.ID),
            new("@namespace", namespaceName),
            new("@workflow", archive.Name),
            new("@scheduler_id", archive.SchedulerId),
            new("@started_at", archive.StartedAt),
            new("@finished_at", archive.FinishedAt),
            new("@is_successful", archive.IsSuccessful),
            new("@error_message", archive.ErrorMessage),
            new("@arguments", (archive.Arguments is null ? null : JsonSerializer.Serialize(archive.Arguments)))
        ]),
        new(@"CALL set_archived_workflow_options(@id, @namespace, @workflow, @completion_action, @purge_delay, @error_on_activity_timeout, @error_on_activity_failure)", [
            new("@id", archive.ID),
            new("@namespace", namespaceName),
            new("@workflow", archive.Name),
            new("@completion_action", archive.Options.CompletionAction),
            new("@purge_delay", archive.Options.PurgeDelay?.ToString()),
            new("@error_on_activity_timeout", archive.Options.ErrorOnActivityFailure),
            new("@error_on_activity_failure", archive.Options.ErrorOnActivityTimeout)
        ]),
        .. (archive.MetaData?.SelectMany(pair=>
            pair.Value.Select((value,index)=> new SqlCommand("CALL add_archived_workflow_metadata_entry(@id, @namespace, @workflow, @key_name, @value_index, @value)",[
                    new("@id", archive.ID),
                    new("@namespace", namespaceName),
                    new("@workflow", archive.Name),
                    new("@key_name", pair.Key),
                    new("@value_index", index),
                    new("@value", value)
                ])
            ))??[]),
        .. archive.Steps.SelectMany<WorkflowStep, SqlCommand>((step, index)=>
            [
                new SqlCommand("CALL add_archived_workflow_step(@id, @namespace, @workflow, @step_id, @step_type, @step_index, @step_name, @start_time, @end_time, @input, @result_status, @error_message, @result)", [
                    new("@id", archive.ID),
                    new("@namespace", namespaceName),
                    new("@workflow", archive.Name),
                    new("@step_id", index),
                    new("@step_type", step.Type),
                    new("@step_index", step.Index),
                    new("@step_name", step.Name),
                    new("@start_time", step.StartTime),
                    new("@end_time", step.EndTime),
                    new("@input", (step.Input is null ? null : JsonSerializer.Serialize(step.Input))),
                    new("@result_status", step.Status),
                    new("@error_message", step.ErrorMessage),
                    new("@result", (step.Result is null ? null : JsonSerializer.Serialize(step.Result)))
                ]),
                .. (step.Retries?.Select((retry, rindex)=>new SqlCommand("CALL add_archived_workflow_step_retry(@id, @namespace, @workflow, @step_id, @retry_index, @retry_type, @time_stamp)",[
                        new("@id", archive.ID),
                        new("@namespace", namespaceName),
                        new("@workflow", archive.Name),
                        new("@step_id", index),
                        new("@retry_index", rindex),
                        new("@retry_type", retry.RetryType),
                        new("@time_stamp", retry.Timestamp)
                    ]))
                    ??[])
            ]
        )
    ]);

    ValueTask IDbConnection.UnregisterNamespaceAsync(string? namespaceName)
    => ExecuteCommand("CALL unregister_namespace(@namespace)", new NpgsqlParameter("@namespace", namespaceName));
}
