using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Infrastructure.Purchase;

public sealed partial class EfRev869BPurchaseService
{
    public async Task<Rev869BDocumentResult> CreateRfqAsync(Rev869BCreateRfqRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager);
        if (request.Lines.Count == 0 || request.QuoteDueAt <= DateTimeOffset.UtcNow || string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new Rev869BValidationException("RFQ lines, future due date and idempotency key are required.");
        var currency = NormalizeCurrency(request.CurrencyCode);
        if (request.IsSingleSource && string.IsNullOrWhiteSpace(request.SingleSourceJustification)) throw new InvalidOperationException("Single-source RFQ justification is required.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var organization = RequireOrganization();
        var existing = await db.RequestForQuotations.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == organization && x.IdempotencyKey == request.IdempotencyKey.Trim(), ct);
        if (existing is not null)
        {
            await RequireScopeAsync(actor, organization, existing.RequestingDepartmentId, existing.DeliveryWarehouseId, null, existing.OwnerEmployeeId, ct);
            var requested = request.Lines.OrderBy(x => x.PurchaseRequirementHandoffId).Select(x => (x.PurchaseRequirementHandoffId, x.Quantity)).ToArray();
            var persisted = existing.Lines.OrderBy(x => x.PurchaseRequirementHandoffId).Select(x => (x.PurchaseRequirementHandoffId, x.RfqQuantity)).ToArray();
            if (existing.QuoteDueAt != request.QuoteDueAt || existing.CurrencyCode != currency || existing.IsSingleSource != request.IsSingleSource || !requested.SequenceEqual(persisted)) throw new Rev869BConflictException("RFQ idempotency key was reused with a different payload.");
            await tx.RollbackAsync(ct); return Result(existing.Id, existing.RfqNumber, existing.Status, existing.Version);
        }
        var ids = request.Lines.Select(x => x.PurchaseRequirementHandoffId).Distinct().ToArray(); if (ids.Length != request.Lines.Count) throw new InvalidOperationException("Duplicate PendingRFQ handoff in request.");
        var handoffs = await db.PurchaseRequirementHandoffs.Include(x => x.PurchaseRequisition).Include(x => x.PurchaseRequisitionLine)!.ThenInclude(x => x!.Item).Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        if (handoffs.Count != ids.Length || handoffs.Select(x => x.PurchaseRequisitionId).Distinct().Count() != 1) throw new InvalidOperationException("RFQ must reuse valid handoffs from one PR.");
        var pr = handoffs[0].PurchaseRequisition ?? throw new InvalidOperationException("PR unavailable.");
        if (!pr.IsActive || pr.Status is PurchaseRequisitionStatuses.Draft or PurchaseRequisitionStatuses.Submitted or PurchaseRequisitionStatuses.DepartmentVerified or PurchaseRequisitionStatuses.PendingApproval or PurchaseRequisitionStatuses.Rejected or PurchaseRequisitionStatuses.RevisionRequested or PurchaseRequisitionStatuses.Cancelled) throw new InvalidOperationException("PR has not reached an approved PendingRFQ state.");
        await RequireScopeAsync(actor, pr.OrganizationId, pr.RequestingDepartmentId, pr.DeliveryWarehouseId, null, pr.RequesterEmployeeId, ct);
        var quantities = request.Lines.ToDictionary(x => x.PurchaseRequirementHandoffId, x => x.Quantity);
        foreach (var h in handoffs)
        {
            if (h.Status != "PendingRFQ" || h.HandoffQuantity <= 0 || h.PurchaseRequisitionLine?.ItemId is null) throw new InvalidOperationException("Only valid PendingRFQ handoffs with Item Master references may be sourced.");
            var sourced = await db.RequestForQuotationLines.Where(x => x.PurchaseRequirementHandoffId == h.Id && x.RequestForQuotation!.Status != Rev869BStatuses.Cancelled).SumAsync(x => (decimal?)x.RfqQuantity, ct) ?? 0m;
            if (quantities[h.Id] <= 0 || sourced + quantities[h.Id] > h.HandoffQuantity) throw new InvalidOperationException("RFQ split exceeds approved handoff quantity.");
        }
        var today = DateOnly.FromDateTime(DateTime.UtcNow); var next = await NextNumberAsync(organization, "RFQ", today, ct);
        var rfq = new RequestForQuotation { OrganizationId = organization, RfqNumber = next.Number, FinancialYear = next.Year, SequenceNumber = next.Sequence, PurchaseRequisitionId = pr.Id, RequestingDepartmentId = pr.RequestingDepartmentId, DeliveryWarehouseId = pr.DeliveryWarehouseId, OwnerEmployeeId = actor, QuoteDueAt = request.QuoteDueAt, CurrencyCode = currency, IsSingleSource = request.IsSingleSource, SingleSourceJustification = Trim(request.SingleSourceJustification), IdempotencyKey = request.IdempotencyKey.Trim(), CreatedBy = user.LoginId };
        var lineNo = 0;
        foreach (var h in handoffs.OrderBy(x => x.PurchaseRequisitionLine!.LineNumber))
        {
            var line = h.PurchaseRequisitionLine!; var ordered = await OrderedQuantityAsync(line.Id, ct); var outstanding = h.HandoffQuantity - ordered; if (outstanding <= 0 || quantities[h.Id] > outstanding) throw new InvalidOperationException("Approved outstanding quantity is exhausted.");
            rfq.Lines.Add(new RequestForQuotationLine { PurchaseRequirementHandoffId = h.Id, PurchaseRequisitionLineId = line.Id, ItemId = line.ItemId!.Value, LineNumber = ++lineNo, PrNumberSnapshot = pr.PrNumber, PrLineNumberSnapshot = line.LineNumber, ItemCodeSnapshot = line.ItemCodeSnapshot, ItemNameSnapshot = line.ItemNameSnapshot, UomSnapshot = line.UomSnapshot, SpecificationSnapshot = line.SpecificationSnapshot, ApprovedQuantitySnapshot = h.HandoffQuantity, AlreadyOrderedQuantitySnapshot = ordered, OutstandingQuantitySnapshot = outstanding, RfqQuantity = quantities[h.Id], RequiredDateSnapshot = line.RequiredDate, CreatedBy = user.LoginId });
        }
        db.RequestForQuotations.Add(rfq); AddStatus("RFQ", rfq.Id, rfq.RfqNumber, null, rfq.Status, "Create", "Created from existing PendingRFQ handoff", request.IdempotencyKey); await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", "CreateRFQ", nameof(RequestForQuotation), rfq.Id.ToString(), null, new { rfq.RfqNumber, rfq.PurchaseRequisitionId, lineCount = rfq.Lines.Count }, ct); await tx.CommitAsync(ct); return Result(rfq.Id, rfq.RfqNumber, rfq.Status, rfq.Version);
    }

    public async Task<Rev869BDocumentResult> InviteVendorAsync(string rfqNumber, Rev869BInviteVendorRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var organization = RequireOrganization();
        var rfq = await db.RequestForQuotations.AsNoTracking().Include(x => x.Lines).ThenInclude(x => x.Item)
            .SingleOrDefaultAsync(x => x.OrganizationId == organization && x.RfqNumber == rfqNumber.Trim().ToUpper(), ct)
            ?? throw new Rev869BNotFoundException("RFQ was not found in the current organization.");
        await RequireScopeAsync(actor, organization, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, null, rfq.OwnerEmployeeId, ct);
        var duplicate = await db.RfqVendorInvitations.AsNoTracking().SingleOrDefaultAsync(x => x.RequestForQuotationId == rfq.Id && x.VendorId == request.VendorId, ct);
        if (duplicate is not null)
        {
            if (duplicate.IdempotencyKey != request.IdempotencyKey.Trim()) throw new Rev869BConflictException("Vendor was already invited with a different idempotency key.");
            await tx.RollbackAsync(ct); return Result(duplicate.Id, rfq.RfqNumber, duplicate.Status, duplicate.Version);
        }
        if (rfq.Status is Rev869BStatuses.Cancelled or Rev869BStatuses.Closed) throw new Rev869BConflictException("RFQ is closed.");
        foreach (var category in rfq.Lines.Select(x => x.Item!.CategoryId).Distinct()) if (!await vendors.IsEligibleAsync(request.VendorId, rfq.OrganizationId, category, DateOnly.FromDateTime(DateTime.UtcNow), ct)) throw new InvalidOperationException("Vendor is not active, approved, effective and qualified.");
        var newVersion = await ReserveRfqAsync(rfq.Id, organization, request.RfqVersion, ct);
        var invitation = new RfqVendorInvitation { RequestForQuotationId = rfq.Id, VendorId = request.VendorId, InvitedAt = DateTimeOffset.UtcNow, QuoteDueAtSnapshot = rfq.QuoteDueAt, VendorQualificationSnapshotJson = JsonSerializer.Serialize(new { eligible = true, checkedAt = DateTimeOffset.UtcNow }, JsonOptions), IdempotencyKey = request.IdempotencyKey.Trim(), CreatedBy = user.LoginId };
        db.RfqVendorInvitations.Add(invitation);
        if (rfq.Status == Rev869BStatuses.Draft)
        {
            Rev869BStatusContracts.RequireRfq(rfq.Status, Rev869BStatuses.Issued);
            await db.RequestForQuotations.Where(x => x.Id == rfq.Id && x.OrganizationId == organization && x.Version == newVersion)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, Rev869BStatuses.Issued).SetProperty(x => x.IssuedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        }
        AddStatus("RFQInvitation", invitation.Id, rfq.RfqNumber, null, invitation.Status, "InviteVendor", RequiredRemarks(request.Remarks), request.IdempotencyKey); await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", "InviteVendor", nameof(RfqVendorInvitation), invitation.Id.ToString(), null, new { rfq.RfqNumber, request.VendorId }, ct); await tx.CommitAsync(ct); return Result(invitation.Id, rfq.RfqNumber, invitation.Status, invitation.Version);
    }

    public async Task<Rev869BDocumentResult> SubmitQuotationRevisionAsync(Guid invitationId, Rev869BSubmitQuotationRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.PurchaseManager);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var organization = RequireOrganization();
        var invitation = await db.RfqVendorInvitations.AsNoTracking().Include(x => x.RequestForQuotation)!.ThenInclude(x => x!.Lines).ThenInclude(x => x.Item)
            .SingleOrDefaultAsync(x => x.Id == invitationId && x.RequestForQuotation!.OrganizationId == organization, ct)
            ?? throw new Rev869BNotFoundException("RFQ invitation was not found in the current organization.");
        var rfq = invitation.RequestForQuotation!;
        await RequireScopeAsync(actor, organization, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, null, rfq.OwnerEmployeeId, ct);
        var replay = await db.VendorQuotations.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organization && x.IdempotencyKey == request.IdempotencyKey.Trim(), ct);
        if (replay is not null)
        {
            if (replay.RfqVendorInvitationId != invitationId || replay.VendorQuoteReference != request.VendorQuoteReference.Trim()) throw new Rev869BConflictException("Quotation idempotency key was reused with a different payload.");
            await tx.RollbackAsync(ct); return Result(replay.Id, replay.QuotationNumber, replay.Status, replay.Version);
        }
        if (NormalizeCurrency(request.CurrencyCode) != rfq.CurrencyCode) throw new InvalidOperationException("Currency conversion is not configured; quote must match RFQ currency.");
        if (request.Lines.Count != rfq.Lines.Count || request.Lines.Select(x => x.RequestForQuotationLineId).Distinct().Count() != rfq.Lines.Count) throw new InvalidOperationException("Quotation must contain every RFQ line exactly once.");
        foreach (var category in rfq.Lines.Select(x => x.Item!.CategoryId).Distinct()) if (!await vendors.IsEligibleAsync(invitation.VendorId, rfq.OrganizationId, category, DateOnly.FromDateTime(DateTime.UtcNow), ct)) throw new InvalidOperationException("Vendor qualification is missing or expired.");
        var source = Required(request.SubmissionSource, "Submission source").ToUpperInvariant();
        if (source is not ("EMAIL_RECEIVED" or "PHYSICAL_RECEIVED")) throw new Rev869BValidationException("Internal entry must identify an approved received-on-behalf-of-vendor source.");
        if (request.ReceivedAt > DateTimeOffset.UtcNow || request.ReceivedAt == default || Required(request.AttachmentSha256, "Attachment SHA-256").Length != 64) throw new Rev869BValidationException("Received timestamp and 64-character attachment SHA-256 are required.");
        var now = DateTimeOffset.UtcNow; var late = request.ReceivedAt > invitation.QuoteDueAtSnapshot;
        if (late && (!request.RequestLateAuthorization || !IsRole(Rev869ARoleCodes.PurchaseManager) || string.IsNullOrWhiteSpace(request.LateAuthorizationRemarks))) throw new Rev869BValidationException("Late quotation/revision requires Purchase Manager authorization and remarks.");
        var previous = await db.VendorQuotations.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == organization && x.RfqVendorInvitationId == invitationId && x.IsCurrentRevision, ct);
        if (previous is null && request.PreviousQuotationVersion.HasValue || previous is not null && request.PreviousQuotationVersion != previous.Version) throw new DbUpdateConcurrencyException("Previous quotation version is stale or inconsistent.");
        await ReserveInvitationAsync(invitation.Id, organization, request.InvitationVersion, ct);
        if (previous is not null) await ReserveQuotationAsync(previous.Id, organization, request.PreviousQuotationVersion!.Value, ct);
        var next = await NextNumberAsync(organization, "VQ", DateOnly.FromDateTime(now.UtcDateTime), ct);
        var quote = new VendorQuotation { OrganizationId = organization, QuotationNumber = next.Number, FinancialYear = next.Year, SequenceNumber = next.Sequence, RfqVendorInvitationId = invitation.Id, VendorId = invitation.VendorId, RootQuotationId = previous?.RootQuotationId ?? Guid.NewGuid(), PreviousRevisionId = previous?.Id, RevisionNumber = (previous?.RevisionNumber ?? 0) + 1, VendorQuoteReference = Required(request.VendorQuoteReference, "Vendor quote reference"), SubmissionSource = source, ReceivedAt = request.ReceivedAt, AttachmentObjectKey = Required(request.AttachmentObjectKey, "Attachment object key"), AttachmentSha256 = request.AttachmentSha256.Trim().ToUpperInvariant(), VendorAttestation = Required(request.VendorAttestation, "Vendor attestation"), CurrencyCode = rfq.CurrencyCode, Status = Rev869BStatuses.Submitted, SubmittedAt = now, IsLateSubmission = late, LateAuthorizedByEmployeeId = late ? actor : null, LateAuthorizationRemarks = late ? request.LateAuthorizationRemarks!.Trim() : null, PaymentTermsSnapshot = Required(request.PaymentTerms, "Payment terms"), DeliveryTermsSnapshot = Required(request.DeliveryTerms, "Delivery terms"), WarrantyTermsSnapshot = Required(request.WarrantyTerms, "Warranty terms"), IdempotencyKey = request.IdempotencyKey.Trim(), CreatedBy = user.LoginId };
        if (previous is not null)
        {
            Rev869BStatusContracts.RequireQuotation(previous.Status, Rev869BStatuses.Superseded);
            await db.VendorQuotations.Where(x => x.Id == previous.Id && x.OrganizationId == organization && x.Version == previous.Version + 1)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsCurrentRevision, false).SetProperty(x => x.Status, Rev869BStatuses.Superseded).SetProperty(x => x.UpdatedAt, now).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        }
        var lineNo = 0;
        foreach (var input in request.Lines)
        {
            var rfqLine = rfq.Lines.SingleOrDefault(x => x.Id == input.RequestForQuotationLineId) ?? throw new InvalidOperationException("Quotation line is outside RFQ."); if (input.Quantity <= 0 || input.Quantity > rfqLine.RfqQuantity) throw new InvalidOperationException("Quotation quantity exceeds RFQ.");
            var taxableBase = decimal.Round(input.Quantity * input.UnitRate, 2, MidpointRounding.AwayFromZero) - input.DiscountValue;
            var tax = await taxes.ResolveAsync(new TaxResolutionRequest(rfq.OrganizationId, TaxJurisdictions.IndiaGst, Required(input.HsnSacCode, "HSN/SAC"), input.SupplierStateCode, input.PlaceOfSupplyStateCode, input.VendorRegistrationType, DateOnly.FromDateTime(now.UtcDateTime), taxableBase), ct);
            var calc = Rev869BCommercialCalculator.Calculate(new(input.Quantity, input.UnitRate, input.DiscountValue, input.PackingForwarding, input.Freight, input.Insurance, input.OtherCharges, tax.CgstRate, tax.SgstRate, tax.IgstRate, tax.CessRate, input.RoundOff, tax.RoundingScale));
            quote.Lines.Add(new VendorQuotationLine { RequestForQuotationLineId = rfqLine.Id, LineNumber = ++lineNo, Quantity = input.Quantity, UnitRate = input.UnitRate, DiscountValue = calc.DiscountValue, PackingForwarding = calc.PackingForwarding, Freight = calc.Freight, Insurance = calc.Insurance, OtherCharges = calc.OtherCharges, TaxableValue = calc.TaxableValue, TaxGstSettingId = tax.Id, TaxRuleSnapshotJson = JsonSerializer.Serialize(tax, JsonOptions), HsnSacCode = Required(input.HsnSacCode, "HSN/SAC"), SupplierStateCode = Required(input.SupplierStateCode, "Supplier state"), PlaceOfSupplyStateCode = Required(input.PlaceOfSupplyStateCode, "Place of supply"), VendorRegistrationType = Required(input.VendorRegistrationType, "Vendor registration type"), CgstValue = calc.CgstValue, SgstValue = calc.SgstValue, IgstValue = calc.IgstValue, CessValue = calc.CessValue, RoundOff = calc.RoundOff, TotalPayableValue = calc.TotalPayableValue, PromisedDeliveryDate = input.PromisedDeliveryDate, CreatedBy = user.LoginId });
        }
        quote.TotalPayableValue = quote.Lines.Sum(x => x.TotalPayableValue);
        db.VendorQuotations.Add(quote);
        if (invitation.Status == Rev869BStatuses.Issued)
        {
            Rev869BStatusContracts.RequireInvitation(invitation.Status, Rev869BStatuses.Submitted);
            await db.RfqVendorInvitations.Where(x => x.Id == invitation.Id && x.Version == request.InvitationVersion + 1)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, Rev869BStatuses.Submitted).SetProperty(x => x.UpdatedAt, now).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        }
        AddStatus("VendorQuotation", quote.Id, quote.QuotationNumber, previous?.Status, quote.Status, previous is null ? "Submit" : "Revise", late ? RequiredRemarks(request.LateAuthorizationRemarks) : "Quotation submitted", request.IdempotencyKey);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", previous is null ? "SubmitQuotation" : "ReviseQuotation", nameof(VendorQuotation), quote.Id.ToString(), previous is null ? null : new { previous.Id, previous.RevisionNumber }, new { quote.QuotationNumber, quote.RevisionNumber, quote.TotalPayableValue, quote.SubmissionSource, quote.ReceivedAt, quote.AttachmentSha256 }, ct); await tx.CommitAsync(ct); return Result(quote.Id, quote.QuotationNumber, quote.Status, quote.Version);
    }

    public async Task<Rev869BDocumentResult> VerifyTechnicalAsync(string quotationNumber, Rev869BTechnicalVerificationRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole("TECHNICAL_ENGINEER", Rev869ARoleCodes.TechnicalDirector);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var organization = RequireOrganization();
        var line = await db.VendorQuotationLines.AsNoTracking().Include(x => x.VendorQuotation)!.ThenInclude(x => x!.Lines).Include(x => x.VendorQuotation)!.ThenInclude(x => x!.RfqVendorInvitation)!.ThenInclude(x => x!.RequestForQuotation)
            .SingleOrDefaultAsync(x => x.Id == request.VendorQuotationLineId && x.VendorQuotation!.OrganizationId == organization && x.VendorQuotation.QuotationNumber == quotationNumber.Trim().ToUpper(), ct)
            ?? throw new Rev869BNotFoundException("Quotation line was not found in the current organization/quotation.");
        var quote = line.VendorQuotation!; var rfq = quote.RfqVendorInvitation!.RequestForQuotation!;
        await RequireScopeAsync(actor, organization, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, null, rfq.OwnerEmployeeId, ct);
        var prior = await db.QuotationTechnicalVerifications.AsNoTracking().SingleOrDefaultAsync(x => x.VendorQuotationLineId == line.Id, ct);
        var compliance = request.IsCompliant ? Rev869BStatuses.TechnicallyCompliant : Rev869BStatuses.TechnicallyRejected;
        if (prior is not null)
        {
            if (prior.CorrelationId != request.IdempotencyKey.Trim() || prior.ComplianceStatus != compliance || prior.Remarks != request.Remarks.Trim()) throw new Rev869BConflictException("Quotation line already has a different immutable technical verification.");
            await tx.RollbackAsync(ct); return Result(prior.Id, quote.QuotationNumber, prior.ComplianceStatus, prior.Version);
        }
        if (!quote.IsCurrentRevision || quote.Status != Rev869BStatuses.Submitted) throw new Rev869BConflictException("Technical verification requires an unverified line on the current submitted quotation.");
        var quoteVersion = await ReserveQuotationAsync(quote.Id, organization, request.QuotationVersion, ct);
        var verification = new QuotationTechnicalVerification { VendorQuotationLineId = line.Id, VerifierEmployeeId = actor, ComplianceStatus = compliance, ComplianceSnapshotJson = Required(request.ComplianceEvidenceJson, "Technical evidence"), Remarks = RequiredRemarks(request.Remarks), VerifiedAt = DateTimeOffset.UtcNow, CorrelationId = request.IdempotencyKey.Trim(), CreatedBy = user.LoginId };
        db.QuotationTechnicalVerifications.Add(verification); await db.SaveChangesAsync(ct);
        var lineIds = quote.Lines.Select(x => x.Id).ToArray();
        var all = await db.QuotationTechnicalVerifications.CountAsync(x => lineIds.Contains(x.VendorQuotationLineId), ct);
        if (all == quote.Lines.Count)
        {
            var finalStatus = await db.QuotationTechnicalVerifications.Where(x => lineIds.Contains(x.VendorQuotationLineId)).AllAsync(x => x.ComplianceStatus == Rev869BStatuses.TechnicallyCompliant, ct) ? Rev869BStatuses.TechnicallyCompliant : Rev869BStatuses.TechnicallyRejected;
            Rev869BStatusContracts.RequireQuotation(quote.Status, finalStatus);
            await db.VendorQuotations.Where(x => x.Id == quote.Id && x.OrganizationId == organization && x.Version == quoteVersion)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, finalStatus).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        }
        AddStatus("TechnicalVerification", verification.Id, quote.QuotationNumber, null, verification.ComplianceStatus, "Verify", verification.Remarks, request.IdempotencyKey);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("Purchase", "TechnicalVerification", nameof(QuotationTechnicalVerification), verification.Id.ToString(), null, new { quote.QuotationNumber, verification.ComplianceStatus }, ct); await tx.CommitAsync(ct); return Result(verification.Id, quote.QuotationNumber, verification.ComplianceStatus, verification.Version);
    }
}
