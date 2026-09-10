using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed class EfVendorBillService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit) : IVendorBillService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private string Organization() => !string.IsNullOrWhiteSpace(user.OrganizationId) ? user.OrganizationId.Trim().ToUpperInvariant() : throw new UnauthorizedAccessException("Company scope is required.");
    private Guid Actor() => user.EmployeeId ?? throw new UnauthorizedAccessException("Resolved employee identity is required.");
    private async Task<Company> CompanyAsync(CancellationToken ct) => await db.Companies.SingleOrDefaultAsync(x => x.Code == Organization() && x.IsActive && x.Status == "ACTIVE", ct) ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
    private async Task<T> ProjectionAsync<T>(string sql, Action<NpgsqlParameterCollection> parameters, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        parameters(command.Parameters);
        var json = await command.ExecuteScalarAsync(ct) as string;
        return !string.IsNullOrWhiteSpace(json) ? JsonSerializer.Deserialize<T>(json, JsonOptions)!
            : throw new KeyNotFoundException("Vendor Bill was not found.");
    }

    public async Task<VendorBillPage> ListAsync(string? billNumber, string? status, Guid? vendorId, int page, int pageSize, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        return await ProjectionAsync<VendorBillPage>("SELECT advance.list_vendor_bills(@company,@number,@status,@vendor,@page,@size)::text", p => {
            p.AddWithValue("company", company.Id); p.Add("number", NpgsqlDbType.Text).Value = (object?)billNumber ?? DBNull.Value;
            p.Add("status", NpgsqlDbType.Text).Value = (object?)status ?? DBNull.Value; p.Add("vendor", NpgsqlDbType.Uuid).Value = (object?)vendorId ?? DBNull.Value;
            p.AddWithValue("page", page); p.AddWithValue("size", pageSize);
        }, ct);
    }
    public async Task<VendorBillView?> GetAsync(Guid id, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        try { return await ProjectionAsync<VendorBillView>("SELECT advance.get_vendor_bill(@company,@bill)::text", p => { p.AddWithValue("company", company.Id); p.AddWithValue("bill", id); }, ct); }
        catch (KeyNotFoundException) { return null; }
    }

    public async Task<VendorBillView> CreateAsync(Guid goodsReceiptId, CreateVendorBillRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("create", "ACCOUNTS_ASSISTANT", "ACCOUNTS_MANAGER"); var key = Required(request.IdempotencyKey, "IdempotencyKey"); var billNumber = Required(request.BillNumber, "BillNumber");
        if (request.BillDate == default) throw new StoresValidationException("BillDate is required.");
        if (request.Lines is null || request.Lines.Count == 0 || request.Lines.Any(x => x.GoodsReceiptLineId == Guid.Empty || x.Quantity <= 0 || x.UnitRate < 0 || x.TotalPayableValue < 0)) throw new StoresValidationException("Every bill line requires a GRN line and non-negative monetary values with positive quantity.");
        if (request.Lines.Select(x => x.GoodsReceiptLineId).Distinct().Count() != request.Lines.Count) throw new StoresValidationException("A GRN line may appear only once.");
        var charges = request.Charges ?? [];
        var chargeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DUTY", "INSURANCE", "FREIGHT", "PACKING", "HANDLING", "CLEARING_AGENT", "MISC_INWARD", "NON_CREDITABLE_TAX", "RECOVERABLE_GST" };
        if (charges.Any(x => !chargeTypes.Contains(x.ChargeType?.Trim() ?? "") || x.ChargeValue <= 0 || x.IsRecoverableTax != string.Equals(x.ChargeType?.Trim(), "RECOVERABLE_GST", StringComparison.OrdinalIgnoreCase))) throw new StoresValidationException("Every charge requires a supported type, a positive value, and recoverable-tax classification only for RECOVERABLE_GST.");
        if (request.Lines.Any(x => x.VerifiedGrossWeightKg.HasValue && x.VerifiedGrossWeightKg <= 0)) throw new StoresValidationException("VerifiedGrossWeightKg must be positive when supplied.");
        var hash = Fingerprint(new { goodsReceiptId, billNumber, request.BillDate, request.Lines }); var correlation = Fingerprint("VendorBill.Create:" + key);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var company = await CompanyAsync(ct);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization(), "VendorBill.Create", key, new { goodsReceiptId, request });
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(db, user, Organization(), envelope, "vendor_bill_history", nameof(VendorBill), goodsReceiptId, "CREATED", 0, null, "DRAFT", correlation, "Vendor Bill entered against finalized GRN.", ct);
        (Guid BillId, bool Replayed) result;
        try
        {
            result = await ExecuteCreate(company.Id, goodsReceiptId, billNumber, request.BillDate, request.Lines, key, hash, correlation, ct);
            if (!result.Replayed) await RecordCharges(company.Id, result.BillId, request.Lines, charges, ct);
        }
        catch (PostgresException error) { throw Translate(error); }
        if (!result.Replayed) await audit.WriteAsync("Accounts", "VendorBill.Create", nameof(VendorBill), result.BillId.ToString(), null, new { BillNumber = billNumber }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct); await tx.CommitAsync(ct); return await Load(result.BillId, result.Replayed, ct);
    }
    public Task<VendorBillView> AcceptAsync(Guid id, VendorBillDecisionRequest request, CancellationToken ct) => DecideAsync(id, request, true, ct);
    public Task<VendorBillView> RejectAsync(Guid id, VendorBillDecisionRequest request, CancellationToken ct) => DecideAsync(id, request, false, ct);
    public async Task<VendorBillView> ReverseAsync(Guid id, VendorBillDecisionRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("reverse", "ACCOUNTS_MANAGER");
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        var reason = Required(request.Reason, "Reason");
        var hash = Fingerprint(new { id, request.Version, reason, Action = "REVERSED" });
        var correlation = Fingerprint("VendorBill.Reverse:" + key);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
            Organization(), "VendorBill.Reverse", key, new { id, request });
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(
            db, user, Organization(), envelope, "vendor_bill_history", nameof(VendorBill), id,
            "REVERSED", request.Version, "ACCEPTED", "REVERSED", correlation, reason, ct);
        bool replayed;
        try { replayed = await ExecuteReversal(company.Id, id, request.Version, reason, key, hash, correlation, ct); }
        catch (PostgresException error) { throw Translate(error); }
        if (!replayed) await audit.WriteAsync("Accounts", "VendorBill.Reverse", nameof(VendorBill),
            id.ToString(), new { Status = "ACCEPTED" }, new { Status = "REVERSED", Reason = reason }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
        await tx.CommitAsync(ct);
        return await Load(id, replayed, ct);
    }
    private async Task<VendorBillView> DecideAsync(Guid id, VendorBillDecisionRequest request, bool accept, CancellationToken ct)
    {
        _ = user.RequireRole(accept ? "approve" : "reject", "ACCOUNTS_MANAGER"); var key = Required(request.IdempotencyKey, "IdempotencyKey"); var reason = Required(request.Reason, "Reason"); var operation = accept ? "VendorBill.Accept" : "VendorBill.Reject"; var action = accept ? "ACCEPTED" : "REJECTED"; var hash = Fingerprint(new { id, request.Version, reason, action }); var correlation = Fingerprint(operation + ":" + key);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var company = await CompanyAsync(ct);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization(), operation, key, new { id, request });
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(db, user, Organization(), envelope, "vendor_bill_history", nameof(VendorBill), id, action, request.Version, "DRAFT", action, correlation, reason, ct);
        bool replayed;
        try { replayed = await ExecuteDecision(company.Id, id, request.Version, accept, reason, key, hash, correlation, ct); }
        catch (PostgresException error) { throw Translate(error); }
        if (!replayed) await audit.WriteAsync("Accounts", operation, nameof(VendorBill), id.ToString(), new { Status = "DRAFT" }, new { Status = action }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct); await tx.CommitAsync(ct); return await Load(id, replayed, ct);
    }

    private async Task<(Guid BillId, bool Replayed)> ExecuteCreate(Guid companyId, Guid grnId, string billNumber, DateOnly billDate, IReadOnlyList<VendorBillLineInput> lines, string key, string hash, string correlation, CancellationToken ct)
    {
        var c = (NpgsqlConnection)db.Database.GetDbConnection(); await using var cmd = c.CreateCommand(); cmd.Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction(); cmd.CommandText = "SELECT \"VendorBillId\",\"Replayed\" FROM advance.create_vendor_bill(@company,@grn,@number,@date,CAST(@lines AS jsonb),@key,@hash,@correlation,@actor,@role,@assignment,@type,@login)";
        cmd.Parameters.AddWithValue("company", companyId); cmd.Parameters.AddWithValue("grn", grnId); cmd.Parameters.AddWithValue("number", billNumber); cmd.Parameters.AddWithValue("date", billDate); cmd.Parameters.AddWithValue("lines", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(lines, JsonOptions)); cmd.Parameters.AddWithValue("key", key); cmd.Parameters.AddWithValue("hash", hash); cmd.Parameters.AddWithValue("correlation", correlation); AddActor(cmd);
        await using var reader = await cmd.ExecuteReaderAsync(ct); if (!await reader.ReadAsync(ct)) throw new StoresConflictException("Controlled Vendor Bill creation returned no result."); return (reader.GetGuid(0), reader.GetBoolean(1));
    }
    private async Task RecordCharges(Guid companyId, Guid billId,
        IReadOnlyList<VendorBillLineInput> lines, IReadOnlyList<VendorBillChargeInput> charges,
        CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT advance.record_vendor_bill_charges(@company,@bill,CAST(@lines AS jsonb),CAST(@charges AS jsonb),@actor,@role,@assignment,@type,@login)";
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("bill", billId);
        command.Parameters.AddWithValue("lines", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(lines, JsonOptions));
        command.Parameters.AddWithValue("charges", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(charges, JsonOptions));
        AddActor(command);
        _ = await command.ExecuteScalarAsync(ct);
    }
    private async Task<bool> ExecuteDecision(Guid companyId, Guid id, long version, bool accept, string reason, string key, string hash, string correlation, CancellationToken ct)
    {
        var c = (NpgsqlConnection)db.Database.GetDbConnection(); await using var cmd = c.CreateCommand(); cmd.Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction(); cmd.CommandText = "SELECT \"Replayed\" FROM advance.decide_vendor_bill(@company,@bill,@version,@accept,@reason,@key,@hash,@correlation,@actor,@role,@assignment,@type,@login)";
        cmd.Parameters.AddWithValue("company", companyId); cmd.Parameters.AddWithValue("bill", id); cmd.Parameters.AddWithValue("version", version); cmd.Parameters.AddWithValue("accept", accept); cmd.Parameters.AddWithValue("reason", reason); cmd.Parameters.AddWithValue("key", key); cmd.Parameters.AddWithValue("hash", hash); cmd.Parameters.AddWithValue("correlation", correlation); AddActor(cmd);
        return await cmd.ExecuteScalarAsync(ct) is bool replayed ? replayed : throw new StoresConflictException("Controlled Vendor Bill decision returned no result.");
    }
    private async Task<bool> ExecuteReversal(Guid companyId, Guid id, long version, string reason,
        string key, string hash, string correlation, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT \"Replayed\" FROM advance.reverse_vendor_bill(@company,@bill,@version,@reason,@key,@hash,@correlation,@actor,@role,@assignment,@type,@login)";
        command.Parameters.AddWithValue("company", companyId); command.Parameters.AddWithValue("bill", id);
        command.Parameters.AddWithValue("version", version); command.Parameters.AddWithValue("reason", reason);
        command.Parameters.AddWithValue("key", key); command.Parameters.AddWithValue("hash", hash);
        command.Parameters.AddWithValue("correlation", correlation); AddActor(command);
        return await command.ExecuteScalarAsync(ct) is bool replayed ? replayed
            : throw new StoresConflictException("Controlled Vendor Bill reversal returned no result.");
    }
    private void AddActor(NpgsqlCommand cmd) { cmd.Parameters.AddWithValue("actor", Actor()); cmd.Parameters.AddWithValue("role", user.RoleCode); cmd.Parameters.AddWithValue("assignment", user.ResolvedRoleAssignmentId!.Value); cmd.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!); cmd.Parameters.AddWithValue("login", user.LoginId); }
    private static string Required(string? value, string name) => !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new StoresValidationException($"{name} is required.");
    private static string Fingerprint(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value is string text ? text : JsonSerializer.Serialize(value, JsonOptions)))).ToLowerInvariant();
    private static Exception Translate(PostgresException error) => error.SqlState == PostgresErrorCodes.InsufficientPrivilege
        ? new UnauthorizedAccessException(error.MessageText, error)
        : new StoresConflictException(error.MessageText);
    private async Task<VendorBillView> Load(Guid id, bool replayed, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var value = await ProjectionAsync<VendorBillView>("SELECT advance.get_vendor_bill(@company,@bill)::text", p => { p.AddWithValue("company", company.Id); p.AddWithValue("bill", id); }, ct);
        return value with { Replayed = replayed };
    }
}