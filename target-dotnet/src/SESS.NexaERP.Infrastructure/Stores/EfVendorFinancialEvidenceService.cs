using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed class EfVendorFinancialEvidenceService(
    NexaErpDbContext db, ICurrentUser user, IAuditWriter audit)
    : IVendorFinancialEvidenceService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string Organization() => !string.IsNullOrWhiteSpace(user.OrganizationId)
        ? user.OrganizationId.Trim().ToUpperInvariant()
        : throw new UnauthorizedAccessException("Company scope is required.");
    private Guid Actor() => user.EmployeeId
        ?? throw new UnauthorizedAccessException("Resolved employee identity is required.");

    private async Task<Company> CompanyAsync(CancellationToken ct) =>
        await db.Companies.SingleOrDefaultAsync(
            x => x.Code == Organization() && x.IsActive && x.Status == "ACTIVE", ct)
        ?? throw new UnauthorizedAccessException("Selected company is unavailable.");

    public async Task<IReadOnlyList<VendorAdvancePurchaseOrderOption>> ListAdvancePurchaseOrdersAsync(
        Guid? vendorId, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        return await JsonScalar<List<VendorAdvancePurchaseOrderOption>>(
            "SELECT advance.list_vendor_advance_purchase_orders(@company,@vendor)::text",
            p =>
            {
                p.AddWithValue("company", company.Id);
                p.Add("vendor", NpgsqlDbType.Uuid).Value = (object?)vendorId ?? DBNull.Value;
            }, ct);
    }

    public async Task<VendorAdvancePage> ListAdvancesAsync(
        Guid? vendorId, Guid? purchaseOrderId, bool? outstandingOnly,
        int page, int pageSize, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        return await JsonScalar<VendorAdvancePage>(
            "SELECT advance.list_vendor_advances(@company,@vendor,@po,@only,@page,@size)::text",
            p =>
            {
                p.AddWithValue("company", company.Id);
                p.Add("vendor", NpgsqlDbType.Uuid).Value = (object?)vendorId ?? DBNull.Value;
                p.Add("po", NpgsqlDbType.Uuid).Value = (object?)purchaseOrderId ?? DBNull.Value;
                p.Add("only", NpgsqlDbType.Boolean).Value = (object?)outstandingOnly ?? DBNull.Value;
                p.AddWithValue("page", Math.Max(page, 1));
                p.AddWithValue("size", Math.Clamp(pageSize, 1, 200));
            }, ct);
    }

    public async Task<VendorPaymentPage> ListPaymentsAsync(
        Guid? vendorId, int page, int pageSize, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        return await JsonScalar<VendorPaymentPage>(
            "SELECT advance.list_vendor_payments(@company,@vendor,@page,@size)::text",
            p =>
            {
                p.AddWithValue("company", company.Id);
                p.Add("vendor", NpgsqlDbType.Uuid).Value = (object?)vendorId ?? DBNull.Value;
                p.AddWithValue("page", Math.Max(page, 1));
                p.AddWithValue("size", Math.Clamp(pageSize, 1, 200));
            }, ct);
    }

    public async Task<IReadOnlyList<VendorPayableView>> ListPayablesAsync(
        Guid? vendorId, bool overdueOnly, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        return await JsonScalar<List<VendorPayableView>>(
            "SELECT advance.list_vendor_payables(@company,@vendor,@overdue)::text",
            p =>
            {
                p.AddWithValue("company", company.Id);
                p.Add("vendor", NpgsqlDbType.Uuid).Value = (object?)vendorId ?? DBNull.Value;
                p.AddWithValue("overdue", overdueOnly);
            }, ct);
    }

    public async Task<IReadOnlyList<VendorPositionView>> ListVendorPositionsAsync(
        CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        return await JsonScalar<List<VendorPositionView>>(
            "SELECT advance.list_vendor_positions(@company)::text",
            p => p.AddWithValue("company", company.Id), ct);
    }

    public async Task<VendorAdvanceView> RecordAdvanceAsync(
        RecordVendorAdvanceRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("approve", "ACCOUNTS_MANAGER");
        ValidateAdvance(request);
        var company = await CompanyAsync(ct);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
            Organization(), "VendorAdvance.Record", Required(request.IdempotencyKey, "IdempotencyKey"), request);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(
            db, user, Organization(), envelope, "vendor_advances", "VendorAdvance",
            request.PurchaseOrderId, "RECORDED", 0, null, "RECORDED",
            envelope.RequestFingerprint, "Immutable vendor advance recorded.", ct);
        var result = await ExecuteEvidence(
            "SELECT \"EvidenceId\",\"Replayed\" FROM advance.record_vendor_advance(@company,@po,@date,@amount,@currency,@reference,@evidence,@key,@hash,@actor,@role,@assignment,@type,@login)",
            p =>
            {
                p.AddWithValue("company", company.Id);
                p.AddWithValue("po", request.PurchaseOrderId);
                p.AddWithValue("date", request.PaidDate);
                p.AddWithValue("amount", request.Amount);
                p.AddWithValue("currency", request.CurrencyCode.Trim().ToUpperInvariant());
                p.AddWithValue("reference", request.PaymentReference.Trim());
                p.AddWithValue("evidence", request.EvidenceObjectKey.Trim());
                p.AddWithValue("key", request.IdempotencyKey.Trim());
                p.AddWithValue("hash", envelope.RequestFingerprint);
                AddActor(p);
            }, ct);
        if (!result.Replayed)
            await audit.WriteAsync("Accounts", "VendorAdvance.Record", "VendorAdvance",
                result.Id.ToString(), null, new { request.PurchaseOrderId, request.Amount }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        return await LoadAdvance(result.Id, result.Replayed, ct);
    }

    public async Task<VendorAdvanceView> ReverseAdvanceAsync(
        Guid id, ReverseVendorAdvanceRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("reverse", "ACCOUNTS_MANAGER");
        var reason = Required(request.Reason, "Reason");
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        var company = await CompanyAsync(ct);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
            Organization(), "VendorAdvance.Reverse", key, new { id, reason });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(
            db, user, Organization(), envelope, "vendor_advance_reversals", "VendorAdvance",
            id, "REVERSED", 0, "RECORDED", "REVERSED",
            envelope.RequestFingerprint, reason, ct);
        var result = await ExecuteEvidence(
            "SELECT \"EvidenceId\",\"Replayed\" FROM advance.reverse_vendor_advance(@company,@advance,@reason,@key,@hash,@actor,@role,@assignment,@type,@login)",
            p =>
            {
                p.AddWithValue("company", company.Id);
                p.AddWithValue("advance", id);
                p.AddWithValue("reason", reason);
                p.AddWithValue("key", key);
                p.AddWithValue("hash", envelope.RequestFingerprint);
                AddActor(p);
            }, ct);
        if (!result.Replayed)
            await audit.WriteAsync("Accounts", "VendorAdvance.Reverse", "VendorAdvance",
                id.ToString(), null, new { Reason = reason }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        return await LoadAdvance(id, result.Replayed, ct);
    }

    public async Task<VendorPaymentView> RecordPaymentAsync(
        RecordVendorPaymentRequest request, CancellationToken ct)
    {
        try { return await RecordPaymentCoreAsync(request, ct); }
        catch (Exception error) when (PostgreSqlConcurrency.IsSerializationFailure(error))
        {
            throw new DbUpdateConcurrencyException("Vendor payment changed concurrently. Refresh and retry.", error);
        }
    }

    private async Task<VendorPaymentView> RecordPaymentCoreAsync(
        RecordVendorPaymentRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("approve", "ACCOUNTS_MANAGER");
        ValidatePayment(request);
        var company = await CompanyAsync(ct);
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
            Organization(), "VendorPayment.Record", key, request);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(
            db, user, Organization(), envelope, "vendor_payments", "VendorPayment",
            request.VendorId, "RECORDED", 0, null, "RECORDED",
            envelope.RequestFingerprint, "Immutable vendor payment and allocations recorded.", ct);
        var result = await ExecuteEvidence(
            "SELECT \"EvidenceId\",\"Replayed\" FROM advance.record_vendor_payment(@company,@vendor,@date,@amount,@currency,@reference,@evidence,CAST(@lines AS jsonb),@key,@hash,@actor,@role,@assignment,@type,@login)",
            p =>
            {
                p.AddWithValue("company", company.Id);
                p.AddWithValue("vendor", request.VendorId);
                p.AddWithValue("date", request.PaidDate);
                p.AddWithValue("amount", request.Amount);
                p.AddWithValue("currency", request.CurrencyCode.Trim().ToUpperInvariant());
                p.AddWithValue("reference", request.PaymentReference.Trim());
                p.AddWithValue("evidence", request.EvidenceObjectKey?.Trim() ?? string.Empty);
                p.AddWithValue("lines", NpgsqlDbType.Jsonb,
                    JsonSerializer.Serialize(request.Allocations, Json));
                p.AddWithValue("key", key);
                p.AddWithValue("hash", envelope.RequestFingerprint);
                AddActor(p);
            }, ct);
        if (!result.Replayed)
            await audit.WriteAsync("Accounts", "VendorPayment.Record", "VendorPayment",
                result.Id.ToString(), null, new { request.VendorId, request.Amount }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        return await LoadPayment(result.Id, result.Replayed, ct);
    }

    private static void ValidateAdvance(RecordVendorAdvanceRequest request)
    {
        if (request.PurchaseOrderId == Guid.Empty || request.PaidDate == default ||
            request.Amount <= 0 || request.CurrencyCode?.Trim().Length != 3)
            throw new StoresValidationException(
                "PurchaseOrderId, PaidDate, positive Amount and three-letter CurrencyCode are required.");
        _ = Required(request.PaymentReference, "PaymentReference");
        _ = Required(request.EvidenceObjectKey, "EvidenceObjectKey");
    }

    private static void ValidatePayment(RecordVendorPaymentRequest request)
    {
        if (request.VendorId == Guid.Empty || request.PaidDate == default ||
            request.Amount <= 0 || request.CurrencyCode?.Trim().Length != 3 ||
            request.Allocations is null || request.Allocations.Count == 0 ||
            request.Allocations.Any(x => x.VendorBillId == Guid.Empty || x.Amount <= 0))
            throw new StoresValidationException(
                "VendorId, PaidDate, positive Amount, CurrencyCode and positive bill allocations are required.");
        if (request.Allocations.Select(x => x.VendorBillId).Distinct().Count() !=
            request.Allocations.Count)
            throw new StoresValidationException("A bill may be allocated only once per payment.");
        _ = Required(request.PaymentReference, "PaymentReference");
        if (!string.Equals(request.CurrencyCode.Trim(), "INR", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(request.EvidenceObjectKey))
            throw new StoresValidationException("Bank advice evidence reference is required for a foreign payment.");
    }

    private void AddActor(NpgsqlParameterCollection p)
    {
        p.AddWithValue("actor", Actor());
        p.AddWithValue("role", user.RoleCode);
        p.AddWithValue("assignment", user.ResolvedRoleAssignmentId!.Value);
        p.AddWithValue("type", user.ResolvedRoleAssignmentType!);
        p.AddWithValue("login", user.LoginId);
    }

    private async Task<(Guid Id, bool Replayed)> ExecuteEvidence(
        string sql, Action<NpgsqlParameterCollection> add, CancellationToken ct)
    {
        try
        {
            var c = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var command = new NpgsqlCommand(
                sql, c, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
            add(command.Parameters);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct)) return (reader.GetGuid(0), reader.GetBoolean(1));
            throw new StoresConflictException("Controlled financial command returned no evidence.");
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        {
            throw new UnauthorizedAccessException(e.MessageText, e);
        }
        catch (PostgresException e)
        {
            throw new StoresConflictException(e.MessageText);
        }
    }

    private Task<VendorAdvanceView> LoadAdvance(Guid id, bool replayed, CancellationToken ct) =>
        JsonScalar<VendorAdvanceView>(
            "SELECT advance.vendor_advance_json(@company,@id,@replayed)::text",
            async p =>
            {
                p.AddWithValue("company", (await CompanyAsync(ct)).Id);
                p.AddWithValue("id", id);
                p.AddWithValue("replayed", replayed);
            }, ct);

    private Task<VendorPaymentView> LoadPayment(Guid id, bool replayed, CancellationToken ct) =>
        JsonScalar<VendorPaymentView>(
            "SELECT advance.vendor_payment_json(@company,@id,@replayed)::text",
            async p =>
            {
                p.AddWithValue("company", (await CompanyAsync(ct)).Id);
                p.AddWithValue("id", id);
                p.AddWithValue("replayed", replayed);
            }, ct);

    private async Task<T> JsonScalar<T>(
        string sql, Action<NpgsqlParameterCollection> add, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(
            sql, connection,
            (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        add(command.Parameters);
        var value = await command.ExecuteScalarAsync(ct) as string;
        return !string.IsNullOrWhiteSpace(value)
            ? JsonSerializer.Deserialize<T>(value, Json)!
            : throw new KeyNotFoundException("Vendor financial evidence was not found.");
    }

    private async Task<T> JsonScalar<T>(
        string sql, Func<NpgsqlParameterCollection, Task> add, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(
            sql, connection,
            (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        await add(command.Parameters);
        var value = await command.ExecuteScalarAsync(ct) as string;
        return !string.IsNullOrWhiteSpace(value)
            ? JsonSerializer.Deserialize<T>(value, Json)!
            : throw new KeyNotFoundException("Vendor financial evidence was not found.");
    }

    private static string Required(string? value, string name) =>
        !string.IsNullOrWhiteSpace(value) ? value.Trim()
        : throw new StoresValidationException($"{name} is required.");
}