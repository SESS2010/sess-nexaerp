using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Reporting;

public sealed class EfPurchaseWorkloadService(
    NexaErpDbContext db, ICurrentUser user, IOptions<ReportCalendarOptions> calendar)
    : IPurchaseWorkloadService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> Queues =
    [
        "pr-department-verification", "pr-approval", "pr-stock-check", "rfq-no-quotation",
        "quotation-technical-verification", "comparison-decision", "po-approved-unissued"
    ];

    public async Task<PurchaseWorkloadPage> GetAsync(PurchaseWorkloadRequest request, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.EmployeeId is null || string.IsNullOrWhiteSpace(user.OrganizationId))
            throw new UnauthorizedAccessException("A resolved employee and selected company are required.");
        if (request.Page < 1 || request.PageSize is < 1 or > 1000)
            throw new ReportRequestException("Page must be positive and page size must be from 1 to 1000.");
        if (request.Queue is not null && !Queues.Contains(request.Queue))
            throw new ReportRequestException("Unknown purchase workload queue.");
        if (request.ApprovalRoute is not null &&
            (request.Queue != "pr-approval" || request.ApprovalRoute.Length is 0 or > 80))
            throw new ReportRequestException("Approval route selection requires the PR approval queue.");

        var organization = user.OrganizationId.Trim().ToUpperInvariant();
        var zone = calendar.Value.Resolve(organization);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(
            "SELECT advance.purchase_workload(@organization,@employee,@assignments,@report_timezone,@queue,@route,@offset,@page_size)",
            connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction())
            { CommandTimeout = 60 };
        command.Parameters.AddWithValue("organization", organization);
        command.Parameters.AddWithValue("employee", user.EmployeeId.Value);
        command.Parameters.AddWithValue("assignments", NpgsqlDbType.Array | NpgsqlDbType.Uuid,
            user.EffectiveRoleAssignments.Select(x => x.AssignmentId).Distinct().ToArray());
        command.Parameters.AddWithValue("report_timezone", ReportCalendarOptions.PostgreSqlName(zone));
        command.Parameters.Add("queue", NpgsqlDbType.Text).Value = (object?)request.Queue ?? DBNull.Value;
        command.Parameters.Add("route", NpgsqlDbType.Text).Value = (object?)request.ApprovalRoute ?? DBNull.Value;
        command.Parameters.AddWithValue("offset", ((long)request.Page - 1) * request.PageSize);
        command.Parameters.AddWithValue("page_size", request.PageSize);
        using var result = JsonDocument.Parse((string)(await command.ExecuteScalarAsync(ct))!);
        if (!result.RootElement.GetProperty("allowed").GetBoolean())
            throw new ReportAccessDeniedException();
        var payload = result.RootElement.GetProperty("data");
        var rows = payload.GetProperty("rows").Deserialize<PurchaseWorkloadRow[]>(Json)!;
        rows = rows.Select(row => row with { DetailPath = DetailPath(row) }).ToArray();
        return new(organization, payload.GetProperty("generatedAt").GetDateTimeOffset(), zone.Id,
            payload.GetProperty("tiles").Deserialize<PurchaseWorkloadTile[]>(Json)!,
            request.Queue, request.ApprovalRoute, request.Page, request.PageSize,
            payload.GetProperty("totalRows").GetInt64(), rows);
    }

    private static string DetailPath(PurchaseWorkloadRow row)
    {
        var prefix = row.DocumentType switch
        {
            "PR" => "/api/v1/purchase/requisitions/",
            "RFQ" => "/api/v1/purchase/rfqs/",
            "QUOTATION" => "/api/v1/purchase/quotations/",
            "COMPARISON" => "/api/v1/purchase/comparisons/",
            "PO" => "/api/v1/purchase/purchase-orders/",
            _ => throw new InvalidOperationException("Unknown workload source type.")
        };
        return prefix + Uri.EscapeDataString(row.DocumentNumber);
    }
}
