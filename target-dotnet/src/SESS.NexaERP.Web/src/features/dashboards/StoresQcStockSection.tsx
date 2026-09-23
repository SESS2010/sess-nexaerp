import { useState } from 'react'
import { getStoresQcStock } from '../../api/dashboards'
import type { StoresQcStockPage, StoresQcStockQueue, StoresQcStockTile } from '../../types/dashboard'
import { countOf, formatAge, formatQuantity, formatTimestamp } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { goodsReceiptLink, itemLink } from './dashboardAccess'
import {
  BasisNote, DetailFilterNote, DimensionId, MaybeLink, Money, NullValue, Pager, QueryProblem, SectionFrame, StateNotice,
} from './DashboardParts'
import { useDashboardQuery } from './useDashboardQuery'

const PAGE_SIZE = 50

/** The contract gives these cards a Key only; labels are ours. */
const QUEUE_LABEL: Record<string, { title: string; hint: string }> = {
  QC_HOLD: { title: 'Held for QC', hint: 'Received stock not yet cleared by QC. Overdue means past its recorded QC due time.' },
  PENDING_RETURNABLE_DC: { title: 'Pending returnable DC', hint: 'Stock awaiting a returnable delivery challan. This is never QC-overdue.' },
}

export function StoresQcStockSection() {
  const { can } = useSession()
  const [queue, setQueue] = useState<StoresQcStockQueue | null>(null)
  const [grn, setGrn] = useState<{ id: string; label: string } | null>(null)
  const [page, setPage] = useState(1)

  const { state, reload } = useDashboardQuery(
    () => getStoresQcStock({ queue, documentId: grn?.id, page, pageSize: PAGE_SIZE }),
    `${queue}|${grn?.id}|${page}`,
  )

  const data = state.kind === 'ready' ? state.data : null
  return (
    <SectionFrame
      id="stores-qc-stock"
      title="QC and held stock"
      subtitle="Current GRN-received stock that is held for QC or pending a returnable DC. Not all stock, and not opening stock."
      generatedAt={data?.GeneratedAt}
      timeZone={data?.TimeZone}
    >
      {state.kind === 'loading' && <StateNotice kind="loading" title="Loading QC and held stock…" />}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <QueryProblem state={state} onRetry={reload} />}
      {data && (
        <QcStockBody
          data={data}
          can={can}
          grn={grn}
          onQueue={(next) => { setQueue(next); setPage(1) }}
          onGrn={(next) => { setGrn(next); setPage(1) }}
          onClear={() => { setQueue(null); setGrn(null); setPage(1) }}
          onPage={setPage}
        />
      )}
    </SectionFrame>
  )
}

function QcStockBody({ data, can, grn, onQueue, onGrn, onClear, onPage }: {
  data: StoresQcStockPage
  can: (pageKey: string, action?: string) => boolean
  grn: { id: string; label: string } | null
  onQueue: (queue: StoresQcStockQueue | null) => void
  onGrn: (grn: { id: string; label: string } | null) => void
  onClear: () => void
  onPage: (page: number) => void
}) {
  const nothingHeld = data.Tiles.every((tile) => tile.LineCount === 0) && data.TotalRows === 0
  const valueReason = data.CanViewCommercialValues ? 'none' : 'withheld'

  const activeFilters: string[] = []
  if (data.Filters.Queue) activeFilters.push(QUEUE_LABEL[data.Filters.Queue]?.title ?? data.Filters.Queue)
  if (grn) activeFilters.push(`GRN ${grn.label}`)

  return (
    <>
      <BasisNote>
        {data.ValueBasis} This is a <strong>provisional receipt value before taxes and charges</strong>. It is not Actual BOM
        cost and carries no paid or unpaid information.
      </BasisNote>
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

      <div className="grid gap-3 my-3" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))' }}>
        {data.Tiles.map((tile) => (
          <QcTile key={tile.Key} tile={tile} selected={data.Filters.Queue === tile.Key} valueReason={valueReason}
            onSelect={() => onQueue(data.Filters.Queue === tile.Key ? null : (tile.Key as StoresQcStockQueue))} />
        ))}
      </div>

      <DetailFilterNote active={activeFilters} onClear={onClear} />

      {data.Rows.length === 0 ? (
        !nothingHeld && <StateNotice kind="empty" title="No detail rows match this selection" />
      ) : (
        <div className="table-wrap">
          <p className="field-hint mb-2">
            One GRN line can appear on several rows when its stock is split by warehouse, rack, ownership, custody,
            provenance or serial. The cards above count GRN lines, not rows.
          </p>
          <table className="table">
            <thead>
              <tr>
                <th>GRN</th>
                <th>Kind</th>
                <th>Item</th>
                <th className="text-right">Held quantity</th>
                <th>Where / whose</th>
                <th>Received · QC due</th>
                <th className="text-right">Age</th>
                <th className="text-right">Provisional value</th>
              </tr>
            </thead>
            <tbody>
              {data.Rows.map((row) => (
                <tr key={`${row.AllocationId}-${row.WarehouseId}-${row.RackBinId}-${row.OwnershipAccountId}-${row.CustodyAssignmentId}-${row.ProvenanceLayerId}-${row.SerialId}`}>
                  <td className="mono whitespace-nowrap">
                    <MaybeLink to={goodsReceiptLink(can, row.DocumentId)}>{row.DocumentNumber}</MaybeLink>
                    <div>
                      <button type="button" className="link-button" onClick={() => onGrn({ id: row.DocumentId, label: row.DocumentNumber })}>only this GRN</button>
                    </div>
                  </td>
                  <td>
                    {QUEUE_LABEL[row.Queue]?.title ?? row.Queue}
                    {/* IsOverdue is the server's verdict; never recomputed from the browser clock. */}
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
                  <td>
                    {formatTimestamp(row.ReceivedAt, data.TimeZone)}
                    <div className="field-hint">QC due {formatTimestamp(row.QcDueAt, data.TimeZone)}</div>
                  </td>
                  <td className="text-right">{formatAge(row.ReceiptAgeDays)}</td>
                  <td className="text-right"><Money value={row.ReceiptProvisionalValue} currency={row.Currency} nullReason={valueReason} /></td>
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

function QcTile({ tile, selected, valueReason, onSelect }: {
  tile: StoresQcStockTile
  selected: boolean
  valueReason: 'none' | 'withheld'
  onSelect: () => void
}) {
  const label = QUEUE_LABEL[tile.Key] ?? { title: tile.Key, hint: '' }
  return (
    <button type="button" className="home-tile text-left" onClick={onSelect}
      style={{ outline: selected ? '2px solid var(--color-accent)' : undefined }}
      title={selected ? 'Show all held stock' : 'Show only this kind in the detail rows'}>
      <div className="home-tile-title">{label.title}</div>
      <div className="text-2xl font-semibold">
        <span className="mono">{countOf(tile.LineCount, 'GRN line')}</span>
      </div>
      {tile.Key === 'QC_HOLD' ? (
        <div className={tile.OverdueLineCount > 0 ? 'text-red-700 text-sm' : 'field-hint'}>
          {countOf(tile.OverdueLineCount, 'line')} past QC due
        </div>
      ) : (
        <div className="field-hint">Not subject to QC overdue</div>
      )}
      <div className="field-hint">{tile.OldestReceiptAgeDays === null ? 'No age' : `Oldest received ${formatAge(tile.OldestReceiptAgeDays)} ago`}</div>
      <div className="mt-1">
        {tile.Values === null ? (
          <NullValue reason="withheld" />
        ) : tile.Values.length === 0 ? (
          <NullValue reason={valueReason} />
        ) : (
          tile.Values.map((value) => (
            <div key={value.Currency}><Money value={value.ReceiptProvisionalValue} currency={value.Currency} /></div>
          ))
        )}
      </div>
      {label.hint && <div className="field-hint mt-2">{label.hint}</div>}
    </button>
  )
}
