using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace SESS.NexaERP.Tests;

internal sealed class Rev869BOwnedPostgresDatabase : IAsyncDisposable
{
    public const string Organization = "REV869B-PG-SELF-OWNED-GRAPH";
    public const string Login = "REV869B-PG-DIRECT-ACTOR";
    public const string Issuer = "REV869B-TEST";
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
            await Rev869BCompleteGraphSeeder.SeedAsync(owned.ConnectionString, scenario);
            return owned;
        }
        catch
        {
            await owned.DisposeAsync();
            throw;
        }
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        return await lease.OpenVerifiedConnectionAsync();
    }

    public static async Task SetCommandContextAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid actorEmployeeId, string role)
    {
        var authenticatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce = Guid.NewGuid();
        var transactionId = Convert.ToInt64(await new NpgsqlCommand("SELECT txid_current()", connection, transaction).ExecuteScalarAsync());
        var signingKeyHex = Environment.GetEnvironmentVariable("REV869B_COMMAND_SIGNING_KEY");
        if (signingKeyHex is null || signingKeyHex.Length != 64 || signingKeyHex.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidOperationException("Owned command signing key is missing.");
        var canonical = string.Join('|', actorEmployeeId.ToString("N"), Issuer, Login, role, Organization,
            authenticatedAt.ToString(CultureInfo.InvariantCulture), nonce.ToString("N"),
            transactionId.ToString(CultureInfo.InvariantCulture));
        var key = Convert.FromHexString(signingKeyHex);
        var signature = Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(canonical)));
        var command = new NpgsqlCommand(
            "SELECT nexa.rev869b_open_command_context(@employee,@issuer,@subject,@role,@organization,@authenticatedAt,@nonce,@transactionId,@signature,@signingKey)",
            connection, transaction);
        command.Parameters.AddWithValue("employee", actorEmployeeId);
        command.Parameters.AddWithValue("issuer", Issuer);
        command.Parameters.AddWithValue("subject", Login);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("organization", Organization);
        command.Parameters.AddWithValue("authenticatedAt", authenticatedAt);
        command.Parameters.AddWithValue("nonce", nonce);
        command.Parameters.AddWithValue("transactionId", transactionId);
        command.Parameters.AddWithValue("signature", signature);
        command.Parameters.AddWithValue("signingKey", signingKeyHex);
        await command.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync() => lease.DisposeAsync();
}
