using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Employees;

public sealed class EmployeeRoleAssignmentEvent : CompanyScopedAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid ActorEmployeeId { get; set; }
    public Guid? AssignmentId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? FromRoleCode { get; set; }
    public string? ToRoleCode { get; set; }
    public string? FromAssignmentType { get; set; }
    public string? ToAssignmentType { get; set; }
    public DateOnly? PreviousEffectiveFrom { get; set; }
    public DateOnly? PreviousEffectiveTo { get; set; }
    public DateOnly? NewEffectiveFrom { get; set; }
    public DateOnly? NewEffectiveTo { get; set; }
    public DateOnly EffectiveOn { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ActorLoginId { get; set; } = string.Empty;
    public string ActorRoleCode { get; set; } = string.Empty;
}
