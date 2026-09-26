import { useMemo, useState } from 'react'
import { getPurchaseOpenOrders } from '../../api/dashboards'
import { SortableHeader } from '../../components/SortableHeader'
import { useSort } from '../../hooks/useSort'
import type { PurchaseOpenOrdersPage, PurchaseOpenOrderRow } from '../../types/dashboard'
import { formatAge, formatCount, formatDateOnly, formatQuantity } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { itemLink, poLink, vendorLink } from './dashboardAccess'
import {
  BasisNote, DetailFilterNote, MaybeLink, Money, NullValue, Pager, QueryProblem, SectionFrame, StateNotice,
} from './DashboardParts'
import { HBarChart, type HBar } from './PurchaseCharts'
import {
  AgeText, QuickFilter, RefreshingHint, SERVER_ORDER, SEVERITY, SectionSkeleton, matchesText, sortRows, useFocusRequest,
  usePurchaseSectionQuery, useReportToPage, type FocusRequest, type SectionReport,
} from './PurchaseDashboardKit'

const PAGE_SIZE = 50

type Can = (pageKey: string, action?: string) => boolean

const SOURCE_ISSUE_TEXT: Record<string, string> = {
  CURRENT_REVISION_UNAVAILABLE: 'the current revision cannot be read',
  CANCELLED_UNISSUED_AMENDMENT: 'an unissued amendment was cancelled',
  LINE_PROVENANCE_INCONSISTENT: 'its lines do not trace back consistently',
  RECEIPT_QUANTITY_INCONSISTENT: 'its received quantities are inconsistent',
}

interface Filters {
  vendor: { id: string; label: string } | null
  currency: string | null
  root: { id: string; label: string } | null
  overdueOnly: boolean
}

const NO_FILTERS: Filters = { vendor: null, currency: null, root: null, overdueOnly: false }

/** Focus targets the page may send: `overdue` narrows to overdue lines, anything else clears. */
export const OPEN_ORDERS_FOCUS_OVERDUE = 'overdue'

export function PurchaseOpenOrdersSection({ refreshTick = 0, onReport, focus }: {
  refreshTick?: number
  onReport?: (report: SectionReport<PurchaseOpenOrdersPage>) => void
  focus?: FocusRequest | null
}) {
  const { can } = useSession()
  const [filters, setFilters] = useState<Filters>(NO_FILTERS)
  const [page, setPage] = useState(1)

  const { state, data, refreshing, reload } = usePurchaseSectionQuery(
    () => getPurchaseOpenOrders({
      vendorId: filters.vendor?.id,
      currency: filters.currency,
      rootPurchaseOrderId: filters.root?.id,
      overdueOnly: filters.overdueOnly || undefined,
      page,
      pageSize: PAGE_SIZE,
    }),
    `${filters.vendor?.id}|${filters.currency}|${filters.root?.id}|${filters.overdueOnly}|${page}`,
    refreshTick,
  )
  const unfiltered = !filters.vendor && !filters.currency && !filters.root && !filters.overdueOnly && page === 1
  useReportToPage(onReport, state, data, unfiltered)

  const apply = (next: Partial<Filters>) => {
    setFilters((current) => ({ ...current, ...next }))
    setPage(1)
  }

  useFocusRequest(focus, (target) => {
    setFilters(target === OPEN_ORDERS_FOCUS_OVERDUE ? { ...NO_FILTERS, overdueOnly: true } : NO_FILTERS)
    setPage(1)
  })

  return (
    <SectionFrame
      id="purchase-open-orders"
      title="Open purchase orders"
      subtitle={<>Issued commitments still waiting for material. <RefreshingHint on={refreshing} /></>}
      generatedAt={data?.GeneratedAt}
      timeZone={data?.TimeZone}
    >
      {!data && state.kind === 'loading' && (
        <>
          <StateNotice kind="loading" title="Loading open purchase orders…" />
          <SectionSkeleton tiles={4} />
        </>
      )}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <QueryProblem state={state} onRetry={reload} />}
      {data && (
        <OpenOrdersBody data={data} can={can} filters={filters} onFilter={apply} onClear={() => apply(NO_FILTERS)} onPage={setPage} />
      )}
    </SectionFrame>
  )
}

function OpenOrdersBody({ data, can, filters, onFilter, onClear, onPage }: {
  data: PurchaseOpenOrdersPage
  can: Can
  filters: Filters
  onFilter: (next: Partial<Filters>) => void
  onClear: () => void
  onPage: (page: number) => void
}) {
  const [search, setSearch] = useState('')
  const { sort, toggleSort } = useSort(SERVER_ORDER)
  const nothingOpen = data.Complete && data.OpenPoCount === 0 && data.TotalRows === 0
  const activeFilters: string[] = []
  if (filters.vendor) activeFilters.push(`vendor ${filters.vendor.label}`)
  if (data.Filters.Currency) activeFilters.push(`currency ${data.Filters.Currency}`)
  if (filters.root) activeFilters.push(`PO ${filters.root.label}`)
  if (data.Filters.OverdueOnly) activeFilters.push('overdue against a confirmed date')

  const rows = useMemo(() => {
    const filtered = data.Rows.filter((row) => matchesText(search, [
      row.PoNumber, row.VendorCode, row.VendorName, row.ItemCode, row.ItemName, row.DeliveryTerms,
      DELIVERY_BADGE[row.DeliveryState]?.label ?? row.DeliveryState, row.Currency,
    ]))
    return sortRows<PurchaseOpenOrderRow>(filtered, sort, {
      po: (row) => row.PoNumber,
      vendor: (row) => row.VendorCode,
      item: (row) => row.ItemCode,
      remaining: (row) => row.RemainingQuantity,
      value: (row) => row.Value,
      late: (row) => row.DaysLate,
      age: (row) => row.AgeDays,
    })
  }, [data.Rows, search, sort])

  const statusBars: HBar[] = [
    {
      key: 'open', label: 'Open POs', labelText: 'Open POs', value: data.OpenPoCount,
      display: data.OpenPoCount === null ? <NullValue reason="unknown" /> : <span className="mono font-semibold">{formatCount(data.OpenPoCount)}</span>,
      displayText: data.OpenPoCount === null ? 'unknown' : String(data.OpenPoCount), color: SEVERITY.info.bar,
    },
    {
      key: 'overdue', label: 'Past a confirmed delivery date', labelText: 'Overdue POs', value: data.OverduePoCount,
      display: data.OverduePoCount === null ? <NullValue reason="unknown" /> : <span className="mono font-semibold">{formatCount(data.OverduePoCount)}</span>,
      displayText: data.OverduePoCount === null ? 'unknown' : String(data.OverduePoCount), color: SEVERITY.red.bar,
      onClick: data.OverduePoCount ? () => onFilter({ overdueOnly: !filters.overdueOnly }) : undefined,
      clickHint: 'Show only overdue lines below', selected: filters.overdueOnly,
    },
    {
      key: 'unconfirmed', label: 'Delivery date not confirmed', labelText: 'Delivery date not confirmed', value: data.DeliveryDateUnconfirmedPoCount,
      display: <span className="mono font-semibold">{formatCount(data.DeliveryDateUnconfirmedPoCount)}</span>,
      displayText: String(data.DeliveryDateUnconfirmedPoCount), color: SEVERITY.amber.bar,
    },
  ]

  return (
    <>
      <BasisNote>
        {data.Basis} This is the <strong>gross payable commitment including embedded GST and charges</strong>, not ex-tax cost.
      </BasisNote>

      {!data.Complete && (
        <StateNotice kind="incomplete" title="Some purchase orders could not be reconciled, so overall totals are unknown">
          <p>The overall PO count, amounts and oldest age cannot be stated. They are unknown, not zero. The rows below are reliable, but these POs are excluded from them:</p>
          <ul className="list-disc ml-5">
            {data.SourceIssues.map((issue) => (
              <li key={`${issue.RootPurchaseOrderId}-${issue.Code}`}>
                <MaybeLink to={poLink(can, issue.PoNumber)}><span className="mono">{issue.PoNumber}</span></MaybeLink>
                {': '}{SOURCE_ISSUE_TEXT[issue.Code] ?? 'its source records need reconciliation'} <span className="text-[12px] text-ink-faint mono">({issue.Code})</span>
              </li>
            ))}
          </ul>
          <p>Ask the administrator to reconcile these purchase orders.</p>
        </StateNotice>
      )}

      {data.Complete && !data.DeliveryComplete && (
        <StateNotice kind="incomplete" title="Overdue totals are unknown: some delivery dates are not confirmed">
          {formatCount(data.DeliveryDateUnconfirmedPoCount)} open PO{data.DeliveryDateUnconfirmedPoCount === 1 ? ' needs' : 's need'} delivery-date
          confirmation. Until then the overdue figures cannot be stated. They are unknown, not zero.
        </StateNotice>
      )}

      {nothingOpen && (
        <StateNotice kind="empty" title="No open purchase orders">
          You are permitted to see open purchase orders, and none is outstanding.
        </StateNotice>
      )}

      <div className="grid gap-4 my-3 lg:grid-cols-[minmax(0,5fr)_minmax(0,7fr)]">
        <div className="rounded-lg border border-line p-3">
          <h3 className="text-[13px] font-semibold mb-2">Delivery status (POs)</h3>
          <HBarChart bars={statusBars} caption="Open purchase orders, overdue ones and ones without a confirmed delivery date" />
          <div className="flex flex-wrap gap-x-6 gap-y-1 mt-3 text-[12.5px] text-ink-soft">
            <span>Oldest since first issue: <strong className="text-ink">{data.OldestAgeDays === null ? <NullValue reason={data.Complete ? 'none' : 'unknown'} /> : formatAge(data.OldestAgeDays)}</strong></span>
          </div>
        </div>

        <div>
          {data.Amounts === null ? (
            <p className="mb-3">Outstanding value by currency: <NullValue reason="unknown" /></p>
          ) : data.Amounts.length === 0 ? (
            <p className="mb-3">Outstanding value by currency: <NullValue reason="none" /></p>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Currency</th>
                    <th className="text-right">POs</th>
                    <th className="text-right">Outstanding payable (incl. GST)</th>
                    <th className="text-right">Overdue POs</th>
                    <th className="text-right">Overdue payable</th>
                  </tr>
                </thead>
                <tbody>
                  {data.Amounts.map((amount) => (
                    <tr key={amount.Currency} className={`row-click ${data.Filters.Currency === amount.Currency ? 'row-selected' : ''}`}
                      onClick={() => onFilter({ currency: amount.Currency })} title="Show only this currency in the detail rows">
                      <td className="mono">{amount.Currency}</td>
                      <td className="text-right">{formatCount(amount.PoCount)}</td>
                      <td className="text-right font-semibold"><Money value={amount.Value} currency={amount.Currency} /></td>
                      <td className={`text-right ${amount.OverduePoCount ? 'font-semibold text-red-700' : ''}`}>
                        {amount.OverduePoCount === null ? <NullValue reason="unknown" /> : formatCount(amount.OverduePoCount)}
                      </td>
                      <td className="text-right"><Money value={amount.OverdueValue} currency={amount.Currency} nullReason="unknown" /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <p className="text-[11.5px] text-ink-faint mt-2">Click a currency to list only its lines. Each currency is shown on its own line and never added to another.</p>
        </div>
      </div>

      <DetailFilterNote active={activeFilters} onClear={onClear} />

      {data.Rows.length === 0 ? (
        !nothingOpen && <StateNotice kind="empty" title="No detail rows match this selection" />
      ) : (
        <>
          <QuickFilter id="open-orders-search" value={search} onChange={setSearch} shown={rows.length} onPage={data.Rows.length}
            placeholder="Search PO, vendor, item, delivery…">
            {!filters.overdueOnly && data.OverduePoCount ? (
              <button type="button" className="btn btn-ghost" onClick={() => onFilter({ overdueOnly: true })}>Only overdue</button>
            ) : null}
          </QuickFilter>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <SortableHeader label="PO" sortKey="po" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Vendor" sortKey="vendor" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Item" sortKey="item" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Remaining" sortKey="remaining" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Payable (incl. GST)" sortKey="value" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Delivery · days late" sortKey="late" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Age" sortKey="age" sort={sort} onSort={toggleSort} />
                </tr>
              </thead>
              <tbody>
                {rows.length === 0 && (
                  <tr><td colSpan={7} className="table-empty">No rows on this page match "{search}".</td></tr>
                )}
                {rows.map((row) => (
                  <OpenOrderRow key={`${row.PurchaseOrderId}-${row.LineId}`} row={row} can={can} onFilter={onFilter} />
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
      <Pager page={data.Filters.Page} pageSize={data.Filters.PageSize} totalRows={data.TotalRows} onPage={onPage} />
    </>
  )
}

const DELIVERY_BADGE: Record<string, { className: string; label: string }> = {
  OVERDUE: { className: 'badge badge-error', label: 'Overdue' },
  WITHIN_COMMITMENT: { className: 'badge badge-ok', label: 'Within commitment' },
  CONFIRMATION_REQUIRED: { className: 'badge badge-warn', label: 'Date not confirmed' },
}

function OpenOrderRow({ row, can, onFilter }: { row: PurchaseOpenOrderRow; can: Can; onFilter: (next: Partial<Filters>) => void }) {
  const badge = DELIVERY_BADGE[row.DeliveryState] ?? { className: 'badge badge-muted', label: row.DeliveryState }
  const late = row.DaysLate !== null && row.DaysLate > 0
  return (
    <tr style={row.DeliveryState === 'OVERDUE' ? { background: 'rgba(254, 242, 242, 0.6)' } : undefined}>
      <td className="mono">
        <MaybeLink to={poLink(can, row.PoNumber)}>{row.PoNumber}</MaybeLink> r{row.RevisionNumber}
        {row.CurrentRevisionNumber !== row.RevisionNumber && (
          <div className="field-hint">Current r{row.CurrentRevisionNumber} is {row.CurrentStatus}; quantities are from issued r{row.RevisionNumber}</div>
        )}
        <div>
          <button type="button" className="link-button text-[12px]" onClick={() => onFilter({ root: { id: row.RootPurchaseOrderId, label: row.PoNumber } })}>
            only this PO
          </button>
        </div>
      </td>
      <td>
        <MaybeLink to={vendorLink(can, row.VendorCode)}><span className="mono">{row.VendorCode}</span></MaybeLink>
        <div className="field-hint">{row.VendorName}</div>
        <button type="button" className="link-button text-[12px]" onClick={() => onFilter({ vendor: { id: row.VendorId, label: row.VendorCode } })}>only this vendor</button>
      </td>
      <td>
        <MaybeLink to={itemLink(can, row.ItemCode)}><span className="mono">{row.ItemCode}</span></MaybeLink>
        <div className="field-hint">{row.ItemName}</div>
      </td>
      <td className="text-right">
        <span className="mono">{formatQuantity(row.RemainingQuantity)} {row.Uom}</span>
        <div className="field-hint">of {formatQuantity(row.OrderedQuantity)} ordered, {formatQuantity(row.ReceivedQuantity)} received</div>
        {row.ReceivedQuantity === 0 && <div className="text-[11.5px] text-amber-700">nothing received yet</div>}
      </td>
      <td className="text-right"><Money value={row.Value} currency={row.Currency} /></td>
      <td>
        <span className={badge.className}>{badge.label}</span>
        {late && <div className="text-[12.5px] font-semibold text-red-700 mt-1">{formatAge(row.DaysLate)} late</div>}
        <div className="field-hint">
          {row.CommittedDeliveryDate ? `Committed ${formatDateOnly(row.CommittedDeliveryDate)}` : 'No confirmed commitment'}
          {' · quoted '}{formatDateOnly(row.QuotedDeliveryDate)}
        </div>
        <div className="field-hint">{row.DeliveryTerms}</div>
      </td>
      <td className="text-right"><AgeText days={row.AgeDays} /></td>
    </tr>
  )
}

