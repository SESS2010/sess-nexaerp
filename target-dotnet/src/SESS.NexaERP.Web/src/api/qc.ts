// QC API adapter. The QC backend (Slice 3) is not live yet — every route here
// 404s today. Per the contract's frontend checklist, planned endpoints are
// mocked BEHIND THIS ADAPTER so replacing the mock with live HTTP does not
// change any screen model: the first queue call probes the real endpoint, and
// only a 404 flips the session into mock mode (surfaced by isQcMockMode(), and
// as a banner on every QC page). Nothing in mock mode touches the server.

import { api, ApiError } from './client'
import type {
  CorrectQcInspectionRequest,
  FinalizeQcRevisionRequest,
  QcInspectionResult,
  QcQueueItem,
  QcQueueResult,
  QcRevision,
  UpdateQcRevisionRequest,
} from '../types/qc'

const BASE = '/api/v1/stores/qc'

let mockMode = false

/** True once a QC call has fallen back to the local mock (backend not live). */
export function isQcMockMode(): boolean {
  return mockMode
}

export interface QcQueueQuery {
  page: number
  pageSize: number
  categoryCode?: string
  overdue?: boolean
}

export async function listQcQueue(query: QcQueueQuery): Promise<QcQueueResult> {
  if (mockMode) return mockListQueue(query)
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.categoryCode) params.set('categoryCode', query.categoryCode)
  if (query.overdue) params.set('overdue', 'true')
  try {
    return await api.get<QcQueueResult>(`${BASE}/queue?${params.toString()}`)
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      mockMode = true
      return mockListQueue(query)
    }
    throw error
  }
}

export async function getQcInspection(id: string): Promise<QcInspectionResult> {
  if (mockMode) return mockGet(id)
  return api.get<QcInspectionResult>(`${BASE}/inspections/${encodeURIComponent(id)}`)
}

export async function createQcInspection(goodsReceiptLineId: string): Promise<QcInspectionResult> {
  if (mockMode) return mockCreate(goodsReceiptLineId)
  return api.post<QcInspectionResult>(
    `/api/v1/stores/goods-receipt-lines/${encodeURIComponent(goodsReceiptLineId)}/qc-inspection`,
    { InspectorBasis: 'QC_MANAGER', FallbackReason: null },
  )
}

export async function updateQcRevision(
  revisionId: string,
  body: UpdateQcRevisionRequest,
): Promise<QcInspectionResult> {
  if (mockMode) return mockUpdate(revisionId, body)
  return api.put<QcInspectionResult>(`${BASE}/revisions/${encodeURIComponent(revisionId)}`, body)
}

export async function finalizeQcRevision(
  revisionId: string,
  body: FinalizeQcRevisionRequest,
): Promise<QcInspectionResult> {
  if (mockMode) return mockFinalize(revisionId, body)
  return api.post<QcInspectionResult>(
    `${BASE}/revisions/${encodeURIComponent(revisionId)}/finalize-and-post`,
    body,
  )
}

export async function correctQcInspection(
  id: string,
  body: CorrectQcInspectionRequest,
): Promise<QcInspectionResult> {
  if (mockMode) return mockCorrect(id, body)
  return api.post<QcInspectionResult>(`${BASE}/inspections/${encodeURIComponent(id)}/corrections`, body)
}

/* ------------------------------------------------------------------ */
/* Mock — session-local, deliberately small, mirrors the lifecycle:    */
/* queue → create INITIAL DRAFT → PUT → finalize-and-post → correction */
/* ------------------------------------------------------------------ */

interface MockInspection extends QcInspectionResult {}

const MOCK_LOCATION = 'cd872f83-ce52-4415-b93d-fe9e91ee78c3'

const mockQueue: QcQueueItem[] = [
  {
    InspectionId: null,
    SourceType: 'GRN',
    SourceNumber: 'GRN-2026-0001 (MOCK)',
    GoodsReceiptLineId: 'mock-line-1',
    ItemCode: 'COMP-001',
    ItemName: 'Semi-hermetic compressor',
    CategoryCode: 'REF',
    Quantity: 2,
    QcRackCode: 'QC-REF',
    ReceivedAt: '2026-09-01T10:00:00Z',
    QcDueAt: '2026-09-03T10:00:00Z',
    AgeHours: 26,
    IsOverdue: false,
  },
  {
    InspectionId: null,
    SourceType: 'GRN',
    SourceNumber: 'GRN-2026-0002 (MOCK)',
    GoodsReceiptLineId: 'mock-line-2',
    ItemCode: 'CBLE-090',
    ItemName: 'Control cable 4-core',
    CategoryCode: 'ELE',
    Quantity: 50,
    QcRackCode: 'QC-ELE',
    ReceivedAt: '2026-08-29T09:00:00Z',
    QcDueAt: '2026-08-31T09:00:00Z',
    AgeHours: 99,
    IsOverdue: true,
  },
]

const mockSerialsByLine: Record<string, { InventorySerialId: string; StoredSerialNumber: string }[]> = {
  'mock-line-1': [
    { InventorySerialId: 'mock-serial-1', StoredSerialNumber: 'A12345' },
    { InventorySerialId: 'mock-serial-2', StoredSerialNumber: 'A12346' },
  ],
  'mock-line-2': [],
}

const mockInspections = new Map<string, MockInspection>()
let mockCounter = 0

function delay<T>(value: T): Promise<T> {
  return new Promise((resolve) => setTimeout(() => resolve(value), 150))
}

function mockListQueue(query: QcQueueQuery): Promise<QcQueueResult> {
  let items = mockQueue.map((item) => {
    const existing = [...mockInspections.values()].find(
      (candidate) => candidate.GoodsReceiptLineId === item.GoodsReceiptLineId,
    )
    return { ...item, InspectionId: existing?.Id ?? null }
  })
  // A finalized, uncorrected inspection leaves the queue.
  items = items.filter((item) => {
    const inspection = item.InspectionId ? mockInspections.get(item.InspectionId) : null
    return !inspection || inspection.CurrentRevision.Status !== 'FINAL'
  })
  if (query.categoryCode) items = items.filter((item) => item.CategoryCode === query.categoryCode)
  if (query.overdue) items = items.filter((item) => item.IsOverdue)
  return delay({ TotalCount: items.length, PageNumber: query.page, PageSize: query.pageSize, Items: items })
}

function newRevision(revisionNumber: number, kind: 'INITIAL' | 'CORRECTION', lineId: string, reason: string | null): QcRevision {
  return {
    Id: `mock-rev-${++mockCounter}`,
    RevisionNumber: revisionNumber,
    RevisionKind: kind,
    RevisesRevisionId: null,
    CorrectionReason: reason,
    InspectorEmployeeId: 'mock-inspector',
    InspectorBasis: 'QC_MANAGER',
    FallbackReason: null,
    InspectionStartedAt: new Date().toISOString(),
    InspectionCompletedAt: null,
    InspectedQuantity: 0,
    AcceptedQuantity: 0,
    RejectedQuantity: 0,
    InspectionShortfallRejectedQuantity: 0,
    Decision: '',
    AcceptedConditionLocationId: MOCK_LOCATION,
    Status: 'DRAFT',
    Version: 1,
    ParameterResults: [],
    SerialDispositions: (mockSerialsByLine[lineId] ?? []).map((serial) => ({
      ...serial,
      Disposition: '',
      Reason: null,
    })),
  }
}

function mockCreate(goodsReceiptLineId: string): Promise<QcInspectionResult> {
  const queueItem = mockQueue.find((item) => item.GoodsReceiptLineId === goodsReceiptLineId)
  if (!queueItem) return Promise.reject(new ApiError(404, 'Mock: unknown goods receipt line.'))
  const existing = [...mockInspections.values()].find((i) => i.GoodsReceiptLineId === goodsReceiptLineId)
  if (existing) return delay(existing)
  const inspection: MockInspection = {
    Id: `mock-qci-${++mockCounter}`,
    InspectionNumber: `QCI-2026-${String(mockCounter).padStart(4, '0')} (MOCK)`,
    GoodsReceiptLineId: goodsReceiptLineId,
    DeliveryChallanLineId: null,
    SourceNumber: queueItem.SourceNumber,
    ItemCode: queueItem.ItemCode,
    ItemName: queueItem.ItemName,
    CategoryCode: queueItem.CategoryCode,
    Quantity: queueItem.Quantity,
    QcDueAt: queueItem.QcDueAt,
    StockPostingBatchId: null,
    CurrentRevision: newRevision(1, 'INITIAL', goodsReceiptLineId, null),
    PriorRevisions: [],
  }
  mockInspections.set(inspection.Id, inspection)
  return delay(inspection)
}

function mockGet(id: string): Promise<QcInspectionResult> {
  const inspection = mockInspections.get(id)
  if (!inspection) return Promise.reject(new ApiError(404, 'Mock: inspection not found.'))
  return delay(inspection)
}

function findByRevision(revisionId: string): MockInspection | undefined {
  return [...mockInspections.values()].find((i) => i.CurrentRevision.Id === revisionId)
}

function mockUpdate(revisionId: string, body: UpdateQcRevisionRequest): Promise<QcInspectionResult> {
  const inspection = findByRevision(revisionId)
  if (!inspection) return Promise.reject(new ApiError(404, 'Mock: revision not found.'))
  const revision = inspection.CurrentRevision
  if (revision.Status !== 'DRAFT') return Promise.reject(new ApiError(409, 'A finalized QC revision is immutable; correct it instead.'))
  if (revision.Version !== body.Version) return Promise.reject(new ApiError(409, 'QC revision Version is stale.'))
  inspection.CurrentRevision = {
    ...revision,
    InspectedQuantity: body.InspectedQuantity,
    AcceptedQuantity: body.AcceptedQuantity,
    RejectedQuantity: body.RejectedQuantity,
    InspectionShortfallRejectedQuantity: body.InspectionShortfallRejectedQuantity,
    Decision: body.Decision,
    AcceptedConditionLocationId: body.AcceptedConditionLocationId,
    SerialDispositions: revision.SerialDispositions.map((serial) => {
      const sent = body.SerialDispositions.find((s) => s.InventorySerialId === serial.InventorySerialId)
      return sent ? { ...serial, Disposition: sent.Disposition, Reason: sent.Reason } : serial
    }),
    Version: revision.Version + 1,
  }
  return delay(inspection)
}

function mockFinalize(revisionId: string, body: FinalizeQcRevisionRequest): Promise<QcInspectionResult> {
  const inspection = findByRevision(revisionId)
  if (!inspection) return Promise.reject(new ApiError(404, 'Mock: revision not found.'))
  const revision = inspection.CurrentRevision
  if (revision.Status !== 'DRAFT') return Promise.reject(new ApiError(409, 'Revision is already finalized.'))
  if (revision.Version !== body.Version) return Promise.reject(new ApiError(409, 'QC revision Version is stale.'))
  if (!revision.Decision) return Promise.reject(new ApiError(409, 'A decision is required before finalize.'))
  inspection.CurrentRevision = {
    ...revision,
    Status: 'FINAL',
    InspectionCompletedAt: new Date().toISOString(),
    Version: revision.Version + 1,
  }
  inspection.StockPostingBatchId = `mock-batch-${++mockCounter}`
  return delay(inspection)
}

function mockCorrect(id: string, body: CorrectQcInspectionRequest): Promise<QcInspectionResult> {
  const inspection = mockInspections.get(id)
  if (!inspection) return Promise.reject(new ApiError(404, 'Mock: inspection not found.'))
  if (inspection.CurrentRevision.Status !== 'FINAL') {
    return Promise.reject(new ApiError(409, 'Only a finalized inspection can be corrected.'))
  }
  if (!body.Reason.trim()) return Promise.reject(new ApiError(409, 'A correction reason is required.'))
  const prior = inspection.CurrentRevision
  const next = {
    ...newRevision(prior.RevisionNumber + 1, 'CORRECTION' as const, inspection.GoodsReceiptLineId ?? '', body.Reason),
    RevisesRevisionId: prior.Id,
    InspectedQuantity: prior.InspectedQuantity,
    AcceptedQuantity: prior.AcceptedQuantity,
    RejectedQuantity: prior.RejectedQuantity,
    InspectionShortfallRejectedQuantity: prior.InspectionShortfallRejectedQuantity,
    Decision: prior.Decision,
    SerialDispositions: prior.SerialDispositions.map((serial) => ({ ...serial })),
  }
  inspection.PriorRevisions = [...inspection.PriorRevisions, prior]
  inspection.CurrentRevision = next
  return delay(inspection)
}
