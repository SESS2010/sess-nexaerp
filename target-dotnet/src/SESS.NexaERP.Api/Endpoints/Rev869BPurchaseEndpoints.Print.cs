using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

/// <summary>
/// R7 (approved by name 26 Sep): the data for the printed purchase order. The layout is the frontend's;
/// every figure comes from the snapshots taken when the PO was approved, never recalculated here.
/// Only an issued, closed or cancelled PO prints (a cancelled one says so), each print is audited.
/// </summary>
public static partial class Rev869BPurchaseEndpoints
{
    private static readonly JsonSerializerOptions PrintSnapshotJson = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public sealed record PoPrintParty(string Code, string Name, string? LegalName, string? Gstin, string? Pan, string? Address,
        string? State, string? StateCode, string? ContactPerson, string? Phone, string? Email);
    public sealed record PoPrintLine(int LineNumber, string ItemCode, string ItemName, string HsnSacCode, string Uom, decimal Quantity,
        decimal UnitRate, decimal DiscountValue, decimal HeaderDiscountValue, decimal PackingForwarding, decimal Freight, decimal Insurance,
        decimal OtherCharges, decimal TaxableValue, decimal CgstRate, decimal CgstValue, decimal SgstRate, decimal SgstValue,
        decimal IgstRate, decimal IgstValue, decimal CessRate, decimal CessValue, decimal RoundOff, decimal LineTotal, DateOnly? PromisedDeliveryDate);
    public sealed record PoPrintTotals(decimal TaxableValue, decimal CgstValue, decimal SgstValue, decimal IgstValue, decimal CessValue,
        decimal Charges, decimal RoundOff, decimal TotalPayableValue, string AmountInWords);
    public sealed record PoPrintEvent(string Action, string ToStatus, string EmployeeCode, string EmployeeName, string RoleCode, DateTimeOffset At, string Remarks);
    public sealed record PoPrintView(CompanyPrintHeader Company, string PoNumber, int RevisionNumber, string Status, bool IsCancelled,
        DateTimeOffset? IssuedAt, DateTimeOffset? CancelledAt, string? CancellationReason, string? AmendmentReason, string CurrencyCode,
        PoPrintParty Vendor, string? DeliveryWarehouseCode, string? DeliveryWarehouseName, string? DeliveryLocation,
        string SupplierStateCode, string PlaceOfSupplyStateCode, string SupplyType,
        IReadOnlyList<PoPrintLine> Lines, PoPrintTotals Totals, string PaymentTerms, string DeliveryTerms, string WarrantyTerms,
        string ApprovalRoute, IReadOnlyList<PoPrintEvent> History, DateTimeOffset PrintedAt, string PrintedBy);

    internal static readonly string[] PrintablePoStatuses = [Rev869BStatuses.Issued, Rev869BStatuses.Closed, Rev869BStatuses.Cancelled];

    private static async Task<IResult> PrintPo(string number, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes,
        IPagePermissionService permissions, IAuditWriter audit, CancellationToken ct)
    {
        var row = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines).Include(x => x.Vendor).Include(x => x.DeliveryWarehouse)
            .SingleOrDefaultAsync(x => x.OrganizationId == user.OrganizationId && x.PoNumber == number.Trim().ToUpper() && x.IsCurrentVersion, ct);
        if (row is null) return await Missing(audit, "purchase.po", number, user, ct);
        if (!await Allowed(user, scopes, row.OrganizationId, row.RequestingDepartmentId, row.DeliveryWarehouseId, row.OwnerEmployeeId, ct)) return await Denied(audit, "purchase.po", number, user, ct);
        if (!await permissions.HasPermissionAsync(user.RoleCodes, "purchase.po", PagePermissionActions.ViewCommercialValues, ct))
            return await Denied(audit, "purchase.po", number, user, ct);
        if (!PrintablePoStatuses.Contains(row.Status))
            throw new StoresConflictException($"Purchase order {row.PoNumber} is {row.Status}. Only an issued, closed or cancelled purchase order can be printed.");
        var company = await CompanyPrintBlock.ReadAsync(db, row.CompanyId, ct);
        var quotationLineIds = new List<Guid>();
        var lines = new List<(PurchaseOrderLine Line, Rev869BPoCommercialSnapshot Commercial, Rev869BTaxRuleSnapshot Tax)>();
        foreach (var line in row.Lines.OrderBy(x => x.LineNumber))
        {
            var commercial = JsonSerializer.Deserialize<Rev869BPoCommercialSnapshot>(line.CommercialSnapshotJson, PrintSnapshotJson)!;
            var tax = JsonSerializer.Deserialize<Rev869BTaxRuleSnapshot>(line.TaxRuleSnapshotJson, PrintSnapshotJson)!;
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
        await audit.WriteAsync("Purchase", "PrintPurchaseOrder", nameof(PurchaseOrder), row.Id.ToString(), null,
            new { row.PoNumber, row.RevisionNumber, row.Status, row.TotalPayableValue }, ct);
        return Results.Ok(new PoPrintView(company, row.PoNumber, row.RevisionNumber, row.Status, row.Status == Rev869BStatuses.Cancelled,
            row.IssuedAt, row.CancelledAt, row.CancellationReason, row.AmendmentReason, row.CurrencyCode,
            new PoPrintParty(vendor.VendorCode, vendor.Name, vendor.LegalVendorName, vendor.GstNumber, vendor.PanNumber, vendor.BillingAddress,
                vendor.State, vendor.StateCode, vendor.ContactPerson, vendor.Phone, vendor.Email),
            row.DeliveryWarehouse?.WarehouseCode, row.DeliveryWarehouse?.Name, row.DeliveryWarehouse?.Location,
            first.SupplierStateCode, first.PlaceOfSupplyStateCode, first.SupplierStateCode == first.PlaceOfSupplyStateCode ? "INTRASTATE" : "INTERSTATE",
            printLines, totals, row.PaymentTermsSnapshot, row.DeliveryTermsSnapshot, row.WarrantyTermsSnapshot, row.ApprovalRoute, history,
            DateTimeOffset.UtcNow, user.LoginId));
    }
}
