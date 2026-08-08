using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Inventory;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class PurchaseRequisitionEndpoints
{
    private const string PageRequisitions = "purchase.requisitions";
    private const string PageApprovals = "purchase.requisition-approvals";
    private const string PageStockCheck = "stores.stock-check";
    private const string PageReservations = "stores.reservations";
    private const string PageHandoff = "purchase.requirement-handoff";

    public static IEndpointRouteBuilder MapPurchaseRequisitionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/purchase/requisitions").WithTags("Purchase Requisitions").RequireAuthorization();

        group.MapGet("", async (NexaErpDbContext db, ICurrentUser user, int? page, int? pageSize, string? search, string? status, string? sortBy, string? sortDirection, CancellationToken ct) =>
        {
            var p = MasterEndpointHelpers.NormalizePaging(page, pageSize);
            var q = Scope(db.PurchaseRequisitions.AsNoTracking().Include(x => x.RequestingDepartment).Include(x => x.RequesterEmployee), user);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToUpperInvariant();
                q = q.Where(x => x.PrNumber.ToUpper().Contains(s) || x.PurposeJustification.ToUpper().Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.Trim());
            q = Sort(q, sortBy, sortDirection);
            var total = await q.CountAsync(ct);
            var rows = await q.Skip(p.Skip).Take(p.PageSize).Select(x => ToSummary(x)).ToListAsync(ct);
            return Results.Ok(new PagedResponse<PurchaseRequisitionSummary>(total, p.PageNumber, p.PageSize, rows));
        }).RequirePagePermission(PageRequisitions, PagePermissionActions.View);

        group.MapGet("/{prNumber}", async (string prNumber, NexaErpDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            var pr = await Scope(IncludeDetail(db.PurchaseRequisitions.AsNoTracking()), user).SingleOrDefaultAsync(x => x.PrNumber == NormalizePr(prNumber), ct);
            return pr is null ? Results.NotFound(new { message = "Purchase requisition not found." }) : Results.Ok(ToDetail(pr));
        }).RequirePagePermission(PageRequisitions, PagePermissionActions.View);

        group.MapPost("", async (CreatePurchaseRequisitionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) =>
        {
            var validation = await ValidateDraftAsync(request, db, ct);
            if (validation is not null) return validation;
            var pr = await BuildDraftAsync(request, db, user, ct);
            pr.PrNumber = await NextPrNumberAsync(db, ct);
            AddStatus(db, pr, null, PurchaseRequisitionStatuses.Draft, "Draft created", user, Correlation("CREATE"));
            db.PurchaseRequisitions.Add(pr);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Purchase", "CreateDraft", nameof(PurchaseRequisition), pr.Id.ToString(), null, new { pr.PrNumber, pr.EstimatedTotal }, ct);
            return Results.Created($"/api/v1/purchase/requisitions/{pr.PrNumber}", ToDetail(await Reload(pr.Id, db, ct)));
        }).RequirePagePermission(PageRequisitions, PagePermissionActions.Create);

        group.MapPut("/{prNumber}", async (string prNumber, UpdatePurchaseRequisitionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) =>
        {
            var pr = await IncludeDetail(db.PurchaseRequisitions).SingleOrDefaultAsync(x => x.PrNumber == NormalizePr(prNumber), ct);
            if (pr is null) return Results.NotFound(new { message = "Purchase requisition not found." });
            if (pr.Status != PurchaseRequisitionStatuses.Draft && pr.Status != PurchaseRequisitionStatuses.RevisionRequested) return Results.Conflict(new { message = "Only draft or revision-requested PR can be updated." });
            if (request.Version != pr.Version) return Results.Conflict(new { message = "Stale record version. Refresh and retry." });
            var validation = await ValidateDraftAsync(request, db, ct);
            if (validation is not null) return validation;
            var before = ToDetail(pr);
            await ApplyDraftAsync(pr, request, db, user, ct);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Purchase", "UpdateDraft", nameof(PurchaseRequisition), pr.Id.ToString(), before, ToDetail(pr), ct);
            return Results.Ok(ToDetail(await Reload(pr.Id, db, ct)));
        }).RequirePagePermission(PageRequisitions, PagePermissionActions.Update);

        group.MapPost("/{prNumber}/submit", (string prNumber, PurchaseRequisitionActionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) => ChangeStatus(prNumber, request, db, user, audit, "Submit", PurchaseRequisitionStatuses.Draft, PurchaseRequisitionStatuses.Submitted, PageRequisitions, ct)).RequirePagePermission(PageRequisitions, PagePermissionActions.Submit);
        group.MapPost("/{prNumber}/verify", (string prNumber, PurchaseRequisitionActionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) => Verify(prNumber, request, db, user, audit, ct)).RequirePagePermission(PageRequisitions, PagePermissionActions.Verify);
        group.MapPost("/{prNumber}/approve", (string prNumber, PurchaseRequisitionActionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) => Approve(prNumber, request, db, user, audit, ct)).RequirePagePermission(PageApprovals, PagePermissionActions.Approve);
        group.MapPost("/{prNumber}/reject", (string prNumber, PurchaseRequisitionActionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) => ChangeStatus(prNumber, request, db, user, audit, "Reject", PurchaseRequisitionStatuses.PendingApproval, PurchaseRequisitionStatuses.Rejected, PageApprovals, ct)).RequirePagePermission(PageApprovals, PagePermissionActions.Reject);
        group.MapPost("/{prNumber}/request-revision", (string prNumber, PurchaseRequisitionActionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) => ChangeStatus(prNumber, request, db, user, audit, "RequestRevision", PurchaseRequisitionStatuses.PendingApproval, PurchaseRequisitionStatuses.RevisionRequested, PageApprovals, ct)).RequirePagePermission(PageApprovals, PagePermissionActions.RequestRevision);
        group.MapPost("/{prNumber}/resubmit", (string prNumber, PurchaseRequisitionActionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) => ChangeStatus(prNumber, request, db, user, audit, "Resubmit", PurchaseRequisitionStatuses.RevisionRequested, PurchaseRequisitionStatuses.Submitted, PageRequisitions, ct)).RequirePagePermission(PageRequisitions, PagePermissionActions.Resubmit);
        group.MapPost("/{prNumber}/cancel", (string prNumber, PurchaseRequisitionActionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) => ChangeStatus(prNumber, request, db, user, audit, "Cancel", null, PurchaseRequisitionStatuses.Cancelled, PageRequisitions, ct)).RequirePagePermission(PageRequisitions, PagePermissionActions.Cancel);
        group.MapPost("/{prNumber}/hold", (string prNumber, PurchaseRequisitionActionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) => ChangeStatus(prNumber, request, db, user, audit, "Hold", null, PurchaseRequisitionStatuses.Held, PageApprovals, ct)).RequirePagePermission(PageApprovals, PagePermissionActions.Update);

        group.MapPost("/{prNumber}/stock-check", StockCheck).RequirePagePermission(PageStockCheck, PagePermissionActions.Verify);
        group.MapGet("/{prNumber}/status-history", (string prNumber, NexaErpDbContext db, CancellationToken ct) => History(db, prNumber, ct)).RequirePagePermission(PageRequisitions, PagePermissionActions.ViewAuditHistory);
        group.MapGet("/{prNumber}/approval-history", (string prNumber, NexaErpDbContext db, CancellationToken ct) => ApprovalHistory(db, prNumber, ct)).RequirePagePermission(PageApprovals, PagePermissionActions.ViewAuditHistory);
        group.MapGet("/reservations", Reservations).RequirePagePermission(PageReservations, PagePermissionActions.View);
        group.MapGet("/handoffs", Handoffs).RequirePagePermission(PageHandoff, PagePermissionActions.View);
        return endpoints;
    }

    private static async Task<IResult> Verify(string prNumber, PurchaseRequisitionActionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        return await ChangeStatus(prNumber, request, db, user, audit, "DepartmentVerify", PurchaseRequisitionStatuses.Submitted, PurchaseRequisitionStatuses.PendingApproval, PageRequisitions, ct);
    }

    private static async Task<IResult> Approve(string prNumber, PurchaseRequisitionActionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var pr = await IncludeDetail(db.PurchaseRequisitions).SingleOrDefaultAsync(x => x.PrNumber == NormalizePr(prNumber), ct);
        if (pr is null) return Results.NotFound(new { message = "Purchase requisition not found." });
        if (pr.Status != PurchaseRequisitionStatuses.PendingApproval) return Results.Conflict(new { message = "PR must be pending approval." });
        if (request.Version != pr.Version) return Results.Conflict(new { message = "Stale record version. Refresh and retry." });
        if (string.IsNullOrWhiteSpace(request.Remarks)) return Results.BadRequest(new { message = "Remarks are required." });
        if (string.Equals(pr.CreatedBy, user.LoginId, StringComparison.OrdinalIgnoreCase) || string.Equals(pr.SubmittedBy, user.LoginId, StringComparison.OrdinalIgnoreCase))
        {
            await audit.WriteAsync("Security", "Denied", nameof(PurchaseRequisition), pr.Id.ToString(), new { pr.Status }, new { reason = "Self approval blocked", user.RoleCode }, ct);
            return Results.Forbid();
        }
        var expected = RouteFor(pr.EstimatedTotal);
        if (!CanApproveRoute(user.RoleCode, expected))
        {
            await audit.WriteAsync("Security", "Denied", nameof(PurchaseRequisition), pr.Id.ToString(), new { pr.Status }, new { reason = "Approval route mismatch", expected, user.RoleCode }, ct);
            return Results.Forbid();
        }
        var correlation = Idempotency(request, "APPROVE");
        if (await db.PurchaseRequisitionApprovalHistories.AnyAsync(x => x.PurchaseRequisitionId == pr.Id && x.CorrelationId == correlation, ct)) return Results.Ok(ToDetail(pr));
        pr.ApprovalRoute = expected;
        pr.ApprovedBy = user.LoginId;
        pr.ApprovedAt = DateTimeOffset.UtcNow;
        AddApproval(db, pr, "Approve", pr.Status, PurchaseRequisitionStatuses.Approved, request.Remarks, user, correlation);
        SetStatus(db, pr, PurchaseRequisitionStatuses.Approved, request.Remarks, user, correlation);
        SetStatus(db, pr, PurchaseRequisitionStatuses.StockCheckPending, "Approved PR moved to stores stock-check queue", user, correlation);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Purchase", "Approve", nameof(PurchaseRequisition), pr.Id.ToString(), null, new { pr.PrNumber, pr.ApprovalRoute }, ct);
        return Results.Ok(ToDetail(await Reload(pr.Id, db, ct)));
    }

    private static async Task<IResult> ChangeStatus(string prNumber, PurchaseRequisitionActionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, string action, string? requiredStatus, string nextStatus, string page, CancellationToken ct)
    {
        var pr = await IncludeDetail(db.PurchaseRequisitions).SingleOrDefaultAsync(x => x.PrNumber == NormalizePr(prNumber), ct);
        if (pr is null) return Results.NotFound(new { message = "Purchase requisition not found." });
        if (string.IsNullOrWhiteSpace(request.Remarks)) return Results.BadRequest(new { message = "Remarks are required." });
        if (request.Version != pr.Version) return Results.Conflict(new { message = "Stale record version. Refresh and retry." });
        if (requiredStatus is not null && pr.Status != requiredStatus) return Results.Conflict(new { message = $"Invalid PR status sequence. Required: {requiredStatus}." });
        var correlation = Idempotency(request, action);
        if (await db.PurchaseRequisitionStatusHistories.AnyAsync(x => x.PurchaseRequisitionId == pr.Id && x.CorrelationId == correlation, ct)) return Results.Ok(ToDetail(pr));
        if (action == "DepartmentVerify") { pr.VerifiedBy = user.LoginId; pr.VerifiedAt = DateTimeOffset.UtcNow; pr.ApprovalRoute = RouteFor(pr.EstimatedTotal); }
        if (action is "Reject" or "RequestRevision") AddApproval(db, pr, action, pr.Status, nextStatus, request.Remarks, user, correlation);
        if (action == "Submit") { pr.SubmittedBy = user.LoginId; pr.SubmittedAt = DateTimeOffset.UtcNow; }
        SetStatus(db, pr, nextStatus, request.Remarks, user, correlation);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(page.StartsWith("stores", StringComparison.OrdinalIgnoreCase) ? "Stores" : "Purchase", action, nameof(PurchaseRequisition), pr.Id.ToString(), null, new { pr.PrNumber, nextStatus }, ct);
        return Results.Ok(ToDetail(await Reload(pr.Id, db, ct)));
    }
}
