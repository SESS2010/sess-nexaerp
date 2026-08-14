using Npgsql;

namespace SESS.NexaERP.Tests;

/// <summary>Future separately authorized client for the frozen target-local purge saga.</summary>
internal static class Rev869BPurgeCoordinator
{
    internal sealed record PurgeSnapshot(Guid PurgeAttemptId, string State, int CandidateCount,
        byte[] CandidateSha256, Guid AuthorizationId, Guid? PriorAttemptId);

    internal static Task<Guid> RegisterAsync(string databaseName, Guid authorizationId,
        Guid managementDecisionId, Guid? priorAttemptId, string scope, DateTimeOffset cutoff,
        int maximumRows, byte[] nonceSha256, DateTimeOffset expiresAt) =>
        ScalarAsync("REV869B_MANAGEMENT_WRITER_CONNECTION", "nexa_rev869b_management_writer", databaseName,
            "SELECT nexa.rev869b_register_purge_authorization(@id,@decision,@prior,@scope,@cutoff,@maximum,@nonce,@expires)",
            c => { c.Parameters.AddWithValue("id", authorizationId); c.Parameters.AddWithValue("decision", managementDecisionId);
                c.Parameters.AddWithValue("prior", (object?)priorAttemptId ?? DBNull.Value); c.Parameters.AddWithValue("scope", scope);
                c.Parameters.AddWithValue("cutoff", cutoff); c.Parameters.AddWithValue("maximum", maximumRows);
                c.Parameters.AddWithValue("nonce", nonceSha256); c.Parameters.AddWithValue("expires", expiresAt); });

    internal static Task<Guid> StartAsync(string databaseName, Guid authorizationId, Guid attemptId) =>
        ScalarAsync("REV869B_PURGE_WORKER_CONNECTION", "nexa_rev869b_purge_worker", databaseName,
            "SELECT nexa.rev869b_start_purge(@authorization,@attempt)",
            c => { c.Parameters.AddWithValue("authorization", authorizationId); c.Parameters.AddWithValue("attempt", attemptId); });

    internal static Task<int> ExecuteAsync(string databaseName, Guid attemptId) =>
        ScalarAsync<int>("REV869B_PURGE_WORKER_CONNECTION", "nexa_rev869b_purge_worker", databaseName,
            "SELECT nexa.rev869b_execute_purge(@attempt)", c => c.Parameters.AddWithValue("attempt", attemptId));

    internal static Task<Guid> RecordFailureAsync(string databaseName, Guid attemptId,
        string terminalState, string minimizedCategory, byte[] evidenceSha256) =>
        ScalarAsync("REV869B_PURGE_WORKER_CONNECTION", "nexa_rev869b_purge_worker", databaseName,
            "SELECT nexa.rev869b_record_purge_failure(@attempt,@state,@category,@evidence)",
            c => { c.Parameters.AddWithValue("attempt", attemptId); c.Parameters.AddWithValue("state", terminalState);
                c.Parameters.AddWithValue("category", minimizedCategory); c.Parameters.AddWithValue("evidence", evidenceSha256); });

    internal static async Task<string> ReconcileAsync(string databaseName, Guid attemptId)
    {
        await using var connection = await OpenAsync("REV869B_PURGE_WORKER_CONNECTION",
            "nexa_rev869b_purge_worker", databaseName);
        await using var command = new NpgsqlCommand("SELECT nexa.rev869b_reconcile_purge(@attempt)::text", connection);
        command.Parameters.AddWithValue("attempt", attemptId);
        return Convert.ToString(await command.ExecuteScalarAsync())
            ?? throw new InvalidOperationException("Authoritative purge reconciliation was absent.");
    }

    private static Task<Guid> ScalarAsync(string environment, string role, string database,
        string sql, Action<NpgsqlCommand> bind) => ScalarAsync<Guid>(environment, role, database, sql, bind);

    private static async Task<T> ScalarAsync<T>(string environment, string role, string database,
        string sql, Action<NpgsqlCommand> bind)
    {
        await using var connection = await OpenAsync(environment, role, database);
        await using var command = new NpgsqlCommand(sql, connection);
        bind(command);
        var value = await command.ExecuteScalarAsync();
        return value is T typed ? typed : (T)Convert.ChangeType(value!, typeof(T));
    }

    private static async Task<NpgsqlConnection> OpenAsync(string environment, string role, string database)
    {
        var raw = Environment.GetEnvironmentVariable(environment)
            ?? throw new InvalidOperationException(environment + " is required for separately authorized execution.");
        var builder = new NpgsqlConnectionStringBuilder(raw) { Pooling = false };
        if (!string.Equals(builder.Database, database, StringComparison.Ordinal) ||
            !string.Equals(builder.Username, role, StringComparison.Ordinal) ||
            !database.StartsWith(Rev869BTestDatabaseLease.DatabasePrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Exact isolated target and frozen least-privilege role are required.");
        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
}
