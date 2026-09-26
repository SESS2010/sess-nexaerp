import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  STOCK_ADJUSTMENT_PAGE_KEY,
  approveStockAdjustment,
  formatDateOnly,
  formatInstantLocal,
  formatQuantityChange,
  formatRupees,
  getStockAdjustment,
  rejectStockAdjustment,
  stockAdjustmentRefusedDefinitively,
  submitStockAdjustment,
} from '../../api/stockAdjustments'
import { newIdempotencyKey } from '../../api/stores'
import type { StockAdjustmentView } from '../../types/stockAdjustment'
import { stockAdjustmentRoleWords } from '../../types/stockAdjustment'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession } from '../auth/SessionContext'
import { StockAdjustmentFormModal } from './StockAdjustmentFormModal'

type Command = 'submit' | 'approve' | 'reject'

const COMMAND_WORDS: Record<Command, string> = {
  submit: 'submit for approval',
  approve: 'approve',
  reject: 'reject',
}

/**
 * One A2 stock adjustment: lines with their accepted values, the approval
 * snapshot the server took at submission (required roles, who has decided),
 * and the Submit / Approve / Reject / Revise / Reverse actions the session's
 * page permissions allow. The server decides authority; this page only shows
 * what the snapshot says and relays refusals in its own words.
 */
export function StockAdjustmentDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { me, can } = useSession()
  const [adjustment, setAdjustment] = useState<StockAdjustmentView | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [reason, setReason] = useState('')
  const [roleCode, setRoleCode] = useState('')
  const [busy, setBusy] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [editing, setEditing] = useState(false)
  const [reversing, setReversing] = useState(false)

  // One key per command attempt, kept while its outcome is unknown so a retry
  // replays instead of acting twice; dropped after success or a definitive refusal.
  const keys = useRef<Partial<Record<Command, string>>>({})
  const keyFor = (command: Command) => (keys.current[command] ??= newIdempotencyKey(`stock-adjustment-${command}`))

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setAdjustment(await getStockAdjustment(id))
    } catch (err) {
      setAdjustment(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  const run = async (command: Command) => {
    if (!adjustment) return
    if (!reason.trim()) {
      setError(`A reason is required to ${COMMAND_WORDS[command]}; it is kept with the decision.`)
      return
    }
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      const body = { Version: adjustment.Version, Reason: reason.trim(), IdempotencyKey: keyFor(command) }
      const result = command === 'submit'
        ? await submitStockAdjustment(adjustment.Id, body)
        : command === 'approve'
          ? await approveStockAdjustment(adjustment.Id, { ...body, RoleCode: roleCode || null })
          : await rejectStockAdjustment(adjustment.Id, body)
      delete keys.current[command]
      setAdjustment(result)
      setReason('')
      setRoleCode('')
      setConfirming(false)
      const replayed = result.Replayed ? ' (this action had already been recorded; nothing was done twice)' : ''
      setNotice(
        result.Status === 'POSTED'
          ? `${result.AdjustmentNumber} is approved and POSTED: stock and FIFO are updated${replayed}.`
          : `${result.AdjustmentNumber} is now ${result.Status}${replayed}.`,
      )
    } catch (err) {
      if (stockAdjustmentRefusedDefinitively(err)) delete keys.current[command]
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  if (loading && !adjustment) {
    return <div className="page"><p>Loading…</p></div>
  }

  if (!adjustment) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Stock adjustment not found." />
        <p className="field-hint" style={{ marginTop: 16 }}>
          <a className="row-click" onClick={() => navigate('/stores/stock-adjustments')}>← Back to Stock Adjustments</a>
        </p>
      </div>
    )
  }

  const page = STOCK_ADJUSTMENT_PAGE_KEY
  const status = adjustment.Status
  const isDraft = status === 'DRAFT'
  const isSubmitted = status === 'SUBMITTED'
  const isPosted = status === 'POSTED'
  const excluded = !!me && adjustment.ExcludedEmployeeIds.includes(me.EmployeeId)
  const isRecorder = me?.EmployeeId === adjustment.RecordedByEmployeeId

  const canRevise = (isDraft || isSubmitted || status === 'REJECTED') && can(page, 'update')
  const canSubmit = isDraft && can(page, 'submit')
  const canApprove = isSubmitted && can(page, 'approve')
  const canReject = isSubmitted && can(page, 'reject')
  const canReverse = isPosted && can(page, 'create')
  const needsReason = canSubmit || canApprove || canReject

  const currentApprovals = adjustment.Decisions.filter(
    (decision) => decision.RevisionNumber === adjustment.CurrentRevisionNumber && decision.Decision === 'APPROVE',
  )
  const outstanding = adjustment.OutstandingRoleCodes ?? []
  const additions = adjustment.Lines.filter((line) => line.QuantityChange > 0)
  const removals = adjustment.Lines.filter((line) => line.QuantityChange < 0)
  const sum = (rows: typeof adjustment.Lines) => rows.reduce((total, line) => total + line.AcceptedLineValue, 0)

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="mono">{adjustment.AdjustmentNumber}</h1>
          <p className="page-sub">
            {adjustment.ReasonKind.replaceAll('_', ' ')} · warehouse {adjustment.WarehouseCode} · effective {formatDateOnly(adjustment.EffectiveDate)} · revision {adjustment.CurrentRevisionNumber}
          </p>
        </div>
        <div className="action-row">
          <StatusBadge value={status} />
          {canRevise && (
            <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setEditing(true)}>
              {isDraft ? 'Edit' : 'Revise'}
            </button>
          )}
          {canSubmit && (
            <button type="button" className="btn btn-primary" disabled={busy || adjustment.Lines.length === 0} onClick={() => void run('submit')}>
              Submit for approval
            </button>
          )}
          {canApprove && (
            <button type="button" className="btn btn-primary" disabled={busy} onClick={() => { setError(null); setConfirming(true) }}>
              Approve…
            </button>
          )}
          {canReject && (
            <button type="button" className="btn btn-warn" disabled={busy} onClick={() => void run('reject')}>Reject</button>
          )}
          {canReverse && (
            <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setReversing(true)}>Raise reversal</button>
          )}
        </div>
      </div>

      {needsReason && !confirming && (
        <div className="toolbar">
          <input
            className="input search"
            maxLength={1000}
            placeholder="Reason for the action you are about to take (mandatory — kept with the decision)"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
          />
        </div>
      )}

      {notice && <div className="alert">{notice}</div>}
      {!confirming && <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />}

      {isSubmitted && excluded && (
        <div className="alert alert-warn">
          {isRecorder ? 'You recorded this adjustment' : 'You are named as a counter on this adjustment'}, so the server will not accept your approval or rejection. It waits on {outstanding.length > 0 ? outstanding.map(stockAdjustmentRoleWords).join(' and ') : 'the named approvers'}.
        </div>
      )}
      {status === 'REJECTED' && (
        <div className="alert alert-warn">Rejected. Stores revises it (a new revision, back to DRAFT) and submits again, or leaves it rejected.</div>
      )}
      {isPosted && (
        <div className="alert">
          Posted {formatInstantLocal(adjustment.PostedAt)}. A posted adjustment cannot be changed; correct it with a reversing adjustment.
        </div>
      )}

      {confirming && (
        <div className="card" style={{ marginBottom: 16 }}>
          <div className="alert alert-warn">
            <div className="alert-title">Check before you approve</div>
            <p className="alert-body">
              You are approving <strong className="mono">{adjustment.AdjustmentNumber}</strong>: {additions.length > 0 && <>{additions.length} line{additions.length > 1 ? 's' : ''} adding stock worth <strong>{formatRupees(sum(additions))}</strong></>}
              {additions.length > 0 && removals.length > 0 && ' and '}
              {removals.length > 0 && <>{removals.length} line{removals.length > 1 ? 's' : ''} removing stock worth <strong>{formatRupees(sum(removals))}</strong> at FIFO</>} in warehouse <span className="mono">{adjustment.WarehouseCode}</span>, effective {formatDateOnly(adjustment.EffectiveDate)}.
            </p>
            <p className="alert-body">
              Still waiting on: <strong>{outstanding.length > 0 ? outstanding.map(stockAdjustmentRoleWords).join(', ') : '—'}</strong>.
              {' '}If yours is the last of these, <strong>the stock is posted the moment you approve</strong>. Posting cannot be undone; the only correction is a reversing adjustment that goes through approval again.
            </p>
            <p className="alert-body">
              The server re-values the removals from today's FIFO layers at the final approval. If that moves the value into another approval band, it refuses and Stores must revise and resubmit.
            </p>
          </div>

          <div className="form-grid">
            {outstanding.length > 1 && (
              <label className="field">
                <span className="field-label">Approve as</span>
                <select className="input" value={roleCode} onChange={(event) => setRoleCode(event.target.value)}>
                  <option value="">Let the server choose from the roles I hold</option>
                  {outstanding.map((code) => <option key={code} value={code}>{stockAdjustmentRoleWords(code)}</option>)}
                </select>
                <span className="field-hint">Only matters if you hold more than one of the outstanding roles.</span>
              </label>
            )}
            <label className="field field-wide">
              <span className="field-label">Approval reason *</span>
              <input className="input" maxLength={1000} value={reason} onChange={(event) => setReason(event.target.value)} />
            </label>
            <ErrorAlert error={error} className="field-wide" onReload={() => { setConfirming(false); void load() }} fallback="The approval was not recorded." />
            <div className="field-wide modal-actions">
              <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => { setConfirming(false); setError(null) }}>‹ Go back</button>
              <button type="button" className="btn btn-primary" disabled={busy || !reason.trim()} onClick={() => void run('approve')}>
                {busy ? 'Approving…' : `Yes, approve ${adjustment.AdjustmentNumber}`}
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="detail-grid">
        <div><span className="field-label">Reason kind</span> {adjustment.ReasonKind.replaceAll('_', ' ')}</div>
        <div><span className="field-label">Inventory period</span> <span className="mono">{adjustment.InventoryPeriodCode}</span></div>
        <div><span className="field-label">Effective date</span> {formatDateOnly(adjustment.EffectiveDate)}</div>
        <div><span className="field-label">Recorded by</span> <span className="mono">{adjustment.RecordedByEmployeeCode}{isRecorder ? ' (you)' : ''}</span></div>
        <div><span className="field-label">Counters</span> {adjustment.CounterEmployeeIds.length > 0 ? `${adjustment.CounterEmployeeIds.length} named` : '—'}</div>
        <div><span className="field-label">Version</span> <span className="mono">{adjustment.Version}</span></div>
        {adjustment.DaysBackdated > 0 && (
          <>
            <div><span className="field-label">Backdated</span> {adjustment.DaysBackdated} day{adjustment.DaysBackdated === 1 ? '' : 's'}</div>
            <div><span className="field-label">Backdate reason</span> {adjustment.BackdateReason ?? '—'}</div>
            <div><span className="field-label">Evidence id</span> <span className="mono">{adjustment.BackdateEvidenceId ?? '—'}</span></div>
          </>
        )}
        {adjustment.ReversesStockAdjustmentId && (
          <div>
            <span className="field-label">Reverses</span>{' '}
            <a className="row-click mono" onClick={() => navigate(`/stores/stock-adjustments/${adjustment.ReversesStockAdjustmentId}`)}>open the original</a>
          </div>
        )}
        {adjustment.StockPostingBatchId && (
          <div><span className="field-label">Posting batch</span> <span className="mono">{adjustment.StockPostingBatchId}</span></div>
        )}
      </div>
      <p><span className="field-label">Remarks</span> {adjustment.Remarks}</p>

      <h2 className="form-section-title">Approval</h2>
      {adjustment.RequiredRoleCodes.length === 0 ? (
        <p className="field-hint">
          {isDraft || status === 'REJECTED'
            ? `The approvers are named by the server when this is submitted, from the value (now ${formatRupees(adjustment.AbsoluteValue)}), the reason and any backdate.`
            : 'No approval snapshot is recorded.'}
        </p>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Required approver</th>
                <th>Decision on revision {adjustment.CurrentRevisionNumber}</th>
                <th>By</th>
                <th>When</th>
              </tr>
            </thead>
            <tbody>
              {adjustment.RequiredRoleCodes.map((code) => {
                const decision = currentApprovals.find((row) => row.RoleCode === code)
                return (
                  <tr key={code}>
                    <td>{stockAdjustmentRoleWords(code)}</td>
                    <td>{decision ? <StatusBadge value="APPROVED" /> : isSubmitted ? <StatusBadge value="Pending" /> : '—'}</td>
                    <td>{decision ? `${decision.EmployeeCode} — ${decision.EmployeeName}` : '—'}</td>
                    <td>{decision ? formatInstantLocal(decision.DecidedAt) : '—'}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
          <p className="field-hint">
            Snapshot value {formatRupees(adjustment.AbsoluteValue)} (additions at stated value plus removals at FIFO, never netted). The recorder and counters cannot decide; each required role needs a different person.
          </p>
        </div>
      )}

      <h2 className="form-section-title">Lines</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>#</th>
              <th>Item</th>
              <th>Rack/bin</th>
              <th>Lot</th>
              <th>Serial</th>
              <th className="text-right">Change</th>
              <th className="text-right">Unit value</th>
              <th className="text-right">Carrying value</th>
              <th>Remarks</th>
            </tr>
          </thead>
          <tbody>
            {adjustment.Lines.length === 0 && <tr><td colSpan={9} className="table-empty">No lines.</td></tr>}
            {adjustment.Lines.map((line) => (
              <tr key={line.Id}>
                <td className="mono">{line.LineNumber}</td>
                <td><span className="mono">{line.ItemCode}</span> — {line.ItemName}</td>
                <td className="mono">{line.RackBinCode}</td>
                <td className="mono">{line.LotNumber ?? '—'}</td>
                <td className="mono">{line.SerialNumber ?? '—'}</td>
                <td className="text-right mono">{formatQuantityChange(line.QuantityChange)}</td>
                <td className="text-right mono">{line.QuantityChange > 0 ? formatRupees(line.UnitValue) : 'FIFO'}</td>
                <td className="text-right mono">{formatRupees(line.AcceptedLineValue)}</td>
                <td>{line.Remarks ?? '—'}</td>
              </tr>
            ))}
          </tbody>
          {adjustment.Lines.length > 0 && (
            <tfoot>
              <tr>
                <td colSpan={7} className="text-right">Added {formatRupees(sum(additions))} · removed {formatRupees(sum(removals))}</td>
                <td className="text-right mono"><strong>{formatRupees(sum(adjustment.Lines))}</strong></td>
                <td />
              </tr>
            </tfoot>
          )}
        </table>
      </div>
      {removals.length > 0 && !isPosted && (
        <p className="field-hint">Removal values are the FIFO carrying value when the lines were saved; the final approval re-values them from the layers at that moment.</p>
      )}

      {adjustment.Decisions.length > 0 && (
        <>
          <h2 className="form-section-title">Decision history</h2>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Revision</th>
                  <th>Decision</th>
                  <th>Role</th>
                  <th>By</th>
                  <th>When</th>
                  <th>Reason</th>
                </tr>
              </thead>
              <tbody>
                {adjustment.Decisions.map((decision) => (
                  <tr key={decision.Id}>
                    <td className="mono">{decision.RevisionNumber}</td>
                    <td><StatusBadge value={decision.Decision === 'APPROVE' ? 'APPROVED' : decision.Decision === 'REJECT' ? 'REJECTED' : decision.Decision} /></td>
                    <td>{stockAdjustmentRoleWords(decision.RoleCode)}{decision.RoleAssignmentType && decision.RoleAssignmentType !== 'FULL' ? ` (${decision.RoleAssignmentType})` : ''}</td>
                    <td>{decision.EmployeeCode} — {decision.EmployeeName}</td>
                    <td>{formatInstantLocal(decision.DecidedAt)}</td>
                    <td>{decision.Reason}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {editing && (
        <StockAdjustmentFormModal
          mode="edit"
          existing={adjustment}
          onClose={() => setEditing(false)}
          onSaved={(result) => {
            setEditing(false)
            setAdjustment(result)
            setNotice(`${result.AdjustmentNumber} saved as revision ${result.CurrentRevisionNumber} (DRAFT).`)
          }}
        />
      )}

      {reversing && (
        <StockAdjustmentFormModal
          mode="create"
          reverseOf={adjustment}
          onClose={() => setReversing(false)}
          onSaved={(result) => {
            setReversing(false)
            navigate(`/stores/stock-adjustments/${result.Id}`)
          }}
        />
      )}

      <p className="field-hint" style={{ marginTop: 16 }}>
        <a className="row-click" onClick={() => navigate('/stores/stock-adjustments')}>← Back to Stock Adjustments</a>
      </p>
    </div>
  )
}
