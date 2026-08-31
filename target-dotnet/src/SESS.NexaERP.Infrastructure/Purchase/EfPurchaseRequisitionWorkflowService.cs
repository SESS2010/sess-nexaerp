using System.Data;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Purchase;

public sealed class EfPurchaseRequisitionWorkflowService(
    NexaErpDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    IPurchaseApprovalWorkflowService workflow) : IPurchaseRequisitionWorkflowService
{
    public Task<PurchaseRequisitionDetail> SubmitAsync(string number, PurchaseRequisitionActionRequest request, CancellationToken ct) =>
        ChangeStatusAsync(number, request, "Submit", PurchaseRequisitionStatuses.Draft, PurchaseRequisitionStatuses.Submitted, "Purchase", ct);
    public Task<PurchaseRequisitionDetail> VerifyAsync(string number, PurchaseRequisitionActionRequest request, CancellationToken ct) =>
        ChangeStatusAsync(number, request, "DepartmentVerify", PurchaseRequisitionStatuses.Submitted, PurchaseRequisitionStatuses.PendingApproval, "Purchase", ct);
    public Task<PurchaseRequisitionDetail> ApproveAsync(string number, PurchaseRequisitionActionRequest request, CancellationToken ct) => DecideAsync(number, request, "Approve", ct);
    public Task<PurchaseRequisitionDetail> RejectAsync(string number, PurchaseRequisitionActionRequest request, CancellationToken ct) => DecideAsync(number, request, "Reject", ct);
    public Task<PurchaseRequisitionDetail> RequestRevisionAsync(string number, PurchaseRequisitionActionRequest request, CancellationToken ct) => DecideAsync(number, request, "RequestRevision", ct);
    public Task<PurchaseRequisitionDetail> ResubmitAsync(string number, PurchaseRequisitionActionRequest request, CancellationToken ct) =>
        ChangeStatusAsync(number, request, "Resubmit", PurchaseRequisitionStatuses.RevisionRequested, PurchaseRequisitionStatuses.Submitted, "Purchase", ct);
    public Task<PurchaseRequisitionDetail> CancelAsync(string number, PurchaseRequisitionActionRequest request, CancellationToken ct) =>
        ChangeStatusAsync(number, request, "Cancel", null, PurchaseRequisitionStatuses.Cancelled, "Purchase", ct);
    public Task<PurchaseRequisitionDetail> HoldAsync(string number, PurchaseRequisitionActionRequest request, CancellationToken ct) =>
        ChangeStatusAsync(number, request, "Hold", null, PurchaseRequisitionStatuses.Held, "Purchase", ct);

    private async Task<PurchaseRequisitionDetail> DecideAsync(string number, PurchaseRequisitionActionRequest request, string action, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var pr = await Scoped(IncludeDetail(db.PurchaseRequisitions)).SingleOrDefaultAsync(x => x.PrNumber == Normalize(number), ct)
            ?? throw new Rev869BNotFoundException("Purchase requisition not found.");
        if (pr.Status != PurchaseRequisitionStatuses.PendingApproval) throw new Rev869BConflictException("PR must be pending approval.");
        ValidateRequest(request, pr.Version);
        var priorEmployee = pr.CompletedApprovalStepCount == 1
            ? await db.PurchaseRequisitionApprovalHistories.AsNoTracking()
                .Where(x => x.PurchaseRequisitionId == pr.Id && x.ApprovalCycle == pr.ApprovalCycle && x.StepNumber == 1 && x.Action == "Approve")
                .Select(x => (Guid?)x.ResolvedEmployeeId).SingleOrDefaultAsync(ct)
            : null;
        PurchaseApprovalDecision decision;
        try
        {
            decision = workflow.AuthorizeNextStep(pr.ApprovalWorkflowSnapshotJson, pr.ApprovalCycle,
                pr.CompletedApprovalStepCount, pr.CreatorEmployeeId, RequireActor(), user.RoleCodes, priorEmployee);
        }
        catch (UnauthorizedAccessException ex)
        {
            await audit.WriteAsync("Security", "Denied", nameof(PurchaseRequisition), pr.Id.ToString(),
                new { pr.Status }, new { reason = ex.Message, user.RoleCodes }, ct);
            throw;
        }
        var correlation = Correlation(request, action.ToUpperInvariant());
        if (await db.PurchaseRequisitionApprovalHistories.AnyAsync(x => x.PurchaseRequisitionId == pr.Id && x.CorrelationId == correlation, ct))
            throw new Rev869BConflictException("The approval decision was already recorded.");
        var next = action switch
        {
            "Reject" => PurchaseRequisitionStatuses.Rejected,
            "RequestRevision" => PurchaseRequisitionStatuses.RevisionRequested,
            _ when decision.CompletesDocument => PurchaseRequisitionStatuses.StockCheckPending,
            _ => PurchaseRequisitionStatuses.PendingApproval
        };
        if (action == "Approve") pr.CompletedApprovalStepCount = decision.CompletedStepCount;
        if (action == "Approve" && decision.CompletesDocument) { pr.ApprovedBy = user.LoginId; pr.ApprovedAt = DateTimeOffset.UtcNow; }
        AddApproval(pr, action, pr.Status, next, request.Remarks, correlation, decision);
        SetStatus(pr, next, request.Remarks, correlation, decision.ResolvedRoleCode);
        pr.Version = checked(pr.Version + 1);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Purchase", action, nameof(PurchaseRequisition), pr.Id.ToString(), null,
            new { pr.PrNumber, pr.ApprovalRoute, decision.StepNumber, ActingRole = decision.ResolvedRoleCode }, ct);
        await tx.CommitAsync(ct);
        return ToDetail(await ReloadAsync(pr.Id, ct));
    }

    private async Task<PurchaseRequisitionDetail> ChangeStatusAsync(string number, PurchaseRequisitionActionRequest request,
        string action, string? requiredStatus, string nextStatus, string module, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var pr = await Scoped(IncludeDetail(db.PurchaseRequisitions)).SingleOrDefaultAsync(x => x.PrNumber == Normalize(number), ct)
            ?? throw new Rev869BNotFoundException("Purchase requisition not found.");
        ValidateRequest(request, pr.Version);
        if (requiredStatus is not null && pr.Status != requiredStatus)
            throw new Rev869BConflictException($"Invalid PR status sequence. Required: {requiredStatus}.");
        var correlation = Correlation(request, action);
        if (await db.PurchaseRequisitionStatusHistories.AnyAsync(x => x.PurchaseRequisitionId == pr.Id && x.CorrelationId == correlation, ct))
            return ToDetail(pr);
        if (action is "Submit" or "Resubmit")
        {
            if (!pr.RequestingDepartmentId.HasValue) throw new Rev869BConflictException("Requesting department is required for approval workflow selection.");
            var snapshot = await workflow.SelectAndSnapshotAsync(pr.OrganizationId, pr.RequestingDepartmentId.Value, pr.EstimatedTotal, ct);
            pr.ApprovalRoute = snapshot.RouteCode;
            pr.ApprovalCycle = checked(pr.ApprovalCycle + 1);
            pr.RequiredApprovalStepCount = snapshot.Steps.Count;
            pr.CompletedApprovalStepCount = 0;
            pr.ApprovalWorkflowSnapshotJson = workflow.Serialize(snapshot);
            if (pr.CreatorEmployeeId == Guid.Empty) pr.CreatorEmployeeId = pr.RequesterEmployeeId ?? RequireActor();
        }
        if (action == "DepartmentVerify") { pr.VerifiedBy = user.LoginId; pr.VerifiedAt = DateTimeOffset.UtcNow; }
        if (action == "Submit") { pr.SubmittedBy = user.LoginId; pr.SubmittedAt = DateTimeOffset.UtcNow; }
        SetStatus(pr, nextStatus, request.Remarks, correlation);
        pr.Version = checked(pr.Version + 1);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(module, action, nameof(PurchaseRequisition), pr.Id.ToString(), null, new { pr.PrNumber, nextStatus }, ct);
        await tx.CommitAsync(ct);
        return ToDetail(await ReloadAsync(pr.Id, ct));
    }

    private void ValidateRequest(PurchaseRequisitionActionRequest request, uint currentVersion)
    {
        _ = RequireActor();
        if (string.IsNullOrWhiteSpace(request.Remarks)) throw new Rev869BValidationException("Remarks are required.");
        if (request.Version != currentVersion) throw new Rev869BConflictException("Stale record version. Refresh and retry.");
    }

    private Guid RequireActor() => user.IsAuthenticated && user.EmployeeId.HasValue && !string.IsNullOrWhiteSpace(user.OrganizationId)
        ? user.EmployeeId.Value : throw new UnauthorizedAccessException("Authenticated employee identity and organization are required.");
    private IQueryable<PurchaseRequisition> Scoped(IQueryable<PurchaseRequisition> query)
    {
        var employeeId = RequireActor();
        query = query.Where(x => x.OrganizationId == user.OrganizationId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var scopes = db.EmployeeOperationalScopes.Where(x => x.OrganizationId == user.OrganizationId && x.EmployeeId == employeeId && x.IsActive && x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today));
        if (user.RoleCodes.Any(Rev869ARoleCodes.IsExplicitCrossScopeRole) && scopes.Any(x => x.AllowsPrivilegedCrossScope)) return query;
        return query.Where(pr => scopes.Any(scope =>
            (!scope.DepartmentId.HasValue || scope.DepartmentId == pr.RequestingDepartmentId) &&
            (!scope.WarehouseId.HasValue || scope.WarehouseId == pr.DeliveryWarehouseId) &&
            !scope.RackBinId.HasValue && (!scope.OwnRecordsOnly || pr.RequesterEmployeeId == employeeId)));
    }

    private static IQueryable<PurchaseRequisition> IncludeDetail(IQueryable<PurchaseRequisition> query) =>
        query.Include(x => x.RequestingDepartment).Include(x => x.RequesterEmployee).Include(x => x.DeliveryWarehouse).Include(x => x.Lines).ThenInclude(x => x.Item);
    private async Task<PurchaseRequisition> ReloadAsync(Guid id, CancellationToken ct) => await IncludeDetail(db.PurchaseRequisitions.AsNoTracking()).SingleAsync(x => x.Id == id, ct);
    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string Correlation(PurchaseRequisitionActionRequest request, string action) => string.IsNullOrWhiteSpace(request.IdempotencyKey) ? $"REV868_{action}_{Guid.NewGuid():N}" : request.IdempotencyKey.Trim();
    private void SetStatus(PurchaseRequisition pr, string next, string reason, string correlation, string? roleCode = null)
    {
        var previous = pr.Status; pr.Status = next; pr.UpdatedBy = user.LoginId; pr.UpdatedAt = DateTimeOffset.UtcNow;
        db.PurchaseRequisitionStatusHistories.Add(new PurchaseRequisitionStatusHistory { CompanyId = pr.CompanyId, PurchaseRequisitionId = pr.Id, PurchaseRequisition = pr, PrNumber = pr.PrNumber, PreviousStatus = previous, NewStatus = next, Reason = reason.Trim(), ActorLoginId = user.LoginId, ActorRoleCode = roleCode ?? user.RoleCode, CorrelationId = correlation, CreatedBy = user.LoginId });
    }
    private void AddApproval(PurchaseRequisition pr, string action, string from, string to, string remarks, string correlation, PurchaseApprovalDecision decision) =>
        db.PurchaseRequisitionApprovalHistories.Add(new PurchaseRequisitionApprovalHistory { CompanyId = pr.CompanyId, PurchaseRequisitionId = pr.Id, PurchaseRequisition = pr, PrNumber = pr.PrNumber, Action = action, FromStatus = from, ToStatus = to, ApprovalRoute = decision.RouteCode, ApprovalCycle = decision.ApprovalCycle, StepNumber = decision.StepNumber, RequiredApprovalStepCount = decision.RequiredStepCount, ResolvedEmployeeId = decision.ResolvedEmployeeId, ResolvedRoleCode = decision.ResolvedRoleCode, SnapshotIdentity = decision.SnapshotIdentity, Remarks = remarks.Trim(), ActorLoginId = user.LoginId, ActorRoleCode = decision.ResolvedRoleCode, CorrelationId = correlation, CreatedBy = user.LoginId });
    private static PurchaseRequisitionDetail ToDetail(PurchaseRequisition x) => new(x.Id, x.PrNumber, x.OrganizationId, x.RequestingDepartment?.Name ?? string.Empty, x.RequesterEmployee?.EmployeeCode ?? string.Empty, x.RequestDate, x.RequiredByDate, x.Priority, x.PurposeJustification, x.DeliveryWarehouse?.WarehouseCode ?? string.Empty, x.CostCentre, x.ProjectReference, x.ServiceReference, x.WorkOrderReference, x.CustomerReference, x.Status, x.ApprovalRoute, x.EstimatedTotal, x.Version, x.Lines.OrderBy(l => l.LineNumber).Select(l => new PurchaseRequisitionLineSummary(l.Id, l.LineNumber, l.ItemCodeSnapshot, l.ItemNameSnapshot, l.UomSnapshot, l.RequestedQuantity, l.EstimatedUnitPriceSnapshot, l.EstimatedLineTotal, l.OnHandSnapshot, l.ActiveReservedSnapshot, l.AvailableSnapshot, l.ReservedQuantity, l.ShortageQuantity, l.ProcurementHandoffQuantity, l.LineStatus)).ToList());
}
