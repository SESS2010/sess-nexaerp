using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Employees;

public sealed class EmployeeRoleAssignmentEvent : CompanyScopedAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid? AssignmentId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? FromRoleCode { get; set; }
    public string? ToRoleCode { get; set; }
    public bool? PreviousRoleRetained { get; set; }
    public DateOnly EffectiveOn { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ActorLoginId { get; set; } = string.Empty;
    public string ActorRoleCode { get; set; } = string.Empty;
}
