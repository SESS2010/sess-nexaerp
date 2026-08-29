using Npgsql;

internal static class MasterImportRetentionPurgeCommand
{
    private const string ConnectionVariable = "ConnectionStrings__NexaErpInstaller";
    private const string ExpectedDatabaseVariable = "NexaErp__ExpectedDatabase";

    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1 || !string.Equals(args[0], "purge", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Usage: SESS.NexaERP.Installer master-import-retention purge");
            return 2;
        }

        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        var expectedDatabase = Environment.GetEnvironmentVariable(ExpectedDatabaseVariable);
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(expectedDatabase))
        {
            Console.Error.WriteLine($"Set {ConnectionVariable} and {ExpectedDatabaseVariable}. Connection strings are never accepted as command-line arguments.");
            return 2;
        }

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                ApplicationName = "SESS.NexaERP.Installer.MasterImportRetention",
                IncludeErrorDetail = false
            };
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            await RequireSafeClusterAsync(connection, expectedDatabase.Trim());
            await using var transaction = await connection.BeginTransactionAsync();
            await using var command = new NpgsqlCommand(
                """
                SELECT "BatchCount", "RowCount"
                FROM advance.purge_expired_master_import_sensitive_values();
                """,
                connection,
                transaction);
            command.CommandTimeout = 120;
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException("Retention purge function returned no witness.");
            var batchCount = reader.GetInt64(0);
            var rowCount = reader.GetInt64(1);
            await reader.DisposeAsync();
            await transaction.CommitAsync();
            Console.WriteLine($"PURGED: expired batches marked={batchCount}; row payloads cleared={rowCount}; permanent batch counts, hashes and audit metadata retained.");
            return 0;
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            Console.Error.WriteLine($"REFUSED: {exception.Message}");
            return 1;
        }
    }

    private static async Task RequireSafeClusterAsync(NpgsqlConnection connection, string expectedDatabase)
    {
        await using var command = new NpgsqlCommand(DatabasePrincipalProvisioningSql.ClusterGuard, connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new InvalidOperationException("PostgreSQL cluster guard returned no witness.");
        var serverVersion = reader.GetInt32(0);
        var database = reader.GetString(1);
        var hasAdvanceSchema = reader.GetBoolean(2);
        var isSuperuser = reader.GetBoolean(3);
        if (serverVersion < 170000)
            throw new InvalidOperationException("Master import retention purge requires PostgreSQL 17 or later.");
        if (database is "postgres" or "template0" or "template1"
            || !string.Equals(database, expectedDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException("Database target does not match NexaErp__ExpectedDatabase.");
        if (!hasAdvanceSchema)
            throw new InvalidOperationException("The advance schema must exist before retention purge.");
        if (!isSuperuser)
            throw new InvalidOperationException("Retention purge requires an explicit guarded PostgreSQL superuser installer session.");
    }
}
