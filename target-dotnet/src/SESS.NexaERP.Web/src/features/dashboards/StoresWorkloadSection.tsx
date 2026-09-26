import { useEffect } from 'react'
import { getStoresWorkload } from '../../api/dashboards'
import type { StoresWorkloadPage, StoresWorkloadQueue, StoresWorkloadRow } from '../../types/dashboard'
import { formatCount, formatTimestamp } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { storesWorkloadRowLink } from './dashboardAccess'
import { DetailFilterNote, MaybeLink, NullValue, Pager, QueryProblem, SectionFrame, StateNotice } from './DashboardParts'
import {
  AgeChip, RefreshingBadge, SectionSkeleton, SortTh, TableToolbar, ageTone, useLocalRows, useStoresSectionQuery,
} from './StoresDashboardKit'
import type { SectionReport } from './StoresDashboardKit'

export const STORES_WORKLOAD_PAGE_SIZE = 100

export interface WorkloadFilter {
  queue: StoresWorkloadQueue | null
  document: { id: string; label: string } | null
  page: number
}

export const EMPTY_WORKLOAD_FILTER: WorkloadFilter = { queue: null, document: null, page: 1 }

/** The next step a row's document screen offers. The screen itself checks the action permission. */
const NEXT_STEP: Record<string, string> = {
  'gate-no-grn': 'Open gate entry',
  'mir-approval': 'Open to approve',
  'mir-unissued': 'Open to issue',
}

/**
 * Stores workload detail. The overview cards for this endpoint are drawn at
 * the top of the page (StoresKpiTiles); filters live on the page so a card
 * click can select a queue here.
 */
export function StoresWorkloadSection({ filter, onFilter, refreshTick, onReport }: {
  filter: WorkloadFilter
  onFilter: (next: WorkloadFilter) => void
  refreshTick: number
  onReport: (report: SectionReport<StoresWorkloadPage>) => void
}) {
  const { can } = useSession()
  const { state, reload, data, refreshing, report } = useStoresSectionQuery(
    () => getStoresWorkload({ queue: filter.queue, documentId: filter.document?.id, page: filter.page, pageSize: STORES_WORKLOAD_PAGE_SIZE }),
    `${filter.queue}|${filter.document?.id}|${filter.page}`,
    refreshTick,
  )

  useEffect(() => {
    onReport(report)
    // report is rebuilt every render; its fields are the dependencies.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [report.kind, report.data, report.refreshing, report.receivedAt, onReport])

  return (
    <SectionFrame
      id="stores-workload"
      title="Stores workload — detail"
      subtitle="Gate entries waiting for a GRN, and material issue requests waiting for approval or issue. Counts are documents. No money on this section."
      generatedAt={data?.GeneratedAt}
      timeZone={data?.TimeZone}
    >
      {state.kind === 'loading' && !data && <SectionSkeleton label="Loading the Stores queues…" rows={5} />}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <QueryProblem state={state} onRetry={reload} />}
      {data && (
        <WorkloadBody data={data} can={can} filter={filter} onFilter={onFilter} refreshing={refreshing} />
      )}
    </SectionFrame>
  )
}

type WorkloadSortKey = 'document' | 'queue' | 'status' | 'since' | 'age' | 'lines' | 'vendor'

function WorkloadBody({ data, can, filter, onFilter, refreshing }: {
  data: StoresWorkloadPage
  can: (pageKey: string, action?: string) => boolean
  filter: WorkloadFilter
  onFilter: (next: WorkloadFilter) => void
  refreshing: boolean
}) {
  const titleOf = (key: string) => data.Tiles.find((tile) => tile.Key === key)?.Title ?? key
  const readyTiles = data.Tiles.filter((tile) => tile.State === 'READY')
  const deniedTiles = data.Tiles.filter((tile) => tile.State !== 'READY')
  const nothingWaiting = readyTiles.every((tile) => tile.Count === 0) && data.TotalRows === 0

  const activeFilters: string[] = []
  if (data.Filters.Queue) activeFilters.push(titleOf(data.Filters.Queue))
  if (filter.document) activeFilters.push(`document ${filter.document.label}`)

  const { shown, sort, toggle, query, setQuery } = useLocalRows<StoresWorkloadRow, WorkloadSortKey>(
    data.Rows,
    {
      document: (row) => row.DocumentNumber,
      queue: (row) => titleOf(row.Queue),
      status: (row) => row.Status,
      since: (row) => row.WaitingSince,
      age: (row) => row.AgeDays,
      lines: (row) => row.PendingLineCount,
      vendor: (row) => row.VendorName,
    },
    (row) => [row.DocumentNumber, row.DocumentType, row.Status, row.VendorName ?? '', titleOf(row.Queue), row.EligibleApprovalRoles.join(' ')].join(' '),
  )

  return (
    <>
      <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
        <details className="text-[13px] text-ink-soft">
          <summary className="cursor-pointer select-none font-medium text-ink">What each queue counts</summary>
          <ul className="mt-1 list-disc pl-5">
            {data.Tiles.map((tile) => (
              <li key={tile.Key}><strong>{tile.Title}:</strong> {tile.Coverage}</li>
            ))}
          </ul>
        </details>
        <RefreshingBadge on={refreshing} />
      </div>

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

      <div className="mt-2">
        <DetailFilterNote active={activeFilters} onClear={() => onFilter({ queue: null, document: null, page: 1 })} />
      </div>

      <div id="stores-workload-detail" className="scroll-mt-4" />
      {data.Rows.length === 0 ? (
        !nothingWaiting && <StateNotice kind="empty" title="No detail rows match this selection" />
      ) : (
        <>
          <TableToolbar query={query} onQuery={setQuery} placeholder="Find document, vendor, status…"
            shown={shown.length} loaded={data.Rows.length} total={data.TotalRows} />
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <SortTh label="Document" sortKey="document" sort={sort} onSort={toggle} />
                  <SortTh label="Queue" sortKey="queue" sort={sort} onSort={toggle} />
                  <SortTh label="Status" sortKey="status" sort={sort} onSort={toggle} />
                  <SortTh label="Waiting since" sortKey="since" sort={sort} onSort={toggle} />
                  <SortTh label="Age" sortKey="age" sort={sort} onSort={toggle} align="right" />
                  <SortTh label="Pending lines" sortKey="lines" sort={sort} onSort={toggle} align="right" />
                  <SortTh label="Vendor" sortKey="vendor" sort={sort} onSort={toggle} />
                  <th>Who acts</th>
                  <th>Next step</th>
                </tr>
              </thead>
              <tbody>
                {shown.length === 0 && (
                  <tr><td colSpan={9} className="field-hint">No row on this page matches “{query}”.</td></tr>
                )}
                {shown.map((row) => {
                  const link = storesWorkloadRowLink(can, row.Queue, row.DocumentId)
                  return (
                    <tr key={`${row.Queue}-${row.DocumentId}`}>
                      <td className="mono whitespace-nowrap">
                        <MaybeLink to={link}>{row.DocumentNumber}</MaybeLink>
                        <div className="field-hint">{row.DocumentType}</div>
                        <button type="button" className="link-button text-[12px]"
                          onClick={() => onFilter({ ...filter, document: { id: row.DocumentId, label: row.DocumentNumber }, page: 1 })}>
                          only this document
                        </button>
                      </td>
                      <td>{titleOf(row.Queue)}</td>
                      <td><span className="badge badge-muted">{row.Status}</span></td>
                      <td className="whitespace-nowrap">{formatTimestamp(row.WaitingSince, data.TimeZone)}</td>
                      <td className="text-right"><AgeChip days={row.AgeDays} tone={ageTone('workload', row.AgeDays)} /></td>
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
                      <td className="whitespace-nowrap">
                        {link ? (
                          <MaybeLink to={link}><span className="text-[13px] font-medium">{NEXT_STEP[row.Queue] ?? 'Open'} →</span></MaybeLink>
                        ) : (
                          <span className="field-hint" title="The document screen is not permitted for your role">No screen access</span>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </>
      )}
      <Pager page={data.Filters.Page} pageSize={data.Filters.PageSize} totalRows={data.TotalRows} onPage={(page) => onFilter({ ...filter, page })} />
    </>
  )
}
