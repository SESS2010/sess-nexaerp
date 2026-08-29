using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Masters;

public sealed class EfUomConversionService(NexaErpDbContext db) : IUomConversionService
{
    public async Task<UomConversion> GetApprovedAsync(Guid conversionId, DateOnly onDate, CancellationToken cancellationToken)
    {
        var conversion = await db.UomConversions.AsNoTracking().Include(x => x.FromUom).Include(x => x.ToUom)
            .SingleOrDefaultAsync(x => x.Id == conversionId && x.IsActive && x.ApprovalStatus == MasterApprovalStatuses.Approved && x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate), cancellationToken)
            ?? throw new InvalidOperationException("No approved effective UOM conversion exists.");
        if (conversion.FromUom is null || conversion.ToUom is null || !string.Equals(conversion.FromUom.MeasurementDimension, conversion.ToUom.MeasurementDimension, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("UOM conversion crosses incompatible measurement dimensions.");
        return conversion;
    }
}

public sealed class EfTaxGstResolver(NexaErpDbContext db) : ITaxGstResolver
{
    public async Task<TaxGstSetting> ResolveAsync(TaxResolutionRequest request, CancellationToken cancellationToken)
    {
        if (request.TaxableValue < 0) throw new InvalidOperationException("Taxable value cannot be negative.");
        var supplyType = TaxGstSetting.ResolveSupplyType(request.SupplierStateCode, request.PlaceOfSupplyStateCode);
        var supplierState = request.SupplierStateCode.Trim().ToUpperInvariant();
        var placeOfSupply = request.PlaceOfSupplyStateCode.Trim().ToUpperInvariant();
        var matches = await db.TaxGstSettings.AsNoTracking()
            .Where(x => x.OrganizationId == request.OrganizationId && x.JurisdictionCode == request.JurisdictionCode && x.HsnSacCode == request.HsnSacCode && x.SupplierStateCode == supplierState && x.PlaceOfSupplyStateCode == placeOfSupply && x.SupplyType == supplyType && x.VendorRegistrationType == request.VendorRegistrationType)
            .Where(x => x.IsActive && x.ApprovalStatus == MasterApprovalStatuses.Approved && x.EffectiveFrom <= request.TransactionDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= request.TransactionDate))
            .Where(x => !db.TaxGstSettings.Any(child => child.SupersedesTaxGstSettingId == x.Id &&
                child.IsActive && child.ApprovalStatus == MasterApprovalStatuses.Approved &&
                child.EffectiveFrom <= request.TransactionDate &&
                (!child.EffectiveTo.HasValue || child.EffectiveTo.Value >= request.TransactionDate)))
            .Take(2).ToListAsync(cancellationToken);
        if (matches.Count != 1) throw new InvalidOperationException(matches.Count == 0 ? "No effective tax rule is configured." : "Effective tax configuration overlaps or is ambiguous.");
        if (!matches[0].HasValidIndiaComponentSplit()) throw new InvalidOperationException("GST component split is invalid for intrastate/interstate supply.");
        return matches[0];
    }
}
public sealed class EfVendorQualificationService(NexaErpDbContext db) : IVendorQualificationService
{
    public async Task<bool> IsEligibleAsync(Guid vendorId, string organizationId, Guid? itemCategoryId, DateOnly onDate, CancellationToken cancellationToken)
    {
        var vendor = await db.Vendors.AsNoTracking().SingleOrDefaultAsync(x => x.Id == vendorId, cancellationToken);
        if (vendor is null || !VendorQualification.IsVendorEligible(vendor, onDate)) return false;
        if (!itemCategoryId.HasValue) return true;
        return await db.VendorQualifications.AsNoTracking().AnyAsync(x => x.VendorId == vendorId && x.OrganizationId == organizationId && x.ItemCategoryId == itemCategoryId && x.IsActive && (x.VerificationStatus == MasterApprovalStatuses.Verified || x.VerificationStatus == MasterApprovalStatuses.Approved) && x.ApprovalStatus == MasterApprovalStatuses.Approved && x.VerifiedByEmployeeId.HasValue && x.ApprovedByEmployeeId.HasValue && x.VerifiedByEmployeeId != x.ApprovedByEmployeeId && x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate) && db.ControlledConfigurationHistories.Count(h => h.OrganizationId == x.OrganizationId && h.EntityType == nameof(VendorQualification) && h.EntityId == x.Id && h.Action == "Verify" && h.Version == x.Version - 1) == 1 && db.ControlledConfigurationHistories.Count(h => h.OrganizationId == x.OrganizationId && h.EntityType == nameof(VendorQualification) && h.EntityId == x.Id && h.Action == "Approve" && h.Version == x.Version) == 1, cancellationToken);
    }

    public async Task<string> ResolveFinalApproverRoleAsync(string organizationId, DateOnly onDate, CancellationToken cancellationToken)
    {
        var matches = await db.OrganizationPolicies.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.PolicyCode == Rev869APolicyCodes.VendorFinalApprover && x.IsActive && x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Take(2).Select(x => x.PolicyValue).ToListAsync(cancellationToken);
        return matches.Count == 1 ? matches[0] : throw new InvalidOperationException("Vendor final approver policy is missing or ambiguous.");
    }
}
