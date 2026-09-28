// Tracking-lite (R1). Wire shapes of GET /api/v1/tracking/pending, /summary and
// /{docType}/{documentId}/history. Mirrors
// target-dotnet/src/SESS.NexaERP.Application/Tracking/TrackingContracts.cs;
// sample payloads in target-dotnet/docs/installation/tracking-mocks/.

export type TrackingDocType =
  | 'PR'
  | 'RFQ'
  | 'QUOTATION'
  | 'COMPARISON'
  | 'PO'
  | 'GATE_ENTRY'
  | 'GRN'
  | 'QC'
  | 'MIR'
  | 'VENDOR_BILL'

/** One step of a document's timeline. Events come newest first. */
export interface TrackingEvent {
  At: string
  Stage: string
  Action: string
  FromStatus: string | null
  ToStatus: string | null
  EmployeeCode: string | null
  EmployeeName: string | null
  LoginId: string | null
  RoleCode: string | null
  Remarks: string | null
}

/** GET /api/v1/tracking/{docType}/{documentId}/history. A QC history takes the GRN id. */
export interface TrackingHistory {
  DocType: string
  DocumentId: string
  Number: string
  CurrentStatus: string
  PendingWithRole: string | null
  WaitingSince: string | null
  AgeDays: number | null
  IsOverdue: boolean | null
  Events: TrackingEvent[]
}

/** One open item on the Pending page. Overdue first, then oldest first. */
export interface TrackingPendingRow {
  DocType: string
  Queue: string
  DocumentId: string
  Number: string
  Status: string
  PendingWithRole: string | null
  PendingWithRoleName: string | null
  PendingWithEmployeeCode: string | null
  PendingWithEmployeeName: string | null
  WaitingSince: string
  AgeDays: number
  OverdueAfterDays: number
  IsOverdue: boolean
  Link: string
}

/** GET /api/v1/tracking/pending. */
export interface TrackingPendingPage {
  Total: number
  Page: number
  PageSize: number
  GeneratedAt: string
  TimeZone: string
  Items: TrackingPendingRow[]
}

/** GET /api/v1/tracking/summary. A tile with State "ACCESS_DENIED" has null counts. */
export interface TrackingSummaryTile {
  DocType: string
  Queue: string
  Title: string
  State: string
  Count: number | null
  OverdueCount: number | null
  OldestAgeDays: number | null
}
