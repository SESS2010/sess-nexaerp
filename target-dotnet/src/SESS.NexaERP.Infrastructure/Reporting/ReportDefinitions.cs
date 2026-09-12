using SESS.NexaERP.Application.Reporting;

namespace SESS.NexaERP.Infrastructure.Reporting;

internal sealed record ReportDefinition(string Key, string Title, bool UsesPeriod, bool Commercial,
    IReadOnlyList<ReportColumn> SummaryColumns, IReadOnlyList<ReportColumn> DetailColumns,
    IReadOnlyList<ReportColumn>? TotalDimensionColumns = null, string? Coverage = null, bool CurrentOnly = false)
{
    internal IReadOnlyList<ReportColumn> TotalColumns => [..(TotalDimensionColumns ?? [new("uom","Unit")]), ..SummaryColumns.Where(column => column.Type == "number")];
    internal string PageKey => "reports." + Key;
    internal ReportDescriptor Descriptor => new(Key, Title, UsesPeriod, Commercial, Coverage, CurrentOnly);
}

internal static class ReportDefinitions
{
    internal static readonly ReportColumn[] StockDimensions =
    [
        new("itemCode","Item code"), new("itemName","Item"), new("uom","Unit"),
        new("warehouse","Warehouse"), new("rackBin","Rack / bin"),
        new("lot","Lot"), new("serial","Serial"), new("ownership","Ownership"),
        new("condition","Condition"), new("custody","Custody")
    ];

    internal static readonly ReportColumn[] StockDetails =
    [
        new("movementId","Movement ID"), new("postingDate","Posting date","date"),
        .. StockDimensions,
        new("movementType","Movement"), new("referenceType","Document type"),
        new("referenceNumber","Document"), new("quantityIn","Quantity in","number"),
        new("quantityOut","Quantity out","number"), new("netQuantity","Net quantity","number"),
        new("openingStockLineId","Opening-stock line ID"), new("grnLineId","GRN line ID"),
        new("openingContribution","Opening contribution","number"),new("receiptContribution","Receipt contribution","number"),
        new("issueContribution","Issue contribution","number"),new("adjustmentContribution","Adjustment contribution","number"),
        new("openingStockContribution","Opening-stock contribution","number")
    ];

    internal static readonly ReportColumn[] FinancialDimensions =
    [
        new("vendorCode","Vendor code"),new("vendor","Vendor"),new("itemCode","Item code"),new("itemName","Item"),
        new("uom","Unit"),new("currency","Currency")
    ];
    internal static readonly ReportColumn[] FinancialTotals = [new("uom","Unit"),new("currency","Currency")];
    internal static readonly ReportColumn[] GrniMeasures =
    [
        new("quantity","Received, not invoiced","number","quantity"),
        new("receiptValue","Uninvoiced value at PO rate","number","receiptValue")
    ];
    internal static readonly ReportColumn[] VendorMeasures =
    [
        new("quantity","Net accepted quantity","number","quantity"),
        new("materialValue","Net accepted material value","number","materialValue"),
        new("allocatedCharges","Net allocated charges","number","allocatedCharges"),
        new("landedValue","Net accepted landed value","number","landedValue")
    ];
    internal static IReadOnlySet<string> FilterKeys(ReportDefinition definition) => definition.Key is "grni" or "vendor-purchases"
        ? new HashSet<string>(["vendorId","itemId","uom","currency"],StringComparer.Ordinal) : definition.Key == "purchase-register" ? new HashSet<string>(["prLineId","itemId","uom"],StringComparer.Ordinal)
        : definition.Key == "fifo-valuation" ? new HashSet<string>(["itemId","ownershipAccountId","ownership","uom","currency","ageBucket","costBasis"],StringComparer.Ordinal)
        : definition.Key == "pending-approvals" ? new HashSet<string>(["approverKey","uom"],StringComparer.Ordinal)
        : definition.Key == "engineer-custody" ? StockReportSql.FilterKeys.Concat(new[] { "engineerId","engineerCode","engineerName" }).ToHashSet(StringComparer.Ordinal) : StockReportSql.FilterKeys;

    internal static readonly ReportColumn[] ApprovalMeasures =
    [
        new("pendingActions","Pending actions","number","pendingActions"),new("age0to1","0–1 days","number","age0to1"),
        new("age2to7","2–7 days","number","age2to7"),new("age8to30","8–30 days","number","age8to30"),
        new("ageOver30","Over 30 days","number","ageOver30")
    ];
    internal static readonly ReportColumn[] ApproverDimensions =
    [new("approverCode","Approver code"),new("approverName","Approver"),new("role","Role"),new("assignmentKind","Assignment")];

    internal static readonly IReadOnlyList<ReportDefinition> All =
    [
        new("fifo-valuation","FIFO valuation and ageing",false,true,
            [new("itemCode","Item code"),new("itemName","Item"),new("ownership","Ownership"),new("uom","Unit"),
             new("currency","Currency"),new("ageBucket","Age"),new("costBasis","Cost basis"),
             new("quantity","Remaining accounting quantity","number","quantity"),new("value","FIFO value","number","value")],
            [new("layerId","FIFO layer ID"),new("itemCode","Item code"),new("itemName","Item"),new("ownership","Ownership"),
             new("uom","Unit"),new("currency","Currency"),new("receivedAt","Layer received"),new("ageDays","Age in days","number"),
             new("ageBucket","Age"),new("costBasis","Cost basis"),new("receivedQuantity","Original quantity","number"),
             new("consumedQuantity","Consumed quantity","number"),new("quantity","Remaining accounting quantity","number"),
             new("unitCost","Effective unit cost","number"),new("value","FIFO value","number"),
             new("grnNumber","GRN"),new("grnLineId","GRN line ID"),new("openingLine","Opening line"),
             new("openingStockLineId","Opening-stock line ID"),new("acceptedBillLineId","Accepted bill line ID")],
            [new("ownership","Ownership"),new("uom","Unit"),new("currency","Currency")],
            Coverage:"Accounting FIFO layers, including OPENING_LANDED, by recorded currency and ownership. Age uses the original layer receipt date. Physical serial selection does not select a cost layer. No currency conversion. Valuation is refused while accepted returns lack FIFO credits."),
        new("pending-approvals","Pending approvals",false,false,
            [..ApproverDimensions,..ApprovalMeasures],
            [..ApproverDimensions,new("documentType","Document type"),new("document","Document"),new("documentId","Document ID"),
             new("status","Status"),new("sourceScope","Source scope"),new("step","Step","number"),
             new("waitingSince","Waiting since"),new("ageDays","Age in days","number"),new("responsibility","Required action"),..ApprovalMeasures],
            Coverage:"Current selected-company queues and shared item, customer and vendor masters. Named workflow approvers and role pools are distinguished. Counts are actions, not distinct documents. Age starts at the last pending update or recorded submission. Directors see the overview; other approvers see their queue.",
            CurrentOnly:true),
        new("stock-balance","Stock balance",false,false,
            [..StockDimensions,new("quantity","Balance","number","closing")],StockDetails),
        new("movement-roll-forward","Movement roll-forward",true,false,
            [..StockDimensions,new("opening","Opening","number","opening"),
             new("receipts","Receipts","number","receipts"),new("issues","Issues","number","issues"),
             new("adjustments","Adjustments / transfers / returns","number","adjustments"),
             new("openingIntroduced","Opening stock introduced in period","number","opening-stock"),
             new("closing","Closing","number","closing")],StockDetails),
        new("grni","Goods received, not invoiced",false,true,
            [..FinancialDimensions,..GrniMeasures],
            [new("grnLineId","GRN line ID"),new("grnNumber","GRN"),new("receivedDate","Received date","date"),
             new("poNumber","PO"),..FinancialDimensions,new("daysUninvoiced","Age in days","number"),
             new("receivedQuantity","Received quantity","number"),new("acceptedBilledQuantity","Accepted billed quantity","number"),
             new("unitRate","PO unit rate","number"),..GrniMeasures],FinancialTotals),
        new("vendor-purchases","Vendor purchase summary",true,true,
            [..FinancialDimensions,..VendorMeasures],
            [new("billLineId","Bill line ID"),new("billNumber","Bill"),new("billDate","Bill date","date"),
             new("event","Event"),new("eventDate","Event date","date"),new("grnNumber","GRN"),new("poNumber","PO"),
             ..FinancialDimensions,..VendorMeasures],FinancialTotals),
        new("engineer-custody","Custody by engineer",false,false,
            [new("engineerCode","Engineer code"),new("engineerName","Engineer"),..StockDimensions,
             new("quantity","Quantity in custody","number","quantity")],
            [new("movementId","Movement ID"),new("postingDate","Posting date","date"),
             new("engineerCode","Engineer code"),new("engineerName","Engineer"),..StockDimensions,
             new("movementType","Movement"),new("referenceType","Document type"),new("referenceNumber","Document"),
             new("quantityIn","Quantity in","number"),new("quantityOut","Quantity out","number"),new("quantity","Net quantity","number"),
             new("issueLineId","Issue line ID"),new("returnLineId","Return line ID"),new("fitmentId","Fitment ID")],
            [new("engineerCode","Engineer code"),new("engineerName","Engineer"),new("uom","Unit")]),
        new("purchase-register","PR to PO to GRN to bill register",false,false,
            [new("prNumber","PR"),new("prLineNumber","PR line"),new("prStatus","Current PR status"),
             new("itemCode","Item code"),new("itemName","Item"),new("uom","Recorded unit"),
             new("requested","Requested","number","requested"),new("ordered","Issued PO quantity","number","ordered"),
             new("received","Net finalized receipt","number","received"),new("billed","Net accepted bill quantity","number","billed")],
            [new("prNumber","PR"),new("prLineNumber","PR line"),new("stage","Stage"),new("document","Document"),
             new("status","Current status"),new("eventDate","Event date","date"),new("vendor","Vendor"),
             new("itemCode","Item code"),new("itemName","Item"),new("uom","Recorded unit"),
             new("requested","Requested contribution","number"),new("ordered","Ordered contribution","number"),
             new("received","Received contribution","number"),new("billed","Billed contribution","number"),
             new("documentId","Document ID"),new("prLineId","PR line ID"),new("poLineId","PO line ID"),
             new("grnLineId","GRN line ID"),new("billLineId","Bill line ID")])
    ];

    internal static ReportDefinition Find(string key) =>
        All.SingleOrDefault(report => string.Equals(report.Key,key,StringComparison.Ordinal))
        ?? throw new KeyNotFoundException("Report was not found.");
}
