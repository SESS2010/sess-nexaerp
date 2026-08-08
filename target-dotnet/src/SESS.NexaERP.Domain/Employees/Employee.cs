using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Employees;

public sealed class Employee : AuditableEntity
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string OriginalImportedName { get; set; } = string.Empty;
    public string EmployeeType { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid DesignationId { get; set; }
    public Designation? Designation { get; set; }
    public string Status { get; set; } = "Active";
    public DateOnly? DateOfJoining { get; set; }
    public string? OfficialEmail { get; set; }
    public string? MobileNumber { get; set; }
    public bool LoginEnabled { get; set; }
    public string ApprovalStatus { get; set; } = "SeedApproved";
    public bool IsEmployeeCodeLocked { get; set; } = true;
}