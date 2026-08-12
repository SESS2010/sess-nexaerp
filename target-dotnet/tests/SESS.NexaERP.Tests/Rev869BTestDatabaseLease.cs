using System.Security.Cryptography;
using Npgsql;

namespace SESS.NexaERP.Tests;

/// <summary>
/// Source-only design for a disposable REV869B database lease. Merely discovering/listing tests
/// never calls this type. Provisioning is opt-in and every destructive boundary re-proves the
/// source/target identity, retained migration and an unguessable per-run ownership marker.
/// A failed proof quarantines the database by refusing DROP; it never attempts name-only repair.
/// </summary>
internal sealed class Rev869BTestDatabaseLease : IAsyncDisposable
{
    internal const string ExactSourceDatabase = "sess_nexaerp_rev869b_verify";
    internal const string ExactOptIn = "ISOLATED_REV869B_BEHAVIOR_TESTS";
    internal const string MigrationId = "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation";
    internal const string DatabasePrefix = "sess_nexaerp_rev869b_owned_";
    private const string MarkerTable = "rev869b_test_database_lease";
    private readonly string adminConnectionString;
    private readonly SemaphoreSlim disposalGate = new(1, 1);
    private bool disposed;

    private Rev869BTestDatabaseLease(
        string connectionString,
        string adminConnectionString,
        string databaseName,
        string runId,
        string ownershipToken,
        string family)
    {
        ConnectionString = connectionString;
        this.adminConnectionString = adminConnectionString;
        DatabaseName = databaseName;
        RunId = runId;
        OwnershipToken = ownershipToken;
        Family = family;
    }

    internal string ConnectionString { get; }
    internal string DatabaseName { get; }
    internal string RunId { get; }
    internal string OwnershipToken { get; }
    internal string Family { get; }

    internal static async Task<Rev869BTestDatabaseLease> CreateAsync(string scenario, string family)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("REV869B_POSTGRES_OPT_IN"), ExactOptIn, StringComparison.Ordinal))
            throw new InvalidOperationException($"Set REV869B_POSTGRES_OPT_IN={ExactOptIn} explicitly.");
        var raw = Environment.GetEnvironmentVariable("REV869B_POSTGRES")
            ?? throw new InvalidOperationException("REV869B_POSTGRES is required; no fallback is permitted.");
        var source = new NpgsqlConnectionStringBuilder(raw) { Pooling = false };
        if (!string.Equals(source.Database, ExactSourceDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException($"Only the exact isolated source database {ExactSourceDatabase} is permitted.");

        await VerifySourceAsync(source.ConnectionString);

        var runId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var ownershipToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var databaseName = DatabasePrefix + runId[..24];
        RequireSafeOwnedName(databaseName);
        var admin = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = "postgres", Pooling = false };
        await using (var connection = new NpgsqlConnection(admin.ConnectionString))
        {
            await connection.OpenAsync();
            await RequireCurrentDatabaseAsync(connection, "postgres", "administrative CREATE boundary");
            await RequireDatabaseAbsentAsync(connection, databaseName);
            var quotedOwned = new NpgsqlCommandBuilder().QuoteIdentifier(databaseName);
            var quotedTemplate = new NpgsqlCommandBuilder().QuoteIdentifier(ExactSourceDatabase);
            await new NpgsqlCommand($"CREATE DATABASE {quotedOwned} WITH TEMPLATE {quotedTemplate}", connection).ExecuteNonQueryAsync();
        }

        var owned = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = databaseName, Pooling = false };
        var lease = new Rev869BTestDatabaseLease(owned.ConnectionString, admin.ConnectionString, databaseName, runId, ownershipToken, family);
        try
        {
            await lease.EstablishMarkerAsync(scenario);
            await lease.VerifyOwnershipAsync();
            return lease;
        }
        catch
        {
            // The unique database is deliberately quarantined when its marker cannot be proved.
            // An explicit cleanup must independently verify the exact marker before DROP.
            try { await lease.DisposeAsync(); } catch { }
            throw;
        }
    }

    internal async Task<NpgsqlConnection> OpenVerifiedConnectionAsync()
    {
        if (disposed) throw new ObjectDisposedException(nameof(Rev869BTestDatabaseLease));
        var connection = new NpgsqlConnection(ConnectionString);
        try
        {
            await connection.OpenAsync();
            await VerifyOwnershipAsync(connection);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static async Task VerifySourceAsync(string sourceConnectionString)
    {
        await using var source = new NpgsqlConnection(sourceConnectionString);
        await source.OpenAsync();
        await RequireCurrentDatabaseAsync(source, ExactSourceDatabase, "source identity before CREATE");
        await RequireMigrationOnceAsync(source);
    }

    private async Task EstablishMarkerAsync(string scenario)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await RequireCurrentDatabaseAsync(connection, DatabaseName, "target marker provisioning");
        await using var command = new NpgsqlCommand($$"""
            CREATE TABLE nexa.{{MarkerTable}}(
              "OwnershipToken" text PRIMARY KEY,
              "RunId" text NOT NULL UNIQUE,
              "DatabaseName" text NOT NULL UNIQUE,
              "SourceDatabase" text NOT NULL,
              "MigrationId" text NOT NULL,
              "FixtureFamily" text NOT NULL,
              "ScenarioHash" text NOT NULL,
              "ProvisionedAt" timestamptz NOT NULL DEFAULT statement_timestamp()
            );
            REVOKE ALL ON nexa.{{MarkerTable}} FROM PUBLIC;
            INSERT INTO nexa.{{MarkerTable}}("OwnershipToken","RunId","DatabaseName","SourceDatabase","MigrationId","FixtureFamily","ScenarioHash")
            VALUES(@token,@run,@database,@source,@migration,@family,@scenario);
            """, connection);
        command.Parameters.AddWithValue("token", OwnershipToken);
        command.Parameters.AddWithValue("run", RunId);
        command.Parameters.AddWithValue("database", DatabaseName);
        command.Parameters.AddWithValue("source", ExactSourceDatabase);
        command.Parameters.AddWithValue("migration", MigrationId);
        command.Parameters.AddWithValue("family", Family);
        command.Parameters.AddWithValue("scenario", Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(scenario))));
        await command.ExecuteNonQueryAsync();
    }

    private async Task VerifyOwnershipAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await VerifyOwnershipAsync(connection);
    }

    private async Task VerifyOwnershipAsync(NpgsqlConnection connection)
    {
        RequireSafeOwnedName(DatabaseName);
        await RequireCurrentDatabaseAsync(connection, DatabaseName, "target connect/use/drop proof");
        await RequireMigrationOnceAsync(connection);
        await using var marker = new NpgsqlCommand($$"""
            SELECT count(*) FROM nexa.{{MarkerTable}}
            WHERE "OwnershipToken"=@token AND "RunId"=@run AND "DatabaseName"=@database
              AND "SourceDatabase"=@source AND "MigrationId"=@migration AND "FixtureFamily"=@family
            """, connection);
        marker.Parameters.AddWithValue("token", OwnershipToken);
        marker.Parameters.AddWithValue("run", RunId);
        marker.Parameters.AddWithValue("database", DatabaseName);
        marker.Parameters.AddWithValue("source", ExactSourceDatabase);
        marker.Parameters.AddWithValue("migration", MigrationId);
        marker.Parameters.AddWithValue("family", Family);
        if (Convert.ToInt64(await marker.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("Owned database marker mismatch; database is quarantined and DROP is refused.");
    }

    private static async Task RequireMigrationOnceAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("SELECT count(*) FROM nexa.\"__EFMigrationsHistory\" WHERE \"MigrationId\"=@migration", connection);
        command.Parameters.AddWithValue("migration", MigrationId);
        if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("The retained REV869B migration must be installed exactly once.");
    }

    private static async Task RequireCurrentDatabaseAsync(NpgsqlConnection connection, string expected, string boundary)
    {
        var actual = Convert.ToString(await new NpgsqlCommand("SELECT current_database()", connection).ExecuteScalarAsync());
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new InvalidOperationException($"Database identity failed at {boundary}.");
    }

    private static async Task RequireDatabaseAbsentAsync(NpgsqlConnection connection, string databaseName)
    {
        await using var exists = new NpgsqlCommand("SELECT count(*) FROM pg_database WHERE datname=@name", connection);
        exists.Parameters.AddWithValue("name", databaseName);
        if (Convert.ToInt64(await exists.ExecuteScalarAsync()) != 0)
            throw new InvalidOperationException("Unique test database collision; ownership is not proven and no repair is attempted.");
    }

    private static void RequireSafeOwnedName(string databaseName)
    {
        var suffix = databaseName.StartsWith(DatabasePrefix, StringComparison.Ordinal) ? databaseName[DatabasePrefix.Length..] : string.Empty;
        if (suffix.Length != 24 || suffix.Any(c => !Uri.IsHexDigit(c)) ||
            string.Equals(databaseName, ExactSourceDatabase, StringComparison.Ordinal) ||
            databaseName is "postgres" or "template0" or "template1" ||
            databaseName.Contains("rev861", StringComparison.OrdinalIgnoreCase) ||
            databaseName.Contains("rev868", StringComparison.OrdinalIgnoreCase) ||
            databaseName.Contains("rev869a", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unsafe or unexpected REV869B disposable database name.");
    }

    public async ValueTask DisposeAsync()
    {
        await disposalGate.WaitAsync();
        try
        {
            if (disposed) return;
            // Verify while the target is still reachable. Any mismatch leaves it quarantined.
            await VerifyOwnershipAsync();
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(adminConnectionString);
            await admin.OpenAsync();
            await RequireCurrentDatabaseAsync(admin, "postgres", "administrative DROP boundary");
            RequireSafeOwnedName(DatabaseName);
            var quoted = new NpgsqlCommandBuilder().QuoteIdentifier(DatabaseName);
            await new NpgsqlCommand($"DROP DATABASE {quoted} WITH (FORCE)", admin).ExecuteNonQueryAsync();
            await RequireDatabaseAbsentAsync(admin, DatabaseName);
            disposed = true;
        }
        finally
        {
            disposalGate.Release();
        }
    }
}
