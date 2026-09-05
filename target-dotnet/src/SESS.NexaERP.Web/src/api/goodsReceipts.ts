import { api } from './client'
import type {
  CreateGoodsReceiptRequest,
  FinalizeGoodsReceiptRequest,
  GoodsReceiptListResult,
  GoodsReceiptResult,
  ReverseGoodsReceiptRequest,
  UpdateGoodsReceiptRequest,
} from '../types/goodsReceipt'

const BASE = '/api/v1/stores/goods-receipts'

export interface GoodsReceiptListQuery {
  page: number
  pageSize: number
  grnNumber?: string
  gateEntryNumber?: string
  vendorId?: string
  status?: string
  /**
   * Lowercase row-DTO field name (grnnumber, gateentrynumber,
   * purchaseordernumber, vendorname, vendorbilldate, receivedat, status).
   * The GRN list endpoint does not read these yet, so the server ignores them
   * until its sort support lands.
   */
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

export function listGoodsReceipts(query: GoodsReceiptListQuery): Promise<GoodsReceiptListResult> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.grnNumber) params.set('grnNumber', query.grnNumber)
  if (query.gateEntryNumber) params.set('gateEntryNumber', query.gateEntryNumber)
  if (query.vendorId) params.set('vendorId', query.vendorId)
  if (query.status) params.set('status', query.status)
  if (query.sortBy) params.set('sortBy', query.sortBy)
  if (query.sortDirection) params.set('sortDirection', query.sortDirection)
  return api.get<GoodsReceiptListResult>(`${BASE}/?${params.toString()}`)
}

export function getGoodsReceipt(id: string): Promise<GoodsReceiptResult> {
  return api.get<GoodsReceiptResult>(`${BASE}/${encodeURIComponent(id)}`)
}

/**
 * Create carries the idempotency key as an `Idempotency-Key` HTTP header;
 * finalize and reverse carry it as a body field instead. That asymmetry is the
 * shipped contract (StoresGoodsReceiptEndpoints.cs), not a client choice.
 * Mint a fresh key per genuine attempt and reuse it only on network retry.
 */
export function createGoodsReceipt(
  body: CreateGoodsReceiptRequest,
  idempotencyKey: string,
): Promise<GoodsReceiptResult> {
  return api.post<GoodsReceiptResult>(`${BASE}/`, body, { 'Idempotency-Key': idempotencyKey })
}

export function updateGoodsReceipt(
  id: string,
  body: UpdateGoodsReceiptRequest,
): Promise<GoodsReceiptResult> {
  return api.put<GoodsReceiptResult>(`${BASE}/${encodeURIComponent(id)}`, body)
}

export function finalizeGoodsReceipt(
  id: string,
  body: FinalizeGoodsReceiptRequest,
): Promise<GoodsReceiptResult> {
  return api.post<GoodsReceiptResult>(`${BASE}/${encodeURIComponent(id)}/finalize`, body)
}

/** The only path back from FINALIZED. Produces a REVERSAL document and posting batch. */
export function reverseGoodsReceipt(
  id: string,
  body: ReverseGoodsReceiptRequest,
): Promise<GoodsReceiptResult> {
  return api.post<GoodsReceiptResult>(`${BASE}/${encodeURIComponent(id)}/reverse`, body)
}
