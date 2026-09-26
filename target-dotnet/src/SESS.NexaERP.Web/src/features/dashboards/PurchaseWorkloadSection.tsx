import { useMemo, useState } from 'react'
import { getPurchaseWorkload } from '../../api/dashboards'
import { SortableHeader } from '../../components/SortableHeader'
import { useSort } from '../../hooks/useSort'
import type { PurchaseWorkloadPage, PurchaseWorkloadQueue, PurchaseWorkloadRow, PurchaseWorkloadTile } from '../../types/dashboard'
import { PURCHASE_WORKLOAD_QUEUES } from '../../types/dashboard'
import { formatAge, formatCount, formatTimestamp } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { workloadRowLink } from './dashboardAccess'
import {
  AmountList, DetailFilterNote, MaybeLink, Money, NullValue, Pager, QueryProblem, SectionFrame, StateNotice,
} from './DashboardParts'
import { HBarChart, LegendDot, type HBar } from './PurchaseCharts'
import {
  AgeText, QuickFilter, RefreshingHint, SERVER_ORDER, SEVERITY, SectionSkeleton, WORKLOAD_AGE, ageSeverity, matchesText,
  sortRows, thresholdText, useFocusRequest, usePurchaseSectionQuery, useReportToPage,
  type FocusRequest, type SectionReport,
} from './PurchaseDashboardKit'

const PAGE_SIZE = 50

type Can = (pageKey: string, action?: string) => boolean

export function PurchaseWorkloadSection({ refreshTick = 0, onReport, focus }: {
  refreshTick?: number
  onReport?: (report: SectionReport<PurchaseWorkloadPage>) => void
  focus?: FocusRequest | null
}) {
  const { can } = useSession()
  const [queue, setQueue] = useState<PurchaseWorkloadQueue | null>(null)
  const [approvalRoute, setApprovalRoute] = useState<string | null>(null)
  const [page, setPage] = useState(1)

  const { state, data, refreshing, reload } = usePurchaseSectionQuery(
    () => getPurchaseWorkload({ queue, approvalRoute, page, pageSize: PAGE_SIZE }),
    `${queue}|${approvalRoute}|${page}`,
    refreshTick,
  )
  useReportToPage(onReport, state, data, queue === null && approvalRoute === null && page === 1)

  const selectQueue = (next: PurchaseWorkloadQueue | null, route: string | null = null) => {
    setQueue(next)
    setApprovalRoute(route)
    setPage(1)
  }

  useFocusRequest(focus, (target) => {
    if ((PURCHASE_WORKLOAD_QUEUES as readonly string[]).includes(target)) selectQueue(target as PurchaseWorkloadQueue)
    else selectQueue(null)
  })

  return (
    <SectionFrame
      id="purchase-workload"
      title="Workload"
      subtitle={<>Documents waiting in each purchase queue now. Counts are documents, not lines or vendors. <RefreshingHint on={refreshing} /></>}
      generatedAt={data?.GeneratedAt}
      timeZone={data?.TimeZone}
    >
      {!data && state.kind === 'loading' && (
        <>
          <StateNotice kind="loading" title="Loading the purchase queues…" />
          <SectionSkeleton tiles={4} />
        </>
      )}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <QueryProblem state={state} onRetry={reload} />}
      {data && <WorkloadBody data={data} can={can} onSelect={selectQueue} onPage={setPage} />}
    </SectionFrame>
  )
}

function WorkloadBody({ data, can, onSelect, onPage }: {
  data: PurchaseWorkloadPage
  can: Can
  onSelect: (queue: PurchaseWorkloadQueue | null, route?: string | null) => void
  onPage: (page: number) => void
}) {
  const [search, setSearch] = useState('')
  const { sort, toggleSort } = useSort(SERVER_ORDER)
  const tilesByKey = useMemo(() => new Map(data.Tiles.map((tile) => [tile.Key, tile])), [data.Tiles])
  const readyTiles = data.Tiles.filter((tile) => tile.State === 'READY')
  const deniedTiles = data.Tiles.filter((tile) => tile.State !== 'READY')
  const nothingWaiting = readyTiles.every((tile) => tile.Count === 0) && data.TotalRows === 0

  const activeFilters: string[] = []
  if (data.Queue) activeFilters.push(tilesByKey.get(data.Queue)?.Title ?? data.Queue)
  if (data.ApprovalRoute) activeFilters.push(`approval route ${data.ApprovalRoute}`)

  const rows = useMemo(() => {
    const filtered = data.Rows.filter((row) => matchesText(search, [
      row.DocumentNumber, row.DocumentType, row.Status, tilesByKey.get(row.Queue)?.Title ?? row.Queue,
      row.NextApproverEmployeeCode, row.NextApproverRole, row.ResponsibilityIssue, row.ApprovalRoute, ...row.Vendors,
    ]))
    return sortRows<PurchaseWorkloadRow>(filtered, sort, {
      document: (row) => row.DocumentNumber,
      queue: (row) => tilesByKey.get(row.Queue)?.Title ?? row.Queue,
      status: (row) => row.Status,
      waiting: (row) => row.WaitingSince,
      age: (row) => row.AgeDays,
      responsible: (row) => row.ResponsibilityIssue ?? row.NextApproverEmployeeCode ?? row.NextApproverRole,
      value: (row) => row.Value,
      pending: (row) => row.PendingLineCount,
    })
  }, [data.Rows, search, sort, tilesByKey])

  const bars: HBar[] = data.Tiles.map((tile) => {
    const denied = tile.State !== 'READY' || tile.Count === null
    const severity = tile.Count ? ageSeverity(tile.OldestAgeDays, WORKLOAD_AGE) : 'green'
    return {
      key: tile.Key,
      label: tile.Title,
      labelText: tile.Title,
      value: denied ? null : tile.Count,
      display: denied ? <NullValue reason="withheld" /> : <span className="mono font-semibold">{formatCount(tile.Count ?? 0)}</span>,
      displayText: denied ? 'withheld, not zero' : `${tile.Count} waiting${tile.OldestAgeDays !== null ? `, oldest ${formatAge(tile.OldestAgeDays)}` : ''}`,
      color: SEVERITY[severity].bar,
      note: !denied && tile.OldestAgeDays !== null ? <>oldest waiting <AgeText days={tile.OldestAgeDays} thresholds={WORKLOAD_AGE} /></> : undefined,
      onClick: denied ? undefined : () => onSelect(data.Queue === tile.Key ? null : (tile.Key as PurchaseWorkloadQueue)),
      clickHint: 'Show only this queue in the detail rows',
      selected: data.Queue === tile.Key,
    }
  })

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

      <div className="grid gap-4 my-3 lg:grid-cols-[minmax(0,5fr)_minmax(0,7fr)]">
        <div className="rounded-lg border border-line p-3">
          <div className="flex flex-wrap items-baseline justify-between gap-2 mb-2">
            <h3 className="text-[13px] font-semibold">Documents waiting, by queue</h3>
            <span className="flex flex-wrap gap-2">
              <LegendDot color={SEVERITY.green.bar}>fresh</LegendDot>
              <LegendDot color={SEVERITY.amber.bar}>ageing</LegendDot>
              <LegendDot color={SEVERITY.red.bar}>old</LegendDot>
            </span>
          </div>
          <HBarChart bars={bars} caption="Documents waiting in each purchase queue, coloured by the oldest waiting age" />
          <p className="text-[11.5px] text-ink-faint mt-2">Bar colour follows the oldest document in the queue ({thresholdText(WORKLOAD_AGE)}). Click a bar to list that queue below.</p>
        </div>
        <div className="grid gap-2 content-start" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))' }}>
          {data.Tiles.map((tile) => (
            <WorkloadTile key={tile.Key} tile={tile} selected={data.Queue === tile.Key} selectedRoute={data.ApprovalRoute} onSelect={onSelect} />
          ))}
        </div>
      </div>

      <DetailFilterNote active={activeFilters} onClear={() => onSelect(null)} />

      {data.Rows.length === 0 ? (
        !nothingWaiting && (
          <StateNotice kind="empty" title="No detail rows match this selection">
            {data.Queue ? 'The selected queue has no waiting documents on this page.' : 'There are no detail rows.'}
          </StateNotice>
        )
      ) : (
        <>
          <QuickFilter id="workload-search" value={search} onChange={setSearch} shown={rows.length} onPage={data.Rows.length}
            placeholder="Search document, status, approver, vendor…" />
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <SortableHeader label="Document" sortKey="document" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Queue" sortKey="queue" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Status" sortKey="status" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Waiting since" sortKey="waiting" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Age" sortKey="age" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Next responsible" sortKey="responsible" sort={sort} onSort={toggleSort} />
                  <SortableHeader label="Value" sortKey="value" sort={sort} onSort={toggleSort} />
                  <th>Vendors</th>
                  <SortableHeader label="Pending lines" sortKey="pending" sort={sort} onSort={toggleSort} />
                </tr>
              </thead>
              <tbody>
                {rows.length === 0 && (
                  <tr><td colSpan={9} className="table-empty">No rows on this page match "{search}".</td></tr>
                )}
                {rows.map((row) => {
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
                      <td className="whitespace-nowrap">{formatTimestamp(row.WaitingSince, data.TimeZone)}</td>
                      <td className="text-right"><AgeText days={row.AgeDays} thresholds={WORKLOAD_AGE} /></td>
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
        </>
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
  const severity = !denied && tile.Count ? ageSeverity(tile.OldestAgeDays, WORKLOAD_AGE) : null
  return (
    <div
      className="home-tile"
      data-tile-state={denied ? 'denied' : 'ready'}
      style={{
        outline: selected ? '2px solid var(--color-accent)' : undefined,
        opacity: denied ? 0.75 : 1,
        borderLeft: severity ? `4px solid ${SEVERITY[severity].bar}` : undefined,
      }}
    >
      <button type="button" className="text-left w-full cursor-pointer disabled:cursor-default" disabled={denied} onClick={() => onSelect(selected ? null : queue)}
        title={denied ? 'Not permitted' : selected ? 'Show all queues' : 'Show only this queue in the detail rows'}>
        <div className="home-tile-title text-[13px]">{tile.Title}</div>
        {denied ? (
          <div className="mt-1 flex items-center gap-2">
            <span className="badge badge-muted">Permission denied</span>
            <NullValue reason="withheld" />
          </div>
        ) : (
          <>
            <div className="text-2xl font-semibold">{tile.Count === null ? <NullValue reason="withheld" /> : <span className="mono">{formatCount(tile.Count)}</span>}</div>
            <div className="text-[12px] text-ink-soft">
              {tile.OldestAgeDays === null ? 'No waiting age' : <>Oldest waiting <AgeText days={tile.OldestAgeDays} thresholds={WORKLOAD_AGE} /></>}
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
              <button key={band.ApprovalRoute} type="button" className={`badge cursor-pointer ${active ? 'badge-info' : 'badge-muted'}`}
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
