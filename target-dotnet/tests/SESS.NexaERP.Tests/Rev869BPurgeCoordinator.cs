
using Npgsql;

namespace SESS.NexaERP.Tests;

/// <summary>
/// Future-execution coordinator. Every phase uses a fresh, non-pooled autocommit connection so a
/// caller rollback/savepoint cannot restore an approval or erase Started/Rejected/terminal evidence.
/// Correction 16 never executes this code.
/// </summary>
internal static class Rev869BPurgeCoordinator
{
    internal sealed record PhaseResult(Guid ExecutionId, int Value, string Phase);

    internal static async Task<NpgsqlConnection> OpenExactRoleAsync(
        string environmentName, string expectedRole, string databaseName)
    {
        if (!databaseName.StartsWith(Rev869BTestDatabaseLease.DatabasePrefix, StringComparison.Ordinal) ||
            databaseName.Length != Rev869BTestDatabaseLease.DatabasePrefix.Length + 24 ||
            databaseName[Rev869BTestDatabaseLease.DatabasePrefix.Length..].Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidOperationException("An exact high-entropy owned REV869B database name is required.");
        var raw = Environment.GetEnvironmentVariable(environmentName)
            ?? throw new InvalidOperationException($"{environmentName} is required for future authorized execution.");
        var builder = new NpgsqlConnectionStringBuilder(raw) { Pooling = false };
        if (!string.Equals(builder.Database, databaseName, StringComparison.Ordinal) ||
            !string.Equals(builder.Username, expectedRole, StringComparison.Ordinal) ||
            string.Equals(builder.Database, Rev869BTestDatabaseLease.ExactSourceDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException("The exact owned database and least-privilege purge principal are required.");
        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var identity = new NpgsqlCommand(
            "SELECT count(*) FROM pg_database WHERE datname=current_database() AND current_database()=@database AND session_user=@role", connection);
        identity.Parameters.AddWithValue("database", databaseName);
        identity.Parameters.AddWithValue("role", expectedRole);
        if (Convert.ToInt64(await identity.ExecuteScalarAsync()) != 1)
        {
            await connection.DisposeAsync();
            throw new InvalidOperationException("Purge principal/database identity proof failed.");
        }
        return connection;
    }

    internal static async Task<PhaseResult> BeginAsync(
        string databaseName, Guid executionId, byte[] nonceFingerprint)
    {
        await using var executor = await OpenExactRoleAsync(
            "REV869B_PURGE_EXECUTOR_CONNECTION", "nexa_rev869b_purge_executor", databaseName);
        if (executor.FullState != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("A fresh autocommit executor connection is required.");
        await using var command = new NpgsqlCommand(
            "SELECT nexa.rev869b_begin_purge_execution(@execution,@nonce)", executor);
        command.Parameters.AddWithValue("execution", executionId);
        command.Parameters.AddWithValue("nonce", nonceFingerprint);
        var value = Convert.ToInt32(await command.ExecuteScalarAsync());
        // -1 is a committed Rejected result; 0 is a committed ZeroRows result; positive is Started.
        return new PhaseResult(executionId, value, value < 0 ? "Rejected" : value == 0 ? "ZeroRows" : "Started");
    }

    internal static async Task<PhaseResult> ExecuteAsync(string databaseName, Guid executionId)
    {
        await using var executor = await OpenExactRoleAsync(
            "REV869B_PURGE_EXECUTOR_CONNECTION", "nexa_rev869b_purge_executor", databaseName);
        await using var command = new NpgsqlCommand(
            "SELECT nexa.rev869b_purge_temporary_security_ledger(@execution)", executor);
        command.Parameters.AddWithValue("execution", executionId);
        var value = Convert.ToInt32(await command.ExecuteScalarAsync());
        // The function catches the destructive subtransaction. Autocommit then durably commits either
        // Succeeded or Failed/PartialFailure evidence before this connection is disposed.
        return new PhaseResult(executionId, value, value < 0 ? "FailedOrPartialFailure" : "Succeeded");
    }
}
