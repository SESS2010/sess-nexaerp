// Purchase dashboard plumbing and the few compact pieces the shared kit
// (DashboardUi.tsx) does not have: a controlled collapsible panel, a one-line
// error state, a small search box, filter chips, a segmented control, sortable
// compact table headers and a compact pager.
//
// Nothing here computes a business figure. Age colours are DISPLAY thresholds
// on ages the server returned; they are named in the InfoTips.

import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import {
  AlertTriangle, ArrowDown, ArrowUp, ArrowUpDown, ChevronDown, ChevronLeft, ChevronRight, Lock, RefreshCw, Search, X,
  type LucideIcon,
} from 'lucide-react'
import { ApiError } from '../../api/client'
import { getCompanyCode } from '../../api/dashboards'
import type { SortState } from '../../hooks/useSort'
import type {
  PurchaseObligationsPage, PurchaseOpenOrdersPage, PurchaseSpendingPage, PurchaseSpendingPeriodKey, PurchaseWorkloadPage,
} from '../../types/dashboard'
import { DASHBOARD_ERROR_CODES } from '../../types/dashboard'
import { InfoTip, type Tone } from './DashboardUi'
import { useDashboardQuery, type DashboardQueryState } from './useDashboardQuery'

// ---------- sections ----------

export type PurchaseSectionId = 'workload' | 'openOrders' | 'obligations' | 'spending'

export const SECTION_ANCHOR: Record<PurchaseSectionId, string> = {
  workload: 'purchase-workload',
  openOrders: 'purchase-open-orders',
  obligations: 'purchase-obligations',
  spending: 'purchase-spending',
}

/** What a section tells the page, so the overview reuses its response. */
export type SectionReport<T> =
  | { status: 'loading' }
  | { status: 'ready'; data: T; unfiltered: boolean }
  | { status: 'problem' }

export interface PurchaseReports {
  workload?: SectionReport<PurchaseWorkloadPage>
  openOrders?: SectionReport<PurchaseOpenOrdersPage>
  obligations?: SectionReport<PurchaseObligationsPage>
  spending?: SectionReport<PurchaseSpendingPage>
}

/** What the overview asks a detail panel to show. Every request also opens the panel. */
export interface FocusTarget {
  queue?: string
  overdue?: boolean
  vendor?: { id: string; label: string }
  currency?: string
}

export interface FocusRequest {
  target: FocusTarget
  nonce: number
}

export type OpenSection = (section: PurchaseSectionId, target?: FocusTarget) => void

/** Spending period: only the contract's four keys, or one of the last twelve months. Never a date range. */
export interface SpendPeriod {
  period: PurchaseSpendingPeriodKey
  month: string | null
}

export const PERIOD_SHORT: Record<PurchaseSpendingPeriodKey, string> = {
  month: 'Month',
  quarter: 'Qtr',
  'financial-year': 'FY',
  'twelve-months': '12 M',
}

export const PERIOD_LONG: Record<string, string> = {
  month: 'This month',
  quarter: 'This quarter',
  'financial-year': 'This financial year',
  'twelve-months': 'Last 12 months',
}

export const GST_TIP =
  'Spend = accepted vendor-bill payable INCLUDING GST, plus allocated charges, less reversals. ' +
  'Not ex-tax or Actual BOM cost; no payment status implied. Each currency is shown on its own, never added.'

export type Can = (pageKey: string, action?: string) => boolean

export function scrollToSection(id: string) {
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

// ---------- age tones ----------

export interface AgeThresholds { warn: number; bad: number }

/** Documents waiting in a purchase queue. Display thresholds only. */
export const WORKLOAD_AGE: AgeThresholds = { warn: 3, bad: 7 }
/** Goods received not billed, vendor advances not adjusted. Display thresholds only. */
export const OBLIGATION_AGE: AgeThresholds = { warn: 15, bad: 30 }

export function ageTone(days: number | null | undefined, t: AgeThresholds): Tone {
  if (days === null || days === undefined) return 'ok'
  if (days >= t.bad) return 'bad'
  if (days >= t.warn) return 'warn'
  return 'ok'
}

export const TONE_ORDER: Record<Tone, number> = { bad: 0, warn: 1, info: 2, ok: 3, muted: 4 }

// ---------- money, compact ----------

const compactFormatters = new Map<string, Intl.NumberFormat>()

/** Short figure in Indian units, e.g. "₹1.2L". Exact figures are in tooltips and tables. */
export function formatCompactAmount(value: number, currency: string): string {
  let formatter = compactFormatters.get(currency)
  if (!formatter) {
    try {
      formatter = new Intl.NumberFormat('en-IN', { style: 'currency', currency, notation: 'compact', maximumFractionDigits: 1 })
    } catch {
      formatter = new Intl.NumberFormat('en-IN', { notation: 'compact', maximumFractionDigits: 1 })
    }
    compactFormatters.set(currency, formatter)
  }
  return formatter.format(value)
}

/** INR first, then the rest in the server's order. Currencies are listed, never added. */
export function inrFirst<T extends { Currency: string }>(list: T[]): T[] {
  return [...list].sort((a, b) => (a.Currency === 'INR' ? -1 : b.Currency === 'INR' ? 1 : 0))
}

/** "₹1.2L · US$3K" — one figure per currency. */
export function compactAmounts(list: { Currency: string; value: number }[]): string {
  return inrFirst(list).map((a) => formatCompactAmount(a.value, a.Currency)).join(' · ')
}

// ---------- queries, refresh ----------

/**
 * useDashboardQuery plus auto-refresh: `refreshTick` re-runs the same request,
 * and while a refresh of the SAME request is in flight the previous figures
 * stay on screen. Any error or company mismatch drops them.
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
export function useFocusRequest(focus: FocusRequest | null | undefined, apply: (target: FocusTarget) => void) {
  const seen = useRef<number | null>(null)
  const applyRef = useRef(apply)
  applyRef.current = apply
  useEffect(() => {
    if (!focus || seen.current === focus.nonce) return
    seen.current = focus.nonce
    applyRef.current(focus.target)
  }, [focus])
}

/** Refreshes every `intervalMs` while the tab is visible; a hidden tab makes no requests. */
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
  if (minutes < 60) return `${minutes} min ago`
  const hours = Math.round(minutes / 60)
  return hours === 1 ? '1 hour ago' : `${hours} hours ago`
}

// ---------- rows on screen: sort and search ----------

export const SERVER_ORDER: SortState = { sortBy: '', sortDirection: 'asc' }

type SortValue = string | number | null | undefined

/** Sorts the rows already returned (this page only). An empty sortBy keeps the server's order. */
export function sortRows<T>(rows: T[], sort: SortState, accessors: Record<string, (row: T) => SortValue>): T[] {
  const accessor = accessors[sort.sortBy]
  if (!accessor) return rows
  const direction = sort.sortDirection === 'asc' ? 1 : -1
  const empty = (v: SortValue) => v === null || v === undefined || v === ''
  return rows
    .map((row, index) => ({ row, index, value: accessor(row) }))
    .sort((a, b) => {
      // Missing values always sort last, whatever the direction.
      if (empty(a.value)) return empty(b.value) ? a.index - b.index : 1
      if (empty(b.value)) return -1
      const compared = typeof a.value === 'number' && typeof b.value === 'number'
        ? a.value - b.value
        : String(a.value).localeCompare(String(b.value), 'en-IN', { numeric: true, sensitivity: 'base' })
      return compared === 0 ? a.index - b.index : compared * direction
    })
    .map((entry) => entry.row)
}

export function matchesText(query: string, values: (string | number | null | undefined)[]): boolean {
  const needle = query.trim().toLowerCase()
  if (!needle) return true
  return values.some((value) => value !== null && value !== undefined && String(value).toLowerCase().includes(needle))
}

// ---------- compact UI ----------

/**
 * Collapsible detail panel with the same look as DashboardUi's Panel, but
 * CONTROLLED, so a stat card or attention line can open it.
 */
export function DetailPanel({ id, icon: Icon, title, info, right, open, onToggle, children }: {
  id: string
  icon: LucideIcon
  title: string
  info?: string
  right?: ReactNode
  open: boolean
  onToggle: () => void
  children: ReactNode
}) {
  return (
    <section id={id} className="scroll-mt-4 rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className={`flex cursor-pointer select-none flex-wrap items-center gap-2 px-4 py-2.5 ${open ? 'border-b border-slate-100' : ''}`} onClick={onToggle}>
        <Icon size={16} className="text-slate-500" aria-hidden />
        <h2 className="text-sm font-semibold text-slate-800">
          <button type="button" className="cursor-pointer" aria-expanded={open} onClick={(e) => { e.stopPropagation(); onToggle() }}>{title}</button>
        </h2>
        {info && <InfoTip text={info} />}
        <div className="ml-auto flex items-center gap-2" onClick={(e) => e.stopPropagation()}>{right}</div>
        {open ? <ChevronDown size={16} className="text-slate-400" aria-hidden /> : <ChevronRight size={16} className="text-slate-400" aria-hidden />}
      </div>
      {open && <div className="px-4 py-3">{children}</div>}
    </section>
  )
}

export function SearchBox({ value, onChange, placeholder, onFocus }: { value: string; onChange: (v: string) => void; placeholder: string; onFocus?: () => void }) {
  return (
    <label className="relative inline-flex items-center">
      <span className="sr-only">Search the rows on this page</span>
      <Search size={13} className="pointer-events-none absolute left-2 text-slate-400" aria-hidden />
      <input type="search" value={value} placeholder={placeholder} onFocus={onFocus} onChange={(e) => onChange(e.target.value)}
        className="h-7 w-44 rounded-md border border-slate-200 bg-white pl-7 pr-2 text-xs text-slate-700 placeholder:text-slate-400 focus:border-blue-400 focus:outline-none" />
    </label>
  )
}

export function RefreshDot({ on }: { on: boolean }) {
  if (!on) return null
  return <RefreshCw size={13} className="animate-spin text-slate-400" aria-label="Refreshing" />
}

/** Active detail filter; click to remove. */
export function FilterChip({ label, onClear }: { label: string; onClear: () => void }) {
  return (
    <button type="button" onClick={onClear} title="Remove this filter"
      className="inline-flex items-center gap-1 rounded-full bg-blue-50 px-2 py-0.5 text-[11px] font-medium text-blue-700 hover:bg-blue-100">
      {label} <X size={12} aria-hidden />
    </button>
  )
}

export function Segmented<T extends string>({ options, value, onChange, label }: {
  options: { value: T; label: string; title?: string }[]
  value: T | null
  onChange: (value: T) => void
  label: string
}) {
  return (
    <div role="group" aria-label={label} className="inline-flex rounded-md border border-slate-200 bg-slate-50 p-0.5">
      {options.map((option) => (
        <button key={option.value} type="button" title={option.title} aria-pressed={value === option.value} onClick={() => onChange(option.value)}
          className={`rounded px-2 py-0.5 text-[11px] font-medium ${value === option.value ? 'bg-white text-blue-700 shadow-sm' : 'text-slate-500 hover:text-slate-800'}`}>
          {option.label}
        </button>
      ))}
    </div>
  )
}

/** Document number as a link when the target screen is permitted, plain text otherwise. */
export function DocLink({ to, children }: { to: string | null; children: ReactNode }) {
  return to
    ? <Link to={to} className="font-mono text-xs text-blue-600 hover:underline">{children}</Link>
    : <span className="font-mono text-xs text-slate-700">{children}</span>
}

export function SortTh({ label, sortKey, sort, onSort, right }: { label: string; sortKey: string; sort: SortState; onSort: (key: string) => void; right?: boolean }) {
  const active = sort.sortBy === sortKey
  const Icon = !active ? ArrowUpDown : sort.sortDirection === 'asc' ? ArrowUp : ArrowDown
  return (
    <th className={`py-1.5 pr-3 font-medium ${right ? 'text-right' : ''}`} aria-sort={active ? (sort.sortDirection === 'asc' ? 'ascending' : 'descending') : 'none'}>
      <button type="button" onClick={() => onSort(sortKey)} className={`inline-flex items-center gap-1 hover:text-slate-800 ${active ? 'text-slate-800' : ''}`}>
        {label} <Icon size={11} className={active ? '' : 'opacity-40'} aria-hidden />
      </button>
    </th>
  )
}

export function Th({ children, right }: { children: ReactNode; right?: boolean }) {
  return <th className={`py-1.5 pr-3 font-medium ${right ? 'text-right' : ''}`}>{children}</th>
}

/** Compact table shell: small caps header, hairline rows. */
export function CompactTable({ head, children }: { head: ReactNode; children: ReactNode }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead className="text-left text-[11px] uppercase tracking-wide text-slate-500"><tr>{head}</tr></thead>
        <tbody className="[&>tr]:border-t [&>tr]:border-slate-100 [&_td]:py-1.5 [&_td]:pr-3 [&_td]:align-top">{children}</tbody>
      </table>
    </div>
  )
}

export function NoRows({ cols, text }: { cols: number; text: string }) {
  return <tr><td colSpan={cols} className="text-center text-xs text-slate-400">{text}</td></tr>
}

export function CompactPager({ page, pageSize, totalRows, onPage }: { page: number; pageSize: number; totalRows: number; onPage: (page: number) => void }) {
  const pages = Math.max(1, Math.ceil(totalRows / pageSize))
  const from = totalRows === 0 ? 0 : (page - 1) * pageSize + 1
  const to = Math.min(totalRows, page * pageSize)
  const btn = 'grid h-6 w-6 place-items-center rounded border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40'
  return (
    <div className="mt-2 flex items-center justify-end gap-2 text-[11px] text-slate-500">
      <span className="tabular-nums">{from}–{to} of {totalRows}</span>
      {pages > 1 && (
        <>
          <button type="button" className={btn} disabled={page <= 1} onClick={() => onPage(page - 1)} aria-label="Previous page"><ChevronLeft size={13} aria-hidden /></button>
          <button type="button" className={btn} disabled={page >= pages} onClick={() => onPage(page + 1)} aria-label="Next page"><ChevronRight size={13} aria-hidden /></button>
        </>
      )}
    </div>
  )
}

export function SkeletonRows({ rows = 3 }: { rows?: number }) {
  return (
    <div className="space-y-2" aria-hidden="true">
      {Array.from({ length: rows }, (_, index) => <div key={index} className="h-4 animate-pulse rounded bg-slate-100" />)}
    </div>
  )
}

export function OneLine({ tone = 'muted', children }: { tone?: 'muted' | 'warn' | 'bad'; children: ReactNode }) {
  const cls = tone === 'bad' ? 'bg-rose-50 text-rose-800' : tone === 'warn' ? 'bg-amber-50 text-amber-800' : 'bg-slate-50 text-slate-600'
  return <div className={`flex flex-wrap items-center gap-2 rounded-md px-2.5 py-1.5 text-xs ${cls}`}>{children}</div>
}

type Problem = { kind: 'error'; error: unknown } | { kind: 'company-mismatch'; received: string; expected: string }

/**
 * Every error state as ONE line, keeping what the contract requires: a 409
 * source inconsistency shows no figures, the TraceId stays visible, and a
 * response for another company is discarded.
 */
export function CompactProblem({ state, onRetry }: { state: Problem; onRetry: () => void }) {
  const retry = (
    <button type="button" onClick={onRetry} className="ml-auto inline-flex items-center gap-1 font-medium text-blue-600 hover:underline">
      <RefreshCw size={12} aria-hidden /> Retry
    </button>
  )
  if (state.kind === 'company-mismatch') {
    return (
      <OneLine tone="bad">
        <AlertTriangle size={14} aria-hidden /> Discarded: the answer was for <span className="font-mono">{state.received}</span>, not <span className="font-mono">{state.expected}</span>.
        {retry}
      </OneLine>
    )
  }
  const apiError = state.error instanceof ApiError ? state.error : null
  const status = apiError?.status
  const trace = apiError?.traceId ? <span className="font-mono text-[11px] opacity-75">Ref {apiError.traceId}</span> : null

  if (status === 409 && apiError?.code === DASHBOARD_ERROR_CODES.sourceInconsistent) {
    return (
      <OneLine tone="bad">
        <AlertTriangle size={14} aria-hidden /> <strong>Figures don't reconcile</strong> — none shown; administrator action needed.
        {apiError.message && <InfoTip text={apiError.message} />} {trace}
      </OneLine>
    )
  }
  if (status === 403) {
    return <OneLine><Lock size={13} aria-hidden /> You don't have access to this section. {trace}</OneLine>
  }
  if (status === 401) {
    return (
      <OneLine tone="warn">
        <Lock size={13} aria-hidden /> Your session has ended. {trace}
        <Link to="/login" className="ml-auto font-medium text-blue-600 hover:underline">Sign in</Link>
      </OneLine>
    )
  }
  if (status === 400) {
    return (
      <OneLine tone="warn">
        <AlertTriangle size={14} aria-hidden /> Filter not accepted — clear the filters. {apiError?.message && <InfoTip text={apiError.message} />} {trace}
        {retry}
      </OneLine>
    )
  }
  // 500 and anything unexpected: never show exception text, keep the reference.
  return (
    <OneLine tone="bad">
      <AlertTriangle size={14} aria-hidden /> Couldn't load this section. {trace}
      {retry}
    </OneLine>
  )
}

/** Loading skeleton, one-line problem, or the body. */
export function SectionState<T>({ state, data, onRetry, children }: {
  state: DashboardQueryState<T>
  data: T | null
  onRetry: () => void
  children: (data: T) => ReactNode
}) {
  if (data) return <>{children(data)}</>
  if (state.kind === 'loading' || state.kind === 'ready') return <SkeletonRows />
  return <CompactProblem state={state} onRetry={onRetry} />
}
