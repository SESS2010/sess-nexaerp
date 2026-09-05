// Mirrors SESS.NexaERP.Application.Stores.QcContracts (Stores Slice 3, live on
// main since 2026-09-02) and the /api/v1/qc endpoints in QcEndpoints.cs.
//
// The shipped design differs from the earlier mock in three ways that shape
// every screen: QC is per GRN lot allocation (not per line); there is no draft
// revision — one POST finalizes and posts stock; and a finalized inspection is
// changed only by a correction, which is a fresh finalized revision.

/** One GRN lot allocation sitting in QC_HOLD with no inspection yet. */
export interface QcQueueItem {
  GoodsReceiptLineLotAllocationId: string
  GrnNumber: string
  GoodsReceiptLineId: string
  LineNumber: number
  LotOrdinal: number
  ItemId: string
  ItemCode: string
  ItemName: string
  InventoryLotId: string
  SupplierLotNumber: string | null
  Quantity: number
  ReceivedAt: string
  AgeDays: number
  CompletionLimitDays: number
  IsOverdue: boolean
  HasEffectivePolicy: boolean
  /** EFFECTIVE_POLICY or MISSING_POLICY_QC_HOLD. */
  PolicyResolution: string
}

export const SERIAL_DISPOSITIONS = ['ACCEPTED', 'REJECTED'] as const
export type SerialDispositionValue = (typeof SERIAL_DISPOSITIONS)[number]

export interface QcParameterResultRequest {
  QcInspectionPolicyId: string
  SampleOrdinal: number
  ObservedNumericValue: number | null
  ObservedTextValue: string | null
  Result: 'PASS' | 'FAIL'
  Remarks: string | null
}

export interface QcSerialDispositionRequest {
  InventorySerialId: string
  Disposition: SerialDispositionValue
  Reason: string | null
}

/**
 * Accepted + Rejected + DiscrepancyPending must equal the lot allocation
 * quantity exactly. AcceptedConditionLocationId is required when Accepted > 0
 * and must be an effective AVAILABLE condition location.
 */
export interface FinalizeQcInspectionRequest {
  GoodsReceiptLineLotAllocationId: string
  InspectionStartedAt: string
  AcceptedQuantity: number
  RejectedQuantity: number
  DiscrepancyPendingQuantity: number
  AcceptedConditionLocationId: string | null
  ParameterResults: QcParameterResultRequest[]
  SerialDispositions: QcSerialDispositionRequest[]
}

export interface CorrectQcInspectionRequest {
  RevisesRevisionId: string
  CorrectionReason: string
  InspectionStartedAt: string
  AcceptedQuantity: number
  RejectedQuantity: number
  DiscrepancyPendingQuantity: number
  AcceptedConditionLocationId: string | null
  ParameterResults: QcParameterResultRequest[]
  SerialDispositions: QcSerialDispositionRequest[]
}

export interface QcParameterResultView {
  Id: string
  ParameterCode: string
  MeasuredValue: string
  Result: string
}

export interface QcSerialDispositionView {
  InventorySerialId: string
  SerialNumber: string
  Disposition: string
}

/** Decision is server-derived: DISCREPANCY_PENDING, PARTIAL_ACCEPTED, ACCEPTED or REJECTED. */
export interface QcInspectionResult {
  InspectionId: string
  InspectionNumber: string
  RevisionId: string
  RevisionNumber: number
  GoodsReceiptLineLotAllocationId: string
  GrnNumber: string
  ItemCode: string
  LotOrdinal: number
  InspectedQuantity: number
  AcceptedQuantity: number
  RejectedQuantity: number
  DiscrepancyPendingQuantity: number
  Decision: string
  Status: string
  InspectorBasis: string
  InspectorEmployeeId: string
  StockPostingBatchId: string | null
  /** True when the Idempotency-Key matched an earlier finalize and nothing new was written. */
  Replayed: boolean
  ParameterResults: QcParameterResultView[]
  SerialDispositions: QcSerialDispositionView[]
}

/** Row of GET /api/v1/rev869a/configuration/warehouse-condition-locations. */
export interface WarehouseConditionLocation {
  Id: string
  WarehouseId: string
  WarehouseCode: string
  RackBinId: string
  BinCode: string
  ConditionCode: string
  EffectiveFrom: string
  EffectiveTo: string | null
  IsActive: boolean
  Version: number
}

/* ------------------------------------------------------------------ */
/* Inventory concessions (/api/v1/qc/concessions). A concession asks the */
/* Technical Director to accept rejected QC stock for a stated use.     */
/* ------------------------------------------------------------------ */

export interface CreateInventoryConcessionRequest {
  /** Id of the rejected lot disposition on the finalized revision (not exposed by any GET today). */
  QcInspectionLotDispositionId: string
  /** Id of the FAIL parameter result on that revision (QcParameterResultView.Id). */
  FailedParameterResultId: string
  Quantity: number
  FailedParameter: string
  MeasuredValue: string
  TechnicalJustification: string
  IntendedUse: string
  InventorySerialIds: string[]
}

export interface ApproveInventoryConcessionRequest {
  Version: number
  AvailableConditionLocationId: string
  DecisionReason: string
}

export interface RejectInventoryConcessionRequest {
  Version: number
  DecisionReason: string
}

export interface ReverseInventoryConcessionRequest {
  Version: number
  Reason: string
}

/** Status is DRAFT, APPROVED, REJECTED or REVERSED. */
export interface InventoryConcessionResult {
  Id: string
  ConcessionNumber: string
  Status: string
  QcInspectionRevisionId: string
  QcInspectionLotDispositionId: string
  Quantity: number
  GoodsReceiptLineLotAllocationId: string
  InventorySerialIds: string[]
  FailedParameter: string
  MeasuredValue: string
  TechnicalJustification: string
  IntendedUse: string
  CreatedByEmployeeId: string
  DecidedByEmployeeId: string | null
  DecidedRoleCode: string | null
  StockPostingBatchId: string | null
  AvailableProvenanceLayerId: string | null
  ProvenanceAnnotationJson: string | null
  Version: number
  Replayed: boolean
}
