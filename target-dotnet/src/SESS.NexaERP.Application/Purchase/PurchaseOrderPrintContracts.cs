using SESS.NexaERP.Application.Masters;

namespace SESS.NexaERP.Application.Purchase;

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

/// <summary>
/// The data of a printed PO, from the snapshots approved on it (never recalculated). No permission check and
/// no audit row: the print endpoint checks and audits; the PO_ISSUED e-mail composer (email-lite) reads it for
/// the PO the system itself just issued. 409 via StoresConflictException without a company profile.
/// </summary>
public interface IPurchaseOrderPrintQuery
{
    Task<PoPrintView> GetAsync(Guid purchaseOrderId, CancellationToken ct);
}
