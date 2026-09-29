import { useEffect } from 'react'
import { ClipboardList, Filter, Lock } from 'lucide-react'
import { getStoresWorkload } from '../../api/dashboards'
import type { StoresWorkloadPage, StoresWorkloadQueue, StoresWorkloadRow } from '../../types/dashboard'
import { formatCount, formatTimestamp } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { storesWorkloadRowLink } from './dashboardAccess'
import { MaybeLink } from './DashboardParts'
import { AgeChip, CountBadge, InfoTip, Panel } from './DashboardUi'
import {
  COLOUR_RULES, CompactPager, CompactProblem, FilterChips, OneLine, SearchBox, SkeletonRows, SortTh, TD, TH, WORKLOAD_SHORT,
  ageTone, scrollToId, useLocalRows, useStoresSectionQuery,
} from './StoresDashboardKit'
import type { Reveal, SectionReport } from './StoresDashboardKit'

export const STORES_WORKLOAD_PAGE_SIZE = 100
export const STORES_WORKLOAD_PANEL_ID = 'stores-workload'

export interface WorkloadFilter {
  queue: StoresWorkloadQueue | null
  document: { id: string; label: string } | null
  page: number
}

export const EMPTY_WORKLOAD_FILTER: WorkloadFilter = { queue: null, document: null, page: 1 }

/** The next step a row's document screen offers. The screen itself checks the action permission. */
const NEXT_STEP: Record<string, string> = {
  'gate-no-grn': 'Open',
  'mir-approval': 'Approve',
  'mir-unissued': 'Issue',
}

type WorkloadSortKey = 'document' | 'queue' | 'status' | 'age' | 'lines' | 'vendor'

/**
 * Stores workload detail, folded by default. The query lives here (outside
 * the Panel body) so the cards and attention list above get their figures
 * even while the table is closed.
 */
export function StoresWorkloadSection({ filter, onFilter, refreshTick, onReport, reveal }: {
  filter: WorkloadFilter
  onFilter: (next: WorkloadFilter) => void
  refreshTick: number
  onReport: (report: SectionReport<StoresWorkloadPage>) => void
  reveal: Reveal
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

  const titleOf = (key: string) => WORKLOAD_SHORT[key] ?? data?.Tiles.find((tile) => tile.Key === key)?.Title ?? key
  const { shown, sort, setSort, toggle, query, setQuery } = useLocalRows<StoresWorkloadRow, WorkloadSortKey>(
    data?.Rows ?? [],
    {
      document: (row) => row.DocumentNumber,
      queue: (row) => titleOf(row.Queue),
      status: (row) => row.Status,
      age: (row) => row.AgeDays,
      lines: (row) => row.PendingLineCount,
      vendor: (row) => row.VendorName,
    },
    (row) => [row.DocumentNumber, row.DocumentType, row.Status, row.VendorName ?? '', titleOf(row.Queue), row.EligibleApprovalRoles.join(' ')].join(' '),
  )

  useEffect(() => {
    if (reveal.n === 0) return
    setQuery(reveal.search ?? '')
    setSort(null)
    scrollToId(STORES_WORKLOAD_PANEL_ID)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [reveal.n])

  const info = [
    'Gate entries waiting for a GRN, and material issue requests waiting for approval or issue. Counts are documents; no money here.',
    data ? data.Tiles.map((tile) => `${tile.Title}: ${tile.Coverage}`).join(' ') : '',
    'Search and sorting apply to the rows on this page only.',
    data ? `Server time ${formatTimestamp(data.GeneratedAt, data.TimeZone)} (${data.TimeZone}).` : '',
    COLOUR_RULES,
  ].filter(Boolean).join(' ')

  const activeFilters: string[] = []
  if (data?.Filters.Queue) activeFilters.push(titleOf(data.Filters.Queue))
  if (filter.document) activeFilters.push(filter.document.label)

  return (
    <Panel key={reveal.n} id={STORES_WORKLOAD_PANEL_ID} icon={ClipboardList} title="Workload details" info={info}
      collapsible defaultOpen={reveal.n > 0}
      right={<>
        {refreshing && <span className="text-[11px] text-slate-400" role="status">Refreshing…</span>}
        {data && <SearchBox value={query} onChange={setQuery} placeholder="Find document, vendor…" />}
        {data && <CountBadge n={data.TotalRows} />}
      </>}>
      {state.kind === 'loading' && !data && <SkeletonRows label="Stores queues" rows={4} />}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <CompactProblem state={state} onRetry={reload} />}
      {data && (
        <>
          <DeniedLine titles={data.Tiles.filter((tile) => tile.State !== 'READY').map((tile) => tile.Title)} />
          <FilterChips active={activeFilters} onClear={() => onFilter(EMPTY_WORKLOAD_FILTER)} />
          {data.Rows.length === 0 ? (
            <div data-dashboard-state="empty"><OneLine tone="ok">{activeFilters.length ? 'No rows match this filter.' : 'Nothing is waiting in these queues.'}</OneLine></div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr>
                    <SortTh label="Document" sortKey="document" sort={sort} onSort={toggle} />
                    <SortTh label="Queue" sortKey="queue" sort={sort} onSort={toggle} />
                    <SortTh label="Status" sortKey="status" sort={sort} onSort={toggle} />
                    <SortTh label="Age" sortKey="age" sort={sort} onSort={toggle} align="right" />
                    <SortTh label="Lines" sortKey="lines" sort={sort} onSort={toggle} align="right" />
                    <SortTh label="Vendor" sortKey="vendor" sort={sort} onSort={toggle} />
                    <th className={TH}>Who acts</th>
                    <th className={TH}><span className="sr-only">Action</span></th>
                  </tr>
                </thead>
                <tbody>
                  {shown.length === 0 && (
                    <tr><td colSpan={8} className={`${TD} text-xs text-slate-400`}>No row on this page matches “{query}”.</td></tr>
                  )}
                  {shown.map((row) => {
                    const link = storesWorkloadRowLink(can, row.Queue, row.DocumentId)
                    return (
                      <tr key={`${row.Queue}-${row.DocumentId}`} className="hover:bg-slate-50">
                        <td className={`${TD} whitespace-nowrap`}>
                          <span className="mono" title={row.DocumentType}><MaybeLink to={link}>{row.DocumentNumber}</MaybeLink></span>
                          <button type="button" title="Show only this document" aria-label={`Show only ${row.DocumentNumber}`}
                            className="ml-1.5 inline-flex align-middle text-slate-300 hover:text-blue-600"
                            onClick={() => onFilter({ ...filter, document: { id: row.DocumentId, label: row.DocumentNumber }, page: 1 })}>
                            <Filter size={12} aria-hidden />
                          </button>
                        </td>
                        <td className={`${TD} whitespace-nowrap text-slate-600`}>{titleOf(row.Queue)}</td>
                        <td className={`${TD} whitespace-nowrap text-xs text-slate-600`}>{row.Status}</td>
                        <td className={`${TD} text-right`}>
                          <span title={`Waiting since ${formatTimestamp(row.WaitingSince, data.TimeZone)}`}>
                            <AgeChip days={row.AgeDays} tone={ageTone('workload', row.AgeDays)} />
                          </span>
                        </td>
                        <td className={`${TD} text-right tabular-nums`}>{formatCount(row.PendingLineCount)}</td>
                        <td className={`${TD} max-w-[14rem] truncate text-slate-600`} title={row.VendorName ?? undefined}>{row.VendorName ?? <span className="text-slate-300">—</span>}</td>
                        <td className={`${TD} text-xs text-slate-600`}>
                          {row.EligibleApprovalRoles.length > 0 && <span title="Any of these roles">{row.EligibleApprovalRoles.join(', ')}</span>}
                          {/* The MIR workflow names no approver; show the server's reason, never an invented person. */}
                          {row.ResponsibilityIssue && (
                            <span className="ml-1 inline-flex items-center gap-1 text-amber-700">
                              Unassigned <InfoTip text={row.ResponsibilityIssue} />
                            </span>
                          )}
                          {!row.ResponsibilityIssue && row.EligibleApprovalRoles.length === 0 && <span className="text-slate-300">—</span>}
                        </td>
                        <td className={`${TD} whitespace-nowrap text-right`}>
                          {link ? (
                            <MaybeLink to={link}><span className="text-xs font-medium">{NEXT_STEP[row.Queue] ?? 'Open'}</span></MaybeLink>
                          ) : (
                            <span className="inline-flex text-slate-300" title="The document screen is not permitted for your role"><Lock size={12} aria-hidden /></span>
                          )}
                        </td>
                      </tr>
                    )
                  })}
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

function DeniedLine({ titles }: { titles: string[] }) {
  if (titles.length === 0) return null
  return (
    <div data-dashboard-state="denied">
      <OneLine icon={<Lock size={12} aria-hidden />} title="Their counts are withheld from your role — withheld, not zero.">
        No access: {titles.join(', ')}
      </OneLine>
    </div>
  )
}
