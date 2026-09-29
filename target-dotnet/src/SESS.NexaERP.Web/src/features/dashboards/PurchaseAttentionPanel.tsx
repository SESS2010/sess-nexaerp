// "Needs attention": one short sentence per item, built ONLY from what the
// dashboard responses returned. A document is named only when the server
// returned it; a figure is only ever one the server gave.

import { getPurchaseOpenOrders } from '../../api/dashboards'
import type { PurchaseOpenOrderRow } from '../../types/dashboard'
import { PAGE_KEYS } from '../auth/SessionContext'
import { goodsReceiptLink, poLink, workloadRowLink } from './dashboardAccess'
import { AttentionList, type AttentionItem } from './DashboardUi'
import {
  OBLIGATION_AGE, TONE_ORDER, WORKLOAD_AGE, ageTone, compactAmounts, usePurchaseSectionQuery,
  type Can, type OpenSection, type PurchaseReports, type PurchaseSectionId,
} from './PurchaseDashboardKit'

const MAX_ITEMS = 6

const SECTION_NAME: Record<PurchaseSectionId, string> = {
  workload: 'purchase queues',
  openOrders: 'open POs',
  obligations: 'obligations',
  spending: 'spending',
}

/** Singular / plural phrase per workload queue. */
const QUEUE_PHRASE: Record<string, [string, string]> = {
  'pr-department-verification': ['PR to verify', 'PRs to verify'],
  'pr-approval': ['PR to approve', 'PRs to approve'],
  'pr-stock-check': ['PR waiting for stock check', 'PRs waiting for stock check'],
  'rfq-no-quotation': ['RFQ without a quotation', 'RFQs without a quotation'],
  'quotation-technical-verification': ['quotation to verify', 'quotations to verify'],
  'comparison-decision': ['comparison to decide', 'comparisons to decide'],
  'po-approved-unissued': ['approved PO not issued', 'approved POs not issued'],
}

/** List screen for a queue when its oldest document was not in the returned rows. */
function queueListLink(can: Can, queue: string): string | null {
  switch (queue) {
    case 'pr-department-verification':
    case 'pr-approval':
      return can(PAGE_KEYS.requisitions) ? '/purchase/requisitions' : null
    case 'pr-stock-check':
      return can(PAGE_KEYS.stockCheck, 'verify') ? '/stores/stock-check' : null
    case 'rfq-no-quotation':
      return can(PAGE_KEYS.rfq) ? '/purchase/rfqs' : null
    case 'quotation-technical-verification':
      return can(PAGE_KEYS.quotations) ? '/purchase/quotations' : null
    case 'comparison-decision':
      return can(PAGE_KEYS.comparisons) ? '/purchase/comparisons' : null
    case 'po-approved-unissued':
      return can(PAGE_KEYS.purchaseOrders) ? '/purchase/purchase-orders' : null
    default:
      return null
  }
}

const plural = (n: number, one: string, many = `${one}s`) => `${n} ${n === 1 ? one : many}`

export function PurchaseAttention({ reports, can, refreshTick, onOpen }: {
  reports: PurchaseReports
  can: Can
  refreshTick: number
  onOpen: OpenSection
}) {
  const items: AttentionItem[] = []
  const all = [reports.workload, reports.openOrders, reports.obligations, reports.spending].filter(Boolean)
  const checking = all.some((report) => report?.status === 'loading')
  const allLoading = all.length > 0 && all.every((report) => report?.status === 'loading')

  const problem = (section: PurchaseSectionId) => items.push({
    key: `${section}-problem`,
    tone: 'warn',
    text: <>Couldn't load {SECTION_NAME[section]}.</>,
    action: { label: 'Details', onClick: () => onOpen(section) },
  })

  // ----- open orders -----
  const openOrders = reports.openOrders
  if (openOrders?.status === 'problem') problem('openOrders')
  const overdueCount = openOrders?.status === 'ready' ? openOrders.data.OverduePoCount ?? 0 : 0
  const worst = useWorstOverdue(overdueCount > 0, refreshTick)
  if (openOrders?.status === 'ready') {
    const data = openOrders.data
    if (!data.Complete && data.SourceIssues.length > 0) {
      const first = data.SourceIssues[0]
      const link = poLink(can, first.PoNumber)
      items.push({
        key: 'open-orders-source',
        tone: 'bad',
        text: <><strong>{plural(data.SourceIssues.length, 'PO')} don't reconcile</strong> — open-PO totals unknown ({first.PoNumber}).</>,
        action: link ? { label: 'Open', to: link } : { label: 'Details', onClick: () => onOpen('openOrders') },
      })
    }
    if (overdueCount > 0) {
      const link = worst ? poLink(can, worst.PoNumber) : null
      const others = overdueCount - 1
      items.push({
        key: 'open-orders-overdue',
        tone: 'bad',
        text: worst && worst.DaysLate !== null
          ? <><strong className="font-mono">{worst.PoNumber}</strong> is <strong>{plural(worst.DaysLate, 'day')} late</strong> — {worst.VendorName}{others > 0 && <span className="text-slate-500"> (+{others} more late)</span>}</>
          : <><strong>{plural(overdueCount, 'PO')} past the delivery date</strong></>,
        action: link ? { label: 'Open', to: link } : { label: 'List', onClick: () => onOpen('openOrders', { overdue: true }) },
      })
    }
    if (data.DeliveryDateUnconfirmedPoCount > 0) {
      const n = data.DeliveryDateUnconfirmedPoCount
      const example = openOrders.unfiltered ? data.Rows.find((row) => row.DeliveryState === 'CONFIRMATION_REQUIRED') ?? null : null
      const link = example ? poLink(can, example.PoNumber) : null
      items.push({
        key: 'open-orders-unconfirmed',
        tone: 'warn',
        text: <><strong>{plural(n, 'PO')}</strong> without a confirmed delivery date{example && <span className="text-slate-500"> — e.g. {example.PoNumber}, {example.VendorName}</span>}</>,
        action: link ? { label: 'Open', to: link } : { label: 'List', onClick: () => onOpen('openOrders') },
      })
    }
  }

  // ----- workload -----
  const workload = reports.workload
  if (workload?.status === 'problem') problem('workload')
  if (workload?.status === 'ready') {
    const data = workload.data
    for (const tile of data.Tiles) {
      if (tile.State !== 'READY' || !tile.Count) continue
      const tone = ageTone(tile.OldestAgeDays, WORKLOAD_AGE)
      if (tone === 'ok') continue
      const phrase = QUEUE_PHRASE[tile.Key]
      const what = phrase ? plural(tile.Count, phrase[0], phrase[1]) : `${tile.Count} in ${tile.Title}`
      const oldest = workload.unfiltered && tile.OldestAgeDays !== null
        ? data.Rows.find((row) => row.Queue === tile.Key && row.AgeDays === tile.OldestAgeDays) ?? null
        : null
      const link = (oldest ? workloadRowLink(can, oldest.Queue, oldest.DocumentNumber) : null) ?? queueListLink(can, tile.Key)
      items.push({
        key: `workload-${tile.Key}`,
        tone: tone === 'bad' ? 'bad' : 'warn',
        text: <><strong>{what}</strong> — oldest {tile.OldestAgeDays} d{oldest && <span className="text-slate-500"> ({oldest.DocumentNumber})</span>}</>,
        action: link ? { label: 'Open', to: link } : { label: 'List', onClick: () => onOpen('workload', { queue: tile.Key }) },
      })
    }
    if (workload.unfiltered) {
      const unclear = data.Rows.filter((row) => row.ResponsibilityIssue)
      if (unclear.length > 0) {
        const first = unclear[0]
        const link = workloadRowLink(can, first.Queue, first.DocumentNumber)
        items.push({
          key: 'workload-responsibility',
          tone: 'warn',
          text: <><strong>{plural(unclear.length, 'document')}</strong> with no responsible person <span className="text-slate-500">({first.DocumentNumber})</span></>,
          action: link ? { label: 'Open', to: link } : { label: 'List', onClick: () => onOpen('workload') },
        })
      }
    }
  }

  // ----- obligations -----
  const obligations = reports.obligations
  if (obligations?.status === 'problem') problem('obligations')
  if (obligations?.status === 'ready') {
    const data = obligations.data
    for (const tile of data.Tiles) {
      if (!tile.Count) continue
      const tone = ageTone(tile.OldestAgeDays, OBLIGATION_AGE)
      if (tone === 'ok') continue
      const money = tile.Amounts.length ? compactAmounts(tile.Amounts.map((a) => ({ Currency: a.Currency, value: a.Value }))) : null
      const oldest = obligations.unfiltered && tile.OldestAgeDays !== null
        ? data.Rows.find((row) => row.Queue === tile.Key && row.AgeDays === tile.OldestAgeDays) ?? null
        : null
      if (tile.Key === 'grni') {
        const link = oldest ? goodsReceiptLink(can, oldest.DocumentId) : can(PAGE_KEYS.vendorBills) ? '/accounts/vendor-bills' : null
        items.push({
          key: 'obligations-grni',
          tone: tone === 'bad' ? 'bad' : 'warn',
          text: <><strong>{plural(tile.Count, 'receipt')} not billed</strong>{money && ` (${money} ex-GST)`} — oldest {tile.OldestAgeDays} d{oldest && <span className="text-slate-500">, {oldest.VendorName}</span>}</>,
          action: link ? { label: 'Open', to: link } : { label: 'List', onClick: () => onOpen('obligations', { queue: 'grni' }) },
        })
      } else {
        const link = tile.Key === 'vendor-advances' && can(PAGE_KEYS.vendorPayments) ? '/accounts/vendor-payments' : null
        const label = tile.Key === 'vendor-advances' ? plural(tile.Count, 'advance') + ' not adjusted' : `${tile.Count} in ${tile.Title}`
        items.push({
          key: `obligations-${tile.Key}`,
          tone: tone === 'bad' ? 'bad' : 'warn',
          text: <><strong>{label}</strong>{money && ` (${money})`} — oldest {tile.OldestAgeDays} d{oldest && <span className="text-slate-500">, {oldest.VendorName}</span>}</>,
          action: link ? { label: 'Open', to: link } : { label: 'List', onClick: () => onOpen('obligations', { queue: tile.Key }) },
        })
      }
    }
  }

  if (reports.spending?.status === 'problem') problem('spending')

  items.sort((a, b) => TONE_ORDER[a.tone] - TONE_ORDER[b.tone])
  const shown = items.slice(0, MAX_ITEMS)
  const more = items.length - shown.length
  const note = [more > 0 ? `+${more} more — see the details below` : '', checking ? 'Checking the remaining sections…' : ''].filter(Boolean).join(' · ')

  return (
    <AttentionList
      items={shown}
      loading={allLoading}
      clearText={checking ? 'Nothing urgent so far.' : 'All clear. Nothing is late or ageing.'}
      note={note || undefined}
    />
  )
}

/**
 * The most-late PO, from the server's overdueOnly detail rows: a separate,
 * read-only request made only when something is overdue.
 */
function useWorstOverdue(enabled: boolean, refreshTick: number): PurchaseOpenOrderRow | null {
  const { data } = usePurchaseSectionQuery(
    () => (enabled ? getPurchaseOpenOrders({ overdueOnly: true, page: 1, pageSize: 100 }) : Promise.reject(new Error('not needed'))),
    `attention-overdue|${enabled}`,
    enabled ? refreshTick : 0,
  )
  if (!enabled) return null
  let worst: PurchaseOpenOrderRow | null = null
  for (const row of data?.Rows ?? []) {
    if (row.DaysLate !== null && (worst === null || (worst.DaysLate ?? 0) < row.DaysLate)) worst = row
  }
  return worst
}
