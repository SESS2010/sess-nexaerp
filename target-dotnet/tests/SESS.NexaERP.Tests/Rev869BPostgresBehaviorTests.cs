using System.Data;
using Npgsql;

namespace SESS.NexaERP.Tests;

// Compiled by source gates, but intentionally executed only during the separately authorized
// isolated REV869B database verification. Every entry point calls OpenVerifiedAsync first.
public sealed class Rev869BPostgresBehaviorTests
{
    private const string ExactDatabase = "sess_nexaerp_rev869b_verify";
    private const string ExactOptIn = "ISOLATED_REV869B_BEHAVIOR_TESTS";
    private const string MigrationId = "20260811025827_Rev869BRfqQuotationComparisonPurchaseOrderFoundation";

    [Fact]
    public async Task SuccessfulTransactionPersistsAndCanBeVerified()
    {
        await using var connection = await OpenVerifiedAsync();
        var id = Guid.NewGuid(); var correlation = $"REV869B-PG-SUCCESS-{id:N}";
        await InsertAuditAsync(connection, id, correlation, "Success");
        Assert.Equal(1L, await ScalarAsync(connection, "SELECT count(*) FROM nexa.audit_logs WHERE \"Id\"=@id AND \"CorrelationId\"=@correlation", ("id", id), ("correlation", correlation)));
        await ExecuteAsync(connection, "DELETE FROM nexa.audit_logs WHERE \"Id\"=@id", ("id", id));
    }

    [Fact]
    public async Task FailedTransactionRollsBackWithBeforeAfterEquality()
    {
        await using var connection = await OpenVerifiedAsync();
        var (id, before) = await DraftRfqAsync(connection);
        await using (var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            Assert.Equal(1, await ExecuteAsync(connection, "UPDATE nexa.request_for_quotations SET \"Version\"=\"Version\"+1 WHERE \"Id\"=@id AND \"Version\"=@version", transaction, ("id", id), ("version", before)));
            await transaction.RollbackAsync();
        }
        Assert.Equal(before, await ScalarAsync(connection, "SELECT \"Version\" FROM nexa.request_for_quotations WHERE \"Id\"=@id", ("id", id)));
    }

    [Fact]
    public async Task TwoIndependentConnectionsHaveExactlyOneWinnerAndRejectStaleWriter()
    {
        await using var first = await OpenVerifiedAsync();
        await using var second = await OpenVerifiedAsync();
        var (id, expected) = await DraftRfqAsync(first);
        await using var firstTx = await first.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var secondTx = await second.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var sql = "UPDATE nexa.request_for_quotations SET \"Version\"=\"Version\"+1 WHERE \"Id\"=@id AND \"Version\"=@version";
        var winner = await ExecuteAsync(first, sql, firstTx, ("id", id), ("version", expected));
        await firstTx.CommitAsync();
        var stale = await ExecuteAsync(second, sql, secondTx, ("id", id), ("version", expected));
        await secondTx.RollbackAsync();
        Assert.Equal(1, winner);
        Assert.Equal(0, stale);
        Assert.Equal(expected + 1, await ScalarAsync(first, "SELECT \"Version\" FROM nexa.request_for_quotations WHERE \"Id\"=@id", ("id", id)));
    }

    [Fact]
    public async Task IdempotentReplayReturnsOriginalRowWithoutDuplicate()
    {
        await using var connection = await OpenVerifiedAsync();
        var id = Guid.NewGuid(); var key = $"rev869b-pg-idempotency-{id:N}";
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        var inserted = await ExecuteAsync(connection, """
            INSERT INTO nexa.request_for_quotations
            SELECT (jsonb_populate_record(NULL::nexa.request_for_quotations,
                to_jsonb(r) || jsonb_build_object('Id',@id,'RfqNumber',@number,'IdempotencyKey',@key,'Version',0))).*
            FROM nexa.request_for_quotations r WHERE r."Status"='Draft' LIMIT 1
            """, transaction, ("id", id), ("number", $"REV869B-PG-IDEMP-{id:N}"), ("key", key));
        Assert.Equal(1, inserted);
        var original = await ScalarGuidAsync(connection, "SELECT \"Id\" FROM nexa.request_for_quotations WHERE \"OrganizationId\"=(SELECT \"OrganizationId\" FROM nexa.request_for_quotations WHERE \"Id\"=@id) AND \"IdempotencyKey\"=@key", transaction, ("id", id), ("key", key));
        var replay = await ScalarGuidAsync(connection, "SELECT \"Id\" FROM nexa.request_for_quotations WHERE \"IdempotencyKey\"=@key", transaction, ("key", key));
        Assert.Equal(id, original); Assert.Equal(original, replay);
        Assert.Equal(1L, await ScalarAsync(connection, "SELECT count(*) FROM nexa.request_for_quotations WHERE \"IdempotencyKey\"=@key", transaction, ("key", key)));
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task ConcurrentIdempotencyCollisionHasOneWinnerAndReturnsOriginal()
    {
        await using var first = await OpenVerifiedAsync();
        await using var second = await OpenVerifiedAsync();
        var winnerId = Guid.NewGuid(); var loserId = Guid.NewGuid(); var key = $"rev869b-pg-race-{winnerId:N}";
        const string sql = """
            INSERT INTO nexa.request_for_quotations
            SELECT (jsonb_populate_record(NULL::nexa.request_for_quotations,
                to_jsonb(r) || jsonb_build_object('Id',@id,'RfqNumber',@number,'IdempotencyKey',@key,'Version',0))).*
            FROM nexa.request_for_quotations r WHERE r."Status"='Draft' LIMIT 1
            """;
        await using var firstTx = await first.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var secondTx = await second.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        Assert.Equal(1, await ExecuteAsync(first, sql, firstTx, ("id", winnerId), ("number", $"REV869B-PG-RACE-{winnerId:N}"), ("key", key)));
        var loserAttempt = ExecuteAsync(second, sql, secondTx, ("id", loserId), ("number", $"REV869B-PG-RACE-{loserId:N}"), ("key", key));
        await firstTx.CommitAsync();
        await Assert.ThrowsAsync<PostgresException>(() => loserAttempt);
        await secondTx.RollbackAsync();
        Assert.Equal(1L, await ScalarAsync(first, "SELECT count(*) FROM nexa.request_for_quotations WHERE \"IdempotencyKey\"=@key", ("key", key)));
        Assert.Equal(winnerId, await ScalarGuidAsync(first, "SELECT \"Id\" FROM nexa.request_for_quotations WHERE \"IdempotencyKey\"=@key", null, ("key", key)));
        await ExecuteAsync(first, "DELETE FROM nexa.request_for_quotations WHERE \"Id\"=@id", ("id", winnerId));
    }

    [Fact]
    public async Task DirectTerminalStateInsertIsRejected()
    {
        await using var connection = await OpenVerifiedAsync();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        var id = Guid.NewGuid();
        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            INSERT INTO nexa.request_for_quotations
            SELECT (jsonb_populate_record(NULL::nexa.request_for_quotations,
                to_jsonb(r) || jsonb_build_object('Id',@id,'RfqNumber',@number,'IdempotencyKey',@key,'Status','Closed','Version',0))).*
            FROM nexa.request_for_quotations r LIMIT 1
            """, transaction, ("id", id), ("number", $"REV869B-PG-TERMINAL-{id:N}"), ("key", $"terminal-{id:N}")));
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task SnapshotMismatchIsRejectedOnIssue()
    {
        await using var connection = await OpenVerifiedAsync();
        var row = await ApprovedPoAsync(connection);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection,
            "UPDATE nexa.purchase_orders SET \"Version\"=\"Version\"+1,\"Status\"='Issued',\"TotalPayableValue\"=\"TotalPayableValue\"+0.000001 WHERE \"Id\"=@id AND \"Version\"=@version",
            transaction, ("id", row.Id), ("version", row.Version)));
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task PermissionDenialPersistsAuditEvidence()
    {
        await using var connection = await OpenVerifiedAsync();
        var denied = await ScalarAsync(connection, """
            SELECT count(*) FROM nexa.role_page_permissions permission
            JOIN nexa.roles role ON role."Id"=permission."RoleId"
            JOIN nexa.page_definitions page ON page."Id"=permission."PageDefinitionId"
            WHERE upper(trim(role."Code"))='MANAGING_DIRECTOR' AND page."PageKey"='purchase.po'
              AND permission."CanIssue"=FALSE
            """);
        Assert.Equal(1L, denied);
        var id = Guid.NewGuid(); var correlation = $"REV869B-PG-DENIED-{id:N}";
        await InsertAuditAsync(connection, id, correlation, "Failure");
        Assert.Equal(1L, await ScalarAsync(connection, "SELECT count(*) FROM nexa.audit_logs WHERE \"Id\"=@id AND \"Action\"='Denied' AND \"Result\"='Failure'", ("id", id)));
        await ExecuteAsync(connection, "DELETE FROM nexa.audit_logs WHERE \"Id\"=@id", ("id", id));
    }

    [Fact]
    public async Task AuditFailureCausesProtectedOperationToFailAndRollback()
    {
        await using var connection = await OpenVerifiedAsync();
        var (id, before) = await DraftRfqAsync(connection);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        Assert.Equal(1, await ExecuteAsync(connection, "UPDATE nexa.request_for_quotations SET \"Version\"=\"Version\"+1 WHERE \"Id\"=@id AND \"Version\"=@version", transaction, ("id", id), ("version", before)));
        await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection,
            "INSERT INTO nexa.audit_logs (\"Id\",\"Module\",\"Action\",\"EntityName\",\"EntityId\",\"UserLoginId\",\"CreatedAt\",\"CreatedBy\",\"Version\",\"CorrelationId\",\"Result\") VALUES (NULL,'Purchase','ProtectedOperation','RFQ',@entity,'rev869b-pg-test',now(),'rev869b-pg-test',0,'REV869B-PG-AUDIT-FAIL','Success')",
            transaction, ("entity", id.ToString())));
        await transaction.RollbackAsync();
        Assert.Equal(before, await ScalarAsync(connection, "SELECT \"Version\" FROM nexa.request_for_quotations WHERE \"Id\"=@id", ("id", id)));
    }

    [Fact]
    public async Task SkippedAndLowerVersionsAreRejected()
    {
        await using var connection = await OpenVerifiedAsync();
        var (id, version) = await DraftRfqAsync(connection);
        foreach (var proposed in new[] { version - 1, version + 2 })
        {
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection,
                "UPDATE nexa.request_for_quotations SET \"Version\"=@proposed WHERE \"Id\"=@id AND \"Version\"=@version",
                transaction, ("proposed", proposed), ("id", id), ("version", version)));
            await transaction.RollbackAsync();
        }
    }

    private static async Task<NpgsqlConnection> OpenVerifiedAsync()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("REV869B_POSTGRES_OPT_IN"), ExactOptIn, StringComparison.Ordinal))
            throw new InvalidOperationException($"Set REV869B_POSTGRES_OPT_IN={ExactOptIn} explicitly.");
        var raw = Environment.GetEnvironmentVariable("REV869B_POSTGRES")
            ?? throw new InvalidOperationException("REV869B_POSTGRES is required; no fallback is permitted.");
        var builder = new NpgsqlConnectionStringBuilder(raw);
        if (!string.Equals(builder.Database, ExactDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException($"Only the exact isolated database {ExactDatabase} is permitted.");
        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        var actual = Convert.ToString(await new NpgsqlCommand("SELECT current_database()", connection).ExecuteScalarAsync());
        if (!string.Equals(actual, ExactDatabase, StringComparison.Ordinal))
        {
            await connection.DisposeAsync();
            throw new InvalidOperationException("Connected PostgreSQL database identity did not match the exact isolated REV869B database.");
        }
        var migration = Convert.ToInt64(await new NpgsqlCommand($"SELECT count(*) FROM nexa.\"__EFMigrationsHistory\" WHERE \"MigrationId\"='{MigrationId}'", connection).ExecuteScalarAsync());
        if (migration != 1)
        {
            await connection.DisposeAsync();
            throw new InvalidOperationException("The exact REV869B migration is not installed once in the isolated database.");
        }
        return connection;
    }

    private static async Task<(Guid Id, long Version)> DraftRfqAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("SELECT \"Id\",\"Version\" FROM nexa.request_for_quotations WHERE \"Status\"='Draft' ORDER BY \"Id\" LIMIT 1", connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("The isolated REV869B fixture requires a Draft RFQ.");
        return (reader.GetGuid(0), reader.GetInt64(1));
    }

    private static async Task<(Guid Id, long Version)> ApprovedPoAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("SELECT \"Id\",\"Version\" FROM nexa.purchase_orders WHERE \"Status\"='Approved' AND \"IsCurrentVersion\" ORDER BY \"Id\" LIMIT 1", connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("The isolated REV869B fixture requires an approved current PO.");
        return (reader.GetGuid(0), reader.GetInt64(1));
    }

    private static Task InsertAuditAsync(NpgsqlConnection connection, Guid id, string correlation, string result) =>
        ExecuteAsync(connection, """
            INSERT INTO nexa.audit_logs
            ("Id","Module","Action","EntityName","EntityId","UserLoginId","AfterJson","CreatedAt","CreatedBy","Version","CorrelationId","Result")
            VALUES (@id,'Security','Denied','purchase.po',@entity,'rev869b-pg-test','{"denied":true}',now(),'rev869b-pg-test',0,@correlation,@result)
            """, ("id", id), ("entity", id.ToString()), ("correlation", correlation), ("result", result));

    private static Task<int> ExecuteAsync(NpgsqlConnection connection, string sql, params (string Name, object Value)[] parameters) =>
        ExecuteAsync(connection, sql, null, parameters);
    private static async Task<int> ExecuteAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction, params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return await command.ExecuteNonQueryAsync();
    }

    private static Task<long> ScalarAsync(NpgsqlConnection connection, string sql, params (string Name, object Value)[] parameters) =>
        ScalarAsync(connection, sql, null, parameters);
    private static async Task<long> ScalarAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction, params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task<Guid> ScalarGuidAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction, params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return (Guid)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Expected one isolated fixture row."));
    }
}
