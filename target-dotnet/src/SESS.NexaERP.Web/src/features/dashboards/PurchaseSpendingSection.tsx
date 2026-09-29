import { useMemo, useState } from 'react'
import { IndianRupee } from 'lucide-react'
import { getPurchaseSpending } from '../../api/dashboards'
import { useSort } from '../../hooks/useSort'
import type { PurchaseSpendingGroup, PurchaseSpendingPage, PurchaseSpendingRow } from '../../types/dashboard'
import { formatAmount, formatDateOnly, formatMonth, formatQuantity } from '../../utils/dashboardFormat'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { itemLink, poLink, vendorLink } from './dashboardAccess'
import { Money } from './DashboardParts'
import { MiniBars } from './DashboardUi'
import {
  CompactPager, CompactTable, DetailPanel, DocLink, FilterChip, GST_TIP, NoRows, OneLine, PERIOD_LONG, RefreshDot, SECTION_ANCHOR,
  SERVER_ORDER, SearchBox, SectionState, SortTh, Th, formatCompactAmount, inrFirst, matchesText, sortRows, useFocusRequest,
  usePurchaseSectionQuery, useReportToPage, type Can, type FocusRequest, type SectionReport, type SpendPeriod,
} from './PurchaseDashboardKit'

const PAGE_SIZE = 50

interface Filters {
  vendor: { id: string; label: string } | null
  category: { id: string; label: string } | null
  bill: { id: string; label: string } | null
  currency: string | null
}

const NO_FILTERS: Filters = { vendor: null, category: null, bill: null, currency: null }

/** The period comes from the page (Spend trend panel); only the contract's keys or one of the last twelve months. */
export function PurchaseSpendingSection({ refreshTick = 0, onReport, focus, period }: {
  refreshTick?: number
  onReport?: (report: SectionReport<PurchaseSpendingPage>) => void
  focus?: FocusRequest | null
  period: SpendPeriod
}) {
  const { can } = useSession()
  const [open, setOpen] = useState(false)
  const [filters, setFilters] = useState<Filters>(NO_FILTERS)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const month = period.period === 'month' ? period.month : null

  const { state, data, refreshing, reload } = usePurchaseSectionQuery(
    () => getPurchaseSpending({
      period: period.period,
      month,
      vendorId: filters.vendor?.id,
      categoryId: filters.category?.id,
      billId: filters.bill?.id,
      currency: filters.currency,
      page,
      pageSize: PAGE_SIZE,
    }),
    `${period.period}|${month}|${filters.vendor?.id}|${filters.category?.id}|${filters.bill?.id}|${filters.currency}|${page}`,
    refreshTick,
  )
  const unfiltered = !filters.vendor && !filters.category && !filters.bill && !filters.currency && page === 1
  useReportToPage(onReport, state, data, unfiltered)

  const apply = (next: Partial<Filters>) => {
    setFilters((current) => ({ ...current, ...next }))
    setPage(1)
  }

  useFocusRequest(focus, (target) => {
    setOpen(true)
    setFilters({ ...NO_FILTERS, vendor: target.vendor ?? null, currency: target.currency ?? null })
    setPage(1)
  })

  const tip = `${data?.Basis ? `${data.Basis} ` : ''}${GST_TIP} Change the period in Spend trend. Search and sorting work on this page of rows only.`

  return (
    <DetailPanel id={SECTION_ANCHOR.spending} icon={IndianRupee} title="Spending details" info={tip}
      open={open} onToggle={() => setOpen(!open)}
      right={<><RefreshDot on={refreshing} /><SearchBox value={search} onChange={setSearch} onFocus={() => setOpen(true)} placeholder="Bill, PO, vendor, item…" /></>}>
      <SectionState state={state} data={data} onRetry={reload}>
        {(d) => <SpendingBody data={d} can={can} search={search} filters={filters} onFilter={apply} onPage={setPage} />}
      </SectionState>
    </DetailPanel>
  )
}

function SpendingBody({ data, can, search, filters, onFilter, onPage }: {
  data: PurchaseSpendingPage
  can: Can
  search: string
  filters: Filters
  onFilter: (next: Partial<Filters>) => void
  onPage: (page: number) => void
}) {
  const { sort, toggleSort } = useSort(SERVER_ORDER)
  const canOpenBills = can(PAGE_KEYS.vendorBills)
  const periodLabel = data.Filters.Period === 'month' ? formatMonth(data.FromDate) : PERIOD_LONG[data.Filters.Period] ?? data.Filters.Period

  const rows = useMemo(() => {
    const filtered = data.Rows.filter((row) => matchesText(search, [
      row.BillNumber, row.PoNumber, row.VendorCode, row.VendorName, row.CategoryCode, row.ItemCode, row.ItemName, row.Event, row.Currency,
    ]))
    return sortRows<PurchaseSpendingRow>(filtered, sort, {
      date: (row) => row.EventDate,
      bill: (row) => row.BillNumber,
      po: (row) => row.PoNumber,
      vendor: (row) => row.VendorName,
      item: (row) => `${row.CategoryCode} ${row.ItemCode}`,
      amount: (row) => row.Amount,
    })
  }, [data.Rows, search, sort])

  return (
    <>
      <div className="mb-2 flex flex-wrap items-center gap-1.5 text-xs text-slate-500">
        <span className="font-medium text-slate-700">{periodLabel}</span>
        <span>{formatDateOnly(data.FromDate)} – {formatDateOnly(data.ToDate)} · incl. GST</span>
        {filters.vendor && <FilterChip label={`Vendor ${filters.vendor.label}`} onClear={() => onFilter({ vendor: null })} />}
        {filters.category && <FilterChip label={`Category ${filters.category.label}`} onClear={() => onFilter({ category: null })} />}
        {filters.bill && <FilterChip label={`Bill ${filters.bill.label}`} onClear={() => onFilter({ bill: null })} />}
        {filters.currency && <FilterChip label={filters.currency} onClear={() => onFilter({ currency: null })} />}
      </div>

      <div className="mb-3 grid gap-4 md:grid-cols-2">
        <GroupBars title="Top vendors · this FY" groups={data.TopVendors}
          onPick={(group) => onFilter({ vendor: { id: group.Id, label: group.Code }, currency: group.Currency })} />
        <GroupBars title="Categories · this FY" groups={data.Categories}
          onPick={(group) => onFilter({ category: { id: group.Id, label: group.Code }, currency: group.Currency })} />
      </div>

      {data.TotalRows === 0 && data.Rows.length === 0 ? (
        <OneLine>No accepted or reversed bills in this selection.</OneLine>
      ) : (
        <CompactTable head={<>
          <SortTh label="Date" sortKey="date" sort={sort} onSort={toggleSort} />
          <SortTh label="Bill" sortKey="bill" sort={sort} onSort={toggleSort} />
          <SortTh label="PO" sortKey="po" sort={sort} onSort={toggleSort} />
          <SortTh label="Vendor" sortKey="vendor" sort={sort} onSort={toggleSort} />
          <SortTh label="Category · item" sortKey="item" sort={sort} onSort={toggleSort} />
          <Th right>Qty</Th>
          <SortTh label="Amount" sortKey="amount" sort={sort} onSort={toggleSort} right />
        </>}>
          {rows.length === 0 && <NoRows cols={7} text={`No rows on this page match "${search}".`} />}
          {/* One bill line can be accepted, reversed and accepted again on one date; the server's order is stable. */}
          {rows.map((row, index) => (
            <tr key={`${row.BillLineId}-${row.Event}-${index}`} className={row.Event === 'REVERSED' ? 'bg-rose-50/40' : undefined}>
              <td className="whitespace-nowrap text-xs text-slate-600">{formatDateOnly(row.EventDate)}</td>
              <td className="whitespace-nowrap">
                {/* Accounts drill-down only with its own page permission; dashboard access grants none. */}
                <DocLink to={canOpenBills ? `/accounts/vendor-bills/${encodeURIComponent(row.BillId)}` : null}>{row.BillNumber}</DocLink>
                {row.Event === 'REVERSED' && <span className="ml-1 rounded bg-rose-100 px-1 text-[10px] font-medium text-rose-700" title="Reversal: subtracts">Reversed</span>}
              </td>
              <td><DocLink to={poLink(can, row.PoNumber)}>{row.PoNumber}</DocLink></td>
              <td className="max-w-44 truncate text-xs" title={`${row.VendorCode} · ${row.VendorName}`}>
                {vendorLink(can, row.VendorCode) ? <DocLink to={vendorLink(can, row.VendorCode)}>{row.VendorName}</DocLink> : row.VendorName}
              </td>
              <td className="max-w-52 truncate text-xs" title={`${row.CategoryCode} · ${row.ItemCode} ${row.ItemName}`}>
                <span className="text-slate-500">{row.CategoryCode}</span> · <DocLink to={itemLink(can, row.ItemCode)}>{row.ItemCode}</DocLink>
              </td>
              <td className="whitespace-nowrap text-right text-xs tabular-nums">{formatQuantity(row.Quantity)} {row.Uom}</td>
              <td className="text-right text-xs font-medium"
                title={`Billed payable incl. GST ${formatAmount(row.MaterialValue, row.Currency)} + charges ${formatAmount(row.AllocatedCharges, row.Currency)}`}>
                <Money value={row.Amount} currency={row.Currency} />
              </td>
            </tr>
          ))}
        </CompactTable>
      )}
      <CompactPager page={data.Filters.Page} pageSize={data.Filters.PageSize} totalRows={data.TotalRows} onPage={onPage} />
    </>
  )
}

/** Vendor or category spend as bars, one set per currency (never added), each bar a detail filter. */
function GroupBars({ title, groups, onPick }: { title: string; groups: PurchaseSpendingGroup[]; onPick: (group: PurchaseSpendingGroup) => void }) {
  const byCurrency = new Map<string, PurchaseSpendingGroup[]>()
  for (const group of groups) byCurrency.set(group.Currency, [...(byCurrency.get(group.Currency) ?? []), group])
  const currencies = inrFirst([...byCurrency.keys()].map((Currency) => ({ Currency })))
  return (
    <div>
      <div className="mb-1 text-[11px] font-medium uppercase tracking-wide text-slate-500">{title}</div>
      {groups.length === 0 && <div className="text-xs text-slate-400">No spend</div>}
      {currencies.map(({ Currency }) => (
        <div key={Currency} className="mb-1">
          {currencies.length > 1 && <div className="font-mono text-[10px] text-slate-400">{Currency}</div>}
          <MiniBars
            format={(value) => formatCompactAmount(value, Currency)}
            rows={[...(byCurrency.get(Currency) ?? [])].sort((a, b) => b.Amount - a.Amount).slice(0, 5).map((group) => ({
              key: `${group.Id}-${Currency}`,
              // Bars need a length; a net reversal is drawn red and labelled, never shown as positive spend silently.
              label: group.Amount < 0 ? `${group.Name || group.Code} (net reversal −)` : group.Name || group.Code,
              value: Math.abs(group.Amount),
              tone: group.Amount < 0 ? 'bad' : 'info',
              onClick: () => onPick(group),
            }))}
          />
        </div>
      ))}
    </div>
  )
}
