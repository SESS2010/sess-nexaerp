using SESS.NexaERP.Api.Security;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
using Candidate = SESS.NexaERP.Api.Endpoints.GoodsReceiptBarcodeEndpoints.Candidate;

namespace SESS.NexaERP.Api.Endpoints;
internal static class BarcodeStockLotCandidates
{
    // Lot labels identify the item. Existing issue commands retain FIFO allocation authority.
    internal static async Task<List<Candidate>> Read(NexaErpDbContext db, ICurrentUser user, Guid companyId,
        IReadOnlyList<Candidate> lines, Guid? deliveryWarehouseId, CancellationToken ct)
    {
        var items = lines.Where(l => !l.InventorySerialId.HasValue && !l.RequiresSerial).Select(l => l.ItemId).Distinct().ToArray();
        var scopes = await BarcodeScope.Active(db, user, ct);
        var available = db.StockMovements.AsNoTracking().Where(m => m.CompanyId == companyId && items.Contains(m.ItemId) &&
            m.InventoryLotId.HasValue && m.ConditionCode == "AVAILABLE" && (!deliveryWarehouseId.HasValue || m.WarehouseId == deliveryWarehouseId) &&
            m.CustodyAssignment != null && m.CustodyAssignment.CustodyAccount != null && m.CustodyAssignment.CustodyAccount.CustodyType == "WAREHOUSE" &&
            scopes.Any(s => s.CompanyId == companyId && (!s.WarehouseId.HasValue || s.WarehouseId == m.WarehouseId) &&
                (!s.RackBinId.HasValue || s.RackBinId == m.RackBinId)))
            .GroupBy(m => new { m.ItemId, m.InventoryLotId }).Where(g => g.Sum(m => m.QuantityIn - m.QuantityOut) > 0)
            .Select(g => g.Key.InventoryLotId!.Value);
        var allocations = await db.GoodsReceiptLineLotAllocations.AsNoTracking().Where(a => a.CompanyId == companyId &&
            available.Contains(a.InventoryLotId) && a.GoodsReceiptLine != null && a.GoodsReceiptLine.GoodsReceipt != null &&
            a.GoodsReceiptLine.CompanyId == companyId && a.GoodsReceiptLine.GoodsReceipt.CompanyId == companyId)
            .Select(a => new { a.GoodsReceiptLine!.ItemId, a.GoodsReceiptLine.LineNumber, a.LotOrdinal,
                a.GoodsReceiptLine.GoodsReceipt!.GrnNumber }).ToListAsync(ct);
        var result = new List<Candidate>();
        foreach (var line in lines.Where(l => !l.InventorySerialId.HasValue && !l.RequiresSerial))
            foreach (var allocation in allocations.Where(a => a.ItemId == line.ItemId))
                result.Add(line with { Barcode = GrnLotBarcode.Create(user.OrganizationId!, allocation.GrnNumber, allocation.LineNumber, allocation.LotOrdinal) });
        return result;
    }
}