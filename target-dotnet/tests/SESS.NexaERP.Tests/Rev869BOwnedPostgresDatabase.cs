using Npgsql;

namespace SESS.NexaERP.Tests;

internal sealed class Rev869BOwnedPostgresDatabase : IAsyncDisposable
{
    public const string Organization = "REV869B-PG-SELF-OWNED-GRAPH";
    public const string Login = "REV869B-PG-DIRECT-ACTOR";
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
        var command = new NpgsqlCommand("SELECT nexa.rev869b_open_command_context(@employee,@login,@role,@organization)", connection, transaction);
        command.Parameters.AddWithValue("employee", actorEmployeeId);
        command.Parameters.AddWithValue("login", Login);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("organization", Organization);
        await command.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync() => lease.DisposeAsync();
}
