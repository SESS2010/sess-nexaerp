import { useEffect } from 'react'
import { getStoresQcStock } from '../../api/dashboards'
import type { StoresQcStockPage, StoresQcStockQueue, StoresQcStockRow } from '../../types/dashboard'
import { formatQuantity, formatTimestamp } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { goodsReceiptLink, itemLink } from './dashboardAccess'
import {
  BasisNote, DetailFilterNote, DimensionId, MaybeLink, Money, Pager, QueryProblem, SectionFrame, StateNotice,
} from './DashboardParts'
import {
  AgeChip, RefreshingBadge, SectionSkeleton, SortTh, TableToolbar, ageTone, useLocalRows, useStoresSectionQuery,
} from './StoresDashboardKit'
import type { SectionReport } from './StoresDashboardKit'

export const STORES_QC_PAGE_SIZE = 100

export interface QcFilter {
  queue: StoresQcStockQueue | null
  grn: { id: string; label: string } | null
  page: number
}

export const EMPTY_QC_FILTER: QcFilter = { queue: null, grn: null, page: 1 }

/** The contract gives these cards a Key only; labels are ours. */
export const QC_QUEUE_LABEL: Record<string, { title: string; hint: string }> = {
  QC_HOLD: { title: 'Held for QC', hint: 'Received stock not yet cleared by QC. Overdue means past its recorded QC due time.' },
  PENDING_RETURNABLE_DC: { title: 'Pending returnable DC', hint: 'Stock awaiting a returnable delivery challan. This is never QC-overdue.' },
}

export function qcTone(row: StoresQcStockRow) {
  // IsOverdue is the server's verdict; never recomputed from the browser clock.
  return row.Queue === 'QC_HOLD' ? ageTone('qc-hold', row.ReceiptAgeDays, row.IsOverdue) : ageTone('pending-dc', row.ReceiptAgeDays)
}

/**
 * QC and held stock detail. The two overview cards are drawn at the top of
 * the page (StoresKpiTiles); filters live on the page.
 */
export function StoresQcStockSection({ filter, onFilter, refreshTick, onReport }: {
  filter: QcFilter
  onFilter: (next: QcFilter) => void
  refreshTick: number
  onReport: (report: SectionReport<StoresQcStockPage>) => void
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

  return (
    <SectionFrame
      id="stores-qc-stock"
      title="QC and held stock — detail"
      subtitle="Current GRN-received stock that is held for QC or pending a returnable DC. Not all stock, and not opening stock."
      generatedAt={data?.GeneratedAt}
      timeZone={data?.TimeZone}
    >
      {state.kind === 'loading' && !data && <SectionSkeleton label="Loading QC and held stock…" rows={4} />}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <QueryProblem state={state} onRetry={reload} />}
      {data && <QcStockBody data={data} can={can} filter={filter} onFilter={onFilter} refreshing={refreshing} />}
    </SectionFrame>
  )
}

type QcSortKey = 'grn' | 'kind' | 'item' | 'qty' | 'received' | 'due' | 'age' | 'value'

function QcStockBody({ data, can, filter, onFilter, refreshing }: {
  data: StoresQcStockPage
  can: (pageKey: string, action?: string) => boolean
  filter: QcFilter
  onFilter: (next: QcFilter) => void
  refreshing: boolean
}) {
  const nothingHeld = data.Tiles.every((tile) => tile.LineCount === 0) && data.TotalRows === 0
  const valueReason = data.CanViewCommercialValues ? 'none' : 'withheld'

  const activeFilters: string[] = []
  if (data.Filters.Queue) activeFilters.push(QC_QUEUE_LABEL[data.Filters.Queue]?.title ?? data.Filters.Queue)
  if (filter.grn) activeFilters.push(`GRN ${filter.grn.label}`)

  const { shown, sort, toggle, query, setQuery } = useLocalRows<StoresQcStockRow, QcSortKey>(
    data.Rows,
    {
      grn: (row) => row.DocumentNumber,
      kind: (row) => (row.IsOverdue ? '0' : '1') + (QC_QUEUE_LABEL[row.Queue]?.title ?? row.Queue),
      item: (row) => row.ItemCode,
      qty: (row) => row.Quantity,
      received: (row) => row.ReceivedAt,
      due: (row) => row.QcDueAt,
      age: (row) => row.ReceiptAgeDays,
      // Orders the shown rows only; amounts are never added or converted.
      value: (row) => row.ReceiptProvisionalValue,
    },
    (row) => [row.DocumentNumber, row.ItemCode, row.ItemName, row.Uom, QC_QUEUE_LABEL[row.Queue]?.title ?? row.Queue, row.IsOverdue ? 'overdue' : ''].join(' '),
  )

  return (
    <>
      <div className="mb-1 flex flex-wrap items-start justify-between gap-2">
        <BasisNote>
          {data.ValueBasis} This is a <strong>provisional receipt value before taxes and charges</strong>. It is not Actual BOM
          cost and carries no paid or unpaid information.
        </BasisNote>
        <RefreshingBadge on={refreshing} />
      </div>
      {!data.CanViewCommercialValues && (
        <StateNotice kind="denied" title="Values are withheld from your role">
          Quantities and ages are shown; receipt values are withheld, not zero. Values need the commercial permission on both
          the dashboard and the GRN page.
        </StateNotice>
      )}

      {nothingHeld && (
        <StateNotice kind="empty" title="No stock is held for QC or a returnable DC">
          You are permitted to see held stock, and none is held now.
        </StateNotice>
      )}

      <div className="mt-2">
        <DetailFilterNote active={activeFilters} onClear={() => onFilter({ queue: null, grn: null, page: 1 })} />
      </div>

      <div id="stores-qc-stock-detail" className="scroll-mt-4" />
      {data.Rows.length === 0 ? (
        !nothingHeld && <StateNotice kind="empty" title="No detail rows match this selection" />
      ) : (
        <>
          <p className="field-hint mb-2">
            One GRN line can appear on several rows when its stock is split by warehouse, rack, ownership, custody,
            provenance or serial. The cards above count GRN lines, not rows.
          </p>
          <TableToolbar query={query} onQuery={setQuery} placeholder="Find GRN, item code or name…"
            shown={shown.length} loaded={data.Rows.length} total={data.TotalRows} />
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <SortTh label="GRN" sortKey="grn" sort={sort} onSort={toggle} />
                  <SortTh label="Kind" sortKey="kind" sort={sort} onSort={toggle} />
                  <SortTh label="Item" sortKey="item" sort={sort} onSort={toggle} />
                  <SortTh label="Held quantity" sortKey="qty" sort={sort} onSort={toggle} align="right" />
                  <th>Where / whose</th>
                  <SortTh label="Received" sortKey="received" sort={sort} onSort={toggle} />
                  <SortTh label="QC due" sortKey="due" sort={sort} onSort={toggle} />
                  <SortTh label="Age" sortKey="age" sort={sort} onSort={toggle} align="right" />
                  <SortTh label="Provisional value" sortKey="value" sort={sort} onSort={toggle} align="right" />
                </tr>
              </thead>
              <tbody>
                {shown.length === 0 && (
                  <tr><td colSpan={9} className="field-hint">No row on this page matches “{query}”.</td></tr>
                )}
                {shown.map((row) => (
                  <tr key={`${row.AllocationId}-${row.WarehouseId}-${row.RackBinId}-${row.OwnershipAccountId}-${row.CustodyAssignmentId}-${row.ProvenanceLayerId}-${row.SerialId}`}>
                    <td className="mono whitespace-nowrap">
                      {/* The contract links QC allocation rows to the GRN only, never an invented action page. */}
                      <MaybeLink to={goodsReceiptLink(can, row.DocumentId)}>{row.DocumentNumber}</MaybeLink>
                      <div>
                        <button type="button" className="link-button text-[12px]"
                          onClick={() => onFilter({ ...filter, grn: { id: row.DocumentId, label: row.DocumentNumber }, page: 1 })}>
                          only this GRN
                        </button>
                      </div>
                    </td>
                    <td>
                      {QC_QUEUE_LABEL[row.Queue]?.title ?? row.Queue}
                      {row.IsOverdue && <div><span className="badge badge-error">QC overdue</span></div>}
                    </td>
                    <td>
                      <MaybeLink to={itemLink(can, row.ItemCode)}><span className="mono whitespace-nowrap">{row.ItemCode}</span></MaybeLink>
                      <div className="field-hint">{row.ItemName}</div>
                    </td>
                    <td className="text-right mono whitespace-nowrap">{formatQuantity(row.Quantity)} {row.Uom}</td>
                    <td>
                      <div className="flex flex-col">
                        <DimensionId label="Warehouse" id={row.WarehouseId} />
                        <DimensionId label="Rack/bin" id={row.RackBinId} />
                        <DimensionId label="Owner" id={row.OwnershipAccountId} />
                        <DimensionId label="Custody" id={row.CustodyAssignmentId} />
                        {row.SerialId && <DimensionId label="Serial" id={row.SerialId} />}
                      </div>
                    </td>
                    <td className="whitespace-nowrap">{formatTimestamp(row.ReceivedAt, data.TimeZone)}</td>
                    <td className="whitespace-nowrap">
                      {formatTimestamp(row.QcDueAt, data.TimeZone)}
                      {row.Queue !== 'QC_HOLD' && <div className="field-hint">Not subject to QC overdue</div>}
                    </td>
                    <td className="text-right">
                      <AgeChip days={row.ReceiptAgeDays} tone={qcTone(row)}
                        title={row.IsOverdue ? `Received ${row.ReceiptAgeDays} d ago; past its QC due time (server)` : `Received ${row.ReceiptAgeDays} d ago`} />
                    </td>
                    <td className="text-right"><Money value={row.ReceiptProvisionalValue} currency={row.Currency} nullReason={valueReason} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
      <Pager page={data.Filters.Page} pageSize={data.Filters.PageSize} totalRows={data.TotalRows} onPage={(page) => onFilter({ ...filter, page })} />
    </>
  )
}
