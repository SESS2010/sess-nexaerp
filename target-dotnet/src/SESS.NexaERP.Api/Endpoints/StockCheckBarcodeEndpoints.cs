using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;
using Candidate = SESS.NexaERP.Api.Endpoints.GoodsReceiptBarcodeEndpoints.Candidate;

namespace SESS.NexaERP.Api.Endpoints;
public static class StockCheckBarcodeEndpoints
{
    public static IEndpointRouteBuilder MapStockCheckBarcodeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/stores/stock-check/requisitions/{prNumber}/scan-candidates",
            async (string prNumber, NexaErpDbContext db, ICurrentUser user, CancellationToken ct) =>
            {
                var number = prNumber.Trim().ToUpperInvariant();
                var pr = await PurchaseActorScope.StockRequisitions(db.PurchaseRequisitions.AsNoTracking(), db, user)
                    .Where(p => p.PrNumber == number && (p.Status == PurchaseRequisitionStatuses.StockCheckPending ||
                        p.Status == PurchaseRequisitionStatuses.FullyAvailable || p.Status == PurchaseRequisitionStatuses.PartiallyAvailable ||
                        p.Status == PurchaseRequisitionStatuses.NotAvailable))
                    .Select(p => new { p.Id, p.CompanyId, p.DeliveryWarehouseId }).SingleOrDefaultAsync(ct);
                if (pr is null) return Results.NotFound(new { message = "Purchase requisition not found in the stores stock-check scope." });
                var lines = await db.PurchaseRequisitionLines.AsNoTracking().Where(l => l.PurchaseRequisitionId == pr.Id && l.CompanyId == pr.CompanyId && l.ItemId.HasValue)
                    .Select(l => new Candidate(l.LineNumber.ToString(), l.ItemId!.Value, l.ItemCodeSnapshot,
                        l.Item == null ? null : l.Item.Barcode, null, null)).ToListAsync(ct);
                var itemIds = lines.Select(l => l.ItemId).ToArray();
                var serials = await db.StockMovements.AsNoTracking().Where(m => m.CompanyId == pr.CompanyId &&
                    m.WarehouseId == pr.DeliveryWarehouseId && itemIds.Contains(m.ItemId) && m.InventorySerialId.HasValue &&
                    m.ConditionCode == "AVAILABLE" && m.CustodyAssignment != null && m.CustodyAssignment.CustodyAccount != null &&
                    m.CustodyAssignment.CustodyAccount.CustodyType == "WAREHOUSE")
                    .GroupBy(m => new { m.ItemId, m.InventorySerialId, m.InventorySerial!.StoredSerialNumber })
                    .Where(g => g.Sum(m => m.QuantityIn - m.QuantityOut) > 0)
                    .Select(g => new { g.Key.ItemId, g.Key.InventorySerialId, g.Key.StoredSerialNumber }).ToListAsync(ct);
                var result = new List<Candidate>(lines);
                foreach (var line in lines)
                    foreach (var serial in serials.Where(s => s.ItemId == line.ItemId))
                        result.Add(line with { InventorySerialId = serial.InventorySerialId, StoredSerialNumber = serial.StoredSerialNumber });
                result.AddRange(await BarcodeStockLotCandidates.Read(db, user, pr.CompanyId, lines, pr.DeliveryWarehouseId, ct));
                return Results.Ok(result);
            }).RequireAuthorization().AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope)
            .RequirePagePermission("stores.stock-check", PagePermissionActions.Verify);
        return endpoints;
    }
}