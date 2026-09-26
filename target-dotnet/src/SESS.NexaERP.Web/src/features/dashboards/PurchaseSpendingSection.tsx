import { useMemo, useState } from 'react'
import { getPurchaseSpending } from '../../api/dashboards'
import { SortableHeader } from '../../components/SortableHeader'
import { useSort } from '../../hooks/useSort'
import type {
  PurchaseSpendingAmount, PurchaseSpendingGroup, PurchaseSpendingPage, PurchaseSpendingPeriodKey, PurchaseSpendingRow,
} from '../../types/dashboard'
import { countOf, formatAmount, formatCount, formatDateOnly, formatMonth, formatQuantity } from '../../utils/dashboardFormat'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { itemLink, poLink, vendorLink } from './dashboardAccess'
import {
  BasisNote, DetailFilterNote, MaybeLink, Money, NullValue, Pager, QueryProblem, SectionFrame, StateNotice,
} from './DashboardParts'
import { ColumnChart, HBarChart, type HBar } from './PurchaseCharts'
import {
  QuickFilter, RefreshingHint, SERVER_ORDER, SectionSkeleton, matchesText, sortRows, useFocusRequest, usePurchaseSectionQuery,
  useReportToPage, type FocusRequest, type SectionReport,
} from './PurchaseDashboardKit'

const PAGE_SIZE = 50

type Can = (pageKey: string, action?: string) => boolean

export const SPENDING_PERIOD_LABEL: Record<string, string> = {
  month: 'This month',
  quarter: 'This calendar quarter',
  'financial-year': 'This financial year (from 1 April)',
  'twelve-months': 'Last twelve months',
}

/** Shown beside every spending figure on the page. */
export const SPENDING_GST_NOTE = 'incl. GST'

interface Filters {
  period: PurchaseSpendingPeriodKey
  month: string | null
  vendor: { id: string; label: string } | null
  category: { id: string; label: string } | null
  bill: { id: string; label: string } | null
  currency: string | null
}

const DEFAULT_FILTERS: Filters = { period: 'financial-year', month: null, vendor: null, category: null, bill: null, currency: null }

export function PurchaseSpendingSection({ refreshTick = 0, onReport, focus }: {
  refreshTick?: number
  onReport?: (report: SectionReport<PurchaseSpendingPage>) => void
  focus?: FocusRequest | null
}) {
  const { can } = useSession()
  const [filters, setFilters] = useState<Filters>(DEFAULT_FILTERS)
  const [page, setPage] = useState(1)

  const { state, data, refreshing, reload } = usePurchaseSectionQuery(
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
    refreshTick,
  )
  const unfiltered = !filters.vendor && !filters.category && !filters.bill && !filters.currency && page === 1
  useReportToPage(onReport, state, data, unfiltered)

  const apply = (next: Partial<Filters>) => {
    setFilters((current) => ({ ...current, ...next }))
    setPage(1)
  }

  // The page may ask for one of the contract's periods (never a date range).
  useFocusRequest(focus, (target) => {
    if (target in SPENDING_PERIOD_LABEL) apply({ period: target as PurchaseSpendingPeriodKey, month: null })
  })

  return (
    <SectionFrame
      id="purchase-spending"
      title="Spending"
      subtitle={<>Accepted payable purchases plus allocated charges, less reversals. <RefreshingHint on={refreshing} /></>}
      generatedAt={data?.GeneratedAt}
      timeZone={data?.TimeZone}
    >
      {!data && state.kind === 'loading' && (
        <>
          <StateNotice kind="loading" title="Loading spending…" />
          <SectionSkeleton tiles={3} />
        </>
      )}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <QueryProblem state={state} onRetry={reload} />}
      {data && <SpendingBody data={data} can={can} filters={filters} onFilter={apply} onClear={() => apply(DEFAULT_FILTERS)} onPage={setPage} />}
    </SectionFrame>
  )
}

function SpendingBody({ data, can, filters, onFilter, onClear, onPage }: {
  data: PurchaseSpendingPage
  can: Can
  filters: Filters
  onFilter: (next: Partial<Filters>) => void
  onClear: () => void
  onPage: (page: number) => void
}) {
  const [search, setSearch] = useState('')
  const { sort, toggleSort } = useSort(SERVER_ORDER)
  const noActivity = data.Periods.every((period) => period.Amounts.length === 0)
    && data.MonthlyTrend.every((month) => month.Amounts.length === 0)
    && data.TotalRows === 0

  const activeFilters: string[] = []
  if (filters.vendor) activeFilters.push(`vendor ${filters.vendor.label}`)
  if (filters.category) activeFilters.push(`category ${filters.category.label}`)
  if (filters.bill) activeFilters.push(`bill ${filters.bill.label}`)
  if (data.Filters.Currency) activeFilters.push(`currency ${data.Filters.Currency}`)
  const canOpenBills = can(PAGE_KEYS.vendorBills)

  // Currencies seen in the trend: one chart each, never added together.
  const trendCurrencies = useMemo(() => {
    const seen: string[] = []
    for (const month of data.MonthlyTrend) for (const amount of month.Amounts) if (!seen.includes(amount.Currency)) seen.push(amount.Currency)
    return seen
  }, [data.MonthlyTrend])

  const rows = useMemo(() => {
    const filtered = data.Rows.filter((row) => matchesText(search, [
      row.BillNumber, row.PoNumber, row.VendorCode, row.VendorName, row.CategoryCode, row.ItemCode, row.ItemName, row.Event, row.Currency,
    ]))
    return sortRows<PurchaseSpendingRow>(filtered, sort, {
      date: (row) => row.EventDate,
      bill: (row) => row.BillNumber,
      event: (row) => row.Event,
      po: (row) => row.PoNumber,
      vendor: (row) => row.VendorCode,
      category: (row) => `${row.CategoryCode} ${row.ItemCode}`,
      quantity: (row) => row.Quantity,
      amount: (row) => row.Amount,
    })
  }, [data.Rows, search, sort])

  const isSelectedMonth = (fromDate: string) => data.Filters.Period === 'month' && data.FromDate === fromDate

  return (
    <>
      <BasisNote>{data.Basis}</BasisNote>
      <div className="alert alert-warn mb-3" data-dashboard-note="not-ex-tax">
        <div className="alert-body">
          <strong>These figures include GST. They are not ex-tax or Actual BOM component cost.</strong>{' '}
          "Billed payable" is the accepted bill-line payable value with its embedded GST; allocated charges are added; reversals subtract.
          A bill of base 4,000 + GST 720 + freight 12 shows as 4,732 here. No payment status is implied.
          The backend does not yet separate recoverable GST, so no ex-tax figure can be shown.
        </div>
      </div>

      <div className="toolbar mb-3 flex flex-wrap items-center gap-2 rounded-lg border border-line bg-slate-50/60 px-3 py-2">
        <label className="text-[13px] font-semibold" htmlFor="spending-period">Period</label>
        <select id="spending-period" className="input" style={{ width: 'auto' }} value={filters.period}
          onChange={(event) => onFilter({ period: event.target.value as PurchaseSpendingPeriodKey, month: event.target.value === 'month' ? filters.month : null })}>
          {Object.entries(SPENDING_PERIOD_LABEL).map(([key, label]) => <option key={key} value={key}>{label}</option>)}
        </select>
        {filters.period === 'month' && (
          <select className="input" style={{ width: 'auto' }} value={filters.month ?? ''} aria-label="Month"
            onChange={(event) => onFilter({ month: event.target.value || null })}>
            <option value="">Current month</option>
            {data.MonthlyTrend.map((month) => <option key={month.Key} value={month.FromDate}>{formatMonth(month.Key)}</option>)}
          </select>
        )}
        <span className="text-[12.5px] text-ink-soft">{formatDateOnly(data.FromDate)} – {formatDateOnly(data.ToDate)}</span>
        <span className="text-[12px] text-ink-faint">Sets the headline spend figure and the detail rows. Only these periods, or one of the last twelve months, can be chosen.</span>
      </div>

      {noActivity && (
        <StateNotice kind="empty" title="No accepted purchase bills in these periods">
          You are permitted to see spending, and there is no bill acceptance or reversal activity.
        </StateNotice>
      )}

      <div className="grid gap-3 mb-3" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))' }}>
        {data.Periods.map((period) => {
          const selected = data.Filters.Period === period.Key && (period.Key !== 'month' || data.FromDate === period.FromDate)
          return (
            <button key={period.Key} type="button" className="home-tile text-left cursor-pointer"
              style={{ outline: selected ? '2px solid var(--color-accent)' : undefined }}
              onClick={() => onFilter({ period: period.Key as PurchaseSpendingPeriodKey, month: null })}
              title="Use this period for the detail rows">
              <div className="home-tile-title">{SPENDING_PERIOD_LABEL[period.Key] ?? period.Key}</div>
              <div className="text-[12px] text-ink-faint">{formatDateOnly(period.FromDate)} – {formatDateOnly(period.ToDate)}</div>
              <SpendingAmounts amounts={period.Amounts} />
            </button>
          )
        })}
      </div>

      {!noActivity && (
        <div className="rounded-lg border border-line p-3 mb-3">
          <div className="flex flex-wrap items-baseline justify-between gap-2 mb-2">
            <h3 className="text-[13px] font-semibold">Monthly spend, last twelve months ({SPENDING_GST_NOTE})</h3>
            <span className="text-[11.5px] text-ink-faint">Click a month to list its bill events below. Red = net reversal.</span>
          </div>
          {trendCurrencies.length === 0 ? (
            <p className="text-[12.5px] text-ink-faint">No bill activity in the last twelve months.</p>
          ) : (
            <div className="grid gap-4">
              {trendCurrencies.map((currency) => (
                <div key={currency}>
                  {trendCurrencies.length > 1 && <div className="text-[12.5px] font-semibold text-ink-soft mb-1 mono">{currency}</div>}
                  <ColumnChart
                    currency={currency}
                    caption={`Monthly spend in ${currency}, including GST, oldest month first`}
                    clickHint="Show this month's bill events"
                    columns={data.MonthlyTrend.map((month) => {
                      const amount = month.Amounts.find((entry) => entry.Currency === currency)
                      return {
                        key: month.Key,
                        label: formatMonth(month.Key).replace(' 20', " '"),
                        value: amount?.Amount ?? 0,
                        displayText: amount ? `${formatAmount(amount.Amount, currency)} ${SPENDING_GST_NOTE}, ${countOf(amount.BillCount, 'bill')}` : 'no bill activity',
                        selected: isSelectedMonth(month.FromDate),
                        onClick: () => onFilter({ period: 'month', month: month.FromDate }),
                      }
                    })}
                  />
                </div>
              ))}
            </div>
          )}
          <details className="mt-3">
            <summary className="cursor-pointer text-[13px] font-medium text-accent">Monthly trend as a table</summary>
            <div className="table-wrap mt-2">
              <table className="table">
                <thead>
                  <tr>
                    <th>Month</th>
                    <th>Amount by currency ({SPENDING_GST_NOTE})</th>
                    <th className="text-right">POs · bills</th>
                  </tr>
                </thead>
                <tbody>
                  {data.MonthlyTrend.map((month) => (
                    <tr key={month.Key} className={`row-click ${isSelectedMonth(month.FromDate) ? 'row-selected' : ''}`}
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
                  ))}
                </tbody>
              </table>
            </div>
          </details>
        </div>
      )}

      {!noActivity && (
        <div className="grid gap-4 mb-3" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(380px, 1fr))' }}>
          <GroupChart title="Top vendors this financial year (top ten per currency)" groups={data.TopVendors} kind="vendor" can={can}
            selectedId={filters.vendor?.id ?? null}
            onPick={(group) => onFilter({ vendor: { id: group.Id, label: group.Code }, currency: group.Currency })} />
          <GroupChart title="Categories this financial year" groups={data.Categories} kind="category" can={can}
            selectedId={filters.category?.id ?? null}
            onPick={(group) => onFilter({ category: { id: group.Id, label: group.Code }, currency: group.Currency })} />
        </div>
      )}

      <DetailFilterNote active={activeFilters} onClear={() => onClear()} />

      {data.Rows.length === 0 ? (
        !noActivity && <StateNotice kind="empty" title="No bill events match this selection" />
      ) : (
        <>
          <QuickFilter id="spending-search" value={search} onChange={setSearch} shown={rows.length} onPage={data.Rows.length}
            placeholder="Search bill, PO, vendor, category, item…" />
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <SortableHeader label="Decision date" sortKey="date" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Bill" sortKey="bill" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Event" sortKey="event" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="PO" sortKey="po" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Vendor" sortKey="vendor" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Category · item" sortKey="category" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Quantity" sortKey="quantity" sort={sort} onSort={toggleSort} />
                  <th className="text-right">Billed payable (incl. GST)</th>
                  <th className="text-right">Allocated charges</th>
                  <SortableHeader label="Amount (incl. GST)" sortKey="amount" sort={sort} onSort={toggleSort} />
                </tr>
              </thead>
              <tbody>
                {rows.length === 0 && (
                  <tr><td colSpan={10} className="table-empty">No rows on this page match "{search}".</td></tr>
                )}
                {/* One bill line can be accepted, reversed and accepted again on the same
                    date, so no field combination is unique; the server's order is stable. */}
                {rows.map((row, index) => (
                  <tr key={`${row.BillLineId}-${row.Event}-${index}`}>
                    <td className="whitespace-nowrap">{formatDateOnly(row.EventDate)}</td>
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
                    <td className="text-right font-semibold"><Money value={row.Amount} currency={row.Currency} /></td>
                  </tr>
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

function SpendingAmounts({ amounts }: { amounts: PurchaseSpendingAmount[] }) {
  if (amounts.length === 0) return <div className="mt-1"><NullValue reason="none" /></div>
  return (
    <div className="mt-1">
      {amounts.map((amount) => (
        <div key={amount.Currency} className="mb-1">
          <div className="text-xl font-semibold">
            <Money value={amount.Amount} currency={amount.Currency} />{' '}
            <span className="text-[11px] font-medium text-amber-700">{SPENDING_GST_NOTE}</span>
          </div>
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

/** Vendor or category spend as bars, one chart per currency, each bar filtering the detail rows. */
function GroupChart({ title, groups, kind, can, selectedId, onPick }: {
  title: string
  groups: PurchaseSpendingGroup[]
  kind: 'vendor' | 'category'
  can: Can
  selectedId: string | null
  onPick: (group: PurchaseSpendingGroup) => void
}) {
  const byCurrency = new Map<string, PurchaseSpendingGroup[]>()
  for (const group of groups) byCurrency.set(group.Currency, [...(byCurrency.get(group.Currency) ?? []), group])

  return (
    <div className="rounded-lg border border-line p-3">
      <h3 className="text-[13px] font-semibold mb-2">{title} <span className="font-normal text-amber-700">({SPENDING_GST_NOTE})</span></h3>
      {groups.length === 0 ? (
        <p><NullValue reason="none" /></p>
      ) : (
        [...byCurrency.entries()].map(([currency, list]) => {
          const bars: HBar[] = [...list].sort((a, b) => b.Amount - a.Amount).map((group) => {
            const vendorTo = kind === 'vendor' ? vendorLink(can, group.Code) : null
            return {
              key: `${group.Id}-${group.Currency}`,
              label: kind === 'vendor'
                ? <><span className="mono">{group.Code}</span> <span className="text-ink-soft">{group.Name}</span></>
                : <span className="mono">{group.Code}</span>,
              labelText: kind === 'vendor' ? `${group.Code} ${group.Name}` : group.Code,
              value: group.Amount,
              display: <Money value={group.Amount} currency={group.Currency} />,
              displayText: `${formatAmount(group.Amount, group.Currency)} ${SPENDING_GST_NOTE}, ${countOf(group.PoCount, 'PO')}, ${countOf(group.BillCount, 'bill')}`,
              note: <>{countOf(group.PoCount, 'PO')} · {countOf(group.BillCount, 'bill')}{vendorTo && <> · <MaybeLink to={vendorTo}>open vendor</MaybeLink></>}</>,
              onClick: () => onPick(group),
              clickHint: 'Show only these in the detail rows',
              selected: selectedId === group.Id,
            }
          })
          return (
            <div key={currency} className="mb-2">
              {byCurrency.size > 1 && <div className="text-[12.5px] font-semibold text-ink-soft mb-1 mono">{currency}</div>}
              <HBarChart bars={bars} caption={`${title} in ${currency}`} />
            </div>
          )
        })
      )}
    </div>
  )
}
