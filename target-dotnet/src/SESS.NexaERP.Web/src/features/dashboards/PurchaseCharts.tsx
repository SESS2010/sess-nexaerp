// Overview panels: spend trend (with the period selector), purchase queues,
// open orders by delivery, and what is owed to vendors. Each chart holds ONE
// currency; currencies are never added or drawn on a shared axis.

import { useState, type ReactNode } from 'react'
import { ClipboardList, Lock, PackageCheck, TrendingUp, Truck } from 'lucide-react'
import type { PurchaseSpendingPeriodKey } from '../../types/dashboard'
import { formatAmount, formatDateOnly, formatMonth } from '../../utils/dashboardFormat'
import { MiniBars, NoAccess, Panel, type Tone } from './DashboardUi'
import {
  GST_TIP, OBLIGATION_AGE, OneLine, PERIOD_LONG, PERIOD_SHORT, Segmented, SkeletonRows, WORKLOAD_AGE, ageTone, compactAmounts,
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

const PERIOD_OPTIONS = (Object.keys(PERIOD_SHORT) as PurchaseSpendingPeriodKey[]).map((key) => ({
  value: key, label: PERIOD_SHORT[key], title: PERIOD_LONG[key],
}))

export function SpendTrendPanel({ report, allowed, period, onPeriod, onOpen }: {
  report: PurchaseReports['spending']
  allowed: boolean
  period: SpendPeriod
  onPeriod: (next: SpendPeriod) => void
  onOpen: OpenSection
}) {
  const [pickedCurrency, setPickedCurrency] = useState<string | null>(null)
  const stateNode = panelState({ allowed, report, what: 'spending', section: 'spending', onOpen })
  const right = allowed
    ? <Segmented label="Period" value={period.period} options={PERIOD_OPTIONS} onChange={(key) => onPeriod({ period: key, month: null })} />
    : undefined

  let body: ReactNode = stateNode
  if (!stateNode && report?.status === 'ready') {
    const data = report.data
    const currencies = inrFirst(
      [...new Set(data.MonthlyTrend.flatMap((month) => month.Amounts.map((a) => a.Currency)))].map((Currency) => ({ Currency })),
    ).map((c) => c.Currency)
    const currency = pickedCurrency && currencies.includes(pickedCurrency) ? pickedCurrency : currencies[0] ?? null
    const selectedMonth = data.Filters.Period === 'month' ? data.FromDate : null

    // The figure for the chosen period, only when the server gave it as one figure.
    const periodAmounts = data.Filters.Period === 'month'
      ? data.MonthlyTrend.find((m) => m.FromDate === data.FromDate)?.Amounts ?? null
      : data.Periods.find((p) => p.Key === data.Filters.Period)?.Amounts ?? null
    const label = data.Filters.Period === 'month' ? formatMonth(data.FromDate) : PERIOD_LONG[data.Filters.Period] ?? data.Filters.Period

    body = (
      <>
        {currency === null ? (
          <div className="grid h-[120px] place-items-center text-xs text-slate-400">No bills in the last 12 months</div>
        ) : (
          <ColumnChart
            currency={currency}
            months={data.MonthlyTrend.map((month) => {
              const amount = month.Amounts.find((a) => a.Currency === currency)
              return { key: month.Key, fromDate: month.FromDate, value: amount?.Amount ?? 0, bills: amount?.BillCount ?? 0 }
            })}
            selected={selectedMonth}
            onPick={(fromDate) => onPeriod({ period: 'month', month: fromDate })}
          />
        )}
        <div className="mt-2 flex flex-wrap items-baseline gap-x-2 text-xs text-slate-500">
          <span className="font-medium text-slate-700">{label}</span>
          <span className="tabular-nums text-slate-900">
            {periodAmounts === null ? '—' : periodAmounts.length === 0 ? 'No spend'
              : compactAmounts(periodAmounts.map((a) => ({ Currency: a.Currency, value: a.Amount })))}
          </span>
          <span className="text-amber-700">incl. GST</span>
          <span className="ml-auto text-[11px] text-slate-400">{formatDateOnly(data.FromDate)} – {formatDateOnly(data.ToDate)}</span>
        </div>
        {currencies.length > 1 && (
          <div className="mt-1.5">
            <Segmented label="Currency" value={currency} onChange={setPickedCurrency} options={currencies.map((c) => ({ value: c, label: c }))} />
          </div>
        )}
      </>
    )
  }

  return (
    <Panel icon={TrendingUp} title="Spend trend" info={`${GST_TIP} Click a month to see it; the period also sets Spending details.`} right={right}>
      {body}
    </Panel>
  )
}

const INITIAL = ['J', 'F', 'M', 'A', 'M', 'J', 'J', 'A', 'S', 'O', 'N', 'D']

function ColumnChart({ currency, months, selected, onPick }: {
  currency: string
  months: { key: string; fromDate: string; value: number; bills: number }[]
  selected: string | null
  onPick: (fromDate: string) => void
}) {
  const max = Math.max(0, ...months.map((m) => Math.abs(m.value)))
  return (
    <figure className="m-0">
      <figcaption className="sr-only">Monthly spend in {currency}, incl. GST, oldest month first</figcaption>
      <div className="mb-1 text-[10px] tabular-nums text-slate-400">{max > 0 ? formatCompactAmount(max, currency) : ''}</div>
      <div className="flex h-[108px] items-end gap-1 border-b border-slate-200">
        {months.map((m) => {
          const pct = max === 0 ? 0 : (Math.abs(m.value) / max) * 100
          const isSelected = selected === m.fromDate
          const tip = `${formatMonth(m.key)}: ${m.value === 0 && m.bills === 0 ? 'no bills' : `${formatAmount(m.value, currency)} incl. GST · ${m.bills} bill${m.bills === 1 ? '' : 's'}`}`
          const color = m.value < 0 ? 'bg-rose-400' : isSelected ? 'bg-blue-600' : 'bg-blue-300 group-hover:bg-blue-500'
          return (
            <button key={m.key} type="button" title={tip} aria-label={tip} aria-pressed={isSelected} onClick={() => onPick(m.fromDate)}
              className="group flex h-full flex-1 cursor-pointer flex-col justify-end">
              <span className={`block w-full rounded-t-sm ${color}`} style={{ height: `${pct}%`, minHeight: m.value !== 0 ? 2 : 0 }} />
            </button>
          )
        })}
      </div>
      <div className="mt-0.5 flex gap-1">
        {months.map((m) => {
          const month = Number(m.key.split('-')[1])
          return (
            <span key={m.key} className={`flex-1 text-center text-[10px] ${selected === m.fromDate ? 'font-semibold text-blue-700' : 'text-slate-400'}`}>
              {INITIAL[month - 1] ?? '·'}
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
    body = (
      <>
        <MiniBars
          emptyText="Nothing waiting"
          rows={ready.map((tile) => ({
            key: tile.Key,
            label: QUEUE_SHORT[tile.Key] ?? tile.Title,
            value: tile.Count ?? 0,
            tone: tile.Count ? ageTone(tile.OldestAgeDays, WORKLOAD_AGE) : 'muted',
            onClick: () => onOpen('workload', { queue: tile.Key }),
          }))}
        />
        {hidden > 0 && <div className="mt-2 flex items-center gap-1 text-[11px] text-slate-400"><Lock size={11} aria-hidden /> {hidden} queue{hidden === 1 ? '' : 's'} hidden</div>}
      </>
    )
  }
  return (
    <Panel icon={ClipboardList} title="Purchase queues"
      info={`Documents waiting in each queue. Colour = oldest wait: green fresh, amber from ${WORKLOAD_AGE.warn} d, red from ${WORKLOAD_AGE.bad} d. Click a bar to list it.`}>
      {body}
    </Panel>
  )
}

// ---------- open orders by delivery ----------

export function DeliveryPanel({ report, allowed, onOpen }: { report: PurchaseReports['openOrders']; allowed: boolean; onOpen: OpenSection }) {
  const stateNode = panelState({ allowed, report, what: 'open POs', section: 'openOrders', onOpen })
  let body: ReactNode = stateNode
  if (!stateNode && report?.status === 'ready') {
    const data = report.data
    const rows: { key: string; label: string; value: number; tone: Tone; onClick: () => void }[] = []
    if (data.OpenPoCount !== null) rows.push({ key: 'open', label: 'Open POs', value: data.OpenPoCount, tone: 'info', onClick: () => onOpen('openOrders') })
    if (data.OverduePoCount !== null) rows.push({ key: 'late', label: 'Late', value: data.OverduePoCount, tone: 'bad', onClick: () => onOpen('openOrders', { overdue: true }) })
    rows.push({ key: 'unconfirmed', label: 'Date unconfirmed', value: data.DeliveryDateUnconfirmedPoCount, tone: 'warn', onClick: () => onOpen('openOrders') })
    const unknown = [data.OpenPoCount === null && 'open count', data.OverduePoCount === null && 'late count'].filter(Boolean).join(' and ')
    body = (
      <>
        <MiniBars rows={rows} emptyText="No open POs" />
        <div className="mt-2 flex flex-wrap gap-x-3 text-[11px] text-slate-400">
          {data.OldestAgeDays !== null && <span>Oldest open PO {data.OldestAgeDays} d</span>}
          {unknown && <span className="text-amber-700">{unknown} unknown (not zero)</span>}
        </div>
      </>
    )
  }
  return (
    <Panel icon={Truck} title="Open orders by delivery"
      info="Counts are POs. Late = past the confirmed delivery date. Without a confirmed date a PO cannot be tracked as late.">
      {body}
    </Panel>
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
      <div className="py-2 text-xs text-slate-400">Nothing waiting for a bill</div>
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
    <Panel icon={PackageCheck} title="Owed to vendors"
      info={`Top 5 vendors by goods received but not billed, at PO rate before GST. Colour = oldest receipt: amber from ${OBLIGATION_AGE.warn} d, red from ${OBLIGATION_AGE.bad} d. Advances paid are not included.`}>
      {body}
    </Panel>
  )
}
