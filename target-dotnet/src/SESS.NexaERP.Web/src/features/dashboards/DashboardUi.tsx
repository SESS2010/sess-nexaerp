// Compact dashboard kit shared by the Purchase and Stores dashboards.
// Rules: numbers first, one short label, explanations behind an info icon,
// standard lucide icons, one tone vocabulary (ok / warn / bad / info / muted).
import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import {
  AlertTriangle,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  Clock,
  Info,
  Lock,
  RefreshCw,
  type LucideIcon,
} from 'lucide-react'

export type Tone = 'ok' | 'warn' | 'bad' | 'info' | 'muted'

const TONE: Record<Tone, { text: string; bg: string; ring: string; bar: string; icon: string }> = {
  ok: { text: 'text-emerald-700', bg: 'bg-emerald-50', ring: 'ring-emerald-200', bar: 'bg-emerald-500', icon: 'text-emerald-600' },
  warn: { text: 'text-amber-700', bg: 'bg-amber-50', ring: 'ring-amber-200', bar: 'bg-amber-500', icon: 'text-amber-600' },
  bad: { text: 'text-rose-700', bg: 'bg-rose-50', ring: 'ring-rose-200', bar: 'bg-rose-500', icon: 'text-rose-600' },
  info: { text: 'text-sky-700', bg: 'bg-sky-50', ring: 'ring-sky-200', bar: 'bg-sky-500', icon: 'text-sky-600' },
  muted: { text: 'text-slate-500', bg: 'bg-slate-50', ring: 'ring-slate-200', bar: 'bg-slate-300', icon: 'text-slate-400' },
}

export function toneClasses(tone: Tone) {
  return TONE[tone]
}

/** Small (i) icon; the full explanation is in the native tooltip, never on the page. */
export function InfoTip({ text, className = '' }: { text: string; className?: string }) {
  return (
    <span className={`inline-flex cursor-help text-slate-400 hover:text-slate-600 ${className}`} title={text} aria-label={text}>
      <Info size={14} aria-hidden />
    </span>
  )
}

/** Page header: title, one-line context, "Updated …" and a Refresh icon button. */
export function DashHeader({
  icon: Icon,
  title,
  context,
  updated,
  refreshing,
  onRefresh,
  right,
}: {
  icon: LucideIcon
  title: string
  context?: ReactNode
  updated?: string
  refreshing?: boolean
  onRefresh?: () => void
  right?: ReactNode
}) {
  return (
    <div className="mb-4 flex flex-wrap items-center gap-3">
      <div className="flex items-center gap-2.5">
        <span className="grid h-9 w-9 place-items-center rounded-lg bg-blue-600 text-white">
          <Icon size={18} aria-hidden />
        </span>
        <div>
          <h1 className="text-lg font-semibold leading-tight text-slate-900">{title}</h1>
          {context && <div className="text-xs text-slate-500">{context}</div>}
        </div>
      </div>
      <div className="ml-auto flex items-center gap-2 text-xs text-slate-500">
        {right}
        {updated && (
          <span className="inline-flex items-center gap-1">
            <Clock size={13} aria-hidden /> {updated}
          </span>
        )}
        {onRefresh && (
          <button
            type="button"
            onClick={onRefresh}
            className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-slate-200 bg-white text-slate-600 hover:bg-slate-50"
            title="Refresh now"
            aria-label="Refresh now"
          >
            <RefreshCw size={15} className={refreshing ? 'animate-spin' : ''} aria-hidden />
          </button>
        )}
      </div>
    </div>
  )
}

/**
 * Compact KPI widget (~84 px tall): icon, short label, big number, one small
 * sub-line. Tone colours the icon and the number only.
 */
export function StatCard({
  icon: Icon,
  label,
  value,
  sub,
  tone = 'muted',
  info,
  onClick,
  to,
  locked,
  loading,
}: {
  icon: LucideIcon
  label: string
  value: ReactNode
  sub?: ReactNode
  tone?: Tone
  info?: string
  onClick?: () => void
  to?: string
  locked?: boolean
  loading?: boolean
}) {
  const t = TONE[tone]
  const body = (
    <>
      <div className="flex items-center gap-2">
        <span className={`grid h-7 w-7 shrink-0 place-items-center rounded-md ${t.bg} ${t.icon}`}>
          {locked ? <Lock size={14} aria-hidden /> : <Icon size={15} aria-hidden />}
        </span>
        <span className="line-clamp-2 text-xs font-medium leading-tight text-slate-600" title={label}>{label}</span>
        {info && <InfoTip text={info} className="ml-auto" />}
      </div>
      {loading ? (
        <div className="mt-2 h-6 w-16 animate-pulse rounded bg-slate-200" />
      ) : (
        <div className={`mt-1.5 text-2xl font-semibold tabular-nums leading-none ${locked ? 'text-slate-400' : tone === 'muted' ? 'text-slate-900' : t.text}`}>
          {locked ? '—' : value}
        </div>
      )}
      <div className="mt-1 truncate text-[11px] text-slate-500">{locked ? 'No access' : sub}</div>
    </>
  )
  const cls =
    'block rounded-xl border border-slate-200 bg-white px-3.5 py-3 text-left shadow-sm transition hover:border-slate-300 hover:shadow'
  if (to && !locked) return <Link to={to} className={cls}>{body}</Link>
  if (onClick && !locked) return <button type="button" onClick={onClick} className={`${cls} w-full`}>{body}</button>
  return <div className={cls}>{body}</div>
}

/** Responsive grid for StatCards: 2 → 3 → 6 columns. */
export function StatGrid({ children }: { children: ReactNode }) {
  return <div className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-6">{children}</div>
}

export interface AttentionItem {
  key: string
  tone: Exclude<Tone, 'muted'>
  text: ReactNode
  action?: { label: string; to?: string; onClick?: () => void }
}

/** "Needs attention" list: one line per item, icon + sentence + one action. */
export function AttentionList({
  items,
  loading,
  clearText = 'All clear. Nothing needs you right now.',
  note,
}: {
  items: AttentionItem[]
  loading?: boolean
  clearText?: string
  note?: ReactNode
}) {
  return (
    <Panel icon={AlertTriangle} title="Needs attention" right={items.length > 0 ? <CountBadge n={items.length} tone={items.some((i) => i.tone === 'bad') ? 'bad' : 'warn'} /> : undefined}>
      {loading ? (
        <div className="space-y-2">
          <div className="h-8 animate-pulse rounded bg-slate-100" />
          <div className="h-8 animate-pulse rounded bg-slate-100" />
        </div>
      ) : items.length === 0 ? (
        <div className="flex items-center gap-2 rounded-lg bg-emerald-50 px-3 py-2.5 text-sm text-emerald-800">
          <CheckCircle2 size={17} className="text-emerald-600" aria-hidden />
          {clearText}
        </div>
      ) : (
        <ul className="divide-y divide-slate-100">
          {items.map((item) => {
            const t = TONE[item.tone]
            const Icon = item.tone === 'bad' ? AlertTriangle : item.tone === 'warn' ? Clock : Info
            return (
              <li key={item.key} className="flex items-center gap-3 py-2">
                <span className={`grid h-6 w-6 shrink-0 place-items-center rounded-full ${t.bg} ${t.icon}`}>
                  <Icon size={13} aria-hidden />
                </span>
                <span className="min-w-0 flex-1 text-sm text-slate-700">{item.text}</span>
                {item.action &&
                  (item.action.to ? (
                    <Link to={item.action.to} className="inline-flex shrink-0 items-center gap-0.5 text-xs font-medium text-blue-600 hover:underline">
                      {item.action.label} <ChevronRight size={13} aria-hidden />
                    </Link>
                  ) : (
                    <button type="button" onClick={item.action.onClick} className="inline-flex shrink-0 items-center gap-0.5 text-xs font-medium text-blue-600 hover:underline">
                      {item.action.label} <ChevronRight size={13} aria-hidden />
                    </button>
                  ))}
              </li>
            )
          })}
        </ul>
      )}
      {note && <div className="mt-2 text-[11px] text-slate-400">{note}</div>}
    </Panel>
  )
}

export function CountBadge({ n, tone = 'muted' }: { n: number; tone?: Tone }) {
  const t = TONE[tone]
  return <span className={`rounded-full px-2 py-0.5 text-[11px] font-semibold ${t.bg} ${t.text}`}>{n}</span>
}

/** White card with an icon title row. Collapsible panels start closed unless told otherwise. */
export function Panel({
  icon: Icon,
  title,
  info,
  right,
  children,
  collapsible,
  defaultOpen = true,
  id,
}: {
  icon: LucideIcon
  title: string
  info?: string
  right?: ReactNode
  children: ReactNode
  collapsible?: boolean
  defaultOpen?: boolean
  id?: string
}) {
  const [open, setOpen] = useState(defaultOpen)
  const shown = !collapsible || open
  return (
    <section id={id} className="rounded-xl border border-slate-200 bg-white shadow-sm">
      <div
        className={`flex items-center gap-2 px-4 py-2.5 ${shown ? 'border-b border-slate-100' : ''} ${collapsible ? 'cursor-pointer select-none' : ''}`}
        onClick={collapsible ? () => setOpen(!open) : undefined}
      >
        <Icon size={16} className="text-slate-500" aria-hidden />
        <h2 className="text-sm font-semibold text-slate-800">{title}</h2>
        {info && <InfoTip text={info} />}
        <div className="ml-auto flex items-center gap-2" onClick={(e) => e.stopPropagation()}>
          {right}
        </div>
        {collapsible && (open ? <ChevronDown size={16} className="text-slate-400" aria-hidden /> : <ChevronRight size={16} className="text-slate-400" aria-hidden />)}
      </div>
      {shown && <div className="px-4 py-3">{children}</div>}
    </section>
  )
}

/** Horizontal bars on one shared scale; value printed at the end of each bar. */
export function MiniBars({
  rows,
  format = (v) => String(v),
  emptyText = 'Nothing to show',
}: {
  rows: { key: string; label: string; value: number; tone?: Tone; onClick?: () => void }[]
  format?: (v: number) => string
  emptyText?: string
}) {
  const max = Math.max(0, ...rows.map((r) => r.value))
  if (rows.length === 0 || max === 0) return <div className="py-2 text-xs text-slate-400">{emptyText}</div>
  return (
    <ul className="space-y-1.5">
      {rows.map((r) => (
        <li key={r.key} className={`grid grid-cols-[minmax(0,10rem)_1fr_auto] items-center gap-2 text-xs ${r.onClick ? 'cursor-pointer' : ''}`} onClick={r.onClick}>
          <span className="truncate text-slate-600" title={r.label}>{r.label}</span>
          <span className="h-2 rounded-full bg-slate-100">
            <span className={`block h-2 rounded-full ${TONE[r.tone ?? 'info'].bar}`} style={{ width: `${(r.value / max) * 100}%` }} />
          </span>
          <span className="tabular-nums text-slate-700">{format(r.value)}</span>
        </li>
      ))}
    </ul>
  )
}

/** "4 d" chip coloured by tone. */
export function AgeChip({ days, tone }: { days: number | null | undefined; tone: Tone }) {
  if (days === null || days === undefined) return <span className="text-xs text-slate-400">—</span>
  const t = TONE[tone]
  return <span className={`inline-block rounded px-1.5 py-0.5 text-[11px] font-medium tabular-nums ${t.bg} ${t.text}`}>{days} d</span>
}

/** Access-denied state for a whole section: one line, lock icon. */
export function NoAccess({ what }: { what: string }) {
  return (
    <div className="flex items-center gap-2 text-xs text-slate-500">
      <Lock size={13} aria-hidden /> You don't have access to {what}.
    </div>
  )
}
