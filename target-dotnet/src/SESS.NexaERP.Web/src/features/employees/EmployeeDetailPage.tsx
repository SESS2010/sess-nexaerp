import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { changeApprovalStatus, getEmployee, getEmployeeHistory, setLoginStatus } from '../../api/employees'
import type { ApprovalAction } from '../../api/employees'
import type { EmployeeDetail, EmployeeHistorySummary } from '../../types/employee'
import { StatusBadge } from './StatusBadge'
import { EmployeeFormModal } from './EmployeeFormModal'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'

// GET /{employeeCode}/history is guarded by "employees.audit-history" with
// PagePermissionActions.ViewAuditHistory (EmployeeEndpoints.cs); not in PAGE_KEYS.
const PAGE_AUDIT_HISTORY = 'employees.audit-history'

// `permission` is the PagePermissionActions value each route requires on
// employees.master (Submit, Approve, Reject, RequestRevision).
const APPROVAL_ACTIONS: { action: ApprovalAction; label: string; from: string[]; permission: string }[] = [
  { action: 'submit', label: 'Submit', from: ['Draft', 'RevisionRequested', 'Rejected'], permission: 'submit' },
  { action: 'approve', label: 'Approve', from: ['Submitted'], permission: 'approve' },
  { action: 'reject', label: 'Reject', from: ['Submitted'], permission: 'reject' },
  { action: 'revise', label: 'Request revision', from: ['Submitted', 'Approved'], permission: 'request-revision' },
]

export function EmployeeDetailPage() {
  const { employeeCode = '' } = useParams()
  const { can } = useSession()
  const [detail, setDetail] = useState<EmployeeDetail | null>(null)
  const [history, setHistory] = useState<EmployeeHistorySummary[]>([])
  const [historyError, setHistoryError] = useState('')
  const [tab, setTab] = useState<'profile' | 'roles' | 'history'>('profile')
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      setDetail(await getEmployee(employeeCode))
    } catch (err) {
      setDetail(null)
      setError(err)
    }
    try {
      setHistory(await getEmployeeHistory(employeeCode))
      setHistoryError('')
    } catch (err) {
      setHistory([])
      setHistoryError(err instanceof Error ? err.message : 'History unavailable.')
    }
  }, [employeeCode])

  useEffect(() => {
    void load()
  }, [load])

  const runApproval = async (action: ApprovalAction, label: string) => {
    if (!detail) return
    const remarks = window.prompt(`${label} — enter remarks (required):`)
    if (!remarks || !remarks.trim()) return
    setBusy(true)
    setError(null)
    try {
      await changeApprovalStatus(employeeCode, action, remarks.trim(), detail.Version)
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const toggleLogin = async () => {
    if (!detail) return
    const enable = !detail.LoginEnabled
    const reason = window.prompt(`${enable ? 'Activate' : 'Deactivate'} login — enter reason (required):`)
    if (!reason || !reason.trim()) return
    setBusy(true)
    setError(null)
    try {
      await setLoginStatus(employeeCode, enable, reason.trim(), detail.Version)
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="breadcrumbs">
            <Link to="/employees">Employees</Link> / <span className="mono">{employeeCode}</span>
          </div>
          <h1>{detail ? detail.EmployeeName : employeeCode}</h1>
          {detail && (
            <p className="page-sub">
              <StatusBadge value={detail.Status} /> <StatusBadge value={detail.ApprovalStatus} />{' '}
              Login: {detail.LoginEnabled ? 'Enabled' : 'Disabled'}
            </p>
          )}
        </div>
        {detail && (
          <div className="action-row">
            {can(PAGE_KEYS.employees, 'update') && (
              <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setEditing(true)}>Edit</button>
            )}
            {APPROVAL_ACTIONS.filter((item) => item.from.includes(detail.ApprovalStatus) && can(PAGE_KEYS.employees, item.permission)).map((item) => (
              <button key={item.action} type="button" className="btn btn-ghost" disabled={busy} onClick={() => runApproval(item.action, item.label)}>
                {item.label}
              </button>
            ))}
            {/* activate-login requires Update, deactivate-login requires Deactivate. */}
            {can(PAGE_KEYS.employees, detail.LoginEnabled ? 'deactivate' : 'update') && (
              <button type="button" className={`btn ${detail.LoginEnabled ? 'btn-warn' : 'btn-primary'}`} disabled={busy} onClick={toggleLogin}>
                {detail.LoginEnabled ? 'Deactivate login' : 'Activate login'}
              </button>
            )}
          </div>
        )}
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />

      {detail && (
        <>
          <div className="tabs">
            <button type="button" className={`tab${tab === 'profile' ? ' active' : ''}`} onClick={() => setTab('profile')}>Profile</button>
            <button type="button" className={`tab${tab === 'roles' ? ' active' : ''}`} onClick={() => setTab('roles')}>Roles ({detail.Roles.length})</button>
            {can(PAGE_AUDIT_HISTORY, 'view-audit-history') && (
              <button type="button" className={`tab${tab === 'history' ? ' active' : ''}`} onClick={() => setTab('history')}>History ({history.length})</button>
            )}
          </div>

          {tab === 'profile' && (
            <div className="card detail-grid">
              <Field label="Employee code" value={detail.EmployeeCode} mono />
              <Field label="Employee name" value={detail.EmployeeName} />
              <Field label="Imported name" value={detail.OriginalImportedName} />
              <Field label="Type" value={detail.EmployeeType} />
              <Field label="Grade" value={detail.Grade} />
              <Field label="Department" value={detail.Department} />
              <Field label="Designation" value={detail.JobDesignation} />
              <Field label="Skills" value={detail.SkillCategories.join(', ') || '—'} />
              <Field label="Date of joining" value={detail.DateOfJoining ?? '—'} />
              <Field label="Official email" value={detail.OfficialEmail ?? '—'} />
              <Field label="Mobile" value={detail.MobileNumber ?? '—'} />
            </div>
          )}

          {tab === 'roles' && (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr><th>Role code</th><th>Role name</th><th>Effective from</th><th>Effective to</th><th>Approval</th><th>Remarks</th></tr>
                </thead>
                <tbody>
                  {detail.Roles.length === 0 && <tr><td colSpan={6} className="table-empty">No role assignments.</td></tr>}
                  {detail.Roles.map((role) => (
                    <tr key={role.Id}>
                      <td className="mono">{role.RoleCode}</td>
                      <td>{role.RoleName}</td>
                      <td>{role.EffectiveFrom}</td>
                      <td>{role.EffectiveTo ?? '—'}</td>
                      <td><StatusBadge value={role.ApprovalStatus} /></td>
                      <td>{role.Remarks}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {tab === 'history' && can(PAGE_AUDIT_HISTORY, 'view-audit-history') && (
            <div className="table-wrap">
              {historyError && <div className="alert alert-error">{historyError}</div>}
              <table className="table">
                <thead>
                  <tr><th>When</th><th>Action</th><th>From</th><th>To</th><th>Remarks</th><th>By</th></tr>
                </thead>
                <tbody>
                  {history.length === 0 && !historyError && <tr><td colSpan={6} className="table-empty">No history records.</td></tr>}
                  {history.map((item) => (
                    <tr key={item.Id}>
                      <td>{new Date(item.CreatedAt).toLocaleString()}</td>
                      <td>{item.Action}</td>
                      <td><StatusBadge value={item.FromStatus} /></td>
                      <td><StatusBadge value={item.ToStatus} /></td>
                      <td>{item.Remarks}</td>
                      <td>{item.CreatedBy}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}

      {editing && detail && (
        <EmployeeFormModal
          mode="edit"
          existing={detail}
          onClose={() => setEditing(false)}
          onSaved={() => { setEditing(false); void load() }}
        />
      )}
    </div>
  )
}

function Field({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="detail-field">
      <div className="field-label">{label}</div>
      <div className={mono ? 'mono' : undefined}>{value}</div>
    </div>
  )
}
