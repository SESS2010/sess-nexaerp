import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  createProductionBomRevision,
  getProductionBom,
  pinProductionBomRevision,
  replaceProductionBomLines,
  transitionProductionBom,
} from '../../api/boms'
import type { BomTransition } from '../../api/boms'
import { getJobOrder } from '../../api/production'
import { newIdempotencyKey } from '../../api/stores'
import type { ProductionBomView } from '../../types/bom'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { BomLinesEditorModal, editorLine } from '../design/BomLinesEditorModal'

export function ProductionBomDetailPage() {
  const { bomNumber = '' } = useParams()
  const navigate = useNavigate()
  const { me, can } = useSession()
  const [bom, setBom] = useState<ProductionBomView | null>(null)
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
      setBom(await getProductionBom(bomNumber))
    } catch (err) {
      setBom(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [bomNumber])

  useEffect(() => {
    void load()
  }, [load])

  const act = async (label: string, action: () => Promise<ProductionBomView>, done?: (result: ProductionBomView) => string) => {
    if (!reason.trim()) {
      setError(`A reason is required to ${label} — it becomes the history record.`)
      return
    }
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      const result = await action()
      setBom(result)
      setReason('')
      setNotice(done ? done(result) : `${result.BomNumber} revision ${result.CurrentRevisionNumber} is now ${result.Status}.`)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const transition = (kind: BomTransition) => bom && act(kind, () => transitionProductionBom(bom.BomNumber, kind, {
    ExpectedVersion: bom.CurrentRevision.Version,
    Remarks: reason.trim(),
    IdempotencyKey: newIdempotencyKey(`pbom-${kind}`),
  }))

  const newRevision = () => bom && act('start a new revision', () => createProductionBomRevision(bom.BomNumber, {
    ExpectedBomVersion: bom.Version,
    RevisionReason: reason.trim(),
    IdempotencyKey: newIdempotencyKey('pbom-revision'),
  }))

  // Pinning is guarded by the Job Order's own version, so it is read fresh.
  const pin = () => bom && act('pin this revision', async () => {
    const job = await getJobOrder(bom.JobOrderId)
    return pinProductionBomRevision(bom.BomNumber, {
      RevisionId: bom.CurrentRevision.Id,
      ExpectedJobOrderVersion: job.Version,
      Reason: reason.trim(),
      IdempotencyKey: newIdempotencyKey('pbom-pin'),
    })
  }, (result) => `Revision ${result.CurrentRevisionNumber} is pinned to ${result.JobOrderNumber}; material requests are now checked against it.`)

  if (loading && !bom) {
    return <div className="page"><p>Loading…</p></div>
  }

  if (!bom) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Production BOM not found." />
      </div>
    )
  }

  const revision = bom.CurrentRevision
  const page = PAGE_KEYS.productionBom
  const isPreparer = me?.EmployeeId === revision.PreparedByEmployeeId
  const canEdit = revision.Status === 'DRAFT' && can(page, 'update')
  const canSubmit = revision.Status === 'DRAFT' && can(page, 'submit')
  // Approve is granted to TECHNICAL_DIRECTOR only, and the service refuses the preparer.
  const canApprove = revision.Status === 'SUBMITTED' && can(page, 'approve') && !isPreparer
  const canRevise = revision.Status === 'APPROVED' && can(page, 'create')
  const isPinned = bom.PinnedRevisionId === revision.Id
  const canPin = revision.Status === 'APPROVED' && !isPinned && can(page, 'update')
  const needsReason = canSubmit || canApprove || canRevise || canPin

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="mono">{bom.BomNumber}</h1>
          <p className="page-sub">
            Job order <a className="row-click mono" onClick={() => navigate(`/production/job-orders/${bom.JobOrderId}`)}>{bom.JobOrderNumber}</a> · revision {revision.RevisionNumber} · {revision.RevisionReason}
          </p>
        </div>
        <div className="action-row">
          <StatusBadge value={revision.Status} />
          {isPinned && <StatusBadge value="Pinned" />}
          {canEdit && <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setEditing(true)}>Edit lines</button>}
          {canSubmit && <button type="button" className="btn btn-primary" disabled={busy || revision.Lines.length === 0} onClick={() => void transition('submit')}>Submit</button>}
          {canApprove && <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void transition('approve')}>Approve</button>}
          {canPin && <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void pin()}>Pin to job order</button>}
          {canRevise && <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => void newRevision()}>New revision</button>}
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

      {revision.Status === 'SUBMITTED' && isPreparer && (
        <div className="alert">Waiting for the Technical Director to approve. You prepared this revision, so you cannot approve it yourself.</div>
      )}
      {revision.Status === 'APPROVED' && !isPinned && (
        <div className="alert alert-warn" role="alert">
          <div className="alert-title">Approved but not pinned</div>
          <p className="alert-body">{bom.PinnedRevisionId ? 'An earlier revision is still pinned to the job order.' : 'No revision is pinned to the job order yet.'} Material requests are checked against the pinned revision, so pin this one once Production agrees it is the build list.</p>
        </div>
      )}

      <div className="detail-grid">
        <div><span className="field-label">Prepared by</span> <span className="mono">{isPreparer ? `${me?.EmployeeCode} (you)` : revision.PreparedByEmployeeId}</span></div>
        <div><span className="field-label">Submitted</span> {revision.SubmittedAt ? new Date(revision.SubmittedAt).toLocaleString() : '—'}</div>
        <div><span className="field-label">Approved</span> {revision.ApprovedAt ? <>{new Date(revision.ApprovedAt).toLocaleString()}<span className="field-hint">{revision.ApprovalReason}</span></> : '—'}</div>
        <div><span className="field-label">Source</span> Estimated BOM revision <span className="mono">{revision.SourceEstimatedBomRevisionId.slice(0, 8)}…</span></div>
        <div><span className="field-label">Supersedes</span> {revision.SupersedesRevisionId ? `revision ${revision.RevisionNumber - 1}` : '—'}</div>
        <div><span className="field-label">Version</span> <span className="mono">{bom.Version} / rev {revision.Version}</span></div>
      </div>

      <h2 className="form-section-title" style={{ marginTop: 24 }}>Lines ({revision.Lines.length})</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th>#</th><th>Item</th><th className="text-right">Quantity</th><th>Remarks</th></tr>
          </thead>
          <tbody>
            {revision.Lines.length === 0 && <tr><td colSpan={4} className="table-empty">No lines.</td></tr>}
            {revision.Lines.map((line) => (
              <tr key={line.Id}>
                <td className="mono">{line.LineNumber}</td>
                <td className="mono">{line.ItemCode}</td>
                <td className="text-right mono">{line.Quantity} {line.UomCode}</td>
                <td>{line.Remarks ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {editing && (
        <BomLinesEditorModal
          title={`Edit ${bom.BomNumber} revision ${revision.RevisionNumber}`}
          initialLines={revision.Lines.map((line) => editorLine({
            key: line.Id,
            itemId: line.ItemId,
            itemCode: line.ItemCode,
            uomId: line.UomId,
            uomCode: line.UomCode,
            quantity: String(line.Quantity),
            remarks: line.Remarks ?? '',
          }))}
          reasonLabel="Revision reason"
          submitLabel="Save lines"
          canSave={canEdit}
          onClose={() => setEditing(false)}
          onSave={async ({ reason: revisionReason, lines }) => {
            const result = await replaceProductionBomLines(bom.BomNumber, {
              ExpectedVersion: revision.Version,
              RevisionReason: revisionReason,
              Lines: lines,
              IdempotencyKey: newIdempotencyKey('pbom-replace'),
            })
            setEditing(false)
            setBom(result)
            setNotice(`${result.BomNumber} lines saved.`)
          }}
        />
      )}

      <p className="field-hint" style={{ marginTop: 16 }}>
        <a className="row-click" onClick={() => navigate('/production/boms')}>← Back to Production BOMs</a>
      </p>
    </div>
  )
}
