using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class Rev869BPurchaseEndpoints
{
    public static IEndpointRouteBuilder MapRev869BPurchaseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/purchase").WithTags("REV869B Purchase Transactions").RequireAuthorization().AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapPost("/rfqs", (Rev869BCreateRfqRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.CreateRfqAsync(r, ct), h, ct)).RequirePagePermission("purchase.rfq", PagePermissionActions.Create);
        group.MapPost("/rfqs/{number}/vendors", (string number, Rev869BInviteVendorRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.InviteVendorAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.rfq", PagePermissionActions.Submit);
        group.MapPost("/rfq-invitations/{id:guid}/quotations", (Guid id, Rev869BSubmitQuotationRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.SubmitQuotationRevisionAsync(id, r, ct), h, ct)).RequirePagePermission("purchase.vendor-quotations", PagePermissionActions.Create);
        group.MapPost("/quotations/{number}/technical-verifications", (string number, Rev869BTechnicalVerificationRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.VerifyTechnicalAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.technical-verification", PagePermissionActions.Verify);
        group.MapPost("/comparisons", (Rev869BCreateComparisonRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.CreateComparisonAsync(r, ct), h, ct)).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.Create);
        group.MapPost("/comparisons/{number}/recommend", (string number, Rev869BRecommendComparisonRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.RecommendAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.Submit);
        group.MapPost("/comparisons/{number}/approve", (string number, Rev869BApprovalActionRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.ApproveAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.Approve);
        group.MapPost("/comparisons/{number}/reject", (string number, Rev869BApprovalActionRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.RejectAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.Reject);
        group.MapPost("/comparisons/{number}/request-revision", (string number, Rev869BApprovalActionRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.RequestRevisionAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.RequestRevision);
        group.MapPost("/comparisons/{number}/resubmit", (string number, Rev869BApprovalActionRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.ResubmitAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.Resubmit);
        group.MapPost("/purchase-orders", (Rev869BCreatePurchaseOrderRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.CreatePurchaseOrderAsync(r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Create);
        group.MapPost("/purchase-orders/{number}/submit", (string number, Rev869BSubmitPurchaseOrderRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.SubmitPurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Submit);
        group.MapPost("/purchase-orders/{number}/issue", (string number, Rev869BIssuePurchaseOrderRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.IssuePurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Submit);
        group.MapPost("/purchase-orders/{number}/amend", (string number, Rev869BAmendPurchaseOrderRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.AmendPurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Update);
        group.MapPost("/purchase-orders/{number}/revise-rejected", (string number, Rev869BReviseRejectedPurchaseOrderRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.ReviseRejectedPurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Update);
        group.MapPost("/purchase-orders/{number}/approve", (string number, Rev869BPoApprovalActionRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.ApprovePurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Approve);
        group.MapPost("/purchase-orders/{number}/reject", (string number, Rev869BPoApprovalActionRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.RejectPurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Reject);
        group.MapPost("/purchase-orders/{number}/cancel", (string number, Rev869BCancelPurchaseOrderRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.CancelPurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Cancel);
        group.MapGet("/rfqs/{number}", GetRfq).RequirePagePermission("purchase.rfq", PagePermissionActions.View);
        group.MapGet("/comparisons/{number}", GetComparison).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.View);
        group.MapGet("/purchase-orders/{number}", GetPo).RequirePagePermission("purchase.po", PagePermissionActions.View);
        group.MapGet("/material-followup", GetFollowUp).RequirePagePermission("purchase.material-followup", PagePermissionActions.View);
        return endpoints;
    }

    private static async Task<IResult> GetRfq(string number, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IAuditWriter audit, CancellationToken ct)
    {
        var row = await db.RequestForQuotations.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == user.OrganizationId && x.RfqNumber == number.Trim().ToUpper(), ct); if (row is null) return await Missing(audit, "purchase.rfq", number, user, ct); if (!await Allowed(user, scopes, row.OrganizationId, row.RequestingDepartmentId, row.DeliveryWarehouseId, row.OwnerEmployeeId, ct)) return await Denied(audit, "purchase.rfq", number, user, ct); return Results.Ok(row);
    }
    private static async Task<IResult> GetComparison(string number, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IPagePermissionService permissions, IAuditWriter audit, CancellationToken ct)
    {
        var row = await db.CommercialComparisons.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == user.OrganizationId && x.ComparisonNumber == number.Trim().ToUpper(), ct); if (row is null) return await Missing(audit, "purchase.commercial-comparisons", number, user, ct); var rfq = await db.RequestForQuotations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == row.RequestForQuotationId && x.OrganizationId == row.OrganizationId, ct); if (rfq is null) return Results.Conflict(new { message = "Comparison RFQ parent contract is invalid." }); if (!await Allowed(user, scopes, row.OrganizationId, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, row.OwnerEmployeeId, ct)) return await Denied(audit, "purchase.commercial-comparisons", number, user, ct);
        if (await permissions.HasPermissionAsync(user.RoleCode, "purchase.commercial-comparisons", PagePermissionActions.ViewCommercialValues, ct)) return Results.Ok(row);
        await audit.WriteAsync("Security", "Denied", "CommercialValues", row.Id.ToString(), null, new { reason = "Commercial values masked", user.RoleCode }, ct);
        return Results.Ok(new { row.Id, row.ComparisonNumber, row.RequestForQuotationId, row.OwnerEmployeeId, row.CurrencyCode, row.Status, row.IsSingleSource, row.SingleSourceJustification, row.RecommendationRemarks, row.Version, Lines = row.Lines.Select(x => new { x.Id, x.VendorQuotationLineId, x.TechnicalComplianceSnapshot, x.DeliverySnapshot, x.IsRecommended, x.RecommendationReason }) });
    }
    private static async Task<IResult> GetPo(string number, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IPagePermissionService permissions, IAuditWriter audit, CancellationToken ct)
    {
        var row = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == user.OrganizationId && x.PoNumber == number.Trim().ToUpper() && x.IsCurrentVersion, ct); if (row is null) return await Missing(audit, "purchase.po", number, user, ct); if (!await Allowed(user, scopes, row.OrganizationId, row.RequestingDepartmentId, row.DeliveryWarehouseId, row.OwnerEmployeeId, ct)) return await Denied(audit, "purchase.po", number, user, ct);
        if (await permissions.HasPermissionAsync(user.RoleCode, "purchase.po", PagePermissionActions.ViewCommercialValues, ct)) return Results.Ok(row);
        await audit.WriteAsync("Security", "Denied", "CommercialValues", row.Id.ToString(), null, new { reason = "Commercial values masked", user.RoleCode }, ct);
        return Results.Ok(new { row.Id, row.PoNumber, row.RevisionNumber, row.IsCurrentVersion, row.RequestingDepartmentId, row.DeliveryWarehouseId, row.OwnerEmployeeId, row.Status, row.CurrencyCode, row.IssuedAt, row.CancelledAt, row.CancellationReason, row.Version, Lines = row.Lines.Select(x => new { x.Id, x.LineNumber, x.ItemId, x.ItemCodeSnapshot, x.ItemNameSnapshot, x.UomSnapshot, x.OrderedQuantity }) });
    }
    private static async Task<IResult> GetFollowUp(int? page, int? pageSize, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IAuditWriter audit, CancellationToken ct)
    {
        if (!user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(user.OrganizationId)) return await Denied(audit, "purchase.material-followup", "list", user, ct);
        var pageNumber = page ?? 1; var take = pageSize ?? 50; if (pageNumber < 1 || take is < 1 or > 100) return Results.BadRequest(new { message = "page must be positive and pageSize must be 1-100." });
        var rows = await db.MaterialFollowUpHandoffs.AsNoTracking().Where(x => x.PurchaseOrder!.OrganizationId == user.OrganizationId && x.PurchaseOrder.IsCurrentVersion && x.PurchaseOrder.Status == Rev869BStatuses.Issued).OrderBy(x => x.HandoffAt).ThenBy(x => x.Id).Skip((pageNumber - 1) * take).Take(take).Select(x => new { x.Id, x.HandoffNumber, x.PurchaseOrderId, x.PurchaseOrderLineId, x.OrderedQuantitySnapshot, x.Status, x.HandoffAt, DepartmentId = x.PurchaseOrder!.RequestingDepartmentId, WarehouseId = x.PurchaseOrder.DeliveryWarehouseId, OwnerId = x.PurchaseOrder.OwnerEmployeeId }).ToListAsync(ct); var allowed = new List<object>(); foreach (var row in rows) if (await Allowed(user, scopes, user.OrganizationId, row.DepartmentId, row.WarehouseId, row.OwnerId, ct)) allowed.Add(row); else await audit.WriteAsync("Security", "Denied", "purchase.material-followup", row.Id.ToString(), null, new { reason = "Record scope denied", user.RoleCode }, ct); return Results.Ok(new { page = pageNumber, pageSize = take, items = allowed });
    }
    private static async Task<IResult> Denied(IAuditWriter audit, string page, string record, ICurrentUser user, CancellationToken ct) { await audit.WriteAsync("Security", "Denied", page, record, null, new { reason = "Record scope denied", user.RoleCode }, ct); return Results.Forbid(); }
    private static async Task<IResult> Missing(IAuditWriter audit, string page, string record, ICurrentUser user, CancellationToken ct) { await audit.WriteAsync("Security", "Denied", page, record, null, new { reason = "Scoped record missing or denied", user.RoleCode }, ct); return Results.NotFound(); }
    private static async Task<bool> Allowed(ICurrentUser user, IRecordScopeAuthorizer scopes, string organization, Guid? department, Guid? warehouse, Guid? owner, CancellationToken ct) => user.EmployeeId.HasValue && (await scopes.AuthorizeAsync(user.EmployeeId.Value, user.RoleCode, new RecordScopeTarget(organization, department, warehouse, null, owner), DateOnly.FromDateTime(DateTime.UtcNow), ct)).Allowed;
    public static async Task<IResult> Run(Func<Task<Rev869BDocumentResult>> action, HttpContext http, CancellationToken ct)
    {
        var audit = http.RequestServices.GetRequiredService<IAuditWriter>();
        var user = http.RequestServices.GetRequiredService<ICurrentUser>();
        async Task AuditDenied(string reason, string kind)
            => await audit.WriteAsync("Security", "Denied", kind, http.Request.Path, null,
                new { reason, user.RoleCode, user.EmployeeId, user.OrganizationId, method = http.Request.Method, correlationId = http.TraceIdentifier }, ct);
        try { return Results.Ok(await action()); }
        catch (UnauthorizedAccessException ex) { await AuditDenied(ex.Message, "Authorization"); return user.IsAuthenticated ? Results.Forbid() : Results.Unauthorized(); }
        catch (DbUpdateConcurrencyException ex) { await AuditDenied("Concurrent command rejected.", "Concurrency"); return Results.Conflict(new { message = ex.Message }); }
        catch (Rev869BNotFoundException ex) { await AuditDenied("Scoped record missing or denied.", "RecordScope"); return Results.NotFound(new { message = ex.Message }); }
        catch (Rev869BValidationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        catch (Rev869BConflictException ex) { await AuditDenied("Business or idempotency conflict rejected.", "Conflict"); return Results.Conflict(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        catch (OverflowException ex) { return Results.BadRequest(new { message = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }
}
