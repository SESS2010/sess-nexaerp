import { useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { ArrowDown, ArrowUp, ChevronLeft, ChevronRight, FlaskConical, Lock, RotateCw, Search, TriangleAlert } from 'lucide-react'
import { ApiError } from '../../api/client'
import { getCompanyCode } from '../../api/dashboards'
import { DASHBOARD_ERROR_CODES } from '../../types/dashboard'
import type { StoresQcStockRow } from '../../types/dashboard'
import { formatCount } from '../../utils/dashboardFormat'
import type { Tone } from './DashboardUi'
import { useDashboardQuery } from './useDashboardQuery'
import type { DashboardQueryState } from './useDashboardQuery'

// Shared pieces for the Stores dashboard only. Nothing here computes a business
// figure: ages, counts and verdicts are the server's; this file only groups,
// sorts, colours and words them.

// ---------- what a section tells the page ----------

export interface SectionReport<T> {
  /** 'problem' = error or discarded response; the section shows the detail. */
  kind: 'loading' | 'ready' | 'problem'
  data: T | null
  /** A background refresh is in flight while the previous answer stays on screen. */
  refreshing: boolean
  /** Browser time the shown answer arrived; the server's GeneratedAt is shown separately. */
  receivedAt: number | null
}

/**
 * useDashboardQuery plus two things the dashboard needs for auto-refresh:
 * - `refreshTick` changing re-requests the SAME query;
 * - while that refresh loads, the previous answer for the same query and
 *   company stays on screen instead of flashing a skeleton. A changed filter
 *   or page is a new query and shows the skeleton. An error replaces the old
 *   answer: stale figures are never kept beside a failure.
 */
export function useStoresSectionQuery<T extends { CompanyCode: string }>(load: () => Promise<T>, key: string, refreshTick: number) {
  const { state, reload } = useDashboardQuery(load, key)
  const [kept, setKept] = useState<{ key: string; company: string; data: T; at: number } | null>(null)

  useEffect(() => {
    if (state.kind === 'ready') setKept({ key, company: getCompanyCode(), data: state.data, at: Date.now() })
    else if (state.kind !== 'loading') setKept(null)
    // key is read at the moment the answer lands; it is part of the query that produced it.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [state])

  const seenTick = useRef(refreshTick)
  useEffect(() => {
    if (seenTick.current === refreshTick) return
    seenTick.current = refreshTick
    reload()
  }, [refreshTick, reload])

  const valid = kept !== null && kept.key === key && kept.company === getCompanyCode() ? kept : null
  const data: T | null = state.kind === 'ready' ? state.data : state.kind === 'loading' && valid ? valid.data : null
  const refreshing = state.kind === 'loading' && data !== null
  const report: SectionReport<T> = {
    kind: state.kind === 'ready' || refreshing ? 'ready' : state.kind === 'loading' ? 'loading' : 'problem',
    data,
    refreshing,
    // Filled one render after the answer lands (when it is kept); stable across renders.
    receivedAt: data && valid ? valid.at : null,
  }
  return { state: state as DashboardQueryState<T>, reload, data, refreshing, report }
}

// ---------- queue names (the contract gives QC cards a Key only) ----------

export const WORKLOAD_SHORT: Record<string, string> = {
  'gate-no-grn': 'Gate entries → GRN',
  'mir-approval': 'MIRs to approve',
  'mir-unissued': 'MIRs to issue',
}

export const QC_SHORT: Record<string, string> = {
  QC_HOLD: 'Held for QC',
  PENDING_RETURNABLE_DC: 'Returnable DC',
}

export const QC_HINT: Record<string, string> = {
  QC_HOLD: 'GRN lines received and not yet cleared by QC. Counts are distinct GRN lines, not stock rows or units.',
  PENDING_RETURNABLE_DC: 'GRN lines awaiting a returnable delivery challan. These are never QC-overdue.',
}

// ---------- ages and colours ----------

/** Kinds of waiting; each is coloured by its own rule below. */
export type AgeKind = 'workload' | 'qc-hold' | 'pending-dc'

export const COLOUR_RULES =
  'Colours are a display aid only; counts, ages and "overdue" are the server\'s. Red = past QC due (server verdict) or waiting 4+ days. ' +
  'Amber = waiting 2–3 days (held for QC: 2+ days; returnable DC: over 7 days). Blue/green = fresh.'

/**
 * Display thresholds for colour only; they are not a business rule and never
 * change a count. Workload: 0-1 day ok, 2-3 warn, 4+ bad. QC hold is bad
 * ONLY when the server says a line is past its QC due time. A pending
 * returnable DC is never overdue, so it is never bad.
 */
export function ageTone(kind: AgeKind, days: number | null, serverOverdue = false): Tone {
  if (kind === 'qc-hold') {
    if (serverOverdue) return 'bad'
    if (days === null) return 'muted'
    return days >= 2 ? 'warn' : 'ok'
  }
  if (days === null) return 'muted'
  if (kind === 'pending-dc') return days > 7 ? 'warn' : 'ok'
  if (days >= 4) return 'bad'
  if (days >= 2) return 'warn'
  return 'ok'
}

export function qcRowTone(row: StoresQcStockRow): Tone {
  // IsOverdue is the server's verdict; never recomputed from the browser clock.
  return row.Queue === 'QC_HOLD' ? ageTone('qc-hold', row.ReceiptAgeDays, row.IsOverdue) : ageTone('pending-dc', row.ReceiptAgeDays)
}

/** "less than a day", "1 day", "4 days" for sentences. */
export function sentenceAge(days: number): string {
  if (days === 0) return 'less than a day'
  return days === 1 ? '1 day' : `${formatCount(days)} days`
}

export function plural(count: number, singular: string, many = `${singular}s`): string {
  return `${formatCount(count)} ${count === 1 ? singular : many}`
}

// ---------- ageing buckets ----------

export const AGE_BUCKETS = [
  { key: '0-1', label: '0–1 d', min: 0, max: 1, bar: 'bg-sky-400' },
  { key: '2-3', label: '2–3 d', min: 2, max: 3, bar: 'bg-amber-400' },
  { key: '4-7', label: '4–7 d', min: 4, max: 7, bar: 'bg-rose-400' },
  { key: '8+', label: '> 7 d', min: 8, max: Number.POSITIVE_INFINITY, bar: 'bg-rose-700' },
] as const

export function bucketOf(days: number): number {
  const index = AGE_BUCKETS.findIndex((bucket) => days >= bucket.min && days <= bucket.max)
  return index < 0 ? 0 : index
}

// ---------- relative time ----------

export function useNow(intervalMs = 15000): number {
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), intervalMs)
    return () => window.clearInterval(id)
  }, [intervalMs])
  return now
}

export function formatAgo(at: number, now: number): string {
  const seconds = Math.max(0, Math.round((now - at) / 1000))
  if (seconds < 45) return 'just now'
  const minutes = Math.round(seconds / 60)
  if (minutes < 60) return minutes === 1 ? '1 min ago' : `${minutes} min ago`
  const hours = Math.round(minutes / 60)
  return hours === 1 ? '1 hour ago' : `${hours} hours ago`
}

export function scrollToId(id: string) {
  window.setTimeout(() => document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 30)
}

// ---------- "open this panel" requests from cards, attention and charts ----------

/** Each new request (n increases) opens the detail panel and applies the view. */
export interface Reveal {
  n: number
  search?: string
  overdueFirst?: boolean
}

// ---------- compact states ----------

export function SkeletonRows({ rows = 4, label }: { rows?: number; label: string }) {
  return (
    <div role="status" data-dashboard-state="loading" className="space-y-1.5">
      <span className="sr-only">Loading: {label}</span>
      {Array.from({ length: rows }, (_, index) => (
        <div key={index} className="h-6 animate-pulse rounded bg-slate-100" aria-hidden="true" />
      ))}
    </div>
  )
}

export function OneLine({ tone = 'muted', icon, children, title }: { tone?: Tone; icon?: ReactNode; children: ReactNode; title?: string }) {
  const colour = tone === 'bad' ? 'text-rose-700' : tone === 'warn' ? 'text-amber-700' : tone === 'ok' ? 'text-emerald-700' : 'text-slate-500'
  return (
    <div className={`flex flex-wrap items-center gap-1.5 py-1 text-xs ${colour}`} title={title}>
      {icon}
      {children}
    </div>
  )
}

/**
 * Error and discard states for one section, one line each, TraceId kept.
 * The 409 source-inconsistency shows no figures and asks for an administrator.
 */
export function CompactProblem({ state, onRetry }: {
  state: Exclude<DashboardQueryState<unknown>, { kind: 'ready' } | { kind: 'loading' }>
  onRetry: () => void
}) {
  const retry = (
    <button type="button" onClick={onRetry} className="inline-flex items-center gap-1 font-medium text-blue-600 hover:underline">
      <RotateCw size={12} aria-hidden /> Retry
    </button>
  )
  if (state.kind === 'company-mismatch') {
    return (
      <div role="alert" data-dashboard-state="failed">
        <OneLine tone="bad" icon={<TriangleAlert size={13} aria-hidden />}
          title={`The server answered for ${state.received} while ${state.expected} is selected. Nothing from that response is shown.`}>
          Response was for company <span className="mono">{state.received}</span>, not <span className="mono">{state.expected}</span> — discarded. {retry}
        </OneLine>
      </div>
    )
  }
  const apiError = state.error instanceof ApiError ? state.error : null
  const status = apiError?.status
  const trace = apiError?.traceId ? <span className="text-slate-400">TraceId <span className="mono">{apiError.traceId}</span></span> : null

  if (status === 409 && apiError?.code === DASHBOARD_ERROR_CODES.sourceInconsistent) {
    return (
      <div role="alert" data-dashboard-state="source-inconsistent">
        <OneLine tone="bad" icon={<TriangleAlert size={13} aria-hidden />} title={apiError.message}>
          Figures do not reconcile — none shown. Ask an administrator. {trace}
        </OneLine>
      </div>
    )
  }
  if (status === 403) {
    return (
      <div role="status" data-dashboard-state="denied">
        <OneLine icon={<Lock size={13} aria-hidden />} title={apiError?.message}>You don't have access to this section. {trace}</OneLine>
      </div>
    )
  }
  if (status === 401) {
    return (
      <div role="alert" data-dashboard-state="failed">
        <OneLine tone="bad" icon={<Lock size={13} aria-hidden />}>
          Session ended. <Link to="/login" className="font-medium text-blue-600 hover:underline">Sign in again</Link> {trace}
        </OneLine>
      </div>
    )
  }
  const text = status === 400 ? `Filter not accepted: ${apiError?.message ?? ''}` : status === 500 || !apiError ? 'Could not load this section.' : apiError.message
  return (
    <div role="alert" data-dashboard-state="failed">
      <OneLine tone="bad" icon={<TriangleAlert size={13} aria-hidden />}>{text} {trace} {retry}</OneLine>
    </div>
  )
}

export function StoresMockBanner() {
  const variant = new URLSearchParams(window.location.search).get('mock') ?? 'reference'
  return (
    <div className="mb-3 flex items-center gap-2 rounded-lg bg-amber-50 px-3 py-1.5 text-xs text-amber-800" data-dashboard-mock="on"
      title="Every value comes from the contract's example bodies.">
      <FlaskConical size={14} aria-hidden /> <strong>Synthetic mock data</strong> — not company figures (variant <span className="mono">{variant}</span>).
    </div>
  )
}

// ---------- sortable, filterable detail rows ----------

export type SortDir = 'asc' | 'desc'
export interface SortState<K extends string> { key: K; dir: SortDir }

type Accessor<T> = (row: T) => string | number | null

/**
 * Sorts and text-filters the rows ALREADY loaded (one server page). Server
 * order is kept until a header is clicked; a third click returns to it.
 */
export function useLocalRows<T, K extends string>(rows: T[], accessors: Record<K, Accessor<T>>, searchText: (row: T) => string) {
  const [sort, setSort] = useState<SortState<K> | null>(null)
  const [query, setQuery] = useState('')

  const shown = useMemo(() => {
    const needle = query.trim().toLowerCase()
    let result = needle ? rows.filter((row) => searchText(row).toLowerCase().includes(needle)) : rows.slice()
    if (sort) {
      const get = accessors[sort.key]
      const factor = sort.dir === 'asc' ? 1 : -1
      result = result
        .map((row, index) => ({ row, index }))
        .sort((a, b) => {
          const x = get(a.row)
          const y = get(b.row)
          if (x === y) return a.index - b.index
          if (x === null) return 1
          if (y === null) return -1
          if (typeof x === 'number' && typeof y === 'number') return (x - y) * factor
          return String(x).localeCompare(String(y), 'en-IN', { numeric: true }) * factor
        })
        .map((entry) => entry.row)
    }
    return result
    // accessors and searchText are static per table
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rows, sort, query])

  const toggle = (key: K) =>
    setSort((current) => (!current || current.key !== key ? { key, dir: 'asc' } : current.dir === 'asc' ? { key, dir: 'desc' } : null))

  return { shown, sort, setSort, toggle, query, setQuery }
}

export const TH = 'whitespace-nowrap px-2 py-1.5 text-left text-[11px] font-medium uppercase tracking-wide text-slate-500'
export const TD = 'border-t border-slate-100 px-2 py-1.5 align-top'

export function SortTh<K extends string>({ label, sortKey, sort, onSort, align = 'left' }: {
  label: string
  sortKey: K
  sort: SortState<K> | null
  onSort: (key: K) => void
  align?: 'left' | 'right'
}) {
  const active = sort?.key === sortKey
  return (
    <th className={`${TH} ${align === 'right' ? 'text-right' : ''}`} aria-sort={!active ? 'none' : sort.dir === 'asc' ? 'ascending' : 'descending'}>
      <button type="button" onClick={() => onSort(sortKey)} title="Sort the rows on this page"
        className={`inline-flex items-center gap-0.5 uppercase hover:text-slate-800 ${active ? 'text-slate-800' : ''}`}>
        {label}
        {active && (sort.dir === 'asc' ? <ArrowUp size={11} aria-hidden /> : <ArrowDown size={11} aria-hidden />)}
      </button>
    </th>
  )
}

/** Small search box for a Panel header. Search applies to the loaded page only. */
export function SearchBox({ value, onChange, placeholder }: { value: string; onChange: (value: string) => void; placeholder: string }) {
  return (
    <label className="relative hidden sm:block" title="Searches the rows on this page">
      <Search size={13} className="pointer-events-none absolute left-2 top-1/2 -translate-y-1/2 text-slate-400" aria-hidden />
      <input type="search" value={value} placeholder={placeholder} aria-label={placeholder}
        onChange={(event) => onChange(event.target.value)}
        className="h-7 w-48 rounded-md border border-slate-200 bg-white pl-7 pr-2 text-xs text-slate-700 outline-none focus:border-blue-400" />
    </label>
  )
}

/** "1–100 of 240" with previous / next icons; hidden text when one page holds everything. */
export function CompactPager({ page, pageSize, totalRows, shown, loaded, searching, onPage }: {
  page: number
  pageSize: number
  totalRows: number
  shown: number
  loaded: number
  searching: boolean
  onPage: (page: number) => void
}) {
  const pages = Math.max(1, Math.ceil(totalRows / pageSize))
  const from = totalRows === 0 ? 0 : (page - 1) * pageSize + 1
  const to = Math.min(totalRows, (page - 1) * pageSize + loaded)
  const btn = 'grid h-6 w-6 place-items-center rounded border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40'
  return (
    <div className="mt-2 flex items-center justify-end gap-2 text-[11px] text-slate-500">
      <span>
        {searching ? `${formatCount(shown)} match · ` : ''}
        {formatCount(from)}–{formatCount(to)} of {formatCount(totalRows)}
      </span>
      {pages > 1 && (
        <>
          <button type="button" className={btn} disabled={page <= 1} onClick={() => onPage(page - 1)} aria-label="Previous page"><ChevronLeft size={13} aria-hidden /></button>
          <button type="button" className={btn} disabled={page >= pages} onClick={() => onPage(page + 1)} aria-label="Next page"><ChevronRight size={13} aria-hidden /></button>
        </>
      )}
    </div>
  )
}

/** Active detail filter as removable chips; the cards above are never filtered. */
export function FilterChips({ active, onClear }: { active: string[]; onClear: () => void }) {
  if (active.length === 0) return null
  return (
    <div className="mb-2 flex flex-wrap items-center gap-1.5 text-xs" data-dashboard-filter="active"
      title="Filters narrow the rows below only. The cards above always show the whole queue.">
      <span className="text-slate-500">Showing:</span>
      {active.map((label) => (
        <span key={label} className="rounded-full bg-sky-50 px-2 py-0.5 font-medium text-sky-700">{label}</span>
      ))}
      <button type="button" onClick={onClear} className="font-medium text-blue-600 hover:underline">Show all</button>
    </div>
  )
}
