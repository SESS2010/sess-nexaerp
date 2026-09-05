#if DEBUG
using System.Net;
using Npgsql;

internal static class DevelopmentControlledCommandPrincipalCommand
{
    private const string OptInVariable = "NexaErp__EnableDevelopmentControlledCommands";
    private const string ConnectionVariable = "ConnectionStrings__NexaErpDevelopmentControlledCommandBootstrap";
    private const string ExpectedDatabaseVariable = "NexaErp__ExpectedDatabase";
    private const string RuntimePasswordVariable = "NEXAERP_REV869B_APP_RUNTIME_PASSWORD";
    private const string AuditPasswordVariable = "NEXAERP_REV869B_COMMAND_AUDIT_PASSWORD";

    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1 || args[0] is not ("status" or "provision" or "remove"))
        {
            WriteUsage();
            return 2;
        }
        if (!string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), "Development", StringComparison.Ordinal) ||
            !bool.TryParse(Environment.GetEnvironmentVariable(OptInVariable), out var enabled) || !enabled)
        {
            Console.Error.WriteLine($"REFUSED: this Debug-only command requires DOTNET_ENVIRONMENT=Development and {OptInVariable}=true.");
            return 1;
        }
        var raw = Environment.GetEnvironmentVariable(ConnectionVariable);
        var expectedDatabase = Environment.GetEnvironmentVariable(ExpectedDatabaseVariable);
        if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(expectedDatabase))
        {
            Console.Error.WriteLine($"Set {ConnectionVariable} and {ExpectedDatabaseVariable}; connection strings are never accepted as arguments.");
            return 2;
        }

        try
        {
            var installer = new NpgsqlConnectionStringBuilder(raw) { Pooling = false, IncludeErrorDetail = false };
            RequireLoopback(installer.Host);
            await using var connection = new NpgsqlConnection(installer.ConnectionString);
            await connection.OpenAsync();
            await RequireSafeClusterAsync(connection, expectedDatabase.Trim());
            if (args[0] == "status")
            {
                await WriteStatusAsync(connection);
                return 0;
            }

            var runtimePassword = args[0] == "provision" ? RequirePassword(RuntimePasswordVariable) : null;
            var auditPassword = args[0] == "provision" ? RequirePassword(AuditPasswordVariable) : null;
            await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await using (var advisory = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('SESS.NexaERP.DevelopmentControlledCommandPrincipals.v1',0))", connection, transaction))
                await advisory.ExecuteNonQueryAsync();
            if (args[0] == "provision")
            {
                await SetSecretAsync(connection, transaction, "runtime_password", runtimePassword!);
                await SetSecretAsync(connection, transaction, "audit_password", auditPassword!);
                await ExecuteAsync(connection, transaction, """
                    DO $provision$
                    BEGIN
                      ALTER ROLE nexa_rev869b_app_runtime PASSWORD NULL;
                      ALTER ROLE nexa_rev869b_command_audit PASSWORD NULL;
                      EXECUTE format('ALTER ROLE nexa_rev869b_app_runtime PASSWORD %L',current_setting('nexa.development.runtime_password'));
                      EXECUTE format('ALTER ROLE nexa_rev869b_command_audit PASSWORD %L',current_setting('nexa.development.audit_password'));
                    END $provision$;
                    """);
            }
            else
            {
                await ExecuteAsync(connection, transaction, "ALTER ROLE nexa_rev869b_app_runtime PASSWORD NULL; ALTER ROLE nexa_rev869b_command_audit PASSWORD NULL;");
            }
            await transaction.CommitAsync();

            if (args[0] == "provision")
            {
                await VerifyLoginAsync(installer, expectedDatabase.Trim(), "nexa_rev869b_app_runtime", runtimePassword!);
                await VerifyLoginAsync(installer, expectedDatabase.Trim(), "nexa_rev869b_command_audit", auditPassword!);
                Console.WriteLine("PROVISIONED_DEVELOPMENT_ONLY: two real non-administrative LOGIN principals accepted direct loopback sessions.");
            }
            else
            {
                await WriteStatusAsync(connection);
                Console.WriteLine("REMOVED_DEVELOPMENT_ONLY: passwords cleared from both controlled-command development principals.");
            }
            return 0;
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine($"REFUSED: {exception.Message}");
            return 1;
        }
    }

    private static async Task RequireSafeClusterAsync(NpgsqlConnection connection, string expectedDatabase)
    {
        await using var command = new NpgsqlCommand("""
            SELECT current_setting('server_version_num')::integer,current_database(),session_user,current_user,
                   r.rolsuper,to_regnamespace('advance') IS NOT NULL,
                   count(*) FILTER (WHERE managed.rolname IS NOT NULL)=2,
                   bool_and(NOT managed.rolsuper AND NOT managed.rolcreatedb AND NOT managed.rolcreaterole AND
                            NOT managed.rolreplication AND NOT managed.rolbypassrls AND NOT managed.rolinherit AND managed.rolcanlogin)
            FROM pg_roles r
            CROSS JOIN LATERAL (SELECT m.* FROM pg_roles m WHERE m.rolname IN ('nexa_rev869b_app_runtime','nexa_rev869b_command_audit')) managed
            WHERE r.rolname=session_user
            GROUP BY r.rolsuper
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("PostgreSQL cluster guard returned no witness.");
        if (reader.GetInt32(0) < 170000) throw new InvalidOperationException("PostgreSQL 17 or later is required.");
        var database = reader.GetString(1);
        if (database is "postgres" or "template0" or "template1" || !string.Equals(database, expectedDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException("Target does not match the expected non-administrative database.");
        if (!string.Equals(reader.GetString(2), reader.GetString(3), StringComparison.Ordinal))
            throw new InvalidOperationException("Bootstrap refuses an assumed role; use the real installer session.");
        if (!reader.GetBoolean(4) || !reader.GetBoolean(5))
            throw new InvalidOperationException("Bootstrap requires a superuser installer session and the existing advance schema.");
        if (!reader.GetBoolean(6) || !reader.GetBoolean(7))
            throw new InvalidOperationException("Both canonical capability-free REV869B LOGIN roles must already have been installed by migrations.");
    }

    private static async Task VerifyLoginAsync(NpgsqlConnectionStringBuilder template, string database, string user, string password)
    {
        var target = new NpgsqlConnectionStringBuilder(template.ConnectionString)
        {
            Database = database, Username = user, Password = password, Pooling = false,
            ApplicationName = "SESS.NexaERP.Installer.DevelopmentControlledCommandPrincipalWitness"
        };
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT session_user,current_user,current_database(),rolsuper,rolcreatedb,rolcreaterole,rolreplication,rolbypassrls FROM pg_roles WHERE rolname=session_user", connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || !string.Equals(reader.GetString(0), user, StringComparison.Ordinal) ||
            !string.Equals(reader.GetString(1), user, StringComparison.Ordinal) || !string.Equals(reader.GetString(2), database, StringComparison.Ordinal) ||
            Enumerable.Range(3, 5).Any(reader.GetBoolean))
            throw new InvalidOperationException($"Direct session witness failed for {user}.");
        Console.WriteLine($"LOGIN_WITNESS principal={user} session_user={reader.GetString(0)} current_user={reader.GetString(1)} database={reader.GetString(2)} administrative=no");
    }

    private static async Task WriteStatusAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("SELECT rolname,rolpassword IS NOT NULL FROM pg_authid WHERE rolname IN ('nexa_rev869b_app_runtime','nexa_rev869b_command_audit') ORDER BY rolname", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) Console.WriteLine($"DEVELOPMENT_PRINCIPAL role={reader.GetString(0)} credential={(reader.GetBoolean(1) ? "present" : "absent")}");
    }

    private static async Task SetSecretAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string name, string value)
    {
        await using var command = new NpgsqlCommand($"SELECT set_config('nexa.development.{name}',@secret,true)", connection, transaction);
        command.Parameters.AddWithValue("secret", value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync();
    }

    private static string RequirePassword(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(value) || value.Length < 24)
            throw new InvalidOperationException($"{variable} must contain at least 24 characters.");
        return value;
    }

    private static void RequireLoopback(string? host)
    {
        if (!string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) &&
            (!IPAddress.TryParse(host, out var address) || !IPAddress.IsLoopback(address)))
            throw new InvalidOperationException("Development principal bootstrap requires a literal loopback host.");
    }

    private static void WriteUsage() => Console.Error.WriteLine(
        "Usage: SESS.NexaERP.Installer controlled-command-development-principals <status|provision|remove>");
}
#endif