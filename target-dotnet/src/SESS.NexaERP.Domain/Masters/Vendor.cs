using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Masters;

public sealed class Vendor : AuditableEntity
{
    public string VendorCode { get; set; } = string.Empty;
    public bool IsVendorCodeLocked { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LegalVendorName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string VendorType { get; set; } = string.Empty;
    public string? GstNumber { get; set; }
    public string? PanNumber { get; set; }
    public bool MsmeStatus { get; set; }
    public string? MsmeNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }
    public string? State { get; set; }
    public string? StateCode { get; set; }
    public string Country { get; set; } = "India";
    public string? MaterialServiceCategories { get; set; }
    public string? ApprovedMakes { get; set; }
    public string? PaymentTerms { get; set; }
    public string? DeliveryTerms { get; set; }
    public int? CreditPeriodDays { get; set; }
    public string? BankMetadataJson { get; set; }
    public string? AttachmentMetadataJson { get; set; }
    public string ApprovalStatus { get; set; } = MasterApprovalStatuses.Draft;
    public string VendorStatus { get; set; } = MasterStatuses.Draft;
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
