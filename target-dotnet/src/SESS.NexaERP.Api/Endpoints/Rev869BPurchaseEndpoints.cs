using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class Rev869BPurchaseEndpoints
{
    public static IEndpointRouteBuilder MapRev869BPurchaseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/purchase").WithTags("REV869B Purchase Transactions").RequireAuthorization().AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapPost("/rfqs", (Rev869BCreateRfqRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.CreateRfqAsync(r, ct))).RequirePagePermission("purchase.rfq", PagePermissionActions.Create);
        group.MapPost("/rfqs/{number}/vendors", (string number, Rev869BInviteVendorRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.InviteVendorAsync(number, r, ct))).RequirePagePermission("purchase.rfq", PagePermissionActions.Submit);
        group.MapPost("/rfq-invitations/{id:guid}/quotations", (Guid id, Rev869BSubmitQuotationRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.SubmitQuotationRevisionAsync(id, r, ct))).RequirePagePermission("purchase.vendor-quotations", PagePermissionActions.Create);
        group.MapPost("/quotations/{number}/technical-verifications", (string number, Rev869BTechnicalVerificationRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.VerifyTechnicalAsync(number, r, ct))).RequirePagePermission("purchase.technical-verification", PagePermissionActions.Verify);
        group.MapPost("/comparisons", (Rev869BCreateComparisonRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.CreateComparisonAsync(r, ct))).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.Create);
        group.MapPost("/comparisons/{number}/recommend", (string number, Rev869BRecommendComparisonRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.RecommendAsync(number, r, ct))).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.Submit);
        group.MapPost("/comparisons/{number}/approve", (string number, Rev869BApprovalActionRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.ApproveAsync(number, r, ct))).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.Approve);
        group.MapPost("/comparisons/{number}/reject", (string number, Rev869BApprovalActionRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.RejectAsync(number, r, ct))).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.Reject);
        group.MapPost("/comparisons/{number}/request-revision", (string number, Rev869BApprovalActionRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.RequestRevisionAsync(number, r, ct))).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.RequestRevision);
        group.MapPost("/comparisons/{number}/resubmit", (string number, Rev869BApprovalActionRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.ResubmitAsync(number, r, ct))).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.Resubmit);
        group.MapPost("/purchase-orders", (Rev869BCreatePurchaseOrderRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.CreatePurchaseOrderAsync(r, ct))).RequirePagePermission("purchase.po", PagePermissionActions.Create);
        group.MapPost("/purchase-orders/{number}/issue", (string number, Rev869BIssuePurchaseOrderRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.IssuePurchaseOrderAsync(number, r, ct))).RequirePagePermission("purchase.po", PagePermissionActions.Submit);
        group.MapPost("/purchase-orders/{number}/amend", (string number, Rev869BAmendPurchaseOrderRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.AmendPurchaseOrderAsync(number, r, ct))).RequirePagePermission("purchase.po", PagePermissionActions.Update);
        group.MapPost("/purchase-orders/{number}/approve-amendment", (string number, Rev869BApprovalActionRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.ApprovePurchaseOrderAsync(number, r, ct))).RequirePagePermission("purchase.po", PagePermissionActions.Approve);
        group.MapPost("/purchase-orders/{number}/cancel", (string number, Rev869BCancelPurchaseOrderRequest r, IRev869BPurchaseService s, CancellationToken ct) => Run(() => s.CancelPurchaseOrderAsync(number, r, ct))).RequirePagePermission("purchase.po", PagePermissionActions.Cancel);
        group.MapGet("/rfqs/{number}", GetRfq).RequirePagePermission("purchase.rfq", PagePermissionActions.View);
        group.MapGet("/comparisons/{number}", GetComparison).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.View);
        group.MapGet("/purchase-orders/{number}", GetPo).RequirePagePermission("purchase.po", PagePermissionActions.View);
        group.MapGet("/material-followup", GetFollowUp).RequirePagePermission("purchase.material-followup", PagePermissionActions.View);
        return endpoints;
    }

    private static async Task<IResult> GetRfq(string number, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, CancellationToken ct)
    {
        var row = await db.RequestForQuotations.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == user.OrganizationId && x.RfqNumber == number.Trim().ToUpper(), ct); if (row is null) return Results.NotFound(); if (!await Allowed(user, scopes, row.OrganizationId, row.RequestingDepartmentId, row.DeliveryWarehouseId, row.OwnerEmployeeId, ct)) return Results.Forbid(); return Results.Ok(row);
    }
    private static async Task<IResult> GetComparison(string number, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, CancellationToken ct)
    {
        var row = await db.CommercialComparisons.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == user.OrganizationId && x.ComparisonNumber == number.Trim().ToUpper(), ct); if (row is null) return Results.NotFound(); var rfq = await db.RequestForQuotations.AsNoTracking().SingleAsync(x => x.Id == row.RequestForQuotationId, ct); if (!await Allowed(user, scopes, row.OrganizationId, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, row.OwnerEmployeeId, ct)) return Results.Forbid(); return Results.Ok(row);
    }
    private static async Task<IResult> GetPo(string number, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, CancellationToken ct)
    {
        var row = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == user.OrganizationId && x.PoNumber == number.Trim().ToUpper() && x.IsCurrentVersion, ct); if (row is null) return Results.NotFound(); if (!await Allowed(user, scopes, row.OrganizationId, row.RequestingDepartmentId, row.DeliveryWarehouseId, row.OwnerEmployeeId, ct)) return Results.Forbid(); return Results.Ok(row);
    }
    private static async Task<IResult> GetFollowUp(NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, CancellationToken ct)
    {
        if (!user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(user.OrganizationId)) return Results.Forbid(); var rows = await db.MaterialFollowUpHandoffs.AsNoTracking().Where(x => x.PurchaseOrder!.OrganizationId == user.OrganizationId && x.PurchaseOrder.IsCurrentVersion && x.PurchaseOrder.Status == "Issued").Select(x => new { x.Id, x.HandoffNumber, x.PurchaseOrderId, x.PurchaseOrderLineId, x.OrderedQuantitySnapshot, x.Status, x.HandoffAt, DepartmentId = x.PurchaseOrder!.RequestingDepartmentId, WarehouseId = x.PurchaseOrder.DeliveryWarehouseId, OwnerId = x.PurchaseOrder.OwnerEmployeeId }).ToListAsync(ct); var allowed = new List<object>(); foreach (var row in rows) if (await Allowed(user, scopes, user.OrganizationId, row.DepartmentId, row.WarehouseId, row.OwnerId, ct)) allowed.Add(row); return Results.Ok(allowed);
    }
    private static async Task<bool> Allowed(ICurrentUser user, IRecordScopeAuthorizer scopes, string organization, Guid? department, Guid? warehouse, Guid? owner, CancellationToken ct) => user.EmployeeId.HasValue && (await scopes.AuthorizeAsync(user.EmployeeId.Value, user.RoleCode, new RecordScopeTarget(organization, department, warehouse, null, owner), DateOnly.FromDateTime(DateTime.UtcNow), ct)).Allowed;
    private static async Task<IResult> Run(Func<Task<Rev869BDocumentResult>> action)
    {
        try { return Results.Ok(await action()); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (DbUpdateConcurrencyException ex) { return Results.Conflict(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
    }
}