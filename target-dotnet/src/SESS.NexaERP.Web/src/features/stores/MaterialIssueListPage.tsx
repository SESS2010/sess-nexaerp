import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listOutstandingCustody } from '../../api/materialIssues'
import type { OutstandingEngineerCustodyView } from '../../types/materialIssue'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession } from '../auth/SessionContext'

/**
 * GET /api/v1/stores/material-issues/outstanding-custody — every issue whose
 * material is still with an engineer. There is no plain issue list endpoint;
 * an issue is reached from here, from its MIR, or from a return.
 */
export function MaterialIssueListPage() {
  const navigate = useNavigate()
  const { me } = useSession()
  const [rows, setRows] = useState<OutstandingEngineerCustodyView[]>([])
  const [mineOnly, setMineOnly] = useState(false)
  const [dueOnly, setDueOnly] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setRows(await listOutstandingCustody(mineOnly ? me?.EmployeeId : undefined, dueOnly ? true : undefined))
    } catch (err) {
      setRows([])
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [mineOnly, dueOnly, me])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Material Issues — outstanding custody</h1>
          <p className="page-sub">
            Material issued to engineers and not yet fitted or returned. A return is due one day after issue; Stores issues from an approved MIR.
          </p>
        </div>
      </div>

      <div className="toolbar">
        <label className="pager-label"><input type="checkbox" checked={mineOnly} onChange={(event) => setMineOnly(event.target.checked)} /> Only my custody</label>
        <label className="pager-label"><input type="checkbox" checked={dueOnly} onChange={(event) => setDueOnly(event.target.checked)} /> Only return-due</label>
        <div className="spacer" />
        <span className="pager-label">{rows.length} issue(s)</span>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Outstanding custody could not be loaded (Stores roles only)." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th>Issue</th><th>Custodian</th><th>Job Order</th><th>Issued</th><th>Return due</th><th className="text-right">Qty (base)</th><th>Notice</th></tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={7} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && <tr><td colSpan={7} className="table-empty">No outstanding custody.</td></tr>}
            {!loading && rows.map((row) => (
              <tr key={row.MaterialIssueId} className="row-click" onClick={() => navigate(`/stores/material-issues/${row.MaterialIssueId}`)}>
                <td className="mono">{row.IssueNumber}</td>
                <td className="mono">{row.EmployeeId === me?.EmployeeId ? `${row.EmployeeCode} (you)` : row.EmployeeCode}</td>
                <td className="mono">{row.JobOrderId ? row.JobOrderId.slice(0, 8) + '…' : '—'}</td>
                <td>{new Date(row.IssuedAt).toLocaleString()}</td>
                <td>{new Date(row.ReturnDueAt).toLocaleString()}</td>
                <td className="text-right mono">{row.QuantityBase}</td>
                <td>{row.ReturnNotificationDue ? <StatusBadge value="Overdue" /> : '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
