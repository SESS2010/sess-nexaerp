import { useCallback, useEffect, useState } from 'react'
import { explainFatCustody, getFatReadiness, reconcileFatReadiness } from '../../api/production'
import { newIdempotencyKey } from '../../api/stores'
import type { FatReconciliationLineView, JobOrderFatReadinessView, JobOrderView } from '../../types/production'
import { FAT_DISPOSITIONS } from '../../types/production'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

interface Props {
  jobOrder: JobOrderView
  /** The job's FatReadinessStatus changes on reconcile; the parent reloads it. */
  onChanged: () => void
}

/**
 * GET/POST /api/v1/production/job-orders/{id}/fat-readiness.
 *
 * Every base unit issued to an engineer for this job must be fitted,
 * returned (an accepted return document proves RETURNED_LATE) or explained as
 * LOST / SCRAPPED. Reconciliation (QC_MANAGER or DESIGN_ENGINEER) snapshots the
 * balances: any unexplained quantity gives BLOCKED, none gives READY. The
 * database itself refuses READY while unexplained custody remains.
 */
export function FatReadinessPanel({ jobOrder, onChanged }: Props) {
  const { can, me } = useSession()
  const [view, setView] = useState<JobOrderFatReadinessView | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [explaining, setExplaining] = useState<FatReconciliationLineView | null>(null)
  const [quantity, setQuantity] = useState('')
  const [disposition, setDisposition] = useState<string>(FAT_DISPOSITIONS[0])

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setView(await getFatReadiness(jobOrder.Id))
    } catch (err) {
      setView(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [jobOrder.Id])

  useEffect(() => {
    void load()
  }, [load])

  const canReconcile = can(PAGE_KEYS.fatReadiness, 'verify')
  const canExplain = can(PAGE_KEYS.fatReadiness, 'create')

  const reconcile = async () => {
    if (!reason.trim()) {
      setError('A reason is required to reconcile — it is recorded on the reconciliation attempt.')
      return
    }
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      const result = await reconcileFatReadiness(jobOrder.Id, { Reason: reason.trim(), IdempotencyKey: newIdempotencyKey('fat-reconcile') })
      setReason('')
      setNotice(result.Result === 'READY'
        ? `Attempt ${result.AttemptNumber}: READY — every issued unit is fitted, returned or explained.`
        : `Attempt ${result.AttemptNumber}: BLOCKED — ${result.UnexplainedQuantityBase} base unit(s) still unexplained. See the lines below.`)
      await load()
      onChanged()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const explain = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!explaining) return
    const qty = Number(quantity)
    if (!(qty > 0)) { setError('Quantity must be greater than zero.'); return }
    if (qty > explaining.UnexplainedQuantityBase) { setError(`Only ${explaining.UnexplainedQuantityBase} base unit(s) are unexplained on this line.`); return }
    if (!reason.trim()) { setError('A reason is required for a custody explanation.'); return }
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      await explainFatCustody(jobOrder.Id, {
        MaterialIssueLineId: explaining.MaterialIssueLineId,
        QuantityBase: qty,
        Disposition: disposition,
        Reason: reason.trim(),
        IdempotencyKey: newIdempotencyKey('fat-explain'),
      })
      setNotice(`${qty} × ${explaining.ItemCode} recorded as ${disposition}. Re-run the reconciliation to refresh the balances.`)
      setExplaining(null)
      setQuantity('')
      setReason('')
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const latest = view?.LatestReconciliation ?? null

  return (
    <div>
      <div className="toolbar">
        <StatusBadge value={view?.FatReadinessStatus ?? jobOrder.FatReadinessStatus} />
        {latest && <span className="pager-label">Attempt {latest.AttemptNumber} · {new Date(latest.ReconciledAt).toLocaleString()} · {latest.ActorRoleCode}</span>}
        <div className="spacer" />
        {(canReconcile || explaining) && (
          <input
            className="input search"
            placeholder={explaining ? 'Reason for the explanation (mandatory)' : 'Reason for this reconciliation attempt (mandatory)'}
            value={reason}
            onChange={(event) => setReason(event.target.value)}
          />
        )}
        {canReconcile && !explaining && (
          <button type="button" className="btn btn-primary" disabled={busy || loading} onClick={() => void reconcile()}>
            {latest ? 'Re-run reconciliation' : 'Run reconciliation'}
          </button>
        )}
      </div>

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="FAT readiness could not be loaded." />

      {!canReconcile && !latest && !loading && (
        <div className="alert">No reconciliation has been run yet. A QC Manager or Design Engineer runs it; the result is BLOCKED or READY.</div>
      )}

      {explaining && (
        <form onSubmit={explain} className="card" style={{ marginBottom: 16 }}>
          <div className="form-section-title">Explain custody — {explaining.ItemCode} held by {explaining.CustodianEmployeeCode}</div>
          <div className="form-grid">
            <label className="field">
              <span className="field-label">Disposition *</span>
              <select className="input" value={disposition} onChange={(event) => setDisposition(event.target.value)}>
                {FAT_DISPOSITIONS.map((option) => <option key={option} value={option}>{option}</option>)}
              </select>
              <span className="field-hint">A late return is proven by an accepted Material Return document, not explained here.</span>
            </label>
            <label className="field">
              <span className="field-label">Quantity (base) *</span>
              <input className="input mono text-right" inputMode="decimal" value={quantity} onChange={(event) => setQuantity(event.target.value)} />
              <span className="field-hint">Unexplained on this line: {explaining.UnexplainedQuantityBase}</span>
            </label>
            <div className="field-wide modal-actions">
              <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => { setExplaining(null); setQuantity('') }}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? 'Saving…' : 'Record explanation'}</button>
            </div>
          </div>
        </form>
      )}

      {latest && (
        <>
          <div className="detail-grid" style={{ marginBottom: 16 }}>
            <div><span className="field-label">Issued</span> <span className="mono">{latest.IssuedQuantityBase}</span></div>
            <div><span className="field-label">Fitted</span> <span className="mono">{latest.FittedQuantityBase}</span></div>
            <div><span className="field-label">Returned</span> <span className="mono">{latest.ReturnedQuantityBase}</span></div>
            <div><span className="field-label">Explained</span> <span className="mono">{latest.ExplainedQuantityBase}</span></div>
            <div><span className="field-label">Unexplained</span> <span className="mono">{latest.UnexplainedQuantityBase}</span></div>
            <div><span className="field-label">Result</span> <StatusBadge value={latest.Result} /><span className="field-hint">{latest.Reason}</span></div>
          </div>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Item</th>
                  <th>Custodian</th>
                  <th className="text-right">Issued</th>
                  <th className="text-right">Fitted</th>
                  <th className="text-right">Returned</th>
                  <th className="text-right">Late</th>
                  <th className="text-right">Lost</th>
                  <th className="text-right">Scrapped</th>
                  <th className="text-right">Unexplained</th>
                  <th>Class</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {latest.Lines.length === 0 && <tr><td colSpan={11} className="table-empty">Nothing was issued to an engineer for this job.</td></tr>}
                {latest.Lines.map((line) => (
                  <tr key={line.Id}>
                    <td className="mono">{line.ItemCode}</td>
                    <td className="mono">{line.CustodianEmployeeId === me?.EmployeeId ? `${line.CustodianEmployeeCode} (you)` : line.CustodianEmployeeCode}</td>
                    <td className="text-right mono">{line.IssuedQuantityBase}</td>
                    <td className="text-right mono">{line.FittedQuantityBase}</td>
                    <td className="text-right mono">{line.ReturnedQuantityBase}</td>
                    <td className="text-right mono">{line.ReturnedLateQuantityBase || '—'}</td>
                    <td className="text-right mono">{line.ExplainedLostQuantityBase || '—'}</td>
                    <td className="text-right mono">{line.ExplainedScrappedQuantityBase || '—'}</td>
                    <td className="text-right mono">{line.UnexplainedQuantityBase || '—'}</td>
                    <td><StatusBadge value={line.Classification} /></td>
                    <td>
                      {canExplain && line.UnexplainedQuantityBase > 0 && !explaining && (
                        <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => { setExplaining(line); setQuantity(String(line.UnexplainedQuantityBase)); setReason('') }}>Explain</button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  )
}
