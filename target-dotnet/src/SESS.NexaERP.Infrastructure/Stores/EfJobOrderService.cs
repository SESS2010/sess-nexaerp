using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Sales;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed class EfJobOrderService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit) : IJobOrderService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PagedResponse<JobOrderSummary>> ListAsync(int? page, int? pageSize, string? search, string? status, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var number = Math.Max(1, page ?? 1); var size = Math.Clamp(pageSize ?? 50, 1, 200);
        var query = Query().Where(x => x.CompanyId == company.Id && x.CustomerPurchaseOrderId != null);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToUpperInvariant(); query = query.Where(x => x.JobOrderNumber.ToUpper().Contains(term) || x.MachineSerial.ToUpper().Contains(term) || x.CustomerName.ToUpper().Contains(term) || x.CustomerPurchaseOrder!.CustomerPoNumber.ToUpper().Contains(term)); }
        if (!string.IsNullOrWhiteSpace(status)) { var code = status.Trim().ToUpperInvariant(); query = query.Where(x => x.Status == code); }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.JobOrderDate).ThenByDescending(x => x.JobOrderNumber)
            .Skip((number - 1) * size).Take(size).ToListAsync(ct);
        return new(total, number, size, rows.Select(Summary).ToArray());
    }

    public async Task<JobOrderView?> GetAsync(Guid id, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        var row = await Query().SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == id && x.CustomerPurchaseOrderId != null, ct);
        return row is null ? null : View(row);
    }

    public async Task<IReadOnlyList<JobOrderHistoryView>> HistoryAsync(Guid id, CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        if (!await db.JobOrders.AnyAsync(x => x.CompanyId == company.Id && x.Id == id && x.CustomerPurchaseOrderId != null, ct)) throw new KeyNotFoundException("Job Order was not found.");
        return await db.JobOrderHistories.AsNoTracking().Where(x => x.CompanyId == company.Id && x.JobOrderId == id)
            .OrderBy(x => x.CreatedAt).Select(x => new JobOrderHistoryView(x.Id, x.Action, x.FromStatus, x.ToStatus,
                x.ActorEmployeeId, x.ActorRoleCode, x.ResolvedRoleAssignmentId, x.ResolvedRoleAssignmentType,
                x.CorrelationId, x.Remarks, x.CreatedAt)).ToListAsync(ct);
    }

    public async Task<JobOrderView> CreateAsync(CreateJobOrderRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("create", "PRODUCTION_COORDINATOR", "PRODUCTION_MANAGER");
        var actor = Actor(); var assignment = Assignment(); var key = Required(request.IdempotencyKey, "IdempotencyKey"); var fingerprint = Fingerprint(request);
        if (request.CustomerPurchaseOrderLineId == Guid.Empty) throw new StoresValidationException("CustomerPurchaseOrderLineId is required.");
        if (request.MachineOrdinal <= 0) throw new StoresValidationException("MachineOrdinal must be positive.");
        if (request.JobOrderDate == default) throw new StoresValidationException("JobOrderDate is required.");
        var serial = Required(request.MachineSerial, "MachineSerial").ToUpperInvariant();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"JOB_ORDER:" + company.Code + ":" + key},0))", ct);
        var replay = await Query(true).SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.IdempotencyKey == key, ct);
        if (replay is not null)
        {
            if (replay.RequestFingerprint != fingerprint || replay.InitiatedByEmployeeId != actor || replay.InitiatedRoleAssignmentId != assignment)
                throw new StoresConflictException("Idempotency key was reused with different Job Order content or authority.");
            await tx.CommitAsync(ct); return View(replay);
        }
        var line = await db.CustomerPurchaseOrderLines.Include(x => x.CustomerPurchaseOrder)!.ThenInclude(x => x!.Customer)
            .Include(x => x.Item).SingleOrDefaultAsync(x => x.Id == request.CustomerPurchaseOrderLineId && x.CustomerPurchaseOrder!.CompanyId == company.Id, ct)
            ?? throw new StoresValidationException("CustomerPurchaseOrderLineId does not belong to the selected company.");
        var po = line.CustomerPurchaseOrder!;
        if (po.WorkStatus == CustomerPoWorkStatuses.Completed) throw new StoresConflictException("A Job Order cannot be created from a completed Customer PO.");
        if (line.RevisionNumber != po.CurrentRevisionNumber) throw new StoresConflictException("A Job Order must use a line from the current Customer PO revision.");
        if (!line.Quantity.HasValue || line.Quantity <= 0 || line.Quantity != decimal.Truncate(line.Quantity.Value)) throw new StoresValidationException("The machine Customer PO line requires a positive whole Quantity.");
        if (request.MachineOrdinal > line.Quantity.Value) throw new StoresValidationException($"MachineOrdinal {request.MachineOrdinal} exceeds Customer PO line quantity {line.Quantity.Value}.");
        if (await db.JobOrders.AnyAsync(x => x.CompanyId == company.Id && x.MachineSerial == serial, ct)) throw new StoresConflictException("MachineSerial already belongs to a Job Order in this company.");
        var row = new JobOrder
        {
            CompanyId = company.Id, JobOrderNumber = await NextNumberAsync(company.Id, company.Code, ct),
            CustomerPurchaseOrderId = po.Id, CustomerPurchaseOrderLineId = line.Id, MachineOrdinal = request.MachineOrdinal,
            MachineModel = line.Item!.Name, MachineSerial = serial, CustomerName = po.Customer!.Name,
            Status = "PENDING_ACCOUNTS", JobOrderDate = request.JobOrderDate, PlannedCompletionDate = request.PlannedCompletionDate,
            InitiatedByEmployeeId = actor, InitiatedActorRoleCode = user.RoleCode,
            InitiatedRoleAssignmentId = assignment, InitiatedRoleAssignmentType = user.ResolvedRoleAssignmentType,
            IdempotencyKey = key, RequestFingerprint = fingerprint, CreatedBy = user.LoginId
        };
        db.JobOrders.Add(row); AddHistory(row, "CREATE", null, "PENDING_ACCOUNTS", "Production initiated Job Order from current Customer PO line.", company.Code + ":" + key);
        await CommitAsync(company.Code, "JobOrder.Create", key, request, row, ct);
        await tx.CommitAsync(ct); return View(row);
    }

    public async Task<JobOrderView> ConfirmAccountsAsync(Guid id, ConfirmJobOrderRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("verify", "ACCOUNTS_ASSISTANT", "ACCOUNTS_MANAGER");
        var actor = Actor(); var assignment = Assignment(); var key = Required(request.IdempotencyKey, "IdempotencyKey"); var reason = Required(request.Reason, "Reason"); var fingerprint = Fingerprint(request);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var row = await Query(true).SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == id && x.CustomerPurchaseOrderId != null, ct)
            ?? throw new KeyNotFoundException("Job Order was not found.");
        if (row.Status == "OPEN" && row.ConfirmationIdempotencyKey == key)
        {
            if (row.ConfirmationRequestFingerprint != fingerprint || row.AccountsConfirmedByEmployeeId != actor || row.AccountsConfirmationRoleAssignmentId != assignment)
                throw new StoresConflictException("Confirmation replay does not match the original authority or request.");
            await tx.CommitAsync(ct); return View(row);
        }
        if (row.Status != "PENDING_ACCOUNTS") throw new StoresConflictException("Only a Job Order pending Accounts confirmation can be confirmed.");
        if (row.Version != request.ExpectedVersion) throw new DbUpdateConcurrencyException("Job Order Version is stale.");
        if (row.InitiatedByEmployeeId == actor) throw new StoresConflictException("Nobody may provide Accounts confirmation for a Job Order they initiated.");
        row.Status = "OPEN"; row.AccountsConfirmedAt = DateTimeOffset.UtcNow; row.AccountsConfirmedByEmployeeId = actor;
        row.AccountsConfirmationActorRoleCode = user.RoleCode; row.AccountsConfirmationRoleAssignmentId = assignment;
        row.AccountsConfirmationRoleAssignmentType = user.ResolvedRoleAssignmentType; row.AccountsConfirmationReason = reason;
        row.ConfirmationIdempotencyKey = key; row.ConfirmationRequestFingerprint = fingerprint;
        row.Version++; row.UpdatedAt = DateTimeOffset.UtcNow; row.UpdatedBy = user.LoginId;
        AddHistory(row, "ACCOUNTS_CONFIRM", "PENDING_ACCOUNTS", "OPEN", reason, company.Code + ":" + key);
        await CommitAsync(company.Code, "JobOrder.AccountsConfirm", key, request, row, ct);
        await tx.CommitAsync(ct); return View(row);
    }

    private IQueryable<JobOrder> Query(bool tracking = false)
    {
        var query = db.JobOrders.Include(x => x.CustomerPurchaseOrder)!.ThenInclude(x => x!.Customer)
            .Include(x => x.CustomerPurchaseOrderLine)!.ThenInclude(x => x!.Item);
        return tracking ? query : query.AsNoTracking();
    }
    private JobOrderSummary Summary(JobOrder x) => new(x.Id, x.JobOrderNumber, x.CustomerPurchaseOrderId!.Value,
        x.CustomerPurchaseOrderLineId!.Value, x.CustomerPurchaseOrder!.CustomerPoNumber, x.MachineOrdinal!.Value,
        x.MachineModel, x.MachineSerial, x.CustomerName, x.Status, x.JobOrderDate, x.PlannedCompletionDate, x.Version);
    private JobOrderView View(JobOrder x) => new(x.Id, x.JobOrderNumber, x.CustomerPurchaseOrderId!.Value,
        x.CustomerPurchaseOrderLineId!.Value, x.CustomerPurchaseOrder!.PoRecordNumber, x.CustomerPurchaseOrder.CustomerPoNumber,
        x.CustomerPurchaseOrder.CurrentRevisionNumber, x.CustomerPurchaseOrderLine!.SlNo, x.CustomerPurchaseOrderLine.ItemId,
        x.CustomerPurchaseOrderLine.Item!.ItemCode, x.MachineOrdinal!.Value, x.MachineModel, x.MachineSerial, x.CustomerName,
        x.Status, x.JobOrderDate, x.PlannedCompletionDate, x.InitiatedByEmployeeId!.Value, x.InitiatedActorRoleCode!,
        x.InitiatedRoleAssignmentId!.Value, x.InitiatedRoleAssignmentType!, x.AccountsConfirmedAt,
        x.AccountsConfirmedByEmployeeId, x.AccountsConfirmationActorRoleCode, x.AccountsConfirmationRoleAssignmentId,
        x.AccountsConfirmationRoleAssignmentType, x.AccountsConfirmationReason, x.FatReadinessStatus,
        x.FatReconciledAt, x.FatReconciledByEmployeeId, x.LatestFatReconciliationId, x.Version);
    private void AddHistory(JobOrder row, string action, string? from, string to, string remarks, string correlation) => db.JobOrderHistories.Add(new()
    {
        CompanyId = row.CompanyId, JobOrderId = row.Id, Action = action, FromStatus = from, ToStatus = to,
        ActorEmployeeId = Actor(), ActorRoleCode = user.RoleCode, ResolvedRoleAssignmentId = Assignment(),
        ResolvedRoleAssignmentType = user.ResolvedRoleAssignmentType!, CorrelationId = correlation, Remarks = remarks,
        CreatedBy = user.LoginId
    });
    private async Task CommitAsync(string organization, string operation, string key, object request, JobOrder row, CancellationToken ct)
    {
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(organization, operation, key, request);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(db, user, organization, envelope, ct, user.RoleCode)
            ?? throw new InvalidOperationException("Job Order command produced no immutable operation slot.");
        await audit.WriteAsync("Production", operation, nameof(JobOrder), row.Id.ToString(), null,
            new { row.JobOrderNumber, row.CustomerPurchaseOrderId, row.CustomerPurchaseOrderLineId, row.MachineOrdinal, row.Status }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
    }
    private async Task<string> NextNumberAsync(Guid companyId, string organization, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow); var year = today.Month >= 4 ? $"{today.Year % 100:00}-{(today.Year + 1) % 100:00}" : $"{(today.Year - 1) % 100:00}-{today.Year % 100:00}";
        var lockKey = $"NUMBER:{organization}:{year}:JO"; await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey},0))", ct);
        var sequence = await db.PurchaseNumberSequences.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.OrganizationId == organization && x.FinancialYear == year && x.Prefix == "JO" && x.IsActive, ct);
        if (sequence is null) { sequence = new PurchaseNumberSequence { CompanyId = companyId, OrganizationId = organization, FinancialYear = year, Prefix = "JO", CreatedBy = user.LoginId }; db.PurchaseNumberSequences.Add(sequence); }
        sequence.LastNumber++; sequence.UpdatedAt = DateTimeOffset.UtcNow; sequence.UpdatedBy = user.LoginId;
        return $"JO-{organization}-{year}-{sequence.LastNumber:000001}";
    }
    private string Organization() => !string.IsNullOrWhiteSpace(user.OrganizationId) ? user.OrganizationId.Trim().ToUpperInvariant() : throw new UnauthorizedAccessException("Company scope is required.");
    private Guid Actor() => user.EmployeeId ?? throw new UnauthorizedAccessException("Resolved employee identity is required.");
    private Guid Assignment() => user.ResolvedRoleAssignmentId ?? throw new UnauthorizedAccessException("Resolved effective assignment is required.");
    private async Task<SESS.NexaERP.Domain.Foundation.Company> CompanyAsync(CancellationToken ct) => await db.Companies.SingleOrDefaultAsync(x => x.Code == Organization() && x.IsActive && x.Status == "ACTIVE", ct) ?? throw new UnauthorizedAccessException("Selected company is unavailable.");
    private static string Required(string? value, string field) => !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new StoresValidationException(field + " is required.");
    private static string Fingerprint(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions)))).ToLowerInvariant();
}