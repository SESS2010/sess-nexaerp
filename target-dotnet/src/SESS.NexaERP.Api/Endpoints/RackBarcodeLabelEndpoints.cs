using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;
public static class RackBarcodeLabelEndpoints
{
    public sealed record RackLabel(string BinCode, string RackName, string WarehouseCode, string? Barcode);
    public static IEndpointRouteBuilder MapRackBarcodeLabelEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/stores/barcodes/rack-labels", async (NexaErpDbContext db, ICurrentUser user, HttpContext context, CancellationToken ct) =>
        {
            var scopes = BarcodeScope.Active(db, user, context);
            var rows = await db.RackBins.AsNoTracking().Where(r => r.IsActive && r.ApprovalStatus == "Approved" && r.Warehouse != null && r.Warehouse.IsActive &&
                db.Companies.Any(c => c.Id == r.CompanyId && c.Code == user.OrganizationId && c.IsActive) &&
                scopes.Any(s => s.CompanyId == r.CompanyId && !s.OwnRecordsOnly &&
                    (!s.WarehouseId.HasValue || s.WarehouseId == r.WarehouseId) && (!s.RackBinId.HasValue || s.RackBinId == r.Id)))
                .OrderBy(r => r.Warehouse!.WarehouseCode).ThenBy(r => r.BinCode)
                .Select(r => new RackLabel(r.BinCode, r.RackName, r.Warehouse!.WarehouseCode, r.Barcode)).ToListAsync(ct);
            return Results.Ok(rows);
        }).WithMetadata(new BarcodeBusinessDate()).RequireAuthorization().AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope)
            .RequirePagePermission("masters.rack-bins", PagePermissionActions.View)
            .RequirePagePermission("masters.rack-bins", PagePermissionActions.Print);
        return endpoints;
    }
}