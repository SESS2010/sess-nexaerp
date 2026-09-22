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

    private static async Task<IResult> CreateDraft(
        CreatePurchaseRequisitionRequest request, HttpContext http, NexaErpDbContext db,
        ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var validation = await ValidateDraftAsync(request, db, user, ct);
        if (validation is not null) return validation;
        var key = http.Request.Headers["Idempotency-Key"].ToString().Trim();
        if (key.Length is 0 or > 200)
            return Results.BadRequest(new { message = "Idempotency-Key header is required and must not exceed 200 characters." });
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var organization = user.OrganizationId!;
            var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
                organization, "PurchaseRequisition.Create", key, request);
            var attempt = await Rev869BCommandContextAuthorizer.OpenForCreationAsync(
                db, user, organization, envelope, nameof(PurchaseRequisition), ct);
            using var receipt = await Rev869BCommandContextAuthorizer.ReadCommittedReceiptAsync(db, attempt, ct);
            if (receipt is not null)
            {
                var id = receipt.RootElement.GetProperty("EntityId").GetGuid();
                if (id != attempt.CommandId ||
                    receipt.RootElement.GetProperty("EntityType").GetString() != nameof(PurchaseRequisition))
                    throw new InvalidOperationException("The PR creation receipt has inconsistent entity evidence.");
                var existing = await Scope(IncludeDetail(db.PurchaseRequisitions.AsNoTracking()), user, db)
                    .SingleOrDefaultAsync(x => x.Id == id && x.CreatorEmployeeId == user.EmployeeId, ct)
                    ?? throw new InvalidOperationException("The PR creation receipt has no accessible document.");
                await tx.CommitAsync(ct);
                return Results.Created($"/api/v1/purchase/requisitions/{existing.PrNumber}", ToDetail(existing));
            }
            var pr = await BuildDraftAsync(request, db, user, ct, attempt.CommandId);
            pr.CreatorEmployeeId = user.EmployeeId!.Value;
            (pr.PrNumber, pr.PrSequence) = await NextPrNumberAsync(db, pr.CompanyId, pr.OrganizationId, pr.RequestDate, user, ct);
            AddStatus(db, pr, null, PurchaseRequisitionStatuses.Draft, "Draft created", user, Correlation("CREATE"));
            db.PurchaseRequisitions.Add(pr);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Purchase", "CreateDraft", nameof(PurchaseRequisition), pr.Id.ToString(), null,
                new { pr.PrNumber, pr.EstimatedTotal }, ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct,
                new { EntityType = nameof(PurchaseRequisition), EntityId = pr.Id });
            await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/purchase/requisitions/{pr.PrNumber}", ToDetail(await Reload(pr.Id, db, ct)));
        }
        catch (Npgsql.PostgresException error) when (error.ConstraintName == "command_request_replay_mismatch")
        {
            return Results.Conflict(new { message = "Idempotency-Key was already used with different content or actor authority." });
        }
        catch (Npgsql.PostgresException error) when (error.ConstraintName == "UQ_command_request_idempotency")
        {
            return Results.Conflict(new { message = "The creation request changed concurrently. Retry the original request." });
        }
    }

    public static IEndpointRouteBuilder MapPurchaseRequisitionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/purchase/requisitions").WithTags("Purchase Requisitions").RequireAuthorization();
        var stockCheckGroup = endpoints.MapGroup("/api/v1/stores/stock-check").WithTags("Stores Stock Check").RequireAuthorization();

        group.MapGet("/lookups/departments", DepartmentLookups).RequirePagePermission(PageRequisitions, PagePermissionActions.Create);
        group.MapGet("/lookups/warehouses", WarehouseLookups).RequirePagePermission(PageRequisitions, PagePermissionActions.Create);
        group.MapGet("/lookups/items", ItemLookups).RequirePagePermission(PageRequisitions, PagePermissionActions.Create);

        group.MapGet("", (NexaErpDbContext db, ICurrentUser user, int? page, int? pageSize, string? search, string? prNumber, string? status, string? sortBy, string? sortDirection, CancellationToken ct) =>
            ListRequisitions(db, user, page, pageSize, search, prNumber, status, sortBy, sortDirection, false, ct))
            .RequirePagePermission(PageRequisitions, PagePermissionActions.View);

        stockCheckGroup.MapGet("/requisitions", (NexaErpDbContext db, ICurrentUser user, int? page, int? pageSize, string? search, string? prNumber, string? status, string? sortBy, string? sortDirection, CancellationToken ct) =>
            ListRequisitions(db, user, page, pageSize, search, prNumber, status, sortBy, sortDirection, true, ct))
            .RequirePagePermission(PageStockCheck, PagePermissionActions.Verify);

        stockCheckGroup.MapGet("/requisitions/{prNumber}", async (string prNumber, NexaErpDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            var organizationId = user.OrganizationId;
            if (string.IsNullOrWhiteSpace(organizationId)) return Results.NotFound(new { message = "Purchase requisition not found." });
            var pr = await db.PurchaseRequisitions.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId &&
                    x.Status == PurchaseRequisitionStatuses.StockCheckPending &&
                    x.PrNumber == NormalizePr(prNumber))
                .Select(x => new StockCheckPurchaseRequisitionDetail(
                    x.PrNumber,
                    x.Status,
                    x.Version,
                    x.DeliveryWarehouse != null ? x.DeliveryWarehouse.WarehouseCode : string.Empty,
                    x.Lines.OrderBy(line => line.LineNumber).Select(line => new StockCheckPurchaseRequisitionLine(
                        line.LineNumber,
                        line.ItemCodeSnapshot,
                        line.ItemNameSnapshot,
                        line.UomSnapshot,
                        line.RequestedQuantity)).ToList()))
                .SingleOrDefaultAsync(ct);
            return pr is null ? Results.NotFound(new { message = "Purchase requisition not found." }) : Results.Ok(pr);
        }).RequirePagePermission(PageStockCheck, PagePermissionActions.Verify);

        group.MapGet("/{prNumber}", async (string prNumber, NexaErpDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            var pr = await Scope(IncludeDetail(db.PurchaseRequisitions.AsNoTracking()), user, db).SingleOrDefaultAsync(x => x.PrNumber == NormalizePr(prNumber), ct);
            return pr is null ? Results.NotFound(new { message = "Purchase requisition not found." }) : Results.Ok(ToDetail(pr));
        }).RequirePagePermission(PageRequisitions, PagePermissionActions.View);

        group.MapPost("", (CreatePurchaseRequisitionRequest request, HttpContext http, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) =>
            CreateDraft(request, http, db, user, audit, ct))
            .RequirePagePermission(PageRequisitions, PagePermissionActions.Create);

        group.MapPut("/{prNumber}", async (string prNumber, UpdatePurchaseRequisitionRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) =>
        {
            var pr = await Scope(IncludeDetail(db.PurchaseRequisitions), user, db).SingleOrDefaultAsync(x => x.PrNumber == NormalizePr(prNumber), ct);
            if (pr is null) return Results.NotFound(new { message = "Purchase requisition not found." });
            if (pr.Status != PurchaseRequisitionStatuses.Draft && pr.Status != PurchaseRequisitionStatuses.RevisionRequested) return Results.Conflict(new { message = "Only draft or revision-requested PR can be updated." });
            if (request.Version != pr.Version) return Results.Conflict(new { message = "Stale record version. Refresh and retry." });
            var validation = await ValidateDraftAsync(request, pr.CompanyId, db, user, ct);
            if (validation is not null) return validation;
            var before = ToDetail(pr);
            await ApplyDraftAsync(pr, request, db, user, ct);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Purchase", "UpdateDraft", nameof(PurchaseRequisition), pr.Id.ToString(), before, ToDetail(pr), ct);
            return Results.Ok(ToDetail(await Reload(pr.Id, db, ct)));
        }).RequirePagePermission(PageRequisitions, PagePermissionActions.Update);

        group.MapPost("/{prNumber}/submit", (string prNumber, PurchaseRequisitionActionRequest request, IPurchaseRequisitionWorkflowService service, CancellationToken ct) => RunWorkflow(() => service.SubmitAsync(prNumber, request, ct))).RequirePagePermission(PageRequisitions, PagePermissionActions.Submit);
        group.MapPost("/{prNumber}/verify", (string prNumber, PurchaseRequisitionActionRequest request, IPurchaseRequisitionWorkflowService service, CancellationToken ct) => RunWorkflow(() => service.VerifyAsync(prNumber, request, ct))).RequirePagePermission(PageRequisitions, PagePermissionActions.Verify);
        group.MapPost("/{prNumber}/approve", (string prNumber, PurchaseRequisitionActionRequest request, IPurchaseRequisitionWorkflowService service, CancellationToken ct) => RunWorkflow(() => service.ApproveAsync(prNumber, request, ct))).RequirePagePermission(PageApprovals, PagePermissionActions.Approve);
        group.MapPost("/{prNumber}/reject", (string prNumber, PurchaseRequisitionActionRequest request, IPurchaseRequisitionWorkflowService service, CancellationToken ct) => RunWorkflow(() => service.RejectAsync(prNumber, request, ct))).RequirePagePermission(PageApprovals, PagePermissionActions.Reject);
        group.MapPost("/{prNumber}/request-revision", (string prNumber, PurchaseRequisitionActionRequest request, IPurchaseRequisitionWorkflowService service, CancellationToken ct) => RunWorkflow(() => service.RequestRevisionAsync(prNumber, request, ct))).RequirePagePermission(PageApprovals, PagePermissionActions.RequestRevision);
        group.MapPost("/{prNumber}/resubmit", (string prNumber, PurchaseRequisitionActionRequest request, IPurchaseRequisitionWorkflowService service, CancellationToken ct) => RunWorkflow(() => service.ResubmitAsync(prNumber, request, ct))).RequirePagePermission(PageRequisitions, PagePermissionActions.Resubmit);
        group.MapPost("/{prNumber}/cancel", (string prNumber, PurchaseRequisitionActionRequest request, IPurchaseRequisitionWorkflowService service, CancellationToken ct) => RunWorkflow(() => service.CancelAsync(prNumber, request, ct))).RequirePagePermission(PageRequisitions, PagePermissionActions.Cancel);
        group.MapPost("/{prNumber}/hold", (string prNumber, PurchaseRequisitionActionRequest request, IPurchaseRequisitionWorkflowService service, CancellationToken ct) => RunWorkflow(() => service.HoldAsync(prNumber, request, ct))).RequirePagePermission(PageApprovals, PagePermissionActions.Update);

        group.MapPost("/{prNumber}/stock-check", StockCheck).RequirePagePermission(PageStockCheck, PagePermissionActions.Verify);
        group.MapGet("/{prNumber}/status-history", (string prNumber, NexaErpDbContext db, ICurrentUser user, CancellationToken ct) => History(db, prNumber, user, ct)).RequirePagePermission(PageRequisitions, PagePermissionActions.ViewAuditHistory);
        group.MapGet("/{prNumber}/approval-history", (string prNumber, NexaErpDbContext db, ICurrentUser user, CancellationToken ct) => ApprovalHistory(db, prNumber, user, ct)).RequirePagePermission(PageApprovals, PagePermissionActions.ViewAuditHistory);
        group.MapGet("/reservations", Reservations).RequirePagePermission(PageReservations, PagePermissionActions.View);
        group.MapGet("/handoffs", Handoffs).RequirePagePermission(PageHandoff, PagePermissionActions.View);
        return endpoints;
    }

    private static async Task<IResult> ListRequisitions(
        NexaErpDbContext db,
        ICurrentUser user,
        int? page,
        int? pageSize,
        string? search,
        string? prNumber,
        string? status,
        string? sortBy,
        string? sortDirection,
        bool stockCheckPendingOnly,
        CancellationToken ct)
    {
        var p = MasterEndpointHelpers.NormalizePaging(page, pageSize);
        var q = Scope(db.PurchaseRequisitions.AsNoTracking()
            .Include(x => x.RequestingDepartment)
            .Include(x => x.RequesterEmployee), user, db);
        if (stockCheckPendingOnly)
            q = q.Where(x => x.Status == PurchaseRequisitionStatuses.StockCheckPending);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpperInvariant();
            q = q.Where(x => x.PrNumber.ToUpper().Contains(s) || x.PurposeJustification.ToUpper().Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(prNumber)) q = q.Where(x => x.PrNumber == NormalizePr(prNumber));
        q = Sort(q, sortBy, sortDirection);
        var total = await q.CountAsync(ct);
        var rows = await q.Skip(p.Skip).Take(p.PageSize).Select(x => ToSummary(x)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<PurchaseRequisitionSummary>(total, p.PageNumber, p.PageSize, rows));
    }

    private static async Task<IResult> RunWorkflow(Func<Task<PurchaseRequisitionDetail>> command)
    {
        try { return Results.Ok(await command()); }
        catch (Rev869BValidationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        catch (Rev869BNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
        catch (Rev869BConflictException ex) { return Results.Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden); }
    }
}
