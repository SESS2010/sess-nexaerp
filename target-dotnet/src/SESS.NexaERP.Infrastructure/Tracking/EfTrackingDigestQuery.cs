using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Tracking;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Infrastructure.Tracking;

/// <summary>Tracking-lite for system jobs (R1): the 09:00 digest's pending rows and recipients, with no signed-in user.</summary>
public sealed class EfTrackingDigestQuery(NexaErpDbContext db, IOptions<ReportCalendarOptions> calendar) : ITrackingDigestQuery
{
    /// <summary>
    /// Every pending row of the company whose "pending with" role is one of the roles (advance.tracking_source, no user
    /// scope: the digest goes to the role holders). The digest is per role, so the accounts manager who entered a bill
    /// still sees it listed; the database refuses their decision (R3). Overdue first, then oldest.
    /// </summary>
    public async Task<IReadOnlyList<TrackingPendingRow>> PendingForRolesAsync(Guid companyId, IReadOnlyCollection<string> roleCodes, CancellationToken ct)
    {
        var roles = roleCodes.Select(r => r.Trim().ToUpperInvariant()).Distinct().ToArray();
        if (roles.Length == 0) return [];
        var company = await db.Companies.AsNoTracking().Where(c => c.Id == companyId).Select(c => c.Code).SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Company not found.");
        var zone = calendar.Value.Resolve(company);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT s.doc_type,s.queue,s.document_id,s.number,s.status,s.pending_role,r."Name",e."EmployeeCode",e."EmployeeName",
              s.waiting_since,s.age_days,s.overdue_after_days,s.is_overdue,s.link
            FROM advance.tracking_source(@company,@report_timezone) s
            LEFT JOIN advance.roles r ON r."Code"=s.pending_role
            LEFT JOIN advance.employees e ON e."Id"=s.pending_employee_id
            WHERE s.pending_role=ANY(@roles)
            ORDER BY s.is_overdue DESC,s.waiting_since,s.queue,s.document_id
            """, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction()) { CommandTimeout = 60 };
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("report_timezone", ReportCalendarOptions.PostgreSqlName(zone));
        command.Parameters.AddWithValue("roles", NpgsqlDbType.Array | NpgsqlDbType.Text, roles);
        var rows = new List<TrackingPendingRow>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new(reader.GetString(0), reader.GetString(1), reader.GetGuid(2), reader.GetString(3), reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.GetFieldValue<DateTimeOffset>(9), reader.GetInt32(10), reader.GetInt32(11), reader.GetBoolean(12), reader.GetString(13)));
        return rows;
    }

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
