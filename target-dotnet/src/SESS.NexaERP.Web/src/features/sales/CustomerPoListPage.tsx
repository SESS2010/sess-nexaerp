import { useCallback, useEffect, useState } from 'react'
import { getCustomerPo, getCustomerPoLookups, listCustomerPos } from '../../api/customerPos'
import type { CustomerPoDetail, CustomerPoLookups, CustomerPoSummary } from '../../types/customerPo'
import { StatusBadge } from '../employees/StatusBadge'
import { CustomerPoFormModal } from './CustomerPoFormModal'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { SortableHeader } from '../../components/SortableHeader'
import { useSort } from '../../hooks/useSort'

const PAGE_SIZE = 20

function formatAmount(value: number | null): string {
  if (value === null || value === undefined) return '—'
  return value.toLocaleString('en-IN', { maximumFractionDigits: 2 })
}

function formatDate(value: string | null): string {
  if (!value) return '—'
  const [year, month, day] = value.split('-')
  return `${day}-${month}-${year}`
}

export function CustomerPoListPage() {
  const { can } = useSession()
  const [rows, setRows] = useState<CustomerPoSummary[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  // Documents read newest-first, so the ledger defaults to PO date descending.
  const { sort, toggleSort } = useSort({ sortBy: 'customerpodate', sortDirection: 'desc' }, () => setPage(1))
  const [search, setSearch] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [workStatus, setWorkStatus] = useState('')
  const [fiscalYear, setFiscalYear] = useState('')
  const [lookups, setLookups] = useState<CustomerPoLookups | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [editing, setEditing] = useState<CustomerPoDetail | null>(null)

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listCustomerPos({
        page,
        pageSize: PAGE_SIZE,
        search: appliedSearch,
        workStatus,
        fiscalYear,
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
  }, [page, appliedSearch, workStatus, fiscalYear, sort])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    getCustomerPoLookups().then(setLookups).catch(() => undefined)
  }, [])

  const applySearch = () => {
    setPage(1)
    setAppliedSearch(search.trim())
  }

  const openEdit = async (poRecordNumber: string) => {
    try {
      setEditing(await getCustomerPo(poRecordNumber))
    } catch (err) {
      setError(err)
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Customer PO</h1>
          <p className="page-sub">PO ledger — sales flow starts when a customer PO is received ({totalCount} total)</p>
        </div>
        <div className="action-row">
          {can(PAGE_KEYS.customerPo, 'create') && (
            <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
              + New Customer PO
            </button>
          )}
        </div>
      </div>

      <div className="toolbar">
        <input
          className="input search"
          placeholder="Search PO no, customer, invoice, quote…"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onKeyDown={(event) => event.key === 'Enter' && applySearch()}
        />
        <button type="button" className="btn btn-ghost" onClick={applySearch}>Search</button>
        <select className="input" value={workStatus} onChange={(event) => { setWorkStatus(event.target.value); setPage(1) }}>
          <option value="">All work statuses</option>
          {lookups?.WorkStatuses.map((option) => <option key={option} value={option}>{option}</option>)}
        </select>
        <select className="input" value={fiscalYear} onChange={(event) => { setFiscalYear(event.target.value); setPage(1) }}>
          <option value="">All years</option>
          {lookups?.FiscalYears.map((option) => <option key={option} value={option}>{option}</option>)}
        </select>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages}</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load customer POs." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <SortableHeader label="Record" sortKey="porecordnumber" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Customer PO No" sortKey="customerponumber" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="PO Date" sortKey="customerpodate" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Customer" sortKey="customername" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Sales Type" sortKey="salestype" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Amount (₹)" sortKey="totalamountwithgst" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Work Status" sortKey="workstatus" sort={sort} onSort={toggleSort} disabled={loading} />
              <th>Revision</th>
              <SortableHeader label="FY" sortKey="fiscalyear" sort={sort} onSort={toggleSort} disabled={loading} />
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr><td colSpan={9} className="table-empty">Loading…</td></tr>
            )}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={9} className="table-empty">No customer POs found.</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr key={row.Id} className="row-click" onClick={() => void openEdit(row.PoRecordNumber)}>
                <td className="mono">{row.PoRecordNumber}</td>
                <td className="mono">{row.CustomerPoNumber}</td>
                <td>{formatDate(row.CustomerPoDate)}</td>
                <td>{row.CustomerName}</td>
                <td>{row.SalesType ?? '—'}</td>
                <td className="text-right mono">{formatAmount(row.TotalAmountWithGst)}</td>
                <td><StatusBadge value={row.WorkStatus} /></td>
                <td className="mono">R{row.CurrentRevisionNumber}</td>
                <td>{row.FiscalYear ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <CustomerPoFormModal
          mode="create"
          onClose={() => setShowCreate(false)}
          onSaved={() => {
            setShowCreate(false)
            void load()
          }}
        />
      )}
      {editing && (
        <CustomerPoFormModal
          mode="edit"
          existing={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            void load()
          }}
        />
      )}
    </div>
  )
}
