import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  createEstimatedBomRevision,
  getEstimatedBom,
  getEstimatedBomHistory,
  replaceEstimatedBomLines,
  transitionEstimatedBom,
} from '../../api/boms'
import type { BomTransition } from '../../api/boms'
import { newIdempotencyKey } from '../../api/stores'
import type { EstimatedBomHistoryView, EstimatedBomView } from '../../types/bom'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { BomLinesEditorModal, editorLine } from './BomLinesEditorModal'

type Pane = 'lines' | 'canonical' | 'history'

export function EstimatedBomDetailPage() {
  const { bomNumber = '' } = useParams()
  const navigate = useNavigate()
  const { me, can } = useSession()
  const [bom, setBom] = useState<EstimatedBomView | null>(null)
  const [history, setHistory] = useState<EstimatedBomHistoryView[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)
  const [pane, setPane] = useState<Pane>('lines')

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setBom(await getEstimatedBom(bomNumber))
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

  const canHistory = can(PAGE_KEYS.estimatedBom, 'view-audit-history')
  useEffect(() => {
    if (pane !== 'history' || !canHistory || history) return
    getEstimatedBomHistory(bomNumber).then(setHistory).catch(setError)
  }, [pane, canHistory, history, bomNumber])

  const act = async (label: string, action: () => Promise<EstimatedBomView>) => {
    if (!reason.trim()) {
      setError(`A reason is required to ${label.toLowerCase()} — it becomes the history record.`)
      return
    }
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      const result = await action()
      setBom(result)
      setHistory(null)
      setReason('')
      setNotice(`${result.BomNumber} revision ${result.CurrentRevisionNumber} is now ${result.Status}.`)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const transition = (kind: BomTransition) => bom && act(kind, () => transitionEstimatedBom(bom.BomNumber, kind, {
    ExpectedVersion: bom.CurrentRevision.Version,
    Remarks: reason.trim(),
    IdempotencyKey: newIdempotencyKey(`ebom-${kind}`),
  }))

  const newRevision = () => bom && act('start a new revision', () => createEstimatedBomRevision(bom.BomNumber, {
    ExpectedBomVersion: bom.Version,
    RevisionReason: reason.trim(),
    IdempotencyKey: newIdempotencyKey('ebom-revision'),
  }))

  if (loading && !bom) {
    return <div className="page"><p>Loading…</p></div>
  }

  if (!bom) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Estimated BOM not found." />
      </div>
    )
  }

  const revision = bom.CurrentRevision
  const page = PAGE_KEYS.estimatedBom
  const isPreparer = me?.EmployeeId === revision.PreparedByEmployeeId
  const canEdit = revision.Status === 'DRAFT' && can(page, 'update')
  const canSubmit = revision.Status === 'DRAFT' && can(page, 'submit')
  // Approve is granted to TECHNICAL_DIRECTOR only, and the service refuses the preparer.
  const canApprove = revision.Status === 'SUBMITTED' && can(page, 'approve') && !isPreparer
  const canRevise = revision.Status === 'APPROVED' && can(page, 'create')
  const needsReason = canSubmit || canApprove || canRevise
  const mergedLines = revision.Lines.filter((line) => line.CanonicalItemId !== line.OriginalItemId)

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
          {canEdit && <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setEditing(true)}>Edit lines</button>}
          {canSubmit && <button type="button" className="btn btn-primary" disabled={busy || revision.Lines.length === 0} onClick={() => void transition('submit')}>Submit</button>}
          {canApprove && <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void transition('approve')}>Approve</button>}
          {canRevise && <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => void newRevision()}>New revision</button>}
        </div>
      </div>

      {needsReason && (
        <div className="toolbar">
          <input
            className="input search"
            placeholder={canRevise ? 'Reason for the new revision (mandatory)' : 'Reason for the action you are about to take (mandatory — recorded in history)'}
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
      {mergedLines.length > 0 && (
        <div className="alert alert-warn" role="alert">
          <div className="alert-title">{mergedLines.length} line{mergedLines.length > 1 ? 's' : ''} now resolve to a merged item</div>
          <p className="alert-body">The item originally entered has been merged into a survivor in the item master. The canonical view below shows what a material request is checked against.</p>
        </div>
      )}

      <div className="detail-grid">
        <div><span className="field-label">Prepared by</span> <span className="mono">{isPreparer ? `${me?.EmployeeCode} (you)` : revision.PreparedByEmployeeId}</span></div>
        <div><span className="field-label">Submitted</span> {revision.SubmittedAt ? new Date(revision.SubmittedAt).toLocaleString() : '—'}</div>
        <div><span className="field-label">Approved</span> {revision.ApprovedAt ? <>{new Date(revision.ApprovedAt).toLocaleString()}<span className="field-hint">{revision.ApprovalReason}</span></> : '—'}</div>
        <div><span className="field-label">Approved revision</span> {bom.ApprovedRevisionId ? (bom.ApprovedRevisionId === revision.Id ? 'this one' : 'an earlier revision') : '—'}</div>
        <div><span className="field-label">Commercial baseline</span> {bom.CommercialBaselineRevisionId ? (bom.CommercialBaselineRevisionId === revision.Id ? 'this one' : 'an earlier revision') : '—'}</div>
        <div><span className="field-label">Version</span> <span className="mono">{bom.Version} / rev {revision.Version}</span></div>
      </div>

      <div className="tabs" style={{ marginTop: 24 }}>
        <button type="button" className={`tab${pane === 'lines' ? ' active' : ''}`} onClick={() => setPane('lines')}>Lines ({revision.Lines.length})</button>
        <button type="button" className={`tab${pane === 'canonical' ? ' active' : ''}`} onClick={() => setPane('canonical')}>Canonical ({revision.CanonicalLines.length})</button>
        {canHistory && <button type="button" className={`tab${pane === 'history' ? ' active' : ''}`} onClick={() => setPane('history')}>History</button>}
      </div>

      {pane === 'lines' && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr><th>#</th><th>Item</th><th>Canonical item</th><th className="text-right">Quantity</th><th>Remarks</th></tr>
            </thead>
            <tbody>
              {revision.Lines.length === 0 && <tr><td colSpan={5} className="table-empty">No lines. Edit the draft to add items.</td></tr>}
              {revision.Lines.map((line) => (
                <tr key={line.Id}>
                  <td className="mono">{line.LineNumber}</td>
                  <td className="mono">{line.OriginalItemCode}</td>
                  <td>
                    {line.CanonicalItemId === line.OriginalItemId ? '—' : <span className="mono">{line.CanonicalItemCode}</span>}
                    {!line.CanonicalItemActive && <> <StatusBadge value="Inactive" /></>}
                  </td>
                  <td className="text-right mono">{line.Quantity} {line.UomCode}</td>
                  <td>{line.Remarks ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {pane === 'canonical' && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr><th>Canonical item</th><th>Approval</th><th className="text-right">Base quantity</th><th>From lines</th></tr>
            </thead>
            <tbody>
              {revision.CanonicalLines.length === 0 && <tr><td colSpan={4} className="table-empty">No lines.</td></tr>}
              {revision.CanonicalLines.map((line) => (
                <tr key={line.CanonicalItemId}>
                  <td className="mono">{line.CanonicalItemCode}{!line.CanonicalItemActive && <> <StatusBadge value="Inactive" /></>}</td>
                  <td><StatusBadge value={line.CanonicalItemApprovalStatus} /></td>
                  <td className="text-right mono">{line.BaseQuantity} {line.BaseUomCode}</td>
                  <td className="mono">{line.SourceLines.map((source) => source.LineNumber).join(', ')}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

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
                  <td>{row.Action}</td>
                  <td>{row.FromStatus ? <StatusBadge value={row.FromStatus} /> : '—'}</td>
                  <td><StatusBadge value={row.ToStatus} /></td>
                  <td><span className="mono">{row.ActorEmployeeId === me?.EmployeeId ? `${me?.EmployeeCode} (you)` : row.ActorEmployeeId}</span> · {row.ActorRoleCode}</td>
                  <td>{row.Remarks}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {editing && (
        <BomLinesEditorModal
          title={`Edit ${bom.BomNumber} revision ${revision.RevisionNumber}`}
          initialLines={revision.Lines.map((line) => editorLine({
            key: line.Id,
            itemId: line.OriginalItemId,
            itemCode: line.OriginalItemCode,
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
            const result = await replaceEstimatedBomLines(bom.BomNumber, {
              ExpectedVersion: revision.Version,
              RevisionReason: revisionReason,
              Lines: lines,
              IdempotencyKey: newIdempotencyKey('ebom-replace'),
            })
            setEditing(false)
            setBom(result)
            setHistory(null)
            setNotice(`${result.BomNumber} lines saved.`)
          }}
        />
      )}

      <p className="field-hint" style={{ marginTop: 16 }}>
        <a className="row-click" onClick={() => navigate('/design/estimated-boms')}>← Back to Estimated BOMs</a>
      </p>
    </div>
  )
}
