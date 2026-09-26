import { useMemo, useState } from 'react'
import { Filter, Truck } from 'lucide-react'
import { getPurchaseOpenOrders } from '../../api/dashboards'
import { useSort } from '../../hooks/useSort'
import type { PurchaseOpenOrderRow, PurchaseOpenOrdersPage } from '../../types/dashboard'
import { formatDateOnly, formatQuantity } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { itemLink, poLink, vendorLink } from './dashboardAccess'
import { Money } from './DashboardParts'
import { AgeChip, InfoTip } from './DashboardUi'
import {
  CompactPager, CompactTable, DetailPanel, DocLink, FilterChip, NoRows, OneLine, RefreshDot, SECTION_ANCHOR, SERVER_ORDER, SearchBox,
  SectionState, SortTh, Th, matchesText, sortRows, useFocusRequest, usePurchaseSectionQuery, useReportToPage,
  type Can, type FocusRequest, type SectionReport,
} from './PurchaseDashboardKit'

const PAGE_SIZE = 50

export const SOURCE_ISSUE_TEXT: Record<string, string> = {
  CURRENT_REVISION_UNAVAILABLE: 'current revision cannot be read',
  CANCELLED_UNISSUED_AMENDMENT: 'an unissued amendment was cancelled',
  LINE_PROVENANCE_INCONSISTENT: 'lines do not trace back consistently',
  RECEIPT_QUANTITY_INCONSISTENT: 'received quantities are inconsistent',
}

interface Filters {
  vendor: { id: string; label: string } | null
  currency: string | null
  root: { id: string; label: string } | null
  overdueOnly: boolean
}

const NO_FILTERS: Filters = { vendor: null, currency: null, root: null, overdueOnly: false }

const DELIVERY: Record<string, { label: string; cls: string }> = {
  OVERDUE: { label: 'Late', cls: 'bg-rose-50 text-rose-700' },
  WITHIN_COMMITMENT: { label: 'On time', cls: 'bg-emerald-50 text-emerald-700' },
  CONFIRMATION_REQUIRED: { label: 'Date unconfirmed', cls: 'bg-amber-50 text-amber-700' },
}

export function PurchaseOpenOrdersSection({ refreshTick = 0, onReport, focus }: {
  refreshTick?: number
  onReport?: (report: SectionReport<PurchaseOpenOrdersPage>) => void
  focus?: FocusRequest | null
}) {
  const { can } = useSession()
  const [open, setOpen] = useState(false)
  const [filters, setFilters] = useState<Filters>(NO_FILTERS)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')

  const { state, data, refreshing, reload } = usePurchaseSectionQuery(
    () => getPurchaseOpenOrders({
      vendorId: filters.vendor?.id,
      currency: filters.currency,
      rootPurchaseOrderId: filters.root?.id,
      overdueOnly: filters.overdueOnly || undefined,
      page,
      pageSize: PAGE_SIZE,
    }),
    `${filters.vendor?.id}|${filters.currency}|${filters.root?.id}|${filters.overdueOnly}|${page}`,
    refreshTick,
  )
  const unfiltered = !filters.vendor && !filters.currency && !filters.root && !filters.overdueOnly && page === 1
  useReportToPage(onReport, state, data, unfiltered)

  const apply = (next: Partial<Filters>) => {
    setFilters((current) => ({ ...current, ...next }))
    setPage(1)
  }

  useFocusRequest(focus, (target) => {
    setOpen(true)
    setFilters({ ...NO_FILTERS, overdueOnly: Boolean(target.overdue), vendor: target.vendor ?? null, currency: target.currency ?? null })
    setPage(1)
  })

  const tip = (data?.Basis ? `${data.Basis} ` : '') +
    'Values are the gross payable commitment incl. GST and charges, not ex-tax cost. Each currency stays separate. Search and sorting work on this page of rows only.'

  return (
    <DetailPanel id={SECTION_ANCHOR.openOrders} icon={Truck} title="Open PO lines" info={tip}
      open={open} onToggle={() => setOpen(!open)}
      right={<><RefreshDot on={refreshing} /><SearchBox value={search} onChange={setSearch} onFocus={() => setOpen(true)} placeholder="PO, vendor, item…" /></>}>
      <SectionState state={state} data={data} onRetry={reload}>
        {(d) => <OpenOrdersBody data={d} can={can} search={search} filters={filters} onFilter={apply} onPage={setPage} />}
      </SectionState>
    </DetailPanel>
  )
}

function OpenOrdersBody({ data, can, search, filters, onFilter, onPage }: {
  data: PurchaseOpenOrdersPage
  can: Can
  search: string
  filters: Filters
  onFilter: (next: Partial<Filters>) => void
  onPage: (page: number) => void
}) {
  const { sort, toggleSort } = useSort(SERVER_ORDER)
  const nothingOpen = data.Complete && data.OpenPoCount === 0 && data.TotalRows === 0
  const currencies = (data.Amounts ?? []).map((amount) => amount.Currency)

  const rows = useMemo(() => {
    const filtered = data.Rows.filter((row) => matchesText(search, [
      row.PoNumber, row.VendorCode, row.VendorName, row.ItemCode, row.ItemName, row.DeliveryTerms,
      DELIVERY[row.DeliveryState]?.label ?? row.DeliveryState, row.Currency,
    ]))
    return sortRows<PurchaseOpenOrderRow>(filtered, sort, {
      po: (row) => row.PoNumber,
      vendor: (row) => row.VendorName,
      item: (row) => row.ItemCode,
      remaining: (row) => row.RemainingQuantity,
      value: (row) => row.Value,
      late: (row) => row.DaysLate,
      age: (row) => row.AgeDays,
    })
  }, [data.Rows, search, sort])

  return (
    <>
      {!data.Complete && (
        <div className="mb-2">
          <OneLine tone="warn">
            {data.SourceIssues.length} PO{data.SourceIssues.length === 1 ? '' : 's'} don't reconcile — overall totals unknown (not zero); excluded from rows.
            <InfoTip text={data.SourceIssues.map((issue) => `${issue.PoNumber}: ${SOURCE_ISSUE_TEXT[issue.Code] ?? issue.Code}`).join('\n') + '\nAsk the administrator to reconcile.'} />
          </OneLine>
        </div>
      )}
      {data.Complete && !data.DeliveryComplete && (
        <div className="mb-2">
          <OneLine tone="warn">Late totals unknown until {data.DeliveryDateUnconfirmedPoCount} delivery date{data.DeliveryDateUnconfirmedPoCount === 1 ? ' is' : 's are'} confirmed.</OneLine>
        </div>
      )}

      <div className="mb-2 flex flex-wrap items-center gap-1.5">
        <button type="button" onClick={() => onFilter({ overdueOnly: !filters.overdueOnly })} aria-pressed={filters.overdueOnly}
          className={`rounded-full px-2 py-0.5 text-[11px] font-medium ${filters.overdueOnly ? 'bg-rose-600 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}>
          Late only
        </button>
        {currencies.length > 1 && (
          <select aria-label="Currency" value={filters.currency ?? ''} onChange={(e) => onFilter({ currency: e.target.value || null })}
            className="h-6 rounded-md border border-slate-200 bg-white px-1.5 text-[11px] text-slate-700">
            <option value="">All currencies</option>
            {currencies.map((currency) => <option key={currency} value={currency}>{currency}</option>)}
          </select>
        )}
        {filters.vendor && <FilterChip label={`Vendor ${filters.vendor.label}`} onClear={() => onFilter({ vendor: null })} />}
        {filters.root && <FilterChip label={`PO ${filters.root.label}`} onClear={() => onFilter({ root: null })} />}
        {currencies.length <= 1 && filters.currency && <FilterChip label={filters.currency} onClear={() => onFilter({ currency: null })} />}
      </div>

      {nothingOpen ? (
        <OneLine>No open purchase orders.</OneLine>
      ) : (
        <CompactTable head={<>
          <SortTh label="PO" sortKey="po" sort={sort} onSort={toggleSort} />
          <SortTh label="Vendor" sortKey="vendor" sort={sort} onSort={toggleSort} />
          <SortTh label="Item" sortKey="item" sort={sort} onSort={toggleSort} />
          <SortTh label="Remaining" sortKey="remaining" sort={sort} onSort={toggleSort} right />
          <SortTh label="Value" sortKey="value" sort={sort} onSort={toggleSort} right />
          <SortTh label="Delivery" sortKey="late" sort={sort} onSort={toggleSort} />
          <SortTh label="Age" sortKey="age" sort={sort} onSort={toggleSort} />
          <Th><span className="sr-only">Filter</span></Th>
        </>}>
          {rows.length === 0 && <NoRows cols={8} text={search ? `No rows on this page match "${search}".` : 'No lines for this selection.'} />}
          {rows.map((row) => <OpenOrderRow key={`${row.PurchaseOrderId}-${row.LineId}`} row={row} can={can} onFilter={onFilter} />)}
        </CompactTable>
      )}
      <CompactPager page={data.Filters.Page} pageSize={data.Filters.PageSize} totalRows={data.TotalRows} onPage={onPage} />
    </>
  )
}

function OpenOrderRow({ row, can, onFilter }: { row: PurchaseOpenOrderRow; can: Can; onFilter: (next: Partial<Filters>) => void }) {
  const delivery = DELIVERY[row.DeliveryState] ?? { label: row.DeliveryState, cls: 'bg-slate-100 text-slate-600' }
  const dates = `${row.CommittedDeliveryDate ? `Committed ${formatDateOnly(row.CommittedDeliveryDate)}` : 'No confirmed date'} · quoted ${formatDateOnly(row.QuotedDeliveryDate)} · ${row.DeliveryTerms}`
  const revisionNote = row.CurrentRevisionNumber !== row.RevisionNumber
    ? `Current r${row.CurrentRevisionNumber} is ${row.CurrentStatus}; quantities are from issued r${row.RevisionNumber}` : `Revision ${row.RevisionNumber}`
  const iconBtn = 'grid h-5 w-5 place-items-center rounded text-slate-400 hover:bg-slate-100 hover:text-slate-700'
  return (
    <tr className={row.DeliveryState === 'OVERDUE' ? 'bg-rose-50/40' : undefined}>
      <td title={revisionNote}><DocLink to={poLink(can, row.PoNumber)}>{row.PoNumber}</DocLink>{row.CurrentRevisionNumber !== row.RevisionNumber && <span className="ml-1 text-[10px] text-amber-600">r{row.RevisionNumber}</span>}</td>
      <td className="max-w-44 truncate text-xs" title={`${row.VendorCode} · ${row.VendorName}`}>
        {vendorLink(can, row.VendorCode) ? <DocLink to={vendorLink(can, row.VendorCode)}>{row.VendorName}</DocLink> : row.VendorName}
      </td>
      <td className="max-w-52 truncate text-xs" title={`${row.ItemCode} · ${row.ItemName}`}>
        <DocLink to={itemLink(can, row.ItemCode)}>{row.ItemCode}</DocLink> <span className="text-slate-500">{row.ItemName}</span>
      </td>
      <td className="whitespace-nowrap text-right text-xs tabular-nums" title={`${formatQuantity(row.OrderedQuantity)} ordered, ${formatQuantity(row.ReceivedQuantity)} received`}>
        {formatQuantity(row.RemainingQuantity)} {row.Uom}
      </td>
      <td className="text-right text-xs"><Money value={row.Value} currency={row.Currency} /></td>
      <td className="whitespace-nowrap text-xs" title={dates}>
        <span className={`rounded px-1.5 py-0.5 text-[11px] font-medium ${delivery.cls}`}>{delivery.label}</span>
        {row.DaysLate !== null && row.DaysLate > 0 && <span className="ml-1 font-medium text-rose-700">{row.DaysLate} d</span>}
      </td>
      <td><AgeChip days={row.AgeDays} tone="muted" /></td>
      <td className="whitespace-nowrap">
        <span className="inline-flex gap-0.5">
          <button type="button" className={iconBtn} title={`Only vendor ${row.VendorCode}`} aria-label={`Only vendor ${row.VendorCode}`}
            onClick={() => onFilter({ vendor: { id: row.VendorId, label: row.VendorCode } })}><Filter size={12} aria-hidden /></button>
          <button type="button" className={`${iconBtn} text-[10px] font-semibold`} title={`Only ${row.PoNumber}`} aria-label={`Only ${row.PoNumber}`}
            onClick={() => onFilter({ root: { id: row.RootPurchaseOrderId, label: row.PoNumber } })}>PO</button>
        </span>
      </td>
    </tr>
  )
}
