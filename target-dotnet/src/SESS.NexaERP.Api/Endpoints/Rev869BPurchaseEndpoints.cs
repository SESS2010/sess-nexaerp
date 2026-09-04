using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class Rev869BPurchaseEndpoints
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
        group.MapPost("/purchase-orders/{number}/issue", (string number, Rev869BIssuePurchaseOrderRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.IssuePurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Issue);
        group.MapPost("/purchase-orders/{number}/amend", (string number, Rev869BAmendPurchaseOrderRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.AmendPurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Update);
        group.MapPost("/purchase-orders/{number}/revise-rejected", (string number, Rev869BReviseRejectedPurchaseOrderRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.ReviseRejectedPurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Update);
        group.MapPost("/purchase-orders/{number}/approve", (string number, Rev869BPoApprovalActionRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.ApprovePurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Approve);
        group.MapPost("/purchase-orders/{number}/reject", (string number, Rev869BPoApprovalActionRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.RejectPurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Reject);
        group.MapPost("/purchase-orders/{number}/cancel", (string number, Rev869BCancelPurchaseOrderRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.CancelPurchaseOrderAsync(number, r, ct), h, ct)).RequirePagePermission("purchase.po", PagePermissionActions.Cancel);
        group.MapPost("/material-followup/{id:guid}/transition", (Guid id, Rev869BMaterialFollowUpTransitionRequest r, IRev869BPurchaseService s, HttpContext h, CancellationToken ct) => Run(() => s.TransitionMaterialFollowUpAsync(id, r, ct), h, ct)).RequirePagePermission("purchase.material-followup", PagePermissionActions.Update);
        group.MapGet("/rfqs/{number}/vendor-candidates", GetRfqVendorCandidates).RequirePagePermission("purchase.rfq", PagePermissionActions.Submit);
        group.MapGet("/rfq-invitations", GetRfqInvitationCandidates).RequirePagePermission("purchase.vendor-quotations", PagePermissionActions.Create);
        group.MapGet("/comparisons/rfq-candidates", GetComparisonRfqCandidates).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.Create);
        group.MapGet("/rfqs", ListRfqs).RequirePagePermission("purchase.rfq", PagePermissionActions.View);
        group.MapGet("/quotations", ListQuotations).RequirePagePermission("purchase.vendor-quotations", PagePermissionActions.View);
        group.MapGet("/comparisons", ListComparisons).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.View);
        group.MapGet("/purchase-orders", ListPurchaseOrders).RequirePagePermission("purchase.po", PagePermissionActions.View);
        group.MapGet("/rfqs/{number}", GetRfq).RequirePagePermission("purchase.rfq", PagePermissionActions.View);
        group.MapGet("/quotations/{number}", GetQuotation).RequirePagePermission("purchase.vendor-quotations", PagePermissionActions.View);
        group.MapGet("/comparisons/{number}", GetComparison).RequirePagePermission("purchase.commercial-comparisons", PagePermissionActions.View);
        group.MapGet("/purchase-orders/{number}", GetPo).RequirePagePermission("purchase.po", PagePermissionActions.View);
        group.MapGet("/quotations/{number}/attachment", GetQuotationAttachment).RequirePagePermission("purchase.vendor-quotations", PagePermissionActions.Download);
        group.MapGet("/material-followup", GetFollowUp).RequirePagePermission("purchase.material-followup", PagePermissionActions.View);
        return endpoints;
    }

    private static async Task<IResult> GetRfq(string number, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IAuditWriter audit, CancellationToken ct)
    {
        var row = await db.RequestForQuotations.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == user.OrganizationId && x.RfqNumber == number.Trim().ToUpper(), ct); if (row is null) return await Missing(audit, "purchase.rfq", number, user, ct); if (!await Allowed(user, scopes, row.OrganizationId, row.RequestingDepartmentId, row.DeliveryWarehouseId, row.OwnerEmployeeId, ct)) return await Denied(audit, "purchase.rfq", number, user, ct); return Results.Ok(row);
    }
    private static async Task<IResult> GetRfqVendorCandidates(string number, NexaErpDbContext db, ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] IVendorQualificationService qualifications, IAuditWriter audit, CancellationToken ct)
    {
        var rfq = await ScopeRfqs(db.RequestForQuotations.AsNoTracking().Include(x => x.Lines).ThenInclude(x => x.Item), db, user)
            .SingleOrDefaultAsync(x => x.RfqNumber == number.Trim().ToUpperInvariant(), ct);
        if (rfq is null) return await Missing(audit, "purchase.rfq", number, user, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var categories = rfq.Lines.Select(x => x.Item!.CategoryId).Distinct().ToArray();
        var invited = await db.RfqVendorInvitations.AsNoTracking().Where(x => x.RequestForQuotationId == rfq.Id).Select(x => x.VendorId).ToListAsync(ct);
        var vendors = await db.Vendors.AsNoTracking().Where(x => !invited.Contains(x.Id)).OrderBy(x => x.VendorCode).ToListAsync(ct);
        var candidates = new List<RfqVendorCandidate>();
        foreach (var vendor in vendors)
        {
            if (!VendorQualification.IsVendorEligible(vendor, today)) continue;
            var eligible = true;
            foreach (var category in categories)
                if (!await qualifications.IsEligibleAsync(vendor.Id, rfq.OrganizationId, category, today, ct)) { eligible = false; break; }
            if (eligible) candidates.Add(new RfqVendorCandidate(vendor.Id, vendor.VendorCode, vendor.Name));
        }
        return Results.Ok(candidates);
    }
    private static async Task<IResult> GetRfqInvitationCandidates(NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var rfqIds = ScopeRfqs(db.RequestForQuotations.AsNoTracking(), db, user).Select(x => x.Id);
        var invitations = await db.RfqVendorInvitations.AsNoTracking().Include(x => x.Vendor)
            .Include(x => x.RequestForQuotation)!.ThenInclude(x => x!.Lines)
            .Where(x => rfqIds.Contains(x.RequestForQuotationId))
            .OrderBy(x => x.RequestForQuotation!.RfqNumber).ThenBy(x => x.Vendor!.VendorCode).ToListAsync(ct);
        var currentVersions = await db.VendorQuotations.AsNoTracking()
            .Where(x => invitations.Select(i => i.Id).Contains(x.RfqVendorInvitationId) && x.IsCurrentRevision)
            .ToDictionaryAsync(x => x.RfqVendorInvitationId, x => x.Version, ct);
        var rows = invitations.Select(x => new RfqInvitationCandidate(x.Id, x.Version, x.RequestForQuotation!.RfqNumber,
            x.VendorId, x.Vendor!.VendorCode, x.Vendor.Name, x.RequestForQuotation.CurrencyCode,
            x.QuoteDueAtSnapshot, x.Status, currentVersions.TryGetValue(x.Id, out var version) ? version : null,
            x.RequestForQuotation.Lines.OrderBy(line => line.LineNumber)
                .Select(line => new RfqInvitationLineCandidate(line.Id, line.LineNumber, line.ItemId,
                    line.ItemCodeSnapshot, line.ItemNameSnapshot, line.UomSnapshot, line.RfqQuantity)).ToList())).ToList();
        return Results.Ok(rows);
    }
    private static async Task<IResult> GetComparisonRfqCandidates(NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var rows = await ScopeRfqs(db.RequestForQuotations.AsNoTracking(), db, user)
            .Where(rfq => db.VendorQuotations.Any(q => q.OrganizationId == rfq.OrganizationId &&
                q.RfqVendorInvitation!.RequestForQuotationId == rfq.Id && q.IsCurrentRevision &&
                q.Status == Rev869BStatuses.TechnicallyCompliant && q.CurrencyCode == rfq.CurrencyCode))
            .OrderBy(x => x.RfqNumber)
            .Select(rfq => new ComparisonRfqCandidate(rfq.Id, rfq.RfqNumber, rfq.Version, rfq.CurrencyCode,
                db.VendorQuotations.Count(q => q.OrganizationId == rfq.OrganizationId &&
                    q.RfqVendorInvitation!.RequestForQuotationId == rfq.Id && q.IsCurrentRevision &&
                    q.Status == Rev869BStatuses.TechnicallyCompliant && q.CurrencyCode == rfq.CurrencyCode)))
            .ToListAsync(ct);
        return Results.Ok(rows);
    }
    private static async Task<IResult> GetComparison(string number, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IPagePermissionService permissions, IAuditWriter audit, CancellationToken ct)
    {
        var row = await db.CommercialComparisons.AsNoTracking().Include(x => x.Lines).ThenInclude(x => x.VendorQuotationLine).SingleOrDefaultAsync(x => x.OrganizationId == user.OrganizationId && x.ComparisonNumber == number.Trim().ToUpper(), ct); if (row is null) return await Missing(audit, "purchase.commercial-comparisons", number, user, ct); var rfq = await db.RequestForQuotations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == row.RequestForQuotationId && x.OrganizationId == row.OrganizationId, ct); if (rfq is null) return Results.Conflict(new { message = "Comparison RFQ parent contract is invalid." }); if (!await Allowed(user, scopes, row.OrganizationId, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, row.OwnerEmployeeId, ct)) return await Denied(audit, "purchase.commercial-comparisons", number, user, ct);
        if (await permissions.HasPermissionAsync(user.RoleCodes, "purchase.commercial-comparisons", PagePermissionActions.ViewCommercialValues, ct))
            return Results.Ok(new { row.Id, row.ComparisonNumber, row.RequestForQuotationId, row.RecommendedVendorQuotationId,
                row.SelectedVendorId, row.OwnerEmployeeId, row.CurrencyCode, row.TotalPayableValue, row.ApprovalRoute,
                row.ApprovalCycle, row.RequiredApprovalStepCount, row.CompletedApprovalStepCount, row.CreatorEmployeeId,
                row.Status, row.IsSingleSource, row.SingleSourceJustification, row.RecommendationRemarks, row.Version,
                Lines = row.Lines.Select(x => new { x.Id, x.VendorQuotationLineId, VendorQuotationId = x.VendorQuotationLine!.VendorQuotationId,
                    x.VendorId, x.TechnicalComplianceSnapshot, x.CommercialSnapshotJson, x.DeliverySnapshot, x.WarrantySnapshot,
                    x.PaymentTermsSnapshot, x.TotalPayableValue, x.IsRecommended, x.RecommendationReason, x.Version }) });
        await audit.WriteAsync("Security", "Denied", "CommercialValues", row.Id.ToString(), null, new { reason = "Commercial values masked", user.RoleCode }, ct);
        return Results.Ok(new { row.Id, row.ComparisonNumber, row.RequestForQuotationId, row.OwnerEmployeeId, row.CurrencyCode, row.Status, row.IsSingleSource, row.SingleSourceJustification, row.RecommendationRemarks, row.Version, Lines = row.Lines.Select(x => new { x.Id, x.VendorQuotationLineId, VendorQuotationId = x.VendorQuotationLine!.VendorQuotationId, x.TechnicalComplianceSnapshot, x.DeliverySnapshot, x.IsRecommended, x.RecommendationReason }) });
    }
    private static async Task<IResult> GetPo(string number, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IPagePermissionService permissions, IAuditWriter audit, CancellationToken ct)
    {
        var row = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == user.OrganizationId && x.PoNumber == number.Trim().ToUpper() && x.IsCurrentVersion, ct); if (row is null) return await Missing(audit, "purchase.po", number, user, ct); if (!await Allowed(user, scopes, row.OrganizationId, row.RequestingDepartmentId, row.DeliveryWarehouseId, row.OwnerEmployeeId, ct)) return await Denied(audit, "purchase.po", number, user, ct);
        if (await permissions.HasPermissionAsync(user.RoleCodes, "purchase.po", PagePermissionActions.ViewCommercialValues, ct)) return Results.Ok(row);
        await audit.WriteAsync("Security", "Denied", "CommercialValues", row.Id.ToString(), null, new { reason = "Commercial values masked", user.RoleCode }, ct);
        return Results.Ok(new { row.Id, row.PoNumber, row.RevisionNumber, row.IsCurrentVersion, row.RequestingDepartmentId, row.DeliveryWarehouseId, row.OwnerEmployeeId, row.Status, row.CurrencyCode, row.IssuedAt, row.CancelledAt, row.CancellationReason, row.Version, Lines = row.Lines.Select(x => new { x.Id, x.LineNumber, x.ItemId, x.ItemCodeSnapshot, x.ItemNameSnapshot, x.UomSnapshot, x.OrderedQuantity }) });
    }
    private static async Task<IResult> GetQuotationAttachment(string number, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IAuditWriter audit, CancellationToken ct)
    {
        var row = await db.VendorQuotations.AsNoTracking().Include(x => x.RfqVendorInvitation)!.ThenInclude(x => x!.RequestForQuotation)
            .SingleOrDefaultAsync(x => x.OrganizationId == user.OrganizationId && x.QuotationNumber == number.Trim().ToUpperInvariant(), ct);
        if (row is null) return await Missing(audit, "purchase.vendor-quotations", number, user, ct);
        var rfq = row.RfqVendorInvitation!.RequestForQuotation!;
        if (!await Allowed(user, scopes, row.OrganizationId, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, rfq.OwnerEmployeeId, ct))
            return await Denied(audit, "purchase.vendor-quotations", number, user, ct);
        await audit.WriteAsync("Purchase", "AttachmentAccess", nameof(VendorQuotation), row.Id.ToString(), null,
            new { row.QuotationNumber, organizationId = row.OrganizationId, evidencePresent = true }, ct);
        return Results.Ok(new { row.QuotationNumber, row.AttachmentObjectKey, row.AttachmentSha256, row.SubmissionSource, row.ReceivedAt });
    }
    private static async Task<IResult> GetFollowUp(string? handoffNumber, int? page, int? pageSize, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var pageNumber = page ?? 1; var take = pageSize ?? 50;
        if (pageNumber < 1 || take is < 1 or > 100) return Results.BadRequest(new { message = "page must be positive and pageSize must be 1-100." });
        var purchaseOrderIds = ScopePurchaseOrders(db.PurchaseOrders.AsNoTracking(), db, user)
            .Where(x => x.IsCurrentVersion && x.Status == Rev869BStatuses.Issued).Select(x => x.Id);
        var query = db.MaterialFollowUpHandoffs.AsNoTracking().Where(x => purchaseOrderIds.Contains(x.PurchaseOrderId));
        if (!string.IsNullOrWhiteSpace(handoffNumber)) { var number = handoffNumber.Trim().ToUpperInvariant(); query = query.Where(x => x.HandoffNumber == number); }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.HandoffAt).ThenBy(x => x.Id).Skip((pageNumber - 1) * take).Take(take)
            .Select(x => new MaterialFollowUpListItem(x.Id, x.HandoffNumber, x.PurchaseOrderId, x.PurchaseOrderLineId,
                x.OrderedQuantitySnapshot, x.Status, x.HandoffAt, x.Version)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<MaterialFollowUpListItem>(total, pageNumber, take, rows));
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
        catch (UnauthorizedAccessException ex) { await AuditDenied(ex.Message, "Authorization"); return user.IsAuthenticated ? Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden) : Results.Unauthorized(); }
        catch (DbUpdateConcurrencyException ex) { await AuditDenied("Concurrent command rejected.", "Concurrency"); return Results.Conflict(new { message = ex.Message }); }
        catch (Rev869BNotFoundException ex) { await AuditDenied("Scoped record missing or denied.", "RecordScope"); return Results.NotFound(new { message = ex.Message }); }
        catch (Rev869BValidationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        catch (Rev869BConflictException ex) { await AuditDenied("Business or idempotency conflict rejected.", "Conflict"); return Results.Conflict(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        catch (OverflowException ex) { return Results.BadRequest(new { message = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }
}
