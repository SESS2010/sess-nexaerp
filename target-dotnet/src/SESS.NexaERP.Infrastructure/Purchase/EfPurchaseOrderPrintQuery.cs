using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Purchase;

/// <summary>R7 print data (moved from the print endpoint on 27 Sep so the PO_ISSUED e-mail uses the same figures).</summary>
public sealed class EfPurchaseOrderPrintQuery(NexaErpDbContext db) : IPurchaseOrderPrintQuery
{
    private static readonly JsonSerializerOptions SnapshotJson = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public async Task<PoPrintView> GetAsync(Guid purchaseOrderId, CancellationToken ct)
    {
        var row = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines).Include(x => x.Vendor).Include(x => x.DeliveryWarehouse)
            .SingleOrDefaultAsync(x => x.Id == purchaseOrderId, ct) ?? throw new KeyNotFoundException("Purchase order was not found.");
        var company = await CompanyPrintBlock.ReadAsync(db, row.CompanyId, ct);
        var quotationLineIds = new List<Guid>();
        var lines = new List<(PurchaseOrderLine Line, Rev869BPoCommercialSnapshot Commercial, Rev869BTaxRuleSnapshot Tax)>();
        foreach (var line in row.Lines.OrderBy(x => x.LineNumber))
        {
            var commercial = JsonSerializer.Deserialize<Rev869BPoCommercialSnapshot>(line.CommercialSnapshotJson, SnapshotJson)!;
            var tax = JsonSerializer.Deserialize<Rev869BTaxRuleSnapshot>(line.TaxRuleSnapshotJson, SnapshotJson)!;
            lines.Add((line, commercial, tax)); quotationLineIds.Add(commercial.VendorQuotationLineId);
        }
        var promised = await db.VendorQuotationLines.AsNoTracking().Where(x => quotationLineIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => (DateOnly?)x.PromisedDeliveryDate, ct);
        var printLines = lines.Select(x => new PoPrintLine(x.Line.LineNumber, x.Line.ItemCodeSnapshot, x.Line.ItemNameSnapshot, x.Tax.HsnSacCode,
            x.Line.UomSnapshot, x.Line.OrderedQuantity, x.Line.UnitRate, x.Commercial.Result.DiscountValue, x.Commercial.Result.HeaderDiscountValue,
            x.Commercial.Result.PackingForwarding, x.Commercial.Result.Freight, x.Commercial.Result.Insurance, x.Commercial.Result.OtherCharges,
            x.Commercial.Result.TaxableValue, x.Tax.CgstRate, x.Commercial.Result.CgstValue, x.Tax.SgstRate, x.Commercial.Result.SgstValue,
            x.Tax.IgstRate, x.Commercial.Result.IgstValue, x.Tax.CessRate, x.Commercial.Result.CessValue, x.Commercial.Result.RoundOff,
            x.Line.TotalPayableValue, promised.GetValueOrDefault(x.Commercial.VendorQuotationLineId))).ToArray();
        var totals = new PoPrintTotals(printLines.Sum(x => x.TaxableValue), printLines.Sum(x => x.CgstValue), printLines.Sum(x => x.SgstValue),
            printLines.Sum(x => x.IgstValue), printLines.Sum(x => x.CessValue), printLines.Sum(x => x.PackingForwarding + x.Freight + x.Insurance + x.OtherCharges),
            printLines.Sum(x => x.RoundOff), row.TotalPayableValue, AmountInWords.Rupees(row.TotalPayableValue));
        var history = await db.PurchaseTransactionStatusHistories.AsNoTracking()
            .Where(x => x.EntityType == "PurchaseOrder" && x.EntityId == row.Id && x.Action != "ReserveAmendment")
            .OrderBy(x => x.CreatedAt)
            .Select(x => new PoPrintEvent(x.Action, x.ToStatus, x.ActorEmployee!.EmployeeCode, x.ActorEmployee.EmployeeName, x.ActorRoleCode, x.CreatedAt, x.Remarks))
            .ToListAsync(ct);
        var vendor = row.Vendor!;
        var first = lines.First().Tax;
        return new PoPrintView(company, row.PoNumber, row.RevisionNumber, row.Status, row.Status == Rev869BStatuses.Cancelled,
            row.IssuedAt, row.CancelledAt, row.CancellationReason, row.AmendmentReason, row.CurrencyCode,
            new PoPrintParty(vendor.VendorCode, vendor.Name, vendor.LegalVendorName, vendor.GstNumber, vendor.PanNumber, vendor.BillingAddress,
                vendor.State, vendor.StateCode, vendor.ContactPerson, vendor.Phone, vendor.Email),
            row.DeliveryWarehouse?.WarehouseCode, row.DeliveryWarehouse?.Name, row.DeliveryWarehouse?.Location,
            first.SupplierStateCode, first.PlaceOfSupplyStateCode, first.SupplierStateCode == first.PlaceOfSupplyStateCode ? "INTRASTATE" : "INTERSTATE",
            printLines, totals, row.PaymentTermsSnapshot, row.DeliveryTermsSnapshot, row.WarrantyTermsSnapshot, row.ApprovalRoute, history,
            DateTimeOffset.UtcNow, "");
    }
}
