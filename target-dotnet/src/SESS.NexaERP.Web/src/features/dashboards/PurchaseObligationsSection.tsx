import { useState } from 'react'
import { getPurchaseObligations } from '../../api/dashboards'
import type { PurchaseObligationQueue, PurchaseObligationsPage } from '../../types/dashboard'
import { countOf, formatAge, formatCount, formatDateOnly, formatQuantity } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { goodsReceiptLink, itemLink, poLink, vendorLink } from './dashboardAccess'
import {
  BasisNote, DetailFilterNote, MaybeLink, Money, NullValue, Pager, QueryProblem, SectionFrame, StateNotice,
} from './DashboardParts'
import { useDashboardQuery } from './useDashboardQuery'

const PAGE_SIZE = 50

/** What each obligation figure is NOT, in plain words, beside the server's Basis. */
const QUEUE_CAUTION: Record<string, string> = {
  grni: 'Provisional receipt value at the quoted unit rate before GST and added charges. It does not net discounts or later bill adjustments, and it is not landed or Actual BOM cost.',
  'vendor-advances': 'Cash paid to vendors ahead of billing. These are not ex-tax purchase costs.',
}

interface Filters {
  queue: PurchaseObligationQueue | null
  vendor: { id: string; label: string } | null
  currency: string | null
  document: { id: string; label: string } | null
}

const NO_FILTERS: Filters = { queue: null, vendor: null, currency: null, document: null }

export function PurchaseObligationsSection() {
  const { can } = useSession()
  const [filters, setFilters] = useState<Filters>(NO_FILTERS)
  const [page, setPage] = useState(1)

  const { state, reload } = useDashboardQuery(
    () => getPurchaseObligations({
      queue: filters.queue,
      vendorId: filters.vendor?.id,
      currency: filters.currency,
      documentId: filters.document?.id,
      page,
      pageSize: PAGE_SIZE,
    }),
    `${filters.queue}|${filters.vendor?.id}|${filters.currency}|${filters.document?.id}|${page}`,
  )

  const apply = (next: Partial<Filters>) => {
    setFilters((current) => ({ ...current, ...next }))
    setPage(1)
  }

  const data = state.kind === 'ready' ? state.data : null
  return (
    <SectionFrame
      id="purchase-obligations"
      title="Obligations"
      subtitle="Goods received but not billed, and vendor advances still outstanding."
      generatedAt={data?.GeneratedAt}
      timeZone={data?.TimeZone}
    >
      {state.kind === 'loading' && <StateNotice kind="loading" title="Loading obligations…" />}
      {(state.kind === 'error' || state.kind === 'company-mismatch') && <QueryProblem state={state} onRetry={reload} />}
      {data && <ObligationsBody data={data} can={can} onFilter={apply} onClear={() => apply(NO_FILTERS)} filters={filters} onPage={setPage} />}
    </SectionFrame>
  )
}

function ObligationsBody({ data, can, filters, onFilter, onClear, onPage }: {
  data: PurchaseObligationsPage
  can: (pageKey: string, action?: string) => boolean
  filters: Filters
  onFilter: (next: Partial<Filters>) => void
  onClear: () => void
  onPage: (page: number) => void
}) {
  const nothingOutstanding = data.Tiles.every((tile) => tile.Count === 0) && data.TotalRows === 0
  const titleOf = (key: string) => data.Tiles.find((tile) => tile.Key === key)?.Title ?? key

  const activeFilters: string[] = []
  if (data.Filters.Queue) activeFilters.push(titleOf(data.Filters.Queue))
  if (filters.vendor) activeFilters.push(`vendor ${filters.vendor.label}`)
  if (data.Filters.Currency) activeFilters.push(`currency ${data.Filters.Currency}`)
  if (filters.document) activeFilters.push(`document ${filters.document.label}`)

  return (
    <>
      {nothingOutstanding && (
        <StateNotice kind="empty" title="No outstanding obligations">
          You are permitted to see obligations, and nothing is outstanding.
        </StateNotice>
      )}

      <div className="grid gap-3 my-3" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))' }}>
        {data.Tiles.map((tile) => {
          const selected = data.Filters.Queue === tile.Key
          return (
            <button key={tile.Key} type="button" className="home-tile text-left"
              style={{ outline: selected ? '2px solid var(--color-accent)' : undefined }}
              onClick={() => onFilter({ queue: selected ? null : (tile.Key as PurchaseObligationQueue) })}
              title={selected ? 'Show all obligations' : 'Show only this kind in the detail rows'}>
              <div className="home-tile-title">{tile.Title}</div>
              <div className="text-2xl font-semibold"><span className="mono">{formatCount(tile.Count)}</span> <span className="text-sm font-normal">{tile.Count === 1 ? 'document' : 'documents'}</span></div>
              <div className="field-hint">{tile.OldestAgeDays === null ? 'No age' : `Oldest ${formatAge(tile.OldestAgeDays)}`}</div>
              <div className="mt-1">
                {tile.Amounts.length === 0 ? (
                  <NullValue reason="none" />
                ) : (
                  tile.Amounts.map((amount) => (
                    <div key={amount.Currency}>
                      <Money value={amount.Value} currency={amount.Currency} />
                      <span className="field-hint"> · {countOf(amount.DocumentCount, 'doc')}, {countOf(amount.LineCount, 'line')}</span>
                    </div>
                  ))
                )}
              </div>
              <div className="field-hint mt-2"><strong>Basis:</strong> {tile.Basis}</div>
              {QUEUE_CAUTION[tile.Key] && <div className="field-hint">{QUEUE_CAUTION[tile.Key]}</div>}
            </button>
          )
        })}
      </div>

      {data.Vendors.length > 0 && (
        <>
          <h3 className="form-section-title">By vendor</h3>
          <div className="table-wrap mb-3">
            <table className="table">
              <thead>
                <tr>
                  <th>Kind</th>
                  <th>Vendor</th>
                  <th>Currency</th>
                  <th className="text-right">Documents</th>
                  <th className="text-right">Value</th>
                  <th className="text-right">Oldest</th>
                </tr>
              </thead>
              <tbody>
                {data.Vendors.map((group) => (
                  <tr key={`${group.Queue}-${group.VendorId}-${group.Currency}`} className="row-click"
                    title="Show only this vendor, kind and currency in the detail rows"
                    onClick={() => onFilter({ queue: group.Queue as PurchaseObligationQueue, vendor: { id: group.VendorId, label: group.VendorCode }, currency: group.Currency })}>
                    <td>{titleOf(group.Queue)}</td>
                    <td>
                      <MaybeLink to={vendorLink(can, group.VendorCode)}><span className="mono">{group.VendorCode}</span></MaybeLink>
                      <div className="field-hint">{group.VendorName}</div>
                    </td>
                    <td className="mono">{group.Currency}</td>
                    <td className="text-right">{formatCount(group.DocumentCount)}</td>
                    <td className="text-right"><Money value={group.Value} currency={group.Currency} /></td>
                    <td className="text-right">{group.OldestAgeDays === null ? <NullValue reason="none" /> : formatAge(group.OldestAgeDays)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      <DetailFilterNote active={activeFilters} onClear={onClear} />

      {data.Rows.length === 0 ? (
        !nothingOutstanding && <StateNotice kind="empty" title="No detail rows match this selection" />
      ) : (
        <div className="table-wrap">
          <BasisNote>Each row's value follows the basis of its kind above; GRNI and advances are different measures and are never added together.</BasisNote>
          <table className="table">
            <thead>
              <tr>
                <th>Document</th>
                <th>PO</th>
                <th>Vendor</th>
                <th>Item</th>
                <th>Date</th>
                <th className="text-right">Age</th>
                <th className="text-right">Quantity / rate</th>
                <th className="text-right">Outstanding</th>
              </tr>
            </thead>
            <tbody>
              {data.Rows.map((row) => {
                const isGrni = row.Queue === 'grni'
                return (
                  <tr key={`${row.Queue}-${row.DocumentId}-${row.LineId ?? 'advance'}`}>
                    <td className="mono">
                      {isGrni ? (
                        <MaybeLink to={goodsReceiptLink(can, row.DocumentId)}>{row.DocumentNumber}</MaybeLink>
                      ) : (
                        <button type="button" className="link-button mono" title="Show only this advance"
                          onClick={() => onFilter({ document: { id: row.DocumentId, label: row.DocumentNumber } })}>
                          {row.DocumentNumber}
                        </button>
                      )}
                      <div className="field-hint">{titleOf(row.Queue)}</div>
                    </td>
                    <td className="mono"><MaybeLink to={poLink(can, row.PoNumber)}>{row.PoNumber}</MaybeLink></td>
                    <td>
                      <MaybeLink to={vendorLink(can, row.VendorCode)}><span className="mono">{row.VendorCode}</span></MaybeLink>
                      <div className="field-hint">{row.VendorName}</div>
                    </td>
                    <td>
                      {row.ItemCode ? (
                        <>
                          <MaybeLink to={itemLink(can, row.ItemCode)}><span className="mono">{row.ItemCode}</span></MaybeLink>
                          <div className="field-hint">{row.ItemName}</div>
                        </>
                      ) : (
                        <span className="field-hint">Advance, no item line</span>
                      )}
                    </td>
                    <td>{formatDateOnly(row.SourceDate)}<div className="field-hint">{isGrni ? 'received' : 'paid'}</div></td>
                    <td className="text-right">{formatAge(row.AgeDays)}</td>
                    <td className="text-right">
                      {isGrni ? (
                        <>
                          <span className="mono">{row.Quantity === null ? '' : `${formatQuantity(row.Quantity)} ${row.Uom ?? ''}`}</span>
                          <div className="field-hint">at <Money value={row.UnitRate} currency={row.Currency} nullReason="none" /> before GST</div>
                        </>
                      ) : (
                        <span className="field-hint">
                          paid <Money value={row.OriginalAmount} currency={row.Currency} nullReason="none" />
                          {' '}less adjusted <Money value={row.AdjustedAmount} currency={row.Currency} nullReason="none" />
                        </span>
                      )}
                    </td>
                    <td className="text-right"><Money value={row.Value} currency={row.Currency} /></td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
      <Pager page={data.Filters.Page} pageSize={data.Filters.PageSize} totalRows={data.TotalRows} onPage={onPage} />
    </>
  )
}
