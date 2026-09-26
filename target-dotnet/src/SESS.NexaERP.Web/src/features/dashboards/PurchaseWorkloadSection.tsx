import { useMemo, useState } from 'react'
import { ClipboardList } from 'lucide-react'
import { getPurchaseWorkload } from '../../api/dashboards'
import { useSort } from '../../hooks/useSort'
import type { PurchaseWorkloadPage, PurchaseWorkloadQueue, PurchaseWorkloadRow } from '../../types/dashboard'
import { PURCHASE_WORKLOAD_QUEUES } from '../../types/dashboard'
import { formatCount, formatTimestamp } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { workloadRowLink } from './dashboardAccess'
import { Money, NullValue } from './DashboardParts'
import { AgeChip, InfoTip } from './DashboardUi'
import {
  CompactPager, CompactTable, DetailPanel, DocLink, FilterChip, NoRows, OneLine, RefreshDot, SECTION_ANCHOR, SERVER_ORDER, SearchBox,
  SectionState, SortTh, Th, WORKLOAD_AGE, ageTone, matchesText, sortRows, useFocusRequest, usePurchaseSectionQuery, useReportToPage,
  type Can, type FocusRequest, type SectionReport,
} from './PurchaseDashboardKit'

const PAGE_SIZE = 50

/** Short queue names for bars and chips; the server's Title is in the tooltip. */
export const QUEUE_SHORT: Record<string, string> = {
  'pr-department-verification': 'PR verification',
  'pr-approval': 'PR approval',
  'pr-stock-check': 'Stock check',
  'rfq-no-quotation': 'RFQ, no quote',
  'quotation-technical-verification': 'Quote tech check',
  'comparison-decision': 'Comparison',
  'po-approved-unissued': 'PO to issue',
}

const MONEY_TIP =
  'Queues = documents waiting now (documents, not lines). Colour = oldest waiting: amber from 3 days, red from 7. ' +
  'PR values are the saved estimated INR total; quotation, comparison and PO values are the saved total payable incl. GST. ' +
  'None is ex-tax or actual cost. Search and sorting work on this page of rows only.'

export function PurchaseWorkloadSection({ refreshTick = 0, onReport, focus }: {
  refreshTick?: number
  onReport?: (report: SectionReport<PurchaseWorkloadPage>) => void
  focus?: FocusRequest | null
}) {
  const { can } = useSession()
  const [open, setOpen] = useState(false)
  const [queue, setQueue] = useState<PurchaseWorkloadQueue | null>(null)
  const [approvalRoute, setApprovalRoute] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')

  const { state, data, refreshing, reload } = usePurchaseSectionQuery(
    () => getPurchaseWorkload({ queue, approvalRoute, page, pageSize: PAGE_SIZE }),
    `${queue}|${approvalRoute}|${page}`,
    refreshTick,
  )
  useReportToPage(onReport, state, data, queue === null && approvalRoute === null && page === 1)

  const select = (next: PurchaseWorkloadQueue | null, route: string | null = null) => {
    setQueue(next)
    setApprovalRoute(route)
    setPage(1)
  }

  useFocusRequest(focus, (target) => {
    setOpen(true)
    const q = target.queue && (PURCHASE_WORKLOAD_QUEUES as readonly string[]).includes(target.queue) ? (target.queue as PurchaseWorkloadQueue) : null
    select(q)
  })

  return (
    <DetailPanel id={SECTION_ANCHOR.workload} icon={ClipboardList} title="Workload details" info={MONEY_TIP}
      open={open} onToggle={() => setOpen(!open)}
      right={<><RefreshDot on={refreshing} /><SearchBox value={search} onChange={setSearch} onFocus={() => setOpen(true)} placeholder="Document, approver, vendor…" /></>}>
      <SectionState state={state} data={data} onRetry={reload}>
        {(d) => <WorkloadBody data={d} can={can} search={search} onSelect={select} onPage={setPage} />}
      </SectionState>
    </DetailPanel>
  )
}

function WorkloadBody({ data, can, search, onSelect, onPage }: {
  data: PurchaseWorkloadPage
  can: Can
  search: string
  onSelect: (queue: PurchaseWorkloadQueue | null, route?: string | null) => void
  onPage: (page: number) => void
}) {
  const { sort, toggleSort } = useSort(SERVER_ORDER)
  const tilesByKey = useMemo(() => new Map(data.Tiles.map((tile) => [tile.Key, tile])), [data.Tiles])
  const ready = data.Tiles.filter((tile) => tile.State === 'READY')
  const denied = data.Tiles.filter((tile) => tile.State !== 'READY')
  const selectedTile = data.Queue ? tilesByKey.get(data.Queue) : undefined
  const nothingWaiting = ready.every((tile) => !tile.Count) && data.TotalRows === 0

  const rows = useMemo(() => {
    const filtered = data.Rows.filter((row) => matchesText(search, [
      row.DocumentNumber, row.DocumentType, row.Status, tilesByKey.get(row.Queue)?.Title ?? row.Queue,
      row.NextApproverEmployeeCode, row.NextApproverRole, row.ResponsibilityIssue, row.ApprovalRoute, ...row.Vendors,
    ]))
    return sortRows<PurchaseWorkloadRow>(filtered, sort, {
      document: (row) => row.DocumentNumber,
      queue: (row) => QUEUE_SHORT[row.Queue] ?? row.Queue,
      status: (row) => row.Status,
      age: (row) => row.AgeDays,
      responsible: (row) => row.ResponsibilityIssue ?? row.NextApproverEmployeeCode ?? row.NextApproverRole,
      value: (row) => row.Value,
    })
  }, [data.Rows, search, sort, tilesByKey])

  return (
    <>
      <div className="mb-2 flex flex-wrap items-center gap-1.5">
        <select aria-label="Queue" value={data.Queue ?? ''} onChange={(e) => onSelect((e.target.value || null) as PurchaseWorkloadQueue | null)}
          className="h-7 rounded-md border border-slate-200 bg-white px-2 text-xs text-slate-700">
          <option value="">All queues</option>
          {ready.map((tile) => (
            <option key={tile.Key} value={tile.Key}>{QUEUE_SHORT[tile.Key] ?? tile.Title} ({formatCount(tile.Count ?? 0)})</option>
          ))}
        </select>
        {selectedTile?.ApprovalBands.map((band) => (
          <button key={band.ApprovalRoute} type="button" onClick={() => onSelect(data.Queue as PurchaseWorkloadQueue, data.ApprovalRoute === band.ApprovalRoute ? null : band.ApprovalRoute)}
            className={`rounded-full px-2 py-0.5 text-[11px] font-medium ${data.ApprovalRoute === band.ApprovalRoute ? 'bg-blue-600 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}
            title="Approval route">
            {band.ApprovalRoute} · {formatCount(band.Count)}
          </button>
        ))}
        {data.ApprovalRoute && <FilterChip label={`Route ${data.ApprovalRoute}`} onClear={() => onSelect(data.Queue as PurchaseWorkloadQueue)} />}
        {selectedTile?.Coverage && <InfoTip text={selectedTile.Coverage} />}
        {denied.length > 0 && (
          <span className="ml-auto text-[11px] text-slate-400" title={denied.map((tile) => tile.Title).join('; ')}>
            {denied.length} queue{denied.length === 1 ? '' : 's'} hidden (no access — withheld, not zero)
          </span>
        )}
      </div>

      {nothingWaiting ? (
        <OneLine>No purchase documents are waiting.</OneLine>
      ) : (
        <CompactTable head={<>
          <SortTh label="Document" sortKey="document" sort={sort} onSort={toggleSort} />
          <SortTh label="Queue" sortKey="queue" sort={sort} onSort={toggleSort} />
          <SortTh label="Status" sortKey="status" sort={sort} onSort={toggleSort} />
          <SortTh label="Age" sortKey="age" sort={sort} onSort={toggleSort} />
          <SortTh label="Next" sortKey="responsible" sort={sort} onSort={toggleSort} />
          <SortTh label="Value" sortKey="value" sort={sort} onSort={toggleSort} right />
          <Th>Vendors</Th>
        </>}>
          {rows.length === 0 && <NoRows cols={7} text={search ? `No rows on this page match "${search}".` : 'No rows for this selection.'} />}
          {rows.map((row) => {
            const tile = tilesByKey.get(row.Queue)
            const valueWithheld = tile ? !tile.CommercialValuesVisible : row.Currency === null
            return (
              <tr key={`${row.Queue}-${row.DocumentId}`}>
                <td title={`${row.DocumentType} · waiting since ${formatTimestamp(row.WaitingSince, data.TimeZone)}`}>
                  <DocLink to={workloadRowLink(can, row.Queue, row.DocumentNumber)}>{row.DocumentNumber}</DocLink>
                </td>
                <td className="text-xs text-slate-600" title={tile?.Title}>{QUEUE_SHORT[row.Queue] ?? tile?.Title ?? row.Queue}</td>
                <td className="text-xs text-slate-600">{row.Status}</td>
                <td><AgeChip days={row.AgeDays} tone={ageTone(row.AgeDays, WORKLOAD_AGE)} /></td>
                <td className="text-xs">
                  {row.ResponsibilityIssue ? (
                    <span className="text-amber-700" title="The workflow step does not name one responsible employee.">{row.ResponsibilityIssue}</span>
                  ) : row.NextApproverEmployeeCode || row.NextApproverRole ? (
                    <span title={row.NextApproverRole ?? undefined}><span className="font-mono">{row.NextApproverEmployeeCode ?? ''}</span> {row.NextApproverRole && <span className="text-slate-400">{row.NextApproverRole}</span>}</span>
                  ) : <NullValue reason="none" />}
                </td>
                <td className="text-right text-xs"><Money value={row.Value} currency={row.Currency} nullReason={valueWithheld ? 'withheld' : 'none'} /></td>
                <td className="max-w-48 truncate text-xs text-slate-600" title={row.Vendors.join(', ')}>{row.Vendors.length ? row.Vendors.join(', ') : '—'}</td>
              </tr>
            )
          })}
        </CompactTable>
      )}
      <CompactPager page={data.Page} pageSize={data.PageSize} totalRows={data.TotalRows} onPage={onPage} />
    </>
  )
}
