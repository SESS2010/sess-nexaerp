// Wire shapes for the Purchase module. PascalCase to match the API contract
// (see api/client.ts). Mirrors SESS.NexaERP.Application/Purchase/PurchaseRequisitionContracts.cs.

export const PR_STATUSES = [
  'Draft',
  'Submitted',
  'DepartmentVerified',
  'PendingApproval',
  'Approved',
  'StockCheckPending',
  'FullyAvailable',
  'PartiallyAvailable',
  'NotAvailable',
  'Reserved',
  'PurchaseHandoffCreated',
  'Completed',
  'Rejected',
  'RevisionRequested',
] as const

export const PR_PRIORITIES = ['Low', 'Normal', 'High', 'Urgent'] as const

export interface PurchaseRequisitionSummary {
  Id: string
  PrNumber: string
  OrganizationId: string
  RequestingDepartment: string
  RequesterEmployeeCode: string
  RequestDate: string
  RequiredByDate: string
  Priority: string
  Status: string
  ApprovalRoute: string
  EstimatedTotal: number
  Version: number
}

export interface PurchaseRequisitionLineSummary {
  Id: string
  LineNumber: number
  ItemCode: string
  ItemName: string
  Uom: string
  RequestedQuantity: number
  EstimatedUnitPrice: number
  EstimatedLineTotal: number
  OnHand: number
  ActiveReserved: number
  Available: number
  ReservedQuantity: number
  ShortageQuantity: number
  HandoffQuantity: number
  LineStatus: string
}

export interface PurchaseRequisitionDetail {
  Id: string
  PrNumber: string
  OrganizationId: string
  RequestingDepartment: string
  RequesterEmployeeCode: string
  RequestDate: string
  RequiredByDate: string
  Priority: string
  PurposeJustification: string
  DeliveryWarehouseCode: string
  CostCentre: string | null
  ProjectReference: string | null
  ServiceReference: string | null
  WorkOrderReference: string | null
  CustomerReference: string | null
  CustomerPurchaseOrderId: string | null
  CustomerPoRecordNumber: string | null
  Status: string
  ApprovalRoute: string
  EstimatedTotal: number
  Version: number
  Lines: PurchaseRequisitionLineSummary[]
}

export interface PurchaseRequisitionHistorySummary {
  Id: string
  Action: string
  PreviousStatus: string | null
  NewStatus: string
  Remarks: string
  ActorLoginId: string
  ActorRoleCode: string
  CreatedAt: string
  CorrelationId: string
}

export interface PurchaseRequisitionLineRequest {
  ItemCode: string
  RequestedQuantity: number
  EstimatedUnitPrice: number
  RequiredDate: string
  PreferredWarehouseCode: string | null
  ProjectReference: string | null
  MachineReference: string | null
  ServiceReference: string | null
}

export interface CreatePurchaseRequisitionRequest {
  OrganizationId: string
  RequestingDepartmentCode: string
  RequesterEmployeeCode: string
  RequiredByDate: string
  Priority: string
  PurposeJustification: string
  DeliveryWarehouseCode: string
  CostCentre: string | null
  ProjectReference: string | null
  ServiceReference: string | null
  WorkOrderReference: string | null
  CustomerReference: string | null
  Lines: PurchaseRequisitionLineRequest[]
  CustomerPurchaseOrderId?: string | null
}

export interface UpdatePurchaseRequisitionRequest {
  RequiredByDate: string
  Priority: string
  PurposeJustification: string
  DeliveryWarehouseCode: string
  CostCentre: string | null
  ProjectReference: string | null
  ServiceReference: string | null
  WorkOrderReference: string | null
  CustomerReference: string | null
  Lines: PurchaseRequisitionLineRequest[]
  Version: number
  CustomerPurchaseOrderId?: string | null
}

export interface PurchaseRequisitionActionRequest {
  Remarks: string
  Version: number
  IdempotencyKey?: string | null
}

export interface StockReservationSummary {
  Id: string
  ReservationNumber: string
  PrNumber: string
  LineNumber: number
  ItemCode: string
  WarehouseCode: string
  RackBinCode: string | null
  ReservedQuantity: number
  Status: string
}

export interface PurchaseRequirementHandoffSummary {
  Id: string
  HandoffNumber: string
  PrNumber: string
  LineNumber: number
  ItemCode: string
  WarehouseCode: string
  RackBinCode: string | null
  HandoffQuantity: number
  Status: string
}

/** Dropdown option shared by the department and warehouse pickers. */
export interface PurchaseLookupOption {
  Code: string
  Name: string
}

// --- RFQ (REV869B purchase transactions) ---
// Mirrors SESS.NexaERP.Application/Purchase/Rev869BPurchaseContracts.cs and the
// RequestForQuotation entity returned raw by GET /api/v1/purchase/rfqs/{number}.

export interface Rev869BDocumentResult {
  Id: string
  Number: string
  Status: string
  Version: number
}

export interface RfqSourceLineRequest {
  PurchaseRequirementHandoffId: string
  Quantity: number
}

export interface CreateRfqRequest {
  QuoteDueAt: string
  CurrencyCode: string
  IsSingleSource: boolean
  SingleSourceJustification: string | null
  IdempotencyKey: string
  Lines: RfqSourceLineRequest[]
}

export interface InviteVendorRequest {
  VendorId: string
  Remarks: string
  RfqVersion: number
  IdempotencyKey: string
}

export interface RfqLine {
  Id: string
  LineNumber: number
  PrNumberSnapshot: string
  PrLineNumberSnapshot: number
  ItemCodeSnapshot: string
  ItemNameSnapshot: string
  UomSnapshot: string
  SpecificationSnapshot: string | null
  ApprovedQuantitySnapshot: number
  AlreadyOrderedQuantitySnapshot: number
  OutstandingQuantitySnapshot: number
  RfqQuantity: number
  RequiredDateSnapshot: string
}

export interface RfqDetail {
  Id: string
  RfqNumber: string
  OrganizationId: string
  FinancialYear: string
  QuoteDueAt: string
  CurrencyCode: string
  Status: string
  IsSingleSource: boolean
  SingleSourceJustification: string | null
  IssuedAt: string | null
  IsActive: boolean
  Version: number
  Lines: RfqLine[]
}


// --- Vendor quotation ---

export interface QuotationLineRequest {
  RequestForQuotationLineId: string
  Quantity: number
  UnitRate: number
  DiscountValue: number
  PackingForwarding: number
  Freight: number
  Insurance: number
  OtherCharges: number
  PromisedDeliveryDate: string
  HsnSacCode: string
  SupplierStateCode: string
  PlaceOfSupplyStateCode: string
  VendorRegistrationType: string
  RoundOff: number
}

export interface SubmitQuotationRequest {
  VendorQuoteReference: string
  CurrencyCode: string
  PaymentTerms: string
  DeliveryTerms: string
  WarrantyTerms: string
  RequestLateAuthorization: boolean
  LateAuthorizationRemarks: string | null
  SubmissionSource: string
  ReceivedAt: string
  AttachmentObjectKey: string
  AttachmentSha256: string
  VendorAttestation: string
  InvitationVersion: number
  PreviousQuotationVersion: number | null
  IdempotencyKey: string
  Lines: QuotationLineRequest[]
  HeaderDiscountValue: number
}

export interface TechnicalVerificationRequest {
  VendorQuotationLineId: string
  IsCompliant: boolean
  ComplianceEvidenceJson: string
  Remarks: string
  QuotationVersion: number
  IdempotencyKey: string
}

export const VENDOR_REGISTRATION_TYPES = [
  'Regular',
  'Composition',
  'Unregistered',
  'SEZ',
  'Overseas',
] as const

export const QUOTATION_SUBMISSION_SOURCES = ['Email', 'Portal', 'Hardcopy', 'Fax'] as const

// --- Commercial comparison ---

export interface CreateComparisonRequest {
  RfqNumber: string
  RfqVersion: number
  IdempotencyKey: string
}

export interface RecommendComparisonRequest {
  VendorQuotationId: string
  RecommendationRemarks: string
  SingleSourceJustification: string | null
  Version: number
  IdempotencyKey: string
}

export interface ApprovalActionRequest {
  Remarks: string
  Version: number
  IdempotencyKey: string
}

export interface ComparisonLine {
  Id: string
  VendorQuotationLineId: string
  VendorId?: string
  TechnicalComplianceSnapshot: string
  CommercialSnapshotJson?: string
  DeliverySnapshot: string
  WarrantySnapshot?: string
  PaymentTermsSnapshot?: string
  TotalPayableValue?: number
  IsRecommended: boolean
  RecommendationReason: string | null
}

export interface ComparisonDetail {
  Id: string
  ComparisonNumber: string
  RequestForQuotationId: string
  RecommendedVendorQuotationId?: string | null
  SelectedVendorId?: string | null
  OwnerEmployeeId: string
  CurrencyCode: string
  TotalPayableValue?: number
  ApprovalRoute?: string
  ApprovalCycle?: number
  RequiredApprovalStepCount?: number
  CompletedApprovalStepCount?: number
  Status: string
  IsSingleSource: boolean
  SingleSourceJustification: string | null
  RecommendationRemarks: string | null
  Version: number
  Lines: ComparisonLine[]
}

// --- Purchase order ---

export interface CreatePurchaseOrderRequest {
  ComparisonNumber: string
  ComparisonVersion: number
  IdempotencyKey: string
}

export interface PoActionRequest {
  Remarks: string
  Version: number
  IdempotencyKey: string
}

export interface PoApprovalActionRequest {
  Remarks: string
  Version: number
  ExpectedCurrentVersion: number | null
  IdempotencyKey: string
}

export interface AmendPurchaseOrderRequest {
  AmendmentReason: string
  PaymentTerms: string
  DeliveryTerms: string
  WarrantyTerms: string
  Version: number
  IdempotencyKey: string
}

export interface CancelPurchaseOrderRequest {
  Reason: string
  Version: number
  IdempotencyKey: string
}

export interface PurchaseOrderLine {
  Id: string
  LineNumber: number
  ItemId?: string
  ItemCodeSnapshot: string
  ItemNameSnapshot: string
  UomSnapshot: string
  OrderedQuantity: number
  ApprovedOutstandingQuantitySnapshot?: number
  UnitRate?: number
  TotalPayableValue?: number
}

export interface PurchaseOrderDetail {
  Id: string
  PoNumber: string
  RevisionNumber: number
  IsCurrentVersion: boolean
  FinancialYear?: string
  VendorId?: string
  Status: string
  CurrencyCode: string
  ApprovalRoute?: string
  RequiredApprovalStepCount?: number
  CompletedApprovalStepCount?: number
  TaxableValue?: number
  DiscountValue?: number
  TaxValue?: number
  PackingForwarding?: number
  Freight?: number
  Insurance?: number
  OtherCharges?: number
  RoundOff?: number
  TotalPayableValue?: number
  PaymentTermsSnapshot?: string
  DeliveryTermsSnapshot?: string
  WarrantyTermsSnapshot?: string
  AmendmentReason?: string | null
  IssuedAt: string | null
  CancelledAt: string | null
  CancellationReason: string | null
  Version: number
  Lines: PurchaseOrderLine[]
}

/** Document kinds tracked in the browser-local "recent" list. */
export type RecentDocKind = 'rfq' | 'quotation' | 'comparison' | 'purchase-order'

export interface RecentDoc {
  Number: string
  SeenAt: string
}
