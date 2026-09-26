import { ApiError, api } from './client'
import type {
  CloseInventoryPeriodRequest,
  InventoryPeriodView,
  OpenInventoryPeriodRequest,
} from '../types/inventoryPeriod'

// InventoryPeriodEndpoints.cs. Every route sits on page accounts.inventory-periods:
// reads need View, open and close need Approve. The service additionally
// requires the CHIEF_FINANCIAL_OFFICER role (SESS-02). Both writes carry
// IdempotencyKey in the body (not a header); a replay returns the retained
// result with Replayed = true, a changed payload under the same key is refused.
const BASE = '/api/v1/accounts/inventory-periods'

export const INVENTORY_PERIODS_PAGE_KEY = 'accounts.inventory-periods'

export function newInventoryPeriodKey(prefix: string): string {
  return `${prefix}-${crypto.randomUUID()}`
}

/** GET / — accounts.inventory-periods:View. The server returns a bare array. */
export async function listInventoryPeriods(): Promise<InventoryPeriodView[]> {
  const data = await api.get<unknown>(`${BASE}/`)
  return Array.isArray(data) ? (data as InventoryPeriodView[]) : []
}

/** GET /{id} — accounts.inventory-periods:View. 404 for unknown or other-company ids. */
export function getInventoryPeriod(id: string): Promise<InventoryPeriodView> {
  return api.get<InventoryPeriodView>(`${BASE}/${encodeURIComponent(id)}`)
}

/** POST / — accounts.inventory-periods:Approve (CFO). Opens a new period. */
export function openInventoryPeriod(body: OpenInventoryPeriodRequest): Promise<InventoryPeriodView> {
  return api.post<InventoryPeriodView>(`${BASE}/`, body)
}

/** POST /{id}/close — accounts.inventory-periods:Approve (CFO). Irreversible. */
export function closeInventoryPeriod(id: string, body: CloseInventoryPeriodRequest): Promise<InventoryPeriodView> {
  return api.post<InventoryPeriodView>(`${BASE}/${encodeURIComponent(id)}/close`, body)
}

/** True when the server definitely did not commit, so a new attempt needs a new key. */
export function isDefinitiveRefusal(error: unknown): boolean {
  return error instanceof ApiError && (error.status === 400 || error.status === 403 || error.status === 404 || error.status === 409)
}

/** Today's calendar date in India, yyyy-MM-dd. */
export function todayIndia(): string {
  const parts = new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Kolkata', year: 'numeric', month: '2-digit', day: '2-digit' })
    .formatToParts(new Date())
  const pick = (type: string) => parts.find((part) => part.type === type)?.value ?? ''
  return `${pick('year')}-${pick('month')}-${pick('day')}`
}

/** "2026-10-01" → "01/10/2026" (DateOnly; no time-zone shift). */
export function formatPeriodDate(value: string | null | undefined): string {
  if (!value) return '—'
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value)
  return match ? `${match[3]}/${match[2]}/${match[1]}` : value
}

/** "2026-10-01" → "1 October 2026" for confirmation sentences. */
export function formatPeriodDateWords(value: string | null | undefined): string {
  if (!value) return '—'
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value)
  if (!match) return value
  const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]))
  return date.toLocaleDateString('en-IN', { day: 'numeric', month: 'long', year: 'numeric' })
}

/** An offset timestamp shown in the operator's local time, Indian format. */
export function formatPeriodInstant(value: string | null | undefined): string {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('en-IN')
}

/** Inclusive day count between two yyyy-MM-dd dates, or null when either is invalid. */
export function inclusiveDays(start: string, end: string): number | null {
  const a = Date.parse(`${start}T00:00:00Z`)
  const b = Date.parse(`${end}T00:00:00Z`)
  if (Number.isNaN(a) || Number.isNaN(b)) return null
  return Math.round((b - a) / 86_400_000) + 1
}

/** Two inclusive ranges share at least one day. yyyy-MM-dd compares correctly as text. */
export function rangesOverlap(aStart: string, aEnd: string, bStart: string, bEnd: string): boolean {
  return aStart <= bEnd && bStart <= aEnd
}
