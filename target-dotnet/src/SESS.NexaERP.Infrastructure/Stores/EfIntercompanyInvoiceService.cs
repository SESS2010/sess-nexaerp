using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed class EfIntercompanyInvoiceService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit)
    : IIntercompanyInvoiceService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string Organization => !string.IsNullOrWhiteSpace(user.OrganizationId)
        ? user.OrganizationId.Trim().ToUpperInvariant() : throw new UnauthorizedAccessException("Select a company.");

    private async Task<Guid> Company(CancellationToken ct) =>
        await db.Companies.Where(c => c.Code == Organization && c.IsActive && c.Status == "ACTIVE")
            .Select(c => (Guid?)c.Id).SingleOrDefaultAsync(ct)
        ?? throw new UnauthorizedAccessException("Selected company is unavailable.");

    private async Task<NpgsqlCommand> Command(string sql, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        return new(sql, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
    }

    private static string Required(string? value, string name, int maximum = 100)
    {
        var text = value?.Trim() ?? "";
        return text.Length > 0 && text.Length <= maximum && !text.Any(char.IsControl)
            ? text : throw new StoresValidationException($"{name} requires 1 to {maximum} characters without control characters.");
    }

    private void RequireRole(string action) => user.RequireRole(action, "ACCOUNTS_MANAGER");

    public async Task<IntercompanyInvoiceView> RecordAsync(RecordIntercompanyInvoiceRequest request, CancellationToken ct)
    {
        RequireRole("create");
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        var number = Required(request.InvoiceNumber, "InvoiceNumber");
        if (request.CorrelationId == Guid.Empty || request.InvoiceDate == default)
            throw new StoresValidationException("Published intercompany order and invoice date are required.");
        if (request.Evidence?.Content is not { Length: > 0 and <= 5242880 } content)
            throw new StoresValidationException("Retained GST invoice evidence of at most 5 MB is required.");
        var filename = Required((request.Evidence.FileName ?? "").Replace('\\', '/').Split('/').Last(), "FileName", 255);
        var mime = content.AsSpan().StartsWith("%PDF-"u8) ? "application/pdf" :
            content.AsSpan().StartsWith(new byte[] { 0xff, 0xd8, 0xff }) ? "image/jpeg" :
            content.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }) ? "image/png" :
            throw new StoresValidationException("GST invoice evidence must contain PDF, JPEG or PNG bytes.");
        if (!string.Equals(request.Evidence.ContentType?.Trim(), mime, StringComparison.OrdinalIgnoreCase))
            throw new StoresValidationException("Invoice content does not match the declared content type.");
        var sha = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var company = await Company(ct);
            var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization, "IntercompanyInvoice.Record", key,
                new { request.CorrelationId, number, request.InvoiceDate, filename, mime, sha, Size = content.Length });
            var attempt = await Rev869BCommandContextAuthorizer.OpenForCreationAsync(db, user, Organization, envelope, "IntercompanyInvoice", ct);
            using var replay = await Rev869BCommandContextAuthorizer.ReadCommittedReceiptAsync(db, attempt, ct);
            if (replay is not null)
            {
                var prior = replay.RootElement.GetProperty("Invoice").Deserialize<IntercompanyInvoiceView>(Json)!;
                await tx.CommitAsync(ct);
                return prior with { Replayed = true };
            }
            await using var command = await Command("""
                SELECT advance.record_intercompany_invoice(@company,@id,@publication,@number,@date,
                  @filename,@mime,@content,@actor,@role,@assignment,@type,@login)::text
                """, ct);
            command.Parameters.AddWithValue("company", company);
            command.Parameters.AddWithValue("id", attempt.CommandId);
            command.Parameters.AddWithValue("publication", request.CorrelationId);
            command.Parameters.AddWithValue("number", number);
            command.Parameters.AddWithValue("date", request.InvoiceDate);
            command.Parameters.AddWithValue("filename", filename);
            command.Parameters.AddWithValue("mime", mime);
            command.Parameters.AddWithValue("content", NpgsqlDbType.Bytea, content);
            command.Parameters.AddWithValue("actor", user.EmployeeId!.Value);
            command.Parameters.AddWithValue("role", user.RoleCode);
            command.Parameters.AddWithValue("assignment", user.ResolvedRoleAssignmentId!.Value);
            command.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!);
            command.Parameters.AddWithValue("login", user.LoginId);
            var value = (string?)await command.ExecuteScalarAsync(ct)
                ?? throw new InvalidOperationException("Invoice registration returned no result.");
            var result = JsonSerializer.Deserialize<IntercompanyInvoiceView>(value, Json)!;
            await audit.WriteAsync("Accounts", "IntercompanyInvoice.Record", "IntercompanyInvoice", result.Id.ToString(), null,
                new { request.CorrelationId, InvoiceNumber = number, EvidenceSha256 = sha }, ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { Invoice = result });
            await tx.CommitAsync(ct);
            return result;
        }
        catch (Exception error) when (PostgreSqlConcurrency.IsSerializationFailure(error))
        { throw new DbUpdateConcurrencyException("Intercompany order changed concurrently. Refresh and retry.", error); }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        { throw new UnauthorizedAccessException(error.MessageText, error); }
        catch (PostgresException error) when (error.SqlState is PostgresErrorCodes.RaiseException or PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.CheckViolation or PostgresErrorCodes.ForeignKeyViolation)
        { throw new StoresConflictException(error.MessageText); }
    }

    public async Task<IReadOnlyList<IntercompanyInvoiceView>> ListForPurchaseAsync(Guid correlationId, CancellationToken ct)
    {
        RequireRole("view");
        var company = await Company(ct);
        await using var command = await Command("SELECT advance.intercompany_invoices_for_purchase(@company,@publication)::text", ct);
        command.Parameters.AddWithValue("company", company);
        command.Parameters.AddWithValue("publication", correlationId);
        var value = (string)(await command.ExecuteScalarAsync(ct))!;
        return JsonSerializer.Deserialize<List<IntercompanyInvoiceView>>(value, Json)!;
    }

    public async Task<IntercompanyInvoiceView?> GetAsync(Guid id, CancellationToken ct)
    {
        RequireRole("view");
        var company = await Company(ct);
        await using var command = await Command("SELECT advance.intercompany_invoice_json(@company,@id)::text", ct);
        command.Parameters.AddWithValue("company", company);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteScalarAsync(ct) is string value
            ? JsonSerializer.Deserialize<IntercompanyInvoiceView>(value, Json) : null;
    }

    public async Task<SupplierInvoiceContent> DownloadAsync(Guid id, CancellationToken ct)
    {
        RequireRole("download");
        var company = await Company(ct);
        await using var command = await Command("SELECT * FROM advance.intercompany_invoice_content(@company,@id)", ct);
        command.Parameters.AddWithValue("company", company);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new KeyNotFoundException("Intercompany invoice was not found in this company.");
        var content = reader.GetFieldValue<byte[]>(2);
        var sha = reader.GetString(3);
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(content)), sha, StringComparison.OrdinalIgnoreCase))
            throw new StoresConflictException("Retained invoice evidence failed its integrity check.");
        return new(reader.GetString(0), reader.GetString(1), content, sha);
    }
}
