// Mirrors SESS.NexaERP.Application/Stores/EstimatedBomContracts.cs and
// ProductionEngineeringContracts.cs (PascalCase wire contract).
//
// Endpoints:
//   /api/v1/design/estimated-boms     EstimatedBomEndpoints.cs        (page design.estimated-bom)
//   /api/v1/production/boms           ProductionEngineeringEndpoints.cs (page production.production-bom)
// Every command carries IdempotencyKey in the BODY.
//
// Both BOMs revise the same way: DRAFT → submit → SUBMITTED → approve → APPROVED;
// a new revision can only follow an APPROVED one and reopens as DRAFT. Nobody
// may approve a revision they prepared. Both need an OPEN (Accounts-confirmed)
// Job Order, and a Production BOM additionally needs the job's Estimated BOM
// to have an approved revision — its first revision is copied from it.

export const BOM_STATES = ['DRAFT', 'SUBMITTED', 'APPROVED'] as const

export interface BomLineInput {
  ItemId: string
  UomId: string
  Quantity: number
  Remarks: string | null
  /** Estimated BOM only (EstimatedBomLineInput.EstimatedUnitValue, main 7b2fa99).
   *  Null = use the item's last accepted purchase price at approval; the server
   *  refuses approval when neither exists. Never send 0. */
  EstimatedUnitValue?: number | null
}

// --- Estimated BOM (Design) ---

export interface CreateEstimatedBomRequest {
  JobOrderId: string
  RevisionReason: string
  Lines: BomLineInput[]
  IdempotencyKey: string
}

export interface ReplaceEstimatedBomLinesRequest {
  ExpectedVersion: number
  RevisionReason: string
  Lines: BomLineInput[]
  IdempotencyKey: string
}

export interface EstimatedBomActionRequest {
  ExpectedVersion: number
  Remarks: string
  IdempotencyKey: string
}

export interface NewEstimatedBomRevisionRequest {
  ExpectedBomVersion: number
  RevisionReason: string
  IdempotencyKey: string
}

export interface EstimatedBomLineView {
  Id: string
  LineNumber: number
  OriginalItemId: string
  OriginalItemCode: string
  /** After an item merge the canonical item differs from the one originally entered. */
  CanonicalItemId: string
  CanonicalItemCode: string
  CanonicalItemActive: boolean
  CanonicalItemApprovalStatus: string
  UomId: string
  UomCode: string
  Quantity: number
  Remarks: string | null
  /** Frozen at approval from the last accepted purchase price, or the explicit override. Null while no rate exists. */
  EstimatedUnitValue: number | null
  EstimatedUnitValueOverridden: boolean
  CurrencyCode: string
}

export interface EstimatedBomCanonicalLineView {
  CanonicalItemId: string
  CanonicalItemCode: string
  CanonicalItemActive: boolean
  CanonicalItemApprovalStatus: string
  BaseUomId: string
  BaseUomCode: string
  BaseQuantity: number
  SourceLines: EstimatedBomLineView[]
}

export interface EstimatedBomRevisionView {
  Id: string
  RevisionNumber: number
  Status: string
  RevisionReason: string
  PreparedByEmployeeId: string
  SubmittedAt: string | null
  ApprovedAt: string | null
  ApprovedByEmployeeId: string | null
  ApprovalReason: string | null
  Version: number
  Lines: EstimatedBomLineView[]
  CanonicalLines: EstimatedBomCanonicalLineView[]
}

export interface EstimatedBomSummary {
  Id: string
  BomNumber: string
  JobOrderId: string
  JobOrderNumber: string
  CurrentRevisionNumber: number
  Status: string
  ApprovedRevisionId: string | null
  CommercialBaselineRevisionId: string | null
  Version: number
}

export interface EstimatedBomView extends EstimatedBomSummary {
  CurrentRevision: EstimatedBomRevisionView
}

export interface EstimatedBomHistoryView {
  Id: string
  EstimatedBomRevisionId: string
  Action: string
  FromStatus: string | null
  ToStatus: string
  ActorEmployeeId: string
  ActorRoleCode: string
  ResolvedRoleAssignmentId: string
  ResolvedRoleAssignmentType: string
  CorrelationId: string
  Remarks: string
  CreatedAt: string
}

// --- Production BOM ---

export interface CreateProductionBomRequest {
  JobOrderId: string
  RevisionReason: string
  IdempotencyKey: string
}

export interface ReplaceProductionBomRequest {
  ExpectedVersion: number
  RevisionReason: string
  Lines: BomLineInput[]
  IdempotencyKey: string
}

export interface ProductionBomActionRequest {
  ExpectedVersion: number
  Remarks: string
  IdempotencyKey: string
}

export interface NewProductionBomRevisionRequest {
  ExpectedBomVersion: number
  RevisionReason: string
  IdempotencyKey: string
}

/** Pins an APPROVED revision to the Job Order; MIR excess checks read the pinned one. */
export interface PinProductionBomRevisionRequest {
  RevisionId: string
  ExpectedJobOrderVersion: number
  Reason: string
  IdempotencyKey: string
}

export interface ProductionBomLineView {
  Id: string
  LineNumber: number
  ItemId: string
  ItemCode: string
  UomId: string
  UomCode: string
  Quantity: number
  Remarks: string | null
}

export interface ProductionBomRevisionView {
  Id: string
  RevisionNumber: number
  SourceEstimatedBomRevisionId: string
  SupersedesRevisionId: string | null
  Status: string
  RevisionReason: string
  PreparedByEmployeeId: string
  SubmittedAt: string | null
  ApprovedAt: string | null
  ApprovedByEmployeeId: string | null
  ApprovalReason: string | null
  Version: number
  Lines: ProductionBomLineView[]
}

export interface ProductionBomView {
  Id: string
  BomNumber: string
  JobOrderId: string
  JobOrderNumber: string
  PinnedRevisionId: string | null
  CurrentRevisionNumber: number
  Status: string
  Version: number
  CurrentRevision: ProductionBomRevisionView
}
