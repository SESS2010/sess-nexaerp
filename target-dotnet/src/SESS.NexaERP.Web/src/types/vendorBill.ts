/**
 * Vendor bills — POST /api/v1/accounts/vendor-bills/from-grn/{grnId}, then
 * accept / reject / reverse (VendorBillContracts.cs). Raising a bill against a
 * finalized GRN is what turns the Actual BOM from PROVISIONAL_UNBILLED into
 * LANDED_ACCEPTED: the accepted line's LandedUnitRate (ex recoverable tax, plus
 * capitalised charges) is the value the fitted component carries.
 */

/** Charge types accepted by EfVendorBillService; only RECOVERABLE_GST is a recoverable tax. */
export const VENDOR_BILL_CHARGE_TYPES = [
  'FREIGHT', 'INSURANCE', 'PACKING', 'HANDLING', 'CLEARING_AGENT', 'DUTY', 'MISC_INWARD', 'NON_CREDITABLE_TAX', 'RECOVERABLE_GST',
] as const
export type VendorBillChargeType = (typeof VENDOR_BILL_CHARGE_TYPES)[number]

export interface VendorBillLineInput {
  GoodsReceiptLineId: string
  Quantity: number
  UnitRate: number
  /** Payable for the line as billed, tax inclusive. */
  TotalPayableValue: number
  VerifiedGrossWeightKg?: number | null
}

export interface VendorBillChargeInput {
  ChargeType: VendorBillChargeType
  ChargeValue: number
  /** Must be true exactly when ChargeType is RECOVERABLE_GST. */
  IsRecoverableTax: boolean
}

export interface CreateVendorBillRequest {
  BillNumber: string
  BillDate: string
  Lines: VendorBillLineInput[]
  IdempotencyKey: string
  Charges?: VendorBillChargeInput[] | null
}

export interface VendorBillDecisionRequest {
  Version: number
  Reason: string
  IdempotencyKey: string
}

export interface VendorBillLineView {
  Id: string
  LineNumber: number
  GoodsReceiptLineId: string
  PurchaseOrderLineId: string
  ItemId: string
  BilledQuantity: number
  ExpectedUnitRate: number
  BilledUnitRate: number
  ExpectedPayableValue: number
  BilledPayableValue: number
  MatchStatus: string
  VerifiedGrossWeightKg: number | null
  AllocatedChargeValue: number
  /** Inventory value per unit: taxable rate per the governed ITC snapshot plus allocated charges. */
  LandedUnitRate: number
}

export interface VendorBillChargeView {
  Id: string
  ChargeNumber: number
  ChargeType: string
  ChargeValue: number
  IsRecoverableTax: boolean
  IncludedInInventoryCost: boolean
  AllocationBasis: string
}

export interface VendorBillView {
  Id: string
  BillNumber: string
  BillDate: string
  GoodsReceiptId: string
  PurchaseOrderId: string
  VendorId: string
  Status: string
  MatchStatus: string
  TotalPayableValue: number
  TotalChargeValue: number
  TotalLandedValue: number
  Version: number
  Replayed: boolean
  ActorRoleCode: string
  ResolvedRoleAssignmentId: string
  ResolvedRoleAssignmentType: string
  DecidedAt: string | null
  DecidedByEmployeeId: string | null
  DecisionReason: string | null
  ReversedAt: string | null
  ReversedByEmployeeId: string | null
  ReversalReason: string | null
  Lines: VendorBillLineView[]
  Charges: VendorBillChargeView[]
}

export interface VendorBillPage {
  Total: number
  Page: number
  PageSize: number
  Items: VendorBillView[]
}
