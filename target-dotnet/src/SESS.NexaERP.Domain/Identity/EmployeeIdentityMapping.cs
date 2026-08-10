using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;

namespace SESS.NexaERP.Domain.Identity;

public static class IdentityTypes
{
    public const string Human = "HUMAN";
    public const string Service = "SERVICE";
}

public sealed class EmployeeIdentityMapping : AuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string IdentityType { get; set; } = IdentityTypes.Human;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public static string NormalizeIssuer(string value) => value.Trim().TrimEnd('/').ToUpperInvariant();
    public static string NormalizeSubject(string value) => value.Trim();
}
