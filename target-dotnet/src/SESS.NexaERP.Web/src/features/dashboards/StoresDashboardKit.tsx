import { useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { getCompanyCode } from '../../api/dashboards'
import { formatCount } from '../../utils/dashboardFormat'
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

// ---------- ages and colours ----------

export type Tone = 'red' | 'amber' | 'green' | 'info' | 'grey'

/** Kinds of waiting; each is coloured by its own rule below. */
export type AgeKind = 'workload' | 'qc-hold' | 'pending-dc'

/**
 * Display thresholds for colour only; they are not a business rule and never
 * change a count. Workload: 0-1 day green, 2-3 amber, 4+ red. QC hold is red
 * ONLY when the server says a line is past its QC due time. A pending
 * returnable DC is never overdue, so it is never red.
 */
export function ageTone(kind: AgeKind, days: number | null, serverOverdue = false): Tone {
  if (kind === 'qc-hold') {
    if (serverOverdue) return 'red'
    if (days === null) return 'grey'
    return days >= 2 ? 'amber' : 'green'
  }
  if (days === null) return 'grey'
  if (kind === 'pending-dc') return days > 7 ? 'amber' : 'green'
  if (days >= 4) return 'red'
  if (days >= 2) return 'amber'
  return 'green'
}

export const TONE_CHIP: Record<Tone, string> = {
  red: 'bg-red-50 text-red-800 border-red-200',
  amber: 'bg-amber-50 text-amber-800 border-amber-200',
  green: 'bg-emerald-50 text-emerald-800 border-emerald-200',
  info: 'bg-blue-50 text-blue-800 border-blue-200',
  grey: 'bg-slate-50 text-slate-600 border-slate-200',
}

export const TONE_BAR: Record<Tone, string> = {
  red: 'border-l-red-500',
  amber: 'border-l-amber-400',
  green: 'border-l-emerald-500',
  info: 'border-l-blue-500',
  grey: 'border-l-slate-300',
}

export const TONE_DOT: Record<Tone, string> = {
  red: 'bg-red-500',
  amber: 'bg-amber-400',
  green: 'bg-emerald-500',
  info: 'bg-blue-500',
  grey: 'bg-slate-300',
}

export const TONE_WORD: Record<Tone, string> = {
  red: 'Overdue',
  amber: 'Ageing',
  green: 'Clear',
  info: 'Waiting',
  grey: 'Not available',
}

/** "0 d", "4 d": the server's whole-day age, short. */
export function shortAge(days: number | null): string {
  return days === null ? '—' : `${formatCount(days)} d`
}

/** "today", "1 day", "4 days" for sentences. */
export function sentenceAge(days: number): string {
  if (days === 0) return 'less than a day'
  return days === 1 ? '1 day' : `${formatCount(days)} days`
}

export function AgeChip({ days, tone, title }: { days: number | null; tone: Tone; title?: string }) {
  return (
    <span className={`inline-block whitespace-nowrap rounded-md border px-1.5 py-0.5 font-mono text-[12px] tabular-nums ${TONE_CHIP[tone]}`}
      title={title ?? (days === null ? 'No age recorded' : `Waiting ${sentenceAge(days)} (server age)`)}>
      {shortAge(days)}
    </span>
  )
}

export function plural(count: number, singular: string, many = `${singular}s`): string {
  return `${formatCount(count)} ${count === 1 ? singular : many}`
}

// ---------- ageing buckets (for the charts) ----------

export const AGE_BUCKETS = [
  { key: '0-1', label: '0–1 d', min: 0, max: 1 },
  { key: '2-3', label: '2–3 d', min: 2, max: 3 },
  { key: '4-7', label: '4–7 d', min: 4, max: 7 },
  { key: '8+', label: '> 7 d', min: 8, max: Number.POSITIVE_INFINITY },
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
  window.setTimeout(() => document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 0)
}

// ---------- skeletons ----------

export function SkeletonBar({ className = '' }: { className?: string }) {
  return <div className={`animate-pulse rounded bg-slate-200/80 ${className}`} aria-hidden="true" />
}

/** A loading placeholder that still carries the loading state and its words for screen readers and checks. */
export function SectionSkeleton({ label, rows = 4 }: { label: string; rows?: number }) {
  return (
    <div role="status" data-dashboard-state="loading" className="flex flex-col gap-2 py-1">
      <span className="sr-only">Loading: {label}</span>
      <SkeletonBar className="h-9 w-full" />
      {Array.from({ length: rows }, (_, index) => (
        <SkeletonBar key={index} className="h-7" />
      ))}
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

  return { shown, sort, toggle, query, setQuery }
}

export function SortTh<K extends string>({ label, sortKey, sort, onSort, align = 'left' }: {
  label: string
  sortKey: K
  sort: SortState<K> | null
  onSort: (key: K) => void
  align?: 'left' | 'right'
}) {
  const active = sort?.key === sortKey
  const arrow = !active ? '↕' : sort.dir === 'asc' ? '▲' : '▼'
  return (
    <th className={align === 'right' ? 'text-right' : undefined} aria-sort={!active ? 'none' : sort.dir === 'asc' ? 'ascending' : 'descending'}>
      <button type="button" onClick={() => onSort(sortKey)}
        className={`inline-flex cursor-pointer items-center gap-1 border-0 bg-transparent p-0 uppercase tracking-[0.08em] ${active ? 'text-ink' : 'text-ink-faint'} hover:text-ink`}
        title="Sort the rows on this page">
        {label}
        <span className={`text-[9px] ${active ? 'text-accent' : 'opacity-50'}`} aria-hidden="true">{arrow}</span>
      </button>
    </th>
  )
}

export function TableToolbar({ query, onQuery, placeholder, shown, loaded, total }: {
  query: string
  onQuery: (value: string) => void
  placeholder: string
  shown: number
  loaded: number
  total: number
}) {
  return (
    <div className="mb-2 flex flex-wrap items-center gap-2">
      <input type="search" className="input w-full sm:w-72" value={query} placeholder={placeholder}
        onChange={(event) => onQuery(event.target.value)} aria-label={placeholder} />
      <span className="field-hint">
        {query.trim() ? `${formatCount(shown)} of ${formatCount(loaded)} rows on this page match` : `${formatCount(loaded)} of ${formatCount(total)} rows on this page`}
        {' · '}search and sorting apply to this page only
      </span>
    </div>
  )
}

// ---------- small bits ----------

export function RefreshingBadge({ on }: { on: boolean }) {
  if (!on) return null
  return (
    <span className="inline-flex items-center gap-1.5 text-[12px] text-ink-soft" role="status">
      <span className="inline-block size-2 animate-pulse rounded-full bg-blue-500" aria-hidden="true" />
      Refreshing…
    </span>
  )
}

export function StoresMockBanner() {
  const variant = new URLSearchParams(window.location.search).get('mock') ?? 'reference'
  return (
    <div className="alert alert-warn mb-3" data-dashboard-mock="on">
      <div className="alert-body">
        <strong>SYNTHETIC MOCK DATA — not company figures.</strong> Variant: <span className="mono">{variant}</span>.
        Every value comes from the contract's example bodies.
      </div>
    </div>
  )
}

export function Card({ title, subtitle, right, children, id }: { title: ReactNode; subtitle?: ReactNode; right?: ReactNode; children: ReactNode; id?: string }) {
  return (
    <section className="card" id={id}>
      <div className="mb-3 flex flex-wrap items-baseline justify-between gap-2">
        <div>
          <h2 className="text-lg font-semibold">{title}</h2>
          {subtitle && <p className="page-sub">{subtitle}</p>}
        </div>
        {right}
      </div>
      {children}
    </section>
  )
}
