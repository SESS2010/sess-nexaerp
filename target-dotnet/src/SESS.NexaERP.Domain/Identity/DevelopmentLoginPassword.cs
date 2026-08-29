using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;

namespace SESS.NexaERP.Domain.Identity;

/// <summary>
/// Per-employee development password (PBKDF2 hash). Used only by the
/// Debug-only development sign-in pipeline; production authentication remains
/// OIDC (REV866) and never reads this table.
/// </summary>
public sealed class DevelopmentLoginPassword : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
}
