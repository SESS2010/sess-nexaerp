import { api } from './client'
import type {
  CreateGateEntryRequest,
  FinalizeGateEntryRequest,
  GateEntryListResult,
  GateEntryResult,
  SourcePurchaseOrder,
  UpdateGateEntryRequest,
} from '../types/stores'

const BASE = '/api/v1/stores/gate-entries'

export function newIdempotencyKey(prefix: string): string {
  return `${prefix}-${crypto.randomUUID()}`
}

export interface GateEntryListQuery {
  page: number
  pageSize: number
  /** Exact document number; the server upper-cases it. */
  gateEntryNumber?: string
  purchaseOrderNumber?: string
  vendorId?: string
  from?: string
  to?: string
  state?: string
  /**
   * Lowercase row-DTO field name (gateentrynumber, purchaseordernumber,
   * vendorname, arrivedat, status). The gate-entry list endpoint does not
   * read these yet, so the server ignores them until its sort support lands.
   */
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

export function listGateEntries(query: GateEntryListQuery): Promise<GateEntryListResult> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.gateEntryNumber) params.set('gateEntryNumber', query.gateEntryNumber)
  if (query.purchaseOrderNumber) params.set('purchaseOrderNumber', query.purchaseOrderNumber)
  if (query.vendorId) params.set('vendorId', query.vendorId)
  if (query.from) params.set('from', query.from)
  if (query.to) params.set('to', query.to)
  if (query.state) params.set('state', query.state)
  if (query.sortBy) params.set('sortBy', query.sortBy)
  if (query.sortDirection) params.set('sortDirection', query.sortDirection)
  return api.get<GateEntryListResult>(`${BASE}/?${params.toString()}`)
}

export function getGateEntry(id: string): Promise<GateEntryResult> {
  return api.get<GateEntryResult>(`${BASE}/${encodeURIComponent(id)}`)
}

/**
 * Create requires an `Idempotency-Key` HTTP header, not a body field. Replaying
 * the same key with different data is rejected as a conflict by the API, so the
 * caller must mint a fresh key per genuine attempt and reuse it on retry.
 */
export function createGateEntry(
  body: CreateGateEntryRequest,
  idempotencyKey: string,
): Promise<GateEntryResult> {
  return api.post<GateEntryResult>(`${BASE}/`, body, { 'Idempotency-Key': idempotencyKey })
}

export function updateGateEntry(
  id: string,
  body: UpdateGateEntryRequest,
): Promise<GateEntryResult> {
  return api.put<GateEntryResult>(`${BASE}/${encodeURIComponent(id)}`, body)
}

export function finalizeGateEntry(
  id: string,
  body: FinalizeGateEntryRequest,
): Promise<GateEntryResult> {
  return api.post<GateEntryResult>(`${BASE}/${encodeURIComponent(id)}/finalize`, body)
}

/** Gate Entry can only be raised against a current, Issued purchase order. */
export function getSourcePurchaseOrder(poNumber: string): Promise<SourcePurchaseOrder> {
  return api.get<SourcePurchaseOrder>(
    `/api/v1/purchase/purchase-orders/${encodeURIComponent(poNumber)}`,
  )
}
