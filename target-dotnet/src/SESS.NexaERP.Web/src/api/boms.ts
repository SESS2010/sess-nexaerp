import { ApiError, api, getStoredToken } from './client'
import type { PagedResponse } from './client'
import type {
  CreateEstimatedBomRequest,
  CreateProductionBomRequest,
  EstimatedBomActionRequest,
  EstimatedBomHistoryView,
  EstimatedBomSummary,
  EstimatedBomView,
  NewEstimatedBomRevisionRequest,
  NewProductionBomRevisionRequest,
  PinProductionBomRevisionRequest,
  ProductionBomActionRequest,
  ProductionBomView,
  ReplaceEstimatedBomLinesRequest,
  ReplaceProductionBomRequest,
} from '../types/bom'

// EstimatedBomEndpoints.cs and ProductionEngineeringEndpoints.cs.
const ESTIMATED = '/api/v1/design/estimated-boms'
const PRODUCTION = '/api/v1/production/boms'

// --- Estimated BOM (page design.estimated-bom) ---

export interface EstimatedBomListQuery {
  page: number
  pageSize: number
  search?: string
  status?: string
}

export function listEstimatedBoms(query: EstimatedBomListQuery): Promise<PagedResponse<EstimatedBomSummary>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.search) params.set('search', query.search)
  if (query.status) params.set('status', query.status)
  return api.getPaged<EstimatedBomSummary>(`${ESTIMATED}/?${params.toString()}`)
}

export function getEstimatedBom(bomNumber: string): Promise<EstimatedBomView> {
  return api.get<EstimatedBomView>(`${ESTIMATED}/${encodeURIComponent(bomNumber)}`)
}

export function getEstimatedBomHistory(bomNumber: string): Promise<EstimatedBomHistoryView[]> {
  return api.get<EstimatedBomHistoryView[]>(`${ESTIMATED}/${encodeURIComponent(bomNumber)}/history`)
}

export function createEstimatedBom(body: CreateEstimatedBomRequest): Promise<EstimatedBomView> {
  return api.post<EstimatedBomView>(`${ESTIMATED}/`, body)
}

export function replaceEstimatedBomLines(bomNumber: string, body: ReplaceEstimatedBomLinesRequest): Promise<EstimatedBomView> {
  return api.put<EstimatedBomView>(`${ESTIMATED}/${encodeURIComponent(bomNumber)}`, body)
}

export type BomTransition = 'submit' | 'approve'

export function transitionEstimatedBom(bomNumber: string, transition: BomTransition, body: EstimatedBomActionRequest): Promise<EstimatedBomView> {
  return api.post<EstimatedBomView>(`${ESTIMATED}/${encodeURIComponent(bomNumber)}/${transition}`, body)
}

export function createEstimatedBomRevision(bomNumber: string, body: NewEstimatedBomRevisionRequest): Promise<EstimatedBomView> {
  return api.post<EstimatedBomView>(`${ESTIMATED}/${encodeURIComponent(bomNumber)}/revisions`, body)
}

async function authorizedFetch(path: string, init?: RequestInit): Promise<Response> {
  const headers = new Headers(init?.headers)
  const token = getStoredToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)
  const response = await fetch(path, { ...init, headers })
  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`
    try {
      const body = await response.json()
      message = body.Detail || body.message || body.Title || message
    } catch { /* keep the status text */ }
    // ApiError so ErrorAlert renders a 403/409 the same way as api.* calls.
    throw new ApiError(response.status, message)
  }
  return response
}

/** GET /workbook/template (page action download) — saves the xlsx the import expects. */
export async function downloadEstimatedBomTemplate(): Promise<void> {
  const response = await authorizedFetch(`${ESTIMATED}/workbook/template`)
  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = 'estimated-bom-template.xlsx'
  anchor.click()
  URL.revokeObjectURL(url)
}

/** POST /workbook/import — multipart with one `file`; the idempotency key travels as a header here. */
export async function importEstimatedBomWorkbook(file: File, idempotencyKey: string): Promise<EstimatedBomView> {
  const body = new FormData()
  body.set('file', file)
  const response = await authorizedFetch(`${ESTIMATED}/workbook/import`, {
    method: 'POST',
    body,
    headers: { 'Idempotency-Key': idempotencyKey },
  })
  return (await response.json()) as EstimatedBomView
}

// --- Production BOM (page production.production-bom) ---

/** Not paged: the endpoint returns every Production BOM of the company. */
export function listProductionBoms(): Promise<ProductionBomView[]> {
  return api.get<ProductionBomView[]>(`${PRODUCTION}/`)
}

export function getProductionBom(bomNumber: string): Promise<ProductionBomView> {
  return api.get<ProductionBomView>(`${PRODUCTION}/${encodeURIComponent(bomNumber)}`)
}

/** Copies the job's approved Estimated BOM revision into DRAFT revision 1. */
export function createProductionBom(body: CreateProductionBomRequest): Promise<ProductionBomView> {
  return api.post<ProductionBomView>(`${PRODUCTION}/`, body)
}

export function replaceProductionBomLines(bomNumber: string, body: ReplaceProductionBomRequest): Promise<ProductionBomView> {
  return api.put<ProductionBomView>(`${PRODUCTION}/${encodeURIComponent(bomNumber)}`, body)
}

export function transitionProductionBom(bomNumber: string, transition: BomTransition, body: ProductionBomActionRequest): Promise<ProductionBomView> {
  return api.post<ProductionBomView>(`${PRODUCTION}/${encodeURIComponent(bomNumber)}/${transition}`, body)
}

export function createProductionBomRevision(bomNumber: string, body: NewProductionBomRevisionRequest): Promise<ProductionBomView> {
  return api.post<ProductionBomView>(`${PRODUCTION}/${encodeURIComponent(bomNumber)}/revisions`, body)
}

export function pinProductionBomRevision(bomNumber: string, body: PinProductionBomRevisionRequest): Promise<ProductionBomView> {
  return api.post<ProductionBomView>(`${PRODUCTION}/${encodeURIComponent(bomNumber)}/pin`, body)
}
