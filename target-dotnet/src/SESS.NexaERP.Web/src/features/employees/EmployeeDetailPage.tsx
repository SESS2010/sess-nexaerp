import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { changeApprovalStatus, getEmployee, getEmployeeHistory, provisionDevLogin, setLoginStatus } from '../../api/employees'
import type { ApprovalAction } from '../../api/employees'
import type { EmployeeDetail, EmployeeHistorySummary } from '../../types/employee'
import { StatusBadge } from './StatusBadge'
import { EmployeeFormModal } from './EmployeeFormModal'

const APPROVAL_ACTIONS: { action: ApprovalAction; label: string; from: string[] }[] = [
  { action: 'submit', label: 'Submit', from: ['Draft', 'RevisionRequested', 'Rejected'] },
  { action: 'approve', label: 'Approve', from: ['Submitted'] },
  { action: 'reject', label: 'Reject', from: ['Submitted'] },
  { action: 'revise', label: 'Request revision', from: ['Submitted', 'Approved'] },
]

export function EmployeeDetailPage() {
  const { employeeCode = '' } = useParams()
  const [detail, setDetail] = useState<EmployeeDetail | null>(null)
  const [history, setHistory] = useState<EmployeeHistorySummary[]>([])
  const [historyError, setHistoryError] = useState('')
  const [tab, setTab] = useState<'profile' | 'roles' | 'history'>('profile')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)

  const load = useCallback(async () => {
    setError('')
    try {
      setDetail(await getEmployee(employeeCode))
    } catch (err) {
      setDetail(null)
      setError(err instanceof Error ? err.message : 'Failed to load employee.')
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
    const remarks = window.prompt(`${label} — enter remarks (required):`)
    if (!remarks || !remarks.trim()) return
    setBusy(true)
    setError('')
    try {
      await changeApprovalStatus(employeeCode, action, remarks.trim())
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed.')
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
    setError('')
    try {
      await setLoginStatus(employeeCode, enable, reason.trim())
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed.')
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
            <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setEditing(true)}>Edit</button>
            {APPROVAL_ACTIONS.filter((item) => item.from.includes(detail.ApprovalStatus)).map((item) => (
              <button key={item.action} type="button" className="btn btn-ghost" disabled={busy} onClick={() => runApproval(item.action, item.label)}>
                {item.label}
              </button>
            ))}
            <button type="button" className={`btn ${detail.LoginEnabled ? 'btn-warn' : 'btn-primary'}`} disabled={busy} onClick={toggleLogin}>
              {detail.LoginEnabled ? 'Deactivate login' : 'Activate login'}
            </button>
            <button
              type="button"
              className="btn btn-primary"
              disabled={busy}
              onClick={async () => {
                const password = window.prompt(`Set login password for ${detail.EmployeeCode} (min 6 characters):`)
                if (!password) return
                const roleCode = window.prompt('ERP role code for this login:', 'SOFTWARE_DEVELOPER')
                if (roleCode === null) return
                setBusy(true)
                setError('')
                try {
                  await provisionDevLogin(employeeCode, password, roleCode.trim())
                  window.alert(`${detail.EmployeeCode} can now sign in with their employee ID and this password.`)
                  await load()
                } catch (err) {
                  setError(err instanceof Error ? err.message : 'Provisioning failed.')
                } finally {
                  setBusy(false)
                }
              }}
            >
              Provision login
            </button>
          </div>
        )}
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {detail && (
        <>
          <div className="tabs">
            <button type="button" className={`tab${tab === 'profile' ? ' active' : ''}`} onClick={() => setTab('profile')}>Profile</button>
            <button type="button" className={`tab${tab === 'roles' ? ' active' : ''}`} onClick={() => setTab('roles')}>Roles ({detail.Roles.length})</button>
            <button type="button" className={`tab${tab === 'history' ? ' active' : ''}`} onClick={() => setTab('history')}>History ({history.length})</button>
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

          {tab === 'history' && (
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
