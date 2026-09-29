import { api, authorizedFetch, saveResponseAsFile } from './client'
import type { PagedResponse } from './client'
import type { CustomerPoDetail, CustomerPoLookups, CustomerPoSummary, UpsertCustomerPoRequest } from '../types/customerPo'

const BASE = '/api/v1/sales/customer-pos'

export interface CustomerPoListQuery {
  page: number
  pageSize: number
  search?: string
  /** Exact internal PO record number (e.g. CPO-…); server upper-cases it. */
  poRecordNumber?: string
  /** Exact customer's own PO number. */
  customerPoNumber?: string
  workStatus?: string
  salesType?: string
  serviceMode?: string
  fiscalYear?: string
  sortBy?: string
  sortDirection?: string
}

export function listCustomerPos(query: CustomerPoListQuery): Promise<PagedResponse<CustomerPoSummary>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.search) params.set('search', query.search)
  if (query.poRecordNumber) params.set('poRecordNumber', query.poRecordNumber)
  if (query.customerPoNumber) params.set('customerPoNumber', query.customerPoNumber)
  if (query.workStatus) params.set('workStatus', query.workStatus)
  if (query.salesType) params.set('salesType', query.salesType)
  if (query.serviceMode) params.set('serviceMode', query.serviceMode)
  if (query.fiscalYear) params.set('fiscalYear', query.fiscalYear)
  if (query.sortBy) params.set('sortBy', query.sortBy)
  if (query.sortDirection) params.set('sortDirection', query.sortDirection)
  return api.getPaged<CustomerPoSummary>(`${BASE}?${params.toString()}`)
}

export function getCustomerPo(poRecordNumber: string): Promise<CustomerPoDetail> {
  return api.get<CustomerPoDetail>(`${BASE}/${encodeURIComponent(poRecordNumber)}`)
}

export function getCustomerPoLookups(): Promise<CustomerPoLookups> {
  return api.get<CustomerPoLookups>(`${BASE}/lookups`)
}

export function getNextCustomerPoNumber(): Promise<{ PoRecordNumber: string }> {
  return api.get<{ PoRecordNumber: string }>(`${BASE}/next-number`)
}

export function createCustomerPo(body: UpsertCustomerPoRequest): Promise<{ PoRecordNumber: string; Version: number }> {
  return api.post<{ PoRecordNumber: string; Version: number }>(BASE, body)
}

export type CustomerPoOptionKind = 'SERVICE_MODE' | 'SALES_TYPE'

export function addCustomerPoOption(kind: CustomerPoOptionKind, value: string): Promise<{ Kind: string; Value: string }> {
  return api.post<{ Kind: string; Value: string }>(`${BASE}/options`, { Kind: kind, Value: value })
}

export function updateCustomerPo(poRecordNumber: string, body: UpsertCustomerPoRequest): Promise<CustomerPoDetail> {
  return api.put<CustomerPoDetail>(`${BASE}/${encodeURIComponent(poRecordNumber)}`, body)
}

async function uploadPdf(path: string, file: File, version: number, revisionReason: string): Promise<Record<string, string | number>> {
  const body = new FormData()
  body.set('file', file)
  body.set('version', String(version))
  body.set('revisionReason', revisionReason)
  const response = await authorizedFetch(path, { method: 'POST', body })
  return (await response.json()) as Record<string, string>
}

async function downloadPdf(path: string, fileName: string): Promise<void> {
  const response = await authorizedFetch(path)
  await saveResponseAsFile(response, fileName || 'document.pdf')
}

export function uploadCustomerPoFile(poRecordNumber: string, file: File, version: number, revisionReason: string) {
  return uploadPdf(`${BASE}/${encodeURIComponent(poRecordNumber)}/file`, file, version, revisionReason)
}

export function downloadCustomerPoFile(poRecordNumber: string, fileName: string) {
  return downloadPdf(`${BASE}/${encodeURIComponent(poRecordNumber)}/file`, fileName)
}
