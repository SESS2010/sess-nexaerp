import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import {
  AlertTriangle, Archive, ArrowRight, ArrowUpRight, BadgeAlert, Box, Boxes, Building2, Calculator, CalendarDays,
  CalendarRange, ChartBar, ChartLine, CheckCircle2, ChevronRight, ClipboardList, Cog, CreditCard, Crosshair, DoorOpen,
  FileSpreadsheet, FileText, Handshake, IdCard, Inbox, Landmark, Layers, LayoutGrid, ListChecks, ListTree, Mail, Package,
  PackageCheck, PackageMinus, Receipt, Scale, Search, Send, ShieldCheck, ShoppingCart, SlidersHorizontal, Truck,
  Undo2, Users, Wrench, type LucideIcon,
} from 'lucide-react'
import { getTrackingSummary, historyAvailability } from '../../api/tracking'
import { listItems } from '../../api/items'
import { listPurchaseOrders } from '../../api/purchase'
import { listMaterialIssueRequests } from '../../api/materialIssues'
import type { TrackingSummaryTile } from '../../types/tracking'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { accentClasses, type Accent } from '../dashboards/DashboardUi'
import { catalogSize, shortcutGroups, type CatalogEntry } from './pageCatalog'

// Every module screen is page-permission gated, so an approver, a
// storekeeper and a requester each see a different subset of the app. This
// page is the one place every signed-in role can land: it needs only
// /api/v1/session/me. The shortcuts come from PAGE_CATALOG (the sidebar's own
// pages), filtered by the session's page grants, so a tile never opens onto a
// 403. Each headline figure is requested only when the role may open the
// screen behind it, and is a count the server returned, never an estimate.

type Load<T> = { kind: 'loading' } | { kind: 'hidden' } | { kind: 'ready'; value: T }

function useCount(enabled: boolean, fetch: () => Promise<number>): Load<number> {
  const [state, setState] = useState<Load<number>>({ kind: enabled ? 'loading' : 'hidden' })
  useEffect(() => {
    if (!enabled) { setState({ kind: 'hidden' }); return undefined }
    let cancelled = false
    fetch().then((value) => { if (!cancelled) setState({ kind: 'ready', value }) }).catch(() => { if (!cancelled) setState({ kind: 'hidden' }) })
    return () => { cancelled = true }
    // fetch is a fresh closure each render; enabled is what decides a reload.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [enabled])
  return state
}

function useTrackingSummary(enabled: boolean): Load<TrackingSummaryTile[]> & { error?: unknown } {
  const [state, setState] = useState<Load<TrackingSummaryTile[]> & { error?: unknown }>({ kind: 'loading' })
  useEffect(() => {
    if (!enabled) return undefined
    let cancelled = false
    getTrackingSummary()
      .then((tiles) => { if (!cancelled) setState({ kind: 'ready', value: tiles.filter((tile) => tile.State === 'READY') }) })
      .catch((error: unknown) => {
        if (cancelled) return
        // 403 (no Pending page) and 404 (server without R1) hide the figure quietly.
        setState(historyAvailability(error) === 'error' ? { kind: 'hidden', error } : { kind: 'hidden' })
      })
    return () => { cancelled = true }
  }, [enabled])
  return state
}

const GROUP_ACCENT: Record<string, Accent> = {
  dashboards: 'blue', masters: 'orange', sales: 'amber', purchase: 'blue', stores: 'emerald',
  production: 'orange', accounts: 'rose', reports: 'violet', admin: 'slate',
}

const ENTRY_ICON: Record<string, LucideIcon> = {
  '/tracking/pending': Inbox,
  '/dashboards/purchase': ShoppingCart,
  '/dashboards/stores': Boxes,
  '/employees': Users,
  '/vendors': Building2,
  '/customers': Handshake,
  '/items': Box,
  '/company/profile': Landmark,
  '/sales/customer-po': ClipboardList,
  '/purchase/requisitions': FileText,
  '/purchase/rfqs': Send,
  '/purchase/quotations': FileSpreadsheet,
  '/purchase/comparisons': Scale,
  '/purchase/purchase-orders': ShoppingCart,
  '/stores/stock-check': Layers,
  '/stores/gate-entries': DoorOpen,
  '/stores/goods-receipts': PackageCheck,
  '/qc/inspections': ShieldCheck,
  '/qc/concessions': BadgeAlert,
  '/qc/inspection-policies': ListChecks,
  '/stores/material-issue-requests': ClipboardList,
  '/stores/material-issues': Package,
  '/stores/material-returns': Undo2,
  '/stores/opening-stock': Archive,
  '/stores/machine-deliveries': Truck,
  '/stores/stock-adjustments': SlidersHorizontal,
  '/production/job-orders': Wrench,
  '/design/estimated-boms': Calculator,
  '/production/boms': ListTree,
  '/production/component-fitments': Cog,
  '/accounts/vendor-bills': Receipt,
  '/accounts/vendor-payments': CreditCard,
  '/accounts/inventory-periods': CalendarRange,
  '/reports': ChartBar,
  '/admin/email': Mail,
}

/** Tiles shown before "View all". */
const FIRST_TILES = 12

function greeting(now: Date): string {
  const hour = now.getHours()
  return hour < 12 ? 'Good morning' : hour < 17 ? 'Good afternoon' : 'Good evening'
}

/** QC_MANAGER → "QC Manager"; short words that are acronyms stay upper case. */
export function codeLabel(code: string): string {
  return code.split('_').map((w) => (w.length <= 3 ? w : w[0] + w.slice(1).toLowerCase())).join(' ')
}

const FOCUS = [
  'Open Pending every morning',
  'Work the oldest item first',
  'Check the company at the top',
  'Never share your password or OTP',
]

export function HomePage() {
  const { me, error, can } = useSession()
  const signedIn = me !== null
  const groups = shortcutGroups(can, signedIn)
  const entries = groups.flatMap((group) => group.entries.map((entry) => ({ entry, accent: GROUP_ACCENT[group.id] ?? 'slate' as Accent, group: group.label })))
  const total = catalogSize()
  const [query, setQuery] = useState('')
  const [showAll, setShowAll] = useState(false)
  const now = new Date()

  const summary = useTrackingSummary(signedIn)
  const items = useCount(signedIn && can(PAGE_KEYS.items), () => listItems({ page: 1, pageSize: 1 }).then((r) => r.TotalCount))
  const pos = useCount(signedIn && can(PAGE_KEYS.purchaseOrders), () => listPurchaseOrders({ page: 1, pageSize: 1 }).then((r) => r.TotalCount))
  const mirs = useCount(signedIn && can(PAGE_KEYS.materialIssueRequests), () => listMaterialIssueRequests({ page: 1, pageSize: 1 }).then((r) => r.Total))

  const tiles = summary.kind === 'ready' ? summary.value : []
  const waiting = tiles.reduce((sum, t) => sum + (t.Count ?? 0), 0)
  const overdue = tiles.reduce((sum, t) => sum + (t.OverdueCount ?? 0), 0)
  const busyQueues = tiles.filter((t) => t.Count).length

  // Four headline cards: the four in the design when the role can see them,
  // otherwise filled with figures every role has.
  const cards: KpiProps[] = []
  if (summary.kind !== 'hidden') cards.push({ key: 'pending', icon: FileText, accent: 'blue', label: 'Pending Documents', to: '/tracking/pending', loading: summary.kind === 'loading', value: waiting, note: waiting ? <>{busyQueues} queue{busyQueues === 1 ? '' : 's'}{overdue > 0 && <span className="font-semibold text-rose-600"> · {overdue} overdue</span>}</> : 'Nothing waiting on you' })
  if (items.kind !== 'hidden') cards.push({ key: 'items', icon: Box, accent: 'emerald', label: 'Total Items', to: '/items', loading: items.kind === 'loading', value: items.kind === 'ready' ? items.value : 0, note: 'In the item master' })
  if (pos.kind !== 'hidden') cards.push({ key: 'pos', icon: ShoppingCart, accent: 'orange', label: 'Purchase Orders', to: '/purchase/purchase-orders', loading: pos.kind === 'loading', value: pos.kind === 'ready' ? pos.value : 0, note: 'All statuses' })
  if (mirs.kind !== 'hidden') cards.push({ key: 'mirs', icon: ChartLine, accent: 'violet', label: 'Material Requests', to: '/stores/material-issue-requests', loading: mirs.kind === 'loading', value: mirs.kind === 'ready' ? mirs.value : 0, note: 'All statuses' })
  if (cards.length < 4 && summary.kind === 'ready') cards.push({ key: 'overdue', icon: AlertTriangle, accent: 'rose', label: 'Overdue', to: '/tracking/pending', value: overdue, note: overdue ? 'Past the expected time' : 'All within time' })
  if (cards.length < 4) cards.push({ key: 'screens', icon: LayoutGrid, accent: 'sky', label: 'Screens You Can Open', value: entries.length, note: `of ${total} in NexaERP` })

  const q = query.trim().toLowerCase()
  const matched = useMemo(
    () => (q ? entries.filter(({ entry, group }) => `${entry.label} ${entry.hint} ${group}`.toLowerCase().includes(q)) : entries),
    [entries, q],
  )
  const visibleTiles = q || showAll ? matched : matched.slice(0, FIRST_TILES)

  const name = me?.EmployeeName ?? ''
  const company = (me?.OrganizationId ?? '').replaceAll('_', ' ')

  return (
    <div className="mx-auto max-w-[1600px] space-y-5">
      {/* ---------- hero ---------- */}
      <section className="relative overflow-hidden rounded-[28px] bg-gradient-to-br from-blue-700 via-indigo-700 to-blue-950 px-7 py-8 text-white shadow-xl shadow-indigo-950/25 sm:px-12 sm:py-10">
        <HeroWaves />
        <span aria-hidden className="pointer-events-none absolute -left-24 -top-24 h-72 w-72 rounded-full bg-blue-400/25 blur-3xl" />
        <span aria-hidden className="pointer-events-none absolute -bottom-32 right-1/3 h-80 w-80 rounded-full bg-cyan-400/15 blur-3xl" />
        <div className="relative flex flex-wrap items-center gap-8">
          <div className="min-w-0 flex-1 basis-[28rem]">
            <div className="flex items-center gap-2 text-[15px] font-medium text-blue-100">
              <CalendarDays size={17} aria-hidden />
              {now.toLocaleDateString('en-IN', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
            </div>
            <h1 className="mt-3 text-[34px] font-extrabold leading-tight tracking-tight sm:text-[42px]">
              {greeting(now)}{name && <>, <span className="bg-gradient-to-r from-cyan-300 to-sky-300 bg-clip-text text-transparent">{name}</span></>}
            </h1>
            <p className="mt-2 text-base text-blue-100 sm:text-[17px]">
              Welcome to <strong className="font-semibold text-white">SESS NexaERP</strong> — Manage your business operations in one place.
            </p>
            <div className="mt-6 flex flex-wrap items-center gap-2.5 text-sm font-medium">
              {me?.EmployeeCode && <span className="rounded-full bg-blue-500 px-4 py-1.5 font-semibold shadow-md shadow-blue-950/30">{me.EmployeeCode}</span>}
              {me?.DepartmentCode && <HeroChip icon={Boxes}>{codeLabel(me.DepartmentCode)}</HeroChip>}
              {company && <HeroChip icon={Building2}>{company}</HeroChip>}
              {me?.RoleCodes?.map((role) => <HeroChip key={role} icon={IdCard}>{codeLabel(role)}</HeroChip>)}
            </div>
          </div>
          <div className="w-full shrink-0 rounded-2xl border border-white/15 bg-white/[0.07] p-6 shadow-2xl shadow-blue-950/30 backdrop-blur-md sm:w-[22rem]">
            <div className="flex items-center gap-2.5 text-base font-semibold">
              <span className="grid h-8 w-8 place-items-center rounded-full bg-amber-400/20 text-amber-300"><Crosshair size={18} aria-hidden /></span>
              Today's Focus
            </div>
            <ul className="mt-4 space-y-3">
              {FOCUS.map((text) => (
                <li key={text} className="flex items-center gap-3 text-[15px] text-blue-50">
                  <CheckCircle2 size={20} className="shrink-0 text-white/80" fill="rgba(255,255,255,0.18)" aria-hidden />
                  {text}
                </li>
              ))}
            </ul>
          </div>
        </div>
      </section>

      <ErrorAlert error={error} fallback="Could not load your session." />
      {summary.error !== undefined && <ErrorAlert error={summary.error} fallback="Could not load pending work." />}

      {/* ---------- headline figures ---------- */}
      {signedIn && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {cards.slice(0, 4).map(({ key, ...card }) => <Kpi key={key} {...card} />)}
        </div>
      )}

      {/* ---------- quick access ---------- */}
      <section className="rounded-[24px] border border-slate-200/80 bg-white p-5 shadow-sm sm:p-6">
        <div className="flex flex-wrap items-center gap-4">
          <span className="grid h-14 w-14 place-items-center rounded-2xl bg-blue-50 text-blue-600"><LayoutGrid size={26} aria-hidden /></span>
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-slate-900">Quick access</h2>
            <p className="text-sm text-slate-500">
              {signedIn ? `${entries.length} of ${total} screens open to your role` : 'Pick a screen. A permission message names what is missing.'}
            </p>
          </div>
          <div className="ml-auto flex w-full items-center gap-3 sm:w-auto">
            <label className="relative flex-1 sm:w-80 sm:flex-none">
              <Search size={18} aria-hidden className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
              <input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Find a screen..." aria-label="Find a screen"
                className="w-full rounded-xl border border-slate-200 bg-white py-2.5 pl-11 pr-3 text-[15px] outline-none transition focus:border-blue-300 focus:ring-4 focus:ring-blue-100" />
            </label>
            {matched.length > FIRST_TILES && !q && (
              <button type="button" onClick={() => setShowAll(!showAll)}
                className="inline-flex shrink-0 items-center gap-1.5 rounded-xl border border-blue-200 bg-white px-4 py-2.5 text-[15px] font-semibold text-blue-600 transition hover:bg-blue-50">
                {showAll ? 'Show less' : 'View all'} <ArrowRight size={16} aria-hidden />
              </button>
            )}
          </div>
        </div>

        <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {visibleTiles.map(({ entry, accent }) => <AppTile key={entry.to} entry={entry} accent={accent} />)}
        </div>
        {visibleTiles.length === 0 && (
          <div className="py-10 text-center text-sm text-slate-500">
            {signedIn && entries.length === 0
              ? 'Your role has no page permissions mapped yet. Ask the administrator to map your role.'
              : `No screen matches “${query}”.`}
          </div>
        )}
      </section>
    </div>
  )
}

function HeroChip({ icon: Icon, children }: { icon: LucideIcon; children: ReactNode }) {
  return (
    <span className="inline-flex items-center gap-2 rounded-full bg-white/10 px-4 py-1.5 ring-1 ring-white/15 backdrop-blur">
      <Icon size={16} aria-hidden /> {children}
    </span>
  )
}

/** Soft light streaks across the hero, drawn once in SVG. */
function HeroWaves() {
  return (
    <svg aria-hidden className="pointer-events-none absolute inset-0 h-full w-full" viewBox="0 0 1200 300" preserveAspectRatio="none">
      <defs>
        <linearGradient id="hero-wave" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0" stopColor="#ffffff" stopOpacity="0" />
          <stop offset="0.55" stopColor="#7dd3fc" stopOpacity="0.35" />
          <stop offset="1" stopColor="#ffffff" stopOpacity="0" />
        </linearGradient>
      </defs>
      {[0, 14, 28, 42, 56].map((shift) => (
        <path key={shift} d={`M0 ${250 - shift} C 300 ${170 - shift}, 520 ${300 - shift}, 820 ${130 - shift} S 1120 ${40 - shift}, 1200 ${60 - shift}`}
          fill="none" stroke="url(#hero-wave)" strokeWidth={1.2} />
      ))}
    </svg>
  )
}

interface KpiProps {
  key: string
  icon: LucideIcon
  accent: Accent
  label: string
  value: number
  note: ReactNode
  to?: string
  loading?: boolean
}

function Kpi({ icon: Icon, accent, label, value, note, to, loading }: Omit<KpiProps, 'key'>) {
  const a = accentClasses(accent)
  const body = (
    <>
      <span aria-hidden className={`pointer-events-none absolute -bottom-12 -right-10 h-32 w-32 rounded-full opacity-50 blur-2xl ${a.wash}`} />
      <span className={`relative grid h-16 w-16 shrink-0 place-items-center rounded-2xl ${a.tile} ${a.icon}`}>
        <Icon size={30} strokeWidth={1.8} aria-hidden />
      </span>
      <span className="relative min-w-0 flex-1">
        <span className="block truncate text-[15px] font-semibold text-slate-800" title={label}>{label}</span>
        {loading
          ? <span className="mt-2 block h-8 w-20 animate-pulse rounded bg-slate-200" />
          : <span className="mt-1 block text-[32px] font-bold leading-none tabular-nums text-slate-900">{value.toLocaleString('en-IN')}</span>}
        <span className="mt-2 block truncate text-[13px] text-slate-500">{note}</span>
      </span>
      {to && <ArrowUpRight size={18} aria-hidden className="relative self-start text-slate-300 transition group-hover:text-blue-600" />}
    </>
  )
  const cls = 'group relative flex items-center gap-4 overflow-hidden rounded-[22px] border border-slate-200/70 bg-white p-5 no-underline shadow-sm transition hover:-translate-y-0.5 hover:shadow-md'
  return to ? <Link to={to} className={cls}>{body}</Link> : <div className={cls}>{body}</div>
}

function AppTile({ entry, accent }: { entry: CatalogEntry; accent: Accent }) {
  const a = accentClasses(accent)
  const Icon = ENTRY_ICON[entry.to] ?? PackageMinus
  return (
    <Link to={entry.to}
      className="group flex items-center gap-4 rounded-2xl border border-slate-200/80 bg-white p-4 no-underline shadow-sm transition hover:-translate-y-0.5 hover:border-blue-200 hover:shadow-md">
      <span className={`grid h-14 w-14 shrink-0 place-items-center rounded-2xl ${a.tile} ${a.icon} transition group-hover:scale-105`}>
        <Icon size={26} strokeWidth={1.8} aria-hidden />
      </span>
      <span className="min-w-0 flex-1">
        <span className="block text-[16px] font-bold leading-tight text-slate-900">{entry.label}</span>
        <span className="mt-1 line-clamp-2 text-[13.5px] leading-snug text-slate-500" title={entry.hint}>{entry.hint}</span>
      </span>
      <ChevronRight size={20} aria-hidden className="shrink-0 text-slate-400 transition group-hover:translate-x-0.5 group-hover:text-blue-600" />
    </Link>
  )
}
