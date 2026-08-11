using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

    public EfRev869BPurchaseService(NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes, IVendorQualificationService vendors, ITaxGstResolver taxes, IAuditWriter audit)
    {
        this.db = db; this.user = user; this.scopes = scopes; this.vendors = vendors; this.taxes = taxes; this.audit = audit;
    }

    private async Task<CommercialComparison> LoadComparisonAsync(string number, CancellationToken ct) => await db.CommercialComparisons.Include(x => x.Lines).SingleAsync(x => x.OrganizationId == RequireOrganization() && x.ComparisonNumber == number.Trim().ToUpper(), ct);
    private async Task<PurchaseOrder> LoadPoAsync(string number, CancellationToken ct) => await db.PurchaseOrders.Include(x => x.Lines).SingleAsync(x => x.OrganizationId == RequireOrganization() && x.PoNumber == number.Trim().ToUpper() && x.IsCurrentVersion, ct);
    private async Task AuthorizeComparisonAsync(Guid actor, CommercialComparison comparison, CancellationToken ct) { var rfq = await db.RequestForQuotations.AsNoTracking().SingleAsync(x => x.Id == comparison.RequestForQuotationId, ct); await RequireScopeAsync(actor, comparison.OrganizationId, rfq.RequestingDepartmentId, rfq.DeliveryWarehouseId, null, comparison.OwnerEmployeeId, ct); }
    private Task AuthorizePoAsync(Guid actor, PurchaseOrder po, CancellationToken ct) => RequireScopeAsync(actor, po.OrganizationId, po.RequestingDepartmentId, po.DeliveryWarehouseId, null, po.OwnerEmployeeId, ct);
    private async Task RequireScopeAsync(Guid actor, string organization, Guid? department, Guid? warehouse, Guid? rackBin, Guid? owner, CancellationToken ct)
    {
        var decision = await scopes.AuthorizeAsync(actor, user.RoleCode, new RecordScopeTarget(organization, department, warehouse, rackBin, owner), DateOnly.FromDateTime(DateTime.UtcNow), ct);
        if (!decision.Allowed) { await audit.WriteAsync("Security", "Denied", "REV869BRecordScope", organization, null, new { decision.Reason, user.RoleCode, department, warehouse }, ct); throw new UnauthorizedAccessException(decision.Reason); }
    }
    private async Task<string> ResolveApprovalRouteAsync(decimal total, string organization, CancellationToken ct) { var policies = await db.PurchaseTransactionApprovalPolicies.AsNoTracking().Where(x => x.OrganizationId == organization).ToListAsync(ct); return Rev869BApprovalRoutes.Resolve(total, policies, DateOnly.FromDateTime(DateTime.UtcNow), organization); }
    private async Task<decimal> OrderedQuantityAsync(Guid prLineId, CancellationToken ct) => await db.PurchaseOrderLines.Where(x => x.PurchaseRequisitionLineId == prLineId && x.PurchaseOrder!.IsCurrentVersion && x.PurchaseOrder.Status != Rev869BStatuses.Cancelled && x.PurchaseOrder.Status != Rev869BStatuses.Superseded).SumAsync(x => (decimal?)x.OrderedQuantity, ct) ?? 0m;
    private async Task<(string Number, string Year, long Sequence)> NextNumberAsync(string organization, string prefix, DateOnly date, CancellationToken ct)
    {
        var year = date.Month >= 4 ? $"{date.Year % 100:00}-{(date.Year + 1) % 100:00}" : $"{(date.Year - 1) % 100:00}-{date.Year % 100:00}";
        var sequence = await db.PurchaseNumberSequences.SingleOrDefaultAsync(x => x.OrganizationId == organization && x.FinancialYear == year && x.Prefix == prefix && x.IsActive, ct);
        if (sequence is null) { sequence = new PurchaseNumberSequence { OrganizationId = organization, FinancialYear = year, Prefix = prefix, CreatedBy = user.LoginId }; db.PurchaseNumberSequences.Add(sequence); }
        sequence.LastNumber++; sequence.UpdatedAt = DateTimeOffset.UtcNow; sequence.UpdatedBy = user.LoginId;
        return ($"{prefix}-{year}-{sequence.LastNumber:000001}", year, sequence.LastNumber);
    }
    private async Task RequireApproverAsync(string route, Guid? departmentId, Guid actor, string creatorLogin, CancellationToken ct)
    {
        if (string.Equals(creatorLogin, user.LoginId, StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("Self-approval is prohibited.");
        var role = Rev869ARoleCodes.Normalize(user.RoleCode);
        if (route == Rev869BApprovalRoutes.Manager)
        {
            if (departmentId is null) throw new UnauthorizedAccessException("Department Manager approval mapping is missing.");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var mappings = await db.DepartmentApprovalMappings.AsNoTracking().Where(x => x.DepartmentId == departmentId && x.ApprovalRouteCode == PurchaseRequisitionApprovalRoutes.Manager && x.IsActive && x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today)).Take(2).ToListAsync(ct);
            if (mappings.Count != 1 || (mappings[0].PrimaryApproverEmployeeId != actor && mappings[0].AlternateApproverEmployeeId != actor)) throw new UnauthorizedAccessException("A single effective department approval mapping did not authorize this employee.");
            if (role != Rev869ARoleCodes.PurchaseManager && role != Rev869ARoleCodes.DepartmentManager) throw new UnauthorizedAccessException("Manager-level approval role is required.");
            return;
        }
        var expected = route == Rev869BApprovalRoutes.TechnicalDirector ? Rev869ARoleCodes.TechnicalDirector : route == Rev869BApprovalRoutes.ManagingDirector ? Rev869ARoleCodes.ManagingDirector : throw new UnauthorizedAccessException("Approval route is missing or unsupported.");
        if (role != expected) throw new UnauthorizedAccessException("Configured approval role does not match current employee role.");
    }
    private void Transition(CommercialComparison comparison, string next, string action, string remarks, string correlation) { var from = comparison.Status; comparison.Status = next; comparison.UpdatedAt = DateTimeOffset.UtcNow; comparison.UpdatedBy = user.LoginId; AddStatus("CommercialComparison", comparison.Id, comparison.ComparisonNumber, from, next, action, remarks, correlation); }
    private void AddApproval(CommercialComparison comparison, string action, string from, string to, string remarks, string correlation) => db.PurchaseTransactionApprovalHistories.Add(new PurchaseTransactionApprovalHistory { CommercialComparisonId = comparison.Id, Action = action, FromStatus = from, ToStatus = to, ApprovalRoute = comparison.ApprovalRoute, ActorEmployeeId = RequireActor(), ActorLoginId = user.LoginId, ActorRoleCode = user.RoleCode, Remarks = RequiredRemarks(remarks), CorrelationId = correlation.Trim(), CreatedBy = user.LoginId });
    private void AddPoHistory(PurchaseOrder po, string action, string from, string to, string reason, string correlation) => db.PurchaseOrderHistories.Add(new PurchaseOrderHistory { PurchaseOrderId = po.Id, Action = action, FromStatus = from, ToStatus = to, RevisionNumber = po.RevisionNumber, ActorEmployeeId = RequireActor(), ActorLoginId = user.LoginId, ActorRoleCode = user.RoleCode, Reason = RequiredRemarks(reason), CorrelationId = correlation.Trim(), CreatedBy = user.LoginId });
    private void AddStatus(string type, Guid id, string number, string? from, string to, string action, string remarks, string correlation) => db.PurchaseTransactionStatusHistories.Add(new PurchaseTransactionStatusHistory { OrganizationId = RequireOrganization(), EntityType = type, EntityId = id, DocumentNumber = number, Action = action, FromStatus = from, ToStatus = to, ActorEmployeeId = RequireActor(), ActorLoginId = user.LoginId, ActorRoleCode = user.RoleCode, Remarks = RequiredRemarks(remarks), CorrelationId = correlation.Trim(), CreatedBy = user.LoginId });
    private Guid RequireActor() => user.IsAuthenticated && user.EmployeeId.HasValue ? user.EmployeeId.Value : throw new UnauthorizedAccessException("A unique active employee identity mapping is required.");
    private string RequireOrganization() => !string.IsNullOrWhiteSpace(user.OrganizationId) ? user.OrganizationId.Trim() : throw new UnauthorizedAccessException("Organization scope is required.");
    private bool IsRole(string code) => Rev869ARoleCodes.Normalize(user.RoleCode) == Rev869ARoleCodes.Normalize(code);
    private void RequireRole(params string[] allowed) { if (!allowed.Any(IsRole)) throw new UnauthorizedAccessException("Role is not authorized for this operation."); }
    private static void CheckVersion(uint actual, uint expected) { if (actual != expected) throw new DbUpdateConcurrencyException("The controlled record changed; reload before retrying."); }
    private static string NormalizeCurrency(string value) { var code = Required(value, "Currency").ToUpperInvariant(); if (code.Length != 3 || code.Any(x => !char.IsLetter(x))) throw new InvalidOperationException("ISO 4217 currency code must contain three letters."); return code; }
    private static string Required(string? value, string name) => !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new InvalidOperationException($"{name} is required.");
    private static string RequiredRemarks(string? value) => Required(value, "Remarks");
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static Rev869BDocumentResult Result(Guid id, string number, string status, uint version) => new(id, number, status, version);
}