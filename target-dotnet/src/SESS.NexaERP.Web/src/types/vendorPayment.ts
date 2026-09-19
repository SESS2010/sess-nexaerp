/**
 * Vendor advances and payments — /api/v1/accounts/vendor-financial-evidence
 * (VendorFinancialEvidenceContracts.cs). An advance is money paid against an
 * issued PO before any bill; a payment settles accepted vendor bills and is
 * allocated bill by bill. Both carry a payment reference and a bank-advice
 * evidence key (mandatory for advances and for any non-INR payment).
 */

export interface VendorAdvancePurchaseOrderOption {
  PurchaseOrderId: string
  PurchaseOrderNumber: string
  VendorId: string
  VendorCode: string
  VendorName: string
  CurrencyCode: string
  PurchaseOrderValue: number
  ActiveAdvanceAmount: number
  AvailableAdvanceAmount: number
  BillPaymentAmount: number
}

export interface RecordVendorAdvanceRequest {
  PurchaseOrderId: string
  PaidDate: string
  Amount: number
  CurrencyCode: string
  PaymentReference: string
  EvidenceObjectKey: string
  IdempotencyKey: string
}

export interface ReverseVendorAdvanceRequest {
  Reason: string
  IdempotencyKey: string
}

export interface VendorAdvanceView {
  Id: string
  AdvanceNumber: string
  PurchaseOrderId: string
  PurchaseOrderNumber: string
  VendorId: string
  VendorCode: string
  VendorName: string
  PaidDate: string
  Amount: number
  CurrencyCode: string
  PaymentReference: string
  EvidenceObjectKey: string
  AdjustedAmount: number
  OutstandingAmount: number
  IsReversed: boolean
  PurchaseOrderCancelled: boolean
  CreatedAt: string
  Replayed: boolean
}

export interface VendorAdvancePage {
  Total: number
  Page: number
  PageSize: number
  Items: VendorAdvanceView[]
}

export interface VendorPaymentAllocationInput {
  VendorBillId: string
  Amount: number
}

export interface RecordVendorPaymentRequest {
  VendorId: string
  PaidDate: string
  Amount: number
  CurrencyCode: string
  PaymentReference: string
  /** Required for a non-INR payment; optional evidence otherwise. */
  EvidenceObjectKey: string
  Allocations: VendorPaymentAllocationInput[]
  IdempotencyKey: string
}

export interface VendorPaymentAllocationView {
  VendorBillId: string
  BillNumber: string
  AcceptedValue: number
  AdvanceAdjustedValue: number
  PreviouslyPaidValue: number
  Amount: number
}

export interface VendorPaymentView {
  Id: string
  PaymentNumber: string
  VendorId: string
  VendorCode: string
  VendorName: string
  PaidDate: string
  Amount: number
  CurrencyCode: string
  PaymentReference: string
  EvidenceObjectKey: string
  CreatedAt: string
  Replayed: boolean
  Allocations: VendorPaymentAllocationView[]
}

export interface VendorPaymentPage {
  Total: number
  Page: number
  PageSize: number
  Items: VendorPaymentView[]
}

/** One accepted bill and what is still owed on it. */
export interface VendorPayableView {
  VendorBillId: string
  BillNumber: string
  PurchaseOrderId: string
  PurchaseOrderNumber: string
  VendorId: string
  VendorCode: string
  VendorName: string
  BillDate: string
  AcceptedAt: string
  PaymentTerms: string
  DueDate: string | null
  AcceptedValue: number
  AdvanceAdjustedValue: number
  PaidValue: number
  OutstandingValue: number
  IsOverdue: boolean
  CurrencyCode: string
}

export interface VendorPositionView {
  VendorId: string
  VendorCode: string
  VendorName: string
  OutstandingAdvance: number
  OutstandingBills: number
  NetPayable: number
  CancelledPurchaseOrdersWithOutstandingAdvance: number
  CurrencyCode: string
}

/** POST /bank-advices (multipart: vendorId + file; PDF/JPEG/PNG, ≤ 5 MB). */
export interface VendorBankAdviceView {
  Id: string
  CompanyId: string
  VendorId: string
  VendorCode: string
  VendorName: string
  FileName: string
  ContentType: string
  SizeBytes: number
  ContentSha256: string
  EvidenceObjectKey: string
  CreatedAt: string
  Replayed: boolean
}
