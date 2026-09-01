// Mirrors SESS.NexaERP.Application/Stores/GateEntryContracts.cs (PascalCase wire contract).

/** Gate Entry is a two-state document: editable while DRAFT, immutable once FINALIZED. */
export const GATE_ENTRY_STATES = ['DRAFT', 'FINALIZED'] as const
export type GateEntryState = (typeof GATE_ENTRY_STATES)[number]

export const TRANSPORT_MODES = [
  'Road',
  'Rail',
  'Air',
  'Sea',
  'Courier',
  'Hand Delivery',
] as const

export interface GateEntryLineRequest {
  PurchaseOrderLineId: string
  DeliveredQuantity: number
}

export interface CreateGateEntryRequest {
  PurchaseOrderNumber: string
  VendorDcNumber: string
  VehicleNumber: string | null
  ModeOfTransport: string
  ArrivedAt: string
  IsoReceiptVerificationJson: string
  Lines: GateEntryLineRequest[]
}

export interface UpdateGateEntryRequest {
  VendorDcNumber: string
  VehicleNumber: string | null
  ModeOfTransport: string
  ArrivedAt: string
  IsoReceiptVerificationJson: string
  Lines: GateEntryLineRequest[]
  Version: number
}

export interface FinalizeGateEntryRequest {
  Version: number
  IdempotencyKey: string
}

export interface GateEntryLineResult {
  Id: string
  LineNumber: number
  PurchaseOrderLineId: string
  ItemId: string
  ItemCode: string
  Uom: string
  DeliveredQuantity: number
}

export interface GateEntryHistoryResult {
  FromStatus: string | null
  ToStatus: string
  Action: string
  ActorEmployeeId: string
  ActorRoleCode: string
  OccurredAt: string
}

export interface GateEntryResult {
  Id: string
  GateEntryNumber: string
  PurchaseOrderNumber: string
  PurchaseOrderId: string
  VendorId: string
  VendorName: string
  VendorDcNumber: string
  VehicleNumber: string | null
  ModeOfTransport: string
  ArrivedAt: string
  IsoReceiptVerificationJson: string
  Status: string
  Version: number
  Lines: GateEntryLineResult[]
  History: GateEntryHistoryResult[]
}

/** The list endpoint returns no total count — only the requested page slice. */
export interface GateEntryListResult {
  Page: number
  PageSize: number
  Items: GateEntryResult[]
}

/**
 * ISO incoming-receipt checks. The API stores whatever JSON object it is given;
 * this shape keeps the storekeeper on a checklist instead of hand-writing JSON,
 * and it is what an auditor will be shown for clause 8.4 receipt verification.
 */
export interface IsoReceiptVerification {
  DocumentsVerified: boolean
  PackagingIntact: boolean
  QuantityMatchesDc: boolean
  VisualDamageObserved: boolean
  ColdChainMaintained: boolean | null
  Remarks: string
}

export const DEFAULT_ISO_VERIFICATION: IsoReceiptVerification = {
  DocumentsVerified: false,
  PackagingIntact: false,
  QuantityMatchesDc: false,
  VisualDamageObserved: false,
  ColdChainMaintained: null,
  Remarks: '',
}

/** A PO line as returned by GET /api/v1/purchase/purchase-orders/{number}. */
export interface SourcePurchaseOrderLine {
  Id: string
  LineNumber: number
  ItemCodeSnapshot: string
  ItemNameSnapshot: string
  UomSnapshot: string
  OrderedQuantity: number
}

export interface SourcePurchaseOrder {
  Id: string
  PoNumber: string
  Status: string
  CurrencyCode: string
  IsCurrentVersion: boolean
  RevisionNumber: number
  Lines: SourcePurchaseOrderLine[]
}
