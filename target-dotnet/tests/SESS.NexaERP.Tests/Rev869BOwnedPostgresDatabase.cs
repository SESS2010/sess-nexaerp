using System.Text.Json;
using Npgsql;

namespace SESS.NexaERP.Tests;

internal sealed class Rev869BOwnedPostgresDatabase : IAsyncDisposable
{
    public const string Organization = "REV869B-PG-SELF-OWNED-GRAPH";
    public const string Login = "REV869B-PG-DIRECT-ACTOR";
    public const string Issuer = "REV869B-TEST";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Rev869BTestDatabaseLease lease;

    private Rev869BOwnedPostgresDatabase(Rev869BTestDatabaseLease lease)
    { this.lease = lease; ConnectionString = lease.ConnectionString; DatabaseName = lease.DatabaseName; }

    public string ConnectionString { get; }
    public string DatabaseName { get; }

    public static async Task<Rev869BOwnedPostgresDatabase> CreateAsync(string scenario)
    {
        var databaseLease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "direct");
        var owned = new Rev869BOwnedPostgresDatabase(databaseLease);
        try
        {
            await Rev869BCompleteGraphSeeder.SeedAsync(databaseLease.OwnerConnectionString, scenario);
            return owned;
        }
        catch
        {
            await owned.DisposeAsync();
            throw;
        }
    }

    public Task<NpgsqlConnection> OpenConnectionAsync() => lease.OpenVerifiedConnectionAsync();

    public static async Task SetCommandContextAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid actorEmployeeId,
        string role,
        params ExactSlot[] slots)
    {
        if (slots.Length == 0) throw new InvalidOperationException("Direct database tests must pre-authorize at least one exact history slot.");
        var transactionId = Convert.ToInt64(await new NpgsqlCommand("SELECT txid_current()", connection, transaction).ExecuteScalarAsync());
        var backendPid = Convert.ToInt32(await new NpgsqlCommand("SELECT pg_backend_pid()", connection, transaction).ExecuteScalarAsync());
        var runtimePrincipal = Convert.ToString(await new NpgsqlCommand("SELECT session_user", connection, transaction).ExecuteScalarAsync())
            ?? throw new InvalidOperationException("Runtime principal is missing.");
        var issuerRaw = Environment.GetEnvironmentVariable("REV869B_COMMAND_ISSUER_CONNECTION")
            ?? throw new InvalidOperationException("Owned command issuer connection is missing.");
        var issuerBuilder = new NpgsqlConnectionStringBuilder(issuerRaw) { Pooling = false };
        var runtimeBuilder = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
        if (!string.Equals(issuerBuilder.Database, runtimeBuilder.Database, StringComparison.Ordinal) ||
            string.Equals(issuerBuilder.Username, runtimeBuilder.Username, StringComparison.Ordinal))
            throw new InvalidOperationException("Owned issuer must be distinct and target the exact runtime database.");

        Guid grantId;
        await using (var issuer = new NpgsqlConnection(issuerBuilder.ConnectionString))
        {
            await issuer.OpenAsync();
            await using var issue = new NpgsqlCommand("""
                SELECT nexa.rev869b_issue_command_grant(
                  @runtime,@backend,@transaction,@actor,@issuer,@subject,@role,@organization,@authenticated,@slots::jsonb)
                """, issuer);
            issue.Parameters.AddWithValue("runtime", runtimePrincipal);
            issue.Parameters.AddWithValue("backend", backendPid);
            issue.Parameters.AddWithValue("transaction", transactionId);
            issue.Parameters.AddWithValue("actor", actorEmployeeId);
            issue.Parameters.AddWithValue("issuer", Issuer);
            issue.Parameters.AddWithValue("subject", Login);
            issue.Parameters.AddWithValue("role", role);
            issue.Parameters.AddWithValue("organization", Organization);
            issue.Parameters.AddWithValue("authenticated", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            issue.Parameters.AddWithValue("slots", JsonSerializer.Serialize(slots, JsonOptions));
            grantId = (Guid)(await issue.ExecuteScalarAsync() ?? throw new InvalidOperationException("Owned exact grant was not returned."));
        }

        await using var open = new NpgsqlCommand("""
            SELECT nexa.rev869b_open_command_context(
              @grant,@actor,@issuer,@subject,@role,@organization,@backend,@transaction)
            """, connection, transaction);
        open.Parameters.AddWithValue("grant", grantId);
        open.Parameters.AddWithValue("actor", actorEmployeeId);
        open.Parameters.AddWithValue("issuer", Issuer);
        open.Parameters.AddWithValue("subject", Login);
        open.Parameters.AddWithValue("role", role);
        open.Parameters.AddWithValue("organization", Organization);
        open.Parameters.AddWithValue("backend", backendPid);
        open.Parameters.AddWithValue("transaction", transactionId);
        await open.ExecuteNonQueryAsync();
    }

    internal sealed record ExactSlot(string ClaimKind, Guid HistoryId, string EntityType, Guid EntityId,
        string Operation, long ParentVersion, string? FromStatus, string ToStatus, string Correlation, string Remarks);

    public ValueTask DisposeAsync() => lease.DisposeAsync();
}
