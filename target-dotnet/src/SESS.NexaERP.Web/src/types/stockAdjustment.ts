// A2 stock adjustment wire contract: SESS.NexaERP.Application/Stores/StockAdjustmentContracts.cs.
// PascalCase on the wire (ApiJsonContract). DateOnly fields are yyyy-MM-dd;
// DateTimeOffset fields arrive as ISO instants and are shown in local time.

export const STOCK_ADJUSTMENT_STATES = ['DRAFT', 'SUBMITTED', 'APPROVED', 'POSTED', 'REJECTED'] as const
export type StockAdjustmentStatus = (typeof STOCK_ADJUSTMENT_STATES)[number]

/** StockAdjustmentReasonKinds.All. DAMAGE_LOSS removes stock only (write-off: TD with Accounts concurrence). */
export const STOCK_ADJUSTMENT_REASON_KINDS = ['COUNT_VARIANCE', 'DAMAGE_LOSS', 'CORRECTION'] as const
export type StockAdjustmentReasonKind = (typeof STOCK_ADJUSTMENT_REASON_KINDS)[number]

export const STOCK_ADJUSTMENT_REASON_WORDS: Record<StockAdjustmentReasonKind, string> = {
  COUNT_VARIANCE: 'Count variance — physical count differs from the book',
  DAMAGE_LOSS: 'Damage / loss write-off — removes stock only',
  CORRECTION: 'Correction — an earlier entry was wrong',
}

/** Role codes the approval snapshot can name, in plain words. Display only; authority comes from the server. */
export const STOCK_ADJUSTMENT_ROLE_WORDS: Record<string, string> = {
  STORES_MANAGER: 'Stores Manager',
  TECHNICAL_DIRECTOR: 'Technical Director',
  MANAGING_DIRECTOR: 'Managing Director',
  ACCOUNTS_MANAGER: 'Accounts Manager (concurrence)',
  STORES_EXECUTIVE: 'Stores Executive',
}

export function stockAdjustmentRoleWords(code: string): string {
  return STOCK_ADJUSTMENT_ROLE_WORDS[code] ?? code.replaceAll('_', ' ')
}

/** StockAdjustmentLineInput. QuantityChange is signed: + adds stock, − removes it. */
export interface StockAdjustmentLineInput {
  ItemId: string
  WarehouseConditionLocationId: string
  QuantityChange: number
  /** Required (≥ 0) for an addition; must be null for a removal (valued from FIFO). */
  UnitValue: number | null
  LotNumber: string | null
  /** A serialized line changes exactly one unit. */
  SerialNumber: string | null
  Remarks: string | null
}

export interface CreateStockAdjustmentRequest {
  WarehouseId: string
  ReasonKind: StockAdjustmentReasonKind
  /** DateOnly, yyyy-MM-dd. Inside the chosen open inventory period, not after the server's (UTC) date. */
  EffectiveDate: string
  InventoryPeriodId: string
  Remarks: string
  Lines: StockAdjustmentLineInput[]
  IdempotencyKey: string
  CounterEmployeeIds: string[] | null
  BackdateReason: string | null
  BackdateEvidenceId: string | null
  ReversesStockAdjustmentId: string | null
}

/** PUT: replaces the lines as a new revision and returns the record to DRAFT. */
export interface ReviseStockAdjustmentRequest {
  Version: number
  Remarks: string
  Lines: StockAdjustmentLineInput[]
  Reason: string
  IdempotencyKey: string
  BackdateReason: string | null
  BackdateEvidenceId: string | null
}

export interface StockAdjustmentTransitionRequest {
  Version: number
  Reason: string
  IdempotencyKey: string
}

export interface StockAdjustmentDecisionRequest extends StockAdjustmentTransitionRequest {
  /** Which outstanding required role the approver acts in; null lets the server pick the least privileged one held. */
  RoleCode: string | null
}

export interface StockAdjustmentLineView {
  Id: string
  LineNumber: number
  ItemId: string
  ItemCode: string
  ItemName: string
  WarehouseConditionLocationId: string
  RackBinId: string
  RackBinCode: string
  LotNumber: string | null
  SerialNumber: string | null
  QuantityChange: number
  UnitValue: number | null
  /** Addition: quantity × stated unit value. Removal: FIFO carrying value at recording. */
  AcceptedLineValue: number
  Remarks: string | null
}

export interface StockAdjustmentDecisionView {
  Id: string
  RevisionNumber: number
  Decision: 'APPROVE' | 'REJECT' | string
  EmployeeId: string
  EmployeeCode: string
  EmployeeName: string
  RoleCode: string
  RoleAssignmentId: string
  RoleAssignmentType: string
  DecidedAt: string
  Reason: string
}

export interface StockAdjustmentView {
  Id: string
  AdjustmentNumber: string
  WarehouseId: string
  WarehouseCode: string
  ReasonKind: StockAdjustmentReasonKind | string
  EffectiveDate: string
  InventoryPeriodId: string
  InventoryPeriodCode: string
  Status: StockAdjustmentStatus | string
  CurrentRevisionNumber: number
  Remarks: string
  RecordedByEmployeeId: string
  RecordedByEmployeeCode: string
  CounterEmployeeIds: string[]
  BackdateReason: string | null
  BackdateEvidenceId: string | null
  DaysBackdated: number
  /** Snapshot value once submitted; before that, the sum of the current lines' accepted values. */
  AbsoluteValue: number
  /** From the approval snapshot taken at submission; empty while DRAFT/REJECTED. */
  RequiredRoleCodes: string[]
  /** Required roles not yet approved on the current revision; only while SUBMITTED. */
  OutstandingRoleCodes: string[]
  /** Recorder and counters: they may not approve or reject. */
  ExcludedEmployeeIds: string[]
  ReversesStockAdjustmentId: string | null
  StockPostingBatchId: string | null
  PostedAt: string | null
  Version: number
  Replayed: boolean
  Lines: StockAdjustmentLineView[]
  Decisions: StockAdjustmentDecisionView[]
}

/** StockAdjustmentPage — note Total, not TotalCount. */
export interface StockAdjustmentPage {
  Total: number
  Page: number
  PageSize: number
  Items: StockAdjustmentView[]
}

export interface StockAdjustmentPeriodView {
  Id: string
  Code: string
  Name: string
  StartDate: string
  EndDate: string
  Status: string
}
