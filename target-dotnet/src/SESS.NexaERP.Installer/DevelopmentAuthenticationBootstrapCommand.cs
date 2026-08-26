#if DEBUG
using Npgsql;

internal static class DevelopmentAuthenticationBootstrapCommand
{
    private const string ConnectionVariable = "ConnectionStrings__NexaErpDevelopmentBootstrap";
    private const string ExpectedDatabaseVariable = "NexaErp__ExpectedDatabase";

    internal static async Task<int> RunAsync(string[] args)
    {
        if (!AuthenticationBootstrapCommand.TryParse(args, out var issuer, out var subject, out var error))
        {
            Console.Error.WriteLine(error);
            WriteUsage();
            return 2;
        }

        if (!string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), "Development", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("REFUSED: authentication-bootstrap-development requires DOTNET_ENVIRONMENT=Development.");
            return 1;
        }
        if (!bool.TryParse(Environment.GetEnvironmentVariable(InstallerCommand.DevelopmentBootstrapSetting), out var enabled) || !enabled)
        {
            Console.Error.WriteLine($"REFUSED: authentication-bootstrap-development requires {InstallerCommand.DevelopmentBootstrapSetting}=true.");
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
                ApplicationName = "SESS.NexaERP.Installer.DevelopmentAuthenticationBootstrap",
                IncludeErrorDetail = false,
                Pooling = false
            };
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            var originalSessionUser = await RequireSafeClusterAsync(connection, expectedDatabase.Trim());
            await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                await ExecuteAsync(connection, transaction, DevelopmentAuthenticationBootstrapCommandSql.AcquireLockAndCreateRole);
                await using var command = new NpgsqlCommand(AuthenticationBootstrapCommandSql.Complete, connection, transaction);
                command.Parameters.AddWithValue("issuer", issuer!);
                command.Parameters.AddWithValue("subject", subject!);
                var result = (string?)await command.ExecuteScalarAsync()
                    ?? throw new InvalidOperationException("Bootstrap function returned no completion witness.");
                await ExecuteAsync(connection, transaction, DevelopmentAuthenticationBootstrapCommandSql.RestoreAndDropRole);
                await transaction.CommitAsync();
                await RequireRecoveryWitnessAsync(connection, originalSessionUser);
                Console.WriteLine($"COMPLETED_DEVELOPMENT_ONLY: {result}");
                return 0;
            }
            catch (Exception ceremonyException)
            {
                try
                {
                    await transaction.RollbackAsync();
                    await RequireRecoveryWitnessAsync(connection, originalSessionUser);
                }
                catch (Exception recoveryException)
                {
                    throw new DevelopmentBootstrapRecoveryException(
                        "Development bootstrap rollback did not restore session authorization and remove its temporary role.",
                        recoveryException);
                }
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ceremonyException).Throw();
                throw new InvalidOperationException("Unreachable development bootstrap exception path.");
            }
        }
        catch (DevelopmentBootstrapRecoveryException exception)
        {
            Console.Error.WriteLine($"RECOVERY_FAILURE: {exception.Message}");
            return 3;
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            Console.Error.WriteLine($"REFUSED: {exception.Message}");
            return 1;
        }
    }

    private static async Task<string> RequireSafeClusterAsync(NpgsqlConnection connection, string expectedDatabase)
    {
        await using var command = new NpgsqlCommand(DevelopmentAuthenticationBootstrapCommandSql.ClusterGuard, connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("PostgreSQL cluster guard returned no witness.");
        if (reader.GetInt32(0) < 170000) throw new InvalidOperationException("Development authentication bootstrap requires PostgreSQL 17 or later.");
        if (reader.GetString(1) is "postgres" or "template0" or "template1" || !string.Equals(reader.GetString(1), expectedDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException("Database target does not match NexaErp__ExpectedDatabase.");
        if (!reader.GetBoolean(2)) throw new InvalidOperationException("The advance schema must exist before development authentication bootstrap.");
        var sessionUser = reader.GetString(3);
        if (!string.Equals(sessionUser, reader.GetString(4), StringComparison.Ordinal))
            throw new InvalidOperationException("Development authentication bootstrap refuses an already-assumed database role.");
        if (!reader.GetBoolean(5) || !reader.GetBoolean(6) || !reader.GetBoolean(7))
            throw new InvalidOperationException("Development authentication bootstrap requires a superuser that owns the exact database and advance schema.");
        if (!reader.GetBoolean(8)) throw new InvalidOperationException("The reviewed authentication bootstrap function is not installed.");
        if (reader.GetInt64(9) != 0) throw new InvalidOperationException("Development authentication bootstrap requires all four managed database principals to be absent.");
        return sessionUser;
    }

    private static async Task RequireRecoveryWitnessAsync(NpgsqlConnection connection, string originalSessionUser)
    {
        await using var command = new NpgsqlCommand(DevelopmentAuthenticationBootstrapCommandSql.RecoveryWitness, connection);
        command.Parameters.AddWithValue("original_session_user", originalSessionUser);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || !reader.GetBoolean(0) || !reader.GetBoolean(1) || !reader.GetBoolean(2))
            throw new InvalidOperationException("Development bootstrap transaction did not restore session authorization and remove its temporary role.");
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }

    private static void WriteUsage() =>
        Console.Error.WriteLine("Usage: SESS.NexaERP.Installer authentication-bootstrap-development --issuer <https-oidc-issuer> --subject <stable-provider-subject>");

    private sealed class DevelopmentBootstrapRecoveryException(string message, Exception innerException)
        : Exception(message, innerException);
}
#endif
