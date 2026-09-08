import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  decideMaterialIssueExcess,
  getMaterialIssueRequest,
  transitionMaterialIssueRequest,
} from '../../api/materialIssues'
import type { MaterialIssueRequestTransition } from '../../api/materialIssues'
import { newIdempotencyKey } from '../../api/stores'
import type { MaterialIssueRequestView } from '../../types/materialIssue'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { MaterialIssueRequestFormModal } from './MaterialIssueRequestFormModal'

const TRANSITION_LABEL: Record<MaterialIssueRequestTransition, string> = {
  submit: 'Submit for approval',
  approve: 'Approve',
  reject: 'Reject',
  cancel: 'Cancel MIR',
}

export function MaterialIssueRequestDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { me, can } = useSession()
  const [mir, setMir] = useState<MaterialIssueRequestView | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setMir(await getMaterialIssueRequest(id))
    } catch (err) {
      setMir(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  const run = async (transition: MaterialIssueRequestTransition) => {
    if (!mir) return
    // Every transition writes a history row whose Remarks is this reason; the
    // service rejects an empty one ("Reason is required.").
    if (!reason.trim()) {
      setError(`A reason is required to ${TRANSITION_LABEL[transition].toLowerCase()} — it becomes the history record.`)
      return
    }
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      const result = await transitionMaterialIssueRequest(mir.Id, transition, {
        Version: mir.Version,
        Reason: reason.trim(),
        IdempotencyKey: newIdempotencyKey(`mir-${transition}`),
      })
      setMir(result)
      setReason('')
      setNotice(`${result.RequestNumber} is now ${result.Status.replaceAll('_', ' ')}.`)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const decide = async (lineId: string, decision: 'APPROVED' | 'REJECTED') => {
    if (!mir) return
    if (!reason.trim()) {
      setError('A reason is required for an excess decision.')
      return
    }
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      const result = await decideMaterialIssueExcess(lineId, {
        Decision: decision,
        Reason: reason.trim(),
        IdempotencyKey: newIdempotencyKey('mir-excess'),
      })
      setMir(result)
      setReason('')
      setNotice(`Excess ${decision.toLowerCase()} recorded.`)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  if (loading && !mir) {
    return <div className="page"><p>Loading…</p></div>
  }

  if (!mir) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Material issue request not found." />
      </div>
    )
  }

  const isDraft = mir.Status === 'DRAFT'
  const isSubmitted = mir.Status === 'SUBMITTED'
  const isMine = me?.EmployeeId === mir.RequestedByEmployeeId
  const page = PAGE_KEYS.materialIssueRequests
  const canEdit = isDraft && can(page, 'update')
  const canSubmit = isDraft && can(page, 'submit')
  // Approve/reject grants exist only for STORES_MANAGER and PRODUCTION_MANAGER,
  // and the service refuses the requester's own MIR either way.
  const canApprove = isSubmitted && can(page, 'approve') && !isMine
  const canReject = isSubmitted && can(page, 'reject') && !isMine
  const canCancel = (isDraft || isSubmitted) && can(page, 'cancel')
  const canDecideExcess = can(PAGE_KEYS.materialIssueExcess, 'approve') && !isMine
  const needsReason = canSubmit || canApprove || canReject || canCancel
  const customerFacingExcess = mir.Lines.filter((line) => line.ExcessBaseQuantity > 0 && line.ExcessClassification !== 'INTERNAL')
  const pendingDecision = customerFacingExcess.filter((line) => !line.TdDecisionPresent)

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="mono">{mir.RequestNumber}</h1>
          <p className="page-sub">
            {mir.Situation.replaceAll('_', ' ')} · {mir.Purpose.replaceAll('_', ' ')} · to {mir.DestinationType.replaceAll('_', ' ')} {mir.DestinationName}
          </p>
        </div>
        <div className="action-row">
          <StatusBadge value={mir.Status} />
          {canEdit && (
            <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setEditing(true)}>Edit</button>
          )}
          {canSubmit && (
            <button type="button" className="btn btn-primary" disabled={busy || mir.Lines.length === 0} onClick={() => run('submit')}>
              {TRANSITION_LABEL.submit}
            </button>
          )}
          {canApprove && (
            <button type="button" className="btn btn-primary" disabled={busy} onClick={() => run('approve')}>{TRANSITION_LABEL.approve}</button>
          )}
          {canReject && (
            <button type="button" className="btn btn-warn" disabled={busy} onClick={() => run('reject')}>{TRANSITION_LABEL.reject}</button>
          )}
          {canCancel && (
            <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => run('cancel')}>{TRANSITION_LABEL.cancel}</button>
          )}
        </div>
      </div>

      {needsReason && (
        <div className="toolbar">
          <input
            className="input search"
            placeholder="Reason for the action you are about to take (mandatory — recorded in history)"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
          />
        </div>
      )}

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />

      {isSubmitted && isMine && (
        <div className="alert">
          Waiting for a Stores Manager or Production Manager to approve. You raised this MIR, so you cannot approve it yourself.
        </div>
      )}

      {pendingDecision.length > 0 && (
        <div className="alert alert-warn" role="alert">
          <div className="alert-title">Customer-facing excess needs a Technical Director decision before issue</div>
          <p className="alert-body">
            {pendingDecision.length} line{pendingDecision.length > 1 ? 's' : ''} ask for more than the Estimated BOM, Production BOM or customer PO allows. Stores cannot issue against this MIR until the TD approves or rejects each one.
          </p>
        </div>
      )}

      <div className="detail-grid">
        <div><span className="field-label">Required date</span> {mir.RequiredDate}</div>
        <div><span className="field-label">Destination</span> {mir.DestinationType.replaceAll('_', ' ')} · {mir.DestinationName}</div>
        <div><span className="field-label">Job order</span> <span className="mono">{mir.JobOrderId ?? '— (not applicable)'}</span></div>
        <div><span className="field-label">Requested by</span> <span className="mono">{isMine ? `${me?.EmployeeCode} (you)` : mir.RequestedByEmployeeId}</span></div>
        <div><span className="field-label">Requesting department</span> <span className="mono">{mir.RequestingDepartmentId === me?.DepartmentId ? me?.DepartmentCode : mir.RequestingDepartmentId}</span></div>
        <div><span className="field-label">Version</span> <span className="mono">{mir.Version}</span></div>
      </div>

      <h2 className="form-section-title">Lines</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>#</th>
              <th>Item</th>
              <th className="text-right">Requested</th>
              <th className="text-right">Base qty</th>
              <th className="text-right">Est. BOM</th>
              <th className="text-right">Prod. BOM</th>
              <th className="text-right">Excess</th>
              <th>Excess class</th>
              <th>TD decision</th>
              <th>Remarks</th>
            </tr>
          </thead>
          <tbody>
            {mir.Lines.length === 0 && <tr><td colSpan={10} className="table-empty">No lines. Edit the draft to add items.</td></tr>}
            {mir.Lines.map((line) => (
              <tr key={line.Id}>
                <td className="mono">{line.LineNumber}</td>
                <td><span className="mono">{line.ItemCode}</span> — {line.ItemName}</td>
                <td className="text-right mono">{line.RequestedQuantity} {line.UomCode}</td>
                <td className="text-right mono">{line.RequestedBaseQuantity}</td>
                <td className="text-right mono">{line.EstimatedBomBaseQuantity || '—'}</td>
                <td className="text-right mono">{line.ProductionBomBaseQuantity || '—'}</td>
                <td className="text-right mono">{line.ExcessBaseQuantity > 0 ? line.ExcessBaseQuantity : '—'}</td>
                <td>{line.ExcessBaseQuantity > 0 ? <StatusBadge value={line.ExcessClassification} /> : '—'}</td>
                <td>
                  {line.ExcessBaseQuantity <= 0 || line.ExcessClassification === 'INTERNAL' ? '—'
                    : line.TdDecisionPresent ? <StatusBadge value="Decided" />
                    : canDecideExcess ? (
                      <span className="action-row">
                        <button type="button" className="btn btn-primary" disabled={busy} onClick={() => decide(line.Id, 'APPROVED')}>Approve</button>
                        <button type="button" className="btn btn-warn" disabled={busy} onClick={() => decide(line.Id, 'REJECTED')}>Reject</button>
                      </span>
                    ) : <StatusBadge value="Pending" />}
                </td>
                <td>{line.Remarks ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {editing && (
        <MaterialIssueRequestFormModal
          mode="edit"
          existing={mir}
          onClose={() => setEditing(false)}
          onSaved={(result) => {
            setEditing(false)
            setMir(result)
            setNotice(`${result.RequestNumber} saved.`)
          }}
        />
      )}

      <p className="field-hint" style={{ marginTop: 16 }}>
        <a className="row-click" onClick={() => navigate('/stores/material-issue-requests')}>← Back to Material Issue Requests</a>
      </p>
    </div>
  )
}
