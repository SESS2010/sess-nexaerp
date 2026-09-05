// QC API adapter for the live Stores Slice 3 endpoints (QcEndpoints.cs).
// The session-local mock that stood in while the backend was 404 is gone;
// every call here reaches the server.

import { api, type PagedResponse } from './client'
import type {
  ApproveInventoryConcessionRequest,
  CorrectQcInspectionRequest,
  CreateInventoryConcessionRequest,
  InventoryConcessionResult,
  RejectInventoryConcessionRequest,
  ReverseInventoryConcessionRequest,
  FinalizeQcInspectionRequest,
  QcInspectionResult,
  QcQueueItem,
  WarehouseConditionLocation,
} from '../types/qc'

const BASE = '/api/v1/qc'

export interface QcQueueQuery {
  page: number
  pageSize: number
}

/** Lot allocations in QC_HOLD with no inspection yet, oldest receipt first. */
export function listQcQueue(query: QcQueueQuery): Promise<PagedResponse<QcQueueItem>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  return api.get<PagedResponse<QcQueueItem>>(`${BASE}/queue?${params.toString()}`)
}

/**
 * The queue has no filter by allocation, so a page refresh on the inspection
 * screen re-finds its row by walking the queue. The cap is a runaway guard.
 */
export async function findQcQueueItem(allocationId: string): Promise<QcQueueItem | null> {
  for (let page = 1; page <= 20; page++) {
    const result = await listQcQueue({ page, pageSize: 100 })
    const hit = result.Items.find((item) => item.GoodsReceiptLineLotAllocationId === allocationId)
    if (hit) return hit
    if (page * 100 >= result.TotalCount) break
  }
  return null
}

export function getQcInspection(inspectionNumber: string): Promise<QcInspectionResult> {
  return api.get<QcInspectionResult>(`${BASE}/inspections/${encodeURIComponent(inspectionNumber)}`)
}

/**
 * One POST creates the inspection, its INITIAL revision and the stock posting.
 * The Idempotency-Key header is mandatory: reuse the same key when retrying the
 * same data (the server replays and sets Replayed=true); mint a new key when the
 * data changes, or the server answers 409 "reused with different QC data".
 */
export function finalizeQcInspection(
  body: FinalizeQcInspectionRequest,
  idempotencyKey: string,
): Promise<QcInspectionResult> {
  return api.post<QcInspectionResult>(`${BASE}/inspections`, body, { 'Idempotency-Key': idempotencyKey })
}

/** Reverses the finalized revision's posting and finalizes a CORRECTION revision in its place. */
export function correctQcInspection(
  inspectionNumber: string,
  body: CorrectQcInspectionRequest,
  idempotencyKey: string,
): Promise<QcInspectionResult> {
  return api.post<QcInspectionResult>(
    `${BASE}/inspections/${encodeURIComponent(inspectionNumber)}/corrections`,
    body,
    { 'Idempotency-Key': idempotencyKey },
  )
}

/**
 * Effective AVAILABLE condition locations — the only valid targets for
 * accepted quantity. Served by the REV869A configuration endpoints, so the
 * caller needs masters.warehouse-condition-locations:View as well as the QC
 * page permission.
 */
export function listAvailableConditionLocations(): Promise<WarehouseConditionLocation[]> {
  const params = new URLSearchParams()
  params.set('conditionCode', 'AVAILABLE')
  params.set('effectiveOnly', 'true')
  return api.get<WarehouseConditionLocation[]>(
    `/api/v1/rev869a/configuration/warehouse-condition-locations?${params.toString()}`,
  )
}

/* ------------------------------------------------------------------ */
/* Concessions. Create/approve/reverse need an Idempotency-Key header;  */
/* reject does not. Approve/reject/reverse are TECHNICAL_DIRECTOR only   */
/* on the server (RequireTechnicalDirector) and also need the page       */
/* permission qc.inspection-policies:approve (reject/approve) or :cancel */
/* (reverse).                                                            */
/* ------------------------------------------------------------------ */

export function getConcession(concessionNumber: string): Promise<InventoryConcessionResult> {
  return api.get<InventoryConcessionResult>(`${BASE}/concessions/${encodeURIComponent(concessionNumber)}`)
}

export function createConcession(
  body: CreateInventoryConcessionRequest,
  idempotencyKey: string,
): Promise<InventoryConcessionResult> {
  return api.post<InventoryConcessionResult>(`${BASE}/concessions`, body, { 'Idempotency-Key': idempotencyKey })
}

export function approveConcession(
  concessionNumber: string,
  body: ApproveInventoryConcessionRequest,
  idempotencyKey: string,
): Promise<InventoryConcessionResult> {
  return api.post<InventoryConcessionResult>(
    `${BASE}/concessions/${encodeURIComponent(concessionNumber)}/approve`,
    body,
    { 'Idempotency-Key': idempotencyKey },
  )
}

export function rejectConcession(
  concessionNumber: string,
  body: RejectInventoryConcessionRequest,
): Promise<InventoryConcessionResult> {
  return api.post<InventoryConcessionResult>(`${BASE}/concessions/${encodeURIComponent(concessionNumber)}/reject`, body)
}

export function reverseConcession(
  concessionNumber: string,
  body: ReverseInventoryConcessionRequest,
  idempotencyKey: string,
): Promise<InventoryConcessionResult> {
  return api.post<InventoryConcessionResult>(
    `${BASE}/concessions/${encodeURIComponent(concessionNumber)}/reverse`,
    body,
    { 'Idempotency-Key': idempotencyKey },
  )
}
