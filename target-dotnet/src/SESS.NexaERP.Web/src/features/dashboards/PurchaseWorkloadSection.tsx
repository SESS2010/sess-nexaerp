import { useState } from 'react'
import { getPurchaseWorkload } from '../../api/dashboards'
import type { PurchaseWorkloadPage, PurchaseWorkloadQueue, PurchaseWorkloadTile } from '../../types/dashboard'
import { formatAge, formatCount, formatTimestamp } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { workloadRowLink } from './dashboardAccess'
import {
  AmountList, DetailFilterNote, MaybeLink, Money, NullValue, Pager, QueryProblem, SectionFrame, StateNotice,
} from './DashboardParts'
import { useDashboardQuery } from './useDashboardQuery'

const PAGE_SIZE = 50

export function PurchaseWorkloadSection() {
  const { can } = useSession()
  const [queue, setQueue] = useState<PurchaseWorkloadQueue | null>(null)
  const [approvalRoute, setApprovalRoute] = useState<string | null>(null)
  const [page, setPage] = useState(1)

  const { state, reload } = useDashboardQuery(
    () => getPurchaseWorkload({ queue, approvalRoute, page, pageSize: PAGE_SIZE }),
    `${queue}|${approvalRoute}|${page}`,
  )

  const selectQueue = (next: PurchaseWorkloadQueue | null, route: string | null = null) => {
    setQueue(next)
    setApprovalRoute(route)
    setPage(1)
  }

  const data = state.kind === 'ready' ? state.data : null
  return (
    <SectionFrame
      id="purchase-workload"
      title="Workload"
      subtitle="Documents waiting in each purchase queue now. Counts are documents, not lines or vendors."
      generatedAt={data?.GeneratedAt}
      timeZone={data?.TimeZone}
    >
      {state.kind === 'loading' && <StateNotice kind="loading" title="Loading the purchase queues…" />}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <QueryProblem state={state} onRetry={reload} />}
      {data && <WorkloadBody data={data} can={can} onSelect={selectQueue} onPage={setPage} />}
    </SectionFrame>
  )
}

function WorkloadBody({ data, can, onSelect, onPage }: {
  data: PurchaseWorkloadPage
  can: (pageKey: string, action?: string) => boolean
  onSelect: (queue: PurchaseWorkloadQueue | null, route?: string | null) => void
  onPage: (page: number) => void
}) {
  const tilesByKey = new Map(data.Tiles.map((tile) => [tile.Key, tile]))
  const readyTiles = data.Tiles.filter((tile) => tile.State === 'READY')
  const deniedTiles = data.Tiles.filter((tile) => tile.State !== 'READY')
  const nothingWaiting = readyTiles.every((tile) => tile.Count === 0) && data.TotalRows === 0

  const activeFilters: string[] = []
  if (data.Queue) activeFilters.push(tilesByKey.get(data.Queue)?.Title ?? data.Queue)
  if (data.ApprovalRoute) activeFilters.push(`approval route ${data.ApprovalRoute}`)

  return (
    <>
      <p className="field-hint mb-2">
        <strong>Money on this section:</strong> requisition values are the saved estimated total in INR, not a uniform
        ex-tax cost. Quotation, comparison and PO values are the saved total payable, including GST and commercial terms.
        None of these figures is ex-tax or actual component cost.
      </p>

      {deniedTiles.length > 0 && (
        <StateNotice kind="denied" title={`${deniedTiles.length} queue${deniedTiles.length === 1 ? ' is' : 's are'} not available to you`}>
          Their counts are withheld, not zero: {deniedTiles.map((tile) => tile.Title).join('; ')}.
        </StateNotice>
      )}

      {nothingWaiting && (
        <StateNotice kind="empty" title="No purchase documents are waiting">
          You are permitted to see these queues, and none has a document in it now.
        </StateNotice>
      )}

      <div className="grid gap-3 my-3" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(230px, 1fr))' }}>
        {data.Tiles.map((tile) => (
          <WorkloadTile key={tile.Key} tile={tile} selected={data.Queue === tile.Key} selectedRoute={data.ApprovalRoute} onSelect={onSelect} />
        ))}
      </div>

      <DetailFilterNote active={activeFilters} onClear={() => onSelect(null)} />

      {data.Rows.length === 0 ? (
        !nothingWaiting && (
          <StateNotice kind="empty" title="No detail rows match this selection">
            {data.Queue ? 'The selected queue has no waiting documents on this page.' : 'There are no detail rows.'}
          </StateNotice>
        )
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Document</th>
                <th>Queue</th>
                <th>Status</th>
                <th>Waiting since</th>
                <th className="text-right">Age</th>
                <th>Next responsible</th>
                <th className="text-right">Value</th>
                <th>Vendors</th>
                <th className="text-right">Pending lines</th>
              </tr>
            </thead>
            <tbody>
              {data.Rows.map((row) => {
                const tile = tilesByKey.get(row.Queue)
                const valueWithheld = tile ? !tile.CommercialValuesVisible : row.Currency === null
                return (
                  <tr key={`${row.Queue}-${row.DocumentId}`}>
                    <td className="mono">
                      <MaybeLink to={workloadRowLink(can, row.Queue, row.DocumentNumber)}>{row.DocumentNumber}</MaybeLink>
                      <div className="field-hint">{row.DocumentType}</div>
                    </td>
                    <td>{tile?.Title ?? row.Queue}</td>
                    <td><span className="badge badge-muted">{row.Status}</span></td>
                    <td>{formatTimestamp(row.WaitingSince, data.TimeZone)}</td>
                    <td className="text-right">{formatAge(row.AgeDays)}</td>
                    <td>
                      {row.ResponsibilityIssue ? (
                        <span className="badge badge-warn" title="The saved workflow step does not name a single responsible employee.">
                          {row.ResponsibilityIssue}
                        </span>
                      ) : row.NextApproverEmployeeCode || row.NextApproverRole ? (
                        <>
                          <span className="mono">{row.NextApproverEmployeeCode ?? ''}</span>
                          {row.NextApproverRole && <div className="field-hint">{row.NextApproverRole}</div>}
                        </>
                      ) : (
                        <NullValue reason="none" />
                      )}
                    </td>
                    <td className="text-right">
                      <Money value={row.Value} currency={row.Currency} nullReason={valueWithheld ? 'withheld' : 'none'} />
                    </td>
                    <td>{row.Vendors.length ? row.Vendors.join(', ') : <NullValue reason="none" />}</td>
                    <td className="text-right">{row.PendingLineCount === null ? <NullValue reason="none" /> : formatCount(row.PendingLineCount)}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
      <Pager page={data.Page} pageSize={data.PageSize} totalRows={data.TotalRows} onPage={onPage} />
    </>
  )
}

function WorkloadTile({ tile, selected, selectedRoute, onSelect }: {
  tile: PurchaseWorkloadTile
  selected: boolean
  selectedRoute: string | null
  onSelect: (queue: PurchaseWorkloadQueue | null, route?: string | null) => void
}) {
  const denied = tile.State !== 'READY'
  const queue = tile.Key as PurchaseWorkloadQueue
  return (
    <div
      className="home-tile"
      data-tile-state={denied ? 'denied' : 'ready'}
      style={{ outline: selected ? '2px solid var(--color-accent)' : undefined, opacity: denied ? 0.75 : 1 }}
    >
      <button type="button" className="text-left w-full" disabled={denied} onClick={() => onSelect(selected ? null : queue)}
        title={denied ? 'Not permitted' : selected ? 'Show all queues' : 'Show only this queue in the detail rows'}>
        <div className="home-tile-title">{tile.Title}</div>
        {denied ? (
          <div className="mt-1 flex items-center gap-2">
            <span className="badge badge-muted">Permission denied</span>
            <NullValue reason="withheld" />
          </div>
        ) : (
          <>
            <div className="text-2xl font-semibold">{tile.Count === null ? <NullValue reason="withheld" /> : <span className="mono">{formatCount(tile.Count)}</span>}</div>
            <div className="field-hint">
              {tile.OldestAgeDays === null ? 'No waiting age' : `Oldest waiting ${formatAge(tile.OldestAgeDays)}`}
            </div>
            {tile.Key !== 'rfq-no-quotation' && (
              <div className="mt-1">
                {tile.CommercialValuesVisible ? (
                  <AmountList amounts={tile.Amounts.map((amount) => ({ Currency: amount.Currency, value: amount.Amount }))} />
                ) : (
                  <NullValue reason="withheld" />
                )}
              </div>
            )}
            {tile.UnvaluedDocumentCount !== null && tile.UnvaluedDocumentCount > 0 && (
              <div className="field-hint">{formatCount(tile.UnvaluedDocumentCount)} without a selected value yet</div>
            )}
          </>
        )}
      </button>
      {!denied && tile.ApprovalBands.length > 0 && (
        <div className="mt-2 flex flex-wrap gap-1">
          {tile.ApprovalBands.map((band) => {
            const active = selected && selectedRoute === band.ApprovalRoute
            return (
              <button key={band.ApprovalRoute} type="button" className={`badge ${active ? 'badge-info' : 'badge-muted'}`}
                onClick={() => onSelect(queue, active ? null : band.ApprovalRoute)}
                title="Show only this approval route in the detail rows">
                {band.ApprovalRoute}: {formatCount(band.Count)}
                {tile.CommercialValuesVisible && band.Amounts.length > 0 && (
                  <> · {band.Amounts.map((amount, index) => <Money key={index} value={amount.Amount} currency={amount.Currency} nullReason="withheld" />)}</>
                )}
              </button>
            )
          })}
        </div>
      )}
      {tile.Coverage && <div className="field-hint mt-2">{tile.Coverage}</div>}
    </div>
  )
}
