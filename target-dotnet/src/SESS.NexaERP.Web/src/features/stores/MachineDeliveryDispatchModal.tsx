import { useRef, useState } from 'react'
import {
  dispatchMachine,
  fieldErrorsOf,
  formatDateWords,
  isBusinessRuleConflict,
  isDefinitiveRefusal,
  newIdempotencyKey,
  todayIst,
} from '../../api/machineDeliveries'
import { ApiError } from '../../api/client'
import type {
  DispatchMachineRequest,
  MachineDeliveryJobOrderCandidate,
  MachineDeliveryNature,
  MachineDeliveryPurpose,
  MachineDeliveryView,
} from '../../types/machineDelivery'
import { NATURES, NATURE_WORDS, PURPOSES_BY_NATURE, PURPOSE_WORDS } from '../../types/machineDelivery'
import { ErrorAlert } from '../../components/ErrorAlert'

interface Props {
  job: MachineDeliveryJobOrderCandidate
  onClose: () => void
  onDispatched: (dc: MachineDeliveryView) => void
}

type FieldErrors = Record<string, string[]>

const MAX_DC_NUMBER = 100
const MAX_DESTINATION = 500

/** Client-side mirror of MachineDeliveryRequestValidation.Dispatch, keyed the same way. */
function validate(draft: {
  dcNumber: string
  nature: MachineDeliveryNature | ''
  purpose: MachineDeliveryPurpose | ''
  dispatchDate: string
  expectedReturnDate: string
  destination: string
}): FieldErrors {
  const errors: FieldErrors = {}
  const add = (field: string, message: string) => { errors[field] = [...(errors[field] ?? []), message] }
  const dc = draft.dcNumber.trim()
  if (!dc) add('DcNumber', 'Enter the DC number printed on the challan.')
  else if (dc.length > MAX_DC_NUMBER) add('DcNumber', `At most ${MAX_DC_NUMBER} characters.`)
  const destination = draft.destination.trim()
  if (!destination) add('Destination', 'Enter where the machine is going.')
  else if (destination.length > MAX_DESTINATION) add('Destination', `At most ${MAX_DESTINATION} characters.`)
  if (!draft.nature) add('Nature', 'Choose whether the machine comes back or not.')
  if (!draft.purpose) add('Purpose', 'Choose the purpose.')
  else if (draft.nature && !PURPOSES_BY_NATURE[draft.nature].includes(draft.purpose)) add('Purpose', 'This purpose does not go with the chosen nature.')
  const today = todayIst()
  if (!draft.dispatchDate) add('DispatchDate', 'Enter the dispatch date.')
  else if (draft.dispatchDate > today) add('DispatchDate', 'The dispatch date cannot be in the future.')
  if (draft.nature === 'RETURNABLE') {
    if (!draft.expectedReturnDate) add('ExpectedReturnDate', 'A returnable machine needs the date it is expected back.')
    else if (draft.dispatchDate && draft.expectedReturnDate < draft.dispatchDate) add('ExpectedReturnDate', 'The return date cannot be before the dispatch date.')
  }
  return errors
}

function FieldError({ errors, field }: { errors: FieldErrors; field: string }) {
  const messages = errors[field]
  if (!messages?.length) return null
  return <span className="field-error scan-warning">{messages.join(' ')}</span>
}

export function MachineDeliveryDispatchModal({ job, onClose, onDispatched }: Props) {
  // No pre-selected nature, purpose, dispatch date or return date (contract rule 4):
  // an empty required field is safer than a plausible wrong one.
  const [dcNumber, setDcNumber] = useState('')
  const [nature, setNature] = useState<MachineDeliveryNature | ''>('')
  const [purpose, setPurpose] = useState<MachineDeliveryPurpose | ''>('')
  const [dispatchDate, setDispatchDate] = useState('')
  const [expectedReturnDate, setExpectedReturnDate] = useState('')
  const [destination, setDestination] = useState('')

  const [step, setStep] = useState<'form' | 'confirm'>('form')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  // One key per submission, minted when the form opens and kept across retries
  // whose outcome is unknown (network, renewed sign-in, 5xx) so a replay returns
  // the original DC. After a definitive refusal nothing was committed, so the
  // next attempt gets a fresh key.
  const keyRef = useRef(newIdempotencyKey('machine-dc-dispatch'))

  const changeNature = (value: MachineDeliveryNature | '') => {
    setNature(value)
    // The purpose list is bound to the nature; never carry one across.
    setPurpose('')
    if (value !== 'RETURNABLE') setExpectedReturnDate('')
  }

  const draft = { dcNumber, nature, purpose, dispatchDate, expectedReturnDate, destination }

  const review = (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    const errors = validate(draft)
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) {
      setError('Some fields need attention. They are marked below.')
      return
    }
    setStep('confirm')
  }

  const send = async () => {
    if (saving || !nature || !purpose) return
    setError(null)
    setSaving(true)
    const body: DispatchMachineRequest = {
      JobOrderId: job.JobOrderId,
      DcNumber: dcNumber.trim(),
      Nature: nature,
      Purpose: purpose,
      DispatchDate: dispatchDate,
      ExpectedReturnDate: nature === 'RETURNABLE' ? expectedReturnDate : null,
      Destination: destination.trim(),
      IdempotencyKey: keyRef.current,
    }
    try {
      onDispatched(await dispatchMachine(body))
    } catch (err) {
      if (isDefinitiveRefusal(err)) keyRef.current = newIdempotencyKey('machine-dc-dispatch')
      const serverFields = fieldErrorsOf(err)
      setFieldErrors(serverFields)
      setError(err)
      // A request fault is fixed on the form, next to the field the server named.
      if (Object.keys(serverFields).length > 0) setStep('form')
    } finally {
      setSaving(false)
    }
  }

  const conflict = isBusinessRuleConflict(error)
  const directorMissing =
    conflict && error instanceof ApiError && /managing director/i.test(error.message)
  // A job with a DC is still listed by the server; say so plainly instead of
  // sending the operator back to fields that are not the problem.
  const alreadyDispatched =
    conflict && error instanceof ApiError && /already has a machine DC/i.test(error.message)
  const conflictLine = alreadyDispatched
    ? 'This job already has its DC, so nothing was dispatched. Open the existing DC instead of dispatching again.'
    : 'The server refused this dispatch: check the nature, purpose and dates.'
  const purposes = nature ? PURPOSES_BY_NATURE[nature] : []
  const today = todayIst()

  return (
    <div className="modal-backdrop">
      <div className="modal modal-wide" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{step === 'form' ? 'Dispatch machine' : 'Check before dispatch'}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>✕</button>
        </div>

        <div className="card">
          <div className="detail-grid">
            <div className="detail-field"><span className="field-label">Job order</span><span className="mono">{job.JobOrderNumber}</span></div>
            <div className="detail-field"><span className="field-label">Machine serial</span><span className="mono">{job.MachineSerial}</span></div>
            <div className="detail-field"><span className="field-label">Machine model</span>{job.MachineModel}</div>
            <div className="detail-field"><span className="field-label">Customer</span>{job.CustomerName}</div>
          </div>
          <p className="field-hint">
            Customer, customer PO, machine serial and model are taken from the job order by the server and cannot be edited here.
          </p>
        </div>

        {step === 'form' && (
          <form onSubmit={review} className="form-grid" noValidate>
            <label className="field">
              <span className="field-label">DC number *</span>
              <input
                className="input mono"
                value={dcNumber}
                maxLength={MAX_DC_NUMBER + 20}
                onChange={(event) => setDcNumber(event.target.value)}
              />
              <span className="field-hint">As printed on the challan. Unique within the company.</span>
              <FieldError errors={fieldErrors} field="DcNumber" />
            </label>

            <label className="field">
              <span className="field-label">Dispatch date *</span>
              <input
                type="date"
                className="input"
                value={dispatchDate}
                max={today}
                onChange={(event) => setDispatchDate(event.target.value)}
              />
              <span className="field-hint">
                Not in the future, and not before the job's FAT reconciliation date.{' '}
                <button type="button" className="btn btn-ghost" onClick={() => setDispatchDate(today)}>Today</button>
              </span>
              <FieldError errors={fieldErrors} field="DispatchDate" />
            </label>

            <fieldset className="field field-wide">
              <span className="field-label">Nature *</span>
              {NATURES.map((option) => (
                <label key={option} className="action-row">
                  <input
                    type="radio"
                    name="machine-dc-nature"
                    checked={nature === option}
                    onChange={() => changeNature(option)}
                  />
                  <span>{NATURE_WORDS[option]}</span>
                </label>
              ))}
              <FieldError errors={fieldErrors} field="Nature" />
            </fieldset>

            <label className="field">
              <span className="field-label">Purpose *</span>
              <select
                className="input"
                value={purpose}
                disabled={!nature}
                onChange={(event) => setPurpose(event.target.value as MachineDeliveryPurpose | '')}
              >
                <option value="">{nature ? 'Choose the purpose…' : 'Choose the nature first'}</option>
                {purposes.map((option) => (
                  <option key={option} value={option}>{PURPOSE_WORDS[option]}</option>
                ))}
              </select>
              <FieldError errors={fieldErrors} field="Purpose" />
            </label>

            {nature === 'RETURNABLE' ? (
              <label className="field">
                <span className="field-label">Expected return date *</span>
                <input
                  type="date"
                  className="input"
                  value={expectedReturnDate}
                  min={dispatchDate || undefined}
                  onChange={(event) => setExpectedReturnDate(event.target.value)}
                />
                <FieldError errors={fieldErrors} field="ExpectedReturnDate" />
              </label>
            ) : (
              <div className="field">
                <span className="field-label">Expected return date</span>
                <span className="field-hint">
                  {nature === 'NON_RETURNABLE' ? 'Not applicable: the machine does not come back.' : 'Asked only for a returnable machine.'}
                </span>
                <FieldError errors={fieldErrors} field="ExpectedReturnDate" />
              </div>
            )}

            <label className="field field-wide">
              <span className="field-label">Destination *</span>
              <textarea
                className="input"
                rows={3}
                value={destination}
                onChange={(event) => setDestination(event.target.value)}
              />
              <span className="field-hint">{destination.trim().length} of {MAX_DESTINATION} characters.</span>
              <FieldError errors={fieldErrors} field="Destination" />
            </label>

            <FieldError errors={fieldErrors} field="JobOrderId" />
            <FieldError errors={fieldErrors} field="IdempotencyKey" />

            {conflict && (
              <div className="alert alert-warn field-wide">{conflictLine}</div>
            )}
            <ErrorAlert error={error} className="field-wide" fallback="The dispatch was not recorded." />

            <div className="field-wide modal-actions">
              <button type="button" className="btn btn-ghost" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary">Review dispatch…</button>
            </div>
          </form>
        )}

        {step === 'confirm' && nature && purpose && (
          <div className="form-grid">
            <div className="field-wide alert alert-warn">
              <div className="alert-title">A machine DC is permanent. Check twice.</div>
              <p className="alert-body">
                Once dispatched, <strong>the DC number, the nature, the job order</strong>, the purpose, the dates and the
                destination can never be changed. There is no correction, cancellation, return or re-dispatch, and this job
                can never have a second DC.
              </p>
            </div>

            <div className="field-wide card">
              <p>
                Machine <strong className="mono">{job.MachineSerial}</strong> ({job.MachineModel}) of job order{' '}
                <strong className="mono">{job.JobOrderNumber}</strong> for <strong>{job.CustomerName}</strong>
              </p>
              <p>
                goes out on challan <strong className="mono">{dcNumber.trim()}</strong> on{' '}
                <strong>{formatDateWords(dispatchDate)}</strong>,
              </p>
              <p>
                <strong>
                  {nature === 'RETURNABLE'
                    ? `returnable, ${PURPOSE_WORDS[purpose]}, to be returned by ${formatDateWords(expectedReturnDate)}`
                    : `non-returnable, ${PURPOSE_WORDS[purpose]}: the machine does not come back`}
                </strong>
                ,
              </p>
              <p>to: <strong>{destination.trim()}</strong></p>
              {nature === 'NON_RETURNABLE' && (
                <p className="field-hint">The Managing Director is notified of a non-returnable dispatch.</p>
              )}
              <p className="field-hint">
                The machine becomes DISPATCHED, not delivered. It is delivered only when the customer's signature is recorded.
              </p>
            </div>

            {conflict && (
              <div className="alert alert-warn field-wide">
                {conflictLine}
                {directorMissing && (
                  <p className="alert-body">
                    This one is an administration problem, not a mistake in the form: the company has no active Managing
                    Director to notify of a non-returnable dispatch. Ask the ERP administrator to set one up, then dispatch again.
                  </p>
                )}
              </div>
            )}
            <ErrorAlert error={error} className="field-wide" fallback="The dispatch was not recorded." />

            <div className="field-wide modal-actions">
              <button type="button" className="btn btn-ghost" disabled={saving} onClick={() => { setError(null); setStep('form') }}>
                ‹ Go back and change
              </button>
              <button type="button" className="btn btn-primary" disabled={saving} onClick={() => void send()}>
                {saving ? 'Dispatching…' : `Yes, dispatch on ${dcNumber.trim()}`}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
