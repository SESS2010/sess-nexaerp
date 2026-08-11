using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Infrastructure.Purchase;

public sealed partial class EfRev869BPurchaseService
{
    public async Task<Rev869BDocumentResult> CreateComparisonAsync(Rev869BCreateComparisonRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var organization = RequireOrganization();
        var rfq = await db.RequestForQuotations.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organization && x.RfqNumber == request.RfqNumber.Trim().ToUpper(), ct)
            ?? throw new Rev869BNotFoundException("RFQ was not found in the current organization.");
        await RequireScopeAsync(actor, organization, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, null, rfq.OwnerEmployeeId, ct);
        var existing = await db.CommercialComparisons.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organization && x.IdempotencyKey == request.IdempotencyKey.Trim(), ct);
        if (existing is not null)
        {
            if (existing.RequestForQuotationId != rfq.Id) throw new Rev869BConflictException("Comparison idempotency key was reused for another RFQ.");
            await tx.RollbackAsync(ct); return Result(existing.Id, existing.ComparisonNumber, existing.Status, existing.Version);
        }
        await ReserveRfqAsync(rfq.Id, organization, request.RfqVersion, ct);
        var quotes = await db.VendorQuotations.AsNoTracking().Include(x => x.Lines).Where(x => x.OrganizationId == organization && x.RfqVendorInvitation!.RequestForQuotationId == rfq.Id && x.IsCurrentRevision && x.Status == Rev869BStatuses.TechnicallyCompliant).ToListAsync(ct);
        if (quotes.Count == 0) throw new Rev869BConflictException("No current technically compliant quotation is available.");
        if (quotes.Any(x => x.CurrencyCode != rfq.CurrencyCode)) throw new Rev869BConflictException("Currency conversion unavailable; quotations must match RFQ currency.");
        var next = await NextNumberAsync(organization, "CMP", DateOnly.FromDateTime(DateTime.UtcNow), ct); var comparison = new CommercialComparison { OrganizationId = organization, ComparisonNumber = next.Number, FinancialYear = next.Year, SequenceNumber = next.Sequence, RequestForQuotationId = rfq.Id, OwnerEmployeeId = actor, CurrencyCode = rfq.CurrencyCode, IsSingleSource = rfq.IsSingleSource, SingleSourceJustification = rfq.SingleSourceJustification, IdempotencyKey = request.IdempotencyKey.Trim(), CreatedBy = user.LoginId };
        foreach (var quote in quotes) foreach (var line in quote.Lines)
        {
            var recalculated = await RecalculateAsync(line, organization, DateOnly.FromDateTime(DateTime.UtcNow), ct);
            comparison.Lines.Add(new CommercialComparisonLine { VendorQuotationLineId = line.Id, VendorId = quote.VendorId, TechnicalComplianceSnapshot = Rev869BStatuses.TechnicallyCompliant, CommercialSnapshotJson = JsonSerializer.Serialize(new { Inputs = line, Result = recalculated.Breakdown, TaxRule = recalculated.Tax }, JsonOptions), DeliverySnapshot = line.PromisedDeliveryDate.ToString("yyyy-MM-dd"), WarrantySnapshot = quote.WarrantyTermsSnapshot, PaymentTermsSnapshot = quote.PaymentTermsSnapshot, TotalPayableValue = recalculated.Breakdown.TotalPayableValue, CreatedBy = user.LoginId });
        }
        db.CommercialComparisons.Add(comparison); AddStatus("CommercialComparison", comparison.Id, comparison.ComparisonNumber, null, comparison.Status, "Create", "Created from technically compliant quotations", request.IdempotencyKey); await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", "CreateComparison", nameof(CommercialComparison), comparison.Id.ToString(), null, new { comparison.ComparisonNumber, lineCount = comparison.Lines.Count }, ct); await tx.CommitAsync(ct); return Result(comparison.Id, comparison.ComparisonNumber, comparison.Status, comparison.Version);
    }

    public async Task<Rev869BDocumentResult> RecommendAsync(string number, Rev869BRecommendComparisonRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var organization = RequireOrganization(); var comparison = await LoadComparisonAsync(number, ct); await AuthorizeComparisonAsync(actor, comparison, ct);
        var replay = await db.PurchaseTransactionStatusHistories.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organization && x.EntityType == "CommercialComparison" && x.EntityId == comparison.Id && x.CorrelationId == request.IdempotencyKey.Trim(), ct);
        if (replay is not null)
        {
            var requestedJustification = Trim(request.SingleSourceJustification) ?? comparison.SingleSourceJustification;
            if (replay.Action != "Recommend" || replay.Remarks != request.RecommendationRemarks.Trim() ||
                comparison.RecommendedVendorQuotationId != request.VendorQuotationId ||
                Trim(comparison.SingleSourceJustification) != Trim(requestedJustification))
                throw new Rev869BConflictException("Recommendation idempotency key was reused with a different command.");
            await tx.RollbackAsync(ct); return Result(comparison.Id, comparison.ComparisonNumber, comparison.Status, comparison.Version);
        }
        if (comparison.Status is not (Rev869BStatuses.Draft or Rev869BStatuses.RevisionRequested)) throw new Rev869BConflictException("Comparison cannot be recommended now.");
        var quote = await db.VendorQuotations.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == request.VendorQuotationId && x.OrganizationId == organization && x.RfqVendorInvitation!.RequestForQuotationId == comparison.RequestForQuotationId && x.IsCurrentRevision && x.Status == Rev869BStatuses.TechnicallyCompliant, ct)
            ?? throw new Rev869BConflictException("Recommended quotation is outside this organization/RFQ or is not technically compliant.");
        var comparisonLineIds = comparison.Lines.Select(x => x.VendorQuotationLineId).ToHashSet(); if (quote.Lines.Any(x => !comparisonLineIds.Contains(x.Id))) throw new Rev869BConflictException("Recommended quotation line is outside this comparison.");
        var justification = Trim(request.SingleSourceJustification) ?? comparison.SingleSourceJustification;
        if (comparison.IsSingleSource && string.IsNullOrWhiteSpace(justification)) throw new Rev869BValidationException("Single-source recommendation justification is required.");
        var newVersion = await ReserveComparisonAsync(comparison.Id, organization, request.Version, ct);
        decimal total = 0m; var remarks = RequiredRemarks(request.RecommendationRemarks);
        foreach (var quoteLine in quote.Lines)
        {
            var recalculated = await RecalculateAsync(quoteLine, organization, DateOnly.FromDateTime(DateTime.UtcNow), ct); total = Rev869BCommercialCalculator.Add(total, recalculated.Breakdown.TotalPayableValue);
            await db.CommercialComparisonLines.Where(x => x.CommercialComparisonId == comparison.Id && x.VendorQuotationLineId == quoteLine.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRecommended, true).SetProperty(x => x.RecommendationReason, remarks).SetProperty(x => x.TotalPayableValue, recalculated.Breakdown.TotalPayableValue).SetProperty(x => x.CommercialSnapshotJson, JsonSerializer.Serialize(new { Inputs = quoteLine, Result = recalculated.Breakdown, TaxRule = recalculated.Tax }, JsonOptions)), ct);
        }
        await db.CommercialComparisonLines.Where(x => x.CommercialComparisonId == comparison.Id && !quote.Lines.Select(l => l.Id).Contains(x.VendorQuotationLineId))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRecommended, false).SetProperty(x => x.RecommendationReason, (string?)null), ct);
        var route = await ResolveApprovalRouteAsync(total, organization, ct); Rev869BStatusContracts.RequireComparison(comparison.Status, Rev869BStatuses.PendingApproval);
        await db.CommercialComparisons.Where(x => x.Id == comparison.Id && x.OrganizationId == organization && x.Version == newVersion)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RecommendedVendorQuotationId, quote.Id).SetProperty(x => x.SelectedVendorId, quote.VendorId).SetProperty(x => x.TotalPayableValue, total).SetProperty(x => x.SingleSourceJustification, justification).SetProperty(x => x.RecommendationRemarks, remarks).SetProperty(x => x.ApprovalRoute, route).SetProperty(x => x.Status, Rev869BStatuses.PendingApproval).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        AddStatus("CommercialComparison", comparison.Id, comparison.ComparisonNumber, comparison.Status, Rev869BStatuses.PendingApproval, "Recommend", remarks, request.IdempotencyKey);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", "RecommendVendor", nameof(CommercialComparison), comparison.Id.ToString(), null, new { SelectedVendorId = quote.VendorId, TotalPayableValue = total, ApprovalRoute = route }, ct); await tx.CommitAsync(ct); return Result(comparison.Id, comparison.ComparisonNumber, Rev869BStatuses.PendingApproval, newVersion);
    }

    public Task<Rev869BDocumentResult> ApproveAsync(string number, Rev869BApprovalActionRequest request, CancellationToken ct) => ApprovalActionAsync(number, request, "Approve", Rev869BStatuses.Approved, ct);
    public Task<Rev869BDocumentResult> RejectAsync(string number, Rev869BApprovalActionRequest request, CancellationToken ct) => ApprovalActionAsync(number, request, "Reject", Rev869BStatuses.Rejected, ct);
    public Task<Rev869BDocumentResult> RequestRevisionAsync(string number, Rev869BApprovalActionRequest request, CancellationToken ct) => ApprovalActionAsync(number, request, "RequestRevision", Rev869BStatuses.RevisionRequested, ct);

    public async Task<Rev869BDocumentResult> ResubmitAsync(string number, Rev869BApprovalActionRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var c = await LoadComparisonAsync(number, ct); await AuthorizeComparisonAsync(actor, c, ct);
        var replay = await db.PurchaseTransactionApprovalHistories.AsNoTracking().SingleOrDefaultAsync(x => x.CommercialComparisonId == c.Id && x.CorrelationId == request.IdempotencyKey.Trim(), ct);
        if (replay is not null) { if (replay.Action != "Resubmit" || replay.Remarks != request.Remarks.Trim()) throw new Rev869BConflictException("Resubmit idempotency key was reused."); await tx.RollbackAsync(ct); return Result(c.Id, c.ComparisonNumber, c.Status, c.Version); }
        if (c.Status != Rev869BStatuses.RevisionRequested) throw new Rev869BConflictException("Only revision-requested comparison may be resubmitted.");
        var version = await ReserveComparisonAsync(c.Id, c.OrganizationId, request.Version, ct); var remarks = RequiredRemarks(request.Remarks); Rev869BStatusContracts.RequireComparison(c.Status, Rev869BStatuses.PendingApproval);
        await db.CommercialComparisons.Where(x => x.Id == c.Id && x.OrganizationId == c.OrganizationId && x.Version == version).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, Rev869BStatuses.PendingApproval).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        AddStatus("CommercialComparison", c.Id, c.ComparisonNumber, c.Status, Rev869BStatuses.PendingApproval, "Resubmit", remarks, request.IdempotencyKey); AddApproval(c, "Resubmit", c.Status, Rev869BStatuses.PendingApproval, remarks, request.IdempotencyKey);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", "ResubmitComparison", nameof(CommercialComparison), c.Id.ToString(), null, new { Status = Rev869BStatuses.PendingApproval }, ct); await tx.CommitAsync(ct); return Result(c.Id, c.ComparisonNumber, Rev869BStatuses.PendingApproval, version);
    }

    private async Task<Rev869BDocumentResult> ApprovalActionAsync(string number, Rev869BApprovalActionRequest request, string action, string next, CancellationToken ct)
    {
        var actor = RequireActor(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var c = await LoadComparisonAsync(number, ct); await AuthorizeComparisonAsync(actor, c, ct);
        var replay = await db.PurchaseTransactionApprovalHistories.AsNoTracking().SingleOrDefaultAsync(x => x.CommercialComparisonId == c.Id && x.CorrelationId == request.IdempotencyKey.Trim(), ct);
        if (replay is not null) { if (replay.Action != action || replay.Remarks != request.Remarks.Trim()) throw new Rev869BConflictException("Approval idempotency key was reused."); await tx.RollbackAsync(ct); return Result(c.Id, c.ComparisonNumber, c.Status, c.Version); }
        if (c.Status != Rev869BStatuses.PendingApproval) throw new Rev869BConflictException("Comparison is not pending approval.");
        var department = await db.RequestForQuotations.AsNoTracking().Where(x => x.Id == c.RequestForQuotationId && x.OrganizationId == c.OrganizationId).Select(x => x.RequestingDepartmentId).SingleOrDefaultAsync(ct);
        await RequireApproverAsync(c.ApprovalRoute, department, actor, c.CreatedBy, ct);
        if (c.RecommendedVendorQuotationId.HasValue && await db.QuotationTechnicalVerifications.AnyAsync(x => x.VerifierEmployeeId == actor && x.VendorQuotationLine!.VendorQuotationId == c.RecommendedVendorQuotationId, ct)) throw new UnauthorizedAccessException("Technical verifier cannot commercially approve the same quotation.");
        var remarks = RequiredRemarks(request.Remarks); var version = await ReserveComparisonAsync(c.Id, c.OrganizationId, request.Version, ct); Rev869BStatusContracts.RequireComparison(c.Status, next);
        await db.CommercialComparisons.Where(x => x.Id == c.Id && x.OrganizationId == c.OrganizationId && x.Version == version).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, next).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        AddStatus("CommercialComparison", c.Id, c.ComparisonNumber, c.Status, next, action, remarks, request.IdempotencyKey); AddApproval(c, action, c.Status, next, remarks, request.IdempotencyKey);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", action + "Comparison", nameof(CommercialComparison), c.Id.ToString(), new { status = c.Status }, new { status = next, c.ApprovalRoute }, ct); await tx.CommitAsync(ct); return Result(c.Id, c.ComparisonNumber, next, version);
    }

    public async Task<Rev869BDocumentResult> CreatePurchaseOrderAsync(Rev869BCreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var c = await LoadComparisonAsync(request.ComparisonNumber, ct); await AuthorizeComparisonAsync(actor, c, ct);
        if (c.Status != Rev869BStatuses.Approved || !c.RecommendedVendorQuotationId.HasValue || !c.SelectedVendorId.HasValue) throw new Rev869BConflictException("PO requires approved comparison and explicit selection.");
        var duplicate = await db.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == c.OrganizationId && x.IdempotencyKey == request.IdempotencyKey.Trim(), ct);
        if (duplicate is not null) { await AuthorizePoAsync(actor, duplicate, ct); if (duplicate.CommercialComparisonId != c.Id) throw new Rev869BConflictException("PO idempotency key was reused for another comparison."); await tx.RollbackAsync(ct); return Result(duplicate.Id, duplicate.PoNumber, duplicate.Status, duplicate.Version); }
        await ReserveComparisonAsync(c.Id, c.OrganizationId, request.ComparisonVersion, ct);
        var rfq = await db.RequestForQuotations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == c.RequestForQuotationId && x.OrganizationId == c.OrganizationId, ct) ?? throw new Rev869BConflictException("Comparison RFQ parent is invalid.");
        var quote = await db.VendorQuotations.AsNoTracking().Include(x => x.Lines).ThenInclude(x => x.RequestForQuotationLine)!.ThenInclude(x => x!.Item).SingleOrDefaultAsync(x => x.Id == c.RecommendedVendorQuotationId && x.OrganizationId == c.OrganizationId && x.RfqVendorInvitation!.RequestForQuotationId == rfq.Id && x.VendorId == c.SelectedVendorId, ct) ?? throw new Rev869BConflictException("Selected quotation organization/RFQ/vendor contract is invalid.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var category in quote.Lines.Select(x => x.RequestForQuotationLine!.Item!.CategoryId).Distinct()) if (!await vendors.IsEligibleAsync(quote.VendorId, c.OrganizationId, category, today, ct)) throw new Rev869BConflictException("Selected vendor category qualification is no longer effective.");
        var next = await NextNumberAsync(c.OrganizationId, "PO", today, ct); var po = new PurchaseOrder { OrganizationId = c.OrganizationId, PoNumber = next.Number, FinancialYear = next.Year, SequenceNumber = next.Sequence, RootPurchaseOrderId = Guid.NewGuid(), RevisionNumber = 1, CommercialComparisonId = c.Id, VendorId = quote.VendorId, RequestingDepartmentId = rfq.RequestingDepartmentId, DeliveryWarehouseId = rfq.DeliveryWarehouseId, OwnerEmployeeId = actor, Status = Rev869BStatuses.Draft, CurrencyCode = quote.CurrencyCode, PaymentTermsSnapshot = quote.PaymentTermsSnapshot, DeliveryTermsSnapshot = quote.DeliveryTermsSnapshot, WarrantyTermsSnapshot = quote.WarrantyTermsSnapshot, IdempotencyKey = request.IdempotencyKey.Trim(), CreatedBy = user.LoginId };
        var lineNo = 0;
        foreach (var ql in quote.Lines)
        {
            var source = ql.RequestForQuotationLine!; var ordered = await OrderedQuantityAsync(source.PurchaseRequisitionLineId, ct); var remaining = source.ApprovedQuantitySnapshot - ordered; if (ql.Quantity > remaining) throw new Rev869BConflictException("Cumulative PO quantity exceeds approved outstanding quantity."); var comparisonLine = c.Lines.SingleOrDefault(x => x.VendorQuotationLineId == ql.Id) ?? throw new Rev869BConflictException("Selected quotation line is outside comparison.");
            var recalculated = await RecalculateAsync(ql, c.OrganizationId, today, ct); var calc = recalculated.Breakdown;
            po.Lines.Add(new PurchaseOrderLine { CommercialComparisonLineId = comparisonLine.Id, PurchaseRequisitionLineId = source.PurchaseRequisitionLineId, PurchaseRequirementHandoffId = source.PurchaseRequirementHandoffId, ItemId = source.ItemId, LineNumber = ++lineNo, ItemCodeSnapshot = source.ItemCodeSnapshot, ItemNameSnapshot = source.ItemNameSnapshot, UomSnapshot = source.UomSnapshot, OrderedQuantity = ql.Quantity, ApprovedOutstandingQuantitySnapshot = remaining, UnitRate = ql.UnitRate, CommercialSnapshotJson = JsonSerializer.Serialize(new { Inputs = ql, Result = calc }, JsonOptions), TaxRuleSnapshotJson = JsonSerializer.Serialize(recalculated.Tax, JsonOptions), TotalPayableValue = calc.TotalPayableValue, CreatedBy = user.LoginId });
            po.TaxableValue = Rev869BCommercialCalculator.Add(po.TaxableValue, calc.TaxableValue);
            po.DiscountValue = Rev869BCommercialCalculator.Add(po.DiscountValue, calc.DiscountValue);
            po.TaxValue = Rev869BCommercialCalculator.Add(po.TaxValue, calc.CgstValue, calc.SgstValue, calc.IgstValue, calc.CessValue);
            po.PackingForwarding = Rev869BCommercialCalculator.Add(po.PackingForwarding, calc.PackingForwarding);
            po.Freight = Rev869BCommercialCalculator.Add(po.Freight, calc.Freight);
            po.Insurance = Rev869BCommercialCalculator.Add(po.Insurance, calc.Insurance);
            po.OtherCharges = Rev869BCommercialCalculator.Add(po.OtherCharges, calc.OtherCharges);
            po.RoundOff = Rev869BCommercialCalculator.Add(po.RoundOff, calc.RoundOff);
            po.TotalPayableValue = Rev869BCommercialCalculator.Add(po.TotalPayableValue, calc.TotalPayableValue);
        }
        po.ApprovalRoute = await ResolveApprovalRouteAsync(po.TotalPayableValue, po.OrganizationId, ct);
        db.PurchaseOrders.Add(po); AddPoHistory(po, "Create", "", po.Status, "Created from approved comparison", request.IdempotencyKey); AddStatus("PurchaseOrder", po.Id, po.PoNumber, null, po.Status, "Create", "Created from approved comparison", request.IdempotencyKey); await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", "CreatePO", nameof(PurchaseOrder), po.Id.ToString(), null, new { po.PoNumber, po.TotalPayableValue }, ct); await tx.CommitAsync(ct); return Result(po.Id, po.PoNumber, po.Status, po.Version);
    }

    public async Task<Rev869BDocumentResult> SubmitPurchaseOrderAsync(string number, Rev869BSubmitPurchaseOrderRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var organization = RequireOrganization();
        var po = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines).Where(x => x.OrganizationId == organization && x.PoNumber == number.Trim().ToUpper()).OrderByDescending(x => x.RevisionNumber).FirstOrDefaultAsync(ct)
            ?? throw new Rev869BNotFoundException("Purchase-order version was not found in the current organization.");
        await AuthorizePoAsync(actor, po, ct);
        var replay = await db.PurchaseOrderHistories.AsNoTracking().SingleOrDefaultAsync(x => x.PurchaseOrderId == po.Id && x.CorrelationId == request.IdempotencyKey.Trim(), ct);
        if (replay is not null)
        {
            if (replay.Action is not ("Submit" or "ResubmitRejected") || replay.Reason != request.Remarks.Trim()) throw new Rev869BConflictException("PO submit idempotency key was reused with a different payload.");
            await tx.RollbackAsync(ct); return Result(po.Id, po.PoNumber, replay.ToStatus, checked(request.Version + 1));
        }
        if (po.Status is not (Rev869BStatuses.Draft or Rev869BStatuses.RevisionDraft)) throw new Rev869BConflictException("Only a draft or rejected-PO revision draft may be submitted.");
        var nextStatus = po.Status == Rev869BStatuses.Draft ? Rev869BStatuses.PendingApproval : Rev869BStatuses.Resubmitted;
        var action = po.Status == Rev869BStatuses.Draft ? "Submit" : "ResubmitRejected";
        var version = await ReservePoAsync(po.Id, organization, request.Version, ct); var remarks = RequiredRemarks(request.Remarks); Rev869BStatusContracts.RequirePurchaseOrder(po.Status, nextStatus);
        await db.PurchaseOrders.Where(x => x.Id == po.Id && x.OrganizationId == organization && x.Version == version).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, nextStatus).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        AddPoHistory(po, action, po.Status, nextStatus, remarks, request.IdempotencyKey); AddStatus("PurchaseOrder", po.Id, po.PoNumber, po.Status, nextStatus, action, remarks, request.IdempotencyKey);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", action + "PO", nameof(PurchaseOrder), po.Id.ToString(), new { status = po.Status }, new { status = nextStatus, po.ApprovalRoute, po.TotalPayableValue }, ct); await tx.CommitAsync(ct); return Result(po.Id, po.PoNumber, nextStatus, version);
    }

    public async Task<Rev869BDocumentResult> IssuePurchaseOrderAsync(string number, Rev869BIssuePurchaseOrderRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var po = await LoadPoAsync(number, ct); await AuthorizePoAsync(actor, po, ct);
        var replay = await db.PurchaseOrderHistories.AsNoTracking().SingleOrDefaultAsync(x => x.PurchaseOrderId == po.Id && x.CorrelationId == request.IdempotencyKey.Trim(), ct);
        if (replay is not null) { if (replay.Action != "Issue" || replay.Reason != request.Remarks.Trim()) throw new Rev869BConflictException("PO issue idempotency key was reused."); await tx.RollbackAsync(ct); return Result(po.Id, po.PoNumber, po.Status, po.Version); }
        if (po.Status != Rev869BStatuses.Approved || !po.IsCurrentVersion) throw new Rev869BConflictException("Only approved current PO may be issued.");
        Rev869BPurchaseOrderSnapshot.RequireComplete(po);
        if (await db.MaterialFollowUpHandoffs.AnyAsync(x => x.PurchaseOrderId == po.Id, ct)) throw new Rev869BConflictException("Material Follow-up handoff already exists for this PO version.");
        var version = await ReservePoAsync(po.Id, po.OrganizationId, request.Version, ct); var issuedAt = DateTimeOffset.UtcNow; Rev869BStatusContracts.RequirePurchaseOrder(po.Status, Rev869BStatuses.Issued);
        await db.PurchaseOrders.Where(x => x.Id == po.Id && x.OrganizationId == po.OrganizationId && x.Version == version).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, Rev869BStatuses.Issued).SetProperty(x => x.IssuedAt, issuedAt).SetProperty(x => x.UpdatedAt, issuedAt).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        foreach (var line in po.Lines) db.MaterialFollowUpHandoffs.Add(new MaterialFollowUpHandoff { PurchaseOrderId = po.Id, PurchaseOrderLineId = line.Id, HandoffNumber = $"MFU-{po.Id:N}-{line.LineNumber:000}", OrderedQuantitySnapshot = line.OrderedQuantity, HandoffAt = issuedAt, CorrelationId = request.IdempotencyKey.Trim(), CreatedBy = user.LoginId });
        var remarks = RequiredRemarks(request.Remarks); AddPoHistory(po, "Issue", po.Status, Rev869BStatuses.Issued, remarks, request.IdempotencyKey); AddStatus("PurchaseOrder", po.Id, po.PoNumber, po.Status, Rev869BStatuses.Issued, "Issue", remarks, request.IdempotencyKey); await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", "IssuePO", nameof(PurchaseOrder), po.Id.ToString(), new { status = po.Status }, new { Status = Rev869BStatuses.Issued, IssuedAt = issuedAt }, ct); await tx.CommitAsync(ct); return Result(po.Id, po.PoNumber, Rev869BStatuses.Issued, version);
    }

    public async Task<Rev869BDocumentResult> AmendPurchaseOrderAsync(string number, Rev869BAmendPurchaseOrderRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var organization = RequireOrganization();
        var replay = await db.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organization && x.IdempotencyKey == request.IdempotencyKey.Trim(), ct);
        if (replay is not null)
        {
            if (replay.PoNumber != number.Trim().ToUpperInvariant() || replay.AmendmentReason != request.AmendmentReason.Trim() ||
                replay.PaymentTermsSnapshot != request.PaymentTerms.Trim() || replay.DeliveryTermsSnapshot != request.DeliveryTerms.Trim() ||
                replay.WarrantyTermsSnapshot != request.WarrantyTerms.Trim())
                throw new Rev869BConflictException("PO amendment idempotency key was reused with a different payload.");
            await AuthorizePoAsync(actor, replay, ct); await tx.RollbackAsync(ct); return Result(replay.Id, replay.PoNumber, replay.Status, replay.Version);
        }
        var prior = await LoadPoAsync(number, ct); await AuthorizePoAsync(actor, prior, ct);
        if (prior.Status != Rev869BStatuses.Issued || !prior.IsCurrentVersion) throw new Rev869BConflictException("Only an issued current PO may be amended.");
        if (await db.PurchaseOrders.AnyAsync(x => x.PreviousVersionId == prior.Id && x.Status != Rev869BStatuses.Rejected && x.Status != Rev869BStatuses.Cancelled, ct)) throw new Rev869BConflictException("A pending amendment already exists for this issued version.");
        await ReservePoAsync(prior.Id, prior.OrganizationId, request.Version, ct);
        var next = new PurchaseOrder { OrganizationId = prior.OrganizationId, PoNumber = prior.PoNumber, FinancialYear = prior.FinancialYear, SequenceNumber = prior.SequenceNumber, RootPurchaseOrderId = prior.RootPurchaseOrderId, PreviousVersionId = prior.Id, RevisionNumber = prior.RevisionNumber + 1, IsCurrentVersion = false, CommercialComparisonId = prior.CommercialComparisonId, VendorId = prior.VendorId, RequestingDepartmentId = prior.RequestingDepartmentId, DeliveryWarehouseId = prior.DeliveryWarehouseId, OwnerEmployeeId = actor, Status = Rev869BStatuses.Draft, CurrencyCode = prior.CurrencyCode, ApprovalRoute = await ResolveApprovalRouteAsync(prior.TotalPayableValue, prior.OrganizationId, ct), TaxableValue = prior.TaxableValue, DiscountValue = prior.DiscountValue, TaxValue = prior.TaxValue, PackingForwarding = prior.PackingForwarding, Freight = prior.Freight, Insurance = prior.Insurance, OtherCharges = prior.OtherCharges, RoundOff = prior.RoundOff, TotalPayableValue = prior.TotalPayableValue, PaymentTermsSnapshot = Required(request.PaymentTerms, "Payment terms"), DeliveryTermsSnapshot = Required(request.DeliveryTerms, "Delivery terms"), WarrantyTermsSnapshot = Required(request.WarrantyTerms, "Warranty terms"), AmendmentReason = RequiredRemarks(request.AmendmentReason), IdempotencyKey = request.IdempotencyKey.Trim(), CreatedBy = user.LoginId };
        foreach (var l in prior.Lines) next.Lines.Add(new PurchaseOrderLine { CommercialComparisonLineId = l.CommercialComparisonLineId, PurchaseRequisitionLineId = l.PurchaseRequisitionLineId, PurchaseRequirementHandoffId = l.PurchaseRequirementHandoffId, ItemId = l.ItemId, LineNumber = l.LineNumber, ItemCodeSnapshot = l.ItemCodeSnapshot, ItemNameSnapshot = l.ItemNameSnapshot, UomSnapshot = l.UomSnapshot, OrderedQuantity = l.OrderedQuantity, ApprovedOutstandingQuantitySnapshot = l.ApprovedOutstandingQuantitySnapshot, UnitRate = l.UnitRate, CommercialSnapshotJson = l.CommercialSnapshotJson, TaxRuleSnapshotJson = l.TaxRuleSnapshotJson, TotalPayableValue = l.TotalPayableValue, CreatedBy = user.LoginId });
        db.PurchaseOrders.Add(next); AddPoHistory(next, "Amend", prior.Status, next.Status, request.AmendmentReason, request.IdempotencyKey); AddStatus("PurchaseOrder", next.Id, next.PoNumber, prior.Status, next.Status, "Amend", request.AmendmentReason, request.IdempotencyKey); await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", "AmendPO", nameof(PurchaseOrder), next.Id.ToString(), new { prior.Id, prior.RevisionNumber, prior.Status }, new { next.RevisionNumber, next.Status, next.IsCurrentVersion }, ct); await tx.CommitAsync(ct); return Result(next.Id, next.PoNumber, next.Status, next.Version);
    }

    public async Task<Rev869BDocumentResult> ReviseRejectedPurchaseOrderAsync(string number, Rev869BReviseRejectedPurchaseOrderRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var organization = RequireOrganization();
        var replay = await db.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organization && x.IdempotencyKey == request.IdempotencyKey.Trim(), ct);
        if (replay is not null)
        {
            if (replay.PoNumber != number.Trim().ToUpperInvariant() || replay.Status != Rev869BStatuses.RevisionDraft ||
                replay.AmendmentReason != request.RevisionReason.Trim() || replay.PaymentTermsSnapshot != request.PaymentTerms.Trim() ||
                replay.DeliveryTermsSnapshot != request.DeliveryTerms.Trim() || replay.WarrantyTermsSnapshot != request.WarrantyTerms.Trim())
                throw new Rev869BConflictException("Rejected-PO revision idempotency key was reused with a different payload.");
            await AuthorizePoAsync(actor, replay, ct); await tx.RollbackAsync(ct); return Result(replay.Id, replay.PoNumber, replay.Status, replay.Version);
        }
        var rejected = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines)
            .Where(x => x.OrganizationId == organization && x.PoNumber == number.Trim().ToUpperInvariant() && x.Status == Rev869BStatuses.Rejected)
            .OrderByDescending(x => x.RevisionNumber).FirstOrDefaultAsync(ct)
            ?? throw new Rev869BNotFoundException("Rejected initial purchase order was not found in the current organization.");
        await AuthorizePoAsync(actor, rejected, ct);
        if (await db.PurchaseOrders.AnyAsync(x => x.RootPurchaseOrderId == rejected.RootPurchaseOrderId && x.IsCurrentVersion, ct))
            throw new Rev869BConflictException("A current PO version already exists; rejected amendment recovery must retain and use its issued predecessor.");
        await ReservePoAsync(rejected.Id, organization, request.RejectedVersion, ct);
        Rev869BStatusContracts.RequirePurchaseOrder(rejected.Status, Rev869BStatuses.RevisionDraft);
        var revision = new PurchaseOrder
        {
            OrganizationId = rejected.OrganizationId, PoNumber = rejected.PoNumber, FinancialYear = rejected.FinancialYear,
            SequenceNumber = rejected.SequenceNumber, RootPurchaseOrderId = rejected.RootPurchaseOrderId, PreviousVersionId = rejected.Id,
            RevisionNumber = rejected.RevisionNumber + 1, IsCurrentVersion = true, CommercialComparisonId = rejected.CommercialComparisonId,
            VendorId = rejected.VendorId, RequestingDepartmentId = rejected.RequestingDepartmentId, DeliveryWarehouseId = rejected.DeliveryWarehouseId,
            OwnerEmployeeId = actor, Status = Rev869BStatuses.RevisionDraft, CurrencyCode = rejected.CurrencyCode,
            ApprovalRoute = rejected.ApprovalRoute, TaxableValue = rejected.TaxableValue, DiscountValue = rejected.DiscountValue,
            TaxValue = rejected.TaxValue, PackingForwarding = rejected.PackingForwarding, Freight = rejected.Freight,
            Insurance = rejected.Insurance, OtherCharges = rejected.OtherCharges, RoundOff = rejected.RoundOff,
            TotalPayableValue = rejected.TotalPayableValue, PaymentTermsSnapshot = Required(request.PaymentTerms, "Payment terms"),
            DeliveryTermsSnapshot = Required(request.DeliveryTerms, "Delivery terms"), WarrantyTermsSnapshot = Required(request.WarrantyTerms, "Warranty terms"),
            AmendmentReason = RequiredRemarks(request.RevisionReason), IdempotencyKey = Required(request.IdempotencyKey, "Idempotency key"), CreatedBy = user.LoginId
        };
        foreach (var line in rejected.Lines)
            revision.Lines.Add(new PurchaseOrderLine { CommercialComparisonLineId = line.CommercialComparisonLineId, PurchaseRequisitionLineId = line.PurchaseRequisitionLineId, PurchaseRequirementHandoffId = line.PurchaseRequirementHandoffId, ItemId = line.ItemId, LineNumber = line.LineNumber, ItemCodeSnapshot = line.ItemCodeSnapshot, ItemNameSnapshot = line.ItemNameSnapshot, UomSnapshot = line.UomSnapshot, OrderedQuantity = line.OrderedQuantity, ApprovedOutstandingQuantitySnapshot = line.ApprovedOutstandingQuantitySnapshot, UnitRate = line.UnitRate, CommercialSnapshotJson = line.CommercialSnapshotJson, TaxRuleSnapshotJson = line.TaxRuleSnapshotJson, TotalPayableValue = line.TotalPayableValue, CreatedBy = user.LoginId });
        db.PurchaseOrders.Add(revision);
        AddPoHistory(revision, "ReviseRejected", rejected.Status, revision.Status, request.RevisionReason, request.IdempotencyKey);
        AddStatus("PurchaseOrder", revision.Id, revision.PoNumber, rejected.Status, revision.Status, "ReviseRejected", request.RevisionReason, request.IdempotencyKey);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", "ReviseRejectedPO", nameof(PurchaseOrder), revision.Id.ToString(), new { rejected.Id, rejected.RevisionNumber, rejected.Status }, new { revision.RevisionNumber, revision.Status }, ct);
        await tx.CommitAsync(ct); return Result(revision.Id, revision.PoNumber, revision.Status, revision.Version);
    }

    public async Task<Rev869BDocumentResult> ApprovePurchaseOrderAsync(string number, Rev869BPoApprovalActionRequest request, CancellationToken ct) => await PurchaseOrderApprovalActionAsync(number, request, true, ct);
    public async Task<Rev869BDocumentResult> RejectPurchaseOrderAsync(string number, Rev869BPoApprovalActionRequest request, CancellationToken ct) => await PurchaseOrderApprovalActionAsync(number, request, false, ct);

    private async Task<Rev869BDocumentResult> PurchaseOrderApprovalActionAsync(string number, Rev869BPoApprovalActionRequest request, bool approve, CancellationToken ct)
    {
        var actor = RequireActor(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var organization = RequireOrganization();
        var po = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines).Where(x => x.OrganizationId == organization && x.PoNumber == number.Trim().ToUpper()).OrderByDescending(x => x.RevisionNumber).FirstOrDefaultAsync(ct)
            ?? throw new Rev869BNotFoundException("Purchase-order version was not found in the current organization.");
        await AuthorizePoAsync(actor, po, ct); var action = approve ? "Approve" : "Reject"; var next = approve ? Rev869BStatuses.Approved : Rev869BStatuses.Rejected;
        var replay = await db.PurchaseOrderHistories.AsNoTracking().SingleOrDefaultAsync(x => x.PurchaseOrderId == po.Id && x.CorrelationId == request.IdempotencyKey.Trim(), ct);
        if (replay is not null) { if (replay.Action != action || replay.Reason != request.Remarks.Trim()) throw new Rev869BConflictException("PO approval idempotency key was reused with a different payload."); await tx.RollbackAsync(ct); return Result(po.Id, po.PoNumber, replay.ToStatus, checked(request.Version + 1)); }
        if (po.Status is not (Rev869BStatuses.PendingApproval or Rev869BStatuses.Resubmitted)) throw new Rev869BConflictException("Purchase order is not pending approval.");
        var calculatedTotal = Rev869BCommercialCalculator.Add(po.Lines.Select(x => x.TotalPayableValue).ToArray()); if (calculatedTotal != po.TotalPayableValue) throw new Rev869BConflictException("PO header/line payable values are inconsistent.");
        var route = await ResolveApprovalRouteAsync(calculatedTotal, organization, ct); if (route != po.ApprovalRoute) throw new Rev869BConflictException("PO approval route is stale; recreate the controlled version.");
        await RequireApproverAsync(route, po.RequestingDepartmentId, actor, po.CreatedBy, ct); var remarks = RequiredRemarks(request.Remarks); var version = await ReservePoAsync(po.Id, organization, request.Version, ct); Rev869BStatusContracts.RequirePurchaseOrder(po.Status, next);
        PurchaseOrder? prior = null;
        if (approve && po.PreviousVersionId.HasValue)
        {
            prior = await db.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == po.PreviousVersionId && x.OrganizationId == organization, ct) ?? throw new Rev869BConflictException("Controlled predecessor is unavailable.");
            if (prior.Status == Rev869BStatuses.Issued)
            {
                if (!prior.IsCurrentVersion) throw new Rev869BConflictException("Issued current predecessor is unavailable.");
                if (!request.ExpectedCurrentVersion.HasValue) throw new Rev869BValidationException("Expected current PO version is required for amendment approval.");
                var priorVersion = await ReservePoAsync(prior.Id, organization, request.ExpectedCurrentVersion.Value, ct); Rev869BStatusContracts.RequirePurchaseOrder(prior.Status, Rev869BStatuses.Superseded);
                await db.PurchaseOrders.Where(x => x.Id == prior.Id && x.OrganizationId == organization && x.Version == priorVersion).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, Rev869BStatuses.Superseded).SetProperty(x => x.IsCurrentVersion, false).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
                AddPoHistory(prior, "Supersede", prior.Status, Rev869BStatuses.Superseded, remarks, request.IdempotencyKey + ":prior");
            }
            else if (prior.Status != Rev869BStatuses.Rejected || prior.IsCurrentVersion)
                throw new Rev869BConflictException("Rejected-version recovery predecessor is invalid.");
        }
        var nextIsCurrent = approve;
        await db.PurchaseOrders.Where(x => x.Id == po.Id && x.OrganizationId == organization && x.Version == version).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, next).SetProperty(x => x.IsCurrentVersion, nextIsCurrent).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        AddPoHistory(po, action, po.Status, next, remarks, request.IdempotencyKey); AddStatus("PurchaseOrder", po.Id, po.PoNumber, po.Status, next, action, remarks, request.IdempotencyKey);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", action + "PO", nameof(PurchaseOrder), po.Id.ToString(), new { status = po.Status, po.IsCurrentVersion }, new { status = next, IsCurrentVersion = nextIsCurrent, route, calculatedTotal }, ct); await tx.CommitAsync(ct); return Result(po.Id, po.PoNumber, next, version);
    }

    public async Task<Rev869BDocumentResult> CancelPurchaseOrderAsync(string number, Rev869BCancelPurchaseOrderRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.TechnicalDirector, Rev869ARoleCodes.ManagingDirector); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var po = await LoadPoAsync(number, ct); await AuthorizePoAsync(actor, po, ct);
        var replay = await db.PurchaseOrderHistories.AsNoTracking().SingleOrDefaultAsync(x => x.PurchaseOrderId == po.Id && x.CorrelationId == request.IdempotencyKey.Trim(), ct);
        if (replay is not null) { if (replay.Action != "Cancel" || replay.Reason != request.Reason.Trim()) throw new Rev869BConflictException("PO cancellation idempotency key was reused."); await tx.RollbackAsync(ct); return Result(po.Id, po.PoNumber, po.Status, po.Version); }
        var reason = RequiredRemarks(request.Reason); var version = await ReservePoAsync(po.Id, po.OrganizationId, request.Version, ct); Rev869BStatusContracts.RequirePurchaseOrder(po.Status, Rev869BStatuses.Cancelled); var cancelledAt = DateTimeOffset.UtcNow;
        await db.PurchaseOrders.Where(x => x.Id == po.Id && x.OrganizationId == po.OrganizationId && x.Version == version).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, Rev869BStatuses.Cancelled).SetProperty(x => x.CancelledAt, cancelledAt).SetProperty(x => x.CancellationReason, reason).SetProperty(x => x.UpdatedAt, cancelledAt).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        AddPoHistory(po, "Cancel", po.Status, Rev869BStatuses.Cancelled, reason, request.IdempotencyKey); AddStatus("PurchaseOrder", po.Id, po.PoNumber, po.Status, Rev869BStatuses.Cancelled, "Cancel", reason, request.IdempotencyKey); await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", "CancelPO", nameof(PurchaseOrder), po.Id.ToString(), new { status = po.Status }, new { Status = Rev869BStatuses.Cancelled, CancelledAt = cancelledAt }, ct); await tx.CommitAsync(ct); return Result(po.Id, po.PoNumber, Rev869BStatuses.Cancelled, version);
    }
}
