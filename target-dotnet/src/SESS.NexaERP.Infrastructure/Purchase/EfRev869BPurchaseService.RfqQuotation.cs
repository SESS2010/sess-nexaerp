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
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseExecutive);
        if (request.Lines.Count == 0 || request.QuoteDueAt <= DateTimeOffset.UtcNow || string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new Rev869BValidationException("RFQ lines, future due date and idempotency key are required.");
        var currency = NormalizeCurrency(request.CurrencyCode);
        if (request.IsSingleSource && string.IsNullOrWhiteSpace(request.SingleSourceJustification)) throw new InvalidOperationException("Single-source RFQ justification is required.");
        var organization = RequireOrganization();
        var scope = Rev869BIdempotencyFingerprint.CommandScope(organization, "CreateRFQ", request.IdempotencyKey);
        var fingerprint = Rev869BIdempotencyFingerprint.Create(organization, "CreateRFQ", request.IdempotencyKey, request);
        var ids = request.Lines.Select(x => x.PurchaseRequirementHandoffId).Distinct().ToArray();
        if (ids.Length != request.Lines.Count) throw new InvalidOperationException("Duplicate PendingRFQ handoff in request.");
        var authorizationTarget = await db.PurchaseRequirementHandoffs.AsNoTracking()
            .Include(x => x.PurchaseRequisition).Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        if (authorizationTarget.Count != ids.Length || authorizationTarget.Select(x => x.PurchaseRequisitionId).Distinct().Count() != 1)
            throw new InvalidOperationException("RFQ must reuse valid handoffs from one PR.");
        var authorizationPr = authorizationTarget[0].PurchaseRequisition ?? throw new InvalidOperationException("PR unavailable.");
        await RequireScopeAsync(actor, authorizationPr.OrganizationId, authorizationPr.RequestingDepartmentId,
            authorizationPr.DeliveryWarehouseId, null, authorizationPr.RequesterEmployeeId, ct);
        await using var tx = await BeginTransactionScopeAsync("CreateRFQ", request.IdempotencyKey, request, ct);
        // Serialize at the contested transaction boundary, before replay lookup or number consumption.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({scope},0))", ct);
        var existing = await db.RequestForQuotations.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == organization && x.IdempotencyKey.StartsWith(scope + "."), ct);
        if (existing is not null)
        {
            await RequireScopeAsync(actor, organization, existing.RequestingDepartmentId, existing.DeliveryWarehouseId, null, existing.OwnerEmployeeId, ct);
            var requested = request.Lines.OrderBy(x => x.PurchaseRequirementHandoffId).Select(x => (x.PurchaseRequirementHandoffId, x.Quantity)).ToArray();
            var persisted = existing.Lines.OrderBy(x => x.PurchaseRequirementHandoffId).Select(x => (x.PurchaseRequirementHandoffId, x.RfqQuantity)).ToArray();
            if (existing.IdempotencyKey != fingerprint || existing.QuoteDueAt != request.QuoteDueAt || existing.CurrencyCode != currency || existing.IsSingleSource != request.IsSingleSource ||
                Trim(existing.SingleSourceJustification) != Trim(request.SingleSourceJustification) || !requested.SequenceEqual(persisted))
                throw new Rev869BConflictException("RFQ idempotency key was reused with a different payload.");
            await tx.RollbackAsync(ct); return Result(existing.Id, existing.RfqNumber, Rev869BStatuses.Draft, 0);
        }
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
        var rfq = new RequestForQuotation { OrganizationId = organization, RfqNumber = next.Number, FinancialYear = next.Year, SequenceNumber = next.Sequence, PurchaseRequisitionId = pr.Id, RequestingDepartmentId = pr.RequestingDepartmentId, DeliveryWarehouseId = pr.DeliveryWarehouseId, OwnerEmployeeId = actor, QuoteDueAt = request.QuoteDueAt, CurrencyCode = currency, IsSingleSource = request.IsSingleSource, SingleSourceJustification = Trim(request.SingleSourceJustification), IdempotencyKey = fingerprint, TransitionCorrelationId = fingerprint, CreatedBy = user.LoginId };
        var lineNo = 0;
        foreach (var h in handoffs.OrderBy(x => x.PurchaseRequisitionLine!.LineNumber))
        {
            var line = h.PurchaseRequisitionLine!; var ordered = await OrderedQuantityAsync(line.Id, ct); var outstanding = h.HandoffQuantity - ordered; if (outstanding <= 0 || quantities[h.Id] > outstanding) throw new InvalidOperationException("Approved outstanding quantity is exhausted.");
            rfq.Lines.Add(new RequestForQuotationLine { PurchaseRequirementHandoffId = h.Id, PurchaseRequisitionLineId = line.Id, ItemId = line.ItemId!.Value, LineNumber = ++lineNo, PrNumberSnapshot = pr.PrNumber, PrLineNumberSnapshot = line.LineNumber, ItemCodeSnapshot = line.ItemCodeSnapshot, ItemNameSnapshot = line.ItemNameSnapshot, UomSnapshot = line.UomSnapshot, SpecificationSnapshot = line.SpecificationSnapshot, ApprovedQuantitySnapshot = h.HandoffQuantity, AlreadyOrderedQuantitySnapshot = ordered, OutstandingQuantitySnapshot = outstanding, RfqQuantity = quantities[h.Id], RequiredDateSnapshot = line.RequiredDate, CreatedBy = user.LoginId });
        }
        db.RequestForQuotations.Add(rfq);
        AddStatus("RFQ", rfq.Id, rfq.RfqNumber, null, rfq.Status, "Create", "Created from existing PendingRFQ handoff", fingerprint);
        await SaveAuthorizedChangesAsync(ct); await audit.WriteAsync("Purchase", "CreateRFQ", nameof(RequestForQuotation), rfq.Id.ToString(), null, new { rfq.RfqNumber, rfq.PurchaseRequisitionId, lineCount = rfq.Lines.Count }, ct); await tx.CommitAsync(ct); return Result(rfq.Id, rfq.RfqNumber, rfq.Status, rfq.Version);
    }

    public async Task<Rev869BDocumentResult> InviteVendorAsync(string rfqNumber, Rev869BInviteVendorRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseExecutive);
        await using var tx = await BeginTransactionScopeAsync("InviteVendor", request.IdempotencyKey,
            new { rfqNumber = rfqNumber.Trim().ToUpperInvariant(), request }, ct);
        var organization = RequireOrganization();
        var fingerprint = Rev869BIdempotencyFingerprint.Create(organization, "InviteVendor", request.IdempotencyKey, new { rfqNumber = rfqNumber.Trim().ToUpperInvariant(), request.VendorId, request.Remarks, request.RfqVersion });
        var rfq = await db.RequestForQuotations.AsNoTracking().Include(x => x.Lines).ThenInclude(x => x.Item)
            .SingleOrDefaultAsync(x => x.OrganizationId == organization && x.RfqNumber == rfqNumber.Trim().ToUpper(), ct)
            ?? throw new Rev869BNotFoundException("RFQ was not found in the current organization.");
        await RequireScopeAsync(actor, organization, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, null, rfq.OwnerEmployeeId, ct);
        var duplicate = await db.RfqVendorInvitations.AsNoTracking().SingleOrDefaultAsync(x => x.RequestForQuotationId == rfq.Id && x.VendorId == request.VendorId, ct);
        if (duplicate is not null)
        {
            var evidence = await db.PurchaseTransactionStatusHistories.AsNoTracking().SingleOrDefaultAsync(x =>
                x.OrganizationId == organization && x.EntityType == "RFQInvitation" && x.EntityId == duplicate.Id &&
                x.CorrelationId == fingerprint, ct);
            if (duplicate.IdempotencyKey != fingerprint || evidence?.Remarks != request.Remarks.Trim())
                throw new Rev869BConflictException("Vendor invitation idempotency key was reused with a different payload.");
            await tx.RollbackAsync(ct); return Result(duplicate.Id, rfq.RfqNumber, Rev869BStatuses.Issued, 0);
        }
        if (rfq.Status is Rev869BStatuses.Cancelled or Rev869BStatuses.Closed) throw new Rev869BConflictException("RFQ is closed.");
        var qualificationDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var categories = rfq.Lines.Select(x => x.Item!.CategoryId).Distinct().Order().ToArray();
        foreach (var category in categories) if (!await vendors.IsEligibleAsync(request.VendorId, rfq.OrganizationId, category, qualificationDate, ct)) throw new InvalidOperationException("Vendor is not active, approved, effective and qualified.");
        var qualifications = await db.VendorQualifications.AsNoTracking()
            .Where(x => x.VendorId == request.VendorId && x.OrganizationId == rfq.OrganizationId && x.IsActive &&
                (x.VerificationStatus == MasterApprovalStatuses.Verified || x.VerificationStatus == MasterApprovalStatuses.Approved) && x.ApprovalStatus == MasterApprovalStatuses.Approved &&
                x.EffectiveFrom <= qualificationDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= qualificationDate) &&
                x.VerifiedByEmployeeId.HasValue && x.ApprovedByEmployeeId.HasValue &&
                x.ItemCategoryId.HasValue && categories.Contains(x.ItemCategoryId.Value))
            .OrderBy(x => x.ItemCategoryId).ToListAsync(ct);
        if (qualifications.Count != categories.Length || qualifications.Select(x => x.ItemCategoryId).Distinct().Count() != categories.Length)
            throw new InvalidOperationException("Each RFQ category requires exactly one authoritative effective vendor qualification.");
        var qualificationSnapshotAt = DateTimeOffset.UtcNow;
        var qualificationSnapshot = JsonSerializer.Serialize(new
        {
            snapshotAt = qualificationSnapshotAt,
            qualifications = qualifications.Select(x => new
            {
                vendorQualificationId = x.Id, x.VendorId, x.OrganizationId, itemCategoryId = x.ItemCategoryId,
                qualificationType = x.QualificationCode, qualificationVersion = x.Version, x.EffectiveFrom, x.EffectiveTo,
                x.VerificationStatus, verifiedByEmployeeId = x.VerifiedByEmployeeId,
                x.ApprovalStatus, approvedByEmployeeId = x.ApprovedByEmployeeId, x.IsActive
            }).ToArray()
        }, JsonOptions);
        var newVersion = checked(request.RfqVersion + 1);
        if (rfq.Status == Rev869BStatuses.Draft)
        {
            Rev869BStatusContracts.RequireRfq(rfq.Status, Rev869BStatuses.Issued);
            AddStatus("RFQ", rfq.Id, rfq.RfqNumber, rfq.Status, Rev869BStatuses.Issued, "Issue", RequiredRemarks(request.Remarks), fingerprint);
            await OpenPendingAuthorizationAsync(ct);
            var affected = await db.RequestForQuotations.Where(x => x.Id == rfq.Id && x.OrganizationId == organization && x.Version == request.RfqVersion)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, newVersion).SetProperty(x => x.Status, Rev869BStatuses.Issued).SetProperty(x => x.TransitionCorrelationId, fingerprint).SetProperty(x => x.IssuedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
            RequireCas(affected, request.RfqVersion, "RFQ");
            await SavePreauthorizedChangesAsync(ct);
        }
        else await ReserveRfqAsync(rfq, request.RfqVersion, "ReserveInvitation", RequiredRemarks(request.Remarks), fingerprint, ct);
        var invitation = new RfqVendorInvitation { RequestForQuotationId = rfq.Id, VendorId = request.VendorId, InvitedAt = qualificationSnapshotAt, QuoteDueAtSnapshot = rfq.QuoteDueAt, VendorQualificationSnapshotJson = qualificationSnapshot, IdempotencyKey = fingerprint, TransitionCorrelationId = fingerprint, CreatedBy = user.LoginId };
        db.RfqVendorInvitations.Add(invitation);
        AddStatus("RFQInvitation", invitation.Id, rfq.RfqNumber, null, invitation.Status, "InviteVendor", RequiredRemarks(request.Remarks), fingerprint);
        await SaveAuthorizedChangesAsync(ct); await audit.WriteAsync("Purchase", "InviteVendor", nameof(RfqVendorInvitation), invitation.Id.ToString(), null, new { rfq.RfqNumber, request.VendorId }, ct); await tx.CommitAsync(ct); return Result(invitation.Id, rfq.RfqNumber, invitation.Status, invitation.Version);
    }

    public async Task<Rev869BDocumentResult> SubmitQuotationRevisionAsync(Guid invitationId, Rev869BSubmitQuotationRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole(Rev869ARoleCodes.PurchaseExecutive);
        await using var tx = await BeginTransactionScopeAsync("SubmitQuotation", request.IdempotencyKey,
            new { invitationId, request }, ct);
        var organization = RequireOrganization();
        var quoteScope = Rev869BIdempotencyFingerprint.CommandScope(organization, "SubmitQuotation", request.IdempotencyKey);
        var quoteFingerprint = Rev869BIdempotencyFingerprint.Create(organization, "SubmitQuotation", request.IdempotencyKey, new { invitationId, request });
        var invitation = await db.RfqVendorInvitations.AsNoTracking().Include(x => x.RequestForQuotation)!.ThenInclude(x => x!.Lines).ThenInclude(x => x.Item)
            .SingleOrDefaultAsync(x => x.Id == invitationId && x.RequestForQuotation!.OrganizationId == organization, ct)
            ?? throw new Rev869BNotFoundException("RFQ invitation was not found in the current organization.");
        var rfq = invitation.RequestForQuotation!;
        await RequireScopeAsync(actor, organization, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, null, rfq.OwnerEmployeeId, ct);
        var replay = await db.VendorQuotations.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == organization && x.IdempotencyKey.StartsWith(quoteScope + "."), ct);
        if (replay is not null)
        {
            if (replay.IdempotencyKey != quoteFingerprint || !QuotationPayloadMatches(replay, invitationId, request)) throw new Rev869BConflictException("Quotation idempotency key was reused with a different payload.");
            await tx.RollbackAsync(ct); return Result(replay.Id, replay.QuotationNumber, Rev869BStatuses.Submitted, replay.Version);
        }
        if (NormalizeCurrency(request.CurrencyCode) != rfq.CurrencyCode) throw new InvalidOperationException("Currency conversion is not configured; quote must match RFQ currency.");
        if (request.Lines.Count != rfq.Lines.Count || request.Lines.Select(x => x.RequestForQuotationLineId).Distinct().Count() != rfq.Lines.Count) throw new InvalidOperationException("Quotation must contain every RFQ line exactly once.");
        foreach (var category in rfq.Lines.Select(x => x.Item!.CategoryId).Distinct()) if (!await vendors.IsEligibleAsync(invitation.VendorId, rfq.OrganizationId, category, DateOnly.FromDateTime(invitation.InvitedAt.UtcDateTime), ct)) throw new InvalidOperationException("Vendor qualification was not valid at the controlled invitation event.");
        var source = Required(request.SubmissionSource, "Submission source").ToUpperInvariant();
        if (source is not ("EMAIL_RECEIVED" or "PHYSICAL_RECEIVED")) throw new Rev869BValidationException("Internal entry must identify an approved received-on-behalf-of-vendor source.");
        if (request.ReceivedAt > DateTimeOffset.UtcNow || request.ReceivedAt == default || Required(request.AttachmentSha256, "Attachment SHA-256").Length != 64) throw new Rev869BValidationException("Received timestamp and 64-character attachment SHA-256 are required.");
        var now = DateTimeOffset.UtcNow; var late = request.ReceivedAt > invitation.QuoteDueAtSnapshot;
        if (late) throw new Rev869BValidationException("Late quotation/revision authorization is not configured.");
        var previous = await db.VendorQuotations.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.OrganizationId == organization && x.RfqVendorInvitationId == invitationId && x.IsCurrentRevision, ct);
        if (previous is null && request.PreviousQuotationVersion.HasValue || previous is not null && request.PreviousQuotationVersion != previous.Version) throw new DbUpdateConcurrencyException("Previous quotation version is stale or inconsistent.");
        if (invitation.Status == Rev869BStatuses.Issued)
        {
            var invitationNext = checked(request.InvitationVersion + 1);
            AddStatus("RFQInvitation", invitation.Id, rfq.RfqNumber, invitation.Status, Rev869BStatuses.Submitted, "Submit", "Quotation submitted for invitation", quoteFingerprint);
            await OpenPendingAuthorizationAsync(ct);
            var invitationAffected = await db.RfqVendorInvitations.Where(x => x.Id == invitation.Id && x.RequestForQuotation!.OrganizationId == organization && x.Version == request.InvitationVersion)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, invitationNext).SetProperty(x => x.Status, Rev869BStatuses.Submitted).SetProperty(x => x.TransitionCorrelationId, quoteFingerprint).SetProperty(x => x.UpdatedAt, now).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
            RequireCas(invitationAffected, request.InvitationVersion, "RFQ invitation");
            await SavePreauthorizedChangesAsync(ct);
        }
        else await ReserveInvitationAsync(invitation, organization, rfq.RfqNumber, request.InvitationVersion, "Reserved for quotation revision", quoteFingerprint, ct);
        var next = await NextNumberAsync(organization, "VQ", DateOnly.FromDateTime(now.UtcDateTime), ct);
        var headerDiscount = Rev869BCommercialCalculator.Ensure(request.HeaderDiscountValue, "quotation header discount");
        if (headerDiscount < 0) throw new Rev869BValidationException("Quotation header discount cannot be negative.");
        var quote = new VendorQuotation { OrganizationId = organization, QuotationNumber = next.Number, FinancialYear = next.Year, SequenceNumber = next.Sequence, RfqVendorInvitationId = invitation.Id, VendorId = invitation.VendorId, RootQuotationId = previous?.RootQuotationId ?? Guid.NewGuid(), PreviousRevisionId = previous?.Id, RevisionNumber = (previous?.RevisionNumber ?? 0) + 1, VendorQuoteReference = Required(request.VendorQuoteReference, "Vendor quote reference"), SubmissionSource = source, ReceivedAt = request.ReceivedAt, AttachmentObjectKey = Required(request.AttachmentObjectKey, "Attachment object key"), AttachmentSha256 = request.AttachmentSha256.Trim().ToUpperInvariant(), VendorAttestation = Required(request.VendorAttestation, "Vendor attestation"), CurrencyCode = rfq.CurrencyCode, Status = Rev869BStatuses.Submitted, SubmittedAt = now, IsLateSubmission = late, LateAuthorizedByEmployeeId = late ? actor : null, LateAuthorizationRemarks = late ? request.LateAuthorizationRemarks!.Trim() : null, PaymentTermsSnapshot = Required(request.PaymentTerms, "Payment terms"), DeliveryTermsSnapshot = Required(request.DeliveryTerms, "Delivery terms"), WarrantyTermsSnapshot = Required(request.WarrantyTerms, "Warranty terms"), HeaderDiscountValue = headerDiscount, IdempotencyKey = quoteFingerprint, TransitionCorrelationId = quoteFingerprint, CreatedBy = user.LoginId };
        quote.Status = Rev869BStatuses.Draft;
        if (previous is not null)
        {
            Rev869BStatusContracts.RequireQuotation(previous.Status, Rev869BStatuses.Superseded);
            var previousExpected = request.PreviousQuotationVersion!.Value; var previousNext = checked(previousExpected + 1);
            AddStatus("VendorQuotation", previous.Id, previous.QuotationNumber, previous.Status, Rev869BStatuses.Superseded, "Supersede", "Superseded by controlled quotation revision", quoteFingerprint + ":previous");
            await OpenPendingAuthorizationAsync(ct);
            var previousAffected = await db.VendorQuotations.Where(x => x.Id == previous.Id && x.OrganizationId == organization && x.Version == previousExpected)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, previousNext).SetProperty(x => x.IsCurrentRevision, false).SetProperty(x => x.Status, Rev869BStatuses.Superseded).SetProperty(x => x.TransitionCorrelationId, quoteFingerprint + ":previous").SetProperty(x => x.UpdatedAt, now).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
            RequireCas(previousAffected, previousExpected, "quotation revision");
            await SavePreauthorizedChangesAsync(ct);
        }
        var lineNo = 0; var remainingHeaderDiscount = headerDiscount;
        foreach (var input in request.Lines.OrderBy(x => rfq.Lines.Single(y => y.Id == x.RequestForQuotationLineId).LineNumber))
        {
            var rfqLine = rfq.Lines.SingleOrDefault(x => x.Id == input.RequestForQuotationLineId) ?? throw new InvalidOperationException("Quotation line is outside RFQ."); if (input.Quantity <= 0 || input.Quantity > rfqLine.RfqQuantity) throw new InvalidOperationException("Quotation quantity exceeds RFQ.");
            var undiscountedHeaderInput = new Rev869BCommercialInput(input.Quantity, input.UnitRate, input.DiscountValue, input.PackingForwarding, input.Freight, input.Insurance, input.OtherCharges, 0m, 0m, 0m, 0m, input.RoundOff, 6) { CurrencyCode = rfq.CurrencyCode, ExchangeRate = 1m };
            var availableForHeaderDiscount = Rev869BCommercialCalculator.TaxableValue(undiscountedHeaderInput);
            var allocatedHeaderDiscount = Math.Min(remainingHeaderDiscount, availableForHeaderDiscount);
            var commercialInput = undiscountedHeaderInput with { HeaderDiscountValue = allocatedHeaderDiscount };
            var taxableBase = Rev869BCommercialCalculator.TaxableValue(commercialInput);
            var tax = await taxes.ResolveAsync(new TaxResolutionRequest(rfq.OrganizationId, TaxJurisdictions.IndiaGst, Required(input.HsnSacCode, "HSN/SAC"), input.SupplierStateCode, input.PlaceOfSupplyStateCode, input.VendorRegistrationType, DateOnly.FromDateTime(request.ReceivedAt.UtcDateTime), taxableBase), ct);
            var calc = Rev869BCommercialCalculator.Calculate(commercialInput with { CgstRate = tax.CgstRate, SgstRate = tax.SgstRate, IgstRate = tax.IgstRate, CessRate = tax.CessRate, RoundingScale = tax.RoundingScale });
            quote.Lines.Add(new VendorQuotationLine { RequestForQuotationLineId = rfqLine.Id, LineNumber = ++lineNo, Quantity = input.Quantity, UnitRate = input.UnitRate, DiscountValue = calc.DiscountValue, HeaderDiscountValue = calc.HeaderDiscountValue, PackingForwarding = calc.PackingForwarding, Freight = calc.Freight, Insurance = calc.Insurance, OtherCharges = calc.OtherCharges, TaxableValue = calc.TaxableValue, TaxGstSettingId = tax.Id, TaxRuleSnapshotJson = JsonSerializer.Serialize(TaxSnapshot(tax), JsonOptions), HsnSacCode = Required(input.HsnSacCode, "HSN/SAC"), SupplierStateCode = Required(input.SupplierStateCode, "Supplier state"), PlaceOfSupplyStateCode = Required(input.PlaceOfSupplyStateCode, "Place of supply"), VendorRegistrationType = Required(input.VendorRegistrationType, "Vendor registration type"), CgstValue = calc.CgstValue, SgstValue = calc.SgstValue, IgstValue = calc.IgstValue, CessValue = calc.CessValue, RoundOff = calc.RoundOff, TotalPayableValue = calc.TotalPayableValue, PromisedDeliveryDate = input.PromisedDeliveryDate, CreatedBy = user.LoginId });
            remainingHeaderDiscount -= allocatedHeaderDiscount;
        }
        if (remainingHeaderDiscount != 0m) throw new Rev869BValidationException("Header discount exceeds the quotation assessable value.");
        quote.TotalPayableValue = Rev869BCommercialCalculator.Add(quote.Lines.Select(x => x.TotalPayableValue).ToArray());
        db.VendorQuotations.Add(quote);
        AddStatus("VendorQuotation", quote.Id, quote.QuotationNumber, null, Rev869BStatuses.Draft, "Create", "Created from controlled invitation", quoteFingerprint + ":draft");
        await SaveAuthorizedChangesAsync(ct);
        var submittedVersion = await ReserveQuotationStatusAsync(quote.Id, organization, quote.QuotationNumber, quote.Version,
            Rev869BStatuses.Draft, Rev869BStatuses.Submitted, previous is null ? "Submit" : "Revise",
            late ? RequiredRemarks(request.LateAuthorizationRemarks) : "Quotation submitted", quoteFingerprint, now, ct);
        db.Entry(quote).State = EntityState.Detached;
        quote.Version = submittedVersion;
        quote.Status = Rev869BStatuses.Submitted;
        await audit.WriteAsync("Purchase", previous is null ? "SubmitQuotation" : "ReviseQuotation", nameof(VendorQuotation), quote.Id.ToString(), previous is null ? null : new { previous.Id, previous.RevisionNumber }, new { quote.QuotationNumber, quote.RevisionNumber, quote.TotalPayableValue, quote.SubmissionSource, quote.ReceivedAt, quote.AttachmentSha256 }, ct); await tx.CommitAsync(ct); return Result(quote.Id, quote.QuotationNumber, quote.Status, quote.Version);
    }

    private static bool QuotationPayloadMatches(VendorQuotation stored, Guid invitationId, Rev869BSubmitQuotationRequest request)
    {
        if (stored.RfqVendorInvitationId != invitationId ||
            stored.VendorQuoteReference != request.VendorQuoteReference.Trim() ||
            stored.CurrencyCode != request.CurrencyCode.Trim().ToUpperInvariant() ||
            stored.PaymentTermsSnapshot != request.PaymentTerms.Trim() ||
            stored.DeliveryTermsSnapshot != request.DeliveryTerms.Trim() ||
            stored.WarrantyTermsSnapshot != request.WarrantyTerms.Trim() ||
            stored.SubmissionSource != request.SubmissionSource.Trim().ToUpperInvariant() ||
            stored.ReceivedAt != request.ReceivedAt ||
            stored.AttachmentObjectKey != request.AttachmentObjectKey.Trim() ||
            stored.AttachmentSha256 != request.AttachmentSha256.Trim().ToUpperInvariant() ||
            stored.VendorAttestation != request.VendorAttestation.Trim() ||
            stored.LateAuthorizationRemarks != Trim(request.LateAuthorizationRemarks) ||
            stored.Lines.Count != request.Lines.Count) return false;
        var requestedLines = request.Lines.OrderBy(x => x.RequestForQuotationLineId).ToArray();
        var storedLines = stored.Lines.OrderBy(x => x.RequestForQuotationLineId).ToArray();
        return requestedLines.Zip(storedLines).All(pair =>
            pair.First.RequestForQuotationLineId == pair.Second.RequestForQuotationLineId &&
            pair.First.Quantity == pair.Second.Quantity && pair.First.UnitRate == pair.Second.UnitRate &&
            pair.First.DiscountValue == pair.Second.DiscountValue && pair.First.PackingForwarding == pair.Second.PackingForwarding &&
            pair.First.Freight == pair.Second.Freight && pair.First.Insurance == pair.Second.Insurance &&
            pair.First.OtherCharges == pair.Second.OtherCharges && pair.First.PromisedDeliveryDate == pair.Second.PromisedDeliveryDate &&
            pair.First.HsnSacCode.Trim() == pair.Second.HsnSacCode && pair.First.SupplierStateCode.Trim() == pair.Second.SupplierStateCode &&
            pair.First.PlaceOfSupplyStateCode.Trim() == pair.Second.PlaceOfSupplyStateCode &&
            pair.First.VendorRegistrationType.Trim() == pair.Second.VendorRegistrationType && pair.First.RoundOff == pair.Second.RoundOff);
    }

    public async Task<Rev869BDocumentResult> VerifyTechnicalAsync(string quotationNumber, Rev869BTechnicalVerificationRequest request, CancellationToken ct)
    {
        var actor = RequireActor(); RequireRole("TECHNICAL_ENGINEER", Rev869ARoleCodes.TechnicalDirector);
        await using var tx = await BeginTransactionScopeAsync("TechnicalVerification", request.IdempotencyKey,
            new { quotationNumber = quotationNumber.Trim().ToUpperInvariant(), request }, ct);
        var organization = RequireOrganization();
        var line = await db.VendorQuotationLines.AsNoTracking().Include(x => x.VendorQuotation)!.ThenInclude(x => x!.Lines).Include(x => x.VendorQuotation)!.ThenInclude(x => x!.RfqVendorInvitation)!.ThenInclude(x => x!.RequestForQuotation)
            .SingleOrDefaultAsync(x => x.Id == request.VendorQuotationLineId && x.VendorQuotation!.OrganizationId == organization && x.VendorQuotation.QuotationNumber == quotationNumber.Trim().ToUpper(), ct)
            ?? throw new Rev869BNotFoundException("Quotation line was not found in the current organization/quotation.");
        var quote = line.VendorQuotation!; var rfq = quote.RfqVendorInvitation!.RequestForQuotation!;
        await RequireScopeAsync(actor, organization, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, null, rfq.OwnerEmployeeId, ct);
        var commandScope = Rev869BIdempotencyFingerprint.CommandScope(organization, "TechnicalVerification", request.IdempotencyKey); var commandFingerprint = Rev869BIdempotencyFingerprint.Create(organization, "TechnicalVerification", request.IdempotencyKey, new { quotationNumber = quotationNumber.Trim().ToUpperInvariant(), request });
        var prior = await db.QuotationTechnicalVerifications.AsNoTracking().SingleOrDefaultAsync(x => x.VendorQuotationLineId == line.Id, ct);
        var compliance = request.IsCompliant ? Rev869BStatuses.TechnicallyCompliant : Rev869BStatuses.TechnicallyRejected;
        if (prior is not null)
        {
            if (!prior.CorrelationId.StartsWith(commandScope + ".", StringComparison.Ordinal) || prior.CorrelationId != commandFingerprint || prior.ComplianceStatus != compliance || prior.Remarks != request.Remarks.Trim()) throw new Rev869BConflictException("Quotation line already has a different immutable technical verification.");
            await tx.RollbackAsync(ct); return Result(prior.Id, quote.QuotationNumber, prior.ComplianceStatus, 0);
        }
        if (!quote.IsCurrentRevision || quote.Status != Rev869BStatuses.Submitted) throw new Rev869BConflictException("Technical verification requires an unverified line on the current submitted quotation.");
        var quoteVersion = checked(request.QuotationVersion + 1);
        var verification = new QuotationTechnicalVerification { VendorQuotationLineId = line.Id, VerifierEmployeeId = actor, ComplianceStatus = compliance, ComplianceSnapshotJson = Required(request.ComplianceEvidenceJson, "Technical evidence"), Remarks = RequiredRemarks(request.Remarks), VerifiedAt = DateTimeOffset.UtcNow, CorrelationId = commandFingerprint, CreatedBy = user.LoginId };
        db.QuotationTechnicalVerifications.Add(verification);
        AddStatus("TechnicalVerification", verification.Id, quote.QuotationNumber, null, verification.ComplianceStatus, "Verify", verification.Remarks, commandFingerprint);
        await SaveAuthorizedChangesAsync(ct);
        var lineIds = quote.Lines.Select(x => x.Id).ToArray();
        var all = await db.QuotationTechnicalVerifications.CountAsync(x => lineIds.Contains(x.VendorQuotationLineId), ct);
        if (all == quote.Lines.Count)
        {
            var finalStatus = await db.QuotationTechnicalVerifications.Where(x => lineIds.Contains(x.VendorQuotationLineId)).AllAsync(x => x.ComplianceStatus == Rev869BStatuses.TechnicallyCompliant, ct) ? Rev869BStatuses.TechnicallyCompliant : Rev869BStatuses.TechnicallyRejected;
            Rev869BStatusContracts.RequireQuotation(quote.Status, finalStatus);
            AddStatus("VendorQuotation", quote.Id, quote.QuotationNumber, quote.Status, finalStatus,
                finalStatus == Rev869BStatuses.TechnicallyCompliant ? "Verify" : "RejectTechnical",
                verification.Remarks, commandFingerprint);
            await OpenPendingAuthorizationAsync(ct);
            var affected = await db.VendorQuotations.Where(x => x.Id == quote.Id && x.OrganizationId == organization && x.Version == request.QuotationVersion)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, quoteVersion).SetProperty(x => x.Status, finalStatus).SetProperty(x => x.TransitionCorrelationId, commandFingerprint).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
            RequireCas(affected, request.QuotationVersion, "quotation");
            await SavePreauthorizedChangesAsync(ct);
        }
        else await ReserveQuotationAsync(quote, request.QuotationVersion, verification.Remarks, commandFingerprint, ct);
        await audit.WriteAsync("Purchase", "TechnicalVerification", nameof(QuotationTechnicalVerification), verification.Id.ToString(), null, new { quote.QuotationNumber, verification.ComplianceStatus }, ct); await tx.CommitAsync(ct); return Result(verification.Id, quote.QuotationNumber, verification.ComplianceStatus, verification.Version);
    }
}
