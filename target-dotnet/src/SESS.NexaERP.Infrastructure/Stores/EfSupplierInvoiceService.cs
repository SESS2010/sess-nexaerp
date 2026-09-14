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

public sealed class EfSupplierInvoiceService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit) : ISupplierInvoiceService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string Organization => !string.IsNullOrWhiteSpace(user.OrganizationId)
        ? user.OrganizationId.Trim().ToUpperInvariant() : throw new UnauthorizedAccessException("Company scope is required.");
    private async Task<Guid> Company(CancellationToken ct) =>
        await db.Companies.Where(x => x.Code == Organization && x.IsActive && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct)
        ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
    private async Task<NpgsqlCommand> Command(string sql, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        return new(sql, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
    }
    private static string Required(string? text, string name, int maximum = 100)
    {
        var value = text?.Trim() ?? "";
        return value.Length is > 0 && value.Length <= maximum && !value.Any(char.IsControl)
            ? value : throw new StoresValidationException($"{name} must contain 1 to {maximum} characters without control characters.");
    }
    private void AddActor(NpgsqlCommand command)
    {
        command.Parameters.AddWithValue("actor", user.EmployeeId ?? throw new UnauthorizedAccessException("Employee identity is required."));
        command.Parameters.AddWithValue("role", user.RoleCode);
        command.Parameters.AddWithValue("assignment", user.ResolvedRoleAssignmentId ?? throw new UnauthorizedAccessException("Effective role assignment is required."));
        command.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!);
    }
    private static async Task<T> Translate<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (Exception e) when (PostgreSqlConcurrency.IsSerializationFailure(e))
        { throw new DbUpdateConcurrencyException("Invoice or receipt changed concurrently. Refresh and retry.", e); }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        { throw new UnauthorizedAccessException(e.MessageText, e); }
        catch (PostgresException e) when (e.SqlState is PostgresErrorCodes.RaiseException or PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.CheckViolation)
        { throw new StoresConflictException(e.MessageText); }
    }
    public async Task<PagedResponse<SupplierInvoicePurchaseOrderOption>> ListPurchaseOrdersAsync(
        string? search, int page, int pageSize, CancellationToken ct)
    {
        var term = search?.Trim().ToUpperInvariant() ?? string.Empty;
        if (page < 1 || pageSize is < 1 or > 200 || term.Length > 100 || ((long)page - 1) * pageSize > int.MaxValue)
            throw new StoresValidationException("Use a positive page, 1 to 200 rows and at most 100 search characters.");
        var company = await Company(ct);
        // Match the controlled invoice command's eligibility. Paid POs remain
        // available because documentary invoice intake is independent of advances.
        var query = db.PurchaseOrders.AsNoTracking()
            .Where(po => po.CompanyId == company && po.Status == "Issued");
        if (term.Length > 0)
            query = query.Where(po => po.PoNumber.ToUpper().Contains(term) ||
                po.Vendor!.VendorCode.ToUpper().Contains(term) || po.Vendor!.Name.ToUpper().Contains(term));
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(po => po.PoNumber).ThenBy(po => po.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(po => new SupplierInvoicePurchaseOrderOption(po.Id, po.PoNumber, po.RevisionNumber,
                po.VendorId, po.Vendor!.VendorCode, po.Vendor!.Name, po.CurrencyCode,
                po.Lines.Where(line => line.CompanyId == company).OrderBy(line => line.LineNumber).ThenBy(line => line.Id)
                    .Select(line => new SupplierInvoicePurchaseOrderLineOption(line.Id, line.LineNumber,
                        line.ItemId, line.ItemCodeSnapshot, line.ItemNameSnapshot, line.UomSnapshot,
                        line.OrderedQuantity, line.UnitRate, line.TotalPayableValue)).ToArray()))
            .ToArrayAsync(ct);
        return new(total, page, pageSize, rows);
    }

    public Task<SupplierInvoiceView> RecordAsync(RecordSupplierInvoiceRequest request, CancellationToken ct) => Translate(async () =>
    {
        _ = user.RequireRole("create", "ACCOUNTS_ASSISTANT", "ACCOUNTS_MANAGER");
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        var number = Required(request.InvoiceNumber, "InvoiceNumber");
        var currency = Required(request.CurrencyCode, "CurrencyCode", 3).ToUpperInvariant();
        if (request.PurchaseOrderId == Guid.Empty || request.InvoiceDate == default || currency.Length != 3 ||
            request.Lines is null || request.Lines.Count is < 1 or > 1000 ||
            request.Lines.Select(x => x.PurchaseOrderLineId).Distinct().Count() != request.Lines.Count ||
            request.Lines.Any(x => x.PurchaseOrderLineId == Guid.Empty || x.Quantity <= 0 || x.UnitRate < 0 || x.PayableValue < 0 ||
                decimal.Round(x.Quantity, 6) != x.Quantity || decimal.Round(x.UnitRate, 6) != x.UnitRate ||
                decimal.Round(x.PayableValue, 6) != x.PayableValue))
            throw new StoresValidationException("An invoice requires its PO, date, currency and distinct PO lines with positive quantities and non-negative six-decimal values.");
        if (request.Evidence?.Content is not { Length: > 0 and <= 5242880 } content)
            throw new StoresValidationException("Retained invoice evidence of at most 5 MB is required.");
        var filename = Required((request.Evidence.FileName ?? "").Replace('\\', '/').Split('/').Last(), "FileName", 255);
        var mime = ContentType(content);
        if (!string.Equals(request.Evidence.ContentType?.Trim(), mime, StringComparison.OrdinalIgnoreCase))
            throw new StoresValidationException("Invoice content does not match its declared PDF, JPEG or PNG type.");
        var sha = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization, "SupplierInvoice.Record", key,
            new { request.PurchaseOrderId, number, request.InvoiceDate, currency, request.Lines, filename, mime, sha, Size = content.Length });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await Company(ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForCreationAsync(db, user, Organization, envelope, "SupplierInvoice", ct);
        if (await Replay(attempt, ct) is { } replay) { await tx.CommitAsync(ct); return replay; }
        await using var command = await Command("""
            SELECT advance.record_supplier_invoice(@company,@id,@po,@number,@date,@currency,@lines,
             @filename,@mime,@content,@actor,@role,@assignment,@type,@login)
            """, ct);
        command.Parameters.AddWithValue("company", company); command.Parameters.AddWithValue("id", attempt.CommandId);
        command.Parameters.AddWithValue("po", request.PurchaseOrderId); command.Parameters.AddWithValue("number", number);
        command.Parameters.AddWithValue("date", request.InvoiceDate); command.Parameters.AddWithValue("currency", currency);
        command.Parameters.AddWithValue("lines", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(request.Lines, Json));
        command.Parameters.AddWithValue("filename", filename); command.Parameters.AddWithValue("mime", mime);
        command.Parameters.AddWithValue("content", NpgsqlDbType.Bytea, content); command.Parameters.AddWithValue("login", user.LoginId);
        AddActor(command);
        if (await command.ExecuteScalarAsync(ct) is not Guid id || id != attempt.CommandId)
            throw new InvalidOperationException("Invoice command returned an unexpected identifier.");
        await audit.WriteAsync("Accounts", "SupplierInvoice.Record", "SupplierInvoice", id.ToString(), null,
            new { InvoiceNumber = number, EvidenceSha256 = sha }, ct);
        var result = await Load(company, id, ct) ?? throw new InvalidOperationException("Recorded invoice is missing.");
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { Invoice = result });
        await tx.CommitAsync(ct);
        return result;
    });
    public Task<SupplierInvoiceView> CancelAsync(Guid id, CancelSupplierInvoiceRequest request, CancellationToken ct) =>
        Change(id, request.Version, request.IdempotencyKey, "SupplierInvoice.Cancel", "cancel",
            Required(request.Reason, "Reason", 2000), null, ct);
    public Task<SupplierInvoiceView> LinkAcceptedBillAsync(Guid id, LinkSupplierInvoiceAcceptedBillRequest request, CancellationToken ct) =>
        Change(id, request.Version, request.IdempotencyKey, "SupplierInvoice.LinkBill", "approve", null, request.VendorBillId, ct);
    private Task<SupplierInvoiceView> Change(Guid id, long version, string key, string operation, string action,
        string? reason, Guid? bill, CancellationToken ct) => Translate(async () =>
    {
        _ = user.RequireRole(action, "ACCOUNTS_MANAGER");
        if (id == Guid.Empty || version < 1 || bill == Guid.Empty) throw new StoresValidationException("Invoice, current version and bill identifier are required.");
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization, operation,
            Required(key, "IdempotencyKey"), new { id, version, reason, bill });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await Company(ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(db, user, Organization, envelope,
            "supplier_invoices", "SupplierInvoice", id, action, version, "RECORDED", action,
            envelope.RequestFingerprint, reason ?? "Link retained accepted bill.", ct);
        if (await Replay(attempt, ct) is { } replay) { await tx.CommitAsync(ct); return replay; }
        var sql = bill.HasValue
            ? "SELECT advance.link_supplier_invoice_bill(@company,@id,@version,@bill,@actor,@role,@assignment,@type)"
            : "SELECT advance.cancel_supplier_invoice(@company,@id,@version,@reason,@actor,@role,@assignment,@type)";
        await using var command = await Command(sql, ct);
        command.Parameters.AddWithValue("company", company); command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("version", version); AddActor(command);
        if (bill.HasValue) command.Parameters.AddWithValue("bill", bill.Value);
        else command.Parameters.AddWithValue("reason", reason!);
        _ = await command.ExecuteScalarAsync(ct);
        await audit.WriteAsync("Accounts", operation, "SupplierInvoice", id.ToString(), null, new { reason, bill }, ct);
        var result = await Load(company, id, ct) ?? throw new InvalidOperationException("Invoice is missing.");
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { Invoice = result });
        await tx.CommitAsync(ct);
        return result;
    });
    private async Task<SupplierInvoiceView?> Replay(Rev869BCommandContextAuthorizer.CommandAttemptHandle attempt, CancellationToken ct)
    {
        using var receipt = await Rev869BCommandContextAuthorizer.ReadCommittedReceiptAsync(db, attempt, ct);
        if (receipt is null) return null;
        return (receipt.RootElement.GetProperty("Invoice").Deserialize<SupplierInvoiceView>(Json)
            ?? throw new InvalidOperationException("Invoice receipt is incomplete.")) with { Replayed = true };
    }
    private async Task<SupplierInvoiceView?> Load(Guid company, Guid id, CancellationToken ct)
    {
        await using var command = await Command("SELECT advance.get_supplier_invoice(@company,@id)::text", ct);
        command.Parameters.AddWithValue("company", company); command.Parameters.AddWithValue("id", id);
        return await command.ExecuteScalarAsync(ct) is string value ? JsonSerializer.Deserialize<SupplierInvoiceView>(value, Json) : null;
    }
    public async Task<SupplierInvoiceView?> GetAsync(Guid id, CancellationToken ct) => await Load(await Company(ct), id, ct);
    public async Task<SupplierInvoiceContent> DownloadAsync(Guid id, CancellationToken ct)
    {
        var company = await Company(ct);
        await using var command = await Command("SELECT * FROM advance.supplier_invoice_content(@company,@id)", ct);
        command.Parameters.AddWithValue("company", company); command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new KeyNotFoundException("Invoice evidence not found in this company.");
        var content = reader.GetFieldValue<byte[]>(2); var sha = reader.GetString(3);
        if (Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant() != sha)
            throw new StoresConflictException("Invoice evidence integrity check failed. Administrator action is required.");
        return new(reader.GetString(0), reader.GetString(1), content, sha);
    }
    private static string ContentType(byte[] content) =>
        content.AsSpan().StartsWith("%PDF-"u8) ? "application/pdf" :
        content.AsSpan().StartsWith(new byte[] { 0xff, 0xd8, 0xff }) ? "image/jpeg" :
        content.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }) ? "image/png" :
        throw new StoresValidationException("Invoice evidence must contain PDF, JPEG or PNG bytes.");
}
