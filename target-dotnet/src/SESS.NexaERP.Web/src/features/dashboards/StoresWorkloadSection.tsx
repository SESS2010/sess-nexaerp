import { useState } from 'react'
import { getStoresWorkload } from '../../api/dashboards'
import type { StoresWorkloadPage, StoresWorkloadQueue, StoresWorkloadTile } from '../../types/dashboard'
import { formatAge, formatCount, formatTimestamp } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { storesWorkloadRowLink } from './dashboardAccess'
import { DetailFilterNote, MaybeLink, NullValue, Pager, QueryProblem, SectionFrame, StateNotice } from './DashboardParts'
import { useDashboardQuery } from './useDashboardQuery'

const PAGE_SIZE = 50

export function StoresWorkloadSection() {
  const { can } = useSession()
  const [queue, setQueue] = useState<StoresWorkloadQueue | null>(null)
  const [document, setDocument] = useState<{ id: string; label: string } | null>(null)
  const [page, setPage] = useState(1)

  const { state, reload } = useDashboardQuery(
    () => getStoresWorkload({ queue, documentId: document?.id, page, pageSize: PAGE_SIZE }),
    `${queue}|${document?.id}|${page}`,
  )

  const select = (next: { queue?: StoresWorkloadQueue | null; document?: { id: string; label: string } | null }) => {
    if ('queue' in next) setQueue(next.queue ?? null)
    if ('document' in next) setDocument(next.document ?? null)
    setPage(1)
  }

  const data = state.kind === 'ready' ? state.data : null
  return (
    <SectionFrame
      id="stores-workload"
      title="Stores workload"
      subtitle="Gate entries waiting for a GRN, and material issue requests waiting for approval or issue. Counts are documents. No money on this section."
      generatedAt={data?.GeneratedAt}
      timeZone={data?.TimeZone}
    >
      {state.kind === 'loading' && <StateNotice kind="loading" title="Loading the Stores queues…" />}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <QueryProblem state={state} onRetry={reload} />}
      {data && (
        <StoresWorkloadBody data={data} can={can} document={document} onSelect={select}
          onClear={() => select({ queue: null, document: null })} onPage={setPage} />
      )}
    </SectionFrame>
  )
}

function StoresWorkloadBody({ data, can, document, onSelect, onClear, onPage }: {
  data: StoresWorkloadPage
  can: (pageKey: string, action?: string) => boolean
  document: { id: string; label: string } | null
  onSelect: (next: { queue?: StoresWorkloadQueue | null; document?: { id: string; label: string } | null }) => void
  onClear: () => void
  onPage: (page: number) => void
}) {
  const titleOf = (key: string) => data.Tiles.find((tile) => tile.Key === key)?.Title ?? key
  const readyTiles = data.Tiles.filter((tile) => tile.State === 'READY')
  const deniedTiles = data.Tiles.filter((tile) => tile.State !== 'READY')
  const nothingWaiting = readyTiles.every((tile) => tile.Count === 0) && data.TotalRows === 0

  const activeFilters: string[] = []
  if (data.Filters.Queue) activeFilters.push(titleOf(data.Filters.Queue))
  if (document) activeFilters.push(`document ${document.label}`)

  return (
    <>
      {deniedTiles.length > 0 && (
        <StateNotice kind="denied" title={`${deniedTiles.length} queue${deniedTiles.length === 1 ? ' is' : 's are'} not available to you`}>
          Their counts are withheld, not zero: {deniedTiles.map((tile) => tile.Title).join('; ')}.
        </StateNotice>
      )}

      {nothingWaiting && (
        <StateNotice kind="empty" title="Nothing is waiting in Stores">
          You are permitted to see {deniedTiles.length ? 'the other queues' : 'these queues'}, and none has a document in it now.
        </StateNotice>
      )}

      <div className="grid gap-3 my-3" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))' }}>
        {data.Tiles.map((tile) => (
          <StoresTile key={tile.Key} tile={tile} selected={data.Filters.Queue === tile.Key}
            onSelect={() => onSelect({ queue: data.Filters.Queue === tile.Key ? null : (tile.Key as StoresWorkloadQueue) })} />
        ))}
      </div>

      <DetailFilterNote active={activeFilters} onClear={onClear} />

      {data.Rows.length === 0 ? (
        !nothingWaiting && <StateNotice kind="empty" title="No detail rows match this selection" />
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
                <th className="text-right">Pending lines</th>
                <th>Vendor</th>
                <th>Who acts</th>
              </tr>
            </thead>
            <tbody>
              {data.Rows.map((row) => (
                <tr key={`${row.Queue}-${row.DocumentId}`}>
                  <td className="mono whitespace-nowrap">
                    <MaybeLink to={storesWorkloadRowLink(can, row.Queue, row.DocumentId)}>{row.DocumentNumber}</MaybeLink>
                    <div className="field-hint">{row.DocumentType}</div>
                    <button type="button" className="link-button" onClick={() => onSelect({ document: { id: row.DocumentId, label: row.DocumentNumber } })}>
                      only this document
                    </button>
                  </td>
                  <td>{titleOf(row.Queue)}</td>
                  <td><span className="badge badge-muted">{row.Status}</span></td>
                  <td>{formatTimestamp(row.WaitingSince, data.TimeZone)}</td>
                  <td className="text-right">{formatAge(row.AgeDays)}</td>
                  <td className="text-right mono">{formatCount(row.PendingLineCount)}</td>
                  <td>{row.VendorName ?? <NullValue reason="none" />}</td>
                  <td>
                    {row.EligibleApprovalRoles.length > 0 && (
                      <div>Any of: {row.EligibleApprovalRoles.join(', ')}</div>
                    )}
                    {/* The MIR workflow names no approver; show the server's reason, never an invented person. */}
                    {row.ResponsibilityIssue && <span className="badge badge-warn">{row.ResponsibilityIssue}</span>}
                    {!row.ResponsibilityIssue && row.EligibleApprovalRoles.length === 0 && <NullValue reason="none" />}
                  </td>
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

function StoresTile({ tile, selected, onSelect }: { tile: StoresWorkloadTile; selected: boolean; onSelect: () => void }) {
  const denied = tile.State !== 'READY'
  return (
    <button type="button" className="home-tile text-left" disabled={denied} onClick={onSelect}
      data-tile-state={denied ? 'denied' : 'ready'}
      style={{ outline: selected ? '2px solid var(--color-accent)' : undefined, opacity: denied ? 0.75 : 1 }}
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
          <div className="field-hint">{tile.OldestAgeDays === null ? 'No waiting age' : `Oldest waiting ${formatAge(tile.OldestAgeDays)}`}</div>
        </>
      )}
      <div className="field-hint mt-2">{tile.Coverage}</div>
    </button>
  )
}
