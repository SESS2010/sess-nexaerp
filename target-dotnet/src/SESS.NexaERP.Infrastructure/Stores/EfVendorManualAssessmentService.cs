using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed class EfVendorManualAssessmentService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, IPagePermissionService permissions)
    : IVendorManualAssessmentService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string Organization => !string.IsNullOrWhiteSpace(user.OrganizationId)
        ? user.OrganizationId.Trim().ToUpperInvariant() : throw new UnauthorizedAccessException("Select a company.");
    private async Task<Guid> Company(CancellationToken ct) =>
        await db.Companies.Where(x => x.Code == Organization && x.IsActive && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct)
        ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
    private static string Required(string? value, string name, int maximum)
    {
        var text = value?.Trim() ?? "";
        return text.Length > 0 && text.Length <= maximum && !text.Any(char.IsControl)
            ? text : throw new StoresValidationException($"{name} requires 1 to {maximum} characters without control characters.");
    }
    private async Task<NpgsqlCommand> Command(string sql, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        return new(sql, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
    }
    public async Task<VendorManualAssessmentView> RecordAsync(RecordVendorManualAssessmentRequest request, CancellationToken ct)
    {
        await VendorManualAssessmentAuthority.RequireRoleAsync(user, permissions, "create", ct);
        var reason = Required(request.Reason, "Reason", 2000);
        var key = Required(request.IdempotencyKey, "IdempotencyKey", 100);
        if (request.GoodsReceiptId == Guid.Empty || request.GoodsReceiptVersion < 0
            || request.SupersedesAssessmentId == Guid.Empty
            || request.TechnicalPoints is < 0 or > 15 || request.ResponsePoints is < 0 or > 5
            || request.OverallPoints is < 0 or > 5)
            throw new StoresValidationException("A retained GRN revision and Technical /15, Response /5, Overall /5 are required.");
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var company = await Company(ct);
            var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization, "VendorManualAssessment.Record", key,
                new { request.GoodsReceiptId, request.GoodsReceiptVersion, request.SupersedesAssessmentId,
                    request.TechnicalPoints, request.ResponsePoints, request.OverallPoints, reason });
            var attempt = await Rev869BCommandContextAuthorizer.OpenForCreationAsync(db, user, Organization, envelope, "VendorManualAssessment", ct);
            using var replay = await Rev869BCommandContextAuthorizer.ReadCommittedReceiptAsync(db, attempt, ct);
            if (replay is not null)
            {
                var retained = replay.RootElement.GetProperty("Assessment").Deserialize<VendorManualAssessmentView>(Json)!;
                await tx.CommitAsync(ct);
                return retained with { Replayed = true };
            }
            await using var command = await Command("""
                SELECT advance.record_vendor_manual_assessment(@company,@command,@receipt,@version,@prior,
                    @technical,@response,@overall,@reason,@actor,@role,@assignment,@type,@login)::text
                """, ct);
            command.Parameters.AddWithValue("company", company); command.Parameters.AddWithValue("command", attempt.CommandId);
            command.Parameters.AddWithValue("receipt", request.GoodsReceiptId); command.Parameters.AddWithValue("version", request.GoodsReceiptVersion);
            command.Parameters.AddWithValue("prior", NpgsqlDbType.Uuid, (object?)request.SupersedesAssessmentId ?? DBNull.Value);
            command.Parameters.AddWithValue("technical", request.TechnicalPoints); command.Parameters.AddWithValue("response", request.ResponsePoints);
            command.Parameters.AddWithValue("overall", request.OverallPoints); command.Parameters.AddWithValue("reason", reason);
            command.Parameters.AddWithValue("actor", user.EmployeeId!.Value); command.Parameters.AddWithValue("role", user.RoleCode);
            command.Parameters.AddWithValue("assignment", user.ResolvedRoleAssignmentId!.Value);
            command.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!); command.Parameters.AddWithValue("login", user.LoginId);
            var value = (string?)await command.ExecuteScalarAsync(ct)
                ?? throw new InvalidOperationException("Assessment returned no retained result.");
            var result = JsonSerializer.Deserialize<VendorManualAssessmentView>(value, Json)!;
            await audit.WriteAsync("Quality", "VendorManualAssessment.Record", "VendorManualAssessment", result.Id.ToString(), null,
                new { request.GoodsReceiptId, result.RevisionNumber, result.TechnicalPoints, result.ResponsePoints, result.OverallPoints, reason }, ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { Assessment = result });
            await tx.CommitAsync(ct);
            return result;
        }
        catch (Exception error) when (PostgreSqlConcurrency.IsSerializationFailure(error))
        { throw new DbUpdateConcurrencyException("GRN or assessment changed concurrently. Refresh and retry.", error); }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        { throw new UnauthorizedAccessException(error.MessageText, error); }
        catch (PostgresException error) when (error.SqlState is PostgresErrorCodes.RaiseException or PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.CheckViolation or PostgresErrorCodes.ForeignKeyViolation)
        { throw new StoresConflictException(error.MessageText); }
    }
    public async Task<VendorRatingReceiptPage> ListReceiptsAsync(int page, int pageSize, CancellationToken ct)
    {
        var role = await VendorManualAssessmentAuthority.RequireRoleAsync(user, permissions, "view", ct);
        var organization = user.OrganizationId?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(organization)) throw new UnauthorizedAccessException("Select a company.");
        page = Math.Clamp(page, 1, 100000); pageSize = Math.Clamp(pageSize, 1, 100);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var company = await db.Companies.Where(x => x.Code == organization && x.IsActive && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
        var query = db.GoodsReceipts.AsNoTracking().Where(x => x.CompanyId == company
            && (role == "QC_MANAGER" || x.ReceivedByEmployeeId == user.EmployeeId)
            && x.DocumentKind == "NORMAL" && x.Status == "FINALIZED"
            && !db.GoodsReceipts.Any(reversal => reversal.CompanyId == company
                && reversal.ReversesGoodsReceiptId == x.Id && reversal.Status == "FINALIZED"));
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.ReceivedAt).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new VendorRatingReceiptOption(x.Id, x.Version, x.GrnNumber, x.VendorId, x.ReceivedAt)).ToListAsync(ct);
        await tx.CommitAsync(ct);
        return new(count, page, pageSize, items.AsReadOnly());
    }

    public async Task<IReadOnlyList<VendorManualAssessmentView>> HistoryAsync(Guid goodsReceiptId, CancellationToken ct)
    {
        var role = await VendorManualAssessmentAuthority.RequireRoleAsync(user, permissions, "view", ct);
        var company = await Company(ct);
        if (role == "PRODUCTION_MANAGER" && !await db.GoodsReceipts.AnyAsync(x => x.CompanyId == company
            && x.Id == goodsReceiptId && x.ReceivedByEmployeeId == user.EmployeeId, ct))
            return Array.Empty<VendorManualAssessmentView>();
        await using var command = await Command("SELECT advance.vendor_manual_assessment_history(@company,@receipt)::text", ct);
        command.Parameters.AddWithValue("company", company); command.Parameters.AddWithValue("receipt", goodsReceiptId);
        return JsonSerializer.Deserialize<List<VendorManualAssessmentView>>((string)(await command.ExecuteScalarAsync(ct))!, Json)!;
    }
}
