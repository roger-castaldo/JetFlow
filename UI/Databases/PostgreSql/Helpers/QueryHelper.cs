using Npgsql;
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
}
