import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { confirmJobOrderAccounts, getFatReadiness, getJobOrder, getJobOrderHistory } from '../../api/production'
import { newIdempotencyKey } from '../../api/stores'
import type { JobOrderHistoryView, JobOrderView } from '../../types/production'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { ActualBomPanel } from './ActualBomPanel'
import { FatReadinessPanel } from './FatReadinessPanel'

type Pane = 'fat' | 'actual-bom' | 'history'

export function JobOrderDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { me, can } = useSession()
  const [job, setJob] = useState<JobOrderView | null>(null)
  const [history, setHistory] = useState<JobOrderHistoryView[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [pane, setPane] = useState<Pane>('fat')
  /** True when only the FAT readiness projection could be read (see load). */
  const [fatOnly, setFatOnly] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setJob(await getJobOrder(id))
    } catch (err) {
      setJob(null)
      setError(err)
      // QC_MANAGER (production.fat-readiness:verify) holds no
      // production.job-orders:view, so GET /job-orders/{id} is 403 while
      // GET /job-orders/{id}/fat-readiness/ is readable. Fall back to the FAT
      // view so the reconciliation can still be run from this page.
      // Not gated on can(): this callback is created before /session/me has
      // answered, so the permission list may still be empty here. The server
      // decides; a 403 on the FAT read keeps the original error.
      {
        try {
          const fat = await getFatReadiness(id)
          setJob({
            Id: fat.JobOrderId,
            JobOrderNumber: fat.JobOrderNumber,
            FatReadinessStatus: fat.FatReadinessStatus,
            FatReconciledAt: fat.FatReconciledAt,
            FatReconciledByEmployeeId: fat.FatReconciledByEmployeeId,
            LatestFatReconciliationId: fat.LatestFatReconciliationId,
          } as unknown as JobOrderView)
          setFatOnly(true)
          setError(null)
        } catch {
          // keep the original error
        }
      }
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  const canHistory = can(PAGE_KEYS.jobOrders, 'view-audit-history')
  useEffect(() => {
    if (pane !== 'history' || !canHistory || history) return
    getJobOrderHistory(id).then(setHistory).catch(setError)
  }, [pane, canHistory, history, id])

  const confirm = async () => {
    if (!job) return
    if (!reason.trim()) {
      setError('A reason is required to confirm — it becomes the Accounts confirmation record.')
      return
    }
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      const result = await confirmJobOrderAccounts(job.Id, {
        ExpectedVersion: job.Version,
        Reason: reason.trim(),
        IdempotencyKey: newIdempotencyKey('job-order-confirm'),
      })
      setJob(result)
      setHistory(null)
      setReason('')
      setNotice(`${result.JobOrderNumber} is now ${result.Status.replaceAll('_', ' ')}. Estimated BOM, Production BOM and job-backed MIRs can start.`)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  if (loading && !job) {
    return <div className="page"><p>Loading…</p></div>
  }

  if (!job) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Job order not found." />
      </div>
    )
  }

  if (fatOnly) {
    return (
      <div className="page">
        <div className="page-header">
          <div>
            <h1 className="mono">{job.JobOrderNumber}</h1>
            <p className="page-sub">FAT readiness only — your role cannot read the Job Order itself (production.job-orders:view).</p>
          </div>
          <button type="button" className="btn btn-ghost" onClick={() => navigate('/production/job-orders')}>← Back</button>
        </div>
        <FatReadinessPanel jobOrder={job} onChanged={() => void load()} />
      </div>
    )
  }

  const pending = job.Status === 'PENDING_ACCOUNTS'
  const isInitiator = me?.EmployeeId === job.InitiatedByEmployeeId
  // Verify grants exist only for ACCOUNTS_ASSISTANT and ACCOUNTS_MANAGER, and
  // the service refuses the initiator either way.
  const canConfirm = pending && can(PAGE_KEYS.jobOrders, 'verify') && !isInitiator
  const canFat = can(PAGE_KEYS.fatReadiness)
  const canActualBom = can(PAGE_KEYS.componentFitments)

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="mono">{job.JobOrderNumber}</h1>
          <p className="page-sub">
            {job.CustomerName} · PO <span className="mono">{job.CustomerPoNumber}</span> line {job.CustomerPoLineNumber} · {job.MachineModel} #{job.MachineOrdinal} · serial <span className="mono">{job.MachineSerial}</span>
          </p>
        </div>
        <div className="action-row">
          <StatusBadge value={job.Status} />
          <StatusBadge value={job.FatReadinessStatus} />
          {canConfirm && (
            <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void confirm()}>Confirm (Accounts)</button>
          )}
        </div>
      </div>

      {canConfirm && (
        <div className="toolbar">
          <input
            className="input search"
            placeholder="Reason for the Accounts confirmation (mandatory — recorded in history)"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
          />
        </div>
      )}

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />

      {pending && (
        <div className="alert alert-warn" role="alert">
          <div className="alert-title">Waiting for Accounts confirmation</div>
          <p className="alert-body">
            {isInitiator
              ? 'You initiated this Job Order, so you cannot confirm it. An Accounts Assistant or Accounts Manager must.'
              : 'Until Accounts confirms, an Estimated BOM, a Production BOM or a job-backed material request against this job is refused.'}
          </p>
        </div>
      )}

      <div className="detail-grid">
        <div><span className="field-label">Customer PO record</span> <span className="mono">{job.CustomerPoRecordNumber}</span> rev {job.CustomerPoRevisionNumber}</div>
        <div><span className="field-label">Machine item</span> <span className="mono">{job.MachineItemCode}</span></div>
        <div><span className="field-label">Job order date</span> {job.JobOrderDate}</div>
        <div><span className="field-label">Planned completion</span> {job.PlannedCompletionDate ?? '—'}</div>
        <div><span className="field-label">Initiated by</span> <span className="mono">{isInitiator ? `${me?.EmployeeCode} (you)` : job.InitiatedByEmployeeId}</span> · {job.InitiatedActorRoleCode}</div>
        <div>
          <span className="field-label">Accounts confirmation</span>
          {job.AccountsConfirmedAt
            ? <>{new Date(job.AccountsConfirmedAt).toLocaleString()} · {job.AccountsConfirmationActorRoleCode}<span className="field-hint">{job.AccountsConfirmationReason}</span></>
            : '— (pending)'}
        </div>
        <div><span className="field-label">FAT readiness</span> <StatusBadge value={job.FatReadinessStatus} />{job.FatReconciledAt ? <span className="field-hint">{new Date(job.FatReconciledAt).toLocaleString()}</span> : null}</div>
        <div><span className="field-label">Version</span> <span className="mono">{job.Version}</span></div>
      </div>

      <div className="action-row" style={{ marginTop: 20 }}>
        {can(PAGE_KEYS.estimatedBom) && (
          <button type="button" className="btn btn-ghost" onClick={() => navigate(`/design/estimated-boms?jobOrder=${job.JobOrderNumber}`)}>Estimated BOM</button>
        )}
        {can(PAGE_KEYS.productionBom) && (
          <button type="button" className="btn btn-ghost" onClick={() => navigate(`/production/boms?jobOrder=${job.JobOrderNumber}`)}>Production BOM</button>
        )}
        {canActualBom && (
          <button type="button" className="btn btn-ghost" onClick={() => navigate(`/production/component-fitments?jobOrderId=${job.Id}`)}>Fitments</button>
        )}
      </div>

      <div className="tabs" style={{ marginTop: 24 }}>
        {canFat && <button type="button" className={`tab${pane === 'fat' ? ' active' : ''}`} onClick={() => setPane('fat')}>FAT readiness</button>}
        {canActualBom && <button type="button" className={`tab${pane === 'actual-bom' ? ' active' : ''}`} onClick={() => setPane('actual-bom')}>Actual BOM</button>}
        {canHistory && <button type="button" className={`tab${pane === 'history' ? ' active' : ''}`} onClick={() => setPane('history')}>History</button>}
      </div>

      {pane === 'fat' && canFat && (
        <FatReadinessPanel jobOrder={job} onChanged={() => { setHistory(null); void load() }} />
      )}
      {pane === 'actual-bom' && canActualBom && <ActualBomPanel jobOrderId={job.Id} />}
      {pane === 'history' && canHistory && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr><th>When</th><th>Action</th><th>From</th><th>To</th><th>Actor</th><th>Remarks</th></tr>
            </thead>
            <tbody>
              {!history && <tr><td colSpan={6} className="table-empty">Loading…</td></tr>}
              {history?.length === 0 && <tr><td colSpan={6} className="table-empty">No history.</td></tr>}
              {history?.map((row) => (
                <tr key={row.Id}>
                  <td>{new Date(row.CreatedAt).toLocaleString()}</td>
                  <td>{row.Action.replaceAll('_', ' ')}</td>
                  <td>{row.FromStatus ? <StatusBadge value={row.FromStatus} /> : '—'}</td>
                  <td><StatusBadge value={row.ToStatus} /></td>
                  <td><span className="mono">{row.ActorEmployeeId === me?.EmployeeId ? `${me?.EmployeeCode} (you)` : row.ActorEmployeeId}</span> · {row.ActorRoleCode} ({row.ResolvedRoleAssignmentType})</td>
                  <td>{row.Remarks}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <p className="field-hint" style={{ marginTop: 16 }}>
        <a className="row-click" onClick={() => navigate('/production/job-orders')}>← Back to Job Orders</a>
      </p>
    </div>
  )
}
