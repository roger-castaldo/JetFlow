using Npgsql;

namespace JetFlow.UI.Helpers;

internal record SqlCommand(string Command, NpgsqlParameter[] Arguments);
