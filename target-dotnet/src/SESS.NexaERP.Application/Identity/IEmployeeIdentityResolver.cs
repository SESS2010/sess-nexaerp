namespace SESS.NexaERP.Application.Identity;

public sealed record ResolvedEmployeeIdentity(
    bool Success,
    Guid? EmployeeId,
    Guid? DepartmentId,
    string? EmployeeCode,
    IReadOnlyList<string> RoleCodes,
    string Message);

public interface IEmployeeIdentityResolver
{
    Task<ResolvedEmployeeIdentity> ResolveAsync(string issuer, string subject, string? organizationId, DateOnly onDate, CancellationToken cancellationToken);
}
