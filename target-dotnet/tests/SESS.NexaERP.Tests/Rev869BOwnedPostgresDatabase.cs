using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace SESS.NexaERP.Tests;

internal sealed class Rev869BOwnedPostgresDatabase : IAsyncDisposable
{
    private const string ExactDatabase = "sess_nexaerp_rev869b_verify";
    private const string ExactOptIn = "ISOLATED_REV869B_BEHAVIOR_TESTS";
    private const string MigrationId = "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation";
    public const string Organization = "REV869B-PG-DIRECT-TEST-OWNED";
    public const string Login = "REV869B-PG-DIRECT-ACTOR";
    private readonly string adminConnectionString;
    private bool disposed;

    private Rev869BOwnedPostgresDatabase(string connectionString, string adminConnectionString, string databaseName)
    { ConnectionString = connectionString; this.adminConnectionString = adminConnectionString; DatabaseName = databaseName; }

    public string ConnectionString { get; }
    public string DatabaseName { get; }

    public static async Task<Rev869BOwnedPostgresDatabase> CreateAsync(string scenario)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("REV869B_POSTGRES_OPT_IN"), ExactOptIn, StringComparison.Ordinal))
            throw new InvalidOperationException($"Set REV869B_POSTGRES_OPT_IN={ExactOptIn} explicitly.");
        var raw = Environment.GetEnvironmentVariable("REV869B_POSTGRES")
            ?? throw new InvalidOperationException("REV869B_POSTGRES is required; no fallback is permitted.");
        var source = new NpgsqlConnectionStringBuilder(raw);
        if (!string.Equals(source.Database, ExactDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException($"Only the exact isolated database {ExactDatabase} is permitted.");

        var suffix = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("REV869B-DIRECT|" + scenario)))[..20].ToLowerInvariant();
        var databaseName = "rev869b_direct_" + suffix;
        var admin = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = "postgres", Pooling = false };
        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        var collision = new NpgsqlCommand("SELECT count(*) FROM pg_database WHERE datname=@name", connection);
        collision.Parameters.AddWithValue("name", databaseName);
        if (Convert.ToInt64(await collision.ExecuteScalarAsync()) != 0)
            throw new InvalidOperationException("Deterministic direct-test database already exists; ownership is not proven.");
        var quotedOwned = new NpgsqlCommandBuilder().QuoteIdentifier(databaseName);
        var quotedTemplate = new NpgsqlCommandBuilder().QuoteIdentifier(ExactDatabase);
        await new NpgsqlCommand($"CREATE DATABASE {quotedOwned} WITH TEMPLATE {quotedTemplate}", connection).ExecuteNonQueryAsync();

        var owned = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = databaseName, Pooling = false };
        var lease = new Rev869BOwnedPostgresDatabase(owned.ConnectionString, admin.ConnectionString, databaseName);
        try
        {
            await lease.VerifyAsync();
            await Rev869BCompleteGraphSeeder.SeedAsync(lease.ConnectionString, scenario);
            return lease;
        }
        catch
        {
            await lease.DisposeAsync();
            throw;
        }
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        var actual = Convert.ToString(await new NpgsqlCommand("SELECT current_database()", connection).ExecuteScalarAsync());
        if (!string.Equals(actual, DatabaseName, StringComparison.Ordinal))
        {
            await connection.DisposeAsync();
            throw new InvalidOperationException("Owned direct-test database identity changed.");
        }
        return connection;
    }

    public static async Task SetCommandContextAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid actorEmployeeId, string role)
    {
        var command = new NpgsqlCommand("SELECT set_config('nexa.rev869b_actor_employee_id',@employee,true),set_config('nexa.rev869b_actor_login',@login,true),set_config('nexa.rev869b_actor_role',@role,true),set_config('nexa.rev869b_organization',@organization,true)", connection, transaction);
        command.Parameters.AddWithValue("employee", actorEmployeeId.ToString());
        command.Parameters.AddWithValue("login", Login);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("organization", Organization);
        await command.ExecuteNonQueryAsync();
    }

    private async Task VerifyAsync()
    {
        await using var connection = await OpenConnectionAsync();
        var quote = new NpgsqlCommandBuilder();
        var command = new NpgsqlCommand($"SELECT count(*) FROM nexa.{quote.QuoteIdentifier("__EFMigrationsHistory")} WHERE {quote.QuoteIdentifier("MigrationId")}=@id", connection);
        command.Parameters.AddWithValue("id", MigrationId);
        if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("The exact REV869B migration is not installed once in the isolated template.");
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        var quoted = new NpgsqlCommandBuilder().QuoteIdentifier(DatabaseName);
        try { await new NpgsqlCommand($"DROP DATABASE {quoted} WITH (FORCE)", connection).ExecuteNonQueryAsync(); }
        finally
        {
            var check = new NpgsqlCommand("SELECT count(*) FROM pg_database WHERE datname=@name", connection);
            check.Parameters.AddWithValue("name", DatabaseName);
            if (Convert.ToInt64(await check.ExecuteScalarAsync()) != 0)
                throw new InvalidOperationException("Direct-test owned database cleanup was not exact.");
        }
    }
}
