using System.Security.Cryptography;
using System.Text;
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
    internal string VerifierConnectionString => lease.VerifierConnectionString;
    public string DatabaseName { get; }

    public static async Task<Rev869BOwnedPostgresDatabase> CreateAsync(string scenario)
    {
        var databaseLease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "direct");
        var owned = new Rev869BOwnedPostgresDatabase(databaseLease);
        return owned;
    }

    public Task<NpgsqlConnection> OpenConnectionAsync() => lease.OpenVerifiedConnectionAsync();

    public static async Task<Guid> SetCommandContextAsync(NpgsqlConnection connection,
        NpgsqlTransaction transaction, Guid actorEmployeeId, string role, params ExactSlot[] slots)
    {
        if (slots.Length == 0)
            throw new InvalidOperationException("At least one exact history slot is required.");
        var serialized = JsonSerializer.Serialize(slots, JsonOptions);
        var requestSha = SHA256.HashData(Encoding.UTF8.GetBytes(serialized));
        var callerKey = string.Join('|', slots.Select(s => s.Correlation)) + "|" + actorEmployeeId;
        var keySha = SHA256.HashData(Encoding.UTF8.GetBytes(callerKey));
        var operation = string.Join('+', slots.Select(s => s.Operation).Distinct(StringComparer.Ordinal).Order());
        var backend = Convert.ToInt32(await new NpgsqlCommand("SELECT pg_backend_pid()", connection, transaction).ExecuteScalarAsync());
        var transactionId = Convert.ToInt64(await new NpgsqlCommand("SELECT txid_current()", connection, transaction).ExecuteScalarAsync());

        var auditRaw = Environment.GetEnvironmentVariable("REV869B_COMMAND_AUDIT_CONNECTION")
            ?? throw new InvalidOperationException("REV869B command-audit connection is required.");
        var auditBuilder = new NpgsqlConnectionStringBuilder(auditRaw) { Pooling = false };
        var runtimeBuilder = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
        if (!string.Equals(auditBuilder.Database, runtimeBuilder.Database, StringComparison.Ordinal) ||
            !string.Equals(auditBuilder.Username, "nexa_rev869b_command_audit", StringComparison.Ordinal))
            throw new InvalidOperationException("Exact target command-audit identity is required.");

        var execution = RequireGuid("REV869B_EXECUTION_INSTANCE_ID");
        var service = RequireFingerprint("REV869B_SERVICE_INSTANCE_FINGERPRINT");
        var ownership = RequireFingerprint("REV869B_OWNERSHIP_LEASE_FINGERPRINT");
        Guid commandId;
        Guid attemptId;
        await using (var audit = new NpgsqlConnection(auditBuilder.ConnectionString))
        {
            await audit.OpenAsync();
            await using var register = new NpgsqlCommand(
                "SELECT nexa.rev869b_register_command_request(@organization,@operation,@key,@request,@actor,@issuer,@subject,@role)", audit);
            register.Parameters.AddWithValue("organization", Organization); register.Parameters.AddWithValue("operation", operation);
            register.Parameters.AddWithValue("key", keySha); register.Parameters.AddWithValue("request", requestSha);
            register.Parameters.AddWithValue("actor", actorEmployeeId); register.Parameters.AddWithValue("issuer", Issuer);
            register.Parameters.AddWithValue("subject", Login); register.Parameters.AddWithValue("role", role);
            commandId = (Guid)(await register.ExecuteScalarAsync() ?? throw new InvalidOperationException("Command request was not registered."));

            await using var start = new NpgsqlCommand(
                "SELECT nexa.rev869b_start_command_attempt(@command,@execution,@service,@ownership,@runtime,@backend,@transaction)", audit);
            start.Parameters.AddWithValue("command", commandId); start.Parameters.AddWithValue("execution", execution);
            start.Parameters.AddWithValue("service", service); start.Parameters.AddWithValue("ownership", ownership);
            start.Parameters.AddWithValue("runtime", "nexa_rev869b_app_runtime"); start.Parameters.AddWithValue("backend", backend);
            start.Parameters.AddWithValue("transaction", transactionId);
            attemptId = (Guid)(await start.ExecuteScalarAsync() ?? throw new InvalidOperationException("Committed command replay has no new attempt."));
        }

        await using var open = new NpgsqlCommand(
            "SELECT nexa.rev869b_open_command_attempt(@attempt,@actor,@issuer,@subject,@role,@organization,@slotsSha,@slots::jsonb)",
            connection, transaction);
        open.Parameters.AddWithValue("attempt", attemptId); open.Parameters.AddWithValue("actor", actorEmployeeId);
        open.Parameters.AddWithValue("issuer", Issuer); open.Parameters.AddWithValue("subject", Login);
        open.Parameters.AddWithValue("role", role); open.Parameters.AddWithValue("organization", Organization);
        open.Parameters.AddWithValue("slotsSha", requestSha); open.Parameters.AddWithValue("slots", serialized);
        await open.ExecuteScalarAsync();
        return attemptId;
    }

    private static Guid RequireGuid(string name) =>
        Guid.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value != Guid.Empty
            ? value : throw new InvalidOperationException(name + " must be an exact UUID.");

    private static byte[] RequireFingerprint(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (value is null || value.Length != 64 || value.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidOperationException(name + " must be an exact SHA-256 fingerprint.");
        return Convert.FromHexString(value);
    }

    internal sealed record ExactSlot(string ClaimKind, Guid HistoryId, string EntityType, Guid EntityId,
        string Operation, long ParentVersion, string? FromStatus, string ToStatus, string Correlation, string Remarks);

    public ValueTask DisposeAsync() => lease.DisposeAsync();
}
