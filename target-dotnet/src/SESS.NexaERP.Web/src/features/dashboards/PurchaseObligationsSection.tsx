import { useMemo, useState } from 'react'
import { Receipt } from 'lucide-react'
import { getPurchaseObligations } from '../../api/dashboards'
import { useSort } from '../../hooks/useSort'
import type { PurchaseObligationQueue, PurchaseObligationRow, PurchaseObligationsPage } from '../../types/dashboard'
import { formatAmount, formatDateOnly, formatQuantity } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { goodsReceiptLink, itemLink, poLink, vendorLink } from './dashboardAccess'
import { Money } from './DashboardParts'
import { AgeChip } from './DashboardUi'
import {
  CompactPager, CompactTable, DetailPanel, DocLink, FilterChip, NoRows, OBLIGATION_AGE, OneLine, RefreshDot, SECTION_ANCHOR, SERVER_ORDER,
  SearchBox, SectionState, Segmented, SortTh, Th, ageTone, matchesText, sortRows, useFocusRequest, usePurchaseSectionQuery, useReportToPage,
  type Can, type FocusRequest, type SectionReport,
} from './PurchaseDashboardKit'

const PAGE_SIZE = 50

export const OBLIGATION_TIP: Record<string, string> = {
  grni: 'Received, not billed: provisional value at the PO unit rate BEFORE GST and charges. Not landed or Actual BOM cost.',
  'vendor-advances': 'Advances not adjusted: cash paid to vendors ahead of a bill, less amounts already adjusted.',
}

const SECTION_TIP =
  `${OBLIGATION_TIP.grni} ${OBLIGATION_TIP['vendor-advances']} The two are different measures and are never added. ` +
  'Colour = age: amber from 15 days, red from 30. The API gives no due dates. Search and sorting work on this page of rows only.'

interface Filters {
  queue: PurchaseObligationQueue | null
  vendor: { id: string; label: string } | null
  currency: string | null
  document: { id: string; label: string } | null
}

const NO_FILTERS: Filters = { queue: null, vendor: null, currency: null, document: null }

type Kind = 'all' | PurchaseObligationQueue

export function PurchaseObligationsSection({ refreshTick = 0, onReport, focus }: {
  refreshTick?: number
  onReport?: (report: SectionReport<PurchaseObligationsPage>) => void
  focus?: FocusRequest | null
}) {
  const { can } = useSession()
  const [open, setOpen] = useState(false)
  const [filters, setFilters] = useState<Filters>(NO_FILTERS)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')

  const { state, data, refreshing, reload } = usePurchaseSectionQuery(
    () => getPurchaseObligations({
      queue: filters.queue,
      vendorId: filters.vendor?.id,
      currency: filters.currency,
      documentId: filters.document?.id,
      page,
      pageSize: PAGE_SIZE,
    }),
    `${filters.queue}|${filters.vendor?.id}|${filters.currency}|${filters.document?.id}|${page}`,
    refreshTick,
  )
  const unfiltered = !filters.queue && !filters.vendor && !filters.currency && !filters.document && page === 1
  useReportToPage(onReport, state, data, unfiltered)

  const apply = (next: Partial<Filters>) => {
    setFilters((current) => ({ ...current, ...next }))
    setPage(1)
  }

  useFocusRequest(focus, (target) => {
    setOpen(true)
    const queue = target.queue === 'grni' || target.queue === 'vendor-advances' ? target.queue : null
    setFilters({ ...NO_FILTERS, queue, vendor: target.vendor ?? null, currency: target.currency ?? null })
    setPage(1)
  })

  return (
    <DetailPanel id={SECTION_ANCHOR.obligations} icon={Receipt} title="Obligations" info={SECTION_TIP}
      open={open} onToggle={() => setOpen(!open)}
      right={<><RefreshDot on={refreshing} /><SearchBox value={search} onChange={setSearch} onFocus={() => setOpen(true)} placeholder="GRN, PO, vendor, item…" /></>}>
      <SectionState state={state} data={data} onRetry={reload}>
        {(d) => <ObligationsBody data={d} can={can} search={search} filters={filters} onFilter={apply} onPage={setPage} />}
      </SectionState>
    </DetailPanel>
  )
}

function ObligationsBody({ data, can, search, filters, onFilter, onPage }: {
  data: PurchaseObligationsPage
  can: Can
  search: string
  filters: Filters
  onFilter: (next: Partial<Filters>) => void
  onPage: (page: number) => void
}) {
  const { sort, toggleSort } = useSort(SERVER_ORDER)
  const nothingOutstanding = data.Tiles.every((tile) => tile.Count === 0) && data.TotalRows === 0
  const kindLabel = (key: string) => (key === 'grni' ? 'Not billed' : key === 'vendor-advances' ? 'Advance' : data.Tiles.find((tile) => tile.Key === key)?.Title ?? key)

  const rows = useMemo(() => {
    const filtered = data.Rows.filter((row) => matchesText(search, [
      row.DocumentNumber, row.PoNumber, row.VendorCode, row.VendorName, row.ItemCode, row.ItemName, row.Currency,
    ]))
    return sortRows<PurchaseObligationRow>(filtered, sort, {
      document: (row) => row.DocumentNumber,
      po: (row) => row.PoNumber,
      vendor: (row) => row.VendorName,
      date: (row) => row.SourceDate,
      age: (row) => row.AgeDays,
      value: (row) => row.Value,
    })
  }, [data.Rows, search, sort])

  return (
    <>
      <div className="mb-2 flex flex-wrap items-center gap-1.5">
        <Segmented<Kind> label="Kind" value={data.Filters.Queue ? (data.Filters.Queue as Kind) : 'all'}
          onChange={(kind) => onFilter({ queue: kind === 'all' ? null : kind })}
          options={[
            { value: 'all', label: 'All' },
            { value: 'grni', label: 'Not billed', title: OBLIGATION_TIP.grni },
            { value: 'vendor-advances', label: 'Advances', title: OBLIGATION_TIP['vendor-advances'] },
          ]} />
        {filters.vendor && <FilterChip label={`Vendor ${filters.vendor.label}`} onClear={() => onFilter({ vendor: null })} />}
        {filters.currency && <FilterChip label={filters.currency} onClear={() => onFilter({ currency: null })} />}
        {filters.document && <FilterChip label={filters.document.label} onClear={() => onFilter({ document: null })} />}
      </div>

      {nothingOutstanding ? (
        <OneLine>Nothing outstanding.</OneLine>
      ) : (
        <CompactTable head={<>
          <SortTh label="Document" sortKey="document" sort={sort} onSort={toggleSort} />
          <Th>Kind</Th>
          <SortTh label="PO" sortKey="po" sort={sort} onSort={toggleSort} />
          <SortTh label="Vendor" sortKey="vendor" sort={sort} onSort={toggleSort} />
          <Th>Item</Th>
          <SortTh label="Date" sortKey="date" sort={sort} onSort={toggleSort} />
          <SortTh label="Age" sortKey="age" sort={sort} onSort={toggleSort} />
          <SortTh label="Outstanding" sortKey="value" sort={sort} onSort={toggleSort} right />
        </>}>
          {rows.length === 0 && <NoRows cols={8} text={search ? `No rows on this page match "${search}".` : 'No rows for this selection.'} />}
          {rows.map((row) => {
            const isGrni = row.Queue === 'grni'
            const fmt = (value: number | null) => (value === null ? '—' : formatAmount(value, row.Currency))
            const valueTip = isGrni
              ? `${row.Quantity === null ? '' : `${formatQuantity(row.Quantity)} ${row.Uom ?? ''} `}at ${fmt(row.UnitRate)}, before GST`
              : `Paid ${fmt(row.OriginalAmount)} less adjusted ${fmt(row.AdjustedAmount)}`
            return (
              <tr key={`${row.Queue}-${row.DocumentId}-${row.LineId ?? 'advance'}`}>
                <td>
                  {isGrni ? (
                    <DocLink to={goodsReceiptLink(can, row.DocumentId)}>{row.DocumentNumber}</DocLink>
                  ) : (
                    <button type="button" className="font-mono text-xs text-blue-600 hover:underline" title="Only this advance"
                      onClick={() => onFilter({ document: { id: row.DocumentId, label: row.DocumentNumber } })}>{row.DocumentNumber}</button>
                  )}
                </td>
                <td className="text-xs text-slate-600">{kindLabel(row.Queue)}</td>
                <td><DocLink to={poLink(can, row.PoNumber)}>{row.PoNumber}</DocLink></td>
                <td className="max-w-44 truncate text-xs" title={`${row.VendorCode} · ${row.VendorName}`}>
                  {vendorLink(can, row.VendorCode) ? <DocLink to={vendorLink(can, row.VendorCode)}>{row.VendorName}</DocLink> : row.VendorName}
                </td>
                <td className="max-w-48 truncate text-xs" title={row.ItemName ?? undefined}>
                  {row.ItemCode ? <><DocLink to={itemLink(can, row.ItemCode)}>{row.ItemCode}</DocLink> <span className="text-slate-500">{row.ItemName}</span></> : <span className="text-slate-400">—</span>}
                </td>
                <td className="whitespace-nowrap text-xs text-slate-600" title={isGrni ? 'Received' : 'Paid'}>{formatDateOnly(row.SourceDate)}</td>
                <td><AgeChip days={row.AgeDays} tone={ageTone(row.AgeDays, OBLIGATION_AGE)} /></td>
                <td className="text-right text-xs font-medium" title={valueTip}><Money value={row.Value} currency={row.Currency} /></td>
              </tr>
            )
          })}
        </CompactTable>
      )}
      <CompactPager page={data.Filters.Page} pageSize={data.Filters.PageSize} totalRows={data.TotalRows} onPage={onPage} />
    </>
  )
}
