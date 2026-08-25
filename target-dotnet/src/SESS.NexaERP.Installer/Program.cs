using Npgsql;

return await DatabasePrincipalCommand.RunAsync(args);

internal static class DatabasePrincipalCommand
{
    private const string ConnectionVariable = "ConnectionStrings__NexaErpInstaller";
    private const string ExpectedDatabaseVariable = "NexaErp__ExpectedDatabase";
    private static readonly string[] ManagedRoles =
        ["nexa_erp_owner", "nexa_erp_migration", "nexa_erp_bootstrap", "nexa_erp_runtime"];

    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 2 || !string.Equals(args[0], "database-principals", StringComparison.Ordinal))
        {
            WriteUsage();
            return 2;
        }

        if (string.Equals(args[1], "plan", StringComparison.Ordinal))
        {
            Console.WriteLine(DatabasePrincipalProvisioningSql.Plan);
            return 0;
        }

        if (args[1] is not ("status" or "provision"))
        {
            WriteUsage();
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
                ApplicationName = "SESS.NexaERP.Installer.DatabasePrincipals",
                IncludeErrorDetail = false
            };
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            await RequireSafeClusterAsync(connection, expectedDatabase.Trim());

            var roleCount = await CountManagedRolesAsync(connection);
            if (args[1] == "status")
            {
                if (roleCount == 0)
                {
                    Console.WriteLine("NOT_PROVISIONED: none of the four NexaERP database principals exists.");
                    return 3;
                }

                if (roleCount != ManagedRoles.Length)
                    throw new InvalidOperationException($"PARTIAL_STATE: found {roleCount} of {ManagedRoles.Length} managed roles.");

                await ExecuteAsync(connection, DatabasePrincipalProvisioningSql.Verify);
                Console.WriteLine("VERIFIED: database ownership, role attributes, memberships, and least-privilege grants match the installer contract.");
                return 0;
            }

            await using var transaction = await connection.BeginTransactionAsync();
            await ExecuteAsync(connection, DatabasePrincipalProvisioningSql.AcquireLock, transaction);
            roleCount = await CountManagedRolesAsync(connection, transaction);
            if (roleCount is not (0 or 4))
                throw new InvalidOperationException($"PARTIAL_STATE: found {roleCount} of {ManagedRoles.Length} managed roles; provisioning refuses mixed state.");

            if (roleCount == 0)
                await SetInitialPasswordsAsync(connection, transaction);

            await ExecuteAsync(connection, DatabasePrincipalProvisioningSql.Provision, transaction);
            await ExecuteAsync(connection, DatabasePrincipalProvisioningSql.Verify, transaction);
            await transaction.CommitAsync();
            Console.WriteLine(roleCount == 0
                ? "PROVISIONED: four database principals created, ownership transferred, and grants verified."
                : "RECONCILED: existing principal contract and grants verified; credentials were not changed.");
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
            throw new InvalidOperationException("Database principal provisioning requires PostgreSQL 17 or later.");
        if (database is "postgres" or "template0" or "template1"
            || !string.Equals(database, expectedDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException("Database target does not match NexaErp__ExpectedDatabase.");
        if (!hasAdvanceSchema)
            throw new InvalidOperationException("The advance schema must exist before principal provisioning.");
        if (!isSuperuser)
            throw new InvalidOperationException("Initial principal provisioning requires an explicit PostgreSQL superuser installer session.");
    }

    private static async Task<int> CountManagedRolesAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction = null)
    {
        await using var command = new NpgsqlCommand(
            "SELECT count(*)::integer FROM pg_catalog.pg_roles WHERE rolname = ANY(@roles);",
            connection,
            transaction);
        command.Parameters.AddWithValue("roles", ManagedRoles);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    private static async Task SetInitialPasswordsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var passwords = new Dictionary<string, string?>
        {
            ["migration_password"] = Environment.GetEnvironmentVariable("NEXAERP_MIGRATION_PASSWORD"),
            ["bootstrap_password"] = Environment.GetEnvironmentVariable("NEXAERP_BOOTSTRAP_PASSWORD"),
            ["runtime_password"] = Environment.GetEnvironmentVariable("NEXAERP_RUNTIME_PASSWORD")
        };
        if (passwords.Any(x => string.IsNullOrWhiteSpace(x.Value) || x.Value.Length < 24))
            throw new InvalidOperationException("Initial migration, bootstrap, and runtime passwords must each be supplied through NEXAERP_*_PASSWORD and contain at least 24 characters.");

        foreach (var password in passwords)
        {
            await using var command = new NpgsqlCommand(
                $"SELECT pg_catalog.set_config('nexa.installer.{password.Key}', @secret, true);",
                connection,
                transaction);
            command.Parameters.AddWithValue("secret", password.Value!);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction = null)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }

    private static void WriteUsage() =>
        Console.Error.WriteLine("Usage: SESS.NexaERP.Installer database-principals <plan|status|provision>");
}
