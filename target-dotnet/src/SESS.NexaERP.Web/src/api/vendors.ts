import { api, getStoredToken } from './client'
import type { PagedResponse } from './client'
import type { UpsertVendorRequest, VendorDetail, VendorSummary } from '../types/vendor'

const BASE = '/api/v1/masters/vendors'

export type VendorAction =
  | 'submit'
  | 'approve'
  | 'reject'
  | 'request-clarification'
  | 'request-revision'
  | 'resubmit'
  | 'hold'
  | 'blacklist'
  | 'reactivate'
  | 'deactivate'

export interface VendorListQuery {
  page: number
  pageSize: number
  search?: string
  status?: string
  type?: string
  sortBy?: string
  sortDirection?: string
}

export function listVendors(query: VendorListQuery): Promise<PagedResponse<VendorSummary>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.sortBy) params.set('sortBy', query.sortBy)
  if (query.sortDirection) params.set('sortDirection', query.sortDirection)
  if (query.search) params.set('search', query.search)
  if (query.status) params.set('status', query.status)
  if (query.type) params.set('type', query.type)
  return api.get<PagedResponse<VendorSummary>>(`${BASE}?${params.toString()}`)
}

export function getVendor(vendorCode: string): Promise<VendorDetail> {
  return api.get<VendorDetail>(`${BASE}/${encodeURIComponent(vendorCode)}`)
}

export function createVendor(body: UpsertVendorRequest): Promise<VendorSummary> {
  return api.post<VendorSummary>(BASE, body)
}

export function updateVendor(vendorCode: string, body: UpsertVendorRequest): Promise<VendorDetail> {
  return api.put<VendorDetail>(`${BASE}/${encodeURIComponent(vendorCode)}`, body)
}

export function runVendorAction(vendorCode: string, action: VendorAction, remarks: string, version: number) {
  return api.post<unknown>(`${BASE}/${encodeURIComponent(vendorCode)}/${action}`, {
    Remarks: remarks,
    Version: version,
  })
}

export type VendorAttachmentKind = 'BANK_LEAF' | 'GST_CERTIFICATE' | 'PAN_CARD'

export interface VendorAttachmentInfo {
  Id: string
  Kind: VendorAttachmentKind
  FileName: string
  ContentType: string
  SizeBytes: number
}

export function getNextVendorCode(): Promise<{ VendorCode: string }> {
  return api.get<{ VendorCode: string }>(`${BASE}/next-code`)
}

export async function uploadVendorAttachment(kind: VendorAttachmentKind, file: File): Promise<VendorAttachmentInfo> {
  const body = new FormData()
  body.set('kind', kind)
  body.set('file', file)
  const headers: Record<string, string> = {}
  const token = getStoredToken()
  if (token) headers.Authorization = `Bearer ${token}`
  const response = await fetch(`${BASE}/attachments`, { method: 'POST', body, headers })
  if (!response.ok) {
    let message = `Upload failed (${response.status})`
    try {
      const errorBody = await response.json()
      message = errorBody.Detail || errorBody.message || message
    } catch { /* keep default */ }
    throw new Error(message)
  }
  return (await response.json()) as VendorAttachmentInfo
}

export async function downloadVendorAttachment(attachmentId: string, fileName: string): Promise<void> {
  const headers: Record<string, string> = {}
  const token = getStoredToken()
  if (token) headers.Authorization = `Bearer ${token}`
  const response = await fetch(`${BASE}/attachments/${attachmentId}`, { headers })
  if (!response.ok) throw new Error(`Download failed (${response.status})`)
  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
  URL.revokeObjectURL(url)
}

// Shape stored in the vendor's AttachmentMetadataJson column.
export interface VendorAttachmentMetadata {
  gstCertificate?: { id: string; fileName: string }
  bankLeaf?: { id: string; fileName: string }
  panCard?: { id: string; fileName: string }
}

export function parseAttachmentMetadata(json: string | null): VendorAttachmentMetadata {
  if (!json) return {}
  try {
    return JSON.parse(json) as VendorAttachmentMetadata
  } catch {
    return {}
  }
}

// Shape stored in the vendor's BankMetadataJson column. The API returns it
// only to roles with commercial-view permission (BankMetadata is null
// otherwise), and it may arrive as a JSON string or an object.
export interface VendorBankDetails {
  bankName?: string
  accountHolder?: string
  accountNumber?: string
  ifsc?: string
  branch?: string
}

export function parseBankMetadata(value: unknown): VendorBankDetails {
  if (!value) return {}
  try {
    const parsed = typeof value === 'string' ? JSON.parse(value) : value
    return parsed && typeof parsed === 'object' ? (parsed as VendorBankDetails) : {}
  } catch {
    return {}
  }
}
