using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Masters;

public sealed class Vendor : AuditableEntity
{
    public string VendorCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GstNumber { get; set; }
    public string? PanNumber { get; set; }
    public string ApprovalStatus { get; set; } = "Draft";
    public bool IsActive { get; set; } = true;
}
