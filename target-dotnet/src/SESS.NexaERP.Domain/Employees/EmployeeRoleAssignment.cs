using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Domain.Employees;

public sealed class EmployeeRoleAssignment : CompanyScopedAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string AssignmentType { get; set; } = EmployeeRoleAssignmentTypes.Full;
    public string ApprovalStatus { get; set; } = "SeedApproved";
    public string Remarks { get; set; } = string.Empty;
    public string? EndReason { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string? EndedBy { get; set; }
}

public static class EmployeeRoleAssignmentTypes
{
    public const string Full = "FULL";
    public const string Support = "SUPPORT";
    public const string Temporary = "TEMPORARY";
}
