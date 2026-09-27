namespace SESS.NexaERP.Application.Tracking;

/// <summary>One open item of tracking-lite (R1 contract: audit\contracts\R1-tracking-lite-frontend-contract.md).</summary>
public sealed record TrackingPendingRow(string DocType, string Queue, Guid DocumentId, string Number, string Status,
    string? PendingWithRole, string? PendingWithRoleName, string? PendingWithEmployeeCode, string? PendingWithEmployeeName,
    DateTimeOffset WaitingSince, int AgeDays, int OverdueAfterDays, bool IsOverdue, string Link);

/// <summary>An employee who receives a role digest: holds one of the roles in the company and has an official e-mail.</summary>
public sealed record DigestRecipient(Guid EmployeeId, string EmployeeCode, string EmployeeName, string Email, IReadOnlyList<string> RoleCodes);

/// <summary>
/// Tracking-lite for a system job with no signed-in user (the TD's 09:00 digest). RecipientsAsync is live now;
/// PendingForRolesAsync lands with tracking-lite (target 1 Oct) and refuses until then rather than returning an
/// empty list that would look like "nothing pending".
/// </summary>
public interface ITrackingDigestQuery
{
    Task<IReadOnlyList<TrackingPendingRow>> PendingForRolesAsync(Guid companyId, IReadOnlyCollection<string> roleCodes, CancellationToken ct);
    Task<IReadOnlyList<DigestRecipient>> RecipientsAsync(Guid companyId, IReadOnlyCollection<string> roleCodes, CancellationToken ct);
}
