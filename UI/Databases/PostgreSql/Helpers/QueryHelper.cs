using JetFlow.UI.ArgumentFilters;
using JetFlow.UI.Interfaces;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;

namespace JetFlow.UI.Helpers;

internal class QueryHelper (string connectionString)
{
    public static NpgsqlParameter CreateParameter(string parameterName, string? value)
        => new(parameterName, NpgsqlTypes.NpgsqlDbType.Text) { Value = value };
    public static NpgsqlParameter CreateParameter(string parameterName, Guid? value)
        => new(parameterName, NpgsqlTypes.NpgsqlDbType.Uuid) { Value = value };
    public static NpgsqlParameter CreateParameter(string parameterName, DateTimeOffset? value)
        => new(parameterName, NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = value?.DateTime };
    public static NpgsqlParameter CreateParameter(string parameterName, bool? value)
        => new(parameterName, NpgsqlTypes.NpgsqlDbType.Boolean) { Value = value };
    public static NpgsqlParameter CreateParameter(string parameterName, Enum? value)
        => new(parameterName, NpgsqlTypes.NpgsqlDbType.Text) { Value = value?.ToString() };
    public static NpgsqlParameter CreateParameter(string parameterName, int? value)
        => new(parameterName, NpgsqlTypes.NpgsqlDbType.Integer) { Value = value };
    public static NpgsqlParameter CreateParameter(string parameterName, uint? value)
        => new(parameterName, NpgsqlTypes.NpgsqlDbType.Bigint) { Value = (long?)value };
    public static NpgsqlParameter CreateParameter(string parameterName, long? value)
        => new(parameterName, NpgsqlTypes.NpgsqlDbType.Bigint) { Value = value };
    public static NpgsqlParameter CreateParameter(string parameterName, IEnumerable<string> value)
        => new(parameterName, NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = value.ToArray()
        };
    public static NpgsqlParameter CreateJsonParameter(string parameterName, object? value)
        => new(parameterName, NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = (value is null ? null : JsonSerializer.Serialize(value))};


    private static void AddParameters(NpgsqlBatchCommand command, NpgsqlParameter[] arguments)
    {
        foreach (var arg in arguments)
        {
            if (arg.Value is null)
                command.Parameters.Add(new(arg.ParameterName, arg.NpgsqlDbType) { Value = DBNull.Value });
            else
                command.Parameters.Add(arg);
        }
    }
    private static void AddParameters(NpgsqlCommand command, NpgsqlParameter[] arguments)
    {
        foreach (var arg in arguments)
        {
            if (arg.Value is null)
                command.Parameters.Add(new(arg.ParameterName, arg.NpgsqlDbType) { Value = DBNull.Value });
            else
                command.Parameters.Add(arg);
        }
    }

    private async ValueTask InternalExecuteCommands(NpgsqlConnection connection, NpgsqlTransaction transaction, IEnumerable<SqlCommand> commands)
    {
        await using var batch = connection.CreateBatch();
        batch.Transaction = transaction;
        foreach (var comm in commands)
        {
            var command = new NpgsqlBatchCommand(comm.Command);
            AddParameters(command, comm.Arguments);
            batch.BatchCommands.Add(command);
        }
        await batch.ExecuteNonQueryAsync();
    }

    public async ValueTask ExecuteCommands(IEnumerable<SqlCommand> commands)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await InternalExecuteCommands(connection, transaction, commands);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public ValueTask ExecuteCommands(NpgsqlConnection connection, NpgsqlTransaction transaction, IEnumerable<SqlCommand> commands)
        => InternalExecuteCommands(connection, transaction, commands);

    public ValueTask ExecuteCommand(string sql, params NpgsqlParameter[] arguments)
        => ExecuteCommands([new SqlCommand(sql, arguments)]);

    public ValueTask ExecuteCommand(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, params NpgsqlParameter[] arguments)
        => InternalExecuteCommands(connection, transaction, [new SqlCommand(sql, arguments)]);

    private async ValueTask<IEnumerable<IEnumerable<Dictionary<string, object>>>> InternalExecuteReaders(NpgsqlConnection connection, NpgsqlTransaction? transaction, IEnumerable<SqlCommand> commands)
    {
        IEnumerable<Dictionary<string, object>>[] results = new IEnumerable<Dictionary<string, object>>[commands.Count()];
        var idx = 0;
        foreach (var comm in commands)
        {
            List<Dictionary<string, object>> rows = [];
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = comm.Command;
            AddParameters(command, comm.Arguments);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (var x = 0; x<reader.FieldCount; x++)
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

    public async ValueTask<IEnumerable<IEnumerable<Dictionary<string, object>>>> ExecuteReaders(IEnumerable<SqlCommand> commands)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        return await InternalExecuteReaders(connection, null, commands);
    }

    public ValueTask<IEnumerable<IEnumerable<Dictionary<string, object>>>> ExecuteReaders(NpgsqlConnection connection, NpgsqlTransaction transaction, IEnumerable<SqlCommand> commands)
        => InternalExecuteReaders(connection, transaction, commands);

    public async ValueTask<IEnumerable<Dictionary<string, object>>?> ExecuteReader(SqlCommand command)
        => (await ExecuteReaders([command])).FirstOrDefault();

    public async ValueTask<IEnumerable<Dictionary<string, object>>?> ExecuteReader(NpgsqlConnection connection, NpgsqlTransaction transaction, SqlCommand command)
        => (await ExecuteReaders(connection, transaction, [command])).FirstOrDefault();

    public static (string queryPiece, NpgsqlParameter[] parameters) ConvertArgumentFilter(string workflowTableName, IArgumentFilter filter)
        => filter switch
        {
            StartsWithFilter swf => (
            $@"
            EXISTS (
                SELECT 1
                FROM jsonb_extract_path_recursive(
                    {workflowTableName}.arguments,
                    @prop_{swf.PropertyName.Replace('.', '_')}
                ) AS extracted(value)
                WHERE extracted.value #>> '{{}}' LIKE @filter_{swf.PropertyName.Replace('.', '_')}
            )
            ",
            [
                new($"@prop_{swf.PropertyName.Replace('.','_')}", NpgsqlDbType.Array | NpgsqlDbType.Text)
                {
                    Value = swf.PropertyName.Split('.')
                },
                new($"@filter_{swf.PropertyName.Replace('.','_')}", NpgsqlDbType.Text)
                {
                    Value = $"{swf.Value}%"
                }
            ]),

            EndsWithFilter ewf => (
            $@"
            EXISTS (
                SELECT 1
                FROM jsonb_extract_path_recursive(
                    {workflowTableName}.arguments,
                    @prop_{ewf.PropertyName.Replace('.', '_')}
                ) AS extracted(value)
                WHERE extracted.value #>> '{{}}' LIKE @filter_{ewf.PropertyName.Replace('.', '_')}
            )
            ",
            [
                new($"@prop_{ewf.PropertyName.Replace('.', '_')}", NpgsqlDbType.Array | NpgsqlDbType.Text)
                {
                    Value = ewf.PropertyName.Split('.')
                },
                new($"@filter_{ewf.PropertyName.Replace('.', '_')}", NpgsqlDbType.Text)
                {
                    Value = $"%{ewf.Value}"
                }
            ]),
            _ => throw new InvalidCastException()
        };

    internal static IEnumerable<(string queryPiece, NpgsqlParameter[] parameters)> ConvertDateFilter(string dateColumn, DateRange? dateRange)
    {
        if (dateRange is null)
            return [];
        return (dateRange.Value.Start, dateRange.Value.End) switch
        {
            (null, null) => [],
            (DateTimeOffset start, null) => [
                ($"{dateColumn} >= @{dateColumn.Replace(".", "_")}_start", [CreateParameter($"@{dateColumn.Replace(".", "_")}_start", start)])
            ],
            (null, DateTimeOffset end) => [
                ($"{dateColumn} <= @{dateColumn.Replace(".", "_")}_end", [CreateParameter($"@{dateColumn.Replace(".", "_")}_end", end)])
            ],
            (DateTimeOffset start, DateTimeOffset end) => [
                ($"{dateColumn} BETWEEN @{dateColumn.Replace(".", "_")}_start AND @{dateColumn.Replace(".", "_")}_end", 
                [CreateParameter($"@{dateColumn.Replace(".", "_")}_start", start), CreateParameter($"@{dateColumn.Replace(".", "_")}_end", end)])
            ]
        };
    }

    /*
     * SELECT
    try_bigint(data ->> 'id')             AS id,
    try_numeric(data ->> 'amount')        AS amount,
    try_timestamptz(data ->> 'createdAt') AS created_at
FROM my_table;
     */
}
