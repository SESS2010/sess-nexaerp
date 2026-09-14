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

public sealed class EfPurchaseOpenOrdersService(
    NexaErpDbContext db, ICurrentUser user, IOptions<ReportCalendarOptions> calendar)
    : IPurchaseOpenOrdersService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task<PurchaseOpenOrdersPage> GetAsync(PurchaseOpenOrdersRequest request, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.EmployeeId is null || string.IsNullOrWhiteSpace(user.OrganizationId))
            throw new UnauthorizedAccessException("A resolved employee and selected company are required.");
        if (request.Page < 1 || request.PageSize is < 1 or > 1000)
            throw new ReportRequestException("Choose a page size from 1 to 1000.");
        var currency = request.Currency?.Trim().ToUpperInvariant();
        if (currency is not null && (currency.Length != 3 || currency.Any(c => c is < 'A' or > 'Z')))
            throw new ReportRequestException("Currency must be a three-letter currency code.");
        request = request with { Currency = currency };
        var organization = user.OrganizationId.Trim().ToUpperInvariant();
        var zone = calendar.Value.Resolve(organization);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(
            "SELECT advance.purchase_open_orders(@organization,@employee,@assignments,@report_timezone," +
            "@vendor,@currency,@root,@overdue_only,@offset,@page_size)",
            connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction())
            { CommandTimeout = 60 };
        command.Parameters.AddWithValue("organization", organization);
        command.Parameters.AddWithValue("employee", user.EmployeeId.Value);
        command.Parameters.AddWithValue("assignments", NpgsqlDbType.Array | NpgsqlDbType.Uuid,
            user.EffectiveRoleAssignments.Select(x => x.AssignmentId).Distinct().ToArray());
        command.Parameters.AddWithValue("report_timezone", ReportCalendarOptions.PostgreSqlName(zone));
        command.Parameters.Add("vendor", NpgsqlDbType.Uuid).Value = (object?)request.VendorId ?? DBNull.Value;
        command.Parameters.Add("currency", NpgsqlDbType.Text).Value = (object?)currency ?? DBNull.Value;
        command.Parameters.Add("root", NpgsqlDbType.Uuid).Value = (object?)request.RootPurchaseOrderId ?? DBNull.Value;
        command.Parameters.AddWithValue("overdue_only", request.OverdueOnly);
        command.Parameters.AddWithValue("offset", ((long)request.Page - 1) * request.PageSize);
        command.Parameters.AddWithValue("page_size", request.PageSize);
        using var result = JsonDocument.Parse((string)(await command.ExecuteScalarAsync(ct))!);
        if (!result.RootElement.GetProperty("allowed").GetBoolean()) throw new ReportAccessDeniedException();
        var data = result.RootElement.GetProperty("data");
        return new(organization, data.GetProperty("generatedAt").GetDateTimeOffset(), zone.Id,
            "Remaining quantity at the latest issued PO line payable per unit, by native currency. Age starts at first issue. Unconfirmed delivery dates do not produce overdue totals.",
            data.GetProperty("complete").GetBoolean(),data.GetProperty("openPoCount").Deserialize<long?>(Json),
            data.GetProperty("oldestAgeDays").Deserialize<int?>(Json),
            data.GetProperty("deliveryComplete").GetBoolean(),data.GetProperty("overduePoCount").Deserialize<long?>(Json),
            data.GetProperty("deliveryDateUnconfirmedPoCount").GetInt64(),
            data.GetProperty("sourceIssues").Deserialize<PurchaseOpenOrderIssue[]>(Json)!,
            data.GetProperty("amounts").Deserialize<PurchaseOpenOrderAmount[]?>(Json),request,
            data.GetProperty("totalRows").GetInt64(),data.GetProperty("rows").Deserialize<PurchaseOpenOrderRow[]>(Json)!);
    }
}
