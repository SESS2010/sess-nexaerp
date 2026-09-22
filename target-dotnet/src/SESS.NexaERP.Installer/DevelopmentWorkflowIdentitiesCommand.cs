#if DEBUG
using Npgsql;

internal static class DevelopmentWorkflowIdentitiesCommand
{
    private const string ConnectionVariable = "ConnectionStrings__NexaErpDevelopmentBootstrap";
    private const string ExpectedDatabaseVariable = "NexaErp__ExpectedDatabase";

    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1 || args[0] != "provision")
        {
            WriteUsage();
            return 2;
        }
        if (!string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), "Development", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("REFUSED: workflow-identities-development requires DOTNET_ENVIRONMENT=Development.");
            return 1;
        }
        if (!bool.TryParse(Environment.GetEnvironmentVariable(InstallerCommand.DevelopmentWorkflowIdentitiesSetting), out var enabled) || !enabled)
        {
            Console.Error.WriteLine($"REFUSED: workflow-identities-development requires {InstallerCommand.DevelopmentWorkflowIdentitiesSetting}=true.");
            return 1;
        }

        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        var expectedDatabase = Environment.GetEnvironmentVariable(ExpectedDatabaseVariable);
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(expectedDatabase))
        {
            Console.Error.WriteLine($"Set {ConnectionVariable} and {ExpectedDatabaseVariable}. A connection string is never accepted as a command-line argument.");
            return 2;
        }

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                ApplicationName = "SESS.NexaERP.Installer.DevelopmentWorkflowIdentities",
                IncludeErrorDetail = false,
                Pooling = false
            };
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            await RequireSafeClusterAsync(connection, expectedDatabase.Trim());
            await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await using var command = new NpgsqlCommand(DevelopmentWorkflowIdentitiesCommandSql.Provision, connection, transaction)
            {
                CommandTimeout = 120
            };
            var result = (string?)await command.ExecuteScalarAsync()
                ?? throw new InvalidOperationException("Development workflow identity provisioning returned no witness.");
            await transaction.CommitAsync();
            Console.WriteLine($"PROVISIONED_DEVELOPMENT_ONLY: {result}");
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
        await using var command = new NpgsqlCommand(DevelopmentWorkflowIdentitiesCommandSql.ClusterGuard, connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("PostgreSQL cluster guard returned no witness.");
        if (reader.GetInt32(0) < 170000) throw new InvalidOperationException("Development workflow identities require PostgreSQL 17 or later.");
        if (reader.GetString(1) is "postgres" or "template0" or "template1" || !string.Equals(reader.GetString(1), expectedDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException("Database target does not match NexaErp__ExpectedDatabase.");
        if (!reader.GetBoolean(2)) throw new InvalidOperationException("The advance schema and employee identity table must exist.");
        if (!string.Equals(reader.GetString(3), reader.GetString(4), StringComparison.Ordinal))
            throw new InvalidOperationException("Development workflow identities refuse an already-assumed database role.");
        if (!reader.GetBoolean(5))
            throw new InvalidOperationException("Development workflow identities require an explicit PostgreSQL superuser installer session.");
        var managedRoleCount = reader.GetInt64(8);
        if (managedRoleCount is not (0 or 4))
            throw new InvalidOperationException($"Development workflow identities refuse partial managed-principal state ({managedRoleCount}/4 roles).");
        if (managedRoleCount == 0 && (!reader.GetBoolean(6) || !reader.GetBoolean(7)))
            throw new InvalidOperationException("Before principal provisioning, the installer session must own the exact database and advance schema.");
        if (managedRoleCount == 4 && (!reader.GetBoolean(9) || !reader.GetBoolean(10)))
            throw new InvalidOperationException("After principal provisioning, nexa_erp_owner must own the exact database and advance schema.");
    }

    private static void WriteUsage() =>
        Console.Error.WriteLine("Usage: SESS.NexaERP.Installer workflow-identities-development provision");
}
#endif