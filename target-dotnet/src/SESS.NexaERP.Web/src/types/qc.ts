// Mirrors SESS.NexaERP.Domain/Stores/StoresPart3A.cs (QcInspection, QcInspectionRevision,
// QcInspectionParameterResult, QcInspectionSerialDisposition) and the planned QC contract
// in outputs/sess_api_contract.md §11.3/§13.3.
//
// The QC backend (Slice 3) is NOT live yet — these routes 404 today. The schema and
// domain entities exist, so the shapes below are grounded in real code; the API adapter
// in api/qc.ts falls back to a bannered mock until the endpoints ship.

/** A revision is editable while DRAFT; finalize-and-post makes it FINAL and moves stock. */
export const QC_REVISION_STATES = ['DRAFT', 'FINAL'] as const

export const QC_DECISIONS = ['ACCEPTED', 'PARTIALLY_ACCEPTED', 'REJECTED'] as const
export type QcDecision = (typeof QC_DECISIONS)[number]

export const SERIAL_DISPOSITIONS = ['ACCEPTED', 'REJECTED'] as const
export type SerialDispositionValue = (typeof SERIAL_DISPOSITIONS)[number]

export interface QcQueueItem {
  InspectionId: string | null
  SourceType: 'GRN' | 'DC_RETURN'
  SourceNumber: string
  GoodsReceiptLineId: string
  ItemCode: string
  ItemName: string
  CategoryCode: string
  Quantity: number
  QcRackCode: string
  ReceivedAt: string
  QcDueAt: string
  AgeHours: number
  IsOverdue: boolean
}

export interface QcParameterResult {
  QcInspectionPolicyId: string
  ParameterCode: string
  MeasurementUomCode: string
  LowerLimit: number | null
  UpperLimit: number | null
  InspectionMethod: string
  RequiredSampleSize: number
  SampleOrdinal: number
  ObservedNumericValue: number | null
  ObservedTextValue: string | null
  Result: 'PASS' | 'FAIL' | ''
  Remarks: string | null
}

export interface QcSerialDisposition {
  InventorySerialId: string
  StoredSerialNumber: string
  Disposition: SerialDispositionValue | ''
  Reason: string | null
}

export interface QcRevision {
  Id: string
  RevisionNumber: number
  RevisionKind: 'INITIAL' | 'CORRECTION'
  RevisesRevisionId: string | null
  CorrectionReason: string | null
  InspectorEmployeeId: string
  InspectorBasis: string
  FallbackReason: string | null
  InspectionStartedAt: string
  InspectionCompletedAt: string | null
  InspectedQuantity: number
  AcceptedQuantity: number
  RejectedQuantity: number
  /** Units received but never presented for inspection — auto-rejected on finalize. */
  InspectionShortfallRejectedQuantity: number
  Decision: QcDecision | ''
  AcceptedConditionLocationId: string | null
  Status: string
  Version: number
  ParameterResults: QcParameterResult[]
  SerialDispositions: QcSerialDisposition[]
}

export interface QcInspectionResult {
  Id: string
  InspectionNumber: string
  GoodsReceiptLineId: string | null
  DeliveryChallanLineId: string | null
  /** Denormalized source context for the header — the queue shape carries the same fields. */
  SourceNumber: string
  ItemCode: string
  ItemName: string
  CategoryCode: string
  Quantity: number
  QcDueAt: string | null
  StockPostingBatchId: string | null
  CurrentRevision: QcRevision
  PriorRevisions: QcRevision[]
}

export interface UpdateQcRevisionRequest {
  InspectedQuantity: number
  AcceptedQuantity: number
  RejectedQuantity: number
  InspectionShortfallRejectedQuantity: number
  Decision: QcDecision
  AcceptedConditionLocationId: string | null
  ParameterResults: {
    QcInspectionPolicyId: string
    SampleOrdinal: number
    ObservedNumericValue: number | null
    ObservedTextValue: string | null
    Result: 'PASS' | 'FAIL'
    Remarks: string | null
  }[]
  SerialDispositions: {
    InventorySerialId: string
    Disposition: SerialDispositionValue
    Reason: string | null
  }[]
  Version: number
}

export interface FinalizeQcRevisionRequest {
  Version: number
  IdempotencyKey: string
}

export interface CorrectQcInspectionRequest {
  Reason: string
  Version: number
}

export interface QcQueueResult {
  TotalCount: number
  PageNumber: number
  PageSize: number
  Items: QcQueueItem[]
}
