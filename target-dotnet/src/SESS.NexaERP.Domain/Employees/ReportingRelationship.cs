using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Employees;

public sealed class ReportingRelationship : CompanyScopedAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? ReportingManagerEmployeeId { get; set; }
    public Employee? ReportingManager { get; set; }
    public Guid? DepartmentHeadEmployeeId { get; set; }
    public Employee? DepartmentHead { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}
