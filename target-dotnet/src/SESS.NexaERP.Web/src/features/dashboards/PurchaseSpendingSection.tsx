import { useState } from 'react'
import { getPurchaseSpending } from '../../api/dashboards'
import type { PurchaseSpendingGroup, PurchaseSpendingPage, PurchaseSpendingPeriodKey, PurchaseSpendingAmount } from '../../types/dashboard'
import { countOf, formatCount, formatDateOnly, formatMonth, formatQuantity } from '../../utils/dashboardFormat'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { itemLink, poLink, vendorLink } from './dashboardAccess'
import {
  BasisNote, DetailFilterNote, MaybeLink, Money, NullValue, Pager, QueryProblem, SectionFrame, StateNotice,
} from './DashboardParts'
import { useDashboardQuery } from './useDashboardQuery'

const PAGE_SIZE = 50

const PERIOD_LABEL: Record<string, string> = {
  month: 'This month',
  quarter: 'This calendar quarter',
  'financial-year': 'This financial year (from 1 April)',
  'twelve-months': 'Last twelve months',
}

interface Filters {
  period: PurchaseSpendingPeriodKey
  month: string | null
  vendor: { id: string; label: string } | null
  category: { id: string; label: string } | null
  bill: { id: string; label: string } | null
  currency: string | null
}

const DEFAULT_FILTERS: Filters = { period: 'financial-year', month: null, vendor: null, category: null, bill: null, currency: null }

export function PurchaseSpendingSection() {
  const { can } = useSession()
  const [filters, setFilters] = useState<Filters>(DEFAULT_FILTERS)
  const [page, setPage] = useState(1)

  const { state, reload } = useDashboardQuery(
    () => getPurchaseSpending({
      period: filters.period,
      month: filters.period === 'month' ? filters.month : null,
      vendorId: filters.vendor?.id,
      categoryId: filters.category?.id,
      billId: filters.bill?.id,
      currency: filters.currency,
      page,
      pageSize: PAGE_SIZE,
    }),
    `${filters.period}|${filters.month}|${filters.vendor?.id}|${filters.category?.id}|${filters.bill?.id}|${filters.currency}|${page}`,
  )

  const apply = (next: Partial<Filters>) => {
    setFilters((current) => ({ ...current, ...next }))
    setPage(1)
  }

  const data = state.kind === 'ready' ? state.data : null
  return (
    <SectionFrame
      id="purchase-spending"
      title="Spending"
      subtitle="Accepted payable purchases plus allocated charges, less reversals."
      generatedAt={data?.GeneratedAt}
      timeZone={data?.TimeZone}
    >
      {state.kind === 'loading' && <StateNotice kind="loading" title="Loading spending…" />}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <QueryProblem state={state} onRetry={reload} />}
      {data && <SpendingBody data={data} can={can} filters={filters} onFilter={apply} onClear={() => apply(DEFAULT_FILTERS)} onPage={setPage} />}
    </SectionFrame>
  )
}

function SpendingBody({ data, can, filters, onFilter, onClear, onPage }: {
  data: PurchaseSpendingPage
  can: (pageKey: string, action?: string) => boolean
  filters: Filters
  onFilter: (next: Partial<Filters>) => void
  onClear: () => void
  onPage: (page: number) => void
}) {
  const noActivity = data.Periods.every((period) => period.Amounts.length === 0)
    && data.MonthlyTrend.every((month) => month.Amounts.length === 0)
    && data.TotalRows === 0

  const activeFilters: string[] = []
  if (filters.vendor) activeFilters.push(`vendor ${filters.vendor.label}`)
  if (filters.category) activeFilters.push(`category ${filters.category.label}`)
  if (filters.bill) activeFilters.push(`bill ${filters.bill.label}`)
  if (data.Filters.Currency) activeFilters.push(`currency ${data.Filters.Currency}`)
  const canOpenBills = can(PAGE_KEYS.vendorBills)

  return (
    <>
      <BasisNote>{data.Basis}</BasisNote>
      <div className="alert alert-warn mb-3" data-dashboard-note="not-ex-tax">
        <div className="alert-body">
          <strong>These figures include GST. They are not ex-tax or Actual BOM component cost.</strong>{' '}
          "Billed payable" is the accepted bill-line payable value with its embedded GST; allocated charges are added; reversals subtract.
          A bill of base 4,000 + GST 720 + freight 12 shows as 4,732 here. No payment status is implied.
        </div>
      </div>

      {noActivity && (
        <StateNotice kind="empty" title="No accepted purchase bills in these periods">
          You are permitted to see spending, and there is no bill acceptance or reversal activity.
        </StateNotice>
      )}

      <div className="grid gap-3 my-3" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))' }}>
        {data.Periods.map((period) => (
          <div key={period.Key} className="home-tile">
            <div className="home-tile-title">{PERIOD_LABEL[period.Key] ?? period.Key}</div>
            <div className="field-hint">{formatDateOnly(period.FromDate)} – {formatDateOnly(period.ToDate)}</div>
            <SpendingAmounts amounts={period.Amounts} />
          </div>
        ))}
      </div>

      <div className="grid gap-4 mb-3" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(340px, 1fr))' }}>
        <GroupTable title="Top vendors this financial year (top ten per currency)" groups={data.TopVendors} kind="vendor" can={can}
          onPick={(group) => onFilter({ vendor: { id: group.Id, label: group.Code }, currency: group.Currency })} />
        <GroupTable title="Categories this financial year" groups={data.Categories} kind="category" can={can}
          onPick={(group) => onFilter({ category: { id: group.Id, label: group.Code }, currency: group.Currency })} />
      </div>

      <h3 className="form-section-title">Monthly trend (oldest first)</h3>
      <div className="table-wrap mb-3">
        <table className="table">
          <thead>
            <tr>
              <th>Month</th>
              <th>Amount by currency</th>
              <th className="text-right">POs · bills</th>
            </tr>
          </thead>
          <tbody>
            {data.MonthlyTrend.map((month) => {
              const selected = data.Filters.Period === 'month' && data.Filters.Month === month.FromDate
              return (
                <tr key={month.Key} className="row-click" style={{ fontWeight: selected ? 600 : undefined }}
                  title="Show this month's detail rows"
                  onClick={() => onFilter({ period: 'month', month: month.FromDate })}>
                  <td>{formatMonth(month.Key)}</td>
                  <td>
                    {month.Amounts.length === 0 ? <NullValue reason="none" /> : month.Amounts.map((amount) => (
                      <div key={amount.Currency}><Money value={amount.Amount} currency={amount.Currency} /></div>
                    ))}
                  </td>
                  <td className="text-right">
                    {month.Amounts.map((amount) => <div key={amount.Currency}>{formatCount(amount.PoCount)} · {formatCount(amount.BillCount)}</div>)}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      <div className="toolbar mb-2 flex flex-wrap items-center gap-2">
        <label className="field-label" htmlFor="spending-period">Detail rows for</label>
        <select id="spending-period" className="input" style={{ width: 'auto' }} value={filters.period}
          onChange={(event) => onFilter({ period: event.target.value as PurchaseSpendingPeriodKey, month: event.target.value === 'month' ? filters.month : null })}>
          {Object.entries(PERIOD_LABEL).map(([key, label]) => <option key={key} value={key}>{label}</option>)}
        </select>
        {filters.period === 'month' && (
          <select className="input" style={{ width: 'auto' }} value={filters.month ?? ''} aria-label="Month"
            onChange={(event) => onFilter({ month: event.target.value || null })}>
            <option value="">Current month</option>
            {data.MonthlyTrend.map((month) => <option key={month.Key} value={month.FromDate}>{formatMonth(month.Key)}</option>)}
          </select>
        )}
        <span className="field-hint">{formatDateOnly(data.FromDate)} – {formatDateOnly(data.ToDate)}</span>
      </div>

      <DetailFilterNote active={activeFilters} onClear={() => onClear()} />

      {data.Rows.length === 0 ? (
        !noActivity && <StateNotice kind="empty" title="No bill events match this selection" />
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Decision date</th>
                <th>Bill</th>
                <th>Event</th>
                <th>PO</th>
                <th>Vendor</th>
                <th>Category · item</th>
                <th className="text-right">Quantity</th>
                <th className="text-right">Billed payable (incl. GST)</th>
                <th className="text-right">Allocated charges</th>
                <th className="text-right">Amount</th>
              </tr>
            </thead>
            <tbody>
              {/* One bill line can be accepted, reversed and accepted again on the same
                  date, so no field combination is unique; the server's order is stable. */}
              {data.Rows.map((row, index) => (
                <tr key={`${row.BillLineId}-${row.Event}-${index}`}>
                  <td>{formatDateOnly(row.EventDate)}</td>
                  <td className="mono">
                    <button type="button" className="link-button mono" title="Show only this bill"
                      onClick={() => onFilter({ bill: { id: row.BillId, label: row.BillNumber } })}>{row.BillNumber}</button>
                    {/* Accounts drill-down only with its own page permission; dashboard access grants none. */}
                    {canOpenBills && <div className="field-hint"><MaybeLink to={`/accounts/vendor-bills/${encodeURIComponent(row.BillId)}`}>open bill</MaybeLink></div>}
                  </td>
                  <td><span className={row.Event === 'REVERSED' ? 'badge badge-error' : 'badge badge-ok'}>{row.Event === 'REVERSED' ? 'Reversed (subtracts)' : 'Accepted'}</span></td>
                  <td className="mono"><MaybeLink to={poLink(can, row.PoNumber)}>{row.PoNumber}</MaybeLink></td>
                  <td>
                    <MaybeLink to={vendorLink(can, row.VendorCode)}><span className="mono">{row.VendorCode}</span></MaybeLink>
                    <div className="field-hint">{row.VendorName}</div>
                  </td>
                  <td>
                    <span className="mono">{row.CategoryCode}</span>{' · '}
                    <MaybeLink to={itemLink(can, row.ItemCode)}><span className="mono">{row.ItemCode}</span></MaybeLink>
                    <div className="field-hint">{row.ItemName}</div>
                  </td>
                  <td className="text-right mono">{formatQuantity(row.Quantity)} {row.Uom}</td>
                  <td className="text-right"><Money value={row.MaterialValue} currency={row.Currency} /></td>
                  <td className="text-right"><Money value={row.AllocatedCharges} currency={row.Currency} /></td>
                  <td className="text-right"><Money value={row.Amount} currency={row.Currency} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <Pager page={data.Filters.Page} pageSize={data.Filters.PageSize} totalRows={data.TotalRows} onPage={onPage} />
    </>
  )
}

function SpendingAmounts({ amounts }: { amounts: PurchaseSpendingAmount[] }) {
  if (amounts.length === 0) return <div className="mt-1"><NullValue reason="none" /></div>
  return (
    <div className="mt-1">
      {amounts.map((amount) => (
        <div key={amount.Currency} className="mb-1">
          <div className="text-xl font-semibold"><Money value={amount.Amount} currency={amount.Currency} /></div>
          <div className="field-hint">
            billed payable incl. GST <Money value={amount.MaterialValue} currency={amount.Currency} />
            {' + '}charges <Money value={amount.AllocatedCharges} currency={amount.Currency} />
          </div>
          <div className="field-hint">{countOf(amount.PoCount, 'PO')} · {countOf(amount.BillCount, 'bill')} with activity</div>
        </div>
      ))}
    </div>
  )
}

function GroupTable({ title, groups, kind, can, onPick }: {
  title: string
  groups: PurchaseSpendingGroup[]
  kind: 'vendor' | 'category'
  can: (pageKey: string, action?: string) => boolean
  onPick: (group: PurchaseSpendingGroup) => void
}) {
  return (
    <div>
      <h3 className="form-section-title">{title}</h3>
      {groups.length === 0 ? (
        <p><NullValue reason="none" /></p>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>{kind === 'vendor' ? 'Vendor' : 'Category'}</th>
                <th className="text-right">Amount</th>
                <th className="text-right">POs · bills</th>
              </tr>
            </thead>
            <tbody>
              {groups.map((group) => (
                <tr key={`${group.Id}-${group.Currency}`} className="row-click" onClick={() => onPick(group)} title="Show only these in the detail rows">
                  <td>
                    {kind === 'vendor' ? (
                      <>
                        <MaybeLink to={vendorLink(can, group.Code)}><span className="mono">{group.Code}</span></MaybeLink>
                        <div className="field-hint">{group.Name}</div>
                      </>
                    ) : (
                      <span className="mono">{group.Code}</span>
                    )}
                  </td>
                  <td className="text-right"><Money value={group.Amount} currency={group.Currency} /></td>
                  <td className="text-right">{formatCount(group.PoCount)} · {formatCount(group.BillCount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
