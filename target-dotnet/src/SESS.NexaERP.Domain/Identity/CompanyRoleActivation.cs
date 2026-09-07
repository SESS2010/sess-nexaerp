using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Identity;

public sealed class CompanyRoleActivation : CompanyScopedAuditableEntity
{
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public bool IsEnabled { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Remarks { get; set; } = string.Empty;
}
