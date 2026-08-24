using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Inventory;

public sealed class Warehouse : CompanyScopedAuditableEntity
{
    public string WarehouseCode { get; set; } = string.Empty;
    public bool IsWarehouseCodeLocked { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WarehouseType { get; set; } = string.Empty;
    public string? Location { get; set; }
    public Guid? ResponsibleEmployeeId { get; set; }
    public Employee? ResponsibleEmployee { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? DefaultReceivingLocationId { get; set; }
    public Guid? DefaultAcceptedLocationId { get; set; }
    public Guid? DefaultQcHoldLocationId { get; set; }
    public Guid? DefaultRejectedLocationId { get; set; }
    public Guid? DefaultRepairableLocationId { get; set; }
    public Guid? DefaultScrapLocationId { get; set; }
    public string Status { get; set; } = MasterStatuses.Draft;
    public string ApprovalStatus { get; set; } = MasterApprovalStatuses.Draft;
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
