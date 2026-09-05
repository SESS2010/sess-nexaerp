import { useCallback, useEffect, useState, type ReactNode } from 'react'
import type { PagedResponse } from '../../api/client'
import type { PurchaseDocumentListQuery } from '../../api/purchase'
import { useSort, type SortState } from '../../hooks/useSort'
import { ErrorAlert } from '../../components/ErrorAlert'

export interface RegisterColumn<T> {
  header: string
  render: (row: T) => ReactNode
  /** Server sort key (status, date, or the document-number key); omit for unsortable columns. */
  sortKey?: string
  className?: string
}

interface Props<T> {
  title: string
  subtitle: string
  numberPlaceholder: string
  defaultSort: SortState
  fetch: (query: PurchaseDocumentListQuery) => Promise<PagedResponse<T>>
  columns: RegisterColumn<T>[]
  rowKey: (row: T) => string
  onOpen: (row: T) => void
  createLabel?: string
  onCreate?: () => void
  /**
   * Permission gate for the create button. Each consumer passes
   * can(<its page key>, 'create') — plus the role the service demands — so the
   * button is hidden, not disabled, when the grant is missing.
   */
  canCreate?: boolean
  /** Modal or extra content rendered inside the page. */
  children?: ReactNode
  /** Bumping this value reloads the list (e.g. after a create). */
  reloadToken?: number
}

const PAGE_SIZE = 25

/**
 * Shared register for the REV869B purchase documents (RFQ, vendor quotation,
 * commercial comparison, purchase order). All four list endpoints take the
 * same filters — exact number, status, created-date range, vendor — and the
 * same sort keys, so the page chrome lives here once.
 */
export function PurchaseDocumentRegister<T>({
  title,
  subtitle,
  numberPlaceholder,
  defaultSort,
  fetch,
  columns,
  rowKey,
  onOpen,
  createLabel,
  onCreate,
  canCreate = true,
  children,
  reloadToken = 0,
}: Props<T>) {
  const [rows, setRows] = useState<T[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const { sort, toggleSort } = useSort(defaultSort, () => setPage(1))
  const [number, setNumber] = useState('')
  const [status, setStatus] = useState('')
  const [vendor, setVendor] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [applied, setApplied] = useState({ number: '', status: '', vendor: '' })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await fetch({
        page,
        pageSize: PAGE_SIZE,
        number: applied.number || undefined,
        status: applied.status || undefined,
        vendor: applied.vendor || undefined,
        from: from || undefined,
        to: to || undefined,
        sortBy: sort.sortBy,
        sortDirection: sort.sortDirection,
      })
      setRows(data.Items)
      setTotalCount(data.TotalCount)
    } catch (err) {
      setRows([])
      setTotalCount(0)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [fetch, page, applied, from, to, sort])

  useEffect(() => {
    void load()
  }, [load, reloadToken])

  const apply = () => {
    setPage(1)
    setApplied({ number: number.trim().toUpperCase(), status: status.trim().toUpperCase(), vendor: vendor.trim() })
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))
  const arrow = (key?: string) => (key && sort.sortBy === key ? (sort.sortDirection === 'asc' ? ' ▲' : ' ▼') : '')

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>{title}</h1>
          <p className="page-sub">{subtitle} ({totalCount} total)</p>
        </div>
        {onCreate && canCreate && (
          <div className="action-row">
            <button type="button" className="btn btn-primary" onClick={onCreate}>
              {createLabel ?? '+ New'}
            </button>
          </div>
        )}
      </div>

      <div className="toolbar">
        <input className="input search" placeholder={numberPlaceholder} value={number}
          onChange={(event) => setNumber(event.target.value)}
          onKeyDown={(event) => event.key === 'Enter' && apply()} />
        <input className="input" placeholder="Status" value={status}
          onChange={(event) => setStatus(event.target.value)}
          onKeyDown={(event) => event.key === 'Enter' && apply()} />
        <input className="input" placeholder="Vendor code or name" value={vendor}
          onChange={(event) => setVendor(event.target.value)}
          onKeyDown={(event) => event.key === 'Enter' && apply()} />
        <input type="date" className="input" value={from} onChange={(event) => { setFrom(event.target.value); setPage(1) }} />
        <input type="date" className="input" value={to} onChange={(event) => { setTo(event.target.value); setPage(1) }} />
        <button type="button" className="btn btn-ghost" onClick={apply}>Search</button>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages}</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback={`Failed to load ${title.toLowerCase()} records.`} />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              {columns.map((column) => (
                <th
                  key={column.header}
                  className={`${column.className ?? ''}${column.sortKey ? ' sortable' : ''}`.trim() || undefined}
                  onClick={column.sortKey ? () => toggleSort(column.sortKey as string) : undefined}
                  style={column.sortKey ? { cursor: 'pointer' } : undefined}
                >
                  {column.header}{arrow(column.sortKey)}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={columns.length} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={columns.length} className="table-empty">No records match.</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr key={rowKey(row)} className="row-click" onClick={() => onOpen(row)}>
                {columns.map((column) => (
                  <td key={column.header} className={column.className}>{column.render(row)}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {children}
    </div>
  )
}

export function formatDate(value: string | null | undefined): string {
  return value ? new Date(value).toLocaleDateString('en-IN') : '—'
}

export function formatMoney(value: number | null | undefined): string {
  return value === null || value === undefined ? '—' : value.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
