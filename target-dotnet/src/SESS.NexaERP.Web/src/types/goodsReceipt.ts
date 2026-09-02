// Mirrors SESS.NexaERP.Application/Stores/GoodsReceiptContracts.cs (PascalCase wire contract).
//
// Transcribed from the C# records, not from outputs/sess_api_contract.md — that
// document described a different shape (GateEntryId, LineValue, a serial-validation
// endpoint) that the shipped API does not implement.

/**
 * A GRN is editable while DRAFT and immutable once FINALIZED. Reversal does not
 * flip the status: it creates a separate FINALIZED document with
 * DocumentKind = 'REVERSAL'. The list endpoint accepts only these two values.
 */
export const GOODS_RECEIPT_STATES = ['DRAFT', 'FINALIZED'] as const
export type GoodsReceiptState = (typeof GOODS_RECEIPT_STATES)[number]

/**
 * Set by the server per line from the effective SerialCaptureThreshold rule
 * (unit rate above the threshold ⇒ REQUIRED), overridable per item.
 * REQUIRED: exactly one serial per received unit. OPTIONAL: none, or a complete set.
 */
export type SerialCaptureMode = 'REQUIRED' | 'OPTIONAL'

export interface GoodsReceiptLotRequest {
  LotOrdinal: number
  Quantity: number
  SupplierLotNumber: string | null
  ManufacturerLotNumber: string | null
  ManufactureDate: string | null
  ExpiryDate: string | null
}

export interface GoodsReceiptSerialRequest {
  SerialOrdinal: number
  LotOrdinal: number
  EnteredSerialNumber: string
  /**
   * What actually gets stored. Differs from EnteredSerialNumber only when the
   * storekeeper disambiguates a duplicate — and then the server demands both
   * DuplicateWarningAcknowledged and a DisambiguationReason.
   */
  StoredSerialNumber: string
  DuplicateWarningAcknowledged: boolean
  DisambiguationReason: string | null
}

export interface GoodsReceiptLineRequest {
  GateEntryLineId: string
  Lots: GoodsReceiptLotRequest[]
  Serials: GoodsReceiptSerialRequest[]
}

export interface CreateGoodsReceiptRequest {
  /** The GRN is addressed by Gate Entry *number*, not id. The gate must be FINALIZED. */
  GateEntryNumber: string
  VendorBillNumber: string
  VendorBillDate: string
  ReceivedAt: string
  IsoReceiptVerificationJson: string
  Lines: GoodsReceiptLineRequest[]
}

export interface UpdateGoodsReceiptRequest {
  VendorBillNumber: string
  VendorBillDate: string
  ReceivedAt: string
  IsoReceiptVerificationJson: string
  Lines: GoodsReceiptLineRequest[]
  Version: number
}

/** Finalize and reverse carry the idempotency key in the BODY; create uses the header. */
export interface FinalizeGoodsReceiptRequest {
  Version: number
  IdempotencyKey: string
}

export interface ReverseGoodsReceiptRequest {
  Version: number
  Reason: string
  IdempotencyKey: string
}

export interface GoodsReceiptLotResult {
  Id: string
  InventoryLotId: string
  LotOrdinal: number
  Quantity: number
  SupplierLotNumber: string | null
  ManufacturerLotNumber: string | null
  ManufactureDate: string | null
  ExpiryDate: string | null
}

export interface GoodsReceiptSerialResult {
  Id: string
  InventorySerialId: string | null
  SerialOrdinal: number
  LotOrdinal: number
  EnteredSerialNumber: string
  StoredSerialNumber: string
  DuplicateWarningAcknowledged: boolean
  DisambiguationReason: string | null
}

export interface GoodsReceiptLineResult {
  Id: string
  LineNumber: number
  GateEntryLineId: string
  PurchaseOrderLineId: string
  ItemId: string
  ItemCode: string
  ItemName: string
  ItemCategoryCode: string
  HsnSacCode: string
  GstPercentage: number
  Model: string | null
  ManufacturerPartNumber: string | null
  Uom: string
  ReceivedQuantity: number
  UnitRate: number
  SerialCaptureMode: SerialCaptureMode
  /** Server-computed as VendorBillDate + 13 months. Never sent by the client. */
  WarrantyExpiryDate: string
  QcHoldConditionLocationId: string
  Lots: GoodsReceiptLotResult[]
  Serials: GoodsReceiptSerialResult[]
}

export interface GoodsReceiptHistoryResult {
  FromStatus: string | null
  ToStatus: string
  Action: string
  ActorEmployeeId: string
  ActorRoleCode: string
  OccurredAt: string
}

export interface GoodsReceiptResult {
  Id: string
  GrnNumber: string
  DocumentKind: string
  ReversesGoodsReceiptId: string | null
  ReversalReason: string | null
  GateEntryNumber: string
  GateEntryId: string
  PurchaseOrderNumber: string
  PurchaseOrderId: string
  VendorId: string
  VendorName: string
  VendorBillNumber: string
  VendorBillDate: string
  /** Inherited from the Gate Entry — not entered on the GRN. */
  VendorDcNumber: string
  /** Inherited from the Gate Entry — not entered on the GRN. */
  ModeOfTransport: string
  ReceivedAt: string
  IsoReceiptVerificationJson: string
  Status: string
  Version: number
  StockPostingBatchId: string | null
  /** True when an idempotency key replay returned the original document. */
  Replayed: boolean
  /**
   * Duplicate-serial warnings, recomputed on every read against both this
   * document's serials and inventory_serials for the company. Finalize is
   * refused by the database while any of these are outstanding.
   */
  Warnings: string[]
  Lines: GoodsReceiptLineResult[]
  History: GoodsReceiptHistoryResult[]
}

/** The list endpoint returns no total count — only the requested page slice. */
export interface GoodsReceiptListResult {
  Page: number
  PageSize: number
  Items: GoodsReceiptResult[]
}

/** ISO incoming-inspection checks recorded against the bill, for clause 8.4 evidence. */
export interface IsoGrnVerification {
  BillVerified: boolean
  QuantityVerified: boolean
  CertificatesReceived: boolean
  TestReportReceived: boolean
  Remarks: string
}

export const DEFAULT_ISO_GRN_VERIFICATION: IsoGrnVerification = {
  BillVerified: false,
  QuantityVerified: false,
  CertificatesReceived: false,
  TestReportReceived: false,
  Remarks: '',
}

/** Warranty runs 13 months from the vendor bill date. Mirrors the server rule so the
 *  storekeeper sees the date while filling the form, before the GRN exists. */
export function warrantyFromBillDate(billDate: string): string {
  if (!billDate) return ''
  const [y, m, d] = billDate.split('-').map(Number)
  if (!y || !m || !d) return ''
  // Clamp to the target month's last day, exactly like C# DateOnly.AddMonths:
  // 31 Jan + 13 months is 28/29 Feb, never a rollover into March.
  const totalMonths = m - 1 + 13
  const year = y + Math.floor(totalMonths / 12)
  const month = totalMonths % 12
  const daysInTarget = new Date(Date.UTC(year, month + 1, 0)).getUTCDate()
  const day = Math.min(d, daysInTarget)
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${year}-${pad(month + 1)}-${pad(day)}`
}
