using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Identity;

public sealed class UserAccount : AuditableEntity
{
    public string LoginId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public bool MfaRequired { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
}
