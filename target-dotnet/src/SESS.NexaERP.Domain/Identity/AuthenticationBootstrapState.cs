using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Identity;

public static class AuthenticationBootstrapStatuses
{
    public const string Pending = "PENDING";
    public const string Completed = "COMPLETED";
}

public sealed class AuthenticationBootstrapState : AuditableEntity
{
    public string Status { get; set; } = AuthenticationBootstrapStatuses.Pending;
    public Guid? EmployeeId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? OrganizationId { get; set; }
    public byte[]? IssuerSha256 { get; set; }
    public byte[]? SubjectSha256 { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
}
