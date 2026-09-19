import { api, ApiError, getStoredToken } from './client'
import type {
  RecordVendorAdvanceRequest, RecordVendorPaymentRequest, ReverseVendorAdvanceRequest,
  VendorAdvancePage, VendorAdvancePurchaseOrderOption, VendorAdvanceView, VendorBankAdviceView,
  VendorPayableView, VendorPaymentPage, VendorPaymentView, VendorPositionView,
} from '../types/vendorPayment'

const BASE = '/api/v1/accounts/vendor-financial-evidence'

const q = (params: Record<string, string | number | boolean | undefined | null>) => {
  const p = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) if (value !== undefined && value !== null && value !== '') p.set(key, String(value))
  const s = p.toString()
  return s ? `?${s}` : ''
}

/** Issued POs that can still take an advance, with what is already advanced against each. */
export function listAdvancePurchaseOrders(vendorId?: string): Promise<VendorAdvancePurchaseOrderOption[]> {
  return api.get<VendorAdvancePurchaseOrderOption[]>(`${BASE}/advance-purchase-orders${q({ vendorId })}`)
}

export function listVendorAdvances(query: { vendorId?: string; purchaseOrderId?: string; outstandingOnly?: boolean; page: number; pageSize: number }): Promise<VendorAdvancePage> {
  return api.get<VendorAdvancePage>(`${BASE}/advances${q(query)}`)
}

export function recordVendorAdvance(body: RecordVendorAdvanceRequest): Promise<VendorAdvanceView> {
  return api.post<VendorAdvanceView>(`${BASE}/advances`, body)
}

export function reverseVendorAdvance(id: string, body: ReverseVendorAdvanceRequest): Promise<VendorAdvanceView> {
  return api.post<VendorAdvanceView>(`${BASE}/advances/${encodeURIComponent(id)}/reverse`, body)
}

export function listVendorPayments(query: { vendorId?: string; page: number; pageSize: number }): Promise<VendorPaymentPage> {
  return api.get<VendorPaymentPage>(`${BASE}/payments${q(query)}`)
}

export function recordVendorPayment(body: RecordVendorPaymentRequest): Promise<VendorPaymentView> {
  return api.post<VendorPaymentView>(`${BASE}/payments`, body)
}

/** Accepted bills with an outstanding balance (advance-adjusted, net of earlier payments). */
export function listVendorPayables(query: { vendorId?: string; overdueOnly?: boolean } = {}): Promise<VendorPayableView[]> {
  return api.get<VendorPayableView[]>(`${BASE}/payables${q(query)}`)
}

export function listVendorPositions(): Promise<VendorPositionView[]> {
  return api.get<VendorPositionView[]>(`${BASE}/vendor-positions`)
}

/**
 * POST /bank-advices — multipart `vendorId` + `file` (PDF, JPEG or PNG, ≤ 5 MB),
 * Idempotency-Key in the header. The returned EvidenceObjectKey is what an
 * advance or payment cites.
 */
export async function uploadBankAdvice(vendorId: string, file: File, idempotencyKey: string): Promise<VendorBankAdviceView> {
  const body = new FormData()
  body.set('vendorId', vendorId)
  body.set('file', file)
  const headers = new Headers({ 'Idempotency-Key': idempotencyKey })
  const token = getStoredToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)
  const response = await fetch(`${BASE}/bank-advices`, { method: 'POST', body, headers })
  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`
    try {
      const problem = await response.json()
      message = problem.Detail || problem.message || problem.Title || message
    } catch { /* keep the status text */ }
    throw new ApiError(response.status, message)
  }
  return (await response.json()) as VendorBankAdviceView
}

export function bankAdviceDownloadUrl(id: string): string {
  return `${BASE}/bank-advices/${encodeURIComponent(id)}/content`
}
