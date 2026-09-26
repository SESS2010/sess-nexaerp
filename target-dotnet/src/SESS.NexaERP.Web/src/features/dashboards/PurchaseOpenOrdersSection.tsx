import { useState } from 'react'
import { getPurchaseOpenOrders } from '../../api/dashboards'
import type { PurchaseOpenOrdersPage, PurchaseOpenOrderRow } from '../../types/dashboard'
import { formatAge, formatCount, formatDateOnly, formatQuantity } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { itemLink, poLink, vendorLink } from './dashboardAccess'
import {
  BasisNote, DetailFilterNote, MaybeLink, Money, NullValue, Pager, QueryProblem, SectionFrame, StateNotice,
} from './DashboardParts'
import { useDashboardQuery } from './useDashboardQuery'

const PAGE_SIZE = 50

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

export function PurchaseOpenOrdersSection() {
  const { can } = useSession()
  const [filters, setFilters] = useState<Filters>(NO_FILTERS)
  const [page, setPage] = useState(1)

  const { state, reload } = useDashboardQuery(
    () => getPurchaseOpenOrders({
      vendorId: filters.vendor?.id,
      currency: filters.currency,
      rootPurchaseOrderId: filters.root?.id,
      overdueOnly: filters.overdueOnly || undefined,
      page,
      pageSize: PAGE_SIZE,
    }),
    `${filters.vendor?.id}|${filters.currency}|${filters.root?.id}|${filters.overdueOnly}|${page}`,
  )

  const apply = (next: Partial<Filters>) => {
    setFilters((current) => ({ ...current, ...next }))
    setPage(1)
  }

  const data = state.kind === 'ready' ? state.data : null
  return (
    <SectionFrame
      id="purchase-open-orders"
      title="Open purchase orders"
      subtitle="Issued commitments still waiting for material."
      generatedAt={data?.GeneratedAt}
      timeZone={data?.TimeZone}
    >
      {state.kind === 'loading' && <StateNotice kind="loading" title="Loading open purchase orders…" />}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <QueryProblem state={state} onRetry={reload} />}
      {data && (
        <OpenOrdersBody data={data} can={can} filters={filters} onFilter={apply} onClear={() => apply(NO_FILTERS)} onPage={setPage} />
      )}
    </SectionFrame>
  )
}

function OpenOrdersBody({ data, can, filters, onFilter, onClear, onPage }: {
  data: PurchaseOpenOrdersPage
  can: (pageKey: string, action?: string) => boolean
  filters: Filters
  onFilter: (next: Partial<Filters>) => void
  onClear: () => void
  onPage: (page: number) => void
}) {
  const nothingOpen = data.Complete && data.OpenPoCount === 0 && data.TotalRows === 0
  const activeFilters: string[] = []
  if (filters.vendor) activeFilters.push(`vendor ${filters.vendor.label}`)
  if (data.Filters.Currency) activeFilters.push(`currency ${data.Filters.Currency}`)
  if (filters.root) activeFilters.push(`PO ${filters.root.label}`)
  if (data.Filters.OverdueOnly) activeFilters.push('overdue against a confirmed date')

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
                {': '}{SOURCE_ISSUE_TEXT[issue.Code] ?? 'its source records need reconciliation'} <span className="field-hint mono">({issue.Code})</span>
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

      <div className="flex flex-wrap gap-6 my-3">
        <Figure label="Open POs" value={data.OpenPoCount === null ? <NullValue reason="unknown" /> : formatCount(data.OpenPoCount)} />
        <Figure label="Oldest since first issue" value={data.OldestAgeDays === null ? <NullValue reason={data.Complete ? 'none' : 'unknown'} /> : formatAge(data.OldestAgeDays)} />
        <Figure
          label="Overdue POs (confirmed dates)"
          value={data.OverduePoCount === null ? <NullValue reason="unknown" /> : formatCount(data.OverduePoCount)}
          onClick={data.OverduePoCount ? () => onFilter({ overdueOnly: true }) : undefined}
        />
        <Figure label="Delivery date not confirmed" value={formatCount(data.DeliveryDateUnconfirmedPoCount)} />
      </div>

      {data.Amounts === null ? (
        <p className="mb-3">Outstanding value by currency: <NullValue reason="unknown" /></p>
      ) : data.Amounts.length === 0 ? (
        <p className="mb-3">Outstanding value by currency: <NullValue reason="none" /></p>
      ) : (
        <div className="table-wrap mb-3">
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
                <tr key={amount.Currency} className="row-click" onClick={() => onFilter({ currency: amount.Currency })} title="Show only this currency in the detail rows">
                  <td className="mono">{amount.Currency}</td>
                  <td className="text-right">{formatCount(amount.PoCount)}</td>
                  <td className="text-right"><Money value={amount.Value} currency={amount.Currency} /></td>
                  <td className="text-right">{amount.OverduePoCount === null ? <NullValue reason="unknown" /> : formatCount(amount.OverduePoCount)}</td>
                  <td className="text-right"><Money value={amount.OverdueValue} currency={amount.Currency} nullReason="unknown" /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <DetailFilterNote active={activeFilters} onClear={onClear} />

      {data.Rows.length === 0 ? (
        !nothingOpen && <StateNotice kind="empty" title="No detail rows match this selection" />
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>PO</th>
                <th>Vendor</th>
                <th>Item</th>
                <th className="text-right">Remaining</th>
                <th className="text-right">Payable (incl. GST)</th>
                <th>Delivery</th>
                <th className="text-right">Age</th>
              </tr>
            </thead>
            <tbody>
              {data.Rows.map((row) => (
                <OpenOrderRow key={`${row.PurchaseOrderId}-${row.LineId}`} row={row} can={can} onFilter={onFilter} />
              ))}
            </tbody>
          </table>
        </div>
      )}
      <Pager page={data.Filters.Page} pageSize={data.Filters.PageSize} totalRows={data.TotalRows} onPage={onPage} />
    </>
  )
}

function Figure({ label, value, onClick }: { label: string; value: React.ReactNode; onClick?: () => void }) {
  const body = (
    <>
      <div className="field-label">{label}</div>
      <div className="text-xl font-semibold"><span className="mono">{value}</span></div>
    </>
  )
  return onClick ? (
    <button type="button" className="text-left" onClick={onClick} title="Show only these in the detail rows">{body}</button>
  ) : (
    <div>{body}</div>
  )
}

const DELIVERY_BADGE: Record<string, { className: string; label: string }> = {
  OVERDUE: { className: 'badge badge-error', label: 'Overdue' },
  WITHIN_COMMITMENT: { className: 'badge badge-ok', label: 'Within commitment' },
  CONFIRMATION_REQUIRED: { className: 'badge badge-warn', label: 'Date not confirmed' },
}

function OpenOrderRow({ row, can, onFilter }: { row: PurchaseOpenOrderRow; can: (pageKey: string, action?: string) => boolean; onFilter: (next: Partial<Filters>) => void }) {
  const badge = DELIVERY_BADGE[row.DeliveryState] ?? { className: 'badge badge-muted', label: row.DeliveryState }
  return (
    <tr>
      <td className="mono">
        <MaybeLink to={poLink(can, row.PoNumber)}>{row.PoNumber}</MaybeLink> r{row.RevisionNumber}
        {row.CurrentRevisionNumber !== row.RevisionNumber && (
          <div className="field-hint">Current r{row.CurrentRevisionNumber} is {row.CurrentStatus}; quantities are from issued r{row.RevisionNumber}</div>
        )}
        <div>
          <button type="button" className="link-button" onClick={() => onFilter({ root: { id: row.RootPurchaseOrderId, label: row.PoNumber } })}>
            only this PO
          </button>
        </div>
      </td>
      <td>
        <MaybeLink to={vendorLink(can, row.VendorCode)}><span className="mono">{row.VendorCode}</span></MaybeLink>
        <div className="field-hint">{row.VendorName}</div>
        <button type="button" className="link-button" onClick={() => onFilter({ vendor: { id: row.VendorId, label: row.VendorCode } })}>only this vendor</button>
      </td>
      <td>
        <MaybeLink to={itemLink(can, row.ItemCode)}><span className="mono">{row.ItemCode}</span></MaybeLink>
        <div className="field-hint">{row.ItemName}</div>
      </td>
      <td className="text-right">
        <span className="mono">{formatQuantity(row.RemainingQuantity)} {row.Uom}</span>
        <div className="field-hint">of {formatQuantity(row.OrderedQuantity)} ordered, {formatQuantity(row.ReceivedQuantity)} received</div>
      </td>
      <td className="text-right"><Money value={row.Value} currency={row.Currency} /></td>
      <td>
        <span className={badge.className}>{badge.label}</span>
        <div className="field-hint">
          {row.CommittedDeliveryDate ? `Committed ${formatDateOnly(row.CommittedDeliveryDate)}` : 'No confirmed commitment'}
          {' · quoted '}{formatDateOnly(row.QuotedDeliveryDate)}
        </div>
        {row.DaysLate !== null && row.DaysLate > 0 && <div className="field-hint">{formatAge(row.DaysLate)} late</div>}
        <div className="field-hint">{row.DeliveryTerms}</div>
      </td>
      <td className="text-right">{formatAge(row.AgeDays)}</td>
    </tr>
  )
}
