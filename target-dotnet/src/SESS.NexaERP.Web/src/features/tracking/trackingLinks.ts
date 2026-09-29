// Tracking-lite (R1): the app route for one pending row.
//
// Contract: target-dotnet/docs/installation/R1-tracking-lite-frontend-contract.md
// (updated 29 September 2026, G-13 to G-17). Every document carries its
// existing UUID (DocumentId) plus its display Number; (DocType, DocumentId)
// is the identity. The API's own `Link` field is NOT used, for any doc type:
// every destination is built here from DocType + DocumentId / Number.
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
//   QC           (none)            /qc/inspections                                   the QC queue (G-14). A
//                                                                                    qc-pending row's DocumentId
//                                                                                    is the GRN id, kept for
//                                                                                    history; the queue has no
//                                                                                    supported GRN filter, so
//                                                                                    none is invented
//   MIR          DocumentId        /stores/material-issue-requests/{DocumentId}      :id (guid)
//   VENDOR_BILL  DocumentId        /accounts/vendor-bills/{DocumentId}               :id (guid)
//   (other)      (none)            the Pending page                                  never the API's Link
//
// Number-keyed routes take encodeURIComponent(Number) exactly once, as the
// app's own list pages do (PurchaseOrderListPage, RfqListPage, ...): a "/"
// inside the number becomes %2F, which the router keeps as one segment and
// useParams() decodes back. The raw number is never split into segments and
// never replaces the UUID.

import type { TrackingPendingRow } from '../../types/tracking'

export const PENDING_PAGE_PATH = '/tracking/pending'

/** The QC queue: where a QC pending row opens (G-14). */
export const QC_QUEUE_PATH = '/qc/inspections'

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
      return `/stores/goods-receipts/${row.DocumentId}`
    case 'QC':
      return QC_QUEUE_PATH
    case 'MIR':
      return `/stores/material-issue-requests/${row.DocumentId}`
    case 'VENDOR_BILL':
      return `/accounts/vendor-bills/${row.DocumentId}`
    default:
      return PENDING_PAGE_PATH
  }
}
