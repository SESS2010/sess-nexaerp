// Headline figures, one tile per thing a manager asks first. Every number is
// one the server returned; currencies stay on separate lines. A tile opens
// the section that explains it, narrowed where the contract allows.

import type { ReactNode } from 'react'
import type { PurchaseSpendingAmount } from '../../types/dashboard'
import { countOf, formatAge, formatCount, formatDateOnly, formatMonth } from '../../utils/dashboardFormat'
import { AmountList, Money, NullValue } from './DashboardParts'
import type { PurchaseReports, PurchaseSectionId } from './PurchaseAttentionPanel'
import { OBLIGATION_AGE, SEVERITY, Skeleton, WORKLOAD_AGE, ageSeverity, type Severity } from './PurchaseDashboardKit'
import { SPENDING_GST_NOTE, SPENDING_PERIOD_LABEL } from './PurchaseSpendingSection'

interface Tile {
  key: string
  label: string
  severity: Severity | 'neutral'
  value: ReactNode
  sub?: ReactNode
  section: PurchaseSectionId
  target: string
  hint: string
}

export function PurchaseKpiStrip({ reports, onOpen }: {
  reports: PurchaseReports
  onOpen: (section: PurchaseSectionId, target: string) => void
}) {
  const tiles: (Tile | { key: string; loading: true } | { key: string; problem: string })[] = []

  const workload = reports.workload
  if (workload?.status === 'loading') tiles.push({ key: 'workload', loading: true })
  if (workload?.status === 'problem') tiles.push({ key: 'workload', problem: 'Purchase queues' })
  if (workload?.status === 'ready') {
    const ready = workload.data.Tiles.filter((tile) => tile.State === 'READY')
    const busy = ready.filter((tile) => tile.Count)
    const ages = busy.map((tile) => tile.OldestAgeDays).filter((age): age is number => age !== null)
    const longest = ages.length ? Math.max(...ages) : null
    tiles.push({
      key: 'workload',
      label: 'Purchase queues with work',
      severity: busy.length ? ageSeverity(longest, WORKLOAD_AGE) : 'green',
      value: <>{formatCount(busy.length)} <span className="text-[13px] font-normal text-ink-soft">of {ready.length}</span></>,
      sub: longest === null ? 'Nothing waiting' : `Longest wait ${formatAge(longest)}`,
      section: 'workload',
      target: '',
      hint: 'Open the purchase queues',
    })
  }

  const openOrders = reports.openOrders
  if (openOrders?.status === 'loading') tiles.push({ key: 'open', loading: true }, { key: 'overdue', loading: true })
  if (openOrders?.status === 'problem') tiles.push({ key: 'open', problem: 'Open purchase orders' })
  if (openOrders?.status === 'ready') {
    const data = openOrders.data
    tiles.push({
      key: 'open',
      label: 'Open POs · value outstanding',
      severity: 'neutral',
      value: data.OpenPoCount === null ? <NullValue reason="unknown" /> : formatCount(data.OpenPoCount),
      sub: (
        <>
          <AmountList amounts={data.Amounts === null ? null : data.Amounts.map((amount) => ({ Currency: amount.Currency, value: amount.Value }))} />
          <span className="block text-[11px] text-ink-faint">payable incl. GST</span>
        </>
      ),
      section: 'openOrders',
      target: '',
      hint: 'Open the open purchase orders',
    })
    const overdueValues = (data.Amounts ?? []).filter((amount) => amount.OverdueValue)
    tiles.push({
      key: 'overdue',
      label: 'Late deliveries',
      severity: data.OverduePoCount === null ? 'amber' : data.OverduePoCount > 0 ? 'red' : data.DeliveryDateUnconfirmedPoCount > 0 ? 'amber' : 'green',
      value: data.OverduePoCount === null ? <NullValue reason="unknown" /> : <>{formatCount(data.OverduePoCount)} <span className="text-[13px] font-normal text-ink-soft">{data.OverduePoCount === 1 ? 'PO' : 'POs'}</span></>,
      sub: (
        <>
          {overdueValues.map((amount) => <span key={amount.Currency} className="block"><Money value={amount.OverdueValue} currency={amount.Currency} /> late</span>)}
          {data.DeliveryDateUnconfirmedPoCount > 0 && <span className="block text-amber-700">{countOf(data.DeliveryDateUnconfirmedPoCount, 'PO')} without a confirmed date</span>}
          {data.OverduePoCount === 0 && data.DeliveryDateUnconfirmedPoCount === 0 && 'All on or within commitment'}
        </>
      ),
      section: 'openOrders',
      target: data.OverduePoCount ? 'overdue' : '',
      hint: data.OverduePoCount ? 'List the overdue lines' : 'Open the open purchase orders',
    })
  }

  const obligations = reports.obligations
  if (obligations?.status === 'loading') tiles.push({ key: 'grni', loading: true })
  if (obligations?.status === 'problem') tiles.push({ key: 'grni', problem: 'Obligations' })
  if (obligations?.status === 'ready') {
    for (const tile of obligations.data.Tiles) {
      tiles.push({
        key: `obligation-${tile.Key}`,
        label: tile.Key === 'grni' ? 'Received, not billed' : tile.Key === 'vendor-advances' ? 'Advances not adjusted' : tile.Title,
        severity: tile.Count ? ageSeverity(tile.OldestAgeDays, OBLIGATION_AGE) : 'green',
        value: <>{formatCount(tile.Count)} <span className="text-[13px] font-normal text-ink-soft">{tile.Count === 1 ? 'document' : 'documents'}</span></>,
        sub: (
          <>
            <AmountList amounts={tile.Amounts.map((amount) => ({ Currency: amount.Currency, value: amount.Value }))} />
            <span className="block text-[11px] text-ink-faint">
              {tile.Key === 'grni' ? 'before GST' : tile.Key === 'vendor-advances' ? 'cash paid' : ''}
              {tile.OldestAgeDays !== null && ` · oldest ${formatAge(tile.OldestAgeDays)}`}
            </span>
          </>
        ),
        section: 'obligations',
        target: tile.Key,
        hint: 'List these obligations',
      })
    }
  }

  const spending = reports.spending
  if (spending?.status === 'loading') tiles.push({ key: 'spend', loading: true })
  if (spending?.status === 'problem') tiles.push({ key: 'spend', problem: 'Spending' })
  if (spending?.status === 'ready') {
    const data = spending.data
    const period = data.Filters.Period
    let label = `Spend · ${SPENDING_PERIOD_LABEL[period]?.toLowerCase() ?? period}`
    let amounts: PurchaseSpendingAmount[] | null = null
    if (period === 'month') {
      const month = data.MonthlyTrend.find((bucket) => bucket.FromDate === data.FromDate)
        ?? data.Periods.find((bucket) => bucket.Key === 'month' && bucket.FromDate === data.FromDate)
      amounts = month?.Amounts ?? null
      label = `Spend · ${formatMonth(data.FromDate)}`
    } else {
      amounts = data.Periods.find((bucket) => bucket.Key === period)?.Amounts ?? null
    }
    tiles.push({
      key: 'spend',
      label,
      severity: 'neutral',
      value: amounts === null
        ? <span className="text-[13px] font-normal text-ink-soft">Not given as one figure</span>
        : amounts.length === 0
          ? <NullValue reason="none" />
          : <span className="flex flex-col">{amounts.map((amount) => <Money key={amount.Currency} value={amount.Amount} currency={amount.Currency} />)}</span>,
      sub: (
        <>
          <span className="block font-semibold text-amber-700">{SPENDING_GST_NOTE} — not ex-tax cost</span>
          <span className="block">{amounts === null ? 'See the monthly chart for the last twelve months' : `${formatDateOnly(data.FromDate)} – ${formatDateOnly(data.ToDate)}`}</span>
        </>
      ),
      section: 'spending',
      target: '',
      hint: 'Open spending',
    })
  }

  if (tiles.length === 0) return null

  return (
    <div className="grid gap-3 mb-4" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(190px, 1fr))' }}>
      {tiles.map((tile) => {
        if ('loading' in tile) {
          return (
            <div key={tile.key} className="rounded-xl border border-line bg-white p-4" aria-hidden="true">
              <Skeleton className="h-3 w-2/3 mb-3" />
              <Skeleton className="h-7 w-1/2 mb-2" />
              <Skeleton className="h-3 w-3/4" />
            </div>
          )
        }
        if ('problem' in tile) {
          return (
            <div key={tile.key} className="rounded-xl border border-dashed border-line bg-white p-4">
              <div className="text-[12px] font-semibold uppercase tracking-[0.06em] text-ink-faint">{tile.problem}</div>
              <div className="mt-1 text-[13px] text-ink-soft">Could not load — see the section below.</div>
            </div>
          )
        }
        const color = tile.severity === 'neutral' ? '#94a3b8' : SEVERITY[tile.severity].bar
        return (
          <button key={tile.key} type="button" onClick={() => onOpen(tile.section, tile.target)} title={tile.hint}
            className="cursor-pointer rounded-xl border border-line bg-white p-4 text-left shadow-xs transition-colors hover:border-slate-400 hover:bg-slate-50"
            style={{ borderTop: `4px solid ${color}` }} data-kpi-severity={tile.severity}>
            <div className="text-[11.5px] font-semibold uppercase tracking-[0.06em] text-ink-soft">{tile.label}</div>
            <div className={`mt-1 text-[22px] font-semibold leading-tight tabular-nums ${tile.severity === 'red' ? 'text-red-700' : 'text-ink'}`}>{tile.value}</div>
            {tile.sub && <div className="mt-1 text-[12px] text-ink-soft">{tile.sub}</div>}
          </button>
        )
      })}
    </div>
  )
}
