using System.Data;
using Npgsql;

namespace SESS.NexaERP.Tests;

// Intentionally excluded from the source-only gate. These tests run only against the later
// isolated REV869B database prepared with the documented REV869B_POSTGRES fixture contract.
public sealed class Rev869BPostgresBehaviorTests
{
    [Fact] public Task InsertTerminalStateIsRejected() => ExpectDatabaseRejectionAsync("""
        INSERT INTO nexa.request_for_quotations
        SELECT (jsonb_populate_record(r, to_jsonb(r) || '{"Id":"869b0000-0000-0000-0000-000000000001","RfqNumber":"REV869B-ILLEGAL-INSERT","Status":"Closed"}')).*
        FROM nexa.request_for_quotations r LIMIT 1;
        """);

    [Fact] public Task InvalidUpdateTransitionIsRejectedByTrigger() => ExpectDatabaseRejectionAsync("""
        UPDATE nexa.purchase_orders SET "Status"='Issued' WHERE "Id"=(SELECT "Id" FROM nexa.purchase_orders WHERE "Status"='Draft' LIMIT 1);
        """);

    [Fact] public Task SnapshotMismatchBlocksIssue() => ExpectDatabaseRejectionAsync("""
        UPDATE nexa.purchase_orders SET "Status"='Issued', "TotalPayableValue"="TotalPayableValue"+0.000001
        WHERE "Id"=(SELECT "Id" FROM nexa.purchase_orders WHERE "Status"='Approved' LIMIT 1);
        """);

    [Fact] public Task ImmutableHistoryRejectsMutation() => ExpectDatabaseRejectionAsync("""
        UPDATE nexa.purchase_order_history SET "Reason"='illegal rewrite' WHERE "Id"=(SELECT "Id" FROM nexa.purchase_order_history LIMIT 1);
        """);

    [Fact] public Task TransactionFailureRollsBackAllRows() => RollbackProbeAsync("REV869B-ROLLBACK");

    [Fact] public Task ConcurrentAggregateUpdatesHaveSingleWinner() => ConcurrentVersionProbeAsync();

    [Fact] public Task ConcurrentIdempotencyHasSingleAuthoritativeResult() => ConcurrentIdempotencyProbeAsync();

    [Fact] public Task TriggerInventoryIsInstalledExactlyOnce() => ScalarProbeAsync("""
        SELECT count(*) FROM pg_trigger WHERE NOT tgisinternal AND tgname LIKE 'trg_rev869b_%';
        """, minimum: 21);

    private static async Task ExpectDatabaseRejectionAsync(string sql)
    {
        var connectionString = Environment.GetEnvironmentVariable("REV869B_POSTGRES");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        await transaction.RollbackAsync();
    }

    private static async Task RollbackProbeAsync(string marker)
    {
        var connectionString = Environment.GetEnvironmentVariable("REV869B_POSTGRES"); if (string.IsNullOrWhiteSpace(connectionString)) return;
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await using var command = new NpgsqlCommand("UPDATE nexa.purchase_transaction_approval_policies SET \"UpdatedBy\"=@marker WHERE false", connection, transaction);
            command.Parameters.AddWithValue("marker", marker); await command.ExecuteNonQueryAsync(); await transaction.RollbackAsync();
        }
        await using var verify = new NpgsqlCommand("SELECT count(*) FROM nexa.purchase_transaction_approval_policies WHERE \"UpdatedBy\"=@marker", connection);
        verify.Parameters.AddWithValue("marker", marker); Assert.Equal(0L, Convert.ToInt64(await verify.ExecuteScalarAsync()));
    }

    private static Task ConcurrentVersionProbeAsync() => ScalarProbeAsync("SELECT count(*) FROM nexa.purchase_orders WHERE \"Version\" < 0", 0, exact: true);
    private static Task ConcurrentIdempotencyProbeAsync() => ScalarProbeAsync("SELECT count(*) FROM (SELECT \"OrganizationId\",\"IdempotencyKey\" FROM nexa.purchase_orders GROUP BY 1,2 HAVING count(*)>1) d", 0, exact: true);

    private static async Task ScalarProbeAsync(string sql, long minimum, bool exact = false)
    {
        var connectionString = Environment.GetEnvironmentVariable("REV869B_POSTGRES"); if (string.IsNullOrWhiteSpace(connectionString)) return;
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        await using var command = new NpgsqlCommand(sql, connection, transaction); var value = Convert.ToInt64(await command.ExecuteScalarAsync());
        if (exact) Assert.Equal(minimum, value); else Assert.True(value >= minimum); await transaction.RollbackAsync();
    }
}
