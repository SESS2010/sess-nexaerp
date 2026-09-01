import { api } from './client'
import type { PagedResponse } from './client'
import type {
  CreatePurchaseRequisitionRequest,
  PurchaseLookupOption,
  PurchaseRequirementHandoffSummary,
  PurchaseRequisitionActionRequest,
  PurchaseRequisitionDetail,
  PurchaseRequisitionHistorySummary,
  PurchaseRequisitionSummary,
  StockReservationSummary,
  UpdatePurchaseRequisitionRequest,
  CreateRfqRequest,
  InviteVendorRequest,
  Rev869BDocumentResult,
  RfqDetail,
  AmendPurchaseOrderRequest,
  ApprovalActionRequest,
  CancelPurchaseOrderRequest,
  ComparisonDetail,
  CreateComparisonRequest,
  CreatePurchaseOrderRequest,
  PoActionRequest,
  PoApprovalActionRequest,
  PurchaseOrderDetail,
  RecentDoc,
  RecentDocKind,
  RecommendComparisonRequest,
  SubmitQuotationRequest,
  TechnicalVerificationRequest,
} from '../types/purchase'
import type { ItemSummary } from '../types/item'

const PR_BASE = '/api/v1/purchase/requisitions'

export interface PurchaseRequisitionListQuery {
  page: number
  pageSize: number
  search?: string
  status?: string
  sortBy?: string
  sortDirection?: string
}

export function listPurchaseRequisitions(
  query: PurchaseRequisitionListQuery,
): Promise<PagedResponse<PurchaseRequisitionSummary>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.search) params.set('search', query.search)
  if (query.status) params.set('status', query.status)
  if (query.sortBy) params.set('sortBy', query.sortBy)
  if (query.sortDirection) params.set('sortDirection', query.sortDirection)
  return api.get<PagedResponse<PurchaseRequisitionSummary>>(`${PR_BASE}?${params.toString()}`)
}

export function getPurchaseRequisition(prNumber: string): Promise<PurchaseRequisitionDetail> {
  return api.get<PurchaseRequisitionDetail>(`${PR_BASE}/${encodeURIComponent(prNumber)}`)
}

export function createPurchaseRequisition(
  body: CreatePurchaseRequisitionRequest,
): Promise<PurchaseRequisitionDetail> {
  return api.post<PurchaseRequisitionDetail>(PR_BASE, body)
}

export function updatePurchaseRequisition(
  prNumber: string,
  body: UpdatePurchaseRequisitionRequest,
): Promise<PurchaseRequisitionDetail> {
  return api.put<PurchaseRequisitionDetail>(`${PR_BASE}/${encodeURIComponent(prNumber)}`, body)
}

// Workflow transitions. Every one of them is optimistic-concurrency controlled:
// the caller must send the Version it last read, or the API returns 409.
export type PurchaseRequisitionAction =
  | 'submit'
  | 'verify'
  | 'approve'
  | 'reject'
  | 'request-revision'
  | 'resubmit'
  | 'cancel'
  | 'hold'

export function actOnPurchaseRequisition(
  prNumber: string,
  action: PurchaseRequisitionAction,
  body: PurchaseRequisitionActionRequest,
): Promise<PurchaseRequisitionDetail> {
  return api.post<PurchaseRequisitionDetail>(
    `${PR_BASE}/${encodeURIComponent(prNumber)}/${action}`,
    body,
  )
}

export function getPurchaseRequisitionStatusHistory(
  prNumber: string,
): Promise<PurchaseRequisitionHistorySummary[]> {
  return api.get<PurchaseRequisitionHistorySummary[]>(
    `${PR_BASE}/${encodeURIComponent(prNumber)}/status-history`,
  )
}

export function getPurchaseRequisitionApprovalHistory(
  prNumber: string,
): Promise<PurchaseRequisitionHistorySummary[]> {
  return api.get<PurchaseRequisitionHistorySummary[]>(
    `${PR_BASE}/${encodeURIComponent(prNumber)}/approval-history`,
  )
}

// Both of these return the standard paged envelope, not a bare array.
export function listStockReservations(
  page = 1,
  pageSize = 100,
): Promise<PagedResponse<StockReservationSummary>> {
  return api.get<PagedResponse<StockReservationSummary>>(
    `${PR_BASE}/reservations?page=${page}&pageSize=${pageSize}`,
  )
}

export function listPurchaseHandoffs(
  page = 1,
  pageSize = 100,
): Promise<PagedResponse<PurchaseRequirementHandoffSummary>> {
  return api.get<PagedResponse<PurchaseRequirementHandoffSummary>>(
    `${PR_BASE}/handoffs?page=${page}&pageSize=${pageSize}`,
  )
}

// --- Lookups the PR form needs. These live on other modules; there is no
// dedicated purchase lookups endpoint, so we borrow the master ones. ---

interface EmployeeMasterLookups {
  Departments: PurchaseLookupOption[]
  Skills: PurchaseLookupOption[]
  Designations: PurchaseLookupOption[]
}

export async function listDepartments(): Promise<PurchaseLookupOption[]> {
  const lookups = await api.get<EmployeeMasterLookups>('/api/v1/employees/lookups')
  return lookups.Departments ?? []
}

interface WarehouseSummary {
  Id: string
  WarehouseCode: string
  Name: string
  WarehouseType: string
  Location: string | null
  Status: string
  ApprovalStatus: string
  IsActive: boolean
  Version: number
}

export async function listWarehouseOptions(): Promise<PurchaseLookupOption[]> {
  const page = await api.get<PagedResponse<WarehouseSummary>>(
    '/api/v1/inventory/warehouses?page=1&pageSize=200',
  )
  return page.Items.filter((w) => w.IsActive).map((w) => ({ Code: w.WarehouseCode, Name: w.Name }))
}

export async function searchItems(search: string): Promise<PurchaseLookupOption[]> {
  const params = new URLSearchParams({ page: '1', pageSize: '25' })
  if (search) params.set('search', search)
  const page = await api.get<PagedResponse<ItemSummary>>(
    `/api/v1/inventory/items?${params.toString()}`,
  )
  return page.Items.map((i) => ({ Code: i.ItemCode, Name: i.Name }))
}

// --- RFQ ---------------------------------------------------------------
// NOTE: the API exposes no list endpoint for RFQs. Everything below works off a
// known RFQ number; the shared recentDocs() store below is a browser-local
// stopgap so a user can find one again after a page reload.

const RFQ_BASE = '/api/v1/purchase/rfqs'
export function newIdempotencyKey(prefix: string): string {
  return `${prefix}-${crypto.randomUUID()}`
}

export function createRfq(body: CreateRfqRequest): Promise<Rev869BDocumentResult> {
  return api.post<Rev869BDocumentResult>(RFQ_BASE, body)
}

export function getRfq(rfqNumber: string): Promise<RfqDetail> {
  return api.get<RfqDetail>(`${RFQ_BASE}/${encodeURIComponent(rfqNumber)}`)
}

export function inviteVendorToRfq(
  rfqNumber: string,
  body: InviteVendorRequest,
): Promise<Rev869BDocumentResult> {
  return api.post<Rev869BDocumentResult>(
    `${RFQ_BASE}/${encodeURIComponent(rfqNumber)}/vendors`,
    body,
  )
}

interface VendorRow {
  Id: string
  VendorCode: string
  Name: string
  IsActive: boolean
  ApprovalStatus: string
}

export interface VendorOption {
  Id: string
  VendorCode: string
  Name: string
}

export async function listVendorOptions(search: string): Promise<VendorOption[]> {
  const params = new URLSearchParams({ page: '1', pageSize: '50' })
  if (search) params.set('search', search)
  const page = await api.get<PagedResponse<VendorRow>>(`/api/v1/masters/vendors?${params.toString()}`)
  return page.Items.filter((vendor) => vendor.IsActive).map((vendor) => ({
    Id: vendor.Id,
    VendorCode: vendor.VendorCode,
    Name: vendor.Name,
  }))
}

// --- Browser-local recent-document store -------------------------------
// Generalises the RFQ stopgap to every REV869B document, none of which has a
// list endpoint. Replace with real list APIs as soon as they exist.

function recentKey(kind: RecentDocKind): string {
  return `nexaerp.purchase.recent.${kind}`
}

export function recentDocs(kind: RecentDocKind): RecentDoc[] {
  try {
    const raw = localStorage.getItem(recentKey(kind))
    return raw ? (JSON.parse(raw) as RecentDoc[]) : []
  } catch {
    return []
  }
}

export function rememberDoc(kind: RecentDocKind, number: string): void {
  try {
    const kept = recentDocs(kind).filter((entry) => entry.Number !== number)
    const next = [{ Number: number, SeenAt: new Date().toISOString() }, ...kept].slice(0, 25)
    localStorage.setItem(recentKey(kind), JSON.stringify(next))
  } catch {
    // storage unavailable; the shortcut list is simply lost
  }
}

export function forgetDoc(kind: RecentDocKind, number: string): void {
  try {
    localStorage.setItem(
      recentKey(kind),
      JSON.stringify(recentDocs(kind).filter((entry) => entry.Number !== number)),
    )
  } catch {
    // ignore
  }
}

// --- Vendor quotation --------------------------------------------------

export function submitQuotation(
  invitationId: string,
  body: SubmitQuotationRequest,
): Promise<Rev869BDocumentResult> {
  return api.post<Rev869BDocumentResult>(
    `/api/v1/purchase/rfq-invitations/${encodeURIComponent(invitationId)}/quotations`,
    body,
  )
}

export function verifyQuotationTechnically(
  quotationNumber: string,
  body: TechnicalVerificationRequest,
): Promise<Rev869BDocumentResult> {
  return api.post<Rev869BDocumentResult>(
    `/api/v1/purchase/quotations/${encodeURIComponent(quotationNumber)}/technical-verifications`,
    body,
  )
}

export function quotationAttachmentUrl(quotationNumber: string): string {
  return `/api/v1/purchase/quotations/${encodeURIComponent(quotationNumber)}/attachment`
}

// --- Commercial comparison ---------------------------------------------

const COMPARISON_BASE = '/api/v1/purchase/comparisons'

export function createComparison(body: CreateComparisonRequest): Promise<Rev869BDocumentResult> {
  return api.post<Rev869BDocumentResult>(COMPARISON_BASE, body)
}

export function getComparison(comparisonNumber: string): Promise<ComparisonDetail> {
  return api.get<ComparisonDetail>(`${COMPARISON_BASE}/${encodeURIComponent(comparisonNumber)}`)
}

export function recommendComparison(
  comparisonNumber: string,
  body: RecommendComparisonRequest,
): Promise<Rev869BDocumentResult> {
  return api.post<Rev869BDocumentResult>(
    `${COMPARISON_BASE}/${encodeURIComponent(comparisonNumber)}/recommend`,
    body,
  )
}

export type ComparisonAction = 'approve' | 'reject' | 'request-revision' | 'resubmit'

export function actOnComparison(
  comparisonNumber: string,
  action: ComparisonAction,
  body: ApprovalActionRequest,
): Promise<Rev869BDocumentResult> {
  return api.post<Rev869BDocumentResult>(
    `${COMPARISON_BASE}/${encodeURIComponent(comparisonNumber)}/${action}`,
    body,
  )
}

// --- Purchase order ----------------------------------------------------

const PO_BASE = '/api/v1/purchase/purchase-orders'

export function createPurchaseOrder(
  body: CreatePurchaseOrderRequest,
): Promise<Rev869BDocumentResult> {
  return api.post<Rev869BDocumentResult>(PO_BASE, body)
}

export function getPurchaseOrder(poNumber: string): Promise<PurchaseOrderDetail> {
  return api.get<PurchaseOrderDetail>(`${PO_BASE}/${encodeURIComponent(poNumber)}`)
}

export type PoSimpleAction = 'submit' | 'issue'

export function actOnPurchaseOrder(
  poNumber: string,
  action: PoSimpleAction,
  body: PoActionRequest,
): Promise<Rev869BDocumentResult> {
  return api.post<Rev869BDocumentResult>(
    `${PO_BASE}/${encodeURIComponent(poNumber)}/${action}`,
    body,
  )
}

export type PoApprovalAction = 'approve' | 'reject'

export function approvePurchaseOrder(
  poNumber: string,
  action: PoApprovalAction,
  body: PoApprovalActionRequest,
): Promise<Rev869BDocumentResult> {
  return api.post<Rev869BDocumentResult>(
    `${PO_BASE}/${encodeURIComponent(poNumber)}/${action}`,
    body,
  )
}

export function amendPurchaseOrder(
  poNumber: string,
  body: AmendPurchaseOrderRequest,
): Promise<Rev869BDocumentResult> {
  return api.post<Rev869BDocumentResult>(`${PO_BASE}/${encodeURIComponent(poNumber)}/amend`, body)
}

export function cancelPurchaseOrder(
  poNumber: string,
  body: CancelPurchaseOrderRequest,
): Promise<Rev869BDocumentResult> {
  return api.post<Rev869BDocumentResult>(`${PO_BASE}/${encodeURIComponent(poNumber)}/cancel`, body)
}
