// Tracking-lite (R1): the doc-type order the screens group by, and the sixteen
// queues as the API names them. The live list comes from
// GET /api/v1/tracking/summary; this static copy is the fallback when that
// call fails (a server without R1, or a user the summary refuses), so the
// Pending page's queue filter still works. Mirrors
// target-dotnet/docs/installation/tracking-mocks/summary.json. Nothing here is
// a mock payload: no counts, no dates.

import type { TrackingDocType } from '../../types/tracking'

/** Grouping order for tiles and the queue filter: the document flow, PR to bill. */
export const TRACKING_DOC_TYPE_ORDER: TrackingDocType[] = [
  'PR',
  'RFQ',
  'QUOTATION',
  'COMPARISON',
  'PO',
  'GATE_ENTRY',
  'GRN',
  'QC',
  'MIR',
  'VENDOR_BILL',
]

export interface TrackingQueueInfo {
  DocType: TrackingDocType
  Queue: string
  Title: string
}

/** The sixteen queues, in summary order. Titles are the API's own tile titles. */
export const TRACKING_QUEUES: TrackingQueueInfo[] = [
  { DocType: 'PR', Queue: 'pr-department-verification', Title: 'Requisitions awaiting department verification' },
  { DocType: 'PR', Queue: 'pr-approval', Title: 'Requisitions awaiting approval' },
  { DocType: 'PR', Queue: 'pr-stock-check', Title: 'Requisitions awaiting stock check' },
  { DocType: 'RFQ', Queue: 'rfq-no-quotation', Title: 'RFQs issued with no quotation' },
  { DocType: 'QUOTATION', Queue: 'quotation-technical-verification', Title: 'Quotations awaiting technical verification' },
  { DocType: 'COMPARISON', Queue: 'comparison-decision', Title: 'Comparisons awaiting recommendation or approval' },
  { DocType: 'PO', Queue: 'po-pending-approval', Title: 'POs waiting for approval' },
  { DocType: 'PO', Queue: 'po-approved-unissued', Title: 'POs approved but not issued' },
  { DocType: 'PO', Queue: 'po-delivery-overdue', Title: 'POs past the promised delivery date' },
  { DocType: 'GATE_ENTRY', Queue: 'gate-no-grn', Title: 'Gate entries awaiting GRN' },
  { DocType: 'GRN', Queue: 'grn-not-finalised', Title: 'GRNs not finalised' },
  { DocType: 'QC', Queue: 'qc-pending', Title: 'Receipts awaiting QC' },
  { DocType: 'MIR', Queue: 'mir-approval', Title: 'MIRs awaiting approval' },
  { DocType: 'MIR', Queue: 'mir-unissued', Title: 'Approved MIRs awaiting issue' },
  { DocType: 'VENDOR_BILL', Queue: 'bill-awaiting-decision', Title: 'Vendor bills awaiting accept or reject' },
  { DocType: 'GRN', Queue: 'grn-without-bill', Title: 'Receipts with no vendor bill' },
]

/** Position of a doc type in the flow; unknown types sort last. */
export function docTypeRank(docType: string): number {
  const index = TRACKING_DOC_TYPE_ORDER.indexOf(docType as TrackingDocType)
  return index === -1 ? TRACKING_DOC_TYPE_ORDER.length : index
}

/** Groups queues by doc type in flow order, keeping each group's own order. */
export function groupQueuesByDocType<T extends { DocType: string }>(queues: T[]): { docType: string; queues: T[] }[] {
  const groups = new Map<string, T[]>()
  for (const queue of [...queues].sort((a, b) => docTypeRank(a.DocType) - docTypeRank(b.DocType))) {
    const list = groups.get(queue.DocType)
    if (list) list.push(queue)
    else groups.set(queue.DocType, [queue])
  }
  return [...groups.entries()].map(([docType, list]) => ({ docType, queues: list }))
}

/** The Pending page filtered to one queue: what a home tile links to. */
export function pendingQueuePath(queue: string): string {
  const params = new URLSearchParams({ queue })
  return `/tracking/pending?${params.toString()}`
}
