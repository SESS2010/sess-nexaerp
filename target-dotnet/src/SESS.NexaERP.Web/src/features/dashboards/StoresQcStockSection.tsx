import { useEffect } from 'react'
import { Filter, ShieldCheck } from 'lucide-react'
import { getStoresQcStock } from '../../api/dashboards'
import type { StoresQcStockPage, StoresQcStockQueue, StoresQcStockRow } from '../../types/dashboard'
import { formatQuantity, formatTimestamp } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { goodsReceiptLink, itemLink } from './dashboardAccess'
import { DimensionId, MaybeLink, Money } from './DashboardParts'
import { AgeChip, CountBadge, InfoTip, Panel } from './DashboardUi'
import {
  COLOUR_RULES, CompactPager, CompactProblem, FilterChips, OneLine, QC_SHORT, SearchBox, SkeletonRows, SortTh, TD, TH,
  qcRowTone, scrollToId, useLocalRows, useStoresSectionQuery,
} from './StoresDashboardKit'
import type { Reveal, SectionReport } from './StoresDashboardKit'

export const STORES_QC_PAGE_SIZE = 100
export const STORES_QC_PANEL_ID = 'stores-qc-stock'

export interface QcFilter {
  queue: StoresQcStockQueue | null
  grn: { id: string; label: string } | null
  page: number
}

export const EMPTY_QC_FILTER: QcFilter = { queue: null, grn: null, page: 1 }

type QcSortKey = 'grn' | 'kind' | 'item' | 'qty' | 'due' | 'age' | 'value'

const SPLIT_NOTE =
  'One GRN line can appear on several rows when its stock is split by warehouse, rack, ownership, custody, provenance or serial. The cards count GRN lines, not rows.'

/**
 * QC and held stock detail, folded by default. QC rows link to the GRN only
 * (contract); the query lives outside the Panel body so the overview keeps
 * its figures while the table is closed.
 */
export function StoresQcStockSection({ filter, onFilter, refreshTick, onReport, reveal }: {
  filter: QcFilter
  onFilter: (next: QcFilter) => void
  refreshTick: number
  onReport: (report: SectionReport<StoresQcStockPage>) => void
  reveal: Reveal
}) {
  const { can } = useSession()
  const { state, reload, data, refreshing, report } = useStoresSectionQuery(
    () => getStoresQcStock({ queue: filter.queue, documentId: filter.grn?.id, page: filter.page, pageSize: STORES_QC_PAGE_SIZE }),
    `${filter.queue}|${filter.grn?.id}|${filter.page}`,
    refreshTick,
  )

  useEffect(() => {
    onReport(report)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [report.kind, report.data, report.refreshing, report.receivedAt, onReport])

  const kindOf = (queue: string) => QC_SHORT[queue] ?? queue
  const { shown, sort, setSort, toggle, query, setQuery } = useLocalRows<StoresQcStockRow, QcSortKey>(
    data?.Rows ?? [],
    {
      grn: (row) => row.DocumentNumber,
      kind: (row) => (row.IsOverdue ? '0' : '1') + kindOf(row.Queue),
      item: (row) => row.ItemCode,
      qty: (row) => row.Quantity,
      due: (row) => row.QcDueAt,
      age: (row) => row.ReceiptAgeDays,
      // Orders the shown rows only; amounts are never added or converted.
      value: (row) => row.ReceiptProvisionalValue,
    },
    (row) => [row.DocumentNumber, row.ItemCode, row.ItemName, row.Uom, kindOf(row.Queue), row.IsOverdue ? 'overdue' : ''].join(' '),
  )

  useEffect(() => {
    if (reveal.n === 0) return
    setQuery(reveal.search ?? '')
    setSort(reveal.overdueFirst ? { key: 'kind', dir: 'asc' } : null)
    scrollToId(STORES_QC_PANEL_ID)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [reveal.n])

  const valueReason = data?.CanViewCommercialValues ? 'none' : 'withheld'
  const info = [
    'GRN-received stock held for QC or pending a returnable DC. Not all stock, and not opening stock.',
    data ? `Value basis: ${data.ValueBasis} Provisional receipt value before taxes and charges — not Actual BOM cost, no paid/unpaid information. Currencies are never added together.` : '',
    data && !data.CanViewCommercialValues ? 'Values are withheld from your role (not zero); they need the commercial permission on both the dashboard and the GRN page.' : '',
    SPLIT_NOTE,
    'Overdue is the server’s verdict against each line’s QC due time. Search and sorting apply to the rows on this page only.',
    data ? `Server time ${formatTimestamp(data.GeneratedAt, data.TimeZone)} (${data.TimeZone}).` : '',
    COLOUR_RULES,
  ].filter(Boolean).join(' ')

  const activeFilters: string[] = []
  if (data?.Filters.Queue) activeFilters.push(kindOf(data.Filters.Queue))
  if (filter.grn) activeFilters.push(filter.grn.label)
  if (reveal.overdueFirst && sort?.key === 'kind') activeFilters.push('overdue first')

  return (
    <Panel key={reveal.n} id={STORES_QC_PANEL_ID} icon={ShieldCheck} title="QC & held stock details" info={info}
      collapsible defaultOpen={reveal.n > 0}
      right={<>
        {refreshing && <span className="text-[11px] text-slate-400" role="status">Refreshing…</span>}
        {data && <SearchBox value={query} onChange={setQuery} placeholder="Find GRN, item…" />}
        {data && <CountBadge n={data.TotalRows} />}
      </>}>
      {state.kind === 'loading' && !data && <SkeletonRows label="QC and held stock" rows={4} />}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <CompactProblem state={state} onRetry={reload} />}
      {data && (
        <>
          <FilterChips active={activeFilters} onClear={() => { setSort(null); onFilter(EMPTY_QC_FILTER) }} />
          {data.Rows.length === 0 ? (
            <div data-dashboard-state="empty"><OneLine tone="ok">{activeFilters.length ? 'No rows match this filter.' : 'No stock is held for QC or a returnable DC.'}</OneLine></div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr>
                    <SortTh label="GRN" sortKey="grn" sort={sort} onSort={toggle} />
                    <SortTh label="Kind" sortKey="kind" sort={sort} onSort={toggle} />
                    <SortTh label="Item" sortKey="item" sort={sort} onSort={toggle} />
                    <SortTh label="Qty" sortKey="qty" sort={sort} onSort={toggle} align="right" />
                    <th className={TH}>Where</th>
                    <SortTh label="QC due" sortKey="due" sort={sort} onSort={toggle} />
                    <SortTh label="Age" sortKey="age" sort={sort} onSort={toggle} align="right" />
                    <SortTh label="Value" sortKey="value" sort={sort} onSort={toggle} align="right" />
                  </tr>
                </thead>
                <tbody>
                  {shown.length === 0 && (
                    <tr><td colSpan={8} className={`${TD} text-xs text-slate-400`}>No row on this page matches “{query}”.</td></tr>
                  )}
                  {shown.map((row) => (
                    <tr key={`${row.AllocationId}-${row.WarehouseId}-${row.RackBinId}-${row.OwnershipAccountId}-${row.CustodyAssignmentId}-${row.ProvenanceLayerId}-${row.SerialId}`}
                      className="hover:bg-slate-50">
                      <td className={`${TD} whitespace-nowrap`}>
                        {/* The contract links QC allocation rows to the GRN only, never an invented action page. */}
                        <span className="mono"><MaybeLink to={goodsReceiptLink(can, row.DocumentId)}>{row.DocumentNumber}</MaybeLink></span>
                        <button type="button" title="Show only this GRN" aria-label={`Show only ${row.DocumentNumber}`}
                          className="ml-1.5 inline-flex align-middle text-slate-300 hover:text-blue-600"
                          onClick={() => onFilter({ ...filter, grn: { id: row.DocumentId, label: row.DocumentNumber }, page: 1 })}>
                          <Filter size={12} aria-hidden />
                        </button>
                      </td>
                      <td className={`${TD} whitespace-nowrap text-xs`}>
                        {row.IsOverdue
                          ? <span className="rounded bg-rose-50 px-1.5 py-0.5 font-medium text-rose-700" title="Past its QC due time (server)">QC overdue</span>
                          : <span className="text-slate-600">{kindOf(row.Queue)}</span>}
                      </td>
                      <td className={`${TD} max-w-[16rem]`}>
                        <div className="truncate" title={`${row.ItemCode} — ${row.ItemName}`}>
                          <span className="mono"><MaybeLink to={itemLink(can, row.ItemCode)}>{row.ItemCode}</MaybeLink></span>
                          <span className="ml-1.5 text-xs text-slate-500">{row.ItemName}</span>
                        </div>
                      </td>
                      <td className={`${TD} whitespace-nowrap text-right tabular-nums`}>{formatQuantity(row.Quantity)} <span className="text-xs text-slate-500">{row.Uom}</span></td>
                      <td className={`${TD} whitespace-nowrap`}>
                        <span className="inline-flex items-center gap-1">
                          <DimensionId label="WH" id={row.WarehouseId} />
                          <InfoTip text={[
                            `Warehouse ID ${row.WarehouseId ?? '—'}`,
                            `Rack/bin ID ${row.RackBinId ?? '—'}`,
                            `Owner ID ${row.OwnershipAccountId}`,
                            `Custody ID ${row.CustodyAssignmentId}`,
                            `Provenance ID ${row.ProvenanceLayerId}`,
                            row.SerialId ? `Serial ID ${row.SerialId}` : '',
                          ].filter(Boolean).join(' · ')} />
                        </span>
                      </td>
                      <td className={`${TD} whitespace-nowrap text-xs text-slate-600`}>
                        {row.Queue === 'QC_HOLD'
                          ? formatTimestamp(row.QcDueAt, data.TimeZone)
                          : <span className="text-slate-400" title="Not subject to QC overdue">{formatTimestamp(row.QcDueAt, data.TimeZone)}</span>}
                      </td>
                      <td className={`${TD} text-right`}>
                        <span title={`Received ${formatTimestamp(row.ReceivedAt, data.TimeZone)}`}>
                          <AgeChip days={row.ReceiptAgeDays} tone={qcRowTone(row)} />
                        </span>
                      </td>
                      <td className={`${TD} whitespace-nowrap text-right`}>
                        <Money value={row.ReceiptProvisionalValue} currency={row.Currency} nullReason={valueReason} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <CompactPager page={data.Filters.Page} pageSize={data.Filters.PageSize} totalRows={data.TotalRows}
            shown={shown.length} loaded={data.Rows.length} searching={query.trim() !== ''}
            onPage={(page) => onFilter({ ...filter, page })} />
        </>
      )}
    </Panel>
  )
}
