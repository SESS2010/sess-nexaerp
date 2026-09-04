namespace SESS.NexaERP.Application.Identity;

public sealed record ResolvedEmployeeIdentity(
    bool Success,
    Guid? EmployeeId,
    Guid? DepartmentId,
    string? OrganizationId,
    string? EmployeeCode,
    IReadOnlyList<string> RoleCodes,
    string Message,
    string? PrimaryRoleCode = null,
    string? ActingRoleCode = null)
{
    public static ResolvedEmployeeIdentity Failed(string message) => new(false, null, null, null, null, [], message);
}

public interface IEmployeeIdentityResolver
{
    Task<ResolvedEmployeeIdentity> ResolveAsync(string issuer, string subject, string? organizationId, DateOnly onDate, CancellationToken cancellationToken);
#if DEBUG
    Task<ResolvedEmployeeIdentity> ResolveDevelopmentEmployeeAsync(string employeeCode, string? organizationId, DateOnly onDate, CancellationToken cancellationToken);
#endif
}
