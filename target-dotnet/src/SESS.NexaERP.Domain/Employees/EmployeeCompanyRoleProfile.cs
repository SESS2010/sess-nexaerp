using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Employees;

public sealed class EmployeeCompanyRoleProfile : CompanyScopedAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string ConfigurationStatus { get; set; } = EmployeeRoleProfileStatuses.Pending;
    public Guid? PrimaryRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? PrimaryRoleAssignment { get; set; }
}

public static class EmployeeRoleProfileStatuses
{
    public const string Pending = "PENDING";
    public const string Configured = "CONFIGURED";
}
