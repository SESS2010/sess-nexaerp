import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listJobOrders } from '../../api/production'
import type { JobOrderSummary } from '../../types/production'
import { JOB_ORDER_STATES } from '../../types/production'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { JobOrderFormModal } from './JobOrderFormModal'

const PAGE_SIZE = 25

/**
 * GET /api/v1/production/job-orders. One machine per (Company, Customer PO
 * line, MachineOrdinal). Production initiates, a different Accounts employee
 * confirms; until then the job is PENDING_ACCOUNTS and nothing downstream
 * (Estimated BOM, Production BOM, job-backed MIR) can start.
 */
export function JobOrderListPage() {
  const navigate = useNavigate()
  const { can } = useSession()
  const [rows, setRows] = useState<JobOrderSummary[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [showCreate, setShowCreate] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listJobOrders({ page, pageSize: PAGE_SIZE, search: appliedSearch || undefined, status: status || undefined })
      setRows(data.Items ?? [])
      setTotal(data.TotalCount)
    } catch (err) {
      setRows([])
      setTotal(0)
      setError(err)
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

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const canCreate = can(PAGE_KEYS.jobOrders, 'create')

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Job Orders</h1>
          <p className="page-sub">
            One Job Order per machine on a Customer PO line. Production initiates; a different Accounts employee confirms before any BOM or material request can start.
          </p>
        </div>
        <div className="action-row">
          {canCreate && (
            <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>+ New Job Order</button>
          )}
        </div>
      </div>

      <div className="toolbar">
        <input
          className="input search"
          placeholder="Job order, customer PO, serial or customer…"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onKeyDown={(event) => event.key === 'Enter' && applySearch()}
        />
        <button type="button" className="btn btn-ghost" onClick={applySearch}>Search</button>
        <select className="input" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="">All states</option>
          {JOB_ORDER_STATES.map((option) => <option key={option} value={option}>{option.replaceAll('_', ' ')}</option>)}
        </select>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages} · {total} total</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load job orders." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Job Order</th>
              <th>Customer</th>
              <th>Customer PO</th>
              <th>Machine</th>
              <th>Serial</th>
              <th>Date</th>
              <th>Planned completion</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={8} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={8} className="table-empty">No job orders yet.{canCreate ? ' Create one with “+ New Job Order”.' : ''}</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr key={row.Id} className="row-click" onClick={() => navigate(`/production/job-orders/${row.Id}`)}>
                <td className="mono">{row.JobOrderNumber}</td>
                <td>{row.CustomerName}</td>
                <td className="mono">{row.CustomerPoNumber}</td>
                <td>{row.MachineModel} <span className="field-hint" style={{ display: 'inline' }}>#{row.MachineOrdinal}</span></td>
                <td className="mono">{row.MachineSerial}</td>
                <td>{row.JobOrderDate}</td>
                <td>{row.PlannedCompletionDate ?? '—'}</td>
                <td><StatusBadge value={row.Status} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <JobOrderFormModal
          onClose={() => setShowCreate(false)}
          onSaved={(result) => {
            setShowCreate(false)
            navigate(`/production/job-orders/${result.Id}`)
          }}
        />
      )}
    </div>
  )
}
