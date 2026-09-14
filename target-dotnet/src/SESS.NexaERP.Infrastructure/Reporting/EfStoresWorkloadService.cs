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

public sealed class EfStoresWorkloadService(NexaErpDbContext db,ICurrentUser user,IOptions<ReportCalendarOptions> calendar)
    : IStoresWorkloadService
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    public async Task<StoresWorkloadPage> GetAsync(StoresWorkloadRequest request,CancellationToken ct)
    {
        if(!user.IsAuthenticated||user.EmployeeId is null||string.IsNullOrWhiteSpace(user.OrganizationId))
            throw new UnauthorizedAccessException("A resolved employee and selected company are required.");
        if(request.Page<1||request.PageSize is <1 or >1000)
            throw new ReportRequestException("Choose a positive page and page size from 1 to 1000.");
        if(request.Queue is not(null or "gate-no-grn" or "mir-approval" or "mir-unissued"))
            throw new ReportRequestException("Unknown Stores document workload queue.");
        var organization=user.OrganizationId.Trim().ToUpperInvariant();
        var zone=calendar.Value.Resolve(organization);
        var connection=(NpgsqlConnection)db.Database.GetDbConnection();
        if(connection.State!=ConnectionState.Open)await connection.OpenAsync(ct);
        await using var command=new NpgsqlCommand(
            "SELECT advance.stores_workload(@organization,@employee,@assignments,@report_timezone,@queue,@document,@offset,@page_size)",
            connection,(NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction()){CommandTimeout=60};
        command.Parameters.AddWithValue("organization",organization);
        command.Parameters.AddWithValue("employee",user.EmployeeId.Value);
        command.Parameters.AddWithValue("assignments",NpgsqlDbType.Array|NpgsqlDbType.Uuid,
            user.EffectiveRoleAssignments.Select(x=>x.AssignmentId).Distinct().ToArray());
        command.Parameters.AddWithValue("report_timezone",ReportCalendarOptions.PostgreSqlName(zone));
        command.Parameters.Add("queue",NpgsqlDbType.Text).Value=(object?)request.Queue??DBNull.Value;
        command.Parameters.Add("document",NpgsqlDbType.Uuid).Value=(object?)request.DocumentId??DBNull.Value;
        command.Parameters.AddWithValue("offset",((long)request.Page-1)*request.PageSize);
        command.Parameters.AddWithValue("page_size",request.PageSize);
        using var result=JsonDocument.Parse((string)(await command.ExecuteScalarAsync(ct))!);
        if(!result.RootElement.GetProperty("allowed").GetBoolean())throw new ReportAccessDeniedException();
        var data=result.RootElement.GetProperty("data");
        return new(organization,data.GetProperty("generatedAt").GetDateTimeOffset(),zone.Id,
            data.GetProperty("tiles").Deserialize<StoresWorkloadTile[]>(Json)!,request,
            data.GetProperty("totalRows").GetInt64(),data.GetProperty("rows").Deserialize<StoresWorkloadRow[]>(Json)!);
    }
}
