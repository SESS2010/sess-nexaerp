using Npgsql;

internal static class AuthenticationBootstrapCommand
{
    private const string ConnectionVariable = "ConnectionStrings__NexaErpBootstrap";
    private const string ExpectedDatabaseVariable = "NexaErp__ExpectedDatabase";

    internal static async Task<int> RunAsync(string[] args)
    {
        if (!TryParse(args, out var issuer, out var subject, out var error))
        {
            Console.Error.WriteLine(error);
            WriteUsage();
            return 2;
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
                ApplicationName = "SESS.NexaERP.Installer.AuthenticationBootstrap",
                IncludeErrorDetail = false,
                Pooling = false
            };
            if (!string.Equals(builder.Username, "nexa_erp_bootstrap", StringComparison.Ordinal))
                throw new InvalidOperationException("Authentication bootstrap requires the dedicated nexa_erp_bootstrap connection principal.");
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            await RequireSafeClusterAsync(connection, expectedDatabase.Trim());
            await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await using var command = new NpgsqlCommand(AuthenticationBootstrapCommandSql.Complete, connection, transaction);
            command.Parameters.AddWithValue("issuer", issuer!);
            command.Parameters.AddWithValue("subject", subject!);
            var result = (string?)await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Bootstrap function returned no completion witness.");
            await transaction.CommitAsync();
            Console.WriteLine($"COMPLETED: {result}");
            return 0;
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            Console.Error.WriteLine($"REFUSED: {exception.Message}");
            return 1;
        }
    }

    internal static bool TryParse(string[] args, out string? issuer, out string? subject, out string error)
    {
        issuer = null; subject = null; error = string.Empty;
        if (args.Length != 4)
        {
            error = "Exactly --issuer and --subject are required.";
            return false;
        }
        for (var index = 0; index < args.Length; index += 2)
        {
            var value = args[index + 1].Trim();
            if (args[index] == "--issuer" && issuer is null) issuer = value.TrimEnd('/');
            else if (args[index] == "--subject" && subject is null) subject = value;
            else { error = $"Unknown or repeated option: {args[index]}"; return false; }
        }
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject) || issuer.Length > 500 || subject.Length > 500)
        {
            error = "Issuer and subject must be non-empty and no longer than 500 characters.";
            return false;
        }
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            error = "Issuer must be an absolute HTTPS OIDC issuer without credentials, query, or fragment.";
            return false;
        }
        return true;
    }

    private static async Task RequireSafeClusterAsync(NpgsqlConnection connection, string expectedDatabase)
    {
        await using var command = new NpgsqlCommand(AuthenticationBootstrapCommandSql.ClusterGuard, connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("PostgreSQL cluster guard returned no witness.");
        if (reader.GetInt32(0) < 170000) throw new InvalidOperationException("Authentication bootstrap requires PostgreSQL 17 or later.");
        if (reader.GetString(1) is "postgres" or "template0" or "template1" || !string.Equals(reader.GetString(1), expectedDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException("Database target does not match NexaErp__ExpectedDatabase.");
        if (!reader.GetBoolean(2)) throw new InvalidOperationException("The advance schema must exist before authentication bootstrap.");
        if (!string.Equals(reader.GetString(3), "nexa_erp_bootstrap", StringComparison.Ordinal)) throw new InvalidOperationException("The session principal is not nexa_erp_bootstrap.");
        if (!reader.GetBoolean(4)) throw new InvalidOperationException("The reviewed authentication bootstrap function is not installed.");
    }

    private static void WriteUsage() =>
        Console.Error.WriteLine("Usage: SESS.NexaERP.Installer authentication-bootstrap --issuer <https-oidc-issuer> --subject <stable-provider-subject>");
}
