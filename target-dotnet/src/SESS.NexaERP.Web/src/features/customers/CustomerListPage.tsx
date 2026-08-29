import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listCustomers } from '../../api/customers'
import type { CustomerSummary } from '../../types/customer'
import { StatusBadge } from '../employees/StatusBadge'
import { CustomerFormModal } from './CustomerFormModal'

const STATUS_OPTIONS = ['', 'Draft', 'Pending Approval', 'Active', 'On Hold', 'Inactive', 'Rejected']
const PAGE_SIZE = 20

export function CustomerListPage() {
  const navigate = useNavigate()
  const [rows, setRows] = useState<CustomerSummary[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [showCreate, setShowCreate] = useState(false)

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await listCustomers({ page, pageSize: PAGE_SIZE, search: appliedSearch, status })
      setRows(data.Items)
      setTotalCount(data.TotalCount)
    } catch (err) {
      setRows([])
      setTotalCount(0)
      setError(err instanceof Error ? err.message : 'Failed to load customers.')
    } finally {
      setLoading(false)
    }
  }, [page, appliedSearch, status])

  useEffect(() => {
    void load()
  }, [load])

  const applySearch = () => {
    setPage(1)
    setAppliedSearch(search.trim())
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Customers</h1>
          <p className="page-sub">Customer master with approval workflow ({totalCount} total)</p>
        </div>
        <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
          + New Customer
        </button>
      </div>

      <div className="toolbar">
        <input
          className="input search"
          placeholder="Search by code, name or GSTIN…"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onKeyDown={(event) => event.key === 'Enter' && applySearch()}
        />
        <button type="button" className="btn btn-ghost" onClick={applySearch}>Search</button>
        <select
          className="input"
          value={status}
          onChange={(event) => { setStatus(event.target.value); setPage(1) }}
        >
          {STATUS_OPTIONS.map((option) => (
            <option key={option} value={option}>{option === '' ? 'All statuses' : option}</option>
          ))}
        </select>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages}</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>GSTIN</th>
              <th>PAN</th>
              <th>Status</th>
              <th>Approval</th>
              <th>Active</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr><td colSpan={7} className="table-empty">Loading…</td></tr>
            )}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={7} className="table-empty">No customers found.</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr key={row.Id} className="row-click" onClick={() => navigate(`/customers/${encodeURIComponent(row.CustomerCode)}`)}>
                <td className="mono">{row.CustomerCode}</td>
                <td>{row.Name}</td>
                <td className="mono">{row.GstNumber ?? '—'}</td>
                <td className="mono">{row.PanNumber ?? '—'}</td>
                <td><StatusBadge value={row.Status} /></td>
                <td><StatusBadge value={row.ApprovalStatus} /></td>
                <td>{row.IsActive ? 'Yes' : 'No'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <CustomerFormModal
          mode="create"
          onClose={() => setShowCreate(false)}
          onSaved={(customerCode) => {
            setShowCreate(false)
            navigate(`/customers/${encodeURIComponent(customerCode)}`)
          }}
        />
      )}
    </div>
  )
}
