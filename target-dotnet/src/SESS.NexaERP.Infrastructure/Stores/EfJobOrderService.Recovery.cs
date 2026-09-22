using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfJobOrderService
{
    public async Task<JobOrderView> ReturnToDraftAsync(
        Guid id, ConfirmJobOrderRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("reject", "ACCOUNTS_ASSISTANT", "ACCOUNTS_MANAGER");
        var actor = Actor(); var assignment = Assignment();
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        var reason = Required(request.Reason, "Reason");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var row = await Query(true).SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == id && x.CustomerPurchaseOrderId != null, ct)
            ?? throw new KeyNotFoundException("Job Order was not found.");
        if (await IsRecoveryReplayAsync(row, company.Code, key, "RETURN_TO_DRAFT", request.ExpectedVersion, reason, actor, assignment, ct))
        {
            await tx.CommitAsync(ct); return View(row);
        }
        if (row.Status != "PENDING_ACCOUNTS")
            throw new StoresConflictException("Only a Job Order pending Accounts confirmation can be returned to Draft.");
        if (row.Version != request.ExpectedVersion)
            throw new DbUpdateConcurrencyException("Job Order Version is stale.");
        if (row.InitiatedByEmployeeId == actor)
            throw new StoresConflictException("The Production initiator cannot perform the Accounts return decision.");
        row.Status = "DRAFT"; row.Version++; row.UpdatedAt = DateTimeOffset.UtcNow; row.UpdatedBy = user.LoginId;
        AddRecoveryHistory(row, "RETURN_TO_DRAFT", "PENDING_ACCOUNTS", "DRAFT", reason,
            company.Code + ":" + key, request.ExpectedVersion);
        await CommitAsync(company.Code, "JobOrder.ReturnToDraft", key, request, row, ct);
        await tx.CommitAsync(ct); return View(row);
    }

    public async Task<JobOrderView> ReviseDraftAsync(
        Guid id, ReviseDraftJobOrderRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("update", "PRODUCTION_COORDINATOR", "PRODUCTION_MANAGER");
        var actor = Actor(); var assignment = Assignment();
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        var reason = Required(request.Reason, "Reason");
        if (request.JobOrderDate == default) throw new StoresValidationException("JobOrderDate is required.");
        var serial = Required(request.MachineSerial, "MachineSerial").ToUpperInvariant();
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var row = await Query(true).SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == id && x.CustomerPurchaseOrderId != null, ct)
            ?? throw new KeyNotFoundException("Job Order was not found.");
        if (await IsRecoveryReplayAsync(row, company.Code, key, "REVISE_DRAFT", request.ExpectedVersion, reason, actor, assignment, ct))
        {
            await tx.CommitAsync(ct); return View(row);
        }
        if (row.Status != "DRAFT") throw new StoresConflictException("Only a Draft Job Order can be revised.");
        if (row.Version != request.ExpectedVersion) throw new DbUpdateConcurrencyException("Job Order Version is stale.");
        if (await db.JobOrders.AnyAsync(x => x.CompanyId == company.Id && x.Id != row.Id && x.MachineSerial == serial, ct))
            throw new StoresConflictException("MachineSerial already belongs to a Job Order in this company.");
        row.MachineSerial = serial; row.JobOrderDate = request.JobOrderDate;
        row.PlannedCompletionDate = request.PlannedCompletionDate;
        row.Version++; row.UpdatedAt = DateTimeOffset.UtcNow; row.UpdatedBy = user.LoginId;
        AddRecoveryHistory(row, "REVISE_DRAFT", "DRAFT", "DRAFT", reason,
            company.Code + ":" + key, request.ExpectedVersion);
        await CommitAsync(company.Code, "JobOrder.ReviseDraft", key, request, row, ct);
        await tx.CommitAsync(ct); return View(row);
    }

    public async Task<JobOrderView> ResubmitAsync(
        Guid id, ConfirmJobOrderRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("submit", "PRODUCTION_COORDINATOR", "PRODUCTION_MANAGER");
        var actor = Actor(); var assignment = Assignment();
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        var reason = Required(request.Reason, "Reason");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var row = await Query(true).SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.Id == id && x.CustomerPurchaseOrderId != null, ct)
            ?? throw new KeyNotFoundException("Job Order was not found.");
        if (await IsRecoveryReplayAsync(row, company.Code, key, "RESUBMIT", request.ExpectedVersion, reason, actor, assignment, ct))
        {
            await tx.CommitAsync(ct); return View(row);
        }
        if (row.Status != "DRAFT") throw new StoresConflictException("Only a Draft Job Order can be resubmitted to Accounts.");
        if (row.Version != request.ExpectedVersion) throw new DbUpdateConcurrencyException("Job Order Version is stale.");
        row.Status = "PENDING_ACCOUNTS"; row.Version++; row.UpdatedAt = DateTimeOffset.UtcNow; row.UpdatedBy = user.LoginId;
        AddRecoveryHistory(row, "RESUBMIT", "DRAFT", "PENDING_ACCOUNTS", reason,
            company.Code + ":" + key, request.ExpectedVersion);
        await CommitAsync(company.Code, "JobOrder.Resubmit", key, request, row, ct);
        await tx.CommitAsync(ct); return View(row);
    }

    private async Task<bool> IsRecoveryReplayAsync(JobOrder row, string organization, string key,
        string action, uint expectedVersion, string reason, Guid actor, Guid assignment, CancellationToken ct)
    {
        var correlation = organization + ":" + key;
        var history = await db.JobOrderHistories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CorrelationId == correlation, ct);
        if (history is null) return false;
        if (history.JobOrderId != row.Id || history.Action != action || history.Version != expectedVersion
            || history.Remarks != reason || history.ActorEmployeeId != actor
            || history.ResolvedRoleAssignmentId != assignment)
            throw new StoresConflictException("Idempotency key was reused with different Job Order recovery content or authority.");
        return true;
    }

    private void AddRecoveryHistory(JobOrder row, string action, string from, string to,
        string reason, string correlation, uint sourceVersion)
    {
        var history = new JobOrderHistory
        {
            CompanyId = row.CompanyId, JobOrderId = row.Id, Action = action,
            FromStatus = from, ToStatus = to, ActorEmployeeId = Actor(),
            ActorRoleCode = user.RoleCode, ResolvedRoleAssignmentId = Assignment(),
            ResolvedRoleAssignmentType = user.ResolvedRoleAssignmentType!,
            CorrelationId = correlation, Remarks = reason, CreatedBy = user.LoginId,
            Version = sourceVersion
        };
        db.JobOrderHistories.Add(history);
    }
}