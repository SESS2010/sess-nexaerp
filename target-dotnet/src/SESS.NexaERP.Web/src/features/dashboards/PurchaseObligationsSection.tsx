import { useMemo, useState } from 'react'
import { getPurchaseObligations } from '../../api/dashboards'
import { SortableHeader } from '../../components/SortableHeader'
import { useSort } from '../../hooks/useSort'
import type { PurchaseObligationQueue, PurchaseObligationRow, PurchaseObligationsPage, PurchaseObligationVendor } from '../../types/dashboard'
import { countOf, formatAge, formatAmount, formatCount, formatDateOnly, formatQuantity } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { goodsReceiptLink, itemLink, poLink, vendorLink } from './dashboardAccess'
import {
  BasisNote, DetailFilterNote, MaybeLink, Money, NullValue, Pager, QueryProblem, SectionFrame, StateNotice,
} from './DashboardParts'
import { HBarChart, LegendDot, type HBar } from './PurchaseCharts'
import {
  AgeText, OBLIGATION_AGE, QuickFilter, RefreshingHint, SERVER_ORDER, SEVERITY, SectionSkeleton, ageSeverity, matchesText,
  sortRows, thresholdText, useFocusRequest, usePurchaseSectionQuery, useReportToPage, type FocusRequest, type SectionReport,
} from './PurchaseDashboardKit'

const PAGE_SIZE = 50
/** Vendors drawn per chart; the full list stays in the table below it. */
const CHART_VENDORS = 8

type Can = (pageKey: string, action?: string) => boolean

/** What each obligation figure is NOT, in plain words, beside the server's Basis. */
const QUEUE_CAUTION: Record<string, string> = {
  grni: 'Provisional receipt value at the quoted unit rate before GST and added charges. It does not net discounts or later bill adjustments, and it is not landed or Actual BOM cost.',
  'vendor-advances': 'Cash paid to vendors ahead of billing. These are not ex-tax purchase costs.',
}

interface Filters {
  queue: PurchaseObligationQueue | null
  vendor: { id: string; label: string } | null
  currency: string | null
  document: { id: string; label: string } | null
}

const NO_FILTERS: Filters = { queue: null, vendor: null, currency: null, document: null }

export function PurchaseObligationsSection({ refreshTick = 0, onReport, focus }: {
  refreshTick?: number
  onReport?: (report: SectionReport<PurchaseObligationsPage>) => void
  focus?: FocusRequest | null
}) {
  const { can } = useSession()
  const [filters, setFilters] = useState<Filters>(NO_FILTERS)
  const [page, setPage] = useState(1)

  const { state, data, refreshing, reload } = usePurchaseSectionQuery(
    () => getPurchaseObligations({
      queue: filters.queue,
      vendorId: filters.vendor?.id,
      currency: filters.currency,
      documentId: filters.document?.id,
      page,
      pageSize: PAGE_SIZE,
    }),
    `${filters.queue}|${filters.vendor?.id}|${filters.currency}|${filters.document?.id}|${page}`,
    refreshTick,
  )
  const unfiltered = !filters.queue && !filters.vendor && !filters.currency && !filters.document && page === 1
  useReportToPage(onReport, state, data, unfiltered)

  const apply = (next: Partial<Filters>) => {
    setFilters((current) => ({ ...current, ...next }))
    setPage(1)
  }

  useFocusRequest(focus, (target) => {
    setFilters(target === 'grni' || target === 'vendor-advances' ? { ...NO_FILTERS, queue: target } : NO_FILTERS)
    setPage(1)
  })

  return (
    <SectionFrame
      id="purchase-obligations"
      title="Obligations"
      subtitle={<>Goods received but not billed, and vendor advances still outstanding. <RefreshingHint on={refreshing} /></>}
      generatedAt={data?.GeneratedAt}
      timeZone={data?.TimeZone}
    >
      {!data && state.kind === 'loading' && (
        <>
          <StateNotice kind="loading" title="Loading obligations…" />
          <SectionSkeleton tiles={2} />
        </>
      )}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <QueryProblem state={state} onRetry={reload} />}
      {data && <ObligationsBody data={data} can={can} onFilter={apply} onClear={() => apply(NO_FILTERS)} filters={filters} onPage={setPage} />}
    </SectionFrame>
  )
}

function ObligationsBody({ data, can, filters, onFilter, onClear, onPage }: {
  data: PurchaseObligationsPage
  can: Can
  filters: Filters
  onFilter: (next: Partial<Filters>) => void
  onClear: () => void
  onPage: (page: number) => void
}) {
  const [search, setSearch] = useState('')
  const { sort, toggleSort } = useSort(SERVER_ORDER)
  const nothingOutstanding = data.Tiles.every((tile) => tile.Count === 0) && data.TotalRows === 0
  const titleOf = (key: string) => data.Tiles.find((tile) => tile.Key === key)?.Title ?? key

  const activeFilters: string[] = []
  if (data.Filters.Queue) activeFilters.push(titleOf(data.Filters.Queue))
  if (filters.vendor) activeFilters.push(`vendor ${filters.vendor.label}`)
  if (data.Filters.Currency) activeFilters.push(`currency ${data.Filters.Currency}`)
  if (filters.document) activeFilters.push(`document ${filters.document.label}`)

  // One chart per kind and currency: GRNI and advances are different measures, and currencies are never added.
  const vendorCharts = useMemo(() => {
    const groups = new Map<string, { queue: string; currency: string; vendors: PurchaseObligationVendor[] }>()
    for (const vendor of data.Vendors) {
      const key = `${vendor.Queue}|${vendor.Currency}`
      const group = groups.get(key) ?? { queue: vendor.Queue, currency: vendor.Currency, vendors: [] }
      group.vendors.push(vendor)
      groups.set(key, group)
    }
    return [...groups.entries()].map(([key, group]) => ({
      key,
      ...group,
      vendors: [...group.vendors].sort((a, b) => b.Value - a.Value),
    }))
  }, [data.Vendors])

  const rows = useMemo(() => {
    const filtered = data.Rows.filter((row) => matchesText(search, [
      row.DocumentNumber, row.PoNumber, row.VendorCode, row.VendorName, row.ItemCode, row.ItemName, titleOf(row.Queue), row.Currency,
    ]))
    return sortRows<PurchaseObligationRow>(filtered, sort, {
      document: (row) => row.DocumentNumber,
      po: (row) => row.PoNumber,
      vendor: (row) => row.VendorCode,
      item: (row) => row.ItemCode,
      date: (row) => row.SourceDate,
      age: (row) => row.AgeDays,
      value: (row) => row.Value,
    })
  }, [data.Rows, data.Tiles, search, sort])

  return (
    <>
      {nothingOutstanding && (
        <StateNotice kind="empty" title="No outstanding obligations">
          You are permitted to see obligations, and nothing is outstanding.
        </StateNotice>
      )}

      <div className="grid gap-3 my-3" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))' }}>
        {data.Tiles.map((tile) => {
          const selected = data.Filters.Queue === tile.Key
          const severity = tile.Count > 0 ? ageSeverity(tile.OldestAgeDays, OBLIGATION_AGE) : null
          return (
            <button key={tile.Key} type="button" className="home-tile text-left cursor-pointer"
              style={{
                outline: selected ? '2px solid var(--color-accent)' : undefined,
                borderLeft: severity ? `4px solid ${SEVERITY[severity].bar}` : undefined,
              }}
              onClick={() => onFilter({ queue: selected ? null : (tile.Key as PurchaseObligationQueue) })}
              title={selected ? 'Show all obligations' : 'Show only this kind in the detail rows'}>
              <div className="home-tile-title">{tile.Title}</div>
              <div className="text-2xl font-semibold"><span className="mono">{formatCount(tile.Count)}</span> <span className="text-sm font-normal">{tile.Count === 1 ? 'document' : 'documents'}</span></div>
              <div className="text-[12px] text-ink-soft">{tile.OldestAgeDays === null ? 'No age' : <>Oldest <AgeText days={tile.OldestAgeDays} thresholds={OBLIGATION_AGE} /></>}</div>
              <div className="mt-1">
                {tile.Amounts.length === 0 ? (
                  <NullValue reason="none" />
                ) : (
                  tile.Amounts.map((amount) => (
                    <div key={amount.Currency}>
                      <span className="text-lg font-semibold"><Money value={amount.Value} currency={amount.Currency} /></span>
                      <span className="text-[12px] text-ink-faint"> · {countOf(amount.DocumentCount, 'doc')}, {countOf(amount.LineCount, 'line')}</span>
                    </div>
                  ))
                )}
              </div>
              <div className="field-hint mt-2"><strong>Basis:</strong> {tile.Basis}</div>
              {QUEUE_CAUTION[tile.Key] && <div className="field-hint">{QUEUE_CAUTION[tile.Key]}</div>}
            </button>
          )
        })}
      </div>

      {vendorCharts.length > 0 && (
        <div className="rounded-lg border border-line p-3 mb-3">
          <div className="flex flex-wrap items-baseline justify-between gap-2 mb-2">
            <h3 className="text-[13px] font-semibold">Outstanding by vendor</h3>
            <span className="flex flex-wrap gap-2">
              <LegendDot color={SEVERITY.green.bar}>recent</LegendDot>
              <LegendDot color={SEVERITY.amber.bar}>ageing</LegendDot>
              <LegendDot color={SEVERITY.red.bar}>old</LegendDot>
            </span>
          </div>
          <div className="grid gap-4" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(380px, 1fr))' }}>
            {vendorCharts.map((chart) => {
              const bars: HBar[] = chart.vendors.slice(0, CHART_VENDORS).map((vendor) => {
                const severity = ageSeverity(vendor.OldestAgeDays, OBLIGATION_AGE)
                const selected = filters.vendor?.id === vendor.VendorId && data.Filters.Queue === vendor.Queue && data.Filters.Currency === vendor.Currency
                return {
                  key: vendor.VendorId,
                  label: <><span className="mono">{vendor.VendorCode}</span> <span className="text-ink-soft">{vendor.VendorName}</span></>,
                  labelText: `${vendor.VendorCode} ${vendor.VendorName}`,
                  value: vendor.Value,
                  display: <Money value={vendor.Value} currency={vendor.Currency} />,
                  displayText: `${formatAmount(vendor.Value, vendor.Currency)}, ${countOf(vendor.DocumentCount, 'document')}${vendor.OldestAgeDays !== null ? `, oldest ${formatAge(vendor.OldestAgeDays)}` : ''}`,
                  color: SEVERITY[severity].bar,
                  note: <>{countOf(vendor.DocumentCount, 'document')}{vendor.OldestAgeDays !== null && <> · oldest <AgeText days={vendor.OldestAgeDays} thresholds={OBLIGATION_AGE} /></>}</>,
                  onClick: () => onFilter({ queue: vendor.Queue as PurchaseObligationQueue, vendor: { id: vendor.VendorId, label: vendor.VendorCode }, currency: vendor.Currency }),
                  clickHint: 'Show only this vendor, kind and currency in the detail rows',
                  selected,
                }
              })
              return (
                <div key={chart.key}>
                  <div className="text-[12.5px] font-semibold text-ink-soft mb-1">
                    {titleOf(chart.queue)} · <span className="mono">{chart.currency}</span>
                    {chart.queue === 'grni' && <span className="font-normal"> (before GST)</span>}
                  </div>
                  <HBarChart bars={bars} caption={`${titleOf(chart.queue)} in ${chart.currency}, by vendor`} />
                  {chart.vendors.length > CHART_VENDORS && (
                    <p className="text-[11.5px] text-ink-faint mt-1">Largest {CHART_VENDORS} of {chart.vendors.length} vendors; all are in the table below.</p>
                  )}
                </div>
              )
            })}
          </div>
          <p className="text-[11.5px] text-ink-faint mt-2">Colour follows each vendor's oldest outstanding document ({thresholdText(OBLIGATION_AGE)}). The API gives no due dates for these, so they are shown by age, not by due date.</p>

          <details className="mt-3">
            <summary className="cursor-pointer text-[13px] font-medium text-accent">All vendors as a table</summary>
            <div className="table-wrap mt-2">
              <table className="table">
                <thead>
                  <tr>
                    <th>Kind</th>
                    <th>Vendor</th>
                    <th>Currency</th>
                    <th className="text-right">Documents</th>
                    <th className="text-right">Value</th>
                    <th className="text-right">Oldest</th>
                  </tr>
                </thead>
                <tbody>
                  {data.Vendors.map((group) => (
                    <tr key={`${group.Queue}-${group.VendorId}-${group.Currency}`} className="row-click"
                      title="Show only this vendor, kind and currency in the detail rows"
                      onClick={() => onFilter({ queue: group.Queue as PurchaseObligationQueue, vendor: { id: group.VendorId, label: group.VendorCode }, currency: group.Currency })}>
                      <td>{titleOf(group.Queue)}</td>
                      <td>
                        <MaybeLink to={vendorLink(can, group.VendorCode)}><span className="mono">{group.VendorCode}</span></MaybeLink>
                        <div className="field-hint">{group.VendorName}</div>
                      </td>
                      <td className="mono">{group.Currency}</td>
                      <td className="text-right">{formatCount(group.DocumentCount)}</td>
                      <td className="text-right"><Money value={group.Value} currency={group.Currency} /></td>
                      <td className="text-right"><AgeText days={group.OldestAgeDays} thresholds={OBLIGATION_AGE} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </details>
        </div>
      )}

      <DetailFilterNote active={activeFilters} onClear={onClear} />

      {data.Rows.length === 0 ? (
        !nothingOutstanding && <StateNotice kind="empty" title="No detail rows match this selection" />
      ) : (
        <>
          <BasisNote>Each row's value follows the basis of its kind above; GRNI and advances are different measures and are never added together.</BasisNote>
          <QuickFilter id="obligations-search" value={search} onChange={setSearch} shown={rows.length} onPage={data.Rows.length}
            placeholder="Search GRN/advance, PO, vendor, item…" />
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <SortableHeader label="Document" sortKey="document" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="PO" sortKey="po" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Vendor" sortKey="vendor" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Item" sortKey="item" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Date" sortKey="date" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Age" sortKey="age" sort={sort} onSort={toggleSort} />
                  <th className="text-right">Quantity / rate</th>
                  <SortableHeader label="Outstanding" sortKey="value" sort={sort} onSort={toggleSort} />
                </tr>
              </thead>
              <tbody>
                {rows.length === 0 && (
                  <tr><td colSpan={8} className="table-empty">No rows on this page match "{search}".</td></tr>
                )}
                {rows.map((row) => {
                  const isGrni = row.Queue === 'grni'
                  return (
                    <tr key={`${row.Queue}-${row.DocumentId}-${row.LineId ?? 'advance'}`}>
                      <td className="mono">
                        {isGrni ? (
                          <MaybeLink to={goodsReceiptLink(can, row.DocumentId)}>{row.DocumentNumber}</MaybeLink>
                        ) : (
                          <button type="button" className="link-button mono" title="Show only this advance"
                            onClick={() => onFilter({ document: { id: row.DocumentId, label: row.DocumentNumber } })}>
                            {row.DocumentNumber}
                          </button>
                        )}
                        <div className="field-hint">{titleOf(row.Queue)}</div>
                      </td>
                      <td className="mono"><MaybeLink to={poLink(can, row.PoNumber)}>{row.PoNumber}</MaybeLink></td>
                      <td>
                        <MaybeLink to={vendorLink(can, row.VendorCode)}><span className="mono">{row.VendorCode}</span></MaybeLink>
                        <div className="field-hint">{row.VendorName}</div>
                      </td>
                      <td>
                        {row.ItemCode ? (
                          <>
                            <MaybeLink to={itemLink(can, row.ItemCode)}><span className="mono">{row.ItemCode}</span></MaybeLink>
                            <div className="field-hint">{row.ItemName}</div>
                          </>
                        ) : (
                          <span className="field-hint">Advance, no item line</span>
                        )}
                      </td>
                      <td className="whitespace-nowrap">{formatDateOnly(row.SourceDate)}<div className="field-hint">{isGrni ? 'received' : 'paid'}</div></td>
                      <td className="text-right"><AgeText days={row.AgeDays} thresholds={OBLIGATION_AGE} /></td>
                      <td className="text-right">
                        {isGrni ? (
                          <>
                            <span className="mono">{row.Quantity === null ? '' : `${formatQuantity(row.Quantity)} ${row.Uom ?? ''}`}</span>
                            <div className="field-hint">at <Money value={row.UnitRate} currency={row.Currency} nullReason="none" /> before GST</div>
                          </>
                        ) : (
                          <span className="field-hint">
                            paid <Money value={row.OriginalAmount} currency={row.Currency} nullReason="none" />
                            {' '}less adjusted <Money value={row.AdjustedAmount} currency={row.Currency} nullReason="none" />
                          </span>
                        )}
                      </td>
                      <td className="text-right font-semibold"><Money value={row.Value} currency={row.Currency} /></td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </>
      )}
      <Pager page={data.Filters.Page} pageSize={data.Filters.PageSize} totalRows={data.TotalRows} onPage={onPage} />
    </>
  )
}
