// Overview panels: spend trend (with the period selector), purchase queues,
// open orders by delivery, and what is owed to vendors. Each chart holds ONE
// currency; currencies are never added or drawn on a shared axis.

import { useState, type ReactNode } from 'react'
import {
  AlertTriangle, CalendarDays, ClipboardCheck, ClipboardList, Clock3, Database, FileCheck2, FileText, IndianRupee, Layers, Lock,
  PackageCheck, Receipt, Send, TrendingUp, Truck, Users, type LucideIcon,
} from 'lucide-react'
import { formatAmount, formatDateOnly, formatMonth } from '../../utils/dashboardFormat'
import { MiniBars, NoAccess, Panel, accentClasses, type Accent } from './DashboardUi'
import {
  GST_TIP, OBLIGATION_AGE, OneLine, Segmented, SkeletonRows, WORKLOAD_AGE, ageTone,
  formatCompactAmount, inrFirst, type OpenSection, type PurchaseReports, type PurchaseSectionId, type SectionReport, type SpendPeriod,
} from './PurchaseDashboardKit'
import { QUEUE_SHORT } from './PurchaseWorkloadSection'

/** Loading / problem / locked for an overview panel; null when the report is ready. */
function panelState<T>({ allowed, report, what, section, onOpen }: {
  allowed: boolean
  report: SectionReport<T> | undefined
  what: string
  section: PurchaseSectionId
  onOpen: OpenSection
}): ReactNode {
  if (!allowed) return <NoAccess what={what} />
  if (!report || report.status === 'loading') return <SkeletonRows rows={4} />
  if (report.status === 'problem') {
    return (
      <OneLine tone="warn">
        Couldn't load.
        <button type="button" className="ml-auto font-medium text-blue-600 hover:underline" onClick={() => onOpen(section)}>Details</button>
      </OneLine>
    )
  }
  return null
}

// ---------- spend trend ----------

// Chart ranges. The server's monthly trend covers the last twelve months, so
// 1M/3M/6M/FY/12M are windows onto it. A week needs daily figures the API does
// not return, so 1W is shown disabled rather than faked.
type SpendRange = '1W' | '1M' | '3M' | '6M' | 'FY' | '12M'
const RANGES: { value: SpendRange; label: string; title: string; disabled?: boolean }[] = [
  { value: '1W', label: '1W', title: 'Weekly figures need a backend change: the server reports spend by month only.', disabled: true },
  { value: '1M', label: '1M', title: 'This month' },
  { value: '3M', label: '3M', title: 'Last 3 months (this month and the two before)' },
  { value: '6M', label: '6M', title: 'Last 6 months' },
  { value: 'FY', label: 'FY', title: 'This financial year, from 1 April' },
  { value: '12M', label: '12M', title: 'Last 12 months' },
]
const RANGE_LONG: Record<SpendRange, string> = {
  '1W': 'This week', '1M': 'This month', '3M': 'Last 3 months', '6M': 'Last 6 months', FY: 'This financial year', '12M': 'Last 12 months',
}

/** First day of the Indian financial year (1 April) that contains the given yyyy-mm-dd. */
function fyStart(isoDate: string): string {
  const [y, m] = isoDate.split('-').map(Number)
  return `${m >= 4 ? y : y - 1}-04-01`
}

function RangeChips({ value, onChange }: { value: SpendRange; onChange: (r: SpendRange) => void }) {
  return (
    <div role="group" aria-label="Spend range" className="flex flex-wrap justify-end gap-1">
      {RANGES.map((r) => (
        <button
          key={r.value}
          type="button"
          title={r.title}
          disabled={r.disabled}
          aria-pressed={value === r.value}
          onClick={() => onChange(r.value)}
          className={`min-w-[2.4rem] rounded-lg border px-2 py-1 text-xs font-medium ${
            r.disabled ? 'cursor-not-allowed border-slate-100 text-slate-300'
              : value === r.value ? 'border-blue-200 bg-blue-50 text-blue-700' : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300 hover:text-slate-900'}`}
        >
          {r.label}
        </button>
      ))}
    </div>
  )
}

export function SpendTrendPanel({ report, allowed, period, onPeriod, onOpen }: {
  report: PurchaseReports['spending']
  allowed: boolean
  period: SpendPeriod
  onPeriod: (next: SpendPeriod) => void
  onOpen: OpenSection
}) {
  const [pickedCurrency, setPickedCurrency] = useState<string | null>(null)
  const [range, setRange] = useState<SpendRange>(() =>
    period.period === 'twelve-months' ? '12M' : period.period === 'month' ? '1M' : 'FY')
  const stateNode = panelState({ allowed, report, what: 'spending', section: 'spending', onOpen })

  // Keep Spending details in step where the server has a matching period;
  // 3M and 6M have none, so the details show the last twelve months.
  const pickRange = (next: SpendRange) => {
    setRange(next)
    if (next === 'FY') onPeriod({ period: 'financial-year', month: null })
    else if (next === '1M') onPeriod({ period: 'month', month: null })
    else onPeriod({ period: 'twelve-months', month: null })
  }
  const right = allowed ? <RangeChips value={range} onChange={pickRange} /> : undefined

  let body: ReactNode = stateNode
  if (!stateNode && report?.status === 'ready') {
    const data = report.data
    const currencies = inrFirst(
      [...new Set(data.MonthlyTrend.flatMap((month) => month.Amounts.map((a) => a.Currency)))].map((Currency) => ({ Currency })),
    ).map((c) => c.Currency)
    const currency = pickedCurrency && currencies.includes(pickedCurrency) ? pickedCurrency : currencies[0] ?? null
    const selectedMonth = data.Filters.Period === 'month' && data.Filters.Month ? data.FromDate : null

    const trend = [...data.MonthlyTrend].sort((a, b) => a.FromDate.localeCompare(b.FromDate))
    const latest = trend.length ? trend[trend.length - 1].FromDate : null
    const windowed = range === '1M' ? trend.slice(-1)
      : range === '3M' ? trend.slice(-3)
      : range === '6M' ? trend.slice(-6)
      : range === 'FY' && latest ? trend.filter((m) => m.FromDate >= fyStart(latest))
      : trend
    const bars: ChartMonth[] = windowed.map((month) => {
      const amount = currency ? month.Amounts.find((a) => a.Currency === currency) : undefined
      return { key: month.Key, fromDate: month.FromDate, value: amount?.Amount ?? 0, bills: amount?.BillCount ?? 0 }
    })
    // FY view shows the whole year, April to March; months still to come are
    // drawn as empty placeholders, never as figures.
    const shown: ChartMonth[] = range === 'FY' && latest ? padToMarch(bars, latest) : bars
    // One currency only: the bars shown, added up.
    const total = bars.reduce((sum, b) => sum + b.value, 0)
    const bills = bars.reduce((sum, b) => sum + b.bills, 0)
    const from = windowed[0]?.FromDate
    const to = windowed.length ? windowed[windowed.length - 1].ToDate : undefined

    // No bills at all: draw the months at zero in rupees rather than an empty box.
    const chartCurrency = currency ?? 'INR'
    const noSpend = currency === null || bills === 0
    body = (
      <>
        <div className="flex flex-col gap-4 sm:flex-row">
          <div className="min-w-0 flex-1">
            {shown.length === 0 ? (
              <div className="grid h-[190px] place-items-center text-xs text-slate-400">No months to show</div>
            ) : (
              <ColumnChart
                currency={chartCurrency}
                months={shown}
                selected={selectedMonth}
                onPick={(fromDate) => onPeriod({ period: 'month', month: fromDate })}
              />
            )}
          </div>
          <div className="flex shrink-0 flex-col gap-3 sm:w-[11.5rem]">
            <div className="rounded-xl bg-blue-50/70 p-3.5">
              <div className="flex items-start gap-2.5">
                <span className="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-white text-blue-600 shadow-sm">
                  <IndianRupee size={16} aria-hidden />
                </span>
                <div className="min-w-0">
                  <div className="text-[11px] font-medium text-slate-500">Total spend ({range})</div>
                  <div className="truncate text-lg font-bold tabular-nums text-slate-900">
                    {noSpend ? 'No spend' : formatAmount(total, chartCurrency)}
                  </div>
                </div>
              </div>
              {!noSpend && <div className="mt-1 text-[11px] text-amber-700">incl. GST · {bills} bill{bills === 1 ? '' : 's'}</div>}
            </div>
            <div className="rounded-xl bg-violet-50/60 p-3.5">
              <div className="flex items-start gap-2.5">
                <span className="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-white text-violet-600 shadow-sm">
                  <CalendarDays size={16} aria-hidden />
                </span>
                <div className="min-w-0">
                  <div className="text-[11px] font-medium text-slate-500">{RANGE_LONG[range]}</div>
                  <div className="text-lg font-bold text-slate-900">{bills === 0 ? 'No bills' : `${bills} bill${bills === 1 ? '' : 's'}`}</div>
                </div>
              </div>
            </div>
            {from && to && <div className="px-1 text-[11px] text-slate-400">{formatDateOnly(from)} – {formatDateOnly(to)}</div>}
          </div>
        </div>
        {currencies.length > 1 && (
          <div className="mt-2">
            <Segmented label="Currency" value={currency} onChange={setPickedCurrency} options={currencies.map((c) => ({ value: c, label: c }))} />
          </div>
        )}
      </>
    )
  }

  return (
    <Panel icon={TrendingUp} accent="blue" title="Spend trend" info={`${GST_TIP} Pick a range; click a month's bar to see that month in Spending details. 1W needs weekly figures the server does not provide yet.`} right={right}>
      {body}
    </Panel>
  )
}

const MONTH_SHORT = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

/** Axis top as four round steps (1, 2, 2.5 or 5 × a power of ten each): 0, 2K, 4K, 6K, 8K. */
function niceMax(value: number): number {
  if (value <= 0) return 0
  const quarter = value / 4
  const power = 10 ** Math.floor(Math.log10(quarter))
  const step = [1, 2, 2.5, 5, 10].find((s) => s * power >= quarter) ?? 10
  return step * power * 4
}

interface ChartMonth { key: string; fromDate: string; value: number; bills: number; future?: boolean }

/** Adds the remaining months up to March as empty, unclickable placeholders. */
function padToMarch(months: ChartMonth[], latest: string): ChartMonth[] {
  const out = [...months]
  let [y, m] = latest.split('-').map(Number)
  while (m !== 3) {
    m = m === 12 ? 1 : m + 1
    if (m === 1) y += 1
    const key = `${y}-${String(m).padStart(2, '0')}`
    out.push({ key, fromDate: `${key}-01`, value: 0, bills: 0, future: true })
  }
  return out
}

/** Bars per month with a line through their tops; click a bar to drill into that month. */
function ColumnChart({ currency, months, selected, onPick }: {
  currency: string
  months: ChartMonth[]
  selected: string | null
  onPick: (fromDate: string) => void
}) {
  // With no spend yet the axis still needs a scale; a nominal ₹1L keeps the
  // grid readable without inventing any figure.
  const top = niceMax(Math.max(0, ...months.map((m) => m.value))) || 100_000
  const ticks = [4, 3, 2, 1, 0].map((i) => (top / 4) * i)
  const pct = (v: number) => Math.max(0, Math.min(100, (v / top) * 100))
  const step = 100 / months.length
  const actual = months.map((m, i) => ({ m, i })).filter(({ m }) => !m.future)
  const linePoints = actual.map(({ m, i }) => `${(i + 0.5) * step},${100 - pct(m.value)}`).join(' ')
  return (
    <figure className="m-0">
      <figcaption className="sr-only">Monthly spend in {currency}, incl. GST, oldest month first</figcaption>
      <div className="flex gap-2">
        <div className="relative h-[170px] w-9 shrink-0 text-right text-[10px] tabular-nums text-slate-400">
          {ticks.map((t) => (
            <span key={t} className="absolute right-0 -translate-y-1/2" style={{ top: `${100 - pct(t)}%` }}>
              {t === 0 ? '0' : formatCompactAmount(t, currency).replace(/^[^\d-]+/, '')}
            </span>
          ))}
        </div>
        <div className="relative h-[170px] min-w-0 flex-1">
          {ticks.map((t) => (
            <span key={t} aria-hidden className={`absolute inset-x-0 border-t ${t === 0 ? 'border-slate-200' : 'border-dashed border-slate-100'}`} style={{ top: `${100 - pct(t)}%` }} />
          ))}
          <div className="absolute inset-0 flex items-end">
            {months.map((m) => {
              if (m.future) return <span key={m.key} className="h-full min-w-0 flex-1" title={`${formatMonth(m.key)}: still to come`} />
              const isSelected = selected === m.fromDate
              const tip = `${formatMonth(m.key)}: ${m.value === 0 && m.bills === 0 ? 'no bills' : `${formatAmount(m.value, currency)} incl. GST · ${m.bills} bill${m.bills === 1 ? '' : 's'}`}`
              const color = m.value < 0
                ? 'bg-rose-400'
                : isSelected ? 'bg-gradient-to-t from-blue-600 to-blue-500' : 'bg-gradient-to-t from-blue-400 to-blue-300 group-hover:from-blue-500 group-hover:to-blue-400'
              return (
                <button key={m.key} type="button" title={tip} aria-label={tip} aria-pressed={isSelected} onClick={() => onPick(m.fromDate)}
                  className="group flex h-full min-w-0 flex-1 cursor-pointer flex-col items-center justify-end">
                  <span className={`block w-3/5 max-w-[22px] rounded-t ${color}`} style={{ height: `${pct(Math.abs(m.value))}%`, minHeight: m.value !== 0 ? 3 : 0 }} />
                </button>
              )
            })}
          </div>
          {actual.length > 1 && (
            <svg className="pointer-events-none absolute inset-0 h-full w-full overflow-visible" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden>
              <polyline points={linePoints} fill="none" stroke="#6d28d9" strokeWidth={2} strokeLinejoin="round" vectorEffect="non-scaling-stroke" />
            </svg>
          )}
          {actual.length > 1 && actual.map(({ m, i }) => (
            <span key={m.key} aria-hidden className="pointer-events-none absolute h-2 w-2 -translate-x-1/2 translate-y-1/2 rounded-full border-2 border-violet-700 bg-white"
              style={{ left: `${(i + 0.5) * step}%`, bottom: `${pct(m.value)}%` }} />
          ))}
        </div>
      </div>
      <div className="ml-11 mt-1.5 flex">
        {months.map((m) => {
          const month = Number(m.key.split('-')[1])
          return (
            <span key={m.key} className={`flex-1 text-center text-[11px] ${selected === m.fromDate ? 'font-semibold text-blue-700' : m.future ? 'text-slate-300' : 'text-slate-500'}`}>
              {MONTH_SHORT[month - 1] ?? '·'}
            </span>
          )
        })}
      </div>
    </figure>
  )
}

// ---------- purchase queues ----------

export function QueuesPanel({ report, allowed, onOpen }: { report: PurchaseReports['workload']; allowed: boolean; onOpen: OpenSection }) {
  const stateNode = panelState({ allowed, report, what: 'purchase queues', section: 'workload', onOpen })
  let body: ReactNode = stateNode
  if (!stateNode && report?.status === 'ready') {
    const ready = report.data.Tiles.filter((tile) => tile.State === 'READY')
    const hidden = report.data.Tiles.length - ready.length
    const max = Math.max(1, ...ready.map((tile) => tile.Count ?? 0))
    body = (
      <>
        {ready.length === 0 ? (
          <div className="py-2 text-xs text-slate-400">Nothing waiting</div>
        ) : (
          <ul className="space-y-1">
            {ready.map((tile) => {
              const count = tile.Count ?? 0
              const tone = count ? ageTone(tile.OldestAgeDays, WORKLOAD_AGE) : 'muted'
              const look = QUEUE_LOOK[tile.Key] ?? { icon: ClipboardList, accent: 'blue' as Accent }
              const a = accentClasses(tone === 'bad' ? 'rose' : tone === 'warn' ? 'amber' : look.accent)
              const Icon = tone === 'bad' ? AlertTriangle : tone === 'warn' ? Clock3 : look.icon
              const bar = tone === 'bad' ? 'bg-gradient-to-r from-rose-500 to-pink-500'
                : tone === 'warn' ? 'bg-gradient-to-r from-amber-400 to-orange-500'
                : count ? 'bg-gradient-to-r from-emerald-400 to-emerald-500' : ''
              const label = QUEUE_SHORT[tile.Key] ?? tile.Title
              return (
                <li key={tile.Key}>
                  <button type="button" onClick={() => onOpen('workload', { queue: tile.Key })}
                    title={`${label}: ${count}${tile.OldestAgeDays !== null && count ? ` · oldest ${tile.OldestAgeDays} d` : ''}`}
                    className="grid w-full grid-cols-[1.75rem_minmax(0,8.5rem)_1fr_1.5rem] items-center gap-2.5 rounded-lg px-1 py-1.5 text-left hover:bg-slate-50">
                    <span className={`grid h-7 w-7 place-items-center rounded-full ${a.tile} ${a.icon}`}><Icon size={14} aria-hidden /></span>
                    <span className="truncate text-sm text-slate-700">{label}</span>
                    <span className="h-2 rounded-full bg-gradient-to-r from-blue-100 to-slate-100">
                      {count > 0 && <span className={`block h-2 rounded-full ${bar}`} style={{ width: `${(count / max) * 100}%` }} />}
                    </span>
                    <span className="text-right text-sm font-semibold tabular-nums text-blue-700">{count}</span>
                  </button>
                </li>
              )
            })}
          </ul>
        )}
        {hidden > 0 && <div className="mt-2 flex items-center gap-1 text-[11px] text-slate-400"><Lock size={11} aria-hidden /> {hidden} queue{hidden === 1 ? '' : 's'} hidden</div>}
      </>
    )
  }
  return (
    <Panel icon={ClipboardList} accent="blue" title="Purchase queues" className="h-full"
      action={allowed ? { label: 'View all', onClick: () => onOpen('workload') } : undefined}
      info={`Documents waiting in each queue. Colour = oldest wait: green fresh, amber from ${WORKLOAD_AGE.warn} d, red from ${WORKLOAD_AGE.bad} d. Click a row to list it.`}>
      {body}
    </Panel>
  )
}

/** Icon and colour per workload queue while it is fresh; ageing turns it amber / red. */
const QUEUE_LOOK: Record<string, { icon: LucideIcon; accent: Accent }> = {
  'comparison-decision': { icon: Users, accent: 'blue' },
  'po-approved-unissued': { icon: FileText, accent: 'blue' },
  'pr-approval': { icon: FileCheck2, accent: 'violet' },
  'pr-department-verification': { icon: Clock3, accent: 'orange' },
  'pr-stock-check': { icon: Layers, accent: 'emerald' },
  'quotation-technical-verification': { icon: ClipboardCheck, accent: 'sky' },
  'rfq-no-quotation': { icon: Send, accent: 'purple' },
}

// ---------- open orders by delivery ----------

export function DeliveryPanel({ report, allowed, onOpen }: { report: PurchaseReports['openOrders']; allowed: boolean; onOpen: OpenSection }) {
  const stateNode = panelState({ allowed, report, what: 'open POs', section: 'openOrders', onOpen })
  let body: ReactNode = stateNode
  if (!stateNode && report?.status === 'ready') {
    const data = report.data
    const late = data.OverduePoCount
    const unconfirmed = data.DeliveryDateUnconfirmedPoCount
    const open = data.OpenPoCount
    const toLate = () => onOpen('openOrders', { overdue: true })

    // Late POs split by days late, only when every row is on this page (the
    // overview is unfiltered and the server returned all rows); otherwise one
    // "Late" slice with the server's own count.
    const lateByPo = new Map<string, number>()
    for (const row of data.Rows) {
      if (row.DaysLate !== null && row.DaysLate > 0) lateByPo.set(row.RootPurchaseOrderId, Math.max(lateByPo.get(row.RootPurchaseOrderId) ?? 0, row.DaysLate))
    }
    const complete = report.unfiltered && data.TotalRows === data.Rows.length && late !== null && lateByPo.size === late
    const days = [...lateByPo.values()]
    const slices: Slice[] = [
      { key: 'on-time', label: 'On time', value: open !== null && late !== null ? Math.max(0, open - late - unconfirmed) : 0, color: '#10b981', onClick: () => onOpen('openOrders') },
      ...(complete
        ? [
            { key: 'late-7', label: '1–7 days', value: days.filter((d) => d <= 7).length, color: '#3b82f6', onClick: toLate },
            { key: 'late-14', label: '8–14 days', value: days.filter((d) => d > 7 && d <= 14).length, color: '#f59e0b', onClick: toLate },
            { key: 'late-more', label: '> 14 days', value: days.filter((d) => d > 14).length, color: '#ec4899', onClick: toLate },
          ]
        : [{ key: 'late', label: 'Late', value: late ?? 0, color: '#f43f5e', onClick: toLate }]),
      ...(unconfirmed > 0 ? [{ key: 'unconfirmed', label: 'Date unconfirmed', value: unconfirmed, color: '#94a3b8', onClick: () => onOpen('openOrders') }] : []),
    ]
    const unknown = [open === null && 'open count', late === null && 'late count'].filter(Boolean).join(' and ')
    body = (
      <>
        <div className="flex flex-wrap items-center justify-center gap-x-5 gap-y-3">
          <Donut slices={slices} center={open === null ? '—' : String(open)} caption="Open POs" />
          <ul className="min-w-[8.5rem] flex-1 space-y-2.5">
            {slices.map((s) => (
              <li key={s.key}>
                <button type="button" onClick={s.onClick} className="flex w-full items-center gap-2 text-left text-sm text-slate-700 hover:text-slate-900">
                  <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ background: s.color }} aria-hidden />
                  <span className="truncate">{s.label}</span>
                  <span className="ml-auto font-semibold tabular-nums text-slate-900">{s.value}</span>
                </button>
              </li>
            ))}
          </ul>
        </div>
        <div className="mt-3 flex flex-wrap gap-x-3 text-[11px] text-slate-400">
          {data.OldestAgeDays !== null && <span>Oldest open PO {data.OldestAgeDays} d</span>}
          {late !== null && late > 0 && !complete && <span>Late split by days on the detail list</span>}
          {unknown && <span className="text-amber-700">{unknown} unknown (not zero)</span>}
        </div>
      </>
    )
  }
  return (
    <Panel icon={Truck} accent="blue" title="Open orders by delivery" className="h-full" titleWraps
      action={allowed ? { label: 'View all', onClick: () => onOpen('openOrders') } : undefined}
      info="Counts are POs. Late = past the confirmed delivery date. Without a confirmed date a PO cannot be tracked as late.">
      {body}
    </Panel>
  )
}

interface Slice { key: string; label: string; value: number; color: string; onClick: () => void }

/** Ring chart of PO counts; a solid ring when there is nothing to split. */
function Donut({ slices, center, caption }: { slices: Slice[]; center: string; caption: string }) {
  const r = 42
  const c = 2 * Math.PI * r
  const total = slices.reduce((sum, s) => sum + s.value, 0)
  let offset = 0
  return (
    <div className="relative h-[124px] w-[124px] shrink-0">
      <svg viewBox="0 0 100 100" className="h-full w-full -rotate-90" aria-hidden>
        <circle cx="50" cy="50" r={r} fill="none" stroke={total === 0 ? '#2563eb' : '#f1f5f9'} strokeWidth="11" />
        {total > 0 && slices.filter((s) => s.value > 0).map((s) => {
          const len = (s.value / total) * c
          const dash = `${len} ${c - len}`
          const el = <circle key={s.key} cx="50" cy="50" r={r} fill="none" stroke={s.color} strokeWidth="11" strokeDasharray={dash} strokeDashoffset={-offset} />
          offset += len
          return el
        })}
      </svg>
      <div className="absolute inset-0 grid place-items-center text-center">
        <div>
          <div className="text-2xl font-bold tabular-nums text-slate-900">{center}</div>
          <div className="text-xs text-slate-500">{caption}</div>
        </div>
      </div>
    </div>
  )
}

// ---------- owed to vendors (received, not billed) ----------

export function OwedPanel({ report, allowed, onOpen }: { report: PurchaseReports['obligations']; allowed: boolean; onOpen: OpenSection }) {
  const stateNode = panelState({ allowed, report, what: 'obligations', section: 'obligations', onOpen })
  let body: ReactNode = stateNode
  if (!stateNode && report?.status === 'ready') {
    const vendors = report.data.Vendors.filter((vendor) => vendor.Queue === 'grni')
    const currencies = inrFirst([...new Set(vendors.map((v) => v.Currency))].map((Currency) => ({ Currency }))).map((c) => c.Currency)
    body = currencies.length === 0 ? (
      <div className="flex flex-col items-center justify-center gap-3 py-6 text-center">
        <div className="relative h-16 w-20" aria-hidden>
          <span className="absolute left-0 top-0 grid h-14 w-14 place-items-center rounded-xl bg-blue-50 text-blue-300"><Receipt size={28} /></span>
          <span className="absolute bottom-0 right-0 grid h-9 w-9 place-items-center rounded-lg bg-white text-blue-500 shadow-md"><Database size={18} /></span>
        </div>
        <div className="text-base font-medium text-slate-700">Nothing waiting for a bill</div>
      </div>
    ) : (
      <div className="space-y-2">
        {currencies.map((currency) => (
          <div key={currency}>
            {currencies.length > 1 && <div className="font-mono text-[10px] text-slate-400">{currency}</div>}
            <MiniBars
              format={(value) => formatCompactAmount(value, currency)}
              rows={vendors.filter((v) => v.Currency === currency).sort((a, b) => b.Value - a.Value).slice(0, 5).map((vendor) => ({
                key: `${vendor.VendorId}-${currency}`,
                label: vendor.VendorName || vendor.VendorCode,
                value: vendor.Value,
                tone: ageTone(vendor.OldestAgeDays, OBLIGATION_AGE),
                onClick: () => onOpen('obligations', { queue: 'grni', vendor: { id: vendor.VendorId, label: vendor.VendorCode }, currency }),
              }))}
            />
          </div>
        ))}
      </div>
    )
  }
  return (
    <Panel icon={PackageCheck} accent="blue" title="Owed to vendors" className="flex-1"
      action={allowed ? { label: 'View all', onClick: () => onOpen('obligations', { queue: 'grni' }) } : undefined}
      info={`Top 5 vendors by goods received but not billed, at PO rate before GST. Colour = oldest receipt: amber from ${OBLIGATION_AGE.warn} d, red from ${OBLIGATION_AGE.bad} d. Advances paid are not included.`}>
      {body}
    </Panel>
  )
}
