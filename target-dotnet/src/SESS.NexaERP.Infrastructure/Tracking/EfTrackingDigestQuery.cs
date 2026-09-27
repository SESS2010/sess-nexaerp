using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Tracking;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Tracking;

/// <summary>Tracking-lite for system jobs (R1). Recipients now; pending rows with tracking-lite (target 1 Oct).</summary>
public sealed class EfTrackingDigestQuery(NexaErpDbContext db) : ITrackingDigestQuery
{
    public Task<IReadOnlyList<TrackingPendingRow>> PendingForRolesAsync(Guid companyId, IReadOnlyCollection<string> roleCodes, CancellationToken ct) =>
        throw new NotSupportedException("Tracking-lite pending rows land on integration/r1 by 1 October; the digest cannot be composed before that.");

    /// <summary>
    /// Active employees holding one of the roles in the company today (approved assignment, company role enabled),
    /// with an official e-mail. An employee without an e-mail is not returned (the digest job logs the skip).
    /// </summary>
    public async Task<IReadOnlyList<DigestRecipient>> RecipientsAsync(Guid companyId, IReadOnlyCollection<string> roleCodes, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(330));
        var roles = roleCodes.Select(r => r.Trim().ToUpperInvariant()).Distinct().ToArray();
        var rows = await db.EmployeeRoleAssignments.AsNoTracking()
            .Where(a => a.CompanyId == companyId && roles.Contains(a.Role!.Code) && a.Role.IsActive
                && (a.ApprovalStatus == "Approved" || a.ApprovalStatus == "SeedApproved")
                && a.EffectiveFrom <= today && (!a.EffectiveTo.HasValue || a.EffectiveTo >= today)
                && a.Employee!.Status == "Active" && a.Employee.OfficialEmail != null && a.Employee.OfficialEmail != ""
                && db.CompanyRoleActivations.Any(c => c.CompanyId == companyId && c.RoleId == a.RoleId && c.IsEnabled
                    && c.EffectiveFrom <= today && (!c.EffectiveTo.HasValue || c.EffectiveTo >= today)))
            .Select(a => new { a.EmployeeId, a.Employee!.EmployeeCode, a.Employee.EmployeeName, Email = a.Employee.OfficialEmail!, Role = a.Role!.Code })
            .ToListAsync(ct);
        return rows.GroupBy(r => r.EmployeeId)
            .Select(g => new DigestRecipient(g.Key, g.First().EmployeeCode, g.First().EmployeeName, g.First().Email.Trim(),
                g.Select(r => r.Role).Distinct().Order(StringComparer.Ordinal).ToArray()))
            .OrderBy(r => r.EmployeeCode, StringComparer.Ordinal).ToArray();
    }
}
