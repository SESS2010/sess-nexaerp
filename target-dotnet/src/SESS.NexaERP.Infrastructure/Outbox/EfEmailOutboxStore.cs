using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Outbox;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Outbox;

/// <summary>
/// The e-mail outbox store (Claude, R1). Enqueue joins the caller's open transaction, so a PO issue and
/// its PO_ISSUED row commit or roll back together. Nothing here sends mail: the TD's worker does.
/// </summary>
public sealed class EfEmailOutboxStore(NexaErpDbContext db, ICurrentUser user) : IEmailOutboxStore
{
    private const string Columns = """
        "Id","CompanyId","EventType","SourceEntityType","SourceEntityId","IdempotencyKey","PayloadJson"::text,"ToAddresses","CcAddresses",
        "Subject","Status","Attempts","NextAttemptAt","LastError","CreatedAt","SentAt"
        """;

    private async Task<NpgsqlCommand> Command(string sql, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        return new NpgsqlCommand(sql, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
    }

    public Task<Guid> EnqueueAsync(EmailOutboxRequest request, CancellationToken ct) => EnqueueCoreAsync(db, request, ct);

    /// <summary>Also used by the PO-issue hook inside the purchase service's own transaction.</summary>
    internal static async Task<Guid> EnqueueCoreAsync(NexaErpDbContext db, EmailOutboxRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 200)
            throw new StoresValidationException("An e-mail needs an idempotency key of at most 200 characters.");
        var composed = request.Composition;
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO advance.email_outbox("CompanyId","EventType","SourceEntityType","SourceEntityId","IdempotencyKey","PayloadJson",
              "ToAddresses","CcAddresses","Subject","BodyHtml","BodyText","Status","ComposedAt","CreatedBy")
            VALUES(@company,@event,@sourceType,@sourceId,@key,@payload::jsonb,@to,@cc,@subject,@html,@text,@status,
              CASE WHEN @composed THEN clock_timestamp() END,@by)
            ON CONFLICT ("CompanyId","IdempotencyKey") DO NOTHING
            RETURNING "Id"
            """, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        command.Parameters.AddWithValue("company", request.CompanyId);
        command.Parameters.AddWithValue("event", request.EventType);
        command.Parameters.Add(new NpgsqlParameter("sourceType", NpgsqlDbType.Text) { Value = (object?)request.SourceEntityType ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("sourceId", NpgsqlDbType.Uuid) { Value = (object?)request.SourceEntityId ?? DBNull.Value });
        command.Parameters.AddWithValue("key", request.IdempotencyKey.Trim());
        command.Parameters.AddWithValue("payload", string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson);
        command.Parameters.Add(new NpgsqlParameter("to", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = (composed?.To ?? []).ToArray() });
        command.Parameters.Add(new NpgsqlParameter("cc", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = (composed?.Cc ?? []).ToArray() });
        command.Parameters.Add(new NpgsqlParameter("subject", NpgsqlDbType.Text) { Value = (object?)composed?.Subject ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("html", NpgsqlDbType.Text) { Value = (object?)composed?.BodyHtml ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("text", NpgsqlDbType.Text) { Value = (object?)composed?.BodyText ?? DBNull.Value });
        command.Parameters.AddWithValue("status", composed is null ? EmailStatuses.PendingCompose : EmailStatuses.Queued);
        command.Parameters.AddWithValue("composed", composed is not null);
        command.Parameters.AddWithValue("by", request.CreatedBy);
        if (await command.ExecuteScalarAsync(ct) is Guid id) return id;
        await using var existing = new NpgsqlCommand("""SELECT "Id" FROM advance.email_outbox WHERE "CompanyId"=@company AND "IdempotencyKey"=@key""",
            connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        existing.Parameters.AddWithValue("company", request.CompanyId);
        existing.Parameters.AddWithValue("key", request.IdempotencyKey.Trim());
        return (Guid)(await existing.ExecuteScalarAsync(ct))!;
    }

    public async Task<IReadOnlyList<EmailOutboxItem>> ClaimAsync(string status, int max, CancellationToken ct)
    {
        if (status is not (EmailStatuses.PendingCompose or EmailStatuses.Queued or EmailStatuses.Failed))
            throw new ArgumentOutOfRangeException(nameof(status), "Only PENDING_COMPOSE, QUEUED and FAILED rows are claimed.");
        await using var command = await Command($"""
            UPDATE advance.email_outbox o SET
              "Status" = CASE WHEN @status IN ('QUEUED','FAILED') THEN 'SENDING' ELSE o."Status" END,
              "NextAttemptAt" = clock_timestamp() + interval '5 minutes'
            WHERE o."Id" IN (SELECT "Id" FROM advance.email_outbox WHERE "Status"=@status
                             AND ("NextAttemptAt" IS NULL OR "NextAttemptAt" <= clock_timestamp())
                             ORDER BY "CreatedAt" LIMIT @max FOR UPDATE SKIP LOCKED)
            RETURNING {Columns}
            """, ct);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("max", Math.Clamp(max, 1, 100));
        return await ReadAsync(command, ct);
    }

    public Task ComposeAsync(Guid id, EmailComposition composition, CancellationToken ct)
    {
        if (composition.To.Count == 0) throw new StoresValidationException("A composed e-mail needs at least one recipient.");
        return Transition(id, [EmailStatuses.PendingCompose], """
            "Status"='QUEUED',"ToAddresses"=@to,"CcAddresses"=@cc,"Subject"=@subject,"BodyHtml"=@html,"BodyText"=@text,
            "ComposedAt"=clock_timestamp(),"NextAttemptAt"=NULL
            """, c =>
        {
            c.Parameters.Add(new NpgsqlParameter("to", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = composition.To.ToArray() });
            c.Parameters.Add(new NpgsqlParameter("cc", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = composition.Cc.ToArray() });
            c.Parameters.AddWithValue("subject", composition.Subject);
            c.Parameters.AddWithValue("html", composition.BodyHtml);
            c.Parameters.AddWithValue("text", composition.BodyText);
        }, ct);
    }

    public Task SkipAsync(Guid id, string reason, CancellationToken ct) =>
        Transition(id, [EmailStatuses.PendingCompose], "\"Status\"='SKIPPED',\"LastError\"=left(@reason,2000),\"NextAttemptAt\"=NULL",
            c => c.Parameters.AddWithValue("reason", reason), ct);

    public Task MarkSentAsync(Guid id, string? providerMessageId, CancellationToken ct) =>
        Transition(id, [EmailStatuses.Sending], "\"Status\"='SENT',\"SentAt\"=clock_timestamp(),\"Attempts\"=\"Attempts\"+1,\"ProviderMessageId\"=@provider,\"NextAttemptAt\"=NULL",
            c => c.Parameters.Add(new NpgsqlParameter("provider", NpgsqlDbType.Text) { Value = (object?)providerMessageId ?? DBNull.Value }), ct);

    public Task MarkFailedAsync(Guid id, string error, bool dead, CancellationToken ct) =>
        Transition(id, [EmailStatuses.Sending], $"""
            "Attempts"="Attempts"+1, "LastError"=left(@error,2000),
            "Status"=CASE WHEN @dead OR "Attempts"+1 >= {EmailStatuses.MaxAttempts} THEN 'DEAD' ELSE 'FAILED' END,
            "NextAttemptAt"=CASE WHEN @dead OR "Attempts"+1 >= {EmailStatuses.MaxAttempts} THEN NULL
              ELSE clock_timestamp() + (CASE "Attempts"+1 WHEN 1 THEN interval '1 minute' WHEN 2 THEN interval '5 minutes'
                                                          WHEN 3 THEN interval '15 minutes' ELSE interval '60 minutes' END) END
            """, c => { c.Parameters.AddWithValue("error", error); c.Parameters.AddWithValue("dead", dead); }, ct);

    public Task MarkBlockedAsync(Guid id, string reason, CancellationToken ct) =>
        Transition(id, [EmailStatuses.Sending], "\"Status\"='BLOCKED_ALLOWLIST',\"LastError\"=left(@reason,2000),\"NextAttemptAt\"=NULL",
            c => c.Parameters.AddWithValue("reason", reason), ct);

    public Task RetryAsync(Guid id, CancellationToken ct) =>
        Transition(id, [EmailStatuses.Failed, EmailStatuses.Dead], "\"Status\"='QUEUED',\"NextAttemptAt\"=NULL", _ => { }, ct);

    private async Task Transition(Guid id, string[] from, string set, Action<NpgsqlCommand> parameters, CancellationToken ct)
    {
        await using var command = await Command($"UPDATE advance.email_outbox SET {set} WHERE \"Id\"=@id AND \"Status\" = ANY(@from)", ct);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.Add(new NpgsqlParameter("from", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = from });
        parameters(command);
        if (await command.ExecuteNonQueryAsync(ct) != 1)
            throw new StoresConflictException($"This e-mail is not in a state that allows the change (expected {string.Join(" or ", from)}).");
    }

    public async Task<EmailOutboxPage> ListAsync(string? status, int page, int pageSize, CancellationToken ct)
    {
        var organization = user.OrganizationId?.Trim().ToUpperInvariant() ?? throw new UnauthorizedAccessException("Select a company.");
        var company = await db.Companies.AsNoTracking().Where(c => c.Code == organization).Select(c => (Guid?)c.Id).SingleOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        await using var count = await Command("""SELECT count(*) FROM advance.email_outbox WHERE "CompanyId"=@company AND (@status::text IS NULL OR "Status"=@status)""", ct);
        count.Parameters.AddWithValue("company", company);
        count.Parameters.Add(new NpgsqlParameter("status", NpgsqlDbType.Text) { Value = (object?)status?.Trim().ToUpperInvariant() ?? DBNull.Value });
        var total = Convert.ToInt32(await count.ExecuteScalarAsync(ct));
        await using var command = await Command($"""
            SELECT {Columns} FROM advance.email_outbox WHERE "CompanyId"=@company AND (@status::text IS NULL OR "Status"=@status)
            ORDER BY "CreatedAt" DESC, "Id" LIMIT @size OFFSET @offset
            """, ct);
        command.Parameters.AddWithValue("company", company);
        command.Parameters.Add(new NpgsqlParameter("status", NpgsqlDbType.Text) { Value = (object?)status?.Trim().ToUpperInvariant() ?? DBNull.Value });
        command.Parameters.AddWithValue("size", pageSize);
        command.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        return new(total, page, pageSize, await ReadAsync(command, ct));
    }

    private static async Task<IReadOnlyList<EmailOutboxItem>> ReadAsync(NpgsqlCommand command, CancellationToken ct)
    {
        var rows = new List<EmailOutboxItem>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            string? Text(int i) => reader.IsDBNull(i) ? null : reader.GetString(i);
            DateTimeOffset? Time(int i) => reader.IsDBNull(i) ? null : reader.GetFieldValue<DateTimeOffset>(i);
            rows.Add(new EmailOutboxItem(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), Text(3),
                reader.IsDBNull(4) ? null : reader.GetGuid(4), reader.GetString(5), reader.GetString(6),
                reader.GetFieldValue<string[]>(7), reader.GetFieldValue<string[]>(8), Text(9), reader.GetString(10),
                reader.GetInt32(11), Time(12), Text(13), reader.GetFieldValue<DateTimeOffset>(14), Time(15)));
        }
        return rows;
    }
}
