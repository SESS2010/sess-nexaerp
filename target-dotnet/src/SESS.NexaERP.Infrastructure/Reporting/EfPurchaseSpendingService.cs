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

public sealed class EfPurchaseSpendingService(
    NexaErpDbContext db, ICurrentUser user, IOptions<ReportCalendarOptions> calendar)
    : IPurchaseSpendingService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task<PurchaseSpendingPage> GetAsync(PurchaseSpendingRequest request, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.EmployeeId is null || string.IsNullOrWhiteSpace(user.OrganizationId))
            throw new UnauthorizedAccessException("A resolved employee and selected company are required.");
        if (request.Page < 1 || request.PageSize is < 1 or > 1000 ||
            request.Period is not ("month" or "quarter" or "financial-year" or "twelve-months") ||
            request.Month is { Day: not 1 } || request.Month is not null && request.Period != "month")
            throw new ReportRequestException("Choose a valid period, first-of-month date and page size from 1 to 1000.");
        var currency = request.Currency?.Trim().ToUpperInvariant();
        if (currency is not null && (currency.Length != 3 || currency.Any(c => c is < 'A' or > 'Z')))
            throw new ReportRequestException("Currency must be a three-letter currency code.");
        request = request with { Currency = currency };
        var organization = user.OrganizationId.Trim().ToUpperInvariant();
        var zone = calendar.Value.Resolve(organization);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(
            "SELECT advance.purchase_spending(@organization,@employee,@assignments,@report_timezone," +
            "@period,@month,@vendor,@category,@currency,@bill,@offset,@page_size)",
            connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction())
            { CommandTimeout = 60 };
        command.Parameters.AddWithValue("organization", organization);
        command.Parameters.AddWithValue("employee", user.EmployeeId.Value);
        command.Parameters.AddWithValue("assignments", NpgsqlDbType.Array | NpgsqlDbType.Uuid,
            user.EffectiveRoleAssignments.Select(x => x.AssignmentId).Distinct().ToArray());
        command.Parameters.AddWithValue("report_timezone", ReportCalendarOptions.PostgreSqlName(zone));
        command.Parameters.AddWithValue("period", request.Period);
        command.Parameters.Add("month", NpgsqlDbType.Date).Value = (object?)request.Month ?? DBNull.Value;
        command.Parameters.Add("vendor", NpgsqlDbType.Uuid).Value = (object?)request.VendorId ?? DBNull.Value;
        command.Parameters.Add("category", NpgsqlDbType.Uuid).Value = (object?)request.CategoryId ?? DBNull.Value;
        command.Parameters.Add("currency", NpgsqlDbType.Text).Value = (object?)currency ?? DBNull.Value;
        command.Parameters.Add("bill", NpgsqlDbType.Uuid).Value = (object?)request.BillId ?? DBNull.Value;
        command.Parameters.AddWithValue("offset", ((long)request.Page - 1) * request.PageSize);
        command.Parameters.AddWithValue("page_size", request.PageSize);
        using var result = JsonDocument.Parse((string)(await command.ExecuteScalarAsync(ct))!);
        if (!result.RootElement.GetProperty("allowed").GetBoolean()) throw new ReportAccessDeniedException();
        if (!result.RootElement.GetProperty("valid").GetBoolean())
            throw new ReportRequestException("The selected month must be within the current twelve-month reporting window.");
        var payload = result.RootElement.GetProperty("data");
        return new(organization, payload.GetProperty("generatedAt").GetDateTimeOffset(), zone.Id,
            "Accepted bill material value plus allocated charges, less reversals, by local decision date and native currency. PO and bill counts measure documents with activity.",
            payload.GetProperty("periods").Deserialize<PurchaseSpendingPeriod[]>(Json)!,
            payload.GetProperty("topVendors").Deserialize<PurchaseSpendingGroup[]>(Json)!,
            payload.GetProperty("categories").Deserialize<PurchaseSpendingGroup[]>(Json)!,
            payload.GetProperty("monthlyTrend").Deserialize<PurchaseSpendingPeriod[]>(Json)!,
            payload.GetProperty("fromDate").Deserialize<DateOnly>(Json),
            payload.GetProperty("toDate").Deserialize<DateOnly>(Json), request,
            payload.GetProperty("totalRows").GetInt64(),
            payload.GetProperty("rows").Deserialize<PurchaseSpendingRow[]>(Json)!);
    }
}
