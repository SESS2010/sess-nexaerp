// Mirrors SESS.NexaERP.Application/Stores/MaterialIssueContracts.cs (PascalCase wire contract).
//
// Endpoints: MaterialIssueEndpoints.cs —
//   /api/v1/stores/material-issue-requests   (MIR: draft → submitted → approved/rejected, cancel)
//   /api/v1/stores/material-issue-excess     (TD decision on customer-facing excess, per line)
//   /api/v1/stores/material-issues           (scanner-confirmed issue against an approved MIR)
//   /api/v1/stores/material-returns          (engineer returns custody; Stores accepts)
// Every command carries IdempotencyKey in the BODY (unlike GRN create, which uses a header).

/**
 * EfMaterialIssueService.RequestCommands.cs: a job order is mandatory for the
 * three customer-facing situations and forbidden for CONSUMABLE_OFFICE.
 * Customer-facing situations also require DestinationType = JOB_ORDER;
 * CONSUMABLE_OFFICE must target DEPARTMENT or OTHER.
 */
export const MIR_SITUATIONS = [
  'CONSUMABLE_OFFICE',
  'CHAMBER_MANUFACTURE',
  'SERVICE_CUSTOMER_PO',
  'SITE_PROJECT_PO',
] as const
export type MirSituation = (typeof MIR_SITUATIONS)[number]

export const MIR_JOB_SITUATIONS: readonly MirSituation[] = [
  'CHAMBER_MANUFACTURE',
  'SERVICE_CUSTOMER_PO',
  'SITE_PROJECT_PO',
]

export const MIR_PURPOSES = [
  'FACTORY_ASSEMBLY',
  'PROJECT',
  'SERVICE',
  'WARRANTY',
  'DEMO',
  'SALE',
  'FREE_OF_COST',
] as const

export const MIR_DESTINATION_TYPES = ['DEPARTMENT', 'OTHER', 'JOB_ORDER'] as const
export type MirDestinationType = (typeof MIR_DESTINATION_TYPES)[number]

/** Status values the request service writes (TransitionAsync / IssueAsync). */
export const MIR_STATES = [
  'DRAFT',
  'SUBMITTED',
  'APPROVED',
  'REJECTED',
  'CANCELLED',
  'PARTIALLY_FULFILLED',
  'FULFILLED',
] as const

export interface MaterialIssueRequestLineInput {
  ItemId: string
  UomId: string
  Quantity: number
  CustomerPurchaseOrderLineId: string | null
  Remarks: string | null
}

export interface CreateMaterialIssueRequest {
  Purpose: string
  Situation: string
  DestinationType: string
  JobOrderId: string | null
  CustomerId: string | null
  VendorId: string | null
  DestinationDepartmentId: string | null
  DestinationName: string
  RequestingDepartmentId: string
  /** yyyy-MM-dd (DateOnly). */
  RequiredDate: string
  Lines: MaterialIssueRequestLineInput[]
  IdempotencyKey: string
}

export interface UpdateMaterialIssueRequest extends CreateMaterialIssueRequest {
  Version: number
}

export interface MaterialIssueTransitionRequest {
  Version: number
  Reason: string
  IdempotencyKey: string
}

export interface MaterialIssueExcessDecisionRequest {
  Decision: 'APPROVED' | 'REJECTED'
  Reason: string
  IdempotencyKey: string
}

export interface MaterialIssueRequestLineView {
  Id: string
  LineNumber: number
  ItemId: string
  ItemCode: string
  ItemName: string
  UomId: string
  UomCode: string
  RequestedQuantity: number
  RequestedBaseQuantity: number
  CustomerPurchaseOrderLineId: string | null
  EstimatedBomBaseQuantity: number
  ProductionBomBaseQuantity: number
  CustomerPoBaseQuantity: number
  ExcessBaseQuantity: number
  /** e.g. NONE, INTERNAL (notify only) or CUSTOMER_FACING (blocks issue until TD decides). */
  ExcessClassification: string
  TdDecisionPresent: boolean
  Remarks: string | null
}

export interface MaterialIssueRequestView {
  Id: string
  RequestNumber: string
  Purpose: string
  Situation: string
  DestinationType: string
  JobOrderId: string | null
  CustomerId: string | null
  VendorId: string | null
  DestinationDepartmentId: string | null
  DestinationName: string
  RequestingDepartmentId: string
  RequestedByEmployeeId: string
  RequiredDate: string
  Status: string
  Version: number
  Lines: MaterialIssueRequestLineView[]
}

/** Note: Total, not TotalCount — this page shape differs from PagedResponse. */
export interface MaterialIssueRequestPage {
  Total: number
  Page: number
  PageSize: number
  Items: MaterialIssueRequestView[]
}

// --- Material issue (custody transfer) ---

export interface MaterialIssueScan {
  MaterialIssueRequestLineId: string
  ScanCode: string
  InventorySerialId: string | null
  Quantity: number
}

export interface CreateMaterialIssue {
  IdempotencyKey: string
  IssuedToEmployeeId: string
  IssuedAt: string
  Scans: MaterialIssueScan[]
}

export interface MaterialIssueLineView {
  Id: string
  MaterialIssueRequestLineId: string
  LineNumber: number
  ItemId: string
  QuantityBase: number
  OwnershipAccountId: string
  FromCustodyAssignmentId: string
  ToCustodyAssignmentId: string
  InventoryProvenanceLayerId: string
  InventoryLotId: string | null
  InventorySerialId: string | null
  WarehouseConditionLocationId: string
}

export interface MaterialIssueView {
  Id: string
  IssueNumber: string
  MaterialIssueRequestId: string
  JobOrderId: string | null
  IssuedToEmployeeId: string
  IssuedAt: string
  ReturnDueAt: string
  Status: string
  StockPostingBatchId: string | null
  ActorRoleCode: string
  ResolvedRoleAssignmentId: string
  ResolvedRoleAssignmentType: string
  Version: number
  Replayed: boolean
  Lines: MaterialIssueLineView[]
}

export interface OutstandingEngineerCustodyView {
  MaterialIssueId: string
  IssueNumber: string
  JobOrderId: string | null
  EmployeeId: string
  EmployeeCode: string
  IssuedAt: string
  ReturnDueAt: string
  ReturnNotificationDue: boolean
  QuantityBase: number
}

// --- Material return ---

export interface MaterialReturnLineInput {
  MaterialIssueLineId: string
  ScanCode: string
  ReturnedQuantity: number
  ReportedConsumedQuantity: number
  ReportedStillHeldQuantity: number
}

export interface CreateMaterialReturn {
  DeclaredAt: string
  Lines: MaterialReturnLineInput[]
  IdempotencyKey: string
}

export interface AcceptMaterialReturn {
  Version: number
  AcceptedAt: string
  Reason: string
  IdempotencyKey: string
}

export interface MaterialReturnLineView {
  Id: string
  MaterialIssueLineId: string
  LineNumber: number
  ItemId: string
  ReturnedQuantityBase: number
  ReportedConsumedQuantityBase: number
  ReportedStillHeldQuantityBase: number
  ScanCode: string
  InventorySerialId: string | null
}

export interface MaterialReturnView {
  Id: string
  ReturnNumber: string
  MaterialIssueId: string
  ReturnedByEmployeeId: string
  DeclaredAt: string
  Status: string
  AcceptedAt: string | null
  AcceptedByEmployeeId: string | null
  StockPostingBatchId: string | null
  Version: number
  Replayed: boolean
  ActorRoleCode: string
  ResolvedRoleAssignmentId: string
  ResolvedRoleAssignmentType: string
  AcceptedActorRoleCode: string | null
  AcceptedRoleAssignmentId: string | null
  AcceptedRoleAssignmentType: string | null
  Lines: MaterialReturnLineView[]
}

export interface MaterialReturnPage {
  Total: number
  Page: number
  PageSize: number
  Items: MaterialReturnView[]
}
