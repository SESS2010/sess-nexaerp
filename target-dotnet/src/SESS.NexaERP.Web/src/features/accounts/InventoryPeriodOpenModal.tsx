import { useRef, useState, type FormEvent } from 'react'
import {
  formatPeriodDateWords,
  inclusiveDays,
  isDefinitiveRefusal,
  newInventoryPeriodKey,
  openInventoryPeriod,
  rangesOverlap,
} from '../../api/inventoryPeriods'
import type { InventoryPeriodView } from '../../types/inventoryPeriod'
import { ApiError } from '../../api/client'
import { ErrorAlert } from '../../components/ErrorAlert'

interface Props {
  /** Every period already in the company, open or closed: none may overlap the new one. */
  existing: InventoryPeriodView[]
  onClose: () => void
  onOpened: (period: InventoryPeriodView) => void
}

type FieldErrors = Partial<Record<'Code' | 'Name' | 'StartDate' | 'EndDate' | 'Reason', string>>

const DATE = /^\d{4}-\d{2}-\d{2}$/

/**
 * Opens an inventory period (POST /api/v1/accounts/inventory-periods, CFO).
 * Two steps: the form, then an in-page check that says in plain words what
 * becomes permanent. Code, name and dates can never be edited afterwards.
 */
export function InventoryPeriodOpenModal({ existing, onClose, onOpened }: Props) {
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [reason, setReason] = useState('')

  const [step, setStep] = useState<'form' | 'confirm'>('form')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  // One key per submission, kept across retries whose outcome is unknown
  // (network, renewed sign-in, 5xx) so a replay returns the period already
  // opened. After a definitive refusal nothing was committed: fresh key.
  const keyRef = useRef(newInventoryPeriodKey('inventory-period-open'))

  const overlapping = DATE.test(startDate) && DATE.test(endDate) && startDate <= endDate
    ? existing.filter((p) => rangesOverlap(startDate, endDate, p.StartDate, p.EndDate))
    : []

  const validate = (): FieldErrors => {
    const errors: FieldErrors = {}
    const c = code.trim()
    if (!c || c.length > 30) errors.Code = 'Code needs 1 to 30 characters.'
    const n = name.trim()
    if (!n || n.length > 150) errors.Name = 'Name needs 1 to 150 characters.'
    if (!DATE.test(startDate)) errors.StartDate = 'Pick the first day of the period.'
    if (!DATE.test(endDate)) errors.EndDate = 'Pick the last day of the period.'
    else if (DATE.test(startDate) && endDate < startDate) errors.EndDate = 'The last day cannot be before the first day.'
    const r = reason.trim()
    if (!r || r.length > 2000) errors.Reason = 'A reason is required (at most 2000 characters).'
    return errors
  }

  const review = (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    const errors = validate()
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) {
      setError('Some fields need attention. They are marked below.')
      return
    }
    if (overlapping.length > 0) {
      setError(`These dates overlap ${overlapping.map((p) => p.Code).join(', ')}. Inventory periods cannot overlap, even a closed one.`)
      return
    }
    setStep('confirm')
  }

  const send = async () => {
    if (saving) return
    setError(null)
    setSaving(true)
    try {
      onOpened(await openInventoryPeriod({
        Code: code.trim().toUpperCase(),
        Name: name.trim(),
        StartDate: startDate,
        EndDate: endDate,
        Reason: reason.trim(),
        IdempotencyKey: keyRef.current,
      }))
    } catch (err) {
      if (isDefinitiveRefusal(err)) keyRef.current = newInventoryPeriodKey('inventory-period-open')
      setError(err)
      if (err instanceof ApiError && err.status === 400) setStep('form')
    } finally {
      setSaving(false)
    }
  }

  const overlapRefused = error instanceof ApiError && error.status === 409 && /cannot overlap/i.test(error.message)
  const days = inclusiveDays(startDate, endDate)
  const fieldError = (key: keyof FieldErrors) =>
    fieldErrors[key] ? <span className="field-error scan-warning">{fieldErrors[key]}</span> : null

  return (
    <div className="modal-backdrop">
      <div className="modal modal-wide" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{step === 'form' ? 'Open inventory period' : 'Check before opening'}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>✕</button>
        </div>

        {step === 'form' && (
          <form onSubmit={review} className="form-grid" noValidate>
            <label className="field">
              <span className="field-label">Code *</span>
              <input className="input mono" value={code} maxLength={30} onChange={(event) => setCode(event.target.value)} placeholder="e.g. INV-2026-10" />
              <span className="field-hint">Up to 30 characters; saved in capitals.</span>
              {fieldError('Code')}
            </label>
            <label className="field">
              <span className="field-label">Name *</span>
              <input className="input" value={name} maxLength={150} onChange={(event) => setName(event.target.value)} placeholder="e.g. October 2026" />
              {fieldError('Name')}
            </label>
            <label className="field">
              <span className="field-label">First day (inclusive) *</span>
              <input className="input" type="date" value={startDate} onChange={(event) => setStartDate(event.target.value)} />
              {fieldError('StartDate')}
            </label>
            <label className="field">
              <span className="field-label">Last day (inclusive) *</span>
              <input className="input" type="date" value={endDate} min={startDate || undefined} onChange={(event) => setEndDate(event.target.value)} />
              {fieldError('EndDate')}
            </label>
            <label className="field field-wide">
              <span className="field-label">Reason *</span>
              <textarea className="input" rows={2} maxLength={2000} value={reason} onChange={(event) => setReason(event.target.value)} />
              <span className="field-hint">Recorded against your CFO assignment in the period's decision history.</span>
              {fieldError('Reason')}
            </label>

            {overlapping.length > 0 && (
              <div className="field-wide alert alert-warn">
                These dates overlap {overlapping.map((p) => `${p.Code} (${p.Status})`).join(', ')}. Inventory periods cannot
                overlap, including closed ones — pick dates outside every existing period.
              </div>
            )}
            <ErrorAlert error={error} className="field-wide" fallback="The period was not opened." />

            <div className="field-wide modal-actions">
              <button type="button" className="btn btn-ghost" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary">Review…</button>
            </div>
          </form>
        )}

        {step === 'confirm' && (
          <div className="form-grid">
            <div className="field-wide alert alert-warn">
              <div className="alert-title">An inventory period is permanent. Check twice.</div>
              <p className="alert-body">
                Once opened, <strong>the code, name and dates can never be changed</strong>, and the period cannot be deleted.
                No other inventory period may ever cover any of these days, even after this one is closed.
              </p>
            </div>

            <div className="field-wide card">
              <p>
                Period <strong className="mono">{code.trim().toUpperCase()}</strong> — <strong>{name.trim()}</strong>
              </p>
              <p>
                from <strong>{formatPeriodDateWords(startDate)}</strong> to <strong>{formatPeriodDateWords(endDate)}</strong>
                {days !== null && <> ({days} {days === 1 ? 'day' : 'days'}, both ends included)</>}
              </p>
              <p>opens now, and stock adjustments dated inside it can be posted until it is closed.</p>
              <p className="field-hint">Reason: {reason.trim()}</p>
            </div>

            {overlapRefused && (
              <div className="alert alert-warn field-wide">
                The server found another inventory period on some of these days, so nothing was opened. Go back and pick dates
                outside every existing period.
              </div>
            )}
            <ErrorAlert error={error} className="field-wide" fallback="The period was not opened." />

            <div className="field-wide modal-actions">
              <button type="button" className="btn btn-ghost" disabled={saving} onClick={() => { setError(null); setStep('form') }}>
                ‹ Go back and change
              </button>
              <button type="button" className="btn btn-primary" disabled={saving} onClick={() => void send()}>
                {saving ? 'Opening…' : `Yes, open ${code.trim().toUpperCase()}`}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
