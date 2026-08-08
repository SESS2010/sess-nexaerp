using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Identity;

public sealed class Role : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsPrivileged { get; set; }
    public bool IsActive { get; set; } = true;
}
