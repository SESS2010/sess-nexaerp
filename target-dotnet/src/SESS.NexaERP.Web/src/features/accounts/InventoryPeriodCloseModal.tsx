import { useRef, useState } from 'react'
import {
  closeInventoryPeriod,
  formatPeriodDateWords,
  isDefinitiveRefusal,
  newInventoryPeriodKey,
} from '../../api/inventoryPeriods'
import type { InventoryPeriodView } from '../../types/inventoryPeriod'
import { ErrorAlert } from '../../components/ErrorAlert'

interface Props {
  period: InventoryPeriodView
  onClose: () => void
  onClosed: (period: InventoryPeriodView) => void
  /** Re-reads the list; offered when the period changed underneath (stale version). */
  onReload: () => void
}

/**
 * Closes an OPEN inventory period (POST /{id}/close, CFO). Irreversible:
 * the reason is entered first, then an in-page confirmation states the
 * consequence in plain words before anything is sent.
 */
export function InventoryPeriodCloseModal({ period, onClose, onClosed, onReload }: Props) {
  const [reason, setReason] = useState('')
  const [step, setStep] = useState<'form' | 'confirm'>('form')
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  // Kept across unknown-outcome retries so a replay returns the closed period;
  // renewed after a definitive refusal.
  const keyRef = useRef(newInventoryPeriodKey('inventory-period-close'))

  const review = (event: React.FormEvent) => {
    event.preventDefault()
    const text = reason.trim()
    if (!text || text.length > 2000) {
      setError('A reason for closing is required (at most 2000 characters).')
      return
    }
    setError(null)
    setStep('confirm')
  }

  const send = async () => {
    if (saving) return
    setError(null)
    setSaving(true)
    try {
      onClosed(await closeInventoryPeriod(period.Id, {
        Version: period.Version,
        Reason: reason.trim(),
        IdempotencyKey: keyRef.current,
      }))
    } catch (err) {
      if (isDefinitiveRefusal(err)) keyRef.current = newInventoryPeriodKey('inventory-period-close')
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{step === 'form' ? `Close ${period.Code}` : 'Check before closing'}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>✕</button>
        </div>

        <div className="card">
          <div className="detail-grid">
            <div className="detail-field"><span className="field-label">Period</span><span className="mono">{period.Code}</span></div>
            <div className="detail-field"><span className="field-label">Name</span>{period.Name}</div>
            <div className="detail-field"><span className="field-label">From</span>{formatPeriodDateWords(period.StartDate)}</div>
            <div className="detail-field"><span className="field-label">To</span>{formatPeriodDateWords(period.EndDate)}</div>
          </div>
        </div>

        {step === 'form' && (
          <form onSubmit={review} className="form-grid" noValidate>
            <label className="field field-wide">
              <span className="field-label">Reason for closing *</span>
              <textarea className="input" rows={3} maxLength={2000} value={reason} onChange={(event) => setReason(event.target.value)} />
              <span className="field-hint">Recorded against your CFO assignment in the period's decision history.</span>
            </label>
            <ErrorAlert error={error} className="field-wide" fallback="The period was not closed." />
            <div className="field-wide modal-actions">
              <button type="button" className="btn btn-ghost" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary">Review…</button>
            </div>
          </form>
        )}

        {step === 'confirm' && (
          <div className="form-grid">
            <div className="field-wide alert alert-warn">
              <div className="alert-title">Closing cannot be undone.</div>
              <p className="alert-body">
                A closed period can never be reopened, and <strong>no stock can be adjusted into it</strong> — not by you, not by
                anyone. Every stock adjustment dated from {formatPeriodDateWords(period.StartDate)} to{' '}
                {formatPeriodDateWords(period.EndDate)} must already be posted. Its dates also stay reserved: no new period can
                cover these days.
              </p>
            </div>
            <div className="field-wide card">
              <p className="field-hint">Reason: {reason.trim()}</p>
            </div>

            <ErrorAlert error={error} onReload={onReload} className="field-wide" fallback="The period was not closed." />

            <div className="field-wide modal-actions">
              <button type="button" className="btn btn-ghost" disabled={saving} onClick={() => { setError(null); setStep('form') }}>
                ‹ Go back
              </button>
              <button type="button" className="btn btn-warn" disabled={saving} onClick={() => void send()}>
                {saving ? 'Closing…' : `Yes, close ${period.Code} for good`}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
