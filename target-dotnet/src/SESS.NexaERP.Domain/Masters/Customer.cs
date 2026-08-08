using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Masters;

public sealed class Customer : AuditableEntity
{
    public string CustomerCode { get; set; } = string.Empty;
    public bool IsCustomerCodeLocked { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LegalCustomerName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string CustomerType { get; set; } = string.Empty;
    public string? GstNumber { get; set; }
    public string? PanNumber { get; set; }
    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }
    public string? State { get; set; }
    public string? StateCode { get; set; }
    public string Country { get; set; } = "India";
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Industry { get; set; }
    public string? PaymentTerms { get; set; }
    public int? CreditPeriodDays { get; set; }
    public decimal? CreditLimit { get; set; }
    public string PortalOrganizationId { get; set; } = string.Empty;
    public string Status { get; set; } = MasterStatuses.Draft;
    public string ApprovalStatus { get; set; } = MasterApprovalStatuses.Draft;
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
