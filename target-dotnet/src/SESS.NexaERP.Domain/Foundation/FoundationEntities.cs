using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Foundation;

public static class PrincipalTypes
{
    public const string Internal = "INTERNAL";
    public const string Vendor = "VENDOR";
    public const string Customer = "CUSTOMER";
    public const string Service = "SERVICE";
}

public sealed class Company : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public bool IsActive { get; set; } = true;
}

public sealed class UserIdentityMapping : AuditableEntity
{
    public Guid UserAccountId { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string IdentityKind { get; set; } = "HUMAN";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeUserBinding : AuditableEntity
{
    public Guid UserAccountId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class VendorUserBinding : AuditableEntity
{
    public Guid UserAccountId { get; set; }
    public Guid VendorId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimaryContact { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CustomerUserBinding : AuditableEntity
{
    public Guid UserAccountId { get; set; }
    public Guid CustomerId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimaryContact { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UserRoleAssignment : AuditableEntity
{
    public Guid UserAccountId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Audience { get; set; } = "INTERNAL";
    public string Scope { get; set; } = "GLOBAL";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class VendorCompanyRelationship : CompanyScopedAuditableEntity
{
    public Guid VendorId { get; set; }
    public string? VendorAssignedCustomerCode { get; set; }
    public string RelationshipStatus { get; set; } = "ACTIVE";
    public Guid? PaymentTermId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CustomerCompanyRelationship : CompanyScopedAuditableEntity
{
    public Guid CustomerId { get; set; }
    public string? CustomerAssignedSupplierCode { get; set; }
    public string RelationshipStatus { get; set; } = "ACTIVE";
    public Guid? PaymentTermId { get; set; }
    public int? CreditPeriodDays { get; set; }
    public decimal? CreditLimit { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CompanySite : CompanyScopedAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SiteType { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string State { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "IN";
    public string TimeZoneId { get; set; } = "Asia/Kolkata";
    public bool IsActive { get; set; } = true;
}

public sealed class CompanyGstRegistration : CompanyScopedAuditableEntity
{
    public Guid? CompanySiteId { get; set; }
    public string Gstin { get; set; } = string.Empty;
    public string RegisteredLegalName { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public string RegistrationType { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Currency : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NumericCode { get; set; }
    public int MinorUnitDigits { get; set; } = 2;
    public string? Symbol { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class FinancialPeriod : CompanyScopedAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PeriodType { get; set; } = "FINANCIAL_YEAR";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "OPEN";
    public DateTimeOffset? ClosedAt { get; set; }
    public Guid? ClosedByUserAccountId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CostCentre : CompanyScopedAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCostCentreId { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Project : CompanyScopedAuditableEntity
{
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Guid? CompanySiteId { get; set; }
    public Guid? CostCentreId { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetEndDate { get; set; }
    public DateOnly? ActualEndDate { get; set; }
    public string Status { get; set; } = "PLANNED";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeCompanyAssignment : CompanyScopedAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public string AssignmentType { get; set; } = "PAYROLL";
    public string EmployeeCode { get; set; } = string.Empty;
    public string? PayrollEmployeeId { get; set; }
    public Guid? CompanySiteId { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeDepartmentAssignment : CompanyScopedAuditableEntity
{
    public Guid EmployeeCompanyAssignmentId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid DesignationId { get; set; }
    public string AssignmentType { get; set; } = "PRIMARY";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public bool IsActive { get; set; } = true;
}

public sealed class Asset : CompanyScopedAuditableEntity
{
    public string AssetCode { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public Guid? ItemId { get; set; }
    public string? SerialNumber { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CustomerAddressId { get; set; }
    public Guid? CompanySiteId { get; set; }
    public DateOnly? InstallationDate { get; set; }
    public DateOnly? WarrantyStartDate { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Document : AuditableEntity
{
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? OwnerDepartmentId { get; set; }
    public Guid? CurrentRevisionId { get; set; }
    public string Status { get; set; } = "DRAFT";
    public bool IsActive { get; set; } = true;
}

public sealed class DocumentRevision : AuditableEntity
{
    public Guid DocumentId { get; set; }
    public string RevisionCode { get; set; } = string.Empty;
    public int RevisionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public byte[] Sha256 { get; set; } = [];
    public string? ChangeSummary { get; set; }
    public string Status { get; set; } = "DRAFT";
    public DateOnly? EffectiveFrom { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public Guid? ReleasedByUserAccountId { get; set; }
    public bool IsCurrent { get; set; }
}

public sealed class DocumentNumberSequence : CompanyScopedAuditableEntity
{
    public string DocumentType { get; set; } = string.Empty;
    public Guid FinancialPeriodId { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public string? Suffix { get; set; }
    public int PaddingLength { get; set; } = 6;
    public long LastNumber { get; set; }
    public string? FormatPattern { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PaymentTerm : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DueDays { get; set; }
    public decimal AdvancePercentage { get; set; }
    public int? DiscountDays { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public bool IsActive { get; set; } = true;
}
