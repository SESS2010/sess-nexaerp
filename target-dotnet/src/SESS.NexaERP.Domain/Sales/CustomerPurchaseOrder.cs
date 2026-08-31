using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Sales;

public static class CustomerPoWorkStatuses
{
    public const string NotCompleted = "Not Completed";
    public const string Wip = "W.I.P";
    public const string Completed = "Completed";

    public static readonly string[] All = [NotCompleted, Wip, Completed];
}

public static class CustomerPoServiceModes
{
    public const string NonAmc = "NON AMC";
    public const string UnderAmc = "Under AMC";
    public const string DispatchMachine = "Dispatch Machine";

    public static readonly string[] All = [NonAmc, UnderAmc, DispatchMachine];
}

public static class CustomerPoSalesTypes
{
    public const string Spares = "Spares";
    public const string ServiceCharges = "Service Charges";
    public const string Machine = "Machine";
    public const string AmcCharges = "AMC Charges";
    public const string SparesAndService = "Spares & Service";
    public const string CalibrationCharges = "Calibration Charges";

    public static readonly string[] All = [Spares, ServiceCharges, Machine, AmcCharges, SparesAndService, CalibrationCharges];
}

/// <summary>
/// Customer PO ledger entry. The sales flow starts here — offers are handled
/// outside the system, so a record is created only when a PO is received.
/// </summary>
public sealed class CustomerPurchaseOrder : AuditableEntity
{
    /// <summary>Internal record number (CPO-00001). Auto-generated, editable before save.</summary>
    public string PoRecordNumber { get; set; } = string.Empty;

    /// <summary>The customer's own PO number, kept verbatim (may be numeric or free text).</summary>
    public string CustomerPoNumber { get; set; } = string.Empty;
    public DateOnly? CustomerPoDate { get; set; }

    /// <summary>Our quotation reference, kept as free text (no offer module).</summary>
    public string? QuoteNumber { get; set; }
    public DateOnly? QuoteDate { get; set; }

    /// <summary>Mandatory shared customer-master identity. Free-text customer identity is forbidden.</summary>
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Which of our companies the PO belongs to (SESS / SESS PVT).</summary>
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string? ServiceMode { get; set; }
    public string? SalesType { get; set; }
    public string? Description { get; set; }
    public decimal? TotalAmountWithGst { get; set; }
    public string WorkStatus { get; set; } = CustomerPoWorkStatuses.NotCompleted;

    public string? PaymentTerms { get; set; }
    public string? ModeOfDelivery { get; set; }

    /// <summary>Financial year label the record belongs to, e.g. "2026-27".</summary>
    public string? FiscalYear { get; set; }
    public string? Remarks { get; set; }

    // PO document header references (Tally-style PO format).
    public string? ReferenceNumber { get; set; }
    public string? OtherReferences { get; set; }
    public string? Destination { get; set; }
    public string? DeliveryTerms { get; set; }

    // Commercial totals. When lines exist these are computed server-side;
    // TotalAmountWithGst stays the single grand-total column either way.
    public decimal? TaxableValue { get; set; }
    public decimal? CgstPercent { get; set; }
    public decimal? CgstAmount { get; set; }
    public decimal? SgstPercent { get; set; }
    public decimal? SgstAmount { get; set; }
    public decimal? IgstPercent { get; set; }
    public decimal? IgstAmount { get; set; }
    public decimal? RoundOff { get; set; }
    public string? AmountInWords { get; set; }

    /// <summary>Uploaded PO copy (customer_po_files.Id).</summary>
    public Guid? PoFileId { get; set; }
    public string? PoFileName { get; set; }

    /// <summary>The current immutable intake revision; starts at one and increases exactly once per change.</summary>
    public int CurrentRevisionNumber { get; set; } = 1;

    public List<CustomerPurchaseOrderLine> Lines { get; set; } = [];
    public List<CustomerPurchaseOrderRevision> Revisions { get; set; } = [];
}

/// <summary>One goods/services row of a customer PO (Sl No, description, qty, rate…).</summary>
public sealed class CustomerPurchaseOrderLine : AuditableEntity
{
    public Guid CustomerPurchaseOrderId { get; set; }
    public CustomerPurchaseOrder? CustomerPurchaseOrder { get; set; }
    public int RevisionNumber { get; set; }
    public CustomerPurchaseOrderRevision? Revision { get; set; }
    public int SlNo { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public decimal? Quantity { get; set; }
    public string? Uom { get; set; }
    public decimal? Rate { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? Amount { get; set; }
}

/// <summary>Append-only identity and canonical JSON snapshot for one intake revision.</summary>
public sealed class CustomerPurchaseOrderRevision : AuditableEntity
{
    public Guid CustomerPurchaseOrderId { get; set; }
    public CustomerPurchaseOrder? CustomerPurchaseOrder { get; set; }
    public int RevisionNumber { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = "{}";
    public List<CustomerPurchaseOrderLine> Lines { get; set; } = [];
}

public static class CustomerPoOptionKinds
{
    public const string ServiceMode = "SERVICE_MODE";
    public const string SalesType = "SALES_TYPE";

    public static readonly string[] All = [ServiceMode, SalesType];
}

/// <summary>User-extendable dropdown option for Customer PO (mode of service / sales type).</summary>
public sealed class CustomerPoOption : AuditableEntity
{
    public string Kind { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>Stored customer PO copy (PDF).</summary>
public sealed class CustomerPoFile : AuditableEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public byte[] Content { get; set; } = [];
}
