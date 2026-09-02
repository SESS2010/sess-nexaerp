import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listEmployees } from '../../api/employees'
import type { EmployeeSummary } from '../../types/employee'
import { StatusBadge } from './StatusBadge'
import { EmployeeFormModal } from './EmployeeFormModal'
import { ErrorAlert } from '../../components/ErrorAlert'

const STATUS_OPTIONS = ['', 'Active', 'Inactive']
const PAGE_SIZE = 20

export function EmployeeListPage() {
  const navigate = useNavigate()
  const [rows, setRows] = useState<EmployeeSummary[]>([])
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
      const data = await listEmployees({ page, pageSize: PAGE_SIZE, search: appliedSearch, status })
      setRows(data)
    } catch (err) {
      setRows([])
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

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Employees</h1>
          <p className="page-sub">Employee master with approval workflow and login control</p>
        </div>
        <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
          + New Employee
        </button>
      </div>

      <div className="toolbar">
        <input
          className="input search"
          placeholder="Search by code or name…"
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
          <span className="pager-label">Page {page}</span>
          <button type="button" className="btn btn-ghost" disabled={rows.length < PAGE_SIZE || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load employees." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Type</th>
              <th>Grade</th>
              <th>Department</th>
              <th>Designation</th>
              <th>Skill</th>
              <th>Status</th>
              <th>Approval</th>
              <th>Login</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr><td colSpan={10} className="table-empty">Loading…</td></tr>
            )}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={10} className="table-empty">No employees found.</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr key={row.Id} className="row-click" onClick={() => navigate(`/employees/${encodeURIComponent(row.EmployeeCode)}`)}>
                <td className="mono">{row.EmployeeCode}</td>
                <td>{row.EmployeeName}</td>
                <td>{row.EmployeeType}</td>
                <td>{row.Grade}</td>
                <td>{row.Department}</td>
                <td>{row.JobDesignation}</td>
                <td>{row.SkillCategory}</td>
                <td><StatusBadge value={row.Status} /></td>
                <td><StatusBadge value={row.ApprovalStatus} /></td>
                <td>{row.LoginEnabled ? 'Enabled' : 'Disabled'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <EmployeeFormModal
          mode="create"
          onClose={() => setShowCreate(false)}
          onSaved={(detail) => {
            setShowCreate(false)
            navigate(`/employees/${encodeURIComponent(detail.EmployeeCode)}`)
          }}
        />
      )}
    </div>
  )
}
