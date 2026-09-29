namespace SESS.NexaERP.Application.Tracking;

/// <summary>Compatibility links for the R1 app. Identity remains DocType + DocumentId, never the display number.</summary>
public static class TrackingDocumentLinks
{
    public static string Document(string docType, Guid documentId, string number) => docType switch
    {
        "PR" => "/purchase/requisitions/" + Uri.EscapeDataString(number),
        "RFQ" => "/purchase/rfqs/" + Uri.EscapeDataString(number),
        "QUOTATION" => "/purchase/quotations",
        "COMPARISON" => "/purchase/comparisons/" + Uri.EscapeDataString(number),
        "PO" => "/purchase/purchase-orders/" + Uri.EscapeDataString(number),
        "GATE_ENTRY" => $"/stores/gate-entries/{documentId:D}",
        "GRN" => $"/stores/goods-receipts/{documentId:D}",
        // A QC tracking id is a GRN id, not an inspection or lot-allocation id.
        "QC" => "/qc/inspections",
        "MIR" => $"/stores/material-issue-requests/{documentId:D}",
        "VENDOR_BILL" => $"/accounts/vendor-bills/{documentId:D}",
        _ => "/tracking/pending"
    };

    public static string Queue(string queue) => "/tracking/pending?queue=" + Uri.EscapeDataString(queue);
}
