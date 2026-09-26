// Shared building blocks for the Purchase dashboard only: refresh, the report
// each section hands up to the page, severity colours, client-side sort and
// quick filter for the rows already on screen, skeletons.
//
// Nothing here computes a business figure. Severity colours use DISPLAY
// thresholds on ages the server returned; they are not a policy and are named
// in the page so a user can see why something is amber or red.

import { useCallback, useEffect, useRef, useState, type CSSProperties, type ReactNode } from 'react'
import { getCompanyCode } from '../../api/dashboards'
import type { SortState } from '../../hooks/useSort'
import { formatAge } from '../../utils/dashboardFormat'
import { NullValue } from './DashboardParts'
import { useDashboardQuery, type DashboardQueryState } from './useDashboardQuery'

// ---------- severity ----------

export type Severity = 'red' | 'amber' | 'green' | 'info'

export interface AgeThresholds {
  /** Age in days from which an item is shown amber (ageing). */
  amber: number
  /** Age in days from which an item is shown red (needs action now). */
  red: number
}

/** Documents waiting in a purchase queue. Display thresholds only. */
export const WORKLOAD_AGE: AgeThresholds = { amber: 3, red: 7 }
/** Goods received not billed, and vendor advances not adjusted. Display thresholds only. */
export const OBLIGATION_AGE: AgeThresholds = { amber: 15, red: 30 }

export function ageSeverity(days: number | null, thresholds: AgeThresholds): Severity {
  if (days === null) return 'green'
  if (days >= thresholds.red) return 'red'
  if (days >= thresholds.amber) return 'amber'
  return 'green'
}

export function thresholdText(thresholds: AgeThresholds): string {
  return `amber from ${thresholds.amber} days, red from ${thresholds.red} days`
}

export const SEVERITY: Record<Severity, { bar: string; text: string; soft: string; border: string; badge: string; label: string }> = {
  red: { bar: '#dc2626', text: 'text-red-700', soft: 'bg-red-50', border: 'border-red-200', badge: 'badge badge-error', label: 'Act now' },
  amber: { bar: '#d97706', text: 'text-amber-700', soft: 'bg-amber-50', border: 'border-amber-300', badge: 'badge badge-warn', label: 'Ageing' },
  green: { bar: '#059669', text: 'text-emerald-700', soft: 'bg-emerald-50', border: 'border-emerald-200', badge: 'badge badge-ok', label: 'On track' },
  info: { bar: '#1d4ed8', text: 'text-blue-700', soft: 'bg-blue-50', border: 'border-blue-200', badge: 'badge badge-info', label: 'Note' },
}

export const SEVERITY_ORDER: Record<Severity, number> = { red: 0, amber: 1, info: 2, green: 3 }

/** An age in days coloured by the given display thresholds; null is shown as a marker, never 0. */
export function AgeText({ days, thresholds, suffix = '' }: { days: number | null; thresholds?: AgeThresholds; suffix?: string }) {
  if (days === null) return <NullValue reason="none" />
  const severity = thresholds ? ageSeverity(days, thresholds) : null
  const className = severity === 'red' ? 'font-semibold text-red-700' : severity === 'amber' ? 'font-semibold text-amber-700' : ''
  return <span className={`whitespace-nowrap ${className}`}>{formatAge(days)}{suffix}</span>
}

// ---------- section reports and focus requests ----------

/** What a section tells the page, so the attention panel and KPI tiles reuse its response. */
export type SectionReport<T> =
  | { status: 'loading' }
  | { status: 'ready'; data: T; unfiltered: boolean }
  | { status: 'problem' }

/** A request from the attention panel or a KPI tile to narrow a section's detail rows. */
export interface FocusRequest {
  target: string
  nonce: number
}

export function scrollToSection(id: string) {
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

/**
 * useDashboardQuery plus two things for auto-refresh:
 * - `refreshTick` re-runs the same request;
 * - while a refresh of the SAME request (same company, filters and page) is in
 *   flight, the previous figures stay on screen instead of flashing a loader.
 * Any error or company mismatch drops the previous figures, so a 409 never
 * leaves an earlier copy of the report visible.
 */
export function usePurchaseSectionQuery<T extends { CompanyCode: string }>(load: () => Promise<T>, key: string, refreshTick: number) {
  const fullKey = `${getCompanyCode()}|${key}`
  const { state, reload } = useDashboardQuery(load, `${fullKey}#${refreshTick}`)
  const last = useRef<{ key: string; data: T } | null>(null)
  if (state.kind === 'ready') last.current = { key: fullKey, data: state.data }
  else if (state.kind !== 'loading') last.current = null
  const stale = state.kind === 'loading' && last.current?.key === fullKey ? last.current.data : null
  const data = state.kind === 'ready' ? state.data : stale
  return { state, data, refreshing: stale !== null, reload }
}

/** Sends the section's current state to the page whenever it changes. */
export function useReportToPage<T>(onReport: ((report: SectionReport<T>) => void) | undefined, state: DashboardQueryState<T>, data: T | null, unfiltered: boolean) {
  const kind = state.kind
  useEffect(() => {
    if (!onReport) return
    if (data) onReport({ status: 'ready', data, unfiltered })
    else if (kind === 'loading') onReport({ status: 'loading' })
    else onReport({ status: 'problem' })
  }, [onReport, data, kind, unfiltered])
}

/** Applies a focus request once per nonce. */
export function useFocusRequest(focus: FocusRequest | null | undefined, apply: (target: string) => void) {
  const seen = useRef<number | null>(null)
  const applyRef = useRef(apply)
  applyRef.current = apply
  useEffect(() => {
    if (!focus || seen.current === focus.nonce) return
    seen.current = focus.nonce
    applyRef.current(focus.target)
  }, [focus])
}

// ---------- auto-refresh ----------

/**
 * Refreshes every `intervalMs` while the tab is visible. A hidden tab makes
 * no requests; when it becomes visible again and the interval has passed, it
 * refreshes at once.
 */
export function usePurchaseAutoRefresh(intervalMs: number) {
  const [tick, setTick] = useState(0)
  const lastRef = useRef(Date.now())
  const refresh = useCallback(() => {
    lastRef.current = Date.now()
    setTick((value) => value + 1)
  }, [])

  useEffect(() => {
    const due = () => document.visibilityState === 'visible' && Date.now() - lastRef.current >= intervalMs
    const timer = window.setInterval(() => { if (due()) refresh() }, 5000)
    const onVisibility = () => { if (due()) refresh() }
    document.addEventListener('visibilitychange', onVisibility)
    return () => {
      window.clearInterval(timer)
      document.removeEventListener('visibilitychange', onVisibility)
    }
  }, [intervalMs, refresh])

  return { tick, refresh }
}

/** Re-renders every `everyMs` so "2 min ago" stays true. */
export function useNow(everyMs: number): number {
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), everyMs)
    return () => window.clearInterval(timer)
  }, [everyMs])
  return now
}

export function formatRelative(then: number, now: number): string {
  const seconds = Math.max(0, Math.round((now - then) / 1000))
  if (seconds < 45) return 'just now'
  const minutes = Math.round(seconds / 60)
  if (minutes < 60) return minutes === 1 ? '1 min ago' : `${minutes} min ago`
  const hours = Math.round(minutes / 60)
  return hours === 1 ? '1 hour ago' : `${hours} hours ago`
}

// ---------- loading ----------

export function Skeleton({ className = '', style }: { className?: string; style?: CSSProperties }) {
  return <div aria-hidden="true" className={`animate-pulse rounded-md bg-slate-200/70 ${className}`} style={style} />
}

/** Placeholder shapes for a section while its first response is on the way. */
export function SectionSkeleton({ tiles = 3 }: { tiles?: number }) {
  return (
    <div aria-hidden="true" className="mt-3">
      <div className="grid gap-3 mb-4" style={{ gridTemplateColumns: `repeat(${tiles}, minmax(0, 1fr))` }}>
        {Array.from({ length: tiles }, (_, index) => (
          <div key={index} className="rounded-lg border border-line p-4">
            <Skeleton className="h-3 w-2/3 mb-3" />
            <Skeleton className="h-7 w-1/3 mb-2" />
            <Skeleton className="h-3 w-1/2" />
          </div>
        ))}
      </div>
      <Skeleton className="h-28 w-full mb-3" />
      {Array.from({ length: 4 }, (_, index) => <Skeleton key={index} className="h-4 w-full mb-2" />)}
    </div>
  )
}

export function RefreshingHint({ on }: { on: boolean }) {
  if (!on) return null
  return (
    <span className="inline-flex items-center gap-1.5 text-[12px] text-ink-soft" role="status">
      <span className="inline-block size-2 animate-pulse rounded-full bg-blue-500" /> Refreshing…
    </span>
  )
}

// ---------- rows on screen: sort and quick filter ----------

export const SERVER_ORDER: SortState = { sortBy: '', sortDirection: 'asc' }

type SortValue = string | number | null | undefined

/** Sorts the rows already returned (this page only). An empty sortBy keeps the server's order. */
export function sortRows<T>(rows: T[], sort: SortState, accessors: Record<string, (row: T) => SortValue>): T[] {
  const accessor = accessors[sort.sortBy]
  if (!accessor) return rows
  const direction = sort.sortDirection === 'asc' ? 1 : -1
  return rows
    .map((row, index) => ({ row, index, value: accessor(row) }))
    .sort((a, b) => {
      const av = a.value
      const bv = b.value
      // Missing values always sort last, whatever the direction.
      if (av === null || av === undefined || av === '') return bv === null || bv === undefined || bv === '' ? a.index - b.index : 1
      if (bv === null || bv === undefined || bv === '') return -1
      const compared = typeof av === 'number' && typeof bv === 'number'
        ? av - bv
        : String(av).localeCompare(String(bv), 'en-IN', { numeric: true, sensitivity: 'base' })
      return compared === 0 ? a.index - b.index : compared * direction
    })
    .map((entry) => entry.row)
}

export function matchesText(query: string, values: (string | number | null | undefined)[]): boolean {
  const needle = query.trim().toLowerCase()
  if (!needle) return true
  return values.some((value) => value !== null && value !== undefined && String(value).toLowerCase().includes(needle))
}

/** Search box for the rows on this page, with an honest count of what it shows. */
export function QuickFilter({ id, value, onChange, shown, onPage, placeholder, children }: {
  id: string
  value: string
  onChange: (value: string) => void
  shown: number
  onPage: number
  placeholder: string
  children?: ReactNode
}) {
  return (
    <div className="flex flex-wrap items-center gap-2 mb-2">
      <label htmlFor={id} className="sr-only">Search the rows on this page</label>
      <input id={id} type="search" className="input" style={{ width: 280, maxWidth: '100%' }} placeholder={placeholder}
        value={value} onChange={(event) => onChange(event.target.value)} />
      {value.trim() && (
        <span className="text-[12px] text-ink-soft">
          Showing {shown} of {onPage} row{onPage === 1 ? '' : 's'} on this page
        </span>
      )}
      {children}
      <span className="text-[12px] text-ink-faint ml-auto">Search and column sorting work on the rows of this page only.</span>
    </div>
  )
}
