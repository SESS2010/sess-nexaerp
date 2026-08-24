using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;

namespace SESS.NexaERP.Domain.Masters;

public static class Rev869APolicyCodes
{
    public const string VendorFinalApprover = "VENDOR_FINAL_APPROVER";
    public const string InventoryValuationMethod = "INVENTORY_VALUATION_METHOD";
}

public static class InventoryValuationMethods
{
    public const string WeightedAverage = "WEIGHTED_AVERAGE";
    public const string Fifo = "FIFO";
}

public sealed class OrganizationPolicy : CompanyScopedAuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public string PolicyCode { get; set; } = string.Empty;
    public string PolicyValue { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class VendorQualification : CompanyScopedAuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public Guid? ItemCategoryId { get; set; }
    public ItemCategory? ItemCategory { get; set; }
    public string QualificationCode { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string VerificationStatus { get; set; } = MasterApprovalStatuses.PendingApproval;
    public Guid? VerifiedByEmployeeId { get; set; }
    public Employee? VerifiedByEmployee { get; set; }
    public string ApprovalStatus { get; set; } = MasterApprovalStatuses.PendingApproval;
    public Guid? ApprovedByEmployeeId { get; set; }
    public Employee? ApprovedByEmployee { get; set; }
    public bool IsActive { get; set; } = true;

    public static bool IsVendorEligible(Vendor vendor, DateOnly onDate) =>
        vendor.IsActive &&
        string.Equals(vendor.VendorStatus, MasterStatuses.Active, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(vendor.ApprovalStatus, MasterApprovalStatuses.Approved, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(vendor.CommercialVerificationStatus, MasterApprovalStatuses.Approved, StringComparison.OrdinalIgnoreCase) &&
        vendor.EffectiveFrom <= onDate && (!vendor.EffectiveTo.HasValue || vendor.EffectiveTo.Value >= onDate);
}

public sealed class ControlledConfigurationHistory : CompanyScopedAuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string ActorLoginId { get; set; } = string.Empty;
    public string ActorRoleCode { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
