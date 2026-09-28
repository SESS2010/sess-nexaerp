// Tracking-lite (R1): GET /api/v1/tracking/pending, /summary and
// /{docType}/{documentId}/history. Access is decided in the database:
//   403 TRACKING_ACCESS_DENIED  the user has no Pending/Tracking page
//   404 NOT_FOUND               the document is outside the user's scope
//   400 TRACKING_REQUEST_INVALID  a doc type outside TrackingDocType
// The endpoints ship on feature/tracking-lite; on a server that predates R1 the
// route does not exist and the history call fails with a bare 404 (no
// envelope). historyAvailability() tells those cases apart for the panel.

import { api, ApiError } from './client'
import type { TrackingDocType, TrackingHistory, TrackingPendingPage, TrackingSummaryTile } from '../types/tracking'

const BASE = '/api/v1/tracking'

export function getTrackingHistory(docType: TrackingDocType, documentId: string): Promise<TrackingHistory> {
  return api.get<TrackingHistory>(`${BASE}/${docType}/${encodeURIComponent(documentId)}/history`)
}

export interface TrackingPendingQuery {
  docType?: string
  queue?: string
  overdueOnly?: boolean
  mine?: boolean
  page?: number
  pageSize?: number
}

export function listTrackingPending(query: TrackingPendingQuery = {}): Promise<TrackingPendingPage> {
  const params = new URLSearchParams()
  if (query.docType) params.set('docType', query.docType)
  if (query.queue) params.set('queue', query.queue)
  if (query.overdueOnly) params.set('overdueOnly', 'true')
  if (query.mine) params.set('mine', 'true')
  params.set('page', String(query.page ?? 1))
  params.set('pageSize', String(query.pageSize ?? 50))
  return api.get<TrackingPendingPage>(`${BASE}/pending?${params.toString()}`)
}

export function getTrackingSummary(): Promise<TrackingSummaryTile[]> {
  return api.get<TrackingSummaryTile[]>(`${BASE}/summary`)
}

/** The endpoint's own code, whichever casing the body used ({code} raw or {Code} enveloped). */
function errorCode(error: ApiError): string | undefined {
  const raw = error.envelope?.code
  return error.code ?? (typeof raw === 'string' ? raw : undefined)
}

export type HistoryAvailability =
  /** 404 {code:"NOT_FOUND"}: the document exists but is outside this user's scope (or does not exist). */
  | 'out-of-scope'
  /** 403 TRACKING_ACCESS_DENIED: the user's roles have no tracking.pending page. */
  | 'no-tracking-page'
  /** Network failure or a 404 without the NOT_FOUND envelope: the server predates R1. */
  | 'not-deployed'
  /** Anything else: show the ordinary error banner. */
  | 'error'

export function historyAvailability(error: unknown): HistoryAvailability {
  if (error instanceof ApiError) {
    const code = errorCode(error)
    if (error.status === 404) return code === 'NOT_FOUND' ? 'out-of-scope' : 'not-deployed'
    if (error.status === 403 && code === 'TRACKING_ACCESS_DENIED') return 'no-tracking-page'
    return 'error'
  }
  // fetch() rejects with a TypeError when the server cannot be reached at all.
  if (error instanceof TypeError) return 'not-deployed'
  return 'error'
}
