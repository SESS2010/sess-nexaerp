using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Identity;

public sealed class Role : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsPrivileged { get; set; }
    public bool IsActive { get; set; } = true;
    public string Audience { get; set; } = RoleAudiences.InternalEmployee;
    public string BusinessArea { get; set; } = RoleBusinessAreas.General;
    public bool IsEmployeeAssignable { get; set; } = true;
    public Guid? ReplacementRoleId { get; set; }
    public Role? ReplacementRole { get; set; }
}

public static class RoleAudiences
{
    public const string InternalEmployee = "INTERNAL_EMPLOYEE";
    public const string ExternalPortal = "EXTERNAL_PORTAL";
    public const string LegacyAlias = "LEGACY_ALIAS";
    public const string SystemSecurity = "SYSTEM_SECURITY";
}

public static class RoleBusinessAreas
{
    public const string General = "GENERAL";
}
