// Row 1: six headline cards. Every number is one the server returned;
// currencies are listed, never added. A card opens the detail panel behind it.

import { AlertTriangle, ClipboardList, IndianRupee, PackageCheck, ShoppingCart, Wallet } from 'lucide-react'
import { formatCount } from '../../utils/dashboardFormat'
import { NullValue } from './DashboardParts'
import { StatCard, StatGrid, type Tone } from './DashboardUi'
import {
  GST_TIP, OBLIGATION_AGE, WORKLOAD_AGE, ageTone, compactAmounts, formatCompactAmount, inrFirst,
  type OpenSection, type PurchaseReports, type SectionReport,
} from './PurchaseDashboardKit'
import { OBLIGATION_TIP } from './PurchaseObligationsSection'

type Show = { workload: boolean; openOrders: boolean; obligations: boolean; spending: boolean }

/** locked / loading / problem, or undefined when the report is ready. */
function base<T>(allowed: boolean, report: SectionReport<T> | undefined) {
  if (!allowed) return { locked: true } as const
  if (!report || report.status === 'loading') return { loading: true } as const
  if (report.status === 'problem') return { problem: true } as const
  return null
}

const PROBLEM = { value: '—', sub: "Couldn't load", tone: 'warn' as Tone }

export function PurchaseStatCards({ reports, show, onOpen }: { reports: PurchaseReports; show: Show; onOpen: OpenSection }) {
  return (
    <StatGrid>
      <QueuesCard report={reports.workload} allowed={show.workload} onOpen={onOpen} />
      <OpenPoCard report={reports.openOrders} allowed={show.openOrders} onOpen={onOpen} />
      <LateCard report={reports.openOrders} allowed={show.openOrders} onOpen={onOpen} />
      <ObligationCard kind="grni" report={reports.obligations} allowed={show.obligations} onOpen={onOpen} />
      <ObligationCard kind="vendor-advances" report={reports.obligations} allowed={show.obligations} onOpen={onOpen} />
      <SpendCard report={reports.spending} allowed={show.spending} onOpen={onOpen} />
    </StatGrid>
  )
}

function QueuesCard({ report, allowed, onOpen }: { report: PurchaseReports['workload']; allowed: boolean; onOpen: OpenSection }) {
  const common = { icon: ClipboardList, label: 'Queues with work', onClick: () => onOpen('workload') }
  const state = base(allowed, report)
  if (state && 'problem' in state) return <StatCard {...common} {...PROBLEM} />
  if (state || report?.status !== 'ready') return <StatCard {...common} {...state} value="" />
  const ready = report.data.Tiles.filter((tile) => tile.State === 'READY')
  const busy = ready.filter((tile) => tile.Count)
  const ages = busy.map((tile) => tile.OldestAgeDays).filter((age): age is number => age !== null)
  const oldest = ages.length ? Math.max(...ages) : null
  const hidden = report.data.Tiles.length - ready.length
  return (
    <StatCard {...common}
      tone={busy.length ? ageTone(oldest, WORKLOAD_AGE) : 'ok'}
      value={<>{busy.length}<span className="text-sm font-normal text-slate-400"> of {ready.length}</span></>}
      sub={oldest === null ? 'Nothing waiting' : `Oldest waiting ${oldest} d`}
      info={`Purchase queues that have at least one document waiting. Colour = oldest wait: amber from ${WORKLOAD_AGE.warn} d, red from ${WORKLOAD_AGE.bad} d.${hidden ? ` ${hidden} queue(s) hidden from your role.` : ''}`} />
  )
}

function OpenPoCard({ report, allowed, onOpen }: { report: PurchaseReports['openOrders']; allowed: boolean; onOpen: OpenSection }) {
  const common = { icon: ShoppingCart, label: 'Open POs', onClick: () => onOpen('openOrders') }
  const state = base(allowed, report)
  if (state && 'problem' in state) return <StatCard {...common} {...PROBLEM} />
  if (state || report?.status !== 'ready') return <StatCard {...common} {...state} value="" />
  const data = report.data
  return (
    <StatCard {...common}
      tone={data.Complete ? 'muted' : 'warn'}
      value={data.OpenPoCount === null ? <NullValue reason="unknown" /> : formatCount(data.OpenPoCount)}
      sub={data.Amounts === null ? 'Value unknown' : data.Amounts.length ? `${compactAmounts(data.Amounts.map((a) => ({ Currency: a.Currency, value: a.Value })))} incl. GST` : 'No value outstanding'}
      info={`Issued POs still waiting for material; value = payable still to receive, incl. GST and charges.${data.Complete ? '' : ' Some POs do not reconcile, so the totals are unknown (not zero).'}`} />
  )
}

function LateCard({ report, allowed, onOpen }: { report: PurchaseReports['openOrders']; allowed: boolean; onOpen: OpenSection }) {
  const common = { icon: AlertTriangle, label: 'Late deliveries', onClick: () => onOpen('openOrders', { overdue: true }) }
  const state = base(allowed, report)
  if (state && 'problem' in state) return <StatCard {...common} {...PROBLEM} />
  if (state || report?.status !== 'ready') return <StatCard {...common} {...state} value="" />
  const data = report.data
  const late = data.OverduePoCount
  const unconfirmed = data.DeliveryDateUnconfirmedPoCount
  const lateAmounts = (data.Amounts ?? []).filter((a) => a.OverdueValue).map((a) => ({ Currency: a.Currency, value: a.OverdueValue as number }))
  const tone: Tone = late === null ? 'warn' : late > 0 ? 'bad' : unconfirmed > 0 ? 'warn' : 'ok'
  const sub = late === null
    ? `${unconfirmed} date${unconfirmed === 1 ? '' : 's'} unconfirmed`
    : late > 0
      ? lateAmounts.length ? `${compactAmounts(lateAmounts)} to receive` : 'Past confirmed date'
      : unconfirmed > 0 ? `${unconfirmed} date${unconfirmed === 1 ? '' : 's'} unconfirmed` : 'All on schedule'
  return (
    <StatCard {...common} tone={tone}
      value={late === null ? <NullValue reason="unknown" /> : formatCount(late)}
      sub={sub}
      info="POs past their confirmed delivery date. POs without a confirmed date cannot be late yet; confirm them with the vendor." />
  )
}

function ObligationCard({ kind, report, allowed, onOpen }: { kind: 'grni' | 'vendor-advances'; report: PurchaseReports['obligations']; allowed: boolean; onOpen: OpenSection }) {
  const common = {
    icon: kind === 'grni' ? PackageCheck : Wallet,
    label: kind === 'grni' ? 'Received, not billed' : 'Advances not adjusted',
    onClick: () => onOpen('obligations', { queue: kind }),
  }
  const state = base(allowed, report)
  if (state && 'problem' in state) return <StatCard {...common} {...PROBLEM} />
  if (state || report?.status !== 'ready') return <StatCard {...common} {...state} value="" />
  const tile = report.data.Tiles.find((t) => t.Key === kind)
  if (!tile) return <StatCard {...common} value={<NullValue reason="none" />} sub="Not reported" />
  const money = tile.Amounts.length ? compactAmounts(tile.Amounts.map((a) => ({ Currency: a.Currency, value: a.Value }))) : null
  return (
    <StatCard {...common}
      tone={tile.Count ? ageTone(tile.OldestAgeDays, OBLIGATION_AGE) : 'ok'}
      value={formatCount(tile.Count)}
      sub={tile.Count ? `${money ?? ''}${kind === 'grni' ? ' ex-GST' : ' paid'}${tile.OldestAgeDays !== null ? ` · ${tile.OldestAgeDays} d` : ''}` : 'None outstanding'}
      info={`${OBLIGATION_TIP[kind]} Colour = oldest: amber from ${OBLIGATION_AGE.warn} d, red from ${OBLIGATION_AGE.bad} d.`} />
  )
}

function SpendCard({ report, allowed, onOpen }: { report: PurchaseReports['spending']; allowed: boolean; onOpen: OpenSection }) {
  const common = { icon: IndianRupee, label: 'Spend this FY', onClick: () => onOpen('spending') }
  const state = base(allowed, report)
  if (state && 'problem' in state) return <StatCard {...common} {...PROBLEM} />
  if (state || report?.status !== 'ready') return <StatCard {...common} {...state} value="" />
  const fy = report.data.Periods.find((period) => period.Key === 'financial-year')
  const amounts = fy ? inrFirst(fy.Amounts) : null
  const [first, ...rest] = amounts ?? []
  return (
    <StatCard {...common}
      value={amounts === null ? <NullValue reason="unknown" /> : first ? formatCompactAmount(first.Amount, first.Currency) : <NullValue reason="none" />}
      sub={<>{rest.length > 0 && `+ ${rest.map((a) => formatCompactAmount(a.Amount, a.Currency)).join(' · ')} · `}<span className="text-amber-700">incl. GST</span></>}
      info={GST_TIP} />
  )
}
