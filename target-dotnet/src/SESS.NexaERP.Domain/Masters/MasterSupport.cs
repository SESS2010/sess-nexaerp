using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Masters;

public static class MasterStatuses
{
    public const string Draft = "Draft";
    public const string PendingApproval = "Pending Approval";
    public const string Active = "Active";
    public const string Approved = "Approved";
    public const string OnHold = "On Hold";
    public const string Blacklisted = "Blacklisted";
    public const string Inactive = "Inactive";
    public const string Rejected = "Rejected";
}

public static class MasterApprovalStatuses
{
    public const string Draft = "Draft";
    public const string PendingApproval = "Pending Approval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string ClarificationRequested = "Clarification Requested";
    public const string RevisionRequested = "Revision Requested";
}

public sealed class ItemCategory : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ItemSubcategory : AuditableEntity
{
    public Guid CategoryId { get; set; }
    public ItemCategory? Category { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class Uom : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MeasurementDimension { get; set; } = string.Empty;
    public int QuantityPrecision { get; set; } = 6;
    public bool IsActive { get; set; } = true;
}

public sealed class Manufacturer : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class VendorCategory : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class VendorContact : AuditableEntity
{
    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string ContactPerson { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class VendorAddress : AuditableEntity
{
    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string AddressType { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? State { get; set; }
    public string? StateCode { get; set; }
    public string Country { get; set; } = "India";
    public bool IsPrimary { get; set; }
}

public sealed class CustomerContact : AuditableEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string ContactPerson { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class CustomerAddress : AuditableEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string AddressType { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? SiteName { get; set; }
    public string? State { get; set; }
    public string? StateCode { get; set; }
    public string Country { get; set; } = "India";
    public bool IsPrimary { get; set; }
}

public sealed class MasterStatusHistory : AuditableEntity
{
    public string MasterType { get; set; } = string.Empty;
    public Guid MasterId { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string SourceRevision { get; set; } = "REV867";
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class MasterApprovalHistory : AuditableEntity
{
    public string MasterType { get; set; } = string.Empty;
    public Guid MasterId { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string ActorLoginId { get; set; } = string.Empty;
    public string ActorRoleCode { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class MasterAttachmentMetadata : AuditableEntity
{
    public string MasterType { get; set; } = string.Empty;
    public Guid MasterId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public bool IsActive { get; set; } = true;
}

