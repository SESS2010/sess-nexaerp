using Npgsql;

return await InstallerCommand.RunAsync(args);

internal static class InstallerCommand
{
    internal const string DevelopmentBootstrapSetting = "NexaErp__AllowDevelopmentAuthenticationBootstrap";

    internal static Task<int> RunAsync(string[] args)
    {
#if !DEBUG
        if (Environment.GetEnvironmentVariable(DevelopmentBootstrapSetting) is not null)
        {
            Console.Error.WriteLine($"REFUSED: {DevelopmentBootstrapSetting} must not be present in a Release build, even when set to false.");
            return Task.FromResult(1);
        }
#endif
        if (args.Length > 0 && args[0] == "authentication-bootstrap")
            return AuthenticationBootstrapCommand.RunAsync(args[1..]);
#if DEBUG
        if (args.Length > 0 && args[0] == "authentication-bootstrap-development")
            return DevelopmentAuthenticationBootstrapCommand.RunAsync(args[1..]);
#endif
        return DatabasePrincipalCommand.RunAsync(args);
    }
}

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

            var roleStatuses = await ReadManagedRoleStatusAsync(connection);
            var roleCount = roleStatuses.Count(x => x.Exists);
            if (args[1] == "status")
            {
                WriteManagedRoleStatus(roleStatuses);
                if (roleCount == 0)
                {
                    Console.WriteLine("NOT_PROVISIONED: none of the four NexaERP database principals exists.");
                    return 3;
                }

                if (roleCount != ManagedRoles.Length)
                    throw new InvalidOperationException(PartialStateMessage(roleStatuses));

                await ExecuteAsync(connection, DatabasePrincipalProvisioningSql.Verify);
                Console.WriteLine("VERIFIED: database ownership, role attributes, memberships, least-privilege grants, and ceremony-function ACL match the installer contract.");
                return 0;
            }

            await using var transaction = await connection.BeginTransactionAsync();
            await ExecuteAsync(connection, DatabasePrincipalProvisioningSql.AcquireLock, transaction);
            roleStatuses = await ReadManagedRoleStatusAsync(connection, transaction);
            roleCount = roleStatuses.Count(x => x.Exists);
            if (roleCount is not (0 or 4))
                throw new InvalidOperationException(PartialStateMessage(roleStatuses) + " Provisioning refuses mixed state.");

            if (roleCount == 0)
                await SetInitialPasswordsAsync(connection, transaction);

            await ExecuteAsync(connection, DatabasePrincipalProvisioningSql.Provision, transaction);
            await ExecuteAsync(connection, DatabasePrincipalProvisioningSql.Verify, transaction);
            roleStatuses = await ReadManagedRoleStatusAsync(connection, transaction);
            await transaction.CommitAsync();
            WriteManagedRoleStatus(roleStatuses);
            Console.WriteLine(roleCount == 0
                ? "PROVISIONED: four database principals created, ownership transferred, and grants including any existing ceremony function verified."
                : "RECONCILED: existing principal contract and grants including any existing ceremony function verified; credentials were not changed.");
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

    private static async Task<IReadOnlyList<ManagedRoleStatus>> ReadManagedRoleStatusAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction = null)
    {
        await using var command = new NpgsqlCommand(DatabasePrincipalProvisioningSql.RoleStatus, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync();
        var statuses = new List<ManagedRoleStatus>(ManagedRoles.Length);
        while (await reader.ReadAsync())
        {
            statuses.Add(new ManagedRoleStatus(
                reader.GetString(0), reader.GetBoolean(1),
                reader.IsDBNull(2) ? null : reader.GetBoolean(2),
                reader.IsDBNull(3) ? null : reader.GetBoolean(3),
                reader.IsDBNull(4) ? null : reader.GetBoolean(4),
                reader.IsDBNull(5) ? null : reader.GetBoolean(5),
                reader.IsDBNull(6) ? null : reader.GetBoolean(6),
                reader.IsDBNull(7) ? null : reader.GetBoolean(7),
                reader.GetBoolean(8), reader.IsDBNull(9) ? null : reader.GetBoolean(9)));
        }
        return statuses;
    }

    private static string PartialStateMessage(IEnumerable<ManagedRoleStatus> statuses) =>
        $"PARTIAL_STATE: missing managed roles: {string.Join(", ", statuses.Where(x => !x.Exists).Select(x => x.RoleName))}.";

    private static void WriteManagedRoleStatus(IEnumerable<ManagedRoleStatus> statuses)
    {
        foreach (var status in statuses)
        {
            var attributes = status.Exists
                ? $"superuser:{YesNo(status.Superuser)},createdb:{YesNo(status.CreateDatabase)},createrole:{YesNo(status.CreateRole)},replication:{YesNo(status.Replication)},bypassrls:{YesNo(status.BypassRowLevelSecurity)}"
                : "n/a";
            var ceremonyGrant = !status.CeremonyFunctionExists || !status.Exists ? "n/a" : YesNo(status.CeremonyExecuteGrant);
            Console.WriteLine($"ROLE_STATUS role={status.RoleName} exists={YesNo(status.Exists)} login={YesNo(status.Login)} attributes={attributes} ceremony_function={(status.CeremonyFunctionExists ? "present" : "absent")} ceremony_execute_grant={ceremonyGrant}");
        }
    }

    private static string YesNo(bool? value) => value.HasValue ? (value.Value ? "yes" : "no") : "n/a";

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
        Console.Error.WriteLine("Usage: SESS.NexaERP.Installer database-principals <plan|status|provision>\n   or: SESS.NexaERP.Installer authentication-bootstrap --issuer <https-oidc-issuer> --subject <stable-provider-subject>"
#if DEBUG
            + "\n   or: SESS.NexaERP.Installer authentication-bootstrap-development --issuer <https-oidc-issuer> --subject <stable-provider-subject>"
#endif
        );

    private sealed record ManagedRoleStatus(
        string RoleName,
        bool Exists,
        bool? Login,
        bool? Superuser,
        bool? CreateDatabase,
        bool? CreateRole,
        bool? Replication,
        bool? BypassRowLevelSecurity,
        bool CeremonyFunctionExists,
        bool? CeremonyExecuteGrant);
}
