using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Masters;

public sealed class EfTaxGstWorkflowService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit) : ITaxGstWorkflowService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] CreatorRoles = OperationRoleContracts.TaxGstCreate;
    private static readonly string[] DecisionRoles = ["TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR"];

    public async Task<TaxGstWorkflowResult> CreateAsync(CreateTaxGstSettingRequest request, string idempotencyKey, CancellationToken ct)
    {
        RequirePrincipal(request.OrganizationId);
        var actorRole = user.RequireRole("tax-gst:create", CreatorRoles);
        if (string.IsNullOrWhiteSpace(request.Remarks)) throw new InvalidOperationException("Tax-rule creation remarks are required.");
        var company = await db.Companies.AsNoTracking().SingleAsync(x => x.Code == request.OrganizationId && x.IsActive, ct);
        var candidate = Build(request, company.Id, user.EmployeeId!.Value);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var overlaps = await AuthoritativeRules(candidate).ToListAsync(ct);
        if (request.SupersedesTaxGstSettingId.HasValue)
        {
            var predecessor = overlaps.SingleOrDefault(x => x.Id == request.SupersedesTaxGstSettingId.Value)
                ?? throw new InvalidOperationException("The superseded tax rule must be the exact current approved rule.");
            if (await db.TaxGstSettings.AnyAsync(x => x.SupersedesTaxGstSettingId == predecessor.Id && x.ApprovalStatus == MasterApprovalStatuses.Approved, ct))
                throw new InvalidOperationException("The tax rule has already been superseded.");
        }
        else if (overlaps.Count != 0) throw new InvalidOperationException("An overlapping effective approved tax rule exists; create an explicit superseding version.");

        db.TaxGstSettings.Add(candidate);
        AddHistory(candidate, "Create", null, candidate, request.Remarks, 0);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(candidate.OrganizationId, "CreateTaxGstSetting", idempotencyKey, request);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(db, user, candidate.OrganizationId, envelope, ct, actorRole)
            ?? throw new InvalidOperationException("The tax-rule command produced no controlled mutation.");
        try
        {
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Settings", "CreateTaxGstSetting", nameof(TaxGstSetting), candidate.Id.ToString(), null, candidate, ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            await Rev869BCommandContextAuthorizer.RecordNoncommitOutcomeAsync((NpgsqlConnection)db.Database.GetDbConnection(), attempt, "RolledBack", "TaxWorkflowRolledBack", ct);
            throw;
        }
        return Result(candidate);
    }

    public Task<TaxGstWorkflowResult> ApproveAsync(Guid id, DecideTaxGstSettingRequest request, CancellationToken ct) => DecideAsync(id, request, true, ct);
    public Task<TaxGstWorkflowResult> RejectAsync(Guid id, DecideTaxGstSettingRequest request, CancellationToken ct) => DecideAsync(id, request, false, ct);

    private async Task<TaxGstWorkflowResult> DecideAsync(Guid id, DecideTaxGstSettingRequest request, bool approve, CancellationToken ct)
    {
        RequirePrincipal(user.OrganizationId);
        var actorRole = user.RequireRole(approve ? "tax-gst:approve" : "tax-gst:reject", DecisionRoles);
        if (string.IsNullOrWhiteSpace(request.Remarks)) throw new InvalidOperationException("Tax-rule decision remarks are required.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var rule = await db.TaxGstSettings.SingleOrDefaultAsync(x => x.Id == id && x.OrganizationId == user.OrganizationId, ct)
            ?? throw new KeyNotFoundException("Tax rule was not found.");
        if (rule.Version != request.ExpectedVersion) throw new DbUpdateConcurrencyException("Tax rule version is stale.");
        if (rule.ApprovalStatus != MasterApprovalStatuses.PendingApproval || rule.DecisionEmployeeId.HasValue)
            throw new InvalidOperationException("Only a pending tax rule may be decided.");
        if (rule.CreatorEmployeeId == user.EmployeeId) throw new UnauthorizedAccessException("A tax-rule creator cannot approve or reject the same rule.");
        if (approve && rule.SupersedesTaxGstSettingId.HasValue && await db.TaxGstSettings.AnyAsync(x => x.SupersedesTaxGstSettingId == rule.SupersedesTaxGstSettingId && x.Id != rule.Id && x.ApprovalStatus == MasterApprovalStatuses.Approved, ct))
            throw new InvalidOperationException("The predecessor already has an approved superseding version.");
        var before = Snapshot(rule);
        rule.ApprovalStatus = approve ? MasterApprovalStatuses.Approved : MasterApprovalStatuses.Rejected;
        rule.DecisionEmployeeId = user.EmployeeId;
        rule.DecisionRoleCode = actorRole;
        rule.DecisionAt = DateTimeOffset.UtcNow;
        rule.DecisionRemarks = request.Remarks.Trim();
        rule.IsActive = approve;
        rule.Version = checked(rule.Version + 1);
        rule.UpdatedAt = DateTimeOffset.UtcNow;
        rule.UpdatedBy = user.LoginId;
        var action = approve ? "Approve" : "Reject";
        AddHistory(rule, action, before, Snapshot(rule), request.Remarks, rule.Version);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(rule.OrganizationId, action + "TaxGstSetting", request.IdempotencyKey, new { id, request });
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(db, user, rule.OrganizationId, envelope, ct, actorRole)
            ?? throw new InvalidOperationException("The tax-rule command produced no controlled mutation.");
        try
        {
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Settings", action + "TaxGstSetting", nameof(TaxGstSetting), rule.Id.ToString(), before, Snapshot(rule), ct);
            await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            await Rev869BCommandContextAuthorizer.RecordNoncommitOutcomeAsync((NpgsqlConnection)db.Database.GetDbConnection(), attempt, "RolledBack", "TaxWorkflowRolledBack", ct);
            throw;
        }
        return Result(rule);
    }

    private IQueryable<TaxGstSetting> AuthoritativeRules(TaxGstSetting candidate) =>
        db.TaxGstSettings.Where(x => x.OrganizationId == candidate.OrganizationId && x.JurisdictionCode == candidate.JurisdictionCode &&
            x.HsnSacCode == candidate.HsnSacCode && x.SupplierStateCode == candidate.SupplierStateCode &&
            x.PlaceOfSupplyStateCode == candidate.PlaceOfSupplyStateCode && x.VendorRegistrationType == candidate.VendorRegistrationType &&
            x.ApprovalStatus == MasterApprovalStatuses.Approved && x.IsActive && x.EffectiveFrom <= (candidate.EffectiveTo ?? DateOnly.MaxValue) &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= candidate.EffectiveFrom) &&
            !db.TaxGstSettings.Any(child => child.SupersedesTaxGstSettingId == x.Id && child.ApprovalStatus == MasterApprovalStatuses.Approved && child.IsActive));

    private TaxGstSetting Build(CreateTaxGstSettingRequest r, Guid companyId, Guid creator)
    {
        var supplier = r.SupplierStateCode.Trim().ToUpperInvariant();
        var place = r.PlaceOfSupplyStateCode.Trim().ToUpperInvariant();
        if (!VendorRegistrationTypes.TryParseCanonical(r.VendorRegistrationType, out var registrationType))
            throw new InvalidOperationException("Vendor registration type must be one of the exact supported values.");
        var rule = new TaxGstSetting
        {
            CompanyId = companyId, OrganizationId = r.OrganizationId.Trim(), JurisdictionCode = r.JurisdictionCode.Trim().ToUpperInvariant(),
            HsnSacCode = r.HsnSacCode.Trim().ToUpperInvariant(), SupplierStateCode = supplier, PlaceOfSupplyStateCode = place,
            SupplyType = TaxGstSetting.ResolveSupplyType(supplier, place), VendorRegistrationType = registrationType.ToCanonicalValue(),
            GstRate = r.GstRate, CgstRate = r.CgstRate, SgstRate = r.SgstRate, IgstRate = r.IgstRate, CessRate = r.CessRate,
            IsExempt = r.IsExempt, IsReverseCharge = r.IsReverseCharge, CurrencyCode = r.CurrencyCode.Trim().ToUpperInvariant(),
            RoundingScale = r.RoundingScale, EffectiveFrom = r.EffectiveFrom, EffectiveTo = r.EffectiveTo,
            ApprovalStatus = MasterApprovalStatuses.PendingApproval, CreatorEmployeeId = creator,
            SupersedesTaxGstSettingId = r.SupersedesTaxGstSettingId, CreatedBy = user.LoginId
        };
        var rates = new[] { rule.GstRate, rule.CgstRate, rule.SgstRate, rule.IgstRate, rule.CessRate };
        if (!rates.All(TaxGstSetting.IsValidRate) || !TaxGstSetting.IsValidRange(rule.EffectiveFrom, rule.EffectiveTo) ||
            rule.RoundingScale is < 0 or > 6 || rule.CurrencyCode.Length != 3 || !rule.HasValidIndiaComponentSplit())
            throw new InvalidOperationException("Invalid tax rate, GST component split, effective range, currency or rounding scale.");
        return rule;
    }

    private void RequirePrincipal(string? organization)
    {
        if (!user.IsAuthenticated || !user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(organization) ||
            !string.Equals(user.OrganizationId, organization, StringComparison.Ordinal)) throw new UnauthorizedAccessException("An exact company employee identity is required.");
    }
    private void AddHistory(TaxGstSetting rule, string action, object? before, object after, string remarks, uint version) => db.ControlledConfigurationHistories.Add(new ControlledConfigurationHistory
    {
        CompanyId = rule.CompanyId, OrganizationId = rule.OrganizationId, EntityType = nameof(TaxGstSetting), EntityId = rule.Id, Action = action,
        BeforeJson = before is null ? null : JsonSerializer.Serialize(before, Json), AfterJson = JsonSerializer.Serialize(after, Json),
        ActorLoginId = user.LoginId, ActorRoleCode = user.RoleCode, Remarks = remarks.Trim(),
        CorrelationId = $"TAX|{rule.Id:N}|{version}|{action.ToUpperInvariant()}", CreatedBy = user.LoginId, Version = version
    });
    private static object Snapshot(TaxGstSetting x) => new { x.Id, x.OrganizationId, x.JurisdictionCode, x.HsnSacCode, x.SupplierStateCode, x.PlaceOfSupplyStateCode, x.VendorRegistrationType, x.GstRate, x.CgstRate, x.SgstRate, x.IgstRate, x.CessRate, x.IsExempt, x.IsReverseCharge, x.CurrencyCode, x.RoundingScale, x.EffectiveFrom, x.EffectiveTo, x.ApprovalStatus, x.CreatorEmployeeId, x.DecisionEmployeeId, x.DecisionRoleCode, x.DecisionAt, x.DecisionRemarks, x.SupersedesTaxGstSettingId, x.IsActive, x.Version };
    private static TaxGstWorkflowResult Result(TaxGstSetting x) => new(x.Id, x.ApprovalStatus, x.Version, x.CreatorEmployeeId, x.DecisionEmployeeId, x.DecisionRoleCode);
}
