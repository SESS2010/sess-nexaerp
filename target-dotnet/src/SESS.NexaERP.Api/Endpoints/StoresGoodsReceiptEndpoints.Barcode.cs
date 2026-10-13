using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Api.Security;

namespace SESS.NexaERP.Api.Endpoints;

public static class GoodsReceiptBarcodeEndpoints
{
    // Metadata only for lines already present in this receipt context. No prices or generic serial search.
    public sealed record Candidate(string LineKey, Guid ItemId, string ItemCode, string? Barcode,
        Guid? InventorySerialId, string? StoredSerialNumber, bool RequiresSerial = false);

    public static IEndpointRouteBuilder MapGoodsReceiptBarcodeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/stores/goods-receipts/gates/{gateId:guid}/scan-candidates",
            async (Guid gateId, NexaErpDbContext db, ICurrentUser user, CancellationToken ct) =>
            {
                if (!BarcodeScope.RequireStores(user, "view"))
                    return Results.Forbid();
                var scopes = await BarcodeScope.Active(db, user, ct);
                var gate = await db.GateEntries.AsNoTracking()
                    .Where(g => g.Id == gateId && db.Companies.Any(c => c.Id == g.CompanyId && c.Code == user.OrganizationId && c.IsActive && c.Status == "ACTIVE") &&
                        g.PurchaseOrder != null && scopes.Any(s => s.CompanyId == g.CompanyId &&
                            (!s.WarehouseId.HasValue || s.WarehouseId == g.PurchaseOrder.DeliveryWarehouseId) &&
                            !s.RackBinId.HasValue && (!s.OwnRecordsOnly || g.ReceivedByEmployeeId == user.EmployeeId)))
                    .Select(g => new { g.Id, g.CompanyId, g.PurchaseOrder!.DeliveryWarehouseId }).SingleOrDefaultAsync(ct);
                if (gate is null) return Results.NotFound(new { message = "Gate entry not found in the current receipt scope." });
                var lines = await db.GateEntryLines.AsNoTracking().Where(l => l.GateEntryId == gate.Id && l.CompanyId == gate.CompanyId)
                    .Select(l => new Candidate(l.Id.ToString(), l.ItemId, l.ItemCodeSnapshot,
                        l.Item == null ? null : l.Item.Barcode, null, null)).ToListAsync(ct);
                // Only serials already captured on this draft receipt identify a line. A new vendor serial
                // cannot safely infer an item from unrelated stock; the operator selects the item then captures it.
                var serials = await db.GoodsReceiptLineSerials.AsNoTracking().Where(serial => serial.CompanyId == gate.CompanyId &&
                    serial.GoodsReceiptLine != null && serial.GoodsReceiptLine.CompanyId == gate.CompanyId &&
                    serial.GoodsReceiptLine.GoodsReceipt != null && serial.GoodsReceiptLine.GoodsReceipt.CompanyId == gate.CompanyId &&
                    serial.GoodsReceiptLine.GoodsReceipt.GateEntryId == gate.Id && serial.GoodsReceiptLine.GoodsReceipt.Status == "DRAFT")
                    .Select(serial => new { serial.GoodsReceiptLine!.GateEntryLineId, InventorySerialId = serial.InventorySerialId ?? serial.Id, StoredSerialNumber = serial.StoredSerialNumberSnapshot }).ToListAsync(ct);
                var result = new List<Candidate>(lines);
                foreach (var line in lines)
                    foreach (var serial in serials.Where(serial => serial.GateEntryLineId.ToString() == line.LineKey))
                        result.Add(line with { InventorySerialId = serial.InventorySerialId, StoredSerialNumber = serial.StoredSerialNumber });
                result.AddRange(await BarcodeStockLotCandidates.Read(db, user, gate.CompanyId, lines, gate.DeliveryWarehouseId, ct));
                return Results.Ok(result);
            }).RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope)
            .RequirePagePermission("inventory.grn", PagePermissionActions.View);
        return endpoints;
    }
}