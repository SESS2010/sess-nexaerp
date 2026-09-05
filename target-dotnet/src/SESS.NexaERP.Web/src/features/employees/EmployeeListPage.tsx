import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listEmployees } from '../../api/employees'
import type { EmployeeSummary } from '../../types/employee'
import { StatusBadge } from './StatusBadge'
import { EmployeeFormModal } from './EmployeeFormModal'
import { ErrorAlert } from '../../components/ErrorAlert'
import { SortableHeader } from '../../components/SortableHeader'
import { useSort } from '../../hooks/useSort'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'

const STATUS_OPTIONS = ['', 'Active', 'Inactive']
const PAGE_SIZE = 20

export function EmployeeListPage() {
  const navigate = useNavigate()
  const { can } = useSession()
  const [rows, setRows] = useState<EmployeeSummary[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [showCreate, setShowCreate] = useState(false)
  const { sort, toggleSort } = useSort({ sortBy: 'employeecode', sortDirection: 'asc' }, () => setPage(1))

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listEmployees({
        page,
        pageSize: PAGE_SIZE,
        search: appliedSearch,
        status,
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
  }, [page, appliedSearch, status, sort.sortBy, sort.sortDirection])

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
        {can(PAGE_KEYS.employees, 'create') && (
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Employee
          </button>
        )}
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
          <span className="pager-label">Page {page} of {Math.max(1, Math.ceil(totalCount / PAGE_SIZE))} · {totalCount} total</span>
          <button type="button" className="btn btn-ghost" disabled={page * PAGE_SIZE >= totalCount || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load employees." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <SortableHeader label="Code" sortKey="employeecode" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Name" sortKey="employeename" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Type" sortKey="employeetype" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Grade" sortKey="grade" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Department" sortKey="department" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Designation" sortKey="jobdesignation" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Skill" sortKey="skillcategory" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Status" sortKey="status" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Approval" sortKey="approvalstatus" sort={sort} onSort={toggleSort} disabled={loading} />
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
