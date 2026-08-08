using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Domain.Authorization;

public sealed class RolePagePermission : AuditableEntity
{
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public Guid PageDefinitionId { get; set; }
    public PageDefinition? PageDefinition { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanApprove { get; set; }
    public bool CanExport { get; set; }
}
