using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Purchase;

public sealed partial class EfRev869BPurchaseService : IRev869BPurchaseService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NexaErpDbContext db;
    private readonly ICurrentUser user;
    private readonly IRecordScopeAuthorizer scopes;
    private readonly IVendorQualificationService vendors;
    private readonly ITaxGstResolver taxes;
    private readonly IAuditWriter audit;
    private readonly IPurchaseApprovalWorkflowService approvalWorkflow;
    private readonly IPurchaseOperationalRoleResolver operationalRoles;
    private readonly List<Rev869BCommandContextAuthorizer.CommandAttemptHandle> pendingCommandAttempts = [];
    private Rev869BCommandContextAuthorizer.CommandEnvelope? currentCommandEnvelope;
    private string? currentActorRoleCode;
    private Guid currentCompanyId;

    public EfRev869BPurchaseService(NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IVendorQualificationService vendors, ITaxGstResolver taxes, IAuditWriter audit)
        : this(db, user, scopes, vendors, taxes, audit, new EfPurchaseApprovalWorkflowService(db), new PurchaseOperationalRoleResolver()) { }

    public EfRev869BPurchaseService(NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IVendorQualificationService vendors, ITaxGstResolver taxes, IAuditWriter audit,
        IPurchaseApprovalWorkflowService approvalWorkflow, IPurchaseOperationalRoleResolver operationalRoles)
    {
        this.db = db; this.user = user; this.scopes = scopes; this.vendors = vendors; this.taxes = taxes; this.audit = audit;
        this.approvalWorkflow = approvalWorkflow; this.operationalRoles = operationalRoles;
    }

    private async Task<Rev869BTransactionScope> BeginTransactionScopeAsync(
        string operation, string idempotencyKey, object request, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("REV869B commands require a service-owned transaction for exact terminal security-audit correlation.");
        currentCommandEnvelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
            RequireOrganization(), operation, idempotencyKey, request);
        currentActorRoleCode = IsApprovalOperation(operation) ? null : operationalRoles.Resolve(operation, user.RoleCodes);
        var scope = new Rev869BTransactionScope(this,
            await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct));
        currentCompanyId = await db.Companies.AsNoTracking()
            .Where(x => x.Code == RequireOrganization() && x.IsActive)
            .Select(x => x.Id).SingleAsync(ct);
        _ = RequireActor();
        _ = RequireOrganization();
        return scope;
    }

    private sealed class Rev869BTransactionScope(
        EfRev869BPurchaseService service,
        IDbContextTransaction owned) : IAsyncDisposable
    {
        private bool finalized;

        public async Task CommitAsync(CancellationToken ct)
        {
            foreach (var attempt in service.pendingCommandAttempts)
                await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(service.db, attempt, ct);
            await owned.CommitAsync(ct);
            service.pendingCommandAttempts.Clear();
            service.currentCommandEnvelope = null;
            service.currentActorRoleCode = null;
            service.currentCompanyId = Guid.Empty;
            finalized = true;
        }

        public async Task RollbackAsync(CancellationToken ct)
        {
            await owned.RollbackAsync(ct);
            await service.RecordRolledBackOutcomesAsync("Rejected", "IdempotentReplayOrExplicitRollback", ct);
            service.currentCommandEnvelope = null;
            service.currentActorRoleCode = null;
            service.currentCompanyId = Guid.Empty;
            finalized = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!finalized)
            {
                try { await owned.RollbackAsync(); }
                finally { await service.RecordRolledBackOutcomesAsync("RolledBack", "BusinessTransactionRolledBack", CancellationToken.None); }
                service.currentCommandEnvelope = null;
                service.currentActorRoleCode = null;
                service.currentCompanyId = Guid.Empty;
                finalized = true;
            }
            await owned.DisposeAsync();
        }
    }

    private async Task<int> SaveAuthorizedChangesAsync(CancellationToken ct)
    {
        await OpenPendingAuthorizationAsync(ct);
        var histories = db.ChangeTracker.Entries()
            .Where(x => x.State == EntityState.Added &&
                (x.Entity is PurchaseTransactionStatusHistory ||
                 x.Entity is PurchaseTransactionApprovalHistory ||
                 x.Entity is PurchaseOrderHistory))
            .Select(x => x.Entity).ToArray();
        if (histories.Length == 0) return await db.SaveChangesAsync(ct);

        foreach (var history in histories) db.Entry(history).State = EntityState.Detached;
        var affected = await db.SaveChangesAsync(ct);
        db.AddRange(histories);
        return affected + await db.SaveChangesAsync(ct);
    }

    private async Task OpenPendingAuthorizationAsync(CancellationToken ct)
    {
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(
            db, user, RequireOrganization(), currentCommandEnvelope ??
                throw new InvalidOperationException("Caller command envelope must be established before mutation."), ct,
            CurrentActorRole());
        if (attempt.HasValue)
        {
            var prior = pendingCommandAttempts.SingleOrDefault(x => x.AttemptId == attempt.Value.AttemptId);
            pendingCommandAttempts.RemoveAll(x => x.AttemptId == attempt.Value.AttemptId);
            pendingCommandAttempts.Add(prior.AttemptId == Guid.Empty
                ? attempt.Value
                : attempt.Value with
                {
                    BusinessFingerprint = System.Security.Cryptography.SHA256.HashData(
                        prior.BusinessFingerprint.Concat(attempt.Value.BusinessFingerprint).ToArray())
                });
        }
    }

    private async Task RecordRolledBackOutcomesAsync(string terminalEvent, string failureCategory, CancellationToken ct)
    {
        var runtime = (Npgsql.NpgsqlConnection)db.Database.GetDbConnection();
        foreach (var attempt in pendingCommandAttempts)
            await Rev869BCommandContextAuthorizer.RecordNoncommitOutcomeAsync(
                runtime, attempt, terminalEvent, failureCategory, ct);
        pendingCommandAttempts.Clear();
    }

    private Task<int> SavePreauthorizedChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    private async Task<CommercialComparison> LoadComparisonAsync(string number, CancellationToken ct) =>
        await db.CommercialComparisons.AsNoTracking().Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.OrganizationId == RequireOrganization() && x.ComparisonNumber == number.Trim().ToUpper(), ct)
        ?? throw new Rev869BNotFoundException("Commercial comparison was not found in the current organization.");
    private async Task<PurchaseOrder> LoadPoAsync(string number, CancellationToken ct) =>
        await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.OrganizationId == RequireOrganization() && x.PoNumber == number.Trim().ToUpper() && x.IsCurrentVersion, ct)
        ?? throw new Rev869BNotFoundException("Current purchase order was not found in the current organization.");
    private async Task<PurchaseOrder> LoadPendingPoAsync(string number, CancellationToken ct) =>
        await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.OrganizationId == RequireOrganization() && x.PoNumber == number.Trim().ToUpper() && !x.IsCurrentVersion && (x.Status == Rev869BStatuses.Draft || x.Status == Rev869BStatuses.PendingApproval), ct)
        ?? throw new Rev869BNotFoundException("Pending purchase-order version was not found in the current organization.");
    private async Task AuthorizeComparisonAsync(Guid actor, CommercialComparison comparison, CancellationToken ct)
    {
        if (comparison.OrganizationId != RequireOrganization()) throw new UnauthorizedAccessException("Cross-organization comparison access is prohibited.");
        var rfq = await db.RequestForQuotations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == comparison.RequestForQuotationId && x.OrganizationId == comparison.OrganizationId, ct)
            ?? throw new Rev869BConflictException("Comparison RFQ organization/parent contract is invalid.");
        await RequireScopeAsync(actor, comparison.OrganizationId, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, null, comparison.OwnerEmployeeId, ct);
    }
    private Task AuthorizePoAsync(Guid actor, PurchaseOrder po, CancellationToken ct) => po.OrganizationId == RequireOrganization()
        ? RequireScopeAsync(actor, po.OrganizationId, po.RequestingDepartmentId, po.DeliveryWarehouseId, null, po.OwnerEmployeeId, ct)
        : throw new UnauthorizedAccessException("Cross-organization purchase-order access is prohibited.");
    private async Task RequireScopeAsync(Guid actor, string organization, Guid? department, Guid? warehouse, Guid? rackBin, Guid? owner, CancellationToken ct)
    {
        var actingRole = CurrentActorRole();
        var decision = await scopes.AuthorizeAsync(actor, actingRole, new RecordScopeTarget(organization, department, warehouse, rackBin, owner), DateOnly.FromDateTime(DateTime.UtcNow), ct);
        if (!decision.Allowed) { await WriteAuditAsync("Security", "Denied", "REV869BRecordScope", organization, null, new { decision.Reason, department, warehouse }, ct); throw new UnauthorizedAccessException(decision.Reason); }
    }

    private static bool IsApprovalOperation(string operation) =>
        operation.StartsWith("Approve", StringComparison.OrdinalIgnoreCase) ||
        operation.StartsWith("Reject", StringComparison.OrdinalIgnoreCase) ||
        operation.StartsWith("RequestRevision", StringComparison.OrdinalIgnoreCase);
    private string CurrentActorRole() => currentActorRoleCode ?? throw new InvalidOperationException("The command's deterministic actor role has not been resolved.");
    private void SetApprovalActorRole(string roleCode)
    {
        if (!IsApprovalOperation(currentCommandEnvelope?.Operation ?? string.Empty))
            throw new InvalidOperationException("Only approval commands may take their role from a workflow step.");
        currentActorRoleCode = roleCode;
    }
    private Task WriteAuditAsync(string module, string action, string entityType, string entityId, object? before, object? after, CancellationToken ct) =>
        audit.WriteAsync(module, action, entityType, entityId,
            before is null ? null : new { Value = before, ActingRole = CurrentActorRole() },
            new { Value = after, ActingRole = CurrentActorRole() }, ct);

    private async Task<uint> ReserveRfqAsync(RequestForQuotation rfq, uint expected, string action, string remarks, string correlation, CancellationToken ct)
    {
        var next = checked(expected + 1);
        AddStatus("RFQ", rfq.Id, rfq.RfqNumber, rfq.Status, rfq.Status, action, remarks, correlation);
        await OpenPendingAuthorizationAsync(ct);
        var affected = await db.RequestForQuotations.Where(x => x.Id == rfq.Id && x.OrganizationId == rfq.OrganizationId && x.Version == expected)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, next).SetProperty(x => x.TransitionCorrelationId, correlation).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, expected, "RFQ");
        await SavePreauthorizedChangesAsync(ct);
        return next;
    }

    private async Task<uint> ReserveInvitationAsync(RfqVendorInvitation invitation, string organization, string documentNumber, uint expected, string remarks, string correlation, CancellationToken ct)
    {
        var next = checked(expected + 1);
        AddStatus("RFQInvitation", invitation.Id, documentNumber, invitation.Status, invitation.Status, "ReserveQuotation", remarks, correlation);
        await OpenPendingAuthorizationAsync(ct);
        var affected = await db.RfqVendorInvitations.Where(x => x.Id == invitation.Id && x.RequestForQuotation!.OrganizationId == organization && x.Version == expected)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, next).SetProperty(x => x.TransitionCorrelationId, correlation).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, expected, "RFQ invitation");
        await SavePreauthorizedChangesAsync(ct);
        return next;
    }

    private async Task<uint> ReserveQuotationAsync(VendorQuotation quotation, uint expected, string remarks, string correlation, CancellationToken ct)
    {
        var next = checked(expected + 1);
        AddStatus("VendorQuotation", quotation.Id, quotation.QuotationNumber, quotation.Status, quotation.Status, "ReserveTechnicalVerification", remarks, correlation);
        await OpenPendingAuthorizationAsync(ct);
        var affected = await db.VendorQuotations.Where(x => x.Id == quotation.Id && x.OrganizationId == quotation.OrganizationId && x.Version == expected)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, next).SetProperty(x => x.TransitionCorrelationId, correlation).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, expected, "vendor quotation");
        await SavePreauthorizedChangesAsync(ct);
        return next;
    }

    private async Task<uint> ReserveQuotationStatusAsync(Guid id, string organization, string documentNumber, uint expected, string from, string to, string action, string remarks, string correlation, DateTimeOffset at, CancellationToken ct)
    {
        var next = checked(expected + 1);
        AddStatus("VendorQuotation", id, documentNumber, from, to, action, remarks, correlation);
        await OpenPendingAuthorizationAsync(ct);
        var query = db.VendorQuotations.Where(x => x.Id == id && x.OrganizationId == organization);
        query = query.Where(x => x.Version == expected && x.Status == from);
        var rows = await query.ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, next).SetProperty(x => x.Status, to).SetProperty(x => x.TransitionCorrelationId, correlation).SetProperty(x => x.UpdatedAt, at).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(rows, expected, "vendor quotation status");
        await SavePreauthorizedChangesAsync(ct);
        return next;
    }

    private async Task<uint> ReserveComparisonAsync(CommercialComparison comparison, uint expected, string correlation, CancellationToken ct)
    {
        var next = checked(expected + 1);
        AddStatus("CommercialComparison", comparison.Id, comparison.ComparisonNumber, comparison.Status, comparison.Status, "ReservePurchaseOrder", "Reserved for purchase-order creation", correlation);
        await OpenPendingAuthorizationAsync(ct);
        var affected = await db.CommercialComparisons.Where(x => x.Id == comparison.Id && x.OrganizationId == comparison.OrganizationId && x.Version == expected)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, next).SetProperty(x => x.TransitionCorrelationId, correlation).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, expected, "commercial comparison");
        await SavePreauthorizedChangesAsync(ct);
        return next;
    }

    private async Task<uint> ReservePoAsync(PurchaseOrder po, uint expected, string remarks, string correlation, CancellationToken ct)
    {
        var next = checked(expected + 1);
        AddStatus("PurchaseOrder", po.Id, po.PoNumber, po.Status, po.Status, "ReserveAmendment", remarks, correlation);
        AddPoHistory(po, "ReserveAmendment", po.Status, po.Status, remarks, correlation);
        await OpenPendingAuthorizationAsync(ct);
        var affected = await db.PurchaseOrders.Where(x => x.Id == po.Id && x.OrganizationId == po.OrganizationId && x.Version == expected)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, next).SetProperty(x => x.TransitionCorrelationId, correlation).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow).SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, expected, "purchase order");
        await SavePreauthorizedChangesAsync(ct);
        return next;
    }

    private static uint RequireCas(int affected, uint expected, string aggregate)
    {
        if (affected != 1) throw new DbUpdateConcurrencyException($"Stale {aggregate} version; reload before retrying.");
        return checked(expected + 1);
    }
    private static Rev869BTaxRuleSnapshot TaxSnapshot(Domain.Masters.TaxGstSetting tax) => new(
        tax.Id, tax.OrganizationId, tax.JurisdictionCode, tax.HsnSacCode, tax.SupplyType, tax.SupplierStateCode,
        tax.PlaceOfSupplyStateCode, tax.VendorRegistrationType, tax.GstRate, tax.CgstRate, tax.SgstRate,
        tax.IgstRate, tax.CessRate, tax.IsExempt, tax.IsReverseCharge, tax.CurrencyCode, tax.RoundingScale,
        tax.EffectiveFrom, tax.EffectiveTo, tax.ApprovalStatus, tax.IsActive);

    private static (Rev869BCommercialBreakdown Breakdown, Rev869BTaxRuleSnapshot Tax) Recalculate(VendorQuotationLine line, string organization, DateOnly quotationReceivedDate)
    {
        Rev869BTaxRuleSnapshot tax;
        try { tax = JsonSerializer.Deserialize<Rev869BTaxRuleSnapshot>(line.TaxRuleSnapshotJson, JsonOptions) ?? throw new JsonException(); }
        catch (JsonException) { throw new Rev869BConflictException("The immutable quotation GST rule snapshot is malformed."); }
        if (tax.OrganizationId != organization || tax.Id != line.TaxGstSettingId || tax.HsnSacCode != line.HsnSacCode ||
            tax.SupplierStateCode != line.SupplierStateCode || tax.PlaceOfSupplyStateCode != line.PlaceOfSupplyStateCode ||
            tax.VendorRegistrationType != line.VendorRegistrationType)
            throw new Rev869BConflictException("The immutable quotation GST rule provenance is mismatched.");
        var input = new Rev869BCommercialInput(line.Quantity, line.UnitRate, line.DiscountValue, line.PackingForwarding, line.Freight, line.Insurance, line.OtherCharges, tax.CgstRate, tax.SgstRate, tax.IgstRate, tax.CessRate, line.RoundOff, tax.RoundingScale)
        { HeaderDiscountValue = line.HeaderDiscountValue, CurrencyCode = tax.CurrencyCode, ExchangeRate = 1m };
        var stored = new Rev869BCommercialBreakdown(line.TaxableValue, line.DiscountValue, line.CgstValue, line.SgstValue, line.IgstValue, line.CessValue, line.PackingForwarding, line.Freight, line.Insurance, line.OtherCharges, line.RoundOff, line.TotalPayableValue)
        { GrossAmount = decimal.Round(line.Quantity * line.UnitRate, tax.RoundingScale, MidpointRounding.AwayFromZero), AssessableValue = Rev869BCommercialCalculator.Add(decimal.Round(line.Quantity * line.UnitRate, tax.RoundingScale, MidpointRounding.AwayFromZero), line.PackingForwarding, line.Freight, line.Insurance, line.OtherCharges), HeaderDiscountValue = line.HeaderDiscountValue, CurrencyCode = tax.CurrencyCode, ExchangeRate = 1m };
        Rev869BCommercialBreakdown calculation;
        try { calculation = Rev869BCommercialCalculator.Reconcile(input, stored, tax, quotationReceivedDate); }
        catch (InvalidOperationException ex) { throw new Rev869BConflictException(ex.Message); }
        return (calculation, tax);
    }
    private sealed record ComparisonCommercialSnapshot(
        string OrganizationId, Guid CommercialComparisonId, Guid RequestForQuotationId, Guid VendorId,
        Guid VendorQuotationId, int QuotationRevision, Guid VendorQuotationLineId, Guid ItemId,
        decimal Quantity, string Uom, string CurrencyCode, decimal ExchangeRate,
        Rev869BCommercialInput Input, Rev869BCommercialBreakdown Result, Rev869BTaxRuleSnapshot TaxRule);
    private static string ComparisonSnapshotJson(CommercialComparison comparison, VendorQuotation quote, VendorQuotationLine line, (Rev869BCommercialBreakdown Breakdown, Rev869BTaxRuleSnapshot Tax) calculated)
    {
        var rfqLine = line.RequestForQuotationLine ?? throw new Rev869BConflictException("Quotation line RFQ provenance is missing.");
        var input = new Rev869BCommercialInput(line.Quantity, line.UnitRate, line.DiscountValue, line.PackingForwarding, line.Freight, line.Insurance, line.OtherCharges, calculated.Tax.CgstRate, calculated.Tax.SgstRate, calculated.Tax.IgstRate, calculated.Tax.CessRate, line.RoundOff, calculated.Tax.RoundingScale)
        { HeaderDiscountValue = line.HeaderDiscountValue, CurrencyCode = quote.CurrencyCode, ExchangeRate = 1m };
        return JsonSerializer.Serialize(new ComparisonCommercialSnapshot(
            comparison.OrganizationId, comparison.Id, comparison.RequestForQuotationId, quote.VendorId, quote.Id,
            quote.RevisionNumber, line.Id, rfqLine.ItemId, line.Quantity, rfqLine.UomSnapshot, quote.CurrencyCode,
            1m, input, calculated.Breakdown, calculated.Tax), JsonOptions);
    }
    private async Task ReconcileComparisonAsync(CommercialComparison comparison, CancellationToken ct)
    {
        if (!comparison.RecommendedVendorQuotationId.HasValue || !comparison.SelectedVendorId.HasValue)
            throw new Rev869BConflictException("Comparison selection evidence is incomplete.");
        var quote = await db.VendorQuotations.AsNoTracking().Include(x => x.Lines).ThenInclude(x => x.RequestForQuotationLine)
            .SingleOrDefaultAsync(x => x.Id == comparison.RecommendedVendorQuotationId && x.OrganizationId == comparison.OrganizationId &&
                x.VendorId == comparison.SelectedVendorId && x.RfqVendorInvitation!.RequestForQuotationId == comparison.RequestForQuotationId &&
                x.IsCurrentRevision && x.Status == Rev869BStatuses.TechnicallyCompliant, ct)
            ?? throw new Rev869BConflictException("Recommended quotation provenance is stale or incomplete.");
        var recommended = comparison.Lines.Where(x => x.IsRecommended).ToArray();
        if (recommended.Length != quote.Lines.Count) throw new Rev869BConflictException("Comparison recommendation line coverage is incomplete.");
        decimal total = 0m;
        foreach (var line in quote.Lines)
        {
            var comparisonLine = recommended.SingleOrDefault(x => x.VendorQuotationLineId == line.Id && x.VendorId == quote.VendorId)
                ?? throw new Rev869BConflictException("Comparison line vendor/quotation provenance is mismatched.");
            ComparisonCommercialSnapshot snapshot;
            try { snapshot = JsonSerializer.Deserialize<ComparisonCommercialSnapshot>(comparisonLine.CommercialSnapshotJson, JsonOptions) ?? throw new JsonException(); }
            catch (JsonException) { throw new Rev869BConflictException("Comparison commercial snapshot is malformed."); }
            var calculated = Recalculate(line, comparison.OrganizationId, DateOnly.FromDateTime(quote.ReceivedAt.UtcDateTime));
            var expectedJson = ComparisonSnapshotJson(comparison, quote, line, calculated);
            var expected = JsonSerializer.Deserialize<ComparisonCommercialSnapshot>(expectedJson, JsonOptions)!;
            if (snapshot != expected || comparisonLine.TotalPayableValue != calculated.Breakdown.TotalPayableValue)
                throw new Rev869BConflictException("Comparison commercial snapshot does not exactly reconcile.");
            total = Rev869BCommercialCalculator.Add(total, calculated.Breakdown.TotalPayableValue);
        }
        if (total != comparison.TotalPayableValue) throw new Rev869BConflictException("Comparison header and recommended-line totals do not exactly reconcile.");
    }
    private async Task<decimal> OrderedQuantityAsync(Guid prLineId, CancellationToken ct) => await db.PurchaseOrderLines.Where(x => x.PurchaseRequisitionLineId == prLineId && x.PurchaseOrder!.IsCurrentVersion && x.PurchaseOrder.Status != Rev869BStatuses.Cancelled && x.PurchaseOrder.Status != Rev869BStatuses.Superseded).SumAsync(x => (decimal?)x.OrderedQuantity, ct) ?? 0m;
    private async Task<(string Number, string Year, long Sequence)> NextNumberAsync(string organization, string prefix, DateOnly date, CancellationToken ct)
    {
        var year = date.Month >= 4 ? $"{date.Year % 100:00}-{(date.Year + 1) % 100:00}" : $"{(date.Year - 1) % 100:00}-{date.Year % 100:00}";
        var sequence = await db.PurchaseNumberSequences.SingleOrDefaultAsync(x => x.OrganizationId == organization && x.FinancialYear == year && x.Prefix == prefix && x.IsActive, ct);
        if (sequence is null) { sequence = new PurchaseNumberSequence { CompanyId = CurrentCompanyId(), OrganizationId = organization, FinancialYear = year, Prefix = prefix, CreatedBy = user.LoginId }; db.PurchaseNumberSequences.Add(sequence); }
        sequence.LastNumber++; sequence.UpdatedAt = DateTimeOffset.UtcNow; sequence.UpdatedBy = user.LoginId;
        return ($"{prefix}-{year}-{sequence.LastNumber:000001}", year, sequence.LastNumber);
    }
    private void Transition(CommercialComparison comparison, string next, string action, string remarks, string correlation) { var from = comparison.Status; comparison.Status = next; comparison.TransitionCorrelationId = correlation; comparison.UpdatedAt = DateTimeOffset.UtcNow; comparison.UpdatedBy = user.LoginId; AddStatus("CommercialComparison", comparison.Id, comparison.ComparisonNumber, from, next, action, remarks, correlation); }
    private void AddApproval(CommercialComparison comparison, string action, string from, string to, string remarks, string correlation, PurchaseApprovalDecision? decision = null)
    {
        db.PurchaseTransactionApprovalHistories.Add(new PurchaseTransactionApprovalHistory { CompanyId = comparison.CompanyId, CommercialComparisonId = comparison.Id, Action = action, FromStatus = from, ToStatus = to, ApprovalRoute = comparison.ApprovalRoute, ApprovalCycle = decision?.ApprovalCycle ?? comparison.ApprovalCycle, StepNumber = decision?.StepNumber ?? 0, RequiredApprovalStepCount = decision?.RequiredStepCount ?? comparison.RequiredApprovalStepCount, ResolvedEmployeeId = decision?.ResolvedEmployeeId ?? Guid.Empty, ResolvedRoleCode = decision?.ResolvedRoleCode ?? CurrentActorRole(), SnapshotIdentity = decision?.SnapshotIdentity ?? string.Empty, ActorEmployeeId = RequireActor(), ActorLoginId = user.LoginId, ActorRoleCode = CurrentActorRole(), Remarks = RequiredRemarks(remarks), CorrelationId = correlation.Trim(), CreatedBy = user.LoginId });
    }
    private void AddPoHistory(PurchaseOrder po, string action, string from, string to, string reason, string correlation, PurchaseApprovalDecision? decision = null)
    {
        db.PurchaseOrderHistories.Add(new PurchaseOrderHistory { CompanyId = po.CompanyId, PurchaseOrderId = po.Id, Action = action, FromStatus = from, ToStatus = to, RevisionNumber = po.RevisionNumber, ApprovalCycle = decision?.ApprovalCycle ?? po.ApprovalCycle, StepNumber = decision?.StepNumber ?? 0, RequiredApprovalStepCount = decision?.RequiredStepCount ?? po.RequiredApprovalStepCount, ResolvedEmployeeId = decision?.ResolvedEmployeeId, ResolvedRoleCode = decision?.ResolvedRoleCode, ApprovalRoute = po.ApprovalRoute, SnapshotIdentity = decision?.SnapshotIdentity, ActorEmployeeId = RequireActor(), ActorLoginId = user.LoginId, ActorRoleCode = CurrentActorRole(), Reason = RequiredRemarks(reason), CorrelationId = correlation.Trim(), CreatedBy = user.LoginId });
    }
    private void AddStatus(string type, Guid id, string number, string? from, string to, string action, string remarks, string correlation)
    {
        db.PurchaseTransactionStatusHistories.Add(new PurchaseTransactionStatusHistory { CompanyId = CurrentCompanyId(), OrganizationId = RequireOrganization(), EntityType = type, EntityId = id, DocumentNumber = number, Action = action, FromStatus = from, ToStatus = to, ActorEmployeeId = RequireActor(), ActorLoginId = user.LoginId, ActorRoleCode = CurrentActorRole(), Remarks = RequiredRemarks(remarks), CorrelationId = correlation.Trim(), CreatedBy = user.LoginId });
    }
    private Guid CurrentCompanyId() => currentCompanyId == Guid.Empty
        ? throw new InvalidOperationException("A resolved company is required inside the Purchase command transaction.")
        : currentCompanyId;
    private Guid RequireActor() => user.IsAuthenticated && user.EmployeeId.HasValue ? user.EmployeeId.Value : throw new UnauthorizedAccessException("A unique active employee identity mapping is required.");
    private string RequireOrganization() => !string.IsNullOrWhiteSpace(user.OrganizationId) ? user.OrganizationId.Trim() : throw new UnauthorizedAccessException("Organization scope is required.");
    private bool IsRole(string code) => user.RoleCodes.Any(x => Rev869ARoleCodes.Normalize(x) == Rev869ARoleCodes.Normalize(code));
    private void RequireRole(params string[] allowed) { if (!allowed.Any(IsRole)) throw new UnauthorizedAccessException("Role is not authorized for this operation."); }
    private static string NormalizeCurrency(string value) { var code = Required(value, "Currency").ToUpperInvariant(); if (code.Length != 3 || code.Any(x => !char.IsLetter(x))) throw new Rev869BValidationException("ISO 4217 currency code must contain three letters."); return code; }
    private static string Required(string? value, string name) => !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new Rev869BValidationException($"{name} is required.");
    private static string RequiredRemarks(string? value) => Required(value, "Remarks");
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static Rev869BDocumentResult Result(Guid id, string number, string status, uint version) => new(id, number, status, version);
}
