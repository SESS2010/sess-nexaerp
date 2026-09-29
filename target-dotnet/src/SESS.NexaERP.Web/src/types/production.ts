// Mirrors SESS.NexaERP.Application/Stores/JobOrderContracts.cs,
// FitmentActualBomContracts.cs and JobOrderFatReadinessContracts.cs
// (PascalCase wire contract).
//
// Endpoints:
//   /api/v1/production/job-orders                       JobOrderEndpoints.cs
//   /api/v1/production/job-orders/{id}/fat-readiness    JobOrderFatReadinessEndpoints.cs
//   /api/v1/production/component-fitments               FitmentActualBomEndpoints.cs
// Every command carries IdempotencyKey in the BODY.

// --- Job Order ---

/**
 * EfJobOrderService: Production initiates → PENDING_ACCOUNTS; a DIFFERENT
 * Accounts employee confirms → OPEN. A PENDING_ACCOUNTS job refuses an
 * Estimated BOM, a Production BOM and a job-backed MIR (409).
 */
export const JOB_ORDER_STATES = ['PENDING_ACCOUNTS', 'OPEN'] as const

/** Job-order column FatReadinessStatus; the database refuses READY while unexplained custody remains. */
export const FAT_READINESS_STATES = ['NOT_RECONCILED', 'BLOCKED', 'READY'] as const

/** GET /job-orders/customer-po-lines: a current-revision line of a non-completed Customer PO in the company. */
export interface JobOrderCustomerPoLineLookup {
  CustomerPurchaseOrderLineId: string
  CustomerPurchaseOrderId: string
  PoRecordNumber: string
  CustomerPoNumber: string
  CustomerName: string
  WorkStatus: string
  RevisionNumber: number
  SlNo: number
  Description: string
  ItemId: string
  ItemCode: string
  Quantity: number | null
  Uom: string | null
  /** MachineOrdinals that already have a Job Order on this line. The adopted
   *  lookup (main 2268658, JobOrderCustomerPoLineView) reports only a count, so
   *  this stays empty and the server refuses a duplicate ordinal. */
  TakenOrdinals: number[]
  /** Job Orders already created on this line (JobOrderCustomerPoLineView.CreatedJobOrderCount). */
  CreatedJobOrderCount: number
}

export interface CreateJobOrderRequest {
  /** Must be a line of the Customer PO's CURRENT revision with a positive whole Quantity. */
  CustomerPurchaseOrderLineId: string
  /** 1..line Quantity — one Job Order per (Company, CPO line, MachineOrdinal). */
  MachineOrdinal: number
  /** Unique per company. */
  MachineSerial: string
  /** yyyy-MM-dd (DateOnly). */
  JobOrderDate: string
  PlannedCompletionDate: string | null
  IdempotencyKey: string
}

export interface ConfirmJobOrderRequest {
  ExpectedVersion: number
  Reason: string
  IdempotencyKey: string
}

export interface JobOrderSummary {
  Id: string
  JobOrderNumber: string
  CustomerPurchaseOrderId: string
  CustomerPurchaseOrderLineId: string
  CustomerPoNumber: string
  MachineOrdinal: number
  MachineModel: string
  MachineSerial: string
  CustomerName: string
  Status: string
  JobOrderDate: string
  PlannedCompletionDate: string | null
  Version: number
}

export interface JobOrderView extends JobOrderSummary {
  CustomerPoRecordNumber: string
  CustomerPoRevisionNumber: number
  CustomerPoLineNumber: number
  MachineItemId: string
  MachineItemCode: string
  InitiatedByEmployeeId: string
  InitiatedActorRoleCode: string
  InitiatedRoleAssignmentId: string
  InitiatedRoleAssignmentType: string
  AccountsConfirmedAt: string | null
  AccountsConfirmedByEmployeeId: string | null
  AccountsConfirmationActorRoleCode: string | null
  AccountsConfirmationRoleAssignmentId: string | null
  AccountsConfirmationRoleAssignmentType: string | null
  AccountsConfirmationReason: string | null
  FatReadinessStatus: string
  FatReconciledAt: string | null
  FatReconciledByEmployeeId: string | null
  LatestFatReconciliationId: string | null
}

export interface JobOrderHistoryView {
  Id: string
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

// --- Component fitment and generated Actual BOM ---

export interface ConfirmComponentFitmentRequest {
  JobOrderId: string
  /** A line of a material issue made against this job; custody must still be with the engineer. */
  MaterialIssueLineId: string
  QuantityBase: number
  /** ISO date-time. */
  FittedAt: string
  ConfirmationNote: string
  /** Re-verifying a reversed fitment links the new record to it. */
  ReverifiesFitmentId: string | null
  IdempotencyKey: string
}

export interface ReverseComponentFitmentRequest {
  Reason: string
  IdempotencyKey: string
}

export interface ComponentFitmentSummary {
  Id: string
  FitmentNumber: string
  JobOrderId: string
  JobOrderNumber: string
  MaterialIssueLineId: string
  ItemId: string
  ItemCode: string
  QuantityBase: number
  FittedAt: string
  ConfirmedByEmployeeId: string
  ActorRoleCode: string
  ResolvedRoleAssignmentId: string
  ResolvedRoleAssignmentType: string
  IsReversed: boolean
  /** The reverser is the confirmer. Allowed, but recorded so a pattern is visible. */
  IsSelfReversal: boolean
  ReversedAt: string | null
  ReversalReason: string | null
  ReverifiesFitmentId: string | null
  Replayed: boolean
}

export interface ActualBomEntryView {
  Id: string
  /** e.g. FITMENT or REVERSAL. */
  EntryKind: string
  ComponentFitmentId: string | null
  ComponentFitmentReversalId: string | null
  MaterialIssueLineId: string
  ItemId: string
  ItemCode: string
  ItemName: string
  UomId: string
  UomCode: string
  QuantityBase: number
  InventoryProvenanceLayerId: string
  InventoryLotId: string | null
  InventorySerialId: string | null
  SerialNumber: string | null
  /** Null for stock that entered at the opening-stock ceremony; then GrnNumber is ''. */
  GoodsReceiptLineId: string | null
  GrnNumber: string
  /** Null until an accepted vendor bill covers the GRN line the stock came from. */
  VendorBillLineId: string | null
  /**
   * For a GRN origin: the accepted bill. For an opening-stock origin: the bill
   * number SESS DECLARED on the workbook — never verified in this system.
   */
  BillNumber: string | null
  /**
   * LANDED_ACCEPTED (valued from an accepted bill), PROVISIONAL_UNBILLED
   * (₹0 until then) or OPENING_CONFIRMED (valued from the Accounts-confirmed
   * ex-tax value at the opening-stock ceremony).
   */
  ValuationStatus: string
  AcceptedMaterialValue: number
  AllocatedChargeValue: number
  TotalAcceptedValue: number
  ValuedAt: string | null
  OccurredAt: string
  /** Set when the fitted stock came from the opening-stock ceremony rather than a GRN. */
  OpeningStockLineId: string | null
  OpeningLineReference: string | null
  /**
   * Being added server-side (agreed 2026-09-21): the provenance sentence
   * without payment state. When the server sends it the pane shows it as-is;
   * until then provenance.ts composes it from the fields above.
   */
  Provenance?: string | null
}

export interface ActualBomVarianceLineView {
  ItemId: string
  ItemCode: string
  ItemName: string
  BaseUomId: string
  BaseUomCode: string
  BaselineQuantity: number
  ActualQuantity: number
  QuantityVariance: number
  /** Null when the baseline revision carries no cost. */
  BaselineValue: number | null
  ActualAcceptedValue: number
  ValueVariance: number | null
}

/**
 * Actual accepted value against a pinned baseline: COMMERCIAL_ESTIMATED_BOM
 * (the offer) or OPERATIONAL_PRODUCTION_BOM (the plan). Computed by the server.
 */
export interface ActualBomBaselineVarianceView {
  BaselineType: string
  BaselineRevisionId: string
  BaselineRevisionNumber: number
  BaselineCostAvailable: boolean
  BaselineValue: number | null
  ActualAcceptedValue: number
  ValueVariance: number | null
  Lines: ActualBomVarianceLineView[]
}

export interface ActualBomView {
  Id: string
  JobOrderId: string
  JobOrderNumber: string
  GeneratedAt: string
  TotalAcceptedMaterialValue: number
  TotalAllocatedChargeValue: number
  TotalAcceptedValue: number
  Entries: ActualBomEntryView[]
  OperationalVariance: ActualBomBaselineVarianceView
  CommercialVariance: ActualBomBaselineVarianceView
}

// --- FAT readiness ---

/** EfJobOrderFatReadinessService: RETURNED_LATE is proven by an accepted return document, never explained. */
export const FAT_DISPOSITIONS = ['LOST', 'SCRAPPED'] as const

export interface CreateFatCustodyExplanationRequest {
  MaterialIssueLineId: string
  QuantityBase: number
  Disposition: string
  Reason: string
  IdempotencyKey: string
}

export interface ReconcileJobOrderFatRequest {
  Reason: string
  IdempotencyKey: string
}

export interface FatCustodyExplanationView {
  Id: string
  JobOrderId: string
  MaterialIssueLineId: string
  QuantityBase: number
  Disposition: string
  Reason: string
  ExplainedByEmployeeId: string
  ActorRoleCode: string
  ResolvedRoleAssignmentId: string
  ResolvedRoleAssignmentType: string
  CreatedAt: string
  Replayed: boolean
}

export interface FatReconciliationLineView {
  Id: string
  MaterialIssueLineId: string
  ItemId: string
  ItemCode: string
  CustodianEmployeeId: string
  CustodianEmployeeCode: string
  IssuedQuantityBase: number
  FittedQuantityBase: number
  ReturnedQuantityBase: number
  ReturnedLateQuantityBase: number
  ExplainedLostQuantityBase: number
  ExplainedScrappedQuantityBase: number
  UnexplainedQuantityBase: number
  Classification: string
}

export interface FatReconciliationView {
  Id: string
  JobOrderId: string
  AttemptNumber: number
  /** BLOCKED or READY. */
  Result: string
  IssuedQuantityBase: number
  FittedQuantityBase: number
  ReturnedQuantityBase: number
  ExplainedQuantityBase: number
  UnexplainedQuantityBase: number
  ReconciledAt: string
  ReconciledByEmployeeId: string
  ActorRoleCode: string
  ResolvedRoleAssignmentId: string
  ResolvedRoleAssignmentType: string
  Reason: string
  Lines: FatReconciliationLineView[]
  Replayed: boolean
}

export interface JobOrderFatReadinessView {
  JobOrderId: string
  JobOrderNumber: string
  FatReadinessStatus: string
  FatReconciledAt: string | null
  FatReconciledByEmployeeId: string | null
  LatestFatReconciliationId: string | null
  LatestReconciliation: FatReconciliationView | null
}
