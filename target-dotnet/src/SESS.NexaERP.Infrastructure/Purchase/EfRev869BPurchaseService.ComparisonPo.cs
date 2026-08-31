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
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await BeginTransactionScopeAsync("CreateComparison", request.IdempotencyKey, request, ct);
        var organization = RequireOrganization();
        var comparisonScope = Rev869BIdempotencyFingerprint.CommandScope(organization, "CreateComparison", request.IdempotencyKey);
        var comparisonFingerprint = Rev869BIdempotencyFingerprint.Create(organization, "CreateComparison", request.IdempotencyKey, request);
        var rfq = await db.RequestForQuotations.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organization && x.RfqNumber == request.RfqNumber.Trim().ToUpper(), ct)
            ?? throw new Rev869BNotFoundException("RFQ was not found in the current organization.");
        await RequireScopeAsync(actor, organization, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, null, rfq.OwnerEmployeeId, ct);
        var existing = await db.CommercialComparisons.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organization && x.IdempotencyKey.StartsWith(comparisonScope + "."), ct);
        if (existing is not null)
        {
            if (existing.IdempotencyKey != comparisonFingerprint || existing.RequestForQuotationId != rfq.Id) throw new Rev869BConflictException("Comparison idempotency key was reused for another RFQ or payload.");
            await tx.RollbackAsync(ct); return Result(existing.Id, existing.ComparisonNumber, Rev869BStatuses.Draft, 0);
        }
        await ReserveRfqAsync(rfq, request.RfqVersion, "ReserveComparison", "Reserved for comparison creation", comparisonFingerprint, ct);
        var quotes = await db.VendorQuotations.AsNoTracking().Include(x => x.Lines).ThenInclude(x => x.RequestForQuotationLine).Where(x => x.OrganizationId == organization && x.RfqVendorInvitation!.RequestForQuotationId == rfq.Id && x.IsCurrentRevision && x.Status == Rev869BStatuses.TechnicallyCompliant).ToListAsync(ct);
        if (quotes.Count == 0) throw new Rev869BConflictException("No current technically compliant quotation is available.");
        if (quotes.Any(x => x.CurrencyCode != rfq.CurrencyCode)) throw new Rev869BConflictException("Currency conversion unavailable; quotations must match RFQ currency.");
        var next = await NextNumberAsync(organization, "CMP", DateOnly.FromDateTime(DateTime.UtcNow), ct); var comparison = new CommercialComparison { OrganizationId = organization, ComparisonNumber = next.Number, FinancialYear = next.Year, SequenceNumber = next.Sequence, RequestForQuotationId = rfq.Id, OwnerEmployeeId = actor, CreatorEmployeeId = actor, CurrencyCode = rfq.CurrencyCode, IsSingleSource = rfq.IsSingleSource, SingleSourceJustification = rfq.SingleSourceJustification, IdempotencyKey = comparisonFingerprint, TransitionCorrelationId = comparisonFingerprint, CreatedBy = user.LoginId };
        comparison.CompanyId = rfq.CompanyId;
        foreach (var quote in quotes) foreach (var line in quote.Lines)
        {
            var recalculated = Recalculate(line, organization, DateOnly.FromDateTime(quote.ReceivedAt.UtcDateTime));
            comparison.Lines.Add(new CommercialComparisonLine { VendorQuotationLineId = line.Id, VendorId = quote.VendorId, TechnicalComplianceSnapshot = Rev869BStatuses.TechnicallyCompliant, CommercialSnapshotJson = ComparisonSnapshotJson(comparison, quote, line, recalculated), DeliverySnapshot = line.PromisedDeliveryDate.ToString("yyyy-MM-dd"), WarrantySnapshot = quote.WarrantyTermsSnapshot, PaymentTermsSnapshot = quote.PaymentTermsSnapshot, TotalPayableValue = recalculated.Breakdown.TotalPayableValue, CreatedBy = user.LoginId });
        }
        foreach (var comparisonLine in comparison.Lines) comparisonLine.CompanyId = comparison.CompanyId;
        db.CommercialComparisons.Add(comparison);
        AddStatus("CommercialComparison", comparison.Id, comparison.ComparisonNumber, null, comparison.Status, "Create", "Created from technically compliant quotations", comparisonFingerprint);
        await SaveAuthorizedChangesAsync(ct); await WriteAuditAsync("Purchase", "CreateComparison", nameof(CommercialComparison), comparison.Id.ToString(), null, new { comparison.ComparisonNumber, lineCount = comparison.Lines.Count }, ct); await tx.CommitAsync(ct); return Result(comparison.Id, comparison.ComparisonNumber, comparison.Status, comparison.Version);
    }

    public async Task<Rev869BDocumentResult> RecommendAsync(string number, Rev869BRecommendComparisonRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager);
        await using var tx = await BeginTransactionScopeAsync("RecommendComparison", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request }, ct);
        var organization = RequireOrganization(); var comparison = await LoadComparisonAsync(number, ct); await AuthorizeComparisonAsync(actor, comparison, ct);
        var commandScope = Rev869BIdempotencyFingerprint.CommandScope(organization, "RecommendComparison", request.IdempotencyKey);
        var commandFingerprint = Rev869BIdempotencyFingerprint.Create(organization, "RecommendComparison", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request });
        var replay = await db.PurchaseTransactionStatusHistories.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organization && x.EntityType == "CommercialComparison" && x.EntityId == comparison.Id && x.CorrelationId.StartsWith(commandScope + "."), ct);
        if (replay is not null)
        {
            var requestedJustification = Trim(request.SingleSourceJustification) ?? comparison.SingleSourceJustification;
            if (replay.CorrelationId != commandFingerprint || replay.Action != "Recommend" || replay.Remarks != request.RecommendationRemarks.Trim() ||
                comparison.RecommendedVendorQuotationId != request.VendorQuotationId ||
                Trim(comparison.SingleSourceJustification) != Trim(requestedJustification))
                throw new Rev869BConflictException("Recommendation idempotency key was reused with a different command.");
            await tx.RollbackAsync(ct); return Result(comparison.Id, comparison.ComparisonNumber, Rev869BStatuses.PendingApproval, checked(request.Version + 1));
        }
        if (comparison.Status is not (Rev869BStatuses.Draft or Rev869BStatuses.RevisionRequested)) throw new Rev869BConflictException("Comparison cannot be recommended now.");
        var quote = await db.VendorQuotations.AsNoTracking().Include(x => x.Lines).ThenInclude(x => x.RequestForQuotationLine).SingleOrDefaultAsync(x => x.Id == request.VendorQuotationId && x.OrganizationId == organization && x.RfqVendorInvitation!.RequestForQuotationId == comparison.RequestForQuotationId && x.IsCurrentRevision && x.Status == Rev869BStatuses.TechnicallyCompliant, ct)
            ?? throw new Rev869BConflictException("Recommended quotation is outside this organization/RFQ or is not technically compliant.");
        var comparisonLineIds = comparison.Lines.Select(x => x.VendorQuotationLineId).ToHashSet(); if (quote.Lines.Any(x => !comparisonLineIds.Contains(x.Id))) throw new Rev869BConflictException("Recommended quotation line is outside this comparison.");
        var justification = Trim(request.SingleSourceJustification) ?? comparison.SingleSourceJustification;
        if (comparison.IsSingleSource && string.IsNullOrWhiteSpace(justification)) throw new Rev869BValidationException("Single-source recommendation justification is required.");
        var newVersion = checked(request.Version + 1); var remarks = RequiredRemarks(request.RecommendationRemarks);
        var recalculations = quote.Lines.ToDictionary(x => x.Id,
            x => Recalculate(x, organization, DateOnly.FromDateTime(quote.ReceivedAt.UtcDateTime)));
        var total = Rev869BCommercialCalculator.Add(recalculations.Values.Select(x => x.Breakdown.TotalPayableValue).ToArray());
        var requestingDepartmentId = await db.RequestForQuotations.AsNoTracking()
            .Where(x => x.Id == comparison.RequestForQuotationId && x.OrganizationId == organization)
            .Select(x => x.RequestingDepartmentId).SingleAsync(ct)
            ?? throw new Rev869BConflictException("Requesting department is required for approval workflow selection.");
        var workflow = await approvalWorkflow.SelectAndSnapshotAsync(organization, requestingDepartmentId, total, ct);
        var route = workflow.RouteCode; var workflowJson = approvalWorkflow.Serialize(workflow); var cycle = checked(comparison.ApprovalCycle + 1);
        Rev869BStatusContracts.RequireComparison(comparison.Status, Rev869BStatuses.PendingApproval);
        AddStatus("CommercialComparison", comparison.Id, comparison.ComparisonNumber, comparison.Status, Rev869BStatuses.PendingApproval, "Recommend", remarks, commandFingerprint);
        await OpenPendingAuthorizationAsync(ct);
        foreach (var quoteLine in quote.Lines)
        {
            var recalculated = recalculations[quoteLine.Id];
            await db.CommercialComparisonLines.Where(x => x.CommercialComparisonId == comparison.Id && x.VendorQuotationLineId == quoteLine.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, x => x.Version + 1).SetProperty(x => x.IsRecommended, true).SetProperty(x => x.RecommendationReason, remarks).SetProperty(x => x.TotalPayableValue, recalculated.Breakdown.TotalPayableValue).SetProperty(x => x.CommercialSnapshotJson, ComparisonSnapshotJson(comparison, quote, quoteLine, recalculated)).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        }
        await db.CommercialComparisonLines.Where(x => x.CommercialComparisonId == comparison.Id && !quote.Lines.Select(l => l.Id).Contains(x.VendorQuotationLineId))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, x => x.Version + 1).SetProperty(x => x.IsRecommended, false).SetProperty(x => x.RecommendationReason, (string?)null).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        var affected = await db.CommercialComparisons.Where(x => x.Id == comparison.Id && x.OrganizationId == organization && x.Version == request.Version)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, newVersion).SetProperty(x => x.RecommendedVendorQuotationId, quote.Id).SetProperty(x => x.SelectedVendorId, quote.VendorId).SetProperty(x => x.TotalPayableValue, total).SetProperty(x => x.SingleSourceJustification, justification).SetProperty(x => x.RecommendationRemarks, remarks).SetProperty(x => x.ApprovalRoute, route).SetProperty(x => x.ApprovalCycle, cycle).SetProperty(x => x.RequiredApprovalStepCount, workflow.Steps.Count).SetProperty(x => x.CompletedApprovalStepCount, 0).SetProperty(x => x.ApprovalWorkflowSnapshotJson, workflowJson).SetProperty(x => x.CreatorEmployeeId, comparison.CreatorEmployeeId == Guid.Empty ? comparison.OwnerEmployeeId : comparison.CreatorEmployeeId).SetProperty(x => x.Status, Rev869BStatuses.PendingApproval).SetProperty(x => x.TransitionCorrelationId, commandFingerprint).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, request.Version, "commercial comparison");
        await SavePreauthorizedChangesAsync(ct); await WriteAuditAsync("Purchase", "RecommendVendor", nameof(CommercialComparison), comparison.Id.ToString(), null, new { SelectedVendorId = quote.VendorId, TotalPayableValue = total, ApprovalRoute = route }, ct); await tx.CommitAsync(ct); return Result(comparison.Id, comparison.ComparisonNumber, Rev869BStatuses.PendingApproval, newVersion);
    }

    public Task<Rev869BDocumentResult> ApproveAsync(string number, Rev869BApprovalActionRequest request, CancellationToken ct) => ApprovalActionAsync(number, request, "Approve", Rev869BStatuses.Approved, ct);
    public Task<Rev869BDocumentResult> RejectAsync(string number, Rev869BApprovalActionRequest request, CancellationToken ct) => ApprovalActionAsync(number, request, "Reject", Rev869BStatuses.Rejected, ct);
    public Task<Rev869BDocumentResult> RequestRevisionAsync(string number, Rev869BApprovalActionRequest request, CancellationToken ct) => ApprovalActionAsync(number, request, "RequestRevision", Rev869BStatuses.RevisionRequested, ct);

    public async Task<Rev869BDocumentResult> ResubmitAsync(string number, Rev869BApprovalActionRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await BeginTransactionScopeAsync("ResubmitComparison", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request }, ct);
        var c = await LoadComparisonAsync(number, ct); await AuthorizeComparisonAsync(actor, c, ct);
        var commandScope = Rev869BIdempotencyFingerprint.CommandScope(c.OrganizationId, "ResubmitComparison", request.IdempotencyKey); var commandFingerprint = Rev869BIdempotencyFingerprint.Create(c.OrganizationId, "ResubmitComparison", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request });
        var replay = await db.PurchaseTransactionApprovalHistories.AsNoTracking().SingleOrDefaultAsync(x => x.CommercialComparisonId == c.Id && x.CorrelationId.StartsWith(commandScope + "."), ct);
        if (replay is not null) { if (replay.CorrelationId != commandFingerprint || replay.Action != "Resubmit" || replay.Remarks != request.Remarks.Trim()) throw new Rev869BConflictException("Resubmit idempotency key was reused."); await tx.RollbackAsync(ct); return Result(c.Id, c.ComparisonNumber, replay.ToStatus, checked(request.Version + 1)); }
        if (c.Status != Rev869BStatuses.RevisionRequested) throw new Rev869BConflictException("Only revision-requested comparison may be resubmitted.");
        var department = await db.RequestForQuotations.AsNoTracking().Where(x => x.Id == c.RequestForQuotationId).Select(x => x.RequestingDepartmentId).SingleAsync(ct)
            ?? throw new Rev869BConflictException("Requesting department is required for approval workflow selection.");
        var workflow = await approvalWorkflow.SelectAndSnapshotAsync(c.OrganizationId, department, c.TotalPayableValue, ct);
        var workflowJson = approvalWorkflow.Serialize(workflow); var cycle = checked(c.ApprovalCycle + 1);
        var version = checked(request.Version + 1); var remarks = RequiredRemarks(request.Remarks); Rev869BStatusContracts.RequireComparison(c.Status, Rev869BStatuses.PendingApproval);
        AddStatus("CommercialComparison", c.Id, c.ComparisonNumber, c.Status, Rev869BStatuses.PendingApproval, "Resubmit", remarks, commandFingerprint); AddApproval(c, "Resubmit", c.Status, Rev869BStatuses.PendingApproval, remarks, commandFingerprint);
        await OpenPendingAuthorizationAsync(ct);
        var affected = await db.CommercialComparisons.Where(x => x.Id == c.Id && x.OrganizationId == c.OrganizationId && x.Version == request.Version).ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, version).SetProperty(x => x.ApprovalRoute, workflow.RouteCode).SetProperty(x => x.ApprovalCycle, cycle).SetProperty(x => x.RequiredApprovalStepCount, workflow.Steps.Count).SetProperty(x => x.CompletedApprovalStepCount, 0).SetProperty(x => x.ApprovalWorkflowSnapshotJson, workflowJson).SetProperty(x => x.Status, Rev869BStatuses.PendingApproval).SetProperty(x => x.TransitionCorrelationId, commandFingerprint).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, request.Version, "commercial comparison");
        await SavePreauthorizedChangesAsync(ct); await WriteAuditAsync("Purchase", "ResubmitComparison", nameof(CommercialComparison), c.Id.ToString(), null, new { Status = Rev869BStatuses.PendingApproval }, ct); await tx.CommitAsync(ct); return Result(c.Id, c.ComparisonNumber, Rev869BStatuses.PendingApproval, version);
    }

    private async Task<Rev869BDocumentResult> ApprovalActionAsync(string number, Rev869BApprovalActionRequest request, string action, string next, CancellationToken ct)
    {
        var actor = RequireActor(); await using var tx = await BeginTransactionScopeAsync(action + "Comparison", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request }, ct); var c = await LoadComparisonAsync(number, ct);
        var commandScope = Rev869BIdempotencyFingerprint.CommandScope(c.OrganizationId, action + "Comparison", request.IdempotencyKey); var commandFingerprint = Rev869BIdempotencyFingerprint.Create(c.OrganizationId, action + "Comparison", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request });
        var replay = await db.PurchaseTransactionApprovalHistories.AsNoTracking().SingleOrDefaultAsync(x => x.CommercialComparisonId == c.Id && x.CorrelationId.StartsWith(commandScope + "."), ct);
        if (replay is not null) throw new Rev869BConflictException("This comparison approval decision was already recorded and cannot be replayed.");
        if (c.Status != Rev869BStatuses.PendingApproval) throw new Rev869BConflictException("Comparison is not pending approval.");
        if (next == Rev869BStatuses.Approved) await ReconcileComparisonAsync(c, ct);
        var priorEmployee = c.CompletedApprovalStepCount == 1
            ? await db.PurchaseTransactionApprovalHistories.AsNoTracking().Where(x => x.CommercialComparisonId == c.Id && x.ApprovalCycle == c.ApprovalCycle && x.StepNumber == 1 && x.Action == "Approve").Select(x => (Guid?)x.ResolvedEmployeeId).SingleOrDefaultAsync(ct)
            : null;
        var decision = approvalWorkflow.AuthorizeNextStep(c.ApprovalWorkflowSnapshotJson, c.ApprovalCycle,
            c.CompletedApprovalStepCount, c.CreatorEmployeeId, actor, user.RoleCodes, priorEmployee);
        SetApprovalActorRole(decision.ResolvedRoleCode);
        await AuthorizeComparisonAsync(actor, c, ct);
        next = action == "Approve" && !decision.CompletesDocument ? Rev869BStatuses.PendingApproval : next;
        if (c.RecommendedVendorQuotationId.HasValue && await db.QuotationTechnicalVerifications.AnyAsync(x => x.VerifierEmployeeId == actor && x.VendorQuotationLine!.VendorQuotationId == c.RecommendedVendorQuotationId, ct)) throw new UnauthorizedAccessException("Technical verifier cannot commercially approve the same quotation.");
        var remarks = RequiredRemarks(request.Remarks); var version = checked(request.Version + 1); Rev869BStatusContracts.RequireComparison(c.Status, next);
        AddStatus("CommercialComparison", c.Id, c.ComparisonNumber, c.Status, next, action, remarks, commandFingerprint); AddApproval(c, action, c.Status, next, remarks, commandFingerprint, decision);
        await OpenPendingAuthorizationAsync(ct);
        var completedSteps = action == "Approve" ? decision.CompletedStepCount : c.CompletedApprovalStepCount;
        var affected = await db.CommercialComparisons.Where(x => x.Id == c.Id && x.OrganizationId == c.OrganizationId && x.Version == request.Version).ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, version).SetProperty(x => x.CompletedApprovalStepCount, completedSteps).SetProperty(x => x.Status, next).SetProperty(x => x.TransitionCorrelationId, commandFingerprint).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, request.Version, "commercial comparison");
        await SavePreauthorizedChangesAsync(ct); await WriteAuditAsync("Purchase", action + "Comparison", nameof(CommercialComparison), c.Id.ToString(), new { status = c.Status }, new { status = next, c.ApprovalRoute }, ct); await tx.CommitAsync(ct); return Result(c.Id, c.ComparisonNumber, next, version);
    }

    public async Task<Rev869BDocumentResult> CreatePurchaseOrderAsync(Rev869BCreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await BeginTransactionScopeAsync("CreatePO", request.IdempotencyKey, request, ct); var c = await LoadComparisonAsync(request.ComparisonNumber, ct); await AuthorizeComparisonAsync(actor, c, ct);
        var poScope = Rev869BIdempotencyFingerprint.CommandScope(c.OrganizationId, "CreatePO", request.IdempotencyKey);
        var poFingerprint = Rev869BIdempotencyFingerprint.Create(c.OrganizationId, "CreatePO", request.IdempotencyKey, request);
        if (c.Status != Rev869BStatuses.Approved || !c.RecommendedVendorQuotationId.HasValue || !c.SelectedVendorId.HasValue) throw new Rev869BConflictException("PO requires approved comparison and explicit selection.");
        var duplicate = await db.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == c.OrganizationId && x.IdempotencyKey.StartsWith(poScope + "."), ct);
        if (duplicate is not null) { await AuthorizePoAsync(actor, duplicate, ct); if (duplicate.IdempotencyKey != poFingerprint || duplicate.CommercialComparisonId != c.Id) throw new Rev869BConflictException("PO idempotency key was reused for another comparison or payload."); await tx.RollbackAsync(ct); return Result(duplicate.Id, duplicate.PoNumber, Rev869BStatuses.Draft, 0); }
        await ReserveComparisonAsync(c, request.ComparisonVersion, poFingerprint, ct);
        var rfq = await db.RequestForQuotations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == c.RequestForQuotationId && x.OrganizationId == c.OrganizationId, ct) ?? throw new Rev869BConflictException("Comparison RFQ parent is invalid.");
        var quote = await db.VendorQuotations.AsNoTracking().Include(x => x.RfqVendorInvitation).Include(x => x.Lines).ThenInclude(x => x.RequestForQuotationLine)!.ThenInclude(x => x!.Item).SingleOrDefaultAsync(x => x.Id == c.RecommendedVendorQuotationId && x.OrganizationId == c.OrganizationId && x.RfqVendorInvitation!.RequestForQuotationId == rfq.Id && x.VendorId == c.SelectedVendorId, ct) ?? throw new Rev869BConflictException("Selected quotation organization/RFQ/vendor contract is invalid.");
        var comparisonApproval = await db.PurchaseTransactionApprovalHistories.AsNoTracking()
            .Where(x => x.CommercialComparisonId == c.Id && x.Action == "Approve" && x.ToStatus == Rev869BStatuses.Approved)
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct)
            ?? throw new Rev869BConflictException("Approved comparison history evidence is missing.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var category in quote.Lines.Select(x => x.RequestForQuotationLine!.Item!.CategoryId).Distinct()) if (!await vendors.IsEligibleAsync(quote.VendorId, c.OrganizationId, category, today, ct)) throw new Rev869BConflictException("Selected vendor category qualification is no longer effective.");
        var next = await NextNumberAsync(c.OrganizationId, "PO", today, ct); var po = new PurchaseOrder { OrganizationId = c.OrganizationId, PoNumber = next.Number, FinancialYear = next.Year, SequenceNumber = next.Sequence, RootPurchaseOrderId = Guid.NewGuid(), RevisionNumber = 1, CommercialComparisonId = c.Id, VendorId = quote.VendorId, RequestingDepartmentId = rfq.RequestingDepartmentId, DeliveryWarehouseId = rfq.DeliveryWarehouseId, OwnerEmployeeId = actor, CreatorEmployeeId = actor, Status = Rev869BStatuses.Draft, CurrencyCode = quote.CurrencyCode, PaymentTermsSnapshot = quote.PaymentTermsSnapshot, DeliveryTermsSnapshot = quote.DeliveryTermsSnapshot, WarrantyTermsSnapshot = quote.WarrantyTermsSnapshot, IdempotencyKey = poFingerprint, TransitionCorrelationId = poFingerprint, CreatedBy = user.LoginId };
        po.CompanyId = c.CompanyId;
        var lineNo = 0; var reconciledLines = new List<Rev869BCommercialBreakdown>();
        foreach (var ql in quote.Lines)
        {
            var source = ql.RequestForQuotationLine!; var ordered = await OrderedQuantityAsync(source.PurchaseRequisitionLineId, ct); var remaining = source.ApprovedQuantitySnapshot - ordered; if (ql.Quantity > remaining) throw new Rev869BConflictException("Cumulative PO quantity exceeds approved outstanding quantity."); var comparisonLine = c.Lines.SingleOrDefault(x => x.VendorQuotationLineId == ql.Id) ?? throw new Rev869BConflictException("Selected quotation line is outside comparison.");
            var recalculated = Recalculate(ql, c.OrganizationId, DateOnly.FromDateTime(quote.ReceivedAt.UtcDateTime)); var calc = recalculated.Breakdown;
            var input = new Rev869BCommercialInput(ql.Quantity, ql.UnitRate, ql.DiscountValue, ql.PackingForwarding, ql.Freight, ql.Insurance, ql.OtherCharges, recalculated.Tax.CgstRate, recalculated.Tax.SgstRate, recalculated.Tax.IgstRate, recalculated.Tax.CessRate, ql.RoundOff, recalculated.Tax.RoundingScale)
            { HeaderDiscountValue = ql.HeaderDiscountValue, CurrencyCode = quote.CurrencyCode, ExchangeRate = 1m };
            var immutable = new Rev869BPoCommercialSnapshot(quote.Id, ql.Id, rfq.Id, c.Id, quote.VendorId, c.OrganizationId,
                quote.RfqVendorInvitation!.VendorQualificationSnapshotJson, quote.AttachmentObjectKey, quote.AttachmentSha256,
                c.ApprovalRoute, comparisonApproval.CreatedAt, quote.ReceivedAt, input, calc)
            { QuotationRevision = quote.RevisionNumber, ItemId = source.ItemId, Quantity = ql.Quantity, Uom = source.UomSnapshot, CurrencyCode = quote.CurrencyCode, ExchangeRate = 1m };
            po.Lines.Add(new PurchaseOrderLine { CommercialComparisonLineId = comparisonLine.Id, PurchaseRequisitionLineId = source.PurchaseRequisitionLineId, PurchaseRequirementHandoffId = source.PurchaseRequirementHandoffId, ItemId = source.ItemId, LineNumber = ++lineNo, ItemCodeSnapshot = source.ItemCodeSnapshot, ItemNameSnapshot = source.ItemNameSnapshot, UomSnapshot = source.UomSnapshot, OrderedQuantity = ql.Quantity, ApprovedOutstandingQuantitySnapshot = remaining, UnitRate = ql.UnitRate, CommercialSnapshotJson = JsonSerializer.Serialize(immutable, JsonOptions), TaxRuleSnapshotJson = JsonSerializer.Serialize(recalculated.Tax, JsonOptions), TotalPayableValue = calc.TotalPayableValue, CreatedBy = user.LoginId });
            reconciledLines.Add(calc);
        }
        var aggregate = Rev869BCommercialCalculator.Aggregate(reconciledLines);
        po.TaxableValue = aggregate.TaxableValue; po.DiscountValue = aggregate.DiscountValue; po.HeaderDiscountValue = aggregate.HeaderDiscountValue; po.TaxValue = aggregate.TaxValue;
        po.PackingForwarding = aggregate.PackingForwarding; po.Freight = aggregate.Freight; po.Insurance = aggregate.Insurance;
        po.OtherCharges = aggregate.OtherCharges; po.RoundOff = aggregate.RoundOff; po.TotalPayableValue = aggregate.TotalPayableValue;
        foreach (var poLine in po.Lines) poLine.CompanyId = po.CompanyId;
        po.ApprovalRoute = c.ApprovalRoute;
        po.ApprovalPolicySnapshotJson = JsonSerializer.Serialize(new { po.OrganizationId, RouteCode = po.ApprovalRoute, ApprovalValue = po.TotalPayableValue, EffectiveOn = DateOnly.FromDateTime(DateTime.UtcNow) }, JsonOptions);
        db.PurchaseOrders.Add(po);
        AddPoHistory(po, "Create", "", po.Status, "Created from approved comparison", poFingerprint); AddStatus("PurchaseOrder", po.Id, po.PoNumber, null, po.Status, "Create", "Created from approved comparison", poFingerprint);
        await SaveAuthorizedChangesAsync(ct); await WriteAuditAsync("Purchase", "CreatePO", nameof(PurchaseOrder), po.Id.ToString(), null, new { po.PoNumber, po.TotalPayableValue }, ct); await tx.CommitAsync(ct); return Result(po.Id, po.PoNumber, po.Status, po.Version);
    }

    public async Task<Rev869BDocumentResult> SubmitPurchaseOrderAsync(string number, Rev869BSubmitPurchaseOrderRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await BeginTransactionScopeAsync("SubmitPO", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request }, ct); var organization = RequireOrganization();
        var po = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines).Where(x => x.OrganizationId == organization && x.PoNumber == number.Trim().ToUpper()).OrderByDescending(x => x.RevisionNumber).FirstOrDefaultAsync(ct)
            ?? throw new Rev869BNotFoundException("Purchase-order version was not found in the current organization.");
        await AuthorizePoAsync(actor, po, ct);
        var commandScope = Rev869BIdempotencyFingerprint.CommandScope(organization, "SubmitPO", request.IdempotencyKey); var commandFingerprint = Rev869BIdempotencyFingerprint.Create(organization, "SubmitPO", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request });
        var replay = await db.PurchaseOrderHistories.AsNoTracking().SingleOrDefaultAsync(x => x.PurchaseOrderId == po.Id && x.CorrelationId.StartsWith(commandScope + "."), ct);
        if (replay is not null)
        {
            if (replay.CorrelationId != commandFingerprint || replay.Action is not ("Submit" or "ResubmitRejected") || replay.Reason != request.Remarks.Trim()) throw new Rev869BConflictException("PO submit idempotency key was reused with a different payload.");
            await tx.RollbackAsync(ct); return Result(po.Id, po.PoNumber, replay.ToStatus, checked(request.Version + 1));
        }
        if (po.Status is not (Rev869BStatuses.Draft or Rev869BStatuses.RevisionDraft)) throw new Rev869BConflictException("Only a draft or rejected-PO revision draft may be submitted.");
        Rev869BPurchaseOrderSnapshot.RequireComplete(po, requireApproved: false);
        if (!po.RequestingDepartmentId.HasValue) throw new Rev869BConflictException("Requesting department is required for approval workflow selection.");
        var workflow = await approvalWorkflow.SelectAndSnapshotAsync(organization, po.RequestingDepartmentId.Value, po.TotalPayableValue, ct);
        var workflowJson = approvalWorkflow.Serialize(workflow); var cycle = checked(po.ApprovalCycle + 1);
        var nextStatus = po.Status == Rev869BStatuses.Draft ? Rev869BStatuses.PendingApproval : Rev869BStatuses.Resubmitted;
        var action = po.Status == Rev869BStatuses.Draft ? "Submit" : "ResubmitRejected";
        var version = checked(request.Version + 1); var remarks = RequiredRemarks(request.Remarks); Rev869BStatusContracts.RequirePurchaseOrder(po.Status, nextStatus);
        AddPoHistory(po, action, po.Status, nextStatus, remarks, commandFingerprint); AddStatus("PurchaseOrder", po.Id, po.PoNumber, po.Status, nextStatus, action, remarks, commandFingerprint);
        await OpenPendingAuthorizationAsync(ct);
        var affected = await db.PurchaseOrders.Where(x => x.Id == po.Id && x.OrganizationId == organization && x.Version == request.Version).ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, version).SetProperty(x => x.ApprovalRoute, workflow.RouteCode).SetProperty(x => x.ApprovalCycle, cycle).SetProperty(x => x.RequiredApprovalStepCount, workflow.Steps.Count).SetProperty(x => x.CompletedApprovalStepCount, 0).SetProperty(x => x.ApprovalWorkflowSnapshotJson, workflowJson).SetProperty(x => x.Status, nextStatus).SetProperty(x => x.TransitionCorrelationId, commandFingerprint).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, request.Version, "purchase order");
        await SavePreauthorizedChangesAsync(ct); await WriteAuditAsync("Purchase", action + "PO", nameof(PurchaseOrder), po.Id.ToString(), new { status = po.Status }, new { status = nextStatus, po.ApprovalRoute, po.TotalPayableValue }, ct); await tx.CommitAsync(ct); return Result(po.Id, po.PoNumber, nextStatus, version);
    }

    public async Task<Rev869BDocumentResult> IssuePurchaseOrderAsync(string number, Rev869BIssuePurchaseOrderRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await BeginTransactionScopeAsync("IssuePO", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request }, ct); var po = await LoadPoAsync(number, ct); await AuthorizePoAsync(actor, po, ct);
        var commandScope = Rev869BIdempotencyFingerprint.CommandScope(po.OrganizationId, "IssuePO", request.IdempotencyKey); var commandFingerprint = Rev869BIdempotencyFingerprint.Create(po.OrganizationId, "IssuePO", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request });
        var replay = await db.PurchaseOrderHistories.AsNoTracking().SingleOrDefaultAsync(x => x.PurchaseOrderId == po.Id && x.CorrelationId.StartsWith(commandScope + "."), ct);
        if (replay is not null) { if (replay.CorrelationId != commandFingerprint || replay.Action != "Issue" || replay.Reason != request.Remarks.Trim()) throw new Rev869BConflictException("PO issue idempotency key was reused."); await tx.RollbackAsync(ct); return Result(po.Id, po.PoNumber, replay.ToStatus, checked(request.Version + 1)); }
        if (po.Status != Rev869BStatuses.Approved || !po.IsCurrentVersion) throw new Rev869BConflictException("Only approved current PO may be issued.");
        var approvingEmployee = await db.PurchaseOrderHistories.AsNoTracking()
            .Where(x => x.PurchaseOrderId == po.Id && x.Action == "Approve" && x.ToStatus == Rev869BStatuses.Approved &&
                x.RevisionNumber == po.RevisionNumber)
            .Select(x => (Guid?)x.ActorEmployeeId).SingleOrDefaultAsync(ct)
            ?? throw new Rev869BConflictException("PO issue requires one exact approval identity for this revision.");
        if (approvingEmployee == actor)
            throw new Rev869BConflictException("The PO approver cannot issue the same controlled revision.");
        Rev869BPurchaseOrderSnapshot.RequireComplete(po);
        if (await db.MaterialFollowUpHandoffs.AnyAsync(x => x.PurchaseOrderId == po.Id, ct)) throw new Rev869BConflictException("Material Follow-up handoff already exists for this PO version.");
        var version = checked(request.Version + 1); var issuedAt = DateTimeOffset.UtcNow; Rev869BStatusContracts.RequirePurchaseOrder(po.Status, Rev869BStatuses.Issued);
        var remarks = RequiredRemarks(request.Remarks);
        foreach (var line in po.Lines)
        {
            var handoff = new MaterialFollowUpHandoff { PurchaseOrderId = po.Id, PurchaseOrderLineId = line.Id, HandoffNumber = $"MFU-{po.Id:N}-{line.LineNumber:000}", OrderedQuantitySnapshot = line.OrderedQuantity, HandoffAt = issuedAt, CorrelationId = commandFingerprint, CreatedBy = user.LoginId };
            handoff.CompanyId = po.CompanyId;
            db.MaterialFollowUpHandoffs.Add(handoff);
            AddStatus("MaterialFollowUp", handoff.Id, handoff.HandoffNumber, null, handoff.Status, "Handoff", remarks, commandFingerprint);
        }
        AddPoHistory(po, "Issue", po.Status, Rev869BStatuses.Issued, remarks, commandFingerprint); AddStatus("PurchaseOrder", po.Id, po.PoNumber, po.Status, Rev869BStatuses.Issued, "Issue", remarks, commandFingerprint);
        await OpenPendingAuthorizationAsync(ct);
        var affected = await db.PurchaseOrders.Where(x => x.Id == po.Id && x.OrganizationId == po.OrganizationId && x.Version == request.Version).ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, version).SetProperty(x => x.Status, Rev869BStatuses.Issued).SetProperty(x => x.TransitionCorrelationId, commandFingerprint).SetProperty(x => x.IssuedAt, issuedAt).SetProperty(x => x.UpdatedAt, issuedAt).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, request.Version, "purchase order");
        await SavePreauthorizedChangesAsync(ct); await WriteAuditAsync("Purchase", "IssuePO", nameof(PurchaseOrder), po.Id.ToString(), new { status = po.Status }, new { Status = Rev869BStatuses.Issued, IssuedAt = issuedAt }, ct); await tx.CommitAsync(ct); return Result(po.Id, po.PoNumber, Rev869BStatuses.Issued, version);
    }

    public async Task<Rev869BDocumentResult> AmendPurchaseOrderAsync(string number, Rev869BAmendPurchaseOrderRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await BeginTransactionScopeAsync("AmendPO", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request }, ct); var organization = RequireOrganization();
        var commandScope = Rev869BIdempotencyFingerprint.CommandScope(organization, "AmendPO", request.IdempotencyKey); var commandFingerprint = Rev869BIdempotencyFingerprint.Create(organization, "AmendPO", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request });
        var replay = await db.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organization && x.IdempotencyKey.StartsWith(commandScope + "."), ct);
        if (replay is not null)
        {
            if (replay.IdempotencyKey != commandFingerprint || replay.PoNumber != number.Trim().ToUpperInvariant() || replay.AmendmentReason != request.AmendmentReason.Trim() ||
                replay.PaymentTermsSnapshot != request.PaymentTerms.Trim() || replay.DeliveryTermsSnapshot != request.DeliveryTerms.Trim() ||
                replay.WarrantyTermsSnapshot != request.WarrantyTerms.Trim())
                throw new Rev869BConflictException("PO amendment idempotency key was reused with a different payload.");
            await AuthorizePoAsync(actor, replay, ct); await tx.RollbackAsync(ct); return Result(replay.Id, replay.PoNumber, Rev869BStatuses.Draft, 0);
        }
        var prior = await LoadPoAsync(number, ct); await AuthorizePoAsync(actor, prior, ct);
        if (prior.Status != Rev869BStatuses.Issued || !prior.IsCurrentVersion) throw new Rev869BConflictException("Only an issued current PO may be amended.");
        if (await db.PurchaseOrders.AnyAsync(x => x.PreviousVersionId == prior.Id && x.Status != Rev869BStatuses.Rejected && x.Status != Rev869BStatuses.Cancelled, ct)) throw new Rev869BConflictException("A pending amendment already exists for this issued version.");
        await ReservePoAsync(prior, request.Version, RequiredRemarks(request.AmendmentReason), commandFingerprint + ":prior", ct);
        var next = new PurchaseOrder { OrganizationId = prior.OrganizationId, PoNumber = prior.PoNumber, FinancialYear = prior.FinancialYear, SequenceNumber = prior.SequenceNumber, RootPurchaseOrderId = prior.RootPurchaseOrderId, PreviousVersionId = prior.Id, RevisionNumber = prior.RevisionNumber + 1, IsCurrentVersion = false, CommercialComparisonId = prior.CommercialComparisonId, VendorId = prior.VendorId, RequestingDepartmentId = prior.RequestingDepartmentId, DeliveryWarehouseId = prior.DeliveryWarehouseId, OwnerEmployeeId = actor, CreatorEmployeeId = actor, Status = Rev869BStatuses.Draft, CurrencyCode = prior.CurrencyCode, ApprovalRoute = prior.ApprovalRoute, TaxableValue = prior.TaxableValue, DiscountValue = prior.DiscountValue, HeaderDiscountValue = prior.HeaderDiscountValue, TaxValue = prior.TaxValue, PackingForwarding = prior.PackingForwarding, Freight = prior.Freight, Insurance = prior.Insurance, OtherCharges = prior.OtherCharges, RoundOff = prior.RoundOff, TotalPayableValue = prior.TotalPayableValue, ApprovalPolicySnapshotJson = prior.ApprovalPolicySnapshotJson, PaymentTermsSnapshot = Required(request.PaymentTerms, "Payment terms"), DeliveryTermsSnapshot = Required(request.DeliveryTerms, "Delivery terms"), WarrantyTermsSnapshot = Required(request.WarrantyTerms, "Warranty terms"), AmendmentReason = RequiredRemarks(request.AmendmentReason), IdempotencyKey = commandFingerprint, TransitionCorrelationId = commandFingerprint, CreatedBy = user.LoginId };
        foreach (var l in prior.Lines) next.Lines.Add(new PurchaseOrderLine { CommercialComparisonLineId = l.CommercialComparisonLineId, PurchaseRequisitionLineId = l.PurchaseRequisitionLineId, PurchaseRequirementHandoffId = l.PurchaseRequirementHandoffId, ItemId = l.ItemId, LineNumber = l.LineNumber, ItemCodeSnapshot = l.ItemCodeSnapshot, ItemNameSnapshot = l.ItemNameSnapshot, UomSnapshot = l.UomSnapshot, OrderedQuantity = l.OrderedQuantity, ApprovedOutstandingQuantitySnapshot = l.ApprovedOutstandingQuantitySnapshot, UnitRate = l.UnitRate, CommercialSnapshotJson = l.CommercialSnapshotJson, TaxRuleSnapshotJson = l.TaxRuleSnapshotJson, TotalPayableValue = l.TotalPayableValue, CreatedBy = user.LoginId });
        next.CompanyId = prior.CompanyId;
        foreach (var line in next.Lines) line.CompanyId = next.CompanyId;
        db.PurchaseOrders.Add(next);
        AddPoHistory(next, "Amend", prior.Status, next.Status, request.AmendmentReason, commandFingerprint); AddStatus("PurchaseOrder", next.Id, next.PoNumber, prior.Status, next.Status, "Amend", request.AmendmentReason, commandFingerprint); await SaveAuthorizedChangesAsync(ct); await WriteAuditAsync("Purchase", "AmendPO", nameof(PurchaseOrder), next.Id.ToString(), new { prior.Id, prior.RevisionNumber, prior.Status }, new { next.RevisionNumber, next.Status, next.IsCurrentVersion }, ct); await tx.CommitAsync(ct); return Result(next.Id, next.PoNumber, next.Status, next.Version);
    }

    public async Task<Rev869BDocumentResult> ReviseRejectedPurchaseOrderAsync(string number, Rev869BReviseRejectedPurchaseOrderRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseManager); await using var tx = await BeginTransactionScopeAsync("ReviseRejectedPO", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request }, ct); var organization = RequireOrganization();
        var commandScope = Rev869BIdempotencyFingerprint.CommandScope(organization, "ReviseRejectedPO", request.IdempotencyKey); var commandFingerprint = Rev869BIdempotencyFingerprint.Create(organization, "ReviseRejectedPO", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request });
        var replay = await db.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organization && x.IdempotencyKey.StartsWith(commandScope + "."), ct);
        if (replay is not null)
        {
            if (replay.IdempotencyKey != commandFingerprint || replay.PoNumber != number.Trim().ToUpperInvariant() ||
                replay.AmendmentReason != request.RevisionReason.Trim() || replay.PaymentTermsSnapshot != request.PaymentTerms.Trim() ||
                replay.DeliveryTermsSnapshot != request.DeliveryTerms.Trim() || replay.WarrantyTermsSnapshot != request.WarrantyTerms.Trim())
                throw new Rev869BConflictException("Rejected-PO revision idempotency key was reused with a different payload.");
            await AuthorizePoAsync(actor, replay, ct); await tx.RollbackAsync(ct); return Result(replay.Id, replay.PoNumber, Rev869BStatuses.RevisionDraft, 0);
        }
        var rejected = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines)
            .Where(x => x.OrganizationId == organization && x.PoNumber == number.Trim().ToUpperInvariant() && x.Status == Rev869BStatuses.Rejected)
            .OrderByDescending(x => x.RevisionNumber).FirstOrDefaultAsync(ct)
            ?? throw new Rev869BNotFoundException("Rejected initial purchase order was not found in the current organization.");
        await AuthorizePoAsync(actor, rejected, ct);
        if (await db.PurchaseOrders.AnyAsync(x => x.RootPurchaseOrderId == rejected.RootPurchaseOrderId && x.IsCurrentVersion, ct))
            throw new Rev869BConflictException("A current PO version already exists; rejected amendment recovery must retain and use its issued predecessor.");
        if (rejected.Version != request.RejectedVersion) throw new DbUpdateConcurrencyException("Stale rejected purchase-order version; reload before retrying.");
        Rev869BStatusContracts.RequirePurchaseOrder(rejected.Status, Rev869BStatuses.RevisionDraft);
        var revision = new PurchaseOrder
        {
            OrganizationId = rejected.OrganizationId, PoNumber = rejected.PoNumber, FinancialYear = rejected.FinancialYear,
            SequenceNumber = rejected.SequenceNumber, RootPurchaseOrderId = rejected.RootPurchaseOrderId, PreviousVersionId = rejected.Id,
            RevisionNumber = rejected.RevisionNumber + 1, IsCurrentVersion = true, CommercialComparisonId = rejected.CommercialComparisonId,
            VendorId = rejected.VendorId, RequestingDepartmentId = rejected.RequestingDepartmentId, DeliveryWarehouseId = rejected.DeliveryWarehouseId,
            OwnerEmployeeId = actor, CreatorEmployeeId = actor, Status = Rev869BStatuses.RevisionDraft, CurrencyCode = rejected.CurrencyCode,
            ApprovalRoute = rejected.ApprovalRoute, TaxableValue = rejected.TaxableValue, DiscountValue = rejected.DiscountValue, HeaderDiscountValue = rejected.HeaderDiscountValue,
            TaxValue = rejected.TaxValue, PackingForwarding = rejected.PackingForwarding, Freight = rejected.Freight,
            Insurance = rejected.Insurance, OtherCharges = rejected.OtherCharges, RoundOff = rejected.RoundOff,
            TotalPayableValue = rejected.TotalPayableValue, ApprovalPolicySnapshotJson = rejected.ApprovalPolicySnapshotJson, PaymentTermsSnapshot = Required(request.PaymentTerms, "Payment terms"),
            DeliveryTermsSnapshot = Required(request.DeliveryTerms, "Delivery terms"), WarrantyTermsSnapshot = Required(request.WarrantyTerms, "Warranty terms"),
            AmendmentReason = RequiredRemarks(request.RevisionReason), IdempotencyKey = commandFingerprint, TransitionCorrelationId = commandFingerprint, CreatedBy = user.LoginId
        };
        foreach (var line in rejected.Lines)
            revision.Lines.Add(new PurchaseOrderLine { CommercialComparisonLineId = line.CommercialComparisonLineId, PurchaseRequisitionLineId = line.PurchaseRequisitionLineId, PurchaseRequirementHandoffId = line.PurchaseRequirementHandoffId, ItemId = line.ItemId, LineNumber = line.LineNumber, ItemCodeSnapshot = line.ItemCodeSnapshot, ItemNameSnapshot = line.ItemNameSnapshot, UomSnapshot = line.UomSnapshot, OrderedQuantity = line.OrderedQuantity, ApprovedOutstandingQuantitySnapshot = line.ApprovedOutstandingQuantitySnapshot, UnitRate = line.UnitRate, CommercialSnapshotJson = line.CommercialSnapshotJson, TaxRuleSnapshotJson = line.TaxRuleSnapshotJson, TotalPayableValue = line.TotalPayableValue, CreatedBy = user.LoginId });
        revision.CompanyId = rejected.CompanyId;
        foreach (var line in revision.Lines) line.CompanyId = revision.CompanyId;
        db.PurchaseOrders.Add(revision);
        AddPoHistory(revision, "ReviseRejected", rejected.Status, revision.Status, request.RevisionReason, commandFingerprint);
        AddStatus("PurchaseOrder", revision.Id, revision.PoNumber, rejected.Status, revision.Status, "ReviseRejected", request.RevisionReason, commandFingerprint);
        await SaveAuthorizedChangesAsync(ct); await WriteAuditAsync("Purchase", "ReviseRejectedPO", nameof(PurchaseOrder), revision.Id.ToString(), new { rejected.Id, rejected.RevisionNumber, rejected.Status }, new { revision.RevisionNumber, revision.Status }, ct);
        await tx.CommitAsync(ct); return Result(revision.Id, revision.PoNumber, revision.Status, revision.Version);
    }

    public async Task<Rev869BDocumentResult> ApprovePurchaseOrderAsync(string number, Rev869BPoApprovalActionRequest request, CancellationToken ct) => await PurchaseOrderApprovalActionAsync(number, request, true, ct);
    public async Task<Rev869BDocumentResult> RejectPurchaseOrderAsync(string number, Rev869BPoApprovalActionRequest request, CancellationToken ct) => await PurchaseOrderApprovalActionAsync(number, request, false, ct);

    private async Task<Rev869BDocumentResult> PurchaseOrderApprovalActionAsync(string number, Rev869BPoApprovalActionRequest request, bool approve, CancellationToken ct)
    {
        var actor = RequireActor(); var action = approve ? "Approve" : "Reject"; await using var tx = await BeginTransactionScopeAsync(action + "PO", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request }, ct); var organization = RequireOrganization();
        var po = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines).Where(x => x.OrganizationId == organization && x.PoNumber == number.Trim().ToUpper()).OrderByDescending(x => x.RevisionNumber).FirstOrDefaultAsync(ct)
            ?? throw new Rev869BNotFoundException("Purchase-order version was not found in the current organization.");
        var next = approve ? Rev869BStatuses.Approved : Rev869BStatuses.Rejected;
        var commandScope = Rev869BIdempotencyFingerprint.CommandScope(organization, action + "PO", request.IdempotencyKey); var commandFingerprint = Rev869BIdempotencyFingerprint.Create(organization, action + "PO", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request });
        var replay = await db.PurchaseOrderHistories.AsNoTracking().SingleOrDefaultAsync(x => x.PurchaseOrderId == po.Id && x.CorrelationId.StartsWith(commandScope + "."), ct);
        if (replay is not null) throw new Rev869BConflictException("This purchase-order approval decision was already recorded and cannot be replayed.");
        if (po.Status is not (Rev869BStatuses.PendingApproval or Rev869BStatuses.Resubmitted)) throw new Rev869BConflictException("Purchase order is not pending approval.");
        Rev869BPurchaseOrderSnapshot.RequireComplete(po, requireApproved: false);
        var calculatedTotal = Rev869BCommercialCalculator.Add(po.Lines.Select(x => x.TotalPayableValue).ToArray()); if (calculatedTotal != po.TotalPayableValue) throw new Rev869BConflictException("PO header/line payable values are inconsistent.");
        var priorStepEmployee = po.CompletedApprovalStepCount == 1
            ? await db.PurchaseOrderHistories.AsNoTracking().Where(x => x.PurchaseOrderId == po.Id && x.ApprovalCycle == po.ApprovalCycle && x.StepNumber == 1 && x.Action == "Approve").Select(x => x.ResolvedEmployeeId).SingleOrDefaultAsync(ct)
            : null;
        var decision = approvalWorkflow.AuthorizeNextStep(po.ApprovalWorkflowSnapshotJson, po.ApprovalCycle,
            po.CompletedApprovalStepCount, po.CreatorEmployeeId, actor, user.RoleCodes, priorStepEmployee);
        SetApprovalActorRole(decision.ResolvedRoleCode);
        await AuthorizePoAsync(actor, po, ct);
        next = approve && !decision.CompletesDocument ? Rev869BStatuses.PendingApproval : next;
        var remarks = RequiredRemarks(request.Remarks); var version = checked(request.Version + 1); Rev869BStatusContracts.RequirePurchaseOrder(po.Status, next);
        PurchaseOrder? prior = null;
        uint? priorExpectedVersion = null;
        if (approve && decision.CompletesDocument && po.PreviousVersionId.HasValue)
        {
            prior = await db.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == po.PreviousVersionId && x.OrganizationId == organization, ct) ?? throw new Rev869BConflictException("Controlled predecessor is unavailable.");
            if (prior.Status == Rev869BStatuses.Issued)
            {
                if (!prior.IsCurrentVersion) throw new Rev869BConflictException("Issued current predecessor is unavailable.");
                if (!request.ExpectedCurrentVersion.HasValue) throw new Rev869BValidationException("Expected current PO version is required for amendment approval.");
                priorExpectedVersion = request.ExpectedCurrentVersion.Value; Rev869BStatusContracts.RequirePurchaseOrder(prior.Status, Rev869BStatuses.Superseded);
                AddPoHistory(prior, "Supersede", prior.Status, Rev869BStatuses.Superseded, remarks, commandFingerprint + ":prior");
                AddStatus("PurchaseOrder", prior.Id, prior.PoNumber, prior.Status, Rev869BStatuses.Superseded,
                    "Supersede", remarks, commandFingerprint + ":prior");
            }
            else if (prior.Status != Rev869BStatuses.Rejected || prior.IsCurrentVersion)
                throw new Rev869BConflictException("Rejected-version recovery predecessor is invalid.");
        }
        var nextIsCurrent = approve && decision.CompletesDocument ? true : po.IsCurrentVersion;
        AddPoHistory(po, action, po.Status, next, remarks, commandFingerprint, decision); AddStatus("PurchaseOrder", po.Id, po.PoNumber, po.Status, next, action, remarks, commandFingerprint);
        await OpenPendingAuthorizationAsync(ct);
        if (prior is not null && priorExpectedVersion.HasValue)
        {
            var priorExpected = priorExpectedVersion.Value; var priorVersion = checked(priorExpected + 1);
            var priorAffected = await db.PurchaseOrders.Where(x => x.Id == prior.Id && x.OrganizationId == organization && x.Version == priorExpected).ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, priorVersion).SetProperty(x => x.Status, Rev869BStatuses.Superseded).SetProperty(x => x.IsCurrentVersion, false).SetProperty(x => x.TransitionCorrelationId, commandFingerprint + ":prior").SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
            RequireCas(priorAffected, priorExpected, "purchase-order predecessor");
        }
        var completedSteps = approve ? decision.CompletedStepCount : po.CompletedApprovalStepCount;
        var affected = await db.PurchaseOrders.Where(x => x.Id == po.Id && x.OrganizationId == organization && x.Version == request.Version).ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, version).SetProperty(x => x.CompletedApprovalStepCount, completedSteps).SetProperty(x => x.Status, next).SetProperty(x => x.IsCurrentVersion, nextIsCurrent).SetProperty(x => x.TransitionCorrelationId, commandFingerprint).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, request.Version, "purchase order");
        await SavePreauthorizedChangesAsync(ct); await WriteAuditAsync("Purchase", action + "PO", nameof(PurchaseOrder), po.Id.ToString(), new { status = po.Status, po.IsCurrentVersion }, new { status = next, IsCurrentVersion = nextIsCurrent, decision.RouteCode, calculatedTotal }, ct); await tx.CommitAsync(ct); return Result(po.Id, po.PoNumber, next, version);
    }

    public async Task<Rev869BDocumentResult> CancelPurchaseOrderAsync(string number, Rev869BCancelPurchaseOrderRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.TechnicalDirector, Rev869ARoleCodes.ManagingDirector); await using var tx = await BeginTransactionScopeAsync("CancelPO", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request }, ct); var po = await LoadPoAsync(number, ct); await AuthorizePoAsync(actor, po, ct);
        var commandScope = Rev869BIdempotencyFingerprint.CommandScope(po.OrganizationId, "CancelPO", request.IdempotencyKey); var commandFingerprint = Rev869BIdempotencyFingerprint.Create(po.OrganizationId, "CancelPO", request.IdempotencyKey, new { number = number.Trim().ToUpperInvariant(), request });
        var replay = await db.PurchaseOrderHistories.AsNoTracking().SingleOrDefaultAsync(x => x.PurchaseOrderId == po.Id && x.CorrelationId.StartsWith(commandScope + "."), ct);
        if (replay is not null) { if (replay.CorrelationId != commandFingerprint || replay.Action != "Cancel" || replay.Reason != request.Reason.Trim()) throw new Rev869BConflictException("PO cancellation idempotency key was reused."); await tx.RollbackAsync(ct); return Result(po.Id, po.PoNumber, replay.ToStatus, checked(request.Version + 1)); }
        var reason = RequiredRemarks(request.Reason); var version = checked(request.Version + 1); Rev869BStatusContracts.RequirePurchaseOrder(po.Status, Rev869BStatuses.Cancelled); var cancelledAt = DateTimeOffset.UtcNow;
        AddPoHistory(po, "Cancel", po.Status, Rev869BStatuses.Cancelled, reason, commandFingerprint); AddStatus("PurchaseOrder", po.Id, po.PoNumber, po.Status, Rev869BStatuses.Cancelled, "Cancel", reason, commandFingerprint);
        await OpenPendingAuthorizationAsync(ct);
        var affected = await db.PurchaseOrders.Where(x => x.Id == po.Id && x.OrganizationId == po.OrganizationId && x.Version == request.Version).ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, version).SetProperty(x => x.Status, Rev869BStatuses.Cancelled).SetProperty(x => x.TransitionCorrelationId, commandFingerprint).SetProperty(x => x.CancelledAt, cancelledAt).SetProperty(x => x.CancellationReason, reason).SetProperty(x => x.UpdatedAt, cancelledAt).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, request.Version, "purchase order");
        await SavePreauthorizedChangesAsync(ct); await WriteAuditAsync("Purchase", "CancelPO", nameof(PurchaseOrder), po.Id.ToString(), new { status = po.Status }, new { Status = Rev869BStatuses.Cancelled, CancelledAt = cancelledAt }, ct); await tx.CommitAsync(ct); return Result(po.Id, po.PoNumber, Rev869BStatuses.Cancelled, version);
    }
}
