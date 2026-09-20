using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

/// <summary>
/// Stock adjustments (A2): Stores records and submits, the roles the approval snapshot names decide,
/// and the decision that completes the review posts the physical and FIFO ledgers through
/// advance.post_stock_adjustment in the same transaction. Lines and decisions are append-only; the
/// approval band is recomputed from the retained lines at every decision and compared with the
/// snapshot taken at submission.
/// </summary>
public sealed partial class EfStockAdjustmentService(
    NexaErpDbContext db, ICurrentUser user, IAuditWriter audit) : IStockAdjustmentService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] RecorderRoles = ["STORES_EXECUTIVE", "STORES_MANAGER"];
    private static readonly string[] ApproverRoles = ["STORES_MANAGER", "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR", "ACCOUNTS_MANAGER"];

    private string Organization() => !string.IsNullOrWhiteSpace(user.OrganizationId)
        ? user.OrganizationId.Trim().ToUpperInvariant()
        : throw new UnauthorizedAccessException("Company scope is required.");
    private Guid Actor() => user.EmployeeId
        ?? throw new UnauthorizedAccessException("Resolved employee identity is required.");
    private async Task<Company> CompanyAsync(CancellationToken ct) =>
        await db.Companies.SingleOrDefaultAsync(x => x.Code == Organization() && x.IsActive && x.Status == "ACTIVE", ct)
        ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
    private static string Required(string? value, string field, int maximum)
    {
        var text = value?.Trim() ?? string.Empty;
        return text.Length > 0 && text.Length <= maximum && !text.Any(char.IsControl)
            ? text : throw new StoresValidationException($"{field} requires 1 to {maximum} characters.");
    }
    private static string? Optional(string? value, string field, int maximum) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, field, maximum);
    private static string Fingerprint(object value) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Json)))).ToLowerInvariant();
    private static Guid RevisionId(Guid adjustmentId, int revision) =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes($"stock-adjustment-revision|{adjustmentId:D}|{revision}"))[..16]);

    public async Task<IReadOnlyList<StockAdjustmentPeriodView>> ListOpenPeriodsAsync(CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        return await db.FinancialPeriods.AsNoTracking()
            .Where(x => x.CompanyId == company.Id && x.PeriodType == "INVENTORY" && x.IsActive && x.Status == "OPEN")
            .OrderByDescending(x => x.StartDate).ThenBy(x => x.Id)
            .Select(x => new StockAdjustmentPeriodView(x.Id, x.Code, x.Name, x.StartDate, x.EndDate, x.Status)).ToListAsync(ct);
    }

    public async Task<StockAdjustmentPage> ListAsync(string? status, int page, int pageSize, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var query = db.StockAdjustments.AsNoTracking().Where(x => x.CompanyId == company.Id);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpperInvariant());
        var total = await query.CountAsync(ct);
        var ids = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.Id).ToListAsync(ct);
        var items = new List<StockAdjustmentView>(ids.Count);
        foreach (var id in ids) items.Add(await LoadAsync(company.Id, id, false, ct));
        return new(total, page, pageSize, items);
    }

    public async Task<StockAdjustmentView?> GetAsync(Guid id, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        return await db.StockAdjustments.AsNoTracking().AnyAsync(x => x.CompanyId == company.Id && x.Id == id, ct)
            ? await LoadAsync(company.Id, id, false, ct) : null;
    }

    public Task<StockAdjustmentView> CreateAsync(CreateStockAdjustmentRequest request, CancellationToken ct) =>
        Guarded(() => CreateCoreAsync(request, ct));
    public Task<StockAdjustmentView> ReviseAsync(Guid id, ReviseStockAdjustmentRequest request, CancellationToken ct) =>
        Guarded(() => ReviseCoreAsync(id, request, ct));
    public Task<StockAdjustmentView> SubmitAsync(Guid id, StockAdjustmentTransitionRequest request, CancellationToken ct) =>
        Guarded(() => SubmitCoreAsync(id, request, ct));
    public Task<StockAdjustmentView> ApproveAsync(Guid id, StockAdjustmentDecisionRequest request, CancellationToken ct) =>
        Guarded(() => ApproveCoreAsync(id, request, ct));
    public Task<StockAdjustmentView> RejectAsync(Guid id, StockAdjustmentTransitionRequest request, CancellationToken ct) =>
        Guarded(() => RejectCoreAsync(id, request, ct));

    private static async Task<StockAdjustmentView> Guarded(Func<Task<StockAdjustmentView>> action)
    {
        try { return await action(); }
        catch (Exception error) when (PostgreSqlConcurrency.IsSerializationFailure(error))
        { throw new DbUpdateConcurrencyException("Stock adjustment changed concurrently. Reload before retrying.", error); }
        catch (Exception error) when (Postgres(error) is { SqlState: PostgresErrorCodes.InsufficientPrivilege } refused)
        { throw new UnauthorizedAccessException(refused.MessageText, error); }
        catch (Exception error) when (Postgres(error) is { SqlState: PostgresErrorCodes.RaiseException or PostgresErrorCodes.UniqueViolation
            or PostgresErrorCodes.CheckViolation or PostgresErrorCodes.ForeignKeyViolation } conflict)
        { throw new StoresConflictException(conflict.MessageText); }
        // The domain policies refuse with InvalidOperationException/ArgumentException; both are business conflicts here.
        catch (InvalidOperationException error)
        { throw new StoresConflictException(error.Message); }
        catch (ArgumentException error)
        { throw new StoresConflictException(error.Message); }
    }

    // A database refusal reaches the service directly from a command or wrapped by SaveChanges.
    private static PostgresException? Postgres(Exception error) =>
        error as PostgresException ?? (error as DbUpdateException)?.InnerException as PostgresException;

    private async Task<StockAdjustmentView> CreateCoreAsync(CreateStockAdjustmentRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("create", RecorderRoles);
        var key = Required(request.IdempotencyKey, "IdempotencyKey", 100);
        var reasonKind = Required(request.ReasonKind, "ReasonKind", 30).ToUpperInvariant();
        if (!StockAdjustmentReasonKinds.All.Contains(reasonKind, StringComparer.Ordinal))
            throw new StoresValidationException("ReasonKind must be COUNT_VARIANCE, DAMAGE_LOSS or CORRECTION.");
        var remarks = Required(request.Remarks, "Remarks", 1000);
        if (request.WarehouseId == Guid.Empty || request.InventoryPeriodId == Guid.Empty || request.EffectiveDate == default)
            throw new StoresValidationException("WarehouseId, InventoryPeriodId and EffectiveDate are required.");
        var counters = (request.CounterEmployeeIds ?? []).Distinct().ToArray();
        if (counters.Contains(Guid.Empty)) throw new StoresValidationException("Every counter must be identified.");
        var lines = ValidateLines(request.Lines, reasonKind);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization(), "StockAdjustment.Create", key, request);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForCreationAsync(db, user, Organization(), envelope, nameof(StockAdjustment), ct);
        if (await ReplayAsync(attempt, tx, ct) is { } replayed) return replayed;

        if (!await db.Warehouses.AsNoTracking().AnyAsync(x => x.CompanyId == company.Id && x.Id == request.WarehouseId && x.IsActive, ct))
            throw new StoresValidationException("WarehouseId is not an active warehouse of the selected company.");
        var period = await db.FinancialPeriods.AsNoTracking().SingleOrDefaultAsync(x =>
            x.CompanyId == company.Id && x.Id == request.InventoryPeriodId && x.PeriodType == "INVENTORY" && x.IsActive, ct)
            ?? throw new StoresValidationException("InventoryPeriodId is not an inventory period of the selected company.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dating = StockAdjustmentPostingDatePolicy.Resolve(request.EffectiveDate, today, period.StartDate, period.EndDate,
            period.Status != "OPEN", request.BackdateReason, request.BackdateEvidenceId);
        foreach (var counter in counters)
            if (!await db.Employees.AsNoTracking().AnyAsync(x => x.Id == counter && x.Status == "Active", ct))
                throw new StoresValidationException("Every counter must be an active employee.");
        if (request.ReversesStockAdjustmentId.HasValue)
            await RequireReversibleAsync(company.Id, request.ReversesStockAdjustmentId.Value, lines, ct);

        var adjustment = new StockAdjustment
        {
            CompanyId = company.Id, AdjustmentNumber = await NextNumberAsync(company.Id, ct),
            WarehouseId = request.WarehouseId, ReasonKind = reasonKind, EffectiveDate = request.EffectiveDate,
            InventoryPeriodId = period.Id, Status = StockAdjustmentStatuses.Draft, CurrentRevisionNumber = 1,
            Remarks = remarks, RecordedByEmployeeId = Actor(), RecordedRoleAssignmentId = user.ResolvedRoleAssignmentId!.Value,
            CounterEmployeeIdsJson = JsonSerializer.Serialize(counters.Order().ToArray()),
            BackdateReason = Optional(request.BackdateReason, "BackdateReason", 1000),
            BackdateEvidenceId = request.BackdateEvidenceId, DaysBackdated = dating.DaysBackdated,
            ReversesStockAdjustmentId = request.ReversesStockAdjustmentId,
            IdempotencyKey = key, RequestFingerprint = envelope.RequestFingerprint, CreatedBy = user.LoginId
        };
        db.StockAdjustments.Add(adjustment);
        // The model maps columns only (no navigations), so the header is saved before its lines.
        await db.SaveChangesAsync(ct);
        await AddLinesAsync(company.Id, adjustment, 1, lines, ct);
        await db.SaveChangesAsync(ct);
        var view = await LoadAsync(company.Id, adjustment.Id, false, ct);
        await audit.WriteAsync("Stores", "StockAdjustment.Create", nameof(StockAdjustment), adjustment.Id.ToString(), null,
            new { adjustment.AdjustmentNumber, adjustment.ReasonKind, Lines = lines.Count }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { Adjustment = view });
        await tx.CommitAsync(ct);
        return view;
    }

    private async Task<StockAdjustmentView> ReviseCoreAsync(Guid id, ReviseStockAdjustmentRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("update", RecorderRoles);
        var key = Required(request.IdempotencyKey, "IdempotencyKey", 100);
        var reason = Required(request.Reason, "Reason", 1000);
        var remarks = Required(request.Remarks, "Remarks", 1000);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization(), "StockAdjustment.Revise", key, new { id, request });

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var adjustment = await TrackedAsync(company.Id, id, ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(db, user, Organization(), envelope,
            "stock_adjustment_lines", nameof(StockAdjustment), id, "REVISE", request.Version, adjustment.Status,
            StockAdjustmentStatuses.Draft, envelope.RequestFingerprint, reason, ct);
        if (await ReplayAsync(attempt, tx, ct) is { } replayed) return replayed;
        RequireVersion(adjustment, request.Version);
        if (adjustment.Status is not (StockAdjustmentStatuses.Draft or StockAdjustmentStatuses.Submitted or StockAdjustmentStatuses.Rejected))
            throw new StoresConflictException("Only a draft, submitted or rejected adjustment can be revised.");
        var lines = ValidateLines(request.Lines, adjustment.ReasonKind);
        var period = await db.FinancialPeriods.AsNoTracking().SingleAsync(x => x.Id == adjustment.InventoryPeriodId, ct);
        var dating = StockAdjustmentPostingDatePolicy.Resolve(adjustment.EffectiveDate, DateOnly.FromDateTime(DateTime.UtcNow),
            period.StartDate, period.EndDate, period.Status != "OPEN", request.BackdateReason, request.BackdateEvidenceId);
        if (adjustment.ReversesStockAdjustmentId.HasValue)
            await RequireReversibleAsync(company.Id, adjustment.ReversesStockAdjustmentId.Value, lines, ct);
        var from = adjustment.Status;
        adjustment.CurrentRevisionNumber++;
        adjustment.Status = StockAdjustmentStatuses.Draft;
        adjustment.Remarks = remarks;
        adjustment.BackdateReason = Optional(request.BackdateReason, "BackdateReason", 1000);
        adjustment.BackdateEvidenceId = request.BackdateEvidenceId;
        adjustment.DaysBackdated = dating.DaysBackdated;
        adjustment.ApprovalSnapshotJson = null;
        Touch(adjustment);
        await AddLinesAsync(company.Id, adjustment, adjustment.CurrentRevisionNumber, lines, ct);
        await db.SaveChangesAsync(ct);
        var view = await LoadAsync(company.Id, id, false, ct);
        await audit.WriteAsync("Stores", "StockAdjustment.Revise", nameof(StockAdjustment), id.ToString(),
            new { Status = from }, new { adjustment.Status, adjustment.CurrentRevisionNumber, reason }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { Adjustment = view });
        await tx.CommitAsync(ct);
        return view;
    }

    private async Task<StockAdjustmentView> SubmitCoreAsync(Guid id, StockAdjustmentTransitionRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("submit", RecorderRoles);
        var key = Required(request.IdempotencyKey, "IdempotencyKey", 100);
        var reason = Required(request.Reason, "Reason", 1000);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization(), "StockAdjustment.Submit", key, new { id, request });

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var adjustment = await TrackedAsync(company.Id, id, ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(db, user, Organization(), envelope,
            "stock_adjustment_lines", nameof(StockAdjustment), id, "SUBMIT", request.Version, adjustment.Status,
            StockAdjustmentStatuses.Submitted, envelope.RequestFingerprint, reason, ct);
        if (await ReplayAsync(attempt, tx, ct) is { } replayed) return replayed;
        RequireVersion(adjustment, request.Version);
        if (adjustment.Status != StockAdjustmentStatuses.Draft)
            throw new StoresConflictException("Only a draft adjustment can be submitted; a rejected one is revised first.");
        var snapshot = await CaptureAsync(company.Id, adjustment, ct);
        adjustment.ApprovalSnapshotJson = JsonSerializer.Serialize(SnapshotRecord.From(snapshot, adjustment.CurrentRevisionNumber), Json);
        adjustment.Status = StockAdjustmentStatuses.Submitted;
        Touch(adjustment);
        await db.SaveChangesAsync(ct);
        var view = await LoadAsync(company.Id, id, false, ct);
        await audit.WriteAsync("Stores", "StockAdjustment.Submit", nameof(StockAdjustment), id.ToString(),
            new { Status = StockAdjustmentStatuses.Draft }, new { adjustment.Status, snapshot.AbsoluteValue, snapshot.RequiredRoleCodes, reason }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { Adjustment = view });
        await tx.CommitAsync(ct);
        return view;
    }

    private async Task<StockAdjustmentView> ApproveCoreAsync(Guid id, StockAdjustmentDecisionRequest request, CancellationToken ct)
    {
        var key = Required(request.IdempotencyKey, "IdempotencyKey", 100);
        var reason = Required(request.Reason, "Reason", 1000);
        var company = await CompanyAsync(ct);
        // The acting role is one of the outstanding required roles; the caller may name it, otherwise
        // the least privileged outstanding role the employee holds is used.
        var pending = await db.StockAdjustments.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == id, ct)
            ?? throw new KeyNotFoundException("Stock adjustment was not found.");
        var stored = pending.ApprovalSnapshotJson is null ? null : JsonSerializer.Deserialize<SnapshotRecord>(pending.ApprovalSnapshotJson, Json);
        var decided = await db.StockAdjustmentDecisions.AsNoTracking()
            .Where(x => x.CompanyId == company.Id && x.StockAdjustmentId == id && x.RevisionNumber == pending.CurrentRevisionNumber && x.Decision == "APPROVE")
            .Select(x => x.RoleCode).ToListAsync(ct);
        var outstanding = (stored?.RequiredRoleCodes ?? []).Except(decided, StringComparer.Ordinal).ToArray();
        var requestedRole = Optional(request.RoleCode, "RoleCode", 100)?.ToUpperInvariant();
        var candidates = requestedRole is null ? outstanding : outstanding.Where(x => x == requestedRole).ToArray();
        if (pending.Status != StockAdjustmentStatuses.Submitted || candidates.Length == 0)
            candidates = requestedRole is null ? ApproverRoles : [requestedRole];
        var role = user.RequireRole("approve", candidates);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization(), "StockAdjustment.Decide", key, new { id, request, decision = "APPROVE" });

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var adjustment = await TrackedAsync(company.Id, id, ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(db, user, Organization(), envelope,
            "stock_adjustment_decisions", nameof(StockAdjustment), id, "APPROVE", request.Version, adjustment.Status,
            StockAdjustmentStatuses.Approved, envelope.RequestFingerprint, reason, ct);
        if (await ReplayAsync(attempt, tx, ct) is { } replayed) return replayed;
        RequireVersion(adjustment, request.Version);
        if (adjustment.Status != StockAdjustmentStatuses.Submitted)
            throw new StoresConflictException("Only a submitted adjustment can be approved.");
        var snapshot = await CaptureAsync(company.Id, adjustment, ct);
        var record = SnapshotRecord.From(snapshot, adjustment.CurrentRevisionNumber);
        if (adjustment.ApprovalSnapshotJson is null || JsonSerializer.Serialize(record, Json) != JsonSerializer.Serialize(
                JsonSerializer.Deserialize<SnapshotRecord>(adjustment.ApprovalSnapshotJson, Json), Json))
            throw new StoresConflictException("The approval snapshot no longer matches the retained lines; revise and resubmit.");
        var revisionId = RevisionId(adjustment.Id, adjustment.CurrentRevisionNumber);
        var review = StockAdjustmentReview.Begin(revisionId, snapshot);
        var priorDecisions = await db.StockAdjustmentDecisions.AsNoTracking()
            .Where(x => x.CompanyId == company.Id && x.StockAdjustmentId == id && x.RevisionNumber == adjustment.CurrentRevisionNumber && x.Decision == "APPROVE")
            .OrderBy(x => x.DecidedAt).ToListAsync(ct);
        foreach (var prior in priorDecisions)
            review = review.Approve(revisionId, prior.EmployeeId, prior.RoleCode, prior.RoleAssignmentId, prior.DecidedAt, prior.Reason);
        var now = DateTimeOffset.UtcNow;
        review = review.Approve(revisionId, Actor(), role, user.ResolvedRoleAssignmentId!.Value, now, reason);
        db.StockAdjustmentDecisions.Add(new()
        {
            CompanyId = company.Id, StockAdjustmentId = id, RevisionNumber = adjustment.CurrentRevisionNumber, Decision = "APPROVE",
            EmployeeId = Actor(), RoleCode = role, RoleAssignmentId = user.ResolvedRoleAssignmentId!.Value,
            RoleAssignmentType = user.ResolvedRoleAssignmentType!, DecidedAt = now, Reason = reason, IdempotencyKey = key, CreatedBy = user.LoginId
        });
        var completed = review.IsApproved;
        if (completed)
        {
            review.RequireApprovedRevision(revisionId);
            // The FIFO carrying value of removals may have moved since submission; the band must still hold.
            var current = await CaptureAsync(company.Id, adjustment, ct, revalueRemovals: true);
            if (!current.RequiredRoleCodes.SequenceEqual(snapshot.RequiredRoleCodes, StringComparer.Ordinal))
                throw new StoresConflictException("The FIFO carrying value moved since submission and changes the approval band; revise and resubmit.");
            adjustment.Status = StockAdjustmentStatuses.Approved;
        }
        Touch(adjustment);
        await db.SaveChangesAsync(ct);
        Guid? batchId = null;
        if (completed)
        {
            batchId = await PostAsync(company.Id, id, role, key, envelope.RequestFingerprint, ct);
            db.ChangeTracker.Clear();
        }
        var view = await LoadAsync(company.Id, id, false, ct);
        await audit.WriteAsync("Stores", "StockAdjustment.Decide", nameof(StockAdjustment), id.ToString(),
            new { Status = StockAdjustmentStatuses.Submitted }, new { view.Status, role, batchId, reason }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { Adjustment = view });
        await tx.CommitAsync(ct);
        return view;
    }

    private async Task<StockAdjustmentView> RejectCoreAsync(Guid id, StockAdjustmentTransitionRequest request, CancellationToken ct)
    {
        var key = Required(request.IdempotencyKey, "IdempotencyKey", 100);
        var reason = Required(request.Reason, "Reason", 1000);
        var role = user.RequireRole("reject", ApproverRoles);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(Organization(), "StockAdjustment.Reject", key, new { id, request });

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var adjustment = await TrackedAsync(company.Id, id, ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(db, user, Organization(), envelope,
            "stock_adjustment_decisions", nameof(StockAdjustment), id, "REJECT", request.Version, adjustment.Status,
            StockAdjustmentStatuses.Rejected, envelope.RequestFingerprint, reason, ct);
        if (await ReplayAsync(attempt, tx, ct) is { } replayed) return replayed;
        RequireVersion(adjustment, request.Version);
        if (adjustment.Status != StockAdjustmentStatuses.Submitted)
            throw new StoresConflictException("Only a submitted adjustment can be rejected.");
        var snapshot = await CaptureAsync(company.Id, adjustment, ct);
        snapshot.ValidateIndependentDecision(Actor(), role);
        db.StockAdjustmentDecisions.Add(new()
        {
            CompanyId = company.Id, StockAdjustmentId = id, RevisionNumber = adjustment.CurrentRevisionNumber, Decision = "REJECT",
            EmployeeId = Actor(), RoleCode = role, RoleAssignmentId = user.ResolvedRoleAssignmentId!.Value,
            RoleAssignmentType = user.ResolvedRoleAssignmentType!, DecidedAt = DateTimeOffset.UtcNow, Reason = reason, IdempotencyKey = key, CreatedBy = user.LoginId
        });
        adjustment.Status = StockAdjustmentStatuses.Rejected;
        Touch(adjustment);
        await db.SaveChangesAsync(ct);
        var view = await LoadAsync(company.Id, id, false, ct);
        await audit.WriteAsync("Stores", "StockAdjustment.Reject", nameof(StockAdjustment), id.ToString(),
            new { Status = StockAdjustmentStatuses.Submitted }, new { adjustment.Status, role, reason }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { Adjustment = view });
        await tx.CommitAsync(ct);
        return view;
    }
}
