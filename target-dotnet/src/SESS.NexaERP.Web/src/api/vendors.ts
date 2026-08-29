import { api } from './client'
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
}

export function listVendors(query: VendorListQuery): Promise<PagedResponse<VendorSummary>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
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
