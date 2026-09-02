import { api, getStoredToken } from './client'
import type { PagedResponse } from './client'
import type {
  ItemDetail, ItemSummary, ItemVendorLink, ReferenceLookup, SubcategoryLookup,
  UpsertItemRequest, VendorSuppliedItem,
} from '../types/item'

const BASE = '/api/v1/inventory/items'
const MASTERS = '/api/v1/masters'

export type ItemAction =
  | 'submit' | 'approve' | 'reject' | 'request-clarification' | 'request-revision'
  | 'resubmit' | 'hold' | 'reactivate' | 'deactivate'

export interface ItemListQuery {
  page: number
  pageSize: number
  search?: string
  status?: string
  category?: string
  sortBy?: string
  sortDirection?: string
}

export function listItems(query: ItemListQuery): Promise<PagedResponse<ItemSummary>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.sortBy) params.set('sortBy', query.sortBy)
  if (query.sortDirection) params.set('sortDirection', query.sortDirection)
  if (query.search) params.set('search', query.search)
  if (query.status) params.set('status', query.status)
  if (query.category) params.set('category', query.category)
  return api.get<PagedResponse<ItemSummary>>(`${BASE}?${params.toString()}`)
}

export function getItem(itemCode: string): Promise<ItemDetail> {
  return api.get<ItemDetail>(`${BASE}/${encodeURIComponent(itemCode)}`)
}

export function createItem(body: UpsertItemRequest): Promise<ItemDetail> {
  return api.post<ItemDetail>(BASE, body)
}

export function updateItem(itemCode: string, body: UpsertItemRequest): Promise<ItemDetail> {
  return api.put<ItemDetail>(`${BASE}/${encodeURIComponent(itemCode)}`, body)
}

export function runItemAction(itemCode: string, action: ItemAction, remarks: string, version: number) {
  return api.post<unknown>(`${BASE}/${encodeURIComponent(itemCode)}/${action}`, { Remarks: remarks, Version: version })
}

export function getItemVendors(itemCode: string): Promise<ItemVendorLink[]> {
  return api.get<ItemVendorLink[]>(`${BASE}/${encodeURIComponent(itemCode)}/vendors`)
}

export function setItemVendors(itemCode: string, vendorCodes: string[]) {
  return api.put<unknown>(`${BASE}/${encodeURIComponent(itemCode)}/vendors`, { VendorCodes: vendorCodes })
}

export function getVendorItems(vendorCode: string): Promise<VendorSuppliedItem[]> {
  return api.get<VendorSuppliedItem[]>(`${MASTERS}/vendors/${encodeURIComponent(vendorCode)}/items`)
}

export async function uploadItemImage(itemCode: string, file: File): Promise<void> {
  const body = new FormData()
  body.set('file', file)
  const headers: Record<string, string> = {}
  const token = getStoredToken()
  if (token) headers.Authorization = `Bearer ${token}`
  const response = await fetch(`${BASE}/${encodeURIComponent(itemCode)}/image`, { method: 'POST', body, headers })
  if (!response.ok) {
    let message = `Image upload failed (${response.status})`
    try {
      const errorBody = await response.json()
      message = errorBody.Detail || errorBody.message || message
    } catch { /* keep default */ }
    throw new Error(message)
  }
}

/** Fetches the item image as an object URL (authenticated); null when absent. */
export async function fetchItemImageUrl(itemCode: string): Promise<string | null> {
  const headers: Record<string, string> = {}
  const token = getStoredToken()
  if (token) headers.Authorization = `Bearer ${token}`
  const response = await fetch(`${BASE}/${encodeURIComponent(itemCode)}/image`, { headers })
  if (!response.ok) return null
  return URL.createObjectURL(await response.blob())
}

// Reference lookups for the item form (categories, subcategories, uoms, manufacturers).
const lookupParams = 'page=1&pageSize=200&isActive=true'

export function listItemCategories(): Promise<PagedResponse<ReferenceLookup>> {
  return api.get<PagedResponse<ReferenceLookup>>(`${MASTERS}/item-categories?${lookupParams}`)
}

export function listItemSubcategories(categoryId: string): Promise<PagedResponse<SubcategoryLookup>> {
  return api.get<PagedResponse<SubcategoryLookup>>(`${MASTERS}/item-subcategories?${lookupParams}&categoryId=${categoryId}`)
}

export function listUoms(): Promise<PagedResponse<ReferenceLookup>> {
  return api.get<PagedResponse<ReferenceLookup>>(`${MASTERS}/uoms?${lookupParams}`)
}

// Inline quick-adds for the master-backed dropdowns.
export function createItemCategory(code: string, name: string): Promise<ReferenceLookup> {
  return api.post<ReferenceLookup>(`${MASTERS}/item-categories`, { Code: code, Name: name })
}

export function createItemSubcategory(categoryId: string, code: string, name: string): Promise<SubcategoryLookup> {
  return api.post<SubcategoryLookup>(`${MASTERS}/item-subcategories`, { CategoryId: categoryId, Code: code, Name: name })
}

export function createUom(code: string, name: string, measurementDimension: string): Promise<ReferenceLookup> {
  return api.post<ReferenceLookup>(`${MASTERS}/uoms`, { Code: code, Name: name, MeasurementDimension: measurementDimension })
}
