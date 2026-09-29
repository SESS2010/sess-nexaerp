using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Application.Tracking;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Infrastructure.Tracking;

/// <summary>
/// Tracking-lite for the signed-in user (R1). The database decides what the user may see (advance.tracking_pending and
/// advance.tracking_history: queue page View plus the dashboard scope rule); this class filters, orders and pages.
/// </summary>
public sealed class EfTrackingService(NexaErpDbContext db, ICurrentUser user, IOptions<ReportCalendarOptions> calendar) : ITrackingService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<TrackingPendingPage> PendingAsync(TrackingPendingRequest request, CancellationToken ct)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 200)
            throw new ReportRequestException("Page must be positive and page size must be from 1 to 200.");
        if (request.DocType is not null && !TrackingDocTypes.All.Contains(request.DocType))
            throw new ReportRequestException("Unknown document type.");
        var (data, zone) = await ReadAsync(ct);
        var queues = data.Queues.ToDictionary(x => x.Queue, StringComparer.Ordinal);
        if (request.Queue is not null && !queues.ContainsKey(request.Queue))
            throw new ReportRequestException("Unknown tracking queue.");
        var roles = data.RoleCodes.ToHashSet(StringComparer.Ordinal);
        var employee = user.EmployeeId!.Value;
        var rows = data.Rows
            .Where(x => request.DocType is null || x.DocType == request.DocType)
            .Where(x => request.Queue is null || x.Queue == request.Queue)
            .Where(x => !request.OverdueOnly || x.IsOverdue)
            .Where(x => !request.Mine || IsMine(x, employee, roles))
            .ToArray();
        var items = rows.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(x => x.ToRow()).ToArray();
        return new(rows.Length, request.Page, request.PageSize, data.GeneratedAt, zone, items);
    }

    public async Task<IReadOnlyList<TrackingSummaryTile>> SummaryAsync(CancellationToken ct)
    {
        var (data, _) = await ReadAsync(ct);
        return data.Queues.Select(q =>
        {
            if (q.State != "READY") return new TrackingSummaryTile(q.DocType, q.Queue, q.Title, q.State, null, null, null);
            var rows = data.Rows.Where(x => x.Queue == q.Queue).ToArray();
            return new TrackingSummaryTile(q.DocType, q.Queue, q.Title, q.State, rows.Length, rows.Count(x => x.IsOverdue),
                rows.Length == 0 ? null : rows.Max(x => x.AgeDays));
        }).ToArray();
    }

    public async Task<TrackingHistory?> HistoryAsync(string docType, Guid documentId, CancellationToken ct)
    {
        var type = docType.Trim().ToUpperInvariant().Replace('-', '_');
        if (!TrackingDocTypes.All.Contains(type)) throw new ReportRequestException("Unknown document type.");
        var (organization, zone) = Identity();
        await using var command = await CommandAsync(
            "SELECT advance.tracking_history(@organization,@employee,@assignments,@report_timezone,@doc_type,@document)", organization, zone, ct);
        command.Parameters.AddWithValue("doc_type", type);
        command.Parameters.AddWithValue("document", documentId);
        using var result = JsonDocument.Parse((string)(await command.ExecuteScalarAsync(ct))!);
        if (!result.RootElement.GetProperty("allowed").GetBoolean()) throw new ReportAccessDeniedException();
        if (!result.RootElement.GetProperty("found").GetBoolean()) return null;
        return result.RootElement.GetProperty("data").Deserialize<TrackingHistory>(Json)!;
    }

    internal static bool IsMine(PendingJson row, Guid employee, IReadOnlySet<string> roles) =>
        row.NotForEmployeeId != employee &&
        (row.PendingWithEmployeeId is { } named ? named == employee : row.PendingWithRole is { } role && roles.Contains(role));

    private async Task<(PendingData Data, string Zone)> ReadAsync(CancellationToken ct)
    {
        var (organization, zone) = Identity();
        await using var command = await CommandAsync(
            "SELECT advance.tracking_pending(@organization,@employee,@assignments,@report_timezone)", organization, zone, ct);
        using var result = JsonDocument.Parse((string)(await command.ExecuteScalarAsync(ct))!);
        if (!result.RootElement.GetProperty("allowed").GetBoolean()) throw new ReportAccessDeniedException();
        return (result.RootElement.GetProperty("data").Deserialize<PendingData>(Json)!, zone.Id);
    }

    private (string Organization, TimeZoneInfo Zone) Identity()
    {
        if (!user.IsAuthenticated || user.EmployeeId is null || string.IsNullOrWhiteSpace(user.OrganizationId))
            throw new UnauthorizedAccessException("A resolved employee and selected company are required.");
        var organization = user.OrganizationId.Trim().ToUpperInvariant();
        return (organization, calendar.Value.Resolve(organization));
    }

    private async Task<NpgsqlCommand> CommandAsync(string sql, string organization, TimeZoneInfo zone, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        var command = new NpgsqlCommand(sql, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction())
            { CommandTimeout = 60 };
        command.Parameters.AddWithValue("organization", organization);
        command.Parameters.AddWithValue("employee", user.EmployeeId!.Value);
        command.Parameters.AddWithValue("assignments", NpgsqlDbType.Array | NpgsqlDbType.Uuid,
            user.EffectiveRoleAssignments.Select(x => x.AssignmentId).Distinct().ToArray());
        command.Parameters.AddWithValue("report_timezone", ReportCalendarOptions.PostgreSqlName(zone));
        return command;
    }

    internal sealed record PendingData(DateTimeOffset GeneratedAt, string[] RoleCodes, QueueJson[] Queues, PendingJson[] Rows);
    internal sealed record QueueJson(string Queue, string DocType, string Title, string State, int OverdueAfterDays);
    internal sealed record PendingJson(string DocType, string Queue, Guid DocumentId, string Number, string Status,
        string? PendingWithRole, string? PendingWithRoleName, Guid? PendingWithEmployeeId, string? PendingWithEmployeeCode,
        string? PendingWithEmployeeName, Guid? NotForEmployeeId, DateTimeOffset WaitingSince, int AgeDays, int OverdueAfterDays,
        bool IsOverdue, string Link)
    {
        public TrackingPendingRow ToRow() => new(DocType, Queue, DocumentId, Number, Status, PendingWithRole, PendingWithRoleName,
            PendingWithEmployeeCode, PendingWithEmployeeName, WaitingSince, AgeDays, OverdueAfterDays, IsOverdue,
            TrackingDocumentLinks.Document(DocType, DocumentId, Number));
    }
}
