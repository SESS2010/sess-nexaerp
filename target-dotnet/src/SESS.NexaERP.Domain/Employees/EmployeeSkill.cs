using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Employees;

public sealed class EmployeeSkill : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid SkillId { get; set; }
    public Skill? Skill { get; set; }
    public bool IsPrimary { get; set; } = true;
}