import { api, authorizedFetch } from './client'
import type { PagedResponse } from './client'
import type { CustomerDetail, CustomerSummary, UpsertCustomerRequest } from '../types/customer'

const BASE = '/api/v1/masters/customers'

export type CustomerAction =
  | 'submit'
  | 'approve'
  | 'reject'
  | 'request-clarification'
  | 'request-revision'
  | 'resubmit'
  | 'hold'
  | 'reactivate'
  | 'deactivate'

export interface CustomerListQuery {
  page: number
  pageSize: number
  search?: string
  status?: string
  sortBy?: string
  sortDirection?: string
}

export function listCustomers(query: CustomerListQuery): Promise<PagedResponse<CustomerSummary>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.sortBy) params.set('sortBy', query.sortBy)
  if (query.sortDirection) params.set('sortDirection', query.sortDirection)
  if (query.search) params.set('search', query.search)
  if (query.status) params.set('status', query.status)
  return api.getPaged<CustomerSummary>(`${BASE}?${params.toString()}`)
}

export function getCustomer(customerCode: string): Promise<CustomerDetail> {
  return api.get<CustomerDetail>(`${BASE}/${encodeURIComponent(customerCode)}`)
}

export function getNextCustomerCode(): Promise<{ CustomerCode: string }> {
  return api.get<{ CustomerCode: string }>(`${BASE}/next-code`)
}

export function createCustomer(body: UpsertCustomerRequest): Promise<CustomerSummary> {
  return api.post<CustomerSummary>(BASE, body)
}

export function updateCustomer(customerCode: string, body: UpsertCustomerRequest): Promise<CustomerDetail> {
  return api.put<CustomerDetail>(`${BASE}/${encodeURIComponent(customerCode)}`, body)
}

export function runCustomerAction(customerCode: string, action: CustomerAction, remarks: string, version: number) {
  return api.post<unknown>(`${BASE}/${encodeURIComponent(customerCode)}/${action}`, {
    Remarks: remarks,
    Version: version,
  })
}

export type CustomerAttachmentKind = 'GST_CERTIFICATE' | 'BANK_LEAF' | 'MSME_CERTIFICATE' | 'PAN_CARD'

export interface CustomerAttachmentInfo {
  Id: string
  Kind: CustomerAttachmentKind
  FileName: string
  ContentType: string
  SizeBytes: number
}

export async function uploadCustomerAttachment(kind: CustomerAttachmentKind, file: File): Promise<CustomerAttachmentInfo> {
  const body = new FormData()
  body.set('kind', kind)
  body.set('file', file)
  const response = await authorizedFetch(`${BASE}/attachments`, { method: 'POST', body })
  return (await response.json()) as CustomerAttachmentInfo
}

export async function downloadCustomerAttachment(attachmentId: string, fileName: string): Promise<void> {
  const response = await authorizedFetch(`${BASE}/attachments/${attachmentId}`)
  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
  URL.revokeObjectURL(url)
}

// Shape stored in the customer's AttachmentMetadataJson column.
export interface CustomerAttachmentMetadata {
  gstCertificate?: { id: string; fileName: string }
  bankLeaf?: { id: string; fileName: string }
  msmeCertificate?: { id: string; fileName: string }
  panCard?: { id: string; fileName: string }
}

export function parseCustomerAttachmentMetadata(json: string | null): CustomerAttachmentMetadata {
  if (!json) return {}
  try {
    return JSON.parse(json) as CustomerAttachmentMetadata
  } catch {
    return {}
  }
}
