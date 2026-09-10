import { api } from './client'
import type { PagedResponse } from './client'
import type {
  ActualBomView,
  ComponentFitmentSummary,
  ConfirmComponentFitmentRequest,
  ConfirmJobOrderRequest,
  CreateFatCustodyExplanationRequest,
  CreateJobOrderRequest,
  FatCustodyExplanationView,
  FatReconciliationView,
  JobOrderCustomerPoLineLookup,
  JobOrderFatReadinessView,
  JobOrderHistoryView,
  JobOrderSummary,
  JobOrderView,
  ReconcileJobOrderFatRequest,
  ReverseComponentFitmentRequest,
} from '../types/production'

// JobOrderEndpoints.cs, JobOrderFatReadinessEndpoints.cs, FitmentActualBomEndpoints.cs.
const JOB_ORDERS = '/api/v1/production/job-orders'
const FITMENTS = '/api/v1/production/component-fitments'

// --- Job Orders (page production.job-orders) ---

export interface JobOrderListQuery {
  page: number
  pageSize: number
  search?: string
  status?: string
}

/** Paged with TotalCount/PageNumber/PageSize (PagedResponse), unlike the MIR list. */
export function listJobOrders(query: JobOrderListQuery): Promise<PagedResponse<JobOrderSummary>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.search) params.set('search', query.search)
  if (query.status) params.set('status', query.status)
  return api.get<PagedResponse<JobOrderSummary>>(`${JOB_ORDERS}/?${params.toString()}`)
}

/**
 * Candidate Customer PO lines (page action create). Production has no
 * sales.customer-po grant, so the Job Order page carries its own lookup.
 * At most 50 rows, filtered on PO numbers, customer, description or item code.
 */
/** GET /production/job-orders/customer-po-lines as served by main 2268658
 *  (JobOrderCustomerPoLineView in JobOrderContracts.cs). No search parameter;
 *  filtering is client-side. */
interface JobOrderCustomerPoLineView {
  Id: string
  CustomerPurchaseOrderId: string
  CustomerPoRecordNumber: string
  CustomerPoNumber: string
  CustomerName: string
  LineNumber: number
  ItemId: string
  ItemCode: string
  ItemName: string
  Quantity: number
  CreatedJobOrderCount: number
}

export async function lookupJobOrderCustomerPoLines(search?: string): Promise<JobOrderCustomerPoLineLookup[]> {
  const rows = await api.get<JobOrderCustomerPoLineView[]>(`${JOB_ORDERS}/customer-po-lines`)
  const term = (search ?? '').trim().toLowerCase()
  return rows
    .filter((row) => !term || [row.CustomerPoRecordNumber, row.CustomerPoNumber, row.CustomerName, row.ItemName, row.ItemCode]
      .some((value) => (value ?? '').toLowerCase().includes(term)))
    .map((row) => ({
      CustomerPurchaseOrderLineId: row.Id,
      CustomerPurchaseOrderId: row.CustomerPurchaseOrderId,
      PoRecordNumber: row.CustomerPoRecordNumber,
      CustomerPoNumber: row.CustomerPoNumber,
      CustomerName: row.CustomerName,
      WorkStatus: '',
      RevisionNumber: 0,
      SlNo: row.LineNumber,
      Description: row.ItemName,
      ItemId: row.ItemId,
      ItemCode: row.ItemCode,
      Quantity: row.Quantity,
      Uom: null,
      TakenOrdinals: [],
      CreatedJobOrderCount: row.CreatedJobOrderCount ?? 0,
    }))
}

export function getJobOrder(id: string): Promise<JobOrderView> {
  return api.get<JobOrderView>(`${JOB_ORDERS}/${encodeURIComponent(id)}`)
}

export function getJobOrderHistory(id: string): Promise<JobOrderHistoryView[]> {
  return api.get<JobOrderHistoryView[]>(`${JOB_ORDERS}/${encodeURIComponent(id)}/history`)
}

/** PRODUCTION_COORDINATOR or PRODUCTION_MANAGER; lands in PENDING_ACCOUNTS. */
export function createJobOrder(body: CreateJobOrderRequest): Promise<JobOrderView> {
  return api.post<JobOrderView>(`${JOB_ORDERS}/`, body)
}

/** ACCOUNTS_ASSISTANT or ACCOUNTS_MANAGER, never the initiator; PENDING_ACCOUNTS → OPEN. */
export function confirmJobOrderAccounts(id: string, body: ConfirmJobOrderRequest): Promise<JobOrderView> {
  return api.post<JobOrderView>(`${JOB_ORDERS}/${encodeURIComponent(id)}/accounts-confirm`, body)
}

// --- FAT readiness (page production.fat-readiness) ---

export function getFatReadiness(jobOrderId: string): Promise<JobOrderFatReadinessView> {
  return api.get<JobOrderFatReadinessView>(`${JOB_ORDERS}/${encodeURIComponent(jobOrderId)}/fat-readiness/`)
}

/** Production or Service explains LOST / SCRAPPED custody; needs a FULL assignment. */
export function explainFatCustody(jobOrderId: string, body: CreateFatCustodyExplanationRequest): Promise<FatCustodyExplanationView> {
  return api.post<FatCustodyExplanationView>(`${JOB_ORDERS}/${encodeURIComponent(jobOrderId)}/fat-readiness/custody-explanations`, body)
}

/** QC_MANAGER or DESIGN_ENGINEER runs the reconciliation; the result is BLOCKED or READY. */
export function reconcileFatReadiness(jobOrderId: string, body: ReconcileJobOrderFatRequest): Promise<FatReconciliationView> {
  return api.post<FatReconciliationView>(`${JOB_ORDERS}/${encodeURIComponent(jobOrderId)}/fat-readiness/reconcile`, body)
}

// --- Component fitments and the generated Actual BOM (page production.component-fitments) ---

export interface ComponentFitmentListQuery {
  page: number
  pageSize: number
  jobOrderId?: string
  /** Hide reversed fitments. */
  activeOnly?: boolean
}

export function listComponentFitments(query: ComponentFitmentListQuery): Promise<PagedResponse<ComponentFitmentSummary>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.jobOrderId) params.set('jobOrderId', query.jobOrderId)
  if (query.activeOnly !== undefined) params.set('activeOnly', String(query.activeOnly))
  return api.get<PagedResponse<ComponentFitmentSummary>>(`${FITMENTS}/?${params.toString()}`)
}

export function getComponentFitment(id: string): Promise<ComponentFitmentSummary> {
  return api.get<ComponentFitmentSummary>(`${FITMENTS}/${encodeURIComponent(id)}`)
}

/** CONSUMPTION_OUT is posted here, never at issue. */
export function confirmComponentFitment(body: ConfirmComponentFitmentRequest): Promise<ComponentFitmentSummary> {
  return api.post<ComponentFitmentSummary>(`${FITMENTS}/`, body)
}

/** PRODUCTION_MANAGER or SERVICE_MANAGER (page action cancel). Self-reversal is allowed but flagged. */
export function reverseComponentFitment(id: string, body: ReverseComponentFitmentRequest): Promise<ComponentFitmentSummary> {
  return api.post<ComponentFitmentSummary>(`${FITMENTS}/${encodeURIComponent(id)}/reverse`, body)
}

/** 404 until the first fitment generates it. */
export function getActualBom(jobOrderId: string): Promise<ActualBomView> {
  return api.get<ActualBomView>(`${FITMENTS}/job-orders/${encodeURIComponent(jobOrderId)}/actual-bom`)
}
