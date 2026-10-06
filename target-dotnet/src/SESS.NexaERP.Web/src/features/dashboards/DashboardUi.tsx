// Compact dashboard kit shared by the Purchase and Stores dashboards.
// Rules: numbers first, one short label, explanations behind an info icon,
// standard lucide icons, one tone vocabulary (ok / warn / bad / info / muted).
import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import {
  AlertTriangle,
  ArrowRight,
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

/**
 * Card identity colour: the icon tile, the corner wash and (unless the tone
 * says late / ageing) the big number. Tone still wins for warn and bad.
 */
export type Accent = 'rose' | 'blue' | 'emerald' | 'orange' | 'violet' | 'purple' | 'sky' | 'amber' | 'slate'

const ACCENT: Record<Accent, { tile: string; icon: string; text: string; wash: string; stroke: string }> = {
  rose: { tile: 'bg-rose-50', icon: 'text-rose-500', text: 'text-rose-600', wash: 'bg-rose-100', stroke: '#f43f5e' },
  blue: { tile: 'bg-blue-50', icon: 'text-blue-600', text: 'text-blue-700', wash: 'bg-blue-100', stroke: '#2563eb' },
  emerald: { tile: 'bg-emerald-50', icon: 'text-emerald-600', text: 'text-emerald-700', wash: 'bg-emerald-100', stroke: '#10b981' },
  orange: { tile: 'bg-orange-50', icon: 'text-orange-500', text: 'text-orange-600', wash: 'bg-orange-100', stroke: '#f97316' },
  violet: { tile: 'bg-violet-50', icon: 'text-violet-600', text: 'text-violet-700', wash: 'bg-violet-100', stroke: '#7c3aed' },
  purple: { tile: 'bg-purple-50', icon: 'text-purple-600', text: 'text-purple-700', wash: 'bg-purple-100', stroke: '#9333ea' },
  sky: { tile: 'bg-sky-50', icon: 'text-sky-600', text: 'text-sky-700', wash: 'bg-sky-100', stroke: '#0284c7' },
  amber: { tile: 'bg-amber-50', icon: 'text-amber-500', text: 'text-amber-600', wash: 'bg-amber-100', stroke: '#f59e0b' },
  slate: { tile: 'bg-slate-100', icon: 'text-slate-500', text: 'text-slate-900', wash: 'bg-slate-100', stroke: '#64748b' },
}

export function accentClasses(accent: Accent) {
  return ACCENT[accent]
}

/** Tiny trend line for a stat card; flat when every value is zero. */
export function Sparkline({ values, accent }: { values: number[]; accent: Accent }) {
  if (values.length < 2) return null
  const w = 120
  const h = 28
  const max = Math.max(...values)
  const min = Math.min(0, ...values)
  const span = max - min || 1
  const points = values.map((v, i) => [(i / (values.length - 1)) * w, h - 3 - ((v - min) / span) * (h - 6)] as const)
  const line = points.map(([x, y], i) => `${i ? 'L' : 'M'}${x.toFixed(1)},${y.toFixed(1)}`).join(' ')
  const stroke = ACCENT[accent].stroke
  return (
    <svg viewBox={`0 0 ${w} ${h}`} className="h-7 w-full" preserveAspectRatio="none" aria-hidden>
      <path d={`${line} L${w},${h} L0,${h} Z`} fill={stroke} opacity={0.08} />
      <path d={line} fill="none" stroke={stroke} strokeWidth={1.5} strokeLinejoin="round" vectorEffect="non-scaling-stroke" />
      {points.map(([x, y], i) => (
        <circle key={i} cx={x} cy={y} r={1.6} fill={stroke} />
      ))}
    </svg>
  )
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
    <div className="mb-5 flex flex-wrap items-center gap-3">
      <div className="flex items-center gap-3.5">
        <span className="grid h-12 w-12 place-items-center rounded-2xl bg-gradient-to-br from-blue-500 to-blue-700 text-white shadow-md shadow-blue-600/25">
          <Icon size={22} aria-hidden />
        </span>
        <div>
          <h1 className="text-2xl font-bold leading-tight tracking-tight text-slate-900">{title}</h1>
          {context && <div className="mt-0.5 flex items-center gap-1.5 text-sm text-slate-500">{context}</div>}
        </div>
      </div>
      <div className="ml-auto flex items-center gap-2.5 text-sm text-slate-500">
        {right}
        {onRefresh ? (
          <button
            type="button"
            onClick={onRefresh}
            className="inline-flex items-center gap-2 rounded-full px-1 py-1 text-slate-500 hover:text-slate-700"
            title="Refresh now"
            aria-label="Refresh now"
          >
            <span className="grid h-8 w-8 place-items-center rounded-full border border-slate-200 bg-white text-blue-600 shadow-sm">
              <RefreshCw size={15} className={refreshing ? 'animate-spin' : ''} aria-hidden />
            </span>
            {updated}
          </button>
        ) : (
          updated && (
            <span className="inline-flex items-center gap-1">
              <Clock size={13} aria-hidden /> {updated}
            </span>
          )
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
  accent,
  spark,
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
  /** Identity colour of the card; without it the tone colours the icon as before. */
  accent?: Accent
  /** Small trend line under the number. */
  spark?: number[]
}) {
  const t = TONE[tone]
  const a = accent ? ACCENT[accent] : null
  const clickable = !locked && Boolean(to || onClick)
  const numberColor = locked
    ? 'text-slate-400'
    : tone === 'warn' || tone === 'bad'
      ? t.text
      : a ? a.text : tone === 'muted' ? 'text-slate-900' : t.text
  const body = (
    <>
      {a && <span aria-hidden className={`pointer-events-none absolute -bottom-10 -right-10 h-28 w-28 rounded-full opacity-60 blur-2xl ${a.wash}`} />}
      {clickable ? (
        <ChevronRight size={18} aria-hidden className="absolute right-3 top-3 rounded-full border border-slate-200 bg-white p-0.5 text-slate-400" />
      ) : (
        info && <InfoTip text={info} className="absolute right-3 top-3" />
      )}
      <div className="relative flex items-center gap-2.5 pr-5">
        <span className={`grid h-9 w-9 shrink-0 place-items-center rounded-xl ${a ? `${a.tile} ${a.icon}` : `${t.bg} ${t.icon}`}`}>
          {locked ? <Lock size={16} aria-hidden /> : <Icon size={18} aria-hidden />}
        </span>
        <span className="line-clamp-2 min-w-0 flex-1 text-[13px] font-semibold leading-tight text-slate-800" title={label}>{label}</span>
      </div>
      {loading ? (
        <div className="relative mt-3 h-8 w-16 animate-pulse rounded bg-slate-200" />
      ) : (
        <div className={`relative mt-3 text-3xl font-bold tabular-nums leading-none ${numberColor}`}>
          {locked ? '—' : value}
        </div>
      )}
      {!locked && !loading && spark && <div className="relative mt-1.5"><Sparkline values={spark} accent={accent ?? 'slate'} /></div>}
      <div className="relative mt-2 truncate text-xs text-slate-500">{locked ? 'No access' : sub}</div>
    </>
  )
  const cls =
    'relative block overflow-hidden rounded-2xl border border-slate-200/80 bg-white p-4 text-left shadow-sm transition hover:-translate-y-0.5 hover:border-slate-300 hover:shadow-md'
  if (to && !locked) return <Link to={to} className={cls} title={info}>{body}</Link>
  if (onClick && !locked) return <button type="button" onClick={onClick} className={`${cls} w-full`} title={info}>{body}</button>
  return <div className={cls} title={info}>{body}</div>
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
  const urgent = items.some((i) => i.tone === 'bad')
  const head = loading || items.length === 0
    ? { bg: 'bg-emerald-50/70', icon: 'text-emerald-600' }
    : urgent ? { bg: 'bg-rose-50', icon: 'text-rose-600' } : { bg: 'bg-amber-50', icon: 'text-amber-600' }
  return (
    <section className="min-w-0 rounded-2xl border border-slate-200/80 bg-white p-2 shadow-sm">
      <div className={`flex items-center gap-3 rounded-xl px-3.5 py-3 ${head.bg}`}>
        <AlertTriangle size={22} className={head.icon} fill="currentColor" stroke="white" aria-hidden />
        <h2 className="text-base font-semibold text-slate-900">Needs attention</h2>
        {items.length > 0 && <CountBadge n={items.length} tone={urgent ? 'bad' : 'warn'} />}
      </div>
      <div className="px-3 pb-2 pt-2">
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
              <li key={item.key} className="flex items-center gap-3.5 py-3">
                <span className={`grid h-9 w-9 shrink-0 place-items-center rounded-full ${t.bg} ${t.icon}`}>
                  <Icon size={18} aria-hidden />
                </span>
                <span className="min-w-0 flex-1 text-[15px] text-slate-700">{item.text}</span>
                {item.action &&
                  (item.action.to ? (
                    <Link to={item.action.to} className="inline-flex shrink-0 items-center gap-1.5 text-sm font-medium text-blue-600 hover:text-blue-700">
                      {item.action.label} <ArrowRight size={15} aria-hidden />
                    </Link>
                  ) : (
                    <button type="button" onClick={item.action.onClick} className="inline-flex shrink-0 items-center gap-1.5 text-sm font-medium text-blue-600 hover:text-blue-700">
                      {item.action.label} <ArrowRight size={15} aria-hidden />
                    </button>
                  ))}
              </li>
            )
          })}
        </ul>
      )}
      {note && <div className="mt-2 text-[11px] text-slate-400">{note}</div>}
      </div>
    </section>
  )
}

export function CountBadge({ n, tone = 'muted' }: { n: number; tone?: Tone }) {
  const t = TONE[tone]
  const strong = tone === 'bad' ? 'bg-rose-100 text-rose-600' : tone === 'warn' ? 'bg-amber-100 text-amber-700' : `${t.bg} ${t.text}`
  return <span className={`grid h-6 min-w-6 place-items-center rounded-full px-1.5 text-xs font-semibold ${strong}`}>{n}</span>
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
  accent,
  action,
  className = '',
  titleWraps,
}: {
  icon: LucideIcon
  title: string
  info?: string
  right?: ReactNode
  children: ReactNode
  collapsible?: boolean
  defaultOpen?: boolean
  id?: string
  /** Overview style: the icon sits in a tinted tile. */
  accent?: Accent
  /** "View all →" style link at the right of the title row. */
  action?: { label: string; onClick: () => void }
  className?: string
  /** Let a long title wrap to two lines instead of being cut. */
  titleWraps?: boolean
}) {
  const [open, setOpen] = useState(defaultOpen)
  const shown = !collapsible || open
  const a = accent ? ACCENT[accent] : null
  return (
    <section id={id} className={`min-w-0 rounded-2xl border border-slate-200/80 bg-white shadow-sm ${className}`}>
      <div
        className={`flex items-center gap-2.5 ${a ? 'px-5 pb-2 pt-4' : `px-4 py-2.5 ${shown ? 'border-b border-slate-100' : ''}`} ${collapsible ? 'cursor-pointer select-none' : ''}`}
        onClick={collapsible ? () => setOpen(!open) : undefined}
      >
        {a ? (
          <span className={`grid h-9 w-9 shrink-0 place-items-center rounded-xl ${a.tile} ${a.icon}`}>
            <Icon size={18} aria-hidden />
          </span>
        ) : (
          <Icon size={16} className="text-slate-500" aria-hidden />
        )}
        <h2 className={`min-w-0 font-semibold leading-tight text-slate-800 ${titleWraps ? '' : 'truncate'} ${a ? 'cursor-help text-[15px]' : 'text-sm'}`} title={a && info ? info : title}>{title}</h2>
        {/* Overview panels keep the explanation in the title tooltip, as in the design. */}
        {info && !a && <InfoTip text={info} className="shrink-0" />}
        <div className="ml-auto flex shrink-0 items-center gap-2" onClick={(e) => e.stopPropagation()}>
          {right}
          {action && (
            <button type="button" onClick={action.onClick} className="inline-flex items-center gap-1 whitespace-nowrap text-sm font-medium text-blue-600 hover:text-blue-700">
              {action.label} <ArrowRight size={15} aria-hidden />
            </button>
          )}
        </div>
        {collapsible && (open ? <ChevronDown size={16} className="text-slate-400" aria-hidden /> : <ChevronRight size={16} className="text-slate-400" aria-hidden />)}
      </div>
      {shown && <div className={a ? 'px-5 pb-5 pt-2' : 'px-4 py-3'}>{children}</div>}
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
