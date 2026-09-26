using Npgsql;

namespace SESS.NexaERP.Tests;

/// <summary>
/// The real RFQ failure of 23 September, reproduced from its actual cause rather than a
/// synthetic one. The client sent QuoteDueAt as 2026-09-30T18:00:00+05:30. Npgsql accepts
/// only offset 0 for timestamptz, so the write failed inside the command's transaction, and
/// the disposal rollback that followed replaced the real message with the name of the
/// disposed object - which is why the operator was shown "NpgsqlTransaction".
///
/// This pins what Npgsql actually throws and what survives, so the fix has a real-world
/// proof and not an invented one.
/// </summary>
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void NonUtcOffsetFailsWithItsOwnMessageAndTheRollbackThatFollowsIsNotTheCause()
    {
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        using var connection = new NpgsqlConnection(server.ConnectionString);
        connection.Open();
        using (var create = new NpgsqlCommand("CREATE TABLE quote (\"QuoteDueAt\" timestamptz NOT NULL)", connection))
            create.ExecuteNonQuery();

        var transaction = connection.BeginTransaction();
        var local = new DateTimeOffset(2026, 9, 30, 18, 0, 0, TimeSpan.FromHours(5.5));

        // 1. The real cause, with its own message.
        var cause = Assert.ThrowsAny<Exception>(() =>
        {
            using var insert = new NpgsqlCommand("INSERT INTO quote(\"QuoteDueAt\") VALUES(@at)", connection, transaction);
            insert.Parameters.AddWithValue("at", local);
            insert.ExecuteNonQuery();
        });
        Assert.Contains("offset", cause.Message, StringComparison.OrdinalIgnoreCase);

        Assert.IsType<ArgumentException>(cause);
        Assert.Contains("only offset 0 (UTC) is supported", cause.Message, StringComparison.Ordinal);

        // 2. Npgsql has already completed the transaction, so nothing further can run in it.
        //    This is why the failure is not confined to the one bad parameter.
        var utcAttempt = Record.Exception(() =>
        {
            using var ok = new NpgsqlCommand("INSERT INTO quote(\"QuoteDueAt\") VALUES(@at)", connection, transaction);
            ok.Parameters.AddWithValue("at", local.ToUniversalTime());
            ok.ExecuteNonQuery();
        });
        Assert.IsType<InvalidOperationException>(utcAttempt);

        // 3. The rollback that disposal performs next throws ObjectDisposedException whose
        //    MESSAGE IS THE BARE STRING "NpgsqlTransaction" - not the usual .NET wording of
        //    "Cannot access a disposed object. Object name: '...'", because Npgsql passes the
        //    type name as the message and leaves ObjectName empty. That bare string is exactly
        //    what the operator was shown as a validation Detail on 23 September, in place of
        //    the offset message above. Pinning it here keeps the real-world explanation
        //    attached to the code rather than to a chat log.
        var rollback = Assert.Throws<ObjectDisposedException>(transaction.Rollback);
        Assert.Equal("NpgsqlTransaction", rollback.Message);
        Assert.True(string.IsNullOrEmpty(rollback.ObjectName));
    }
}
