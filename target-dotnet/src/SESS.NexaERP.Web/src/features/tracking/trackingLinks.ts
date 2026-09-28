// Tracking-lite (R1): the app route for one pending row.
//
// The API's own `Link` field is NOT used. It points at routes this app does
// not have (/stores/grns instead of /stores/goods-receipts, /stores/qc, a
// quotation detail page) and it embeds document numbers that contain "/"
// (PO/SPVT/26-27/000012) without encoding them. Every mapping lives here so
// the fix, when the API's Link is corrected, is one function.
//
//   DocType      Row field used    App route (src/App.tsx)                          Detail page keys by
//   -----------  ----------------  -----------------------------------------------  ---------------------------
//   PR           Number            /purchase/requisitions/{Number}                   :prNumber (number)
//   RFQ          Number            /purchase/rfqs/{Number}                           :rfqNumber (number)
//   QUOTATION    (none)            /purchase/quotations                              list only; no detail route
//   COMPARISON   Number            /purchase/comparisons/{Number}                    :comparisonNumber (number)
//   PO           Number            /purchase/purchase-orders/{Number}                :poNumber (number)
//   GATE_ENTRY   DocumentId        /stores/gate-entries/{DocumentId}                 :id (guid)
//   GRN          DocumentId        /stores/goods-receipts/{DocumentId}               :id (guid)
//   QC           DocumentId        /stores/goods-receipts/{DocumentId}               :id (guid) — a qc-pending row's
//                                                                                    DocumentId IS the GRN id; the
//                                                                                    QC queue itself is /qc/inspections
//   MIR          DocumentId        /stores/material-issue-requests/{DocumentId}      :id (guid)
//   VENDOR_BILL  DocumentId        /accounts/vendor-bills/{DocumentId}               :id (guid)
//   (other)      Link              the API's Link when it is a path, else the Pending page
//
// Number-keyed routes take encodeURIComponent(Number) exactly as the app's
// own list pages do (PurchaseOrderListPage, RfqListPage, ...): a "/" inside
// the number becomes %2F, which the router keeps as one segment and
// useParams() decodes back.

import type { TrackingPendingRow } from '../../types/tracking'

export const PENDING_PAGE_PATH = '/tracking/pending'

export function trackingRowLink(row: TrackingPendingRow): string {
  const number = encodeURIComponent(row.Number)
  switch (row.DocType) {
    case 'PR':
      return `/purchase/requisitions/${number}`
    case 'RFQ':
      return `/purchase/rfqs/${number}`
    case 'QUOTATION':
      return '/purchase/quotations'
    case 'COMPARISON':
      return `/purchase/comparisons/${number}`
    case 'PO':
      return `/purchase/purchase-orders/${number}`
    case 'GATE_ENTRY':
      return `/stores/gate-entries/${row.DocumentId}`
    case 'GRN':
    case 'QC':
      return `/stores/goods-receipts/${row.DocumentId}`
    case 'MIR':
      return `/stores/material-issue-requests/${row.DocumentId}`
    case 'VENDOR_BILL':
      return `/accounts/vendor-bills/${row.DocumentId}`
    default:
      return row.Link && row.Link.startsWith('/') ? row.Link : PENDING_PAGE_PATH
  }
}
