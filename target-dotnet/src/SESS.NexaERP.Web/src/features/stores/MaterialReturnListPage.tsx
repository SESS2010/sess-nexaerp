import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listMaterialReturns } from '../../api/materialIssues'
import type { MaterialReturnView } from '../../types/materialIssue'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession } from '../auth/SessionContext'

const PAGE_SIZE = 25
const RETURN_STATES = ['SUBMITTED', 'ACCEPTED'] as const

/** GET /api/v1/stores/material-returns — declared by engineers, accepted by Stores. */
export function MaterialReturnListPage() {
  const navigate = useNavigate()
  const { me } = useSession()
  const [rows, setRows] = useState<MaterialReturnView[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listMaterialReturns({ page, pageSize: PAGE_SIZE, status: status || undefined })
      setRows(data.Items ?? [])
      setTotal(data.Total)
    } catch (err) {
      setRows([])
      setTotal(0)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [page, status])

  useEffect(() => {
    void load()
  }, [load])

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Material Returns</h1>
          <p className="page-sub">Engineers declare what came back, was consumed or is still held; Stores accepts and custody returns to Stores. Open a return's issue to accept it.</p>
        </div>
      </div>

      <div className="toolbar">
        <select className="input" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="">All states</option>
          {RETURN_STATES.map((option) => <option key={option} value={option}>{option}</option>)}
        </select>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages} · {total} total</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load material returns." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th>Return</th><th>Issue</th><th>Declared</th><th>By</th><th className="text-right">Lines</th><th className="text-right">Returned (base)</th><th>Status</th><th>Accepted</th></tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={8} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && <tr><td colSpan={8} className="table-empty">No material returns yet.</td></tr>}
            {!loading && rows.map((row) => (
              <tr key={row.Id} className="row-click" onClick={() => navigate(`/stores/material-issues/${row.MaterialIssueId}`)}>
                <td className="mono">{row.ReturnNumber}</td>
                <td className="mono">{row.MaterialIssueId.slice(0, 8)}…</td>
                <td>{new Date(row.DeclaredAt).toLocaleString()}</td>
                <td className="mono">{row.ReturnedByEmployeeId === me?.EmployeeId ? `${me?.EmployeeCode} (you)` : row.ReturnedByEmployeeId}</td>
                <td className="text-right mono">{row.Lines.length}</td>
                <td className="text-right mono">{row.Lines.reduce((sum, line) => sum + line.ReturnedQuantityBase, 0)}</td>
                <td><StatusBadge value={row.Status} /></td>
                <td>{row.AcceptedAt ? new Date(row.AcceptedAt).toLocaleString() : '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
