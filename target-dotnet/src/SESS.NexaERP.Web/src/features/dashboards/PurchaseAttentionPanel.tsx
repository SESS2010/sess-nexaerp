// "Needs attention now": plain sentences built ONLY from what the four
// dashboard responses returned, each with the next step as a link to the
// existing screen. Nothing is invented: a document is named only when the
// server returned it, and a figure is only ever one the server gave.

import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { getPurchaseOpenOrders } from '../../api/dashboards'
import type {
  DashboardCurrencyAmount, PurchaseObligationsPage, PurchaseOpenOrderRow, PurchaseOpenOrdersPage, PurchaseSpendingPage, PurchaseWorkloadPage,
} from '../../types/dashboard'
import { countOf, formatAge, formatAmount } from '../../utils/dashboardFormat'
import { PAGE_KEYS } from '../auth/SessionContext'
import { goodsReceiptLink, poLink, vendorLink, workloadRowLink } from './dashboardAccess'
import {
  OBLIGATION_AGE, SEVERITY, SEVERITY_ORDER, Skeleton, WORKLOAD_AGE, ageSeverity, thresholdText, usePurchaseSectionQuery,
  type SectionReport, type Severity,
} from './PurchaseDashboardKit'

type Can = (pageKey: string, action?: string) => boolean

export type PurchaseSectionId = 'workload' | 'openOrders' | 'obligations' | 'spending'

export interface PurchaseReports {
  workload?: SectionReport<PurchaseWorkloadPage>
  openOrders?: SectionReport<PurchaseOpenOrdersPage>
  obligations?: SectionReport<PurchaseObligationsPage>
  spending?: SectionReport<PurchaseSpendingPage>
}

export const SECTION_ANCHOR: Record<PurchaseSectionId, string> = {
  workload: 'purchase-workload',
  openOrders: 'purchase-open-orders',
  obligations: 'purchase-obligations',
  spending: 'purchase-spending',
}

const SECTION_NAME: Record<PurchaseSectionId, string> = {
  workload: 'the purchase queues',
  openOrders: 'open purchase orders',
  obligations: 'obligations (goods not billed, advances)',
  spending: 'spending',
}

/** Singular / plural sentence endings per workload queue. */
const QUEUE_PHRASE: Record<string, [string, string]> = {
  'pr-department-verification': ['PR is waiting for department verification', 'PRs are waiting for department verification'],
  'pr-approval': ['PR is waiting for approval', 'PRs are waiting for approval'],
  'pr-stock-check': ['PR is waiting for a stores stock check', 'PRs are waiting for a stores stock check'],
  'rfq-no-quotation': ['RFQ has no vendor quotation yet', 'RFQs have no vendor quotation yet'],
  'quotation-technical-verification': ['quotation is waiting for technical verification', 'quotations are waiting for technical verification'],
  'comparison-decision': ['comparison is waiting for a recommendation or approval', 'comparisons are waiting for a recommendation or approval'],
  'po-approved-unissued': ['PO is approved but not yet issued to the vendor', 'POs are approved but not yet issued to the vendor'],
}

/** The next step, as the button verb, per queue. */
const QUEUE_STEP: Record<string, string> = {
  'pr-department-verification': 'Verify',
  'pr-approval': 'Review',
  'pr-stock-check': 'Stock-check',
  'rfq-no-quotation': 'Follow up',
  'quotation-technical-verification': 'Verify quotations',
  'comparison-decision': 'Decide',
  'po-approved-unissued': 'Issue',
}

/** List screens for a queue when the oldest document was not in the returned rows. */
function queueListLink(can: Can, queue: string): { to: string; label: string } | null {
  switch (queue) {
    case 'pr-department-verification':
    case 'pr-approval':
      return can(PAGE_KEYS.requisitions) ? { to: '/purchase/requisitions', label: 'Open requisitions' } : null
    case 'pr-stock-check':
      return can(PAGE_KEYS.stockCheck, 'verify') ? { to: '/stores/stock-check', label: 'Open stock check' } : null
    case 'rfq-no-quotation':
      return can(PAGE_KEYS.rfq) ? { to: '/purchase/rfqs', label: 'Open RFQs' } : null
    case 'quotation-technical-verification':
      return can(PAGE_KEYS.quotations) ? { to: '/purchase/quotations', label: 'Open quotations' } : null
    case 'comparison-decision':
      return can(PAGE_KEYS.comparisons) ? { to: '/purchase/comparisons', label: 'Open comparisons' } : null
    case 'po-approved-unissued':
      return can(PAGE_KEYS.purchaseOrders) ? { to: '/purchase/purchase-orders', label: 'Open purchase orders' } : null
    default:
      return null
  }
}

function amountsText(amounts: { Currency: string | null; value: number }[]): string | null {
  const shown = amounts.filter((amount): amount is { Currency: string; value: number } => amount.Currency !== null)
  if (shown.length === 0) return null
  // Separate currencies are listed, never added.
  return shown.map((amount) => formatAmount(amount.value, amount.Currency)).join(' and ')
}

interface Item {
  key: string
  severity: Severity
  text: ReactNode
  actions: ReactNode[]
}

function ActionLink({ to, children, primary }: { to: string; children: ReactNode; primary?: boolean }) {
  return <Link to={to} className={`btn ${primary ? 'btn-primary' : 'btn-ghost'} whitespace-nowrap`} style={{ padding: '4px 10px', fontSize: 12.5 }}>{children}</Link>
}

function ActionButton({ onClick, children }: { onClick: () => void; children: ReactNode }) {
  return <button type="button" className="btn btn-ghost whitespace-nowrap" style={{ padding: '4px 10px', fontSize: 12.5 }} onClick={onClick}>{children}</button>
}

export function PurchaseAttentionPanel({ reports, can, refreshTick, onFocus }: {
  reports: PurchaseReports
  can: Can
  refreshTick: number
  /** Narrow a section's detail rows (target is section-specific) and scroll to it. */
  onFocus: (section: PurchaseSectionId, target: string) => void
}) {
  const items: Item[] = []
  const fresh: ReactNode[] = []
  let checking = false
  let overdue: { count: number; amounts: DashboardCurrencyAmount[] } | null = null

  const problem = (section: PurchaseSectionId) => items.push({
    key: `${section}-problem`,
    severity: 'amber',
    text: <>Could not check {SECTION_NAME[section]}. The section below says why.</>,
    actions: [<ActionButton key="see" onClick={() => onFocus(section, '')}>See the section</ActionButton>],
  })

  // ----- workload -----
  const workload = reports.workload
  if (workload?.status === 'loading') checking = true
  if (workload?.status === 'problem') problem('workload')
  if (workload?.status === 'ready') {
    const data = workload.data
    for (const tile of data.Tiles) {
      if (tile.State !== 'READY' || !tile.Count) continue
      const phrase = QUEUE_PHRASE[tile.Key]
      const what = phrase ? `${tile.Count} ${tile.Count === 1 ? phrase[0] : phrase[1]}` : `${tile.Count} in ${tile.Title}`
      const oldest = workload.unfiltered && tile.OldestAgeDays !== null
        ? data.Rows.find((row) => row.Queue === tile.Key && row.AgeDays === tile.OldestAgeDays) ?? null
        : null
      const severity = ageSeverity(tile.OldestAgeDays, WORKLOAD_AGE)
      const docLink = oldest ? workloadRowLink(can, oldest.Queue, oldest.DocumentNumber) : null
      const list = queueListLink(can, tile.Key)
      const actions: ReactNode[] = []
      if (docLink && oldest) actions.push(<ActionLink key="doc" to={docLink} primary>{QUEUE_STEP[tile.Key] ?? 'Open'} {oldest.DocumentNumber}</ActionLink>)
      else if (list) actions.push(<ActionLink key="list" to={list.to} primary>{list.label}</ActionLink>)
      actions.push(<ActionButton key="rows" onClick={() => onFocus('workload', tile.Key)}>List them</ActionButton>)

      if (severity === 'green') {
        fresh.push(<span key={tile.Key}>{what}</span>)
        continue
      }
      const who = oldest?.ResponsibilityIssue
        ? <> The next responsible person is unclear ({oldest.ResponsibilityIssue}).</>
        : oldest?.NextApproverEmployeeCode || oldest?.NextApproverRole
          ? <> Next: <span className="mono">{oldest.NextApproverEmployeeCode ?? ''}</span>{oldest.NextApproverRole ? ` ${oldest.NextApproverRole}` : ''}.</>
          : null
      items.push({
        key: `workload-${tile.Key}`,
        severity,
        text: (
          <>
            <strong>{what}</strong>
            {tile.OldestAgeDays !== null && <> — oldest {oldest ? <span className="mono">{oldest.DocumentNumber}</span> : 'one'}, waiting {formatAge(tile.OldestAgeDays)}.</>}
            {who}
          </>
        ),
        actions,
      })
    }

    if (workload.unfiltered) {
      const unclear = data.Rows.filter((row) => row.ResponsibilityIssue)
      if (unclear.length > 0) {
        const first = unclear[0]
        const link = workloadRowLink(can, first.Queue, first.DocumentNumber)
        items.push({
          key: 'workload-responsibility',
          severity: 'amber',
          text: (
            <>
              <strong>{countOf(unclear.length, 'waiting document')} {unclear.length === 1 ? 'has' : 'have'} no single responsible person</strong>
              {' '}(for example <span className="mono">{first.DocumentNumber}</span>: {first.ResponsibilityIssue}). Nobody may be acting on {unclear.length === 1 ? 'it' : 'them'}; ask the administrator to correct the workflow step.
            </>
          ),
          actions: link ? [<ActionLink key="doc" to={link}>Open {first.DocumentNumber}</ActionLink>] : [],
        })
      }
    }
  }

  // ----- open orders -----
  const openOrders = reports.openOrders
  if (openOrders?.status === 'loading') checking = true
  if (openOrders?.status === 'problem') problem('openOrders')
  if (openOrders?.status === 'ready') {
    const data = openOrders.data
    if (!data.Complete && data.SourceIssues.length > 0) {
      const first = data.SourceIssues[0]
      const link = poLink(can, first.PoNumber)
      items.push({
        key: 'open-orders-source',
        severity: 'red',
        text: (
          <>
            <strong>{countOf(data.SourceIssues.length, 'purchase order')} could not be reconciled</strong>, so the open-PO totals are unknown
            {' '}(first: <span className="mono">{first.PoNumber}</span>). Ask the administrator to reconcile {data.SourceIssues.length === 1 ? 'it' : 'them'}.
          </>
        ),
        actions: link ? [<ActionLink key="po" to={link}>Open {first.PoNumber}</ActionLink>] : [],
      })
    }
    if (data.OverduePoCount) {
      overdue = {
        count: data.OverduePoCount,
        amounts: (data.Amounts ?? []).filter((amount) => amount.OverdueValue !== null && amount.OverdueValue !== 0)
          .map((amount) => ({ Currency: amount.Currency, Amount: amount.OverdueValue as number })),
      }
    }
    if (data.DeliveryDateUnconfirmedPoCount > 0) {
      const example = openOrders.unfiltered ? data.Rows.find((row) => row.DeliveryState === 'CONFIRMATION_REQUIRED') ?? null : null
      const link = example ? poLink(can, example.PoNumber) : null
      const count = data.DeliveryDateUnconfirmedPoCount
      items.push({
        key: 'open-orders-unconfirmed',
        severity: 'amber',
        text: (
          <>
            <strong>{countOf(count, 'open PO')} {count === 1 ? 'has' : 'have'} no confirmed delivery date</strong>
            {example && <> (for example <span className="mono">{example.PoNumber}</span> from {example.VendorName})</>}.
            {' '}Confirm the date with the vendor; until then lateness cannot be tracked.
          </>
        ),
        actions: [
          ...(link && example ? [<ActionLink key="po" to={link} primary>Open {example.PoNumber}</ActionLink>] : []),
          ...(example && vendorLink(can, example.VendorCode) ? [<ActionLink key="vendor" to={vendorLink(can, example.VendorCode) as string}>Vendor {example.VendorCode}</ActionLink>] : []),
        ],
      })
    }
  }

  // ----- obligations -----
  const obligations = reports.obligations
  if (obligations?.status === 'loading') checking = true
  if (obligations?.status === 'problem') problem('obligations')
  if (obligations?.status === 'ready') {
    const data = obligations.data
    for (const tile of data.Tiles) {
      if (!tile.Count) continue
      const severity = ageSeverity(tile.OldestAgeDays, OBLIGATION_AGE)
      const money = amountsText(tile.Amounts.map((amount) => ({ Currency: amount.Currency, value: amount.Value })))
      const oldest = obligations.unfiltered && tile.OldestAgeDays !== null
        ? data.Rows.find((row) => row.Queue === tile.Key && row.AgeDays === tile.OldestAgeDays) ?? null
        : null
      if (tile.Key === 'grni') {
        const what = `${countOf(tile.Count, 'goods receipt')} ${tile.Count === 1 ? 'is' : 'are'} not yet billed`
        if (severity === 'green') { fresh.push(<span key="grni">{what}</span>); continue }
        const grnTo = oldest ? goodsReceiptLink(can, oldest.DocumentId) : null
        items.push({
          key: 'obligations-grni',
          severity,
          text: (
            <>
              <strong>{what}</strong>{money && <> ({money}, before GST)</>}
              {tile.OldestAgeDays !== null && <> — oldest {oldest ? <><span className="mono">{oldest.DocumentNumber}</span> from {oldest.VendorName}</> : 'one'}, received {formatAge(tile.OldestAgeDays)} ago.</>}
              {' '}Get the vendor bill booked.
            </>
          ),
          actions: [
            ...(grnTo && oldest ? [<ActionLink key="grn" to={grnTo} primary>Open {oldest.DocumentNumber}</ActionLink>] : []),
            ...(can(PAGE_KEYS.vendorBills) ? [<ActionLink key="bills" to="/accounts/vendor-bills">Vendor bills</ActionLink>] : []),
            <ActionButton key="rows" onClick={() => onFocus('obligations', 'grni')}>List them</ActionButton>,
          ],
        })
      } else if (tile.Key === 'vendor-advances') {
        const what = `${countOf(tile.Count, 'vendor advance')} ${tile.Count === 1 ? 'is' : 'are'} not yet adjusted against a bill`
        if (severity === 'green') { fresh.push(<span key="advances">{what}</span>); continue }
        items.push({
          key: 'obligations-advances',
          severity,
          text: (
            <>
              <strong>{what}</strong>{money && <> ({money} paid)</>}
              {tile.OldestAgeDays !== null && <> — oldest {oldest ? <><span className="mono">{oldest.DocumentNumber}</span> to {oldest.VendorName}</> : 'one'}, paid {formatAge(tile.OldestAgeDays)} ago.</>}
              {' '}Check the bill against it.
            </>
          ),
          actions: [
            ...(can(PAGE_KEYS.vendorPayments) ? [<ActionLink key="payments" to="/accounts/vendor-payments" primary>Vendor payments</ActionLink>] : []),
            ...(oldest && poLink(can, oldest.PoNumber) ? [<ActionLink key="po" to={poLink(can, oldest.PoNumber) as string}>PO {oldest.PoNumber}</ActionLink>] : []),
            <ActionButton key="rows" onClick={() => onFocus('obligations', 'vendor-advances')}>List them</ActionButton>,
          ],
        })
      } else {
        const what = `${tile.Count} in ${tile.Title}`
        if (severity === 'green') { fresh.push(<span key={tile.Key}>{what}</span>); continue }
        items.push({
          key: `obligations-${tile.Key}`,
          severity,
          text: <><strong>{what}</strong>{money && <> ({money})</>}{tile.OldestAgeDays !== null && <> — oldest {formatAge(tile.OldestAgeDays)}.</>}</>,
          actions: [<ActionButton key="rows" onClick={() => onFocus('obligations', tile.Key)}>List them</ActionButton>],
        })
      }
    }
  }

  // ----- spending: nothing to act on, but a failed load is worth saying -----
  const spending = reports.spending
  if (spending?.status === 'loading') checking = true
  if (spending?.status === 'problem') problem('spending')

  items.sort((a, b) => SEVERITY_ORDER[a.severity] - SEVERITY_ORDER[b.severity])
  const hasOverdue = overdue !== null
  const allClear = !checking && items.length === 0 && !hasOverdue

  return (
    <section className="card mb-4" aria-labelledby="purchase-attention-title" style={{ padding: 20 }} data-dashboard-attention={allClear ? 'clear' : 'items'}>
      <div className="flex flex-wrap items-baseline justify-between gap-2 mb-3">
        <h2 id="purchase-attention-title" className="text-lg font-semibold">Needs attention now</h2>
        <span className="text-[12px] text-ink-faint">
          Built from the figures below. Ages: queues {thresholdText(WORKLOAD_AGE)}; goods not billed and advances {thresholdText(OBLIGATION_AGE)}.
        </span>
      </div>

      {allClear ? (
        <div className={`flex items-center gap-3 rounded-lg border px-4 py-3 ${SEVERITY.green.border} ${SEVERITY.green.soft}`} role="status">
          <span className="grid size-8 shrink-0 place-items-center rounded-full bg-emerald-600 text-white text-[16px]" aria-hidden="true">✓</span>
          <div>
            <div className="font-semibold text-emerald-800">All clear</div>
            <div className="text-[13px] text-emerald-800/80">
              Nothing is overdue or ageing in the sections you can see.
              {fresh.length > 0 && <> Waiting but still fresh: {joinNodes(fresh)}.</>}
            </div>
          </div>
        </div>
      ) : (
        <ul className="m-0 list-none p-0 flex flex-col gap-2">
          {hasOverdue && overdue && (
            <OverdueItem count={overdue.count} amounts={overdue.amounts} can={can} refreshTick={refreshTick}
              onList={() => onFocus('openOrders', 'overdue')} />
          )}
          {items.map((item) => <AttentionRow key={item.key} severity={item.severity} text={item.text} actions={item.actions} />)}
          {checking && (
            <li className="flex items-center gap-3 rounded-lg border border-dashed border-line px-4 py-3" role="status">
              <Skeleton className="h-3 w-3 rounded-full" />
              <span className="text-[13px] text-ink-soft">Checking the remaining sections…</span>
            </li>
          )}
          {fresh.length > 0 && (
            <li className="px-1 pt-1 text-[12.5px] text-ink-soft">
              <span className="badge badge-ok mr-2">On track</span>Also waiting, still fresh: {joinNodes(fresh)}.
            </li>
          )}
        </ul>
      )}
    </section>
  )
}

function joinNodes(nodes: ReactNode[]): ReactNode {
  return nodes.map((node, index) => (
    <span key={index}>{index > 0 && (index === nodes.length - 1 ? ' and ' : ', ')}{node}</span>
  ))
}

function AttentionRow({ severity, text, actions }: { severity: Severity; text: ReactNode; actions: ReactNode[] }) {
  const style = SEVERITY[severity]
  return (
    <li className={`flex flex-wrap items-center gap-x-4 gap-y-2 rounded-lg border px-4 py-2.5 ${style.border} ${style.soft}`}
      style={{ borderLeft: `5px solid ${style.bar}` }} data-attention-severity={severity}>
      <span className={`${style.badge} shrink-0`}>{style.label}</span>
      <div className="min-w-0 flex-1 text-[13.5px] leading-snug" style={{ minWidth: 260 }}>{text}</div>
      {actions.length > 0 && <div className="flex flex-wrap gap-1.5">{actions}</div>}
    </li>
  )
}

/**
 * Overdue deliveries. The count and overdue value come from the open-orders
 * overview; the most-late PO comes from the server's overdueOnly detail rows
 * (a separate, read-only request made only when something is overdue).
 */
function OverdueItem({ count, amounts, can, refreshTick, onList }: {
  count: number
  amounts: DashboardCurrencyAmount[]
  can: Can
  refreshTick: number
  onList: () => void
}) {
  const pageSize = 100
  const { data } = usePurchaseSectionQuery(
    () => getPurchaseOpenOrders({ overdueOnly: true, page: 1, pageSize }),
    'attention-overdue',
    refreshTick,
  )
  let worst: PurchaseOpenOrderRow | null = null
  for (const row of data?.Rows ?? []) {
    if (row.DaysLate !== null && (worst === null || (worst.DaysLate ?? 0) < row.DaysLate)) worst = row
  }
  const partial = data !== null && data.TotalRows > data.Rows.length
  const money = amountsText(amounts.map((amount) => ({ Currency: amount.Currency, value: amount.Amount })))
  const poTo = worst ? poLink(can, worst.PoNumber) : null
  const vendorTo = worst ? vendorLink(can, worst.VendorCode) : null

  return (
    <AttentionRow
      severity="red"
      text={
        <>
          <strong>{countOf(count, 'PO')} {count === 1 ? 'is' : 'are'} past the confirmed delivery date</strong>
          {money && <> ({money} still to receive, incl. GST)</>}
          {worst && worst.DaysLate !== null && (
            <> — most late{partial ? ` of the first ${pageSize} lines` : ''}: <span className="mono">{worst.PoNumber}</span> from {worst.VendorName}, {formatAge(worst.DaysLate)} late.</>
          )}
          {' '}Chase the vendor for a delivery date.
        </>
      }
      actions={[
        ...(poTo && worst ? [<ActionLink key="po" to={poTo} primary>Open {worst.PoNumber}</ActionLink>] : []),
        ...(vendorTo && worst ? [<ActionLink key="vendor" to={vendorTo}>Vendor {worst.VendorCode}</ActionLink>] : []),
        <ActionButton key="rows" onClick={onList}>List overdue lines</ActionButton>,
      ]}
    />
  )
}
