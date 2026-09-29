// Dashboard wire types, field for field from
// target-dotnet/docs/installation/dashboard-frontend-contract.md (22 Sep 2026).
// A `| null` here is the contract's C# `?`: JSON null is allowed and means
// withheld or unknown — never zero.

export type Guid = string
/** YYYY-MM-DD, company-local calendar. */
export type DateOnly = string
/** ISO timestamp from the server. */
export type DateTimeOffset = string

export interface DashboardCurrencyAmount {
  /** Purchase workload can redact the currency to null. */
  Currency: string | null
  Amount: number
}

// ---------- GET /api/v1/dashboards/purchase/workload ----------

export const PURCHASE_WORKLOAD_QUEUES = [
  'pr-department-verification',
  'pr-approval',
  'pr-stock-check',
  'rfq-no-quotation',
  'quotation-technical-verification',
  'comparison-decision',
  'po-approved-unissued',
] as const
export type PurchaseWorkloadQueue = (typeof PURCHASE_WORKLOAD_QUEUES)[number]

export interface PurchaseWorkloadRequest {
  queue?: PurchaseWorkloadQueue | null
  /** Only with queue=pr-approval; use returned ApprovalBands route values. */
  approvalRoute?: string | null
  page?: number
  pageSize?: number
}

export interface PurchaseWorkloadBand {
  ApprovalRoute: string
  Count: number
  OldestAgeDays: number | null
  Amounts: DashboardCurrencyAmount[]
}

export interface PurchaseWorkloadTile {
  Key: string
  Title: string
  /** READY or ACCESS_DENIED. Denied is not an empty queue. */
  State: 'READY' | 'ACCESS_DENIED' | string
  Count: number | null
  OldestAgeDays: number | null
  CommercialValuesVisible: boolean
  Amounts: DashboardCurrencyAmount[]
  UnvaluedDocumentCount: number | null
  ApprovalBands: PurchaseWorkloadBand[]
  Coverage: string | null
}

export interface PurchaseWorkloadRow {
  Queue: string
  DocumentId: Guid
  DocumentType: string
  DocumentNumber: string
  Status: string
  WaitingSince: DateTimeOffset
  AgeDays: number
  ApprovalRoute: string | null
  NextApproverEmployeeId: Guid | null
  NextApproverEmployeeCode: string | null
  NextApproverRole: string | null
  ResponsibilityIssue: string | null
  Currency: string | null
  Value: number | null
  Vendors: string[]
  PendingLineCount: number | null
  /** API resource path, NOT a browser route. */
  DetailPath: string
}

export interface PurchaseWorkloadPage {
  CompanyCode: string
  GeneratedAt: DateTimeOffset
  TimeZone: string
  Tiles: PurchaseWorkloadTile[]
  Queue: string | null
  ApprovalRoute: string | null
  Page: number
  PageSize: number
  TotalRows: number
  Rows: PurchaseWorkloadRow[]
}

// ---------- GET /api/v1/dashboards/purchase/open-orders ----------

export interface PurchaseOpenOrdersRequest {
  vendorId?: Guid | null
  currency?: string | null
  rootPurchaseOrderId?: Guid | null
  overdueOnly?: boolean
  page?: number
  pageSize?: number
}

export interface PurchaseOpenOrdersFilters {
  VendorId: Guid | null
  Currency: string | null
  RootPurchaseOrderId: Guid | null
  OverdueOnly: boolean
  Page: number
  PageSize: number
}

export interface PurchaseOpenOrderAmount {
  Currency: string
  PoCount: number
  Value: number
  OverduePoCount: number | null
  OverdueValue: number | null
}

export interface PurchaseOpenOrderIssue {
  RootPurchaseOrderId: Guid
  PoNumber: string
  /** CURRENT_REVISION_UNAVAILABLE, CANCELLED_UNISSUED_AMENDMENT, LINE_PROVENANCE_INCONSISTENT or RECEIPT_QUANTITY_INCONSISTENT. */
  Code: string
}

export type DeliveryState = 'CONFIRMATION_REQUIRED' | 'OVERDUE' | 'WITHIN_COMMITMENT' | string

export interface PurchaseOpenOrderRow {
  PurchaseOrderId: Guid
  RootPurchaseOrderId: Guid
  PoNumber: string
  RevisionNumber: number
  CurrentRevisionNumber: number
  CurrentStatus: string
  FirstIssuedAt: DateTimeOffset
  IssuedAt: DateTimeOffset
  AgeDays: number
  VendorId: Guid
  VendorCode: string
  VendorName: string
  LineId: Guid
  ItemId: Guid
  ItemCode: string
  ItemName: string
  Uom: string
  OrderedQuantity: number
  ReceivedQuantity: number
  RemainingQuantity: number
  Currency: string
  Value: number
  QuotedDeliveryDate: DateOnly
  CommittedDeliveryDate: DateOnly | null
  DeliveryTerms: string
  DaysLate: number | null
  DeliveryState: DeliveryState
}

export interface PurchaseOpenOrdersPage {
  CompanyCode: string
  GeneratedAt: DateTimeOffset
  TimeZone: string
  Basis: string
  /** False is not zero outstanding: source integrity is incomplete. */
  Complete: boolean
  OpenPoCount: number | null
  OldestAgeDays: number | null
  DeliveryComplete: boolean
  OverduePoCount: number | null
  DeliveryDateUnconfirmedPoCount: number
  SourceIssues: PurchaseOpenOrderIssue[]
  Amounts: PurchaseOpenOrderAmount[] | null
  Filters: PurchaseOpenOrdersFilters
  TotalRows: number
  Rows: PurchaseOpenOrderRow[]
}

// ---------- GET /api/v1/dashboards/purchase/obligations ----------

export type PurchaseObligationQueue = 'grni' | 'vendor-advances'

export interface PurchaseObligationsRequest {
  queue?: PurchaseObligationQueue | null
  vendorId?: Guid | null
  currency?: string | null
  documentId?: Guid | null
  page?: number
  pageSize?: number
}

export interface PurchaseObligationsFilters {
  Queue: string | null
  VendorId: Guid | null
  Currency: string | null
  DocumentId: Guid | null
  Page: number
  PageSize: number
}

export interface PurchaseObligationAmount {
  Currency: string
  DocumentCount: number
  LineCount: number
  Value: number
  OldestAgeDays: number | null
}

export interface PurchaseObligationTile {
  Key: string
  Title: string
  Basis: string
  Count: number
  OldestAgeDays: number | null
  Amounts: PurchaseObligationAmount[]
}

export interface PurchaseObligationVendor {
  Queue: string
  VendorId: Guid
  VendorCode: string
  VendorName: string
  Currency: string
  DocumentCount: number
  Value: number
  OldestAgeDays: number | null
}

export interface PurchaseObligationRow {
  Queue: string
  DocumentId: Guid
  DocumentNumber: string
  LineId: Guid | null
  PurchaseOrderId: Guid
  RootPurchaseOrderId: Guid
  PoNumber: string
  VendorId: Guid
  VendorCode: string
  VendorName: string
  ItemId: Guid | null
  ItemCode: string | null
  ItemName: string | null
  Uom: string | null
  SourceDate: DateOnly
  AgeDays: number
  Quantity: number | null
  Currency: string
  Value: number
  UnitRate: number | null
  OriginalAmount: number | null
  AdjustedAmount: number | null
}

export interface PurchaseObligationsPage {
  CompanyCode: string
  GeneratedAt: DateTimeOffset
  TimeZone: string
  Tiles: PurchaseObligationTile[]
  Vendors: PurchaseObligationVendor[]
  Filters: PurchaseObligationsFilters
  TotalRows: number
  Rows: PurchaseObligationRow[]
}

// ---------- GET /api/v1/dashboards/purchase/spending ----------

export type PurchaseSpendingPeriodKey = 'month' | 'quarter' | 'financial-year' | 'twelve-months'

export interface PurchaseSpendingRequest {
  period?: PurchaseSpendingPeriodKey
  /** First day of a month; only with period=month, current or preceding eleven months. */
  month?: DateOnly | null
  vendorId?: Guid | null
  categoryId?: Guid | null
  billId?: Guid | null
  currency?: string | null
  page?: number
  pageSize?: number
}

export interface PurchaseSpendingFilters {
  Period: string
  Month: DateOnly | null
  VendorId: Guid | null
  CategoryId: Guid | null
  Currency: string | null
  BillId: Guid | null
  Page: number
  PageSize: number
}

export interface PurchaseSpendingAmount {
  Currency: string
  /** Signed billed payable INCLUDING embedded tax — not ex-tax material cost. */
  MaterialValue: number
  AllocatedCharges: number
  Amount: number
  PoCount: number
  BillCount: number
}

export interface PurchaseSpendingPeriod {
  Key: string
  FromDate: DateOnly
  ToDate: DateOnly
  Amounts: PurchaseSpendingAmount[]
}

export interface PurchaseSpendingGroup {
  Id: Guid
  Code: string
  Name: string
  Currency: string
  MaterialValue: number
  AllocatedCharges: number
  Amount: number
  PoCount: number
  BillCount: number
}

export interface PurchaseSpendingRow {
  BillId: Guid
  BillLineId: Guid
  BillNumber: string
  /** ACCEPTED adds value; REVERSED subtracts. */
  Event: 'ACCEPTED' | 'REVERSED' | string
  EventDate: DateOnly
  PurchaseOrderId: Guid
  RootPurchaseOrderId: Guid
  PoNumber: string
  VendorId: Guid
  VendorCode: string
  VendorName: string
  CategoryId: Guid
  CategoryCode: string
  ItemId: Guid
  ItemCode: string
  ItemName: string
  Uom: string
  Quantity: number
  Currency: string
  MaterialValue: number
  AllocatedCharges: number
  Amount: number
}

export interface PurchaseSpendingPage {
  CompanyCode: string
  GeneratedAt: DateTimeOffset
  TimeZone: string
  Basis: string
  Periods: PurchaseSpendingPeriod[]
  TopVendors: PurchaseSpendingGroup[]
  Categories: PurchaseSpendingGroup[]
  MonthlyTrend: PurchaseSpendingPeriod[]
  FromDate: DateOnly
  ToDate: DateOnly
  Filters: PurchaseSpendingFilters
  TotalRows: number
  Rows: PurchaseSpendingRow[]
}

// ---------- GET /api/v1/dashboards/stores/workload ----------

export type StoresWorkloadQueue = 'gate-no-grn' | 'mir-approval' | 'mir-unissued'

export interface StoresWorkloadRequest {
  queue?: StoresWorkloadQueue | null
  documentId?: Guid | null
  page?: number
  pageSize?: number
}

export interface StoresWorkloadFilters {
  Queue: string | null
  DocumentId: Guid | null
  Page: number
  PageSize: number
}

export interface StoresWorkloadTile {
  Key: string
  Title: string
  /** READY or ACCESS_DENIED. Denied is not an empty queue. */
  State: 'READY' | 'ACCESS_DENIED' | string
  Count: number | null
  OldestAgeDays: number | null
  Coverage: string
}

export interface StoresWorkloadRow {
  Queue: string
  DocumentId: Guid
  /** GATE_ENTRY or MIR. */
  DocumentType: string
  DocumentNumber: string
  Status: string
  WaitingSince: DateTimeOffset
  AgeDays: number
  PendingLineCount: number
  VendorId: Guid | null
  VendorName: string | null
  /** Roles eligible to approve; not a named assignment. */
  EligibleApprovalRoles: string[]
  /** Currently always null: the MIR workflow has no named approver. */
  AssignedApproverEmployeeId: Guid | null
  ResponsibilityIssue: string | null
  /** API resource path, NOT a browser route. */
  DetailPath: string
}

export interface StoresWorkloadPage {
  CompanyCode: string
  GeneratedAt: DateTimeOffset
  TimeZone: string
  Tiles: StoresWorkloadTile[]
  Filters: StoresWorkloadFilters
  TotalRows: number
  Rows: StoresWorkloadRow[]
}

// ---------- GET /api/v1/dashboards/stores/qc-stock ----------

/** Case-sensitive on the wire. */
export type StoresQcStockQueue = 'QC_HOLD' | 'PENDING_RETURNABLE_DC'

export interface StoresQcStockRequest {
  queue?: StoresQcStockQueue | null
  /** A GRN id. */
  documentId?: Guid | null
  page?: number
  pageSize?: number
}

export interface StoresQcStockFilters {
  Queue: string | null
  DocumentId: Guid | null
  Page: number
  PageSize: number
}

export interface StoresQcStockValue {
  Currency: string
  ReceiptProvisionalValue: number
}

export interface StoresQcStockTile {
  Key: string
  /** Distinct GRN lines, not allocation rows or units. */
  LineCount: number
  OverdueLineCount: number
  OldestReceiptAgeDays: number | null
  /** null = commercial access withheld; [] = permitted but empty. */
  Values: StoresQcStockValue[] | null
}

export interface StoresQcStockRow {
  Queue: string
  DocumentId: Guid
  DocumentNumber: string
  LineId: Guid
  /** One GRN line can occupy several rows. */
  AllocationId: Guid
  ItemId: Guid
  ItemCode: string
  ItemName: string
  Uom: string
  Quantity: number
  WarehouseId: Guid | null
  RackBinId: Guid | null
  OwnershipAccountId: Guid
  CustodyAssignmentId: Guid
  ProvenanceLayerId: Guid
  SerialId: Guid | null
  ReceivedAt: DateTimeOffset
  QcDueAt: DateTimeOffset
  /** Server's verdict; never recomputed from the browser clock. */
  IsOverdue: boolean
  ReceiptAgeDays: number
  Currency: string
  ReceiptProvisionalValue: number | null
  /** API resource path, NOT a browser route. */
  DetailPath: string
}

export interface StoresQcStockPage {
  CompanyCode: string
  GeneratedAt: DateTimeOffset
  TimeZone: string
  CanViewCommercialValues: boolean
  ValueBasis: string
  Tiles: StoresQcStockTile[]
  Filters: StoresQcStockFilters
  TotalRows: number
  Rows: StoresQcStockRow[]
}

// ---------- Errors ----------

/** Codes the dashboards render as their own states. */
export const DASHBOARD_ERROR_CODES = {
  accessDenied: 'DASHBOARD_ACCESS_DENIED',
  sourceInconsistent: 'DASHBOARD_SOURCE_INCONSISTENT',
  requestInvalid: 'DASHBOARD_REQUEST_INVALID',
  authenticationRequired: 'AUTHENTICATION_REQUIRED',
} as const
