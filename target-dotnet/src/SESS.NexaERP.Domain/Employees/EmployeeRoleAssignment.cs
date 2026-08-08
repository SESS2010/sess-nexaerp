using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Domain.Employees;

public sealed class EmployeeRoleAssignment : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string ApprovalStatus { get; set; } = "SeedApproved";
    public string Remarks { get; set; } = string.Empty;
}