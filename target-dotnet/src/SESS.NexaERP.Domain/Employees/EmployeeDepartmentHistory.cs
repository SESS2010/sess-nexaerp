using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Employees;

public sealed class EmployeeDepartmentHistory : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? PreviousDepartmentId { get; set; }
    public Department? PreviousDepartment { get; set; }
    public Guid NewDepartmentId { get; set; }
    public Department? NewDepartment { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string SourceRevision { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
