import { api } from './client'
import type { CreateVendorBillRequest, VendorBillDecisionRequest, VendorBillPage, VendorBillView } from '../types/vendorBill'

const BASE = '/api/v1/accounts/vendor-bills'

export interface VendorBillListQuery {
  page: number
  pageSize: number
  billNumber?: string
  status?: string
  vendorId?: string
}

export function listVendorBills(query: VendorBillListQuery): Promise<VendorBillPage> {
  const p = new URLSearchParams({ page: String(query.page), pageSize: String(query.pageSize) })
  if (query.billNumber) p.set('billNumber', query.billNumber)
  if (query.status) p.set('status', query.status)
  if (query.vendorId) p.set('vendorId', query.vendorId)
  return api.get<VendorBillPage>(`${BASE}/?${p.toString()}`)
}

export function getVendorBill(id: string): Promise<VendorBillView> {
  return api.get<VendorBillView>(`${BASE}/${encodeURIComponent(id)}`)
}

/** The idempotency key travels in the body (CreateVendorBillRequest.IdempotencyKey), not the header. */
export function createVendorBillFromGrn(goodsReceiptId: string, request: CreateVendorBillRequest): Promise<VendorBillView> {
  return api.post<VendorBillView>(`${BASE}/from-grn/${encodeURIComponent(goodsReceiptId)}`, request)
}

export function acceptVendorBill(id: string, request: VendorBillDecisionRequest): Promise<VendorBillView> {
  return api.post<VendorBillView>(`${BASE}/${encodeURIComponent(id)}/accept`, request)
}

export function rejectVendorBill(id: string, request: VendorBillDecisionRequest): Promise<VendorBillView> {
  return api.post<VendorBillView>(`${BASE}/${encodeURIComponent(id)}/reject`, request)
}

export function reverseVendorBill(id: string, request: VendorBillDecisionRequest): Promise<VendorBillView> {
  return api.post<VendorBillView>(`${BASE}/${encodeURIComponent(id)}/reverse`, request)
}
