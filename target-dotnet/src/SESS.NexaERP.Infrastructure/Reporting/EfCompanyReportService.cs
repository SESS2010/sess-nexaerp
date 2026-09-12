using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Reporting;

public sealed class EfCompanyReportService(NexaErpDbContext db, ICurrentUser user, IOptions<ReportCalendarOptions>? calendar = null) : ICompanyReportService
{
    private TimeZoneInfo CalendarZone => (calendar?.Value ?? new ReportCalendarOptions()).Resolve(user.OrganizationId);
    private DateOnly Today => ReportCalendarOptions.DateInZone(DateTimeOffset.UtcNow,CalendarZone);
    public async Task<IReadOnlyList<ReportDescriptor>> ListAsync(CancellationToken cancellationToken)
    {
        RequireIdentity();
        await using var command = await CommandAsync(cancellationToken);
        command.CommandText = $"WITH {ReportAccessSql.Context} SELECT \"PageKey\",bool_or(can_commercial) FROM (SELECT \"PageKey\",can_commercial FROM report_grants WHERE can_view UNION ALL SELECT \"PageKey\",false FROM employee_report_grants WHERE can_view) grants GROUP BY \"PageKey\"";
        AddIdentity(command);
        var permitted = new Dictionary<string,bool>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) permitted.Add(reader.GetString(0),reader.GetBoolean(1));
        // Commercial access is checked again by the data query, including for catalogue entries.
        return ReportDefinitions.All.Where(report => permitted.TryGetValue(report.PageKey,out var commercial) && (!report.Commercial || commercial))
            .Select(report => report.Descriptor).ToArray();
    }

    public async Task<CompanyReportPage> GetAsync(string key, CompanyReportRequest request, CancellationToken cancellationToken)
    {
        var definition = ReportDefinitions.Find(key);
        var normalized = Normalize(definition, request, Today);
        await using var command = await PrepareAsync(definition, normalized, false, cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        var header = await ReadHeaderAsync(reader, cancellationToken);
        var rows = new List<JsonElement>();
        while (await reader.ReadAsync(cancellationToken))
        {
            using var document = JsonDocument.Parse(reader.GetString(2));
            rows.Add(document.RootElement.Clone());
        }
        return new(definition.Key, definition.Title, user.OrganizationId!.Trim().ToUpperInvariant(),
            header.GetProperty("generatedAt").GetDateTimeOffset(), definition.UsesPeriod ? normalized.FromDate : null,
            normalized.ToDate!.Value, normalized.Mode, normalized.Page, normalized.PageSize,
            header.GetProperty("totalRows").GetInt64(), header.GetProperty("totalSourceRows").GetInt64(),
            normalized.Mode == "details" ? definition.DetailColumns : definition.SummaryColumns,
            rows, header.GetProperty("totals").EnumerateArray().Select(value => value.Clone()).ToArray(), definition.Coverage,
            header.GetProperty("timeZone").GetString()!);
    }

    public async Task<CompanyReportDownload> ExportAsync(string key, CompanyReportRequest request, CancellationToken cancellationToken)
    {
        var definition = ReportDefinitions.Find(key);
        var normalized = Normalize(definition, request with { Mode = "summary", Metric = null, Page = 1 }, Today);
        await using var command = await PrepareAsync(definition, normalized, true, cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        var header = await ReadHeaderAsync(reader, cancellationToken);
        using var workbook = new ReportWorkbook(definition, user.OrganizationId!.Trim().ToUpperInvariant(), normalized, header);
        while (await reader.ReadAsync(cancellationToken))
        {
            var kind = reader.GetInt32(0);
            using var document = JsonDocument.Parse(reader.GetString(2));
            workbook.Write(kind, document.RootElement);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new(workbook.Complete(), $"{definition.Key}-{normalized.ToDate:yyyy-MM-dd}.xlsx");
    }

    private async Task<NpgsqlCommand> PrepareAsync(ReportDefinition definition, CompanyReportRequest request, bool export, CancellationToken ct)
    {
        RequireIdentity();
        var command = await CommandAsync(ct);
        command.CommandText = definition.Key switch
        {
            "stock-balance" => StockReportSql.Build(false),
            "movement-roll-forward" => StockReportSql.Build(true),
            "engineer-custody" => GroupedReportSql.Build(definition, EngineerCustodyReportSql.Source, "sum((metrics->>'quantity')::numeric)<>0"),
            "fifo-valuation" => SESS.NexaERP.Infrastructure.Persistence.Migrations.ControlledCompanyReportSql.Invocation("fifo-valuation"),
            "pending-approvals" => SESS.NexaERP.Infrastructure.Persistence.Migrations.ControlledCompanyReportSql.Invocation("pending-approvals"),
            "purchase-register" => SESS.NexaERP.Infrastructure.Persistence.Migrations.ControlledCompanyReportSql.Invocation("purchase-register"),
            "grni" => SESS.NexaERP.Infrastructure.Persistence.Migrations.ControlledCompanyReportSql.Invocation("grni"),
            "vendor-purchases" => SESS.NexaERP.Infrastructure.Persistence.Migrations.ControlledCompanyReportSql.Invocation("vendor-purchases"),
            _ => throw new KeyNotFoundException("Report was not found.")
        };
        AddIdentity(command);
        command.Parameters.AddWithValue("page_key", definition.PageKey);
        command.Parameters.AddWithValue("export", export);
        command.Parameters.AddWithValue("commercial", definition.Commercial);
        command.Parameters.AddWithValue("login", user.LoginId);
        command.Parameters.AddWithValue("correlation", Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("from_date", NpgsqlDbType.Date, request.FromDate!.Value);
        command.Parameters.AddWithValue("to_date", NpgsqlDbType.Date, request.ToDate!.Value);
        command.Parameters.AddWithValue("mode", request.Mode);
        command.Parameters.Add("metric", NpgsqlDbType.Text).Value = (object?)request.Metric ?? DBNull.Value;
        command.Parameters.Add("group_filter", NpgsqlDbType.Jsonb).Value = (object?)request.Group ?? DBNull.Value;
        command.Parameters.AddWithValue("offset", ((long)request.Page - 1) * request.PageSize);
        command.Parameters.AddWithValue("page_size", request.PageSize);
        command.Parameters.AddWithValue("report_timezone",ReportCalendarOptions.PostgreSqlName(CalendarZone));
        return command;
    }

    private async Task<NpgsqlCommand> CommandAsync(CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        return new NpgsqlCommand { Connection = connection, Transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction(),
            CommandTimeout = 300 };
    }

    private void RequireIdentity()
    {
        if (!user.IsAuthenticated || user.EmployeeId is null || string.IsNullOrWhiteSpace(user.OrganizationId))
            throw new UnauthorizedAccessException("A resolved employee and selected company are required.");
    }

    private void AddIdentity(NpgsqlCommand command)
    {
        command.Parameters.AddWithValue("organization", user.OrganizationId!.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("employee", user.EmployeeId!.Value);
        command.Parameters.AddWithValue("assignments", NpgsqlDbType.Array | NpgsqlDbType.Uuid,
            user.EffectiveRoleAssignments.Select(assignment => assignment.AssignmentId).Distinct().ToArray());
    }

    private static async Task<JsonElement> ReadHeaderAsync(NpgsqlDataReader reader, CancellationToken ct)
    {
        if (!await reader.ReadAsync(ct) || reader.GetInt32(0) != 0)
            throw new InvalidOperationException("Report query did not return its access decision.");
        using var document = JsonDocument.Parse(reader.GetString(2));
        var header = document.RootElement;
        if (!header.GetProperty("allowed").GetBoolean()) throw new ReportAccessDeniedException();
        if (header.TryGetProperty("sourceReady",out var ready) && !ready.GetBoolean())
            throw new ReportSourceUnavailableException(header.GetProperty("sourceIssue").GetString() ?? "FIFO_SOURCE_INCONSISTENT");
        return header.Clone();
    }

    internal static CompanyReportRequest Normalize(ReportDefinition definition, CompanyReportRequest request, DateOnly? calendarToday = null)
    {
        var today = calendarToday ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var to = request.ToDate ?? today;
        var from = definition.UsesPeriod ? request.FromDate ?? new DateOnly(to.Year, to.Month, 1) : DateOnly.MinValue;
        if (to == DateOnly.MinValue || to == DateOnly.MaxValue || definition.UsesPeriod && (from == DateOnly.MinValue || from == DateOnly.MaxValue))
            throw new ReportRequestException("Report dates must be finite calendar dates.");
        if (definition.CurrentOnly && to != today) throw new ReportRequestException("Pending approvals show the current queue; use today as the to date.");
        if (from > to) throw new ReportRequestException("From date must not be after to date.");
        if (request.Mode is not ("summary" or "details")) throw new ReportRequestException("Mode must be summary or details.");
        if (request.Page < 1 || request.PageSize is < 1 or > 1000) throw new ReportRequestException("Page must be positive and page size must be from 1 to 1000.");
        if (request.Metric is not null && (request.Mode != "details" ||
            !definition.SummaryColumns.Any(column => column.Metric == request.Metric)))
            throw new ReportRequestException("The selected metric is not available for this report drill-through.");
        if (request.Group is not null)
        {
            if (request.Group.Length > 4096) throw new ReportRequestException("Drill-through selection is too large.");
            try
            {
                using var document = JsonDocument.Parse(request.Group);
                if (document.RootElement.ValueKind != JsonValueKind.Object) throw new ReportRequestException("Drill-through selection must be an object.");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var normalizedGroup = new Dictionary<string,string?>(StringComparer.Ordinal);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (!ReportDefinitions.FilterKeys(definition).Contains(property.Name) || !seen.Add(property.Name))
                        throw new ReportRequestException("Unknown or duplicate drill-through dimension.");
                    if (property.Value.ValueKind == JsonValueKind.Null) { normalizedGroup.Add(property.Name,null); continue; }
                    if (property.Value.ValueKind != JsonValueKind.String ||
                        (property.Name.EndsWith("Id", StringComparison.Ordinal) && !Guid.TryParse(property.Value.GetString(), out _)))
                        throw new ReportRequestException("Invalid drill-through dimension value.");
                    normalizedGroup.Add(property.Name,property.Name.EndsWith("Id",StringComparison.Ordinal)
                        ? Guid.Parse(property.Value.GetString()!).ToString("D") : property.Value.GetString());
                }
                request = request with { Group = JsonSerializer.Serialize(normalizedGroup) };
            }
            catch (JsonException) { throw new ReportRequestException("Drill-through selection is not valid JSON."); }
        }
        return request with { FromDate = from, ToDate = to };
    }
}
