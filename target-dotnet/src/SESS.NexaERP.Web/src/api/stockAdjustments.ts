import { ApiError, api } from './client'
import type {
  CreateStockAdjustmentRequest,
  ReviseStockAdjustmentRequest,
  StockAdjustmentDecisionRequest,
  StockAdjustmentPage,
  StockAdjustmentPeriodView,
  StockAdjustmentTransitionRequest,
  StockAdjustmentView,
} from '../types/stockAdjustment'

// StockAdjustmentEndpoints.cs, page stores.stock-adjustments. Every command
// carries IdempotencyKey (and, after create, Version) in the body; there is no
// Idempotency-Key header on these routes.
const BASE = '/api/v1/stores/stock-adjustments'

/** Page key of every route below (advance.page_definitions, migration StockAdjustmentPosting). */
export const STOCK_ADJUSTMENT_PAGE_KEY = 'stores.stock-adjustments'

export interface StockAdjustmentListQuery {
  page: number
  pageSize: number
  status?: string
}

/** GET / (view). Newest first; pageSize is clamped to 1..200 by the server. */
export async function listStockAdjustments(query: StockAdjustmentListQuery): Promise<StockAdjustmentPage> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.status) params.set('status', query.status)
  const data = await api.get<Partial<StockAdjustmentPage> | null>(`${BASE}/?${params.toString()}`)
  // Never .map a value straight off the wire (see toPagedResponse in client.ts).
  const items = Array.isArray(data?.Items) ? data!.Items : []
  return {
    Total: typeof data?.Total === 'number' ? data.Total : items.length,
    Page: typeof data?.Page === 'number' ? data.Page : query.page,
    PageSize: typeof data?.PageSize === 'number' ? data.PageSize : query.pageSize,
    Items: items,
  }
}

/** GET /inventory-periods (view): OPEN inventory periods of the company, newest first. */
export async function listStockAdjustmentPeriods(): Promise<StockAdjustmentPeriodView[]> {
  const data = await api.get<unknown>(`${BASE}/inventory-periods`)
  return Array.isArray(data) ? (data as StockAdjustmentPeriodView[]) : []
}

/** GET /{id} (view). */
export function getStockAdjustment(id: string): Promise<StockAdjustmentView> {
  return api.get<StockAdjustmentView>(`${BASE}/${encodeURIComponent(id)}`)
}

/** POST / (create) → 201 with the DRAFT. STORES_EXECUTIVE / STORES_MANAGER. */
export function createStockAdjustment(body: CreateStockAdjustmentRequest): Promise<StockAdjustmentView> {
  return api.post<StockAdjustmentView>(`${BASE}/`, body)
}

/** PUT /{id} (update): new revision of a DRAFT, SUBMITTED or REJECTED adjustment; it returns to DRAFT. */
export function reviseStockAdjustment(id: string, body: ReviseStockAdjustmentRequest): Promise<StockAdjustmentView> {
  return api.put<StockAdjustmentView>(`${BASE}/${encodeURIComponent(id)}`, body)
}

/** POST /{id}/submit (submit): DRAFT → SUBMITTED; the server takes the approval snapshot. */
export function submitStockAdjustment(id: string, body: StockAdjustmentTransitionRequest): Promise<StockAdjustmentView> {
  return api.post<StockAdjustmentView>(`${BASE}/${encodeURIComponent(id)}/submit`, body)
}

/**
 * POST /{id}/approve (approve). Records one required role's approval; the
 * decision that completes the snapshot also posts stock and FIFO (→ POSTED)
 * in the same transaction.
 */
export function approveStockAdjustment(id: string, body: StockAdjustmentDecisionRequest): Promise<StockAdjustmentView> {
  return api.post<StockAdjustmentView>(`${BASE}/${encodeURIComponent(id)}/approve`, body)
}

/** POST /{id}/reject (reject): SUBMITTED → REJECTED; Stores revises before resubmitting. */
export function rejectStockAdjustment(id: string, body: StockAdjustmentTransitionRequest): Promise<StockAdjustmentView> {
  return api.post<StockAdjustmentView>(`${BASE}/${encodeURIComponent(id)}/reject`, body)
}

/** True when the server definitely did not commit, so the next attempt needs a new idempotency key. */
export function stockAdjustmentRefusedDefinitively(error: unknown): boolean {
  return error instanceof ApiError && (error.status === 400 || error.status === 403 || error.status === 404 || error.status === 409)
}

/** The server's calendar date (UTC), yyyy-MM-dd: the posting-date policy compares against this, not local time. */
export function serverTodayUtc(): string {
  return new Date().toISOString().slice(0, 10)
}

/** Days between two yyyy-MM-dd dates (b − a), with no time-zone shift. */
export function daysBetween(a: string, b: string): number {
  const parse = (value: string) => {
    const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value)
    return match ? Date.UTC(Number(match[1]), Number(match[2]) - 1, Number(match[3])) : NaN
  }
  return Math.round((parse(b) - parse(a)) / 86_400_000)
}

const RUPEES = new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 2 })

export function formatRupees(value: number | null | undefined): string {
  return typeof value === 'number' && Number.isFinite(value) ? RUPEES.format(value) : '—'
}

/** "+2" / "−1.5" for a signed quantity change. */
export function formatQuantityChange(value: number): string {
  return value > 0 ? `+${value}` : `−${Math.abs(value)}`
}

/** yyyy-MM-dd → "15 October 2026" without a time-zone shift. */
export function formatDateOnly(value: string | null | undefined): string {
  if (!value) return '—'
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value)
  if (!match) return value
  return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]))
    .toLocaleDateString('en-IN', { day: 'numeric', month: 'long', year: 'numeric' })
}

/** An offset timestamp shown as a local date and time. */
export function formatInstantLocal(value: string | null | undefined): string {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleString('en-IN', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
