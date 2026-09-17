import { useEffect, useMemo, useRef, useState } from 'react'
import { listAvailableConditionLocations } from '../../api/qc'
import { newIdempotencyKey } from '../../api/stores'
import type {
  QcInspectionPolicy,
  QcParameterResultRequest,
  QcSerialDispositionRequest,
  SerialDispositionValue,
  WarehouseConditionLocation,
} from '../../types/qc'
import { ErrorAlert } from '../../components/ErrorAlert'

export interface QcSerialSource {
  inventorySerialId: string
  serialNumber: string
}

export interface QcDispositionValues {
  inspectionStartedAt: string
  acceptedQuantity: number
  rejectedQuantity: number
  discrepancyPendingQuantity: number
  acceptedConditionLocationId: string | null
  parameterResults: QcParameterResultRequest[]
  serialDispositions: QcSerialDispositionRequest[]
}

interface Props {
  /** The lot allocation quantity — accepted + rejected + discrepancy must equal it. */
  quantity: number
  /** Serialized units on this lot allocation; empty for non-serialized items. */
  serials: QcSerialSource[]
  hasEffectivePolicy: boolean
  /**
   * Policies effective for the lot's item (or category). The server demands
   * samples 1..SampleSize for each; an empty list with hasEffectivePolicy
   * means the policies could not be read and finalize is blocked.
   */
  policies: QcInspectionPolicy[]
  initial?: Partial<{
    inspectionStartedAt: string
    acceptedQuantity: number
    rejectedQuantity: number
    acceptedConditionLocationId: string | null
    serialDispositions: { InventorySerialId: string; Disposition: string }[]
  }>
  /** Correction mode adds the mandatory reason field. */
  correction?: boolean
  busy: boolean
  submitLabel: string
  /**
   * Called with validated values and the idempotency key to send. The key is
   * stable while the payload is unchanged (safe retry) and regenerated when
   * the user edits anything, which is what the server's fingerprint check wants.
   */
  onSubmit: (values: QcDispositionValues, idempotencyKey: string, correctionReason: string) => Promise<void>
}

interface DraftSample {
  policyId: string
  sampleOrdinal: number
  observed: string
  result: 'PASS' | 'FAIL' | ''
  remarks: string
}

interface DraftSerial {
  inventorySerialId: string
  serialNumber: string
  disposition: SerialDispositionValue | ''
  reason: string
}

function toLocalInput(iso: string): string {
  const date = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

/**
 * The disposition entry shared by finalize and correction. It mirrors the
 * server rules in EfQcWorkflowService so the user is told before the round
 * trip: quantities must reconcile to the lot, accepted stock needs an
 * AVAILABLE location, serialized units must be dispositioned one-for-one,
 * and a lot with no effective QC policy fails closed as discrepancy-pending.
 */
export function QcDispositionForm({
  quantity,
  serials,
  hasEffectivePolicy,
  policies,
  initial,
  correction = false,
  busy,
  submitLabel,
  onSubmit,
}: Props) {
  const [startedAt, setStartedAt] = useState(toLocalInput(initial?.inspectionStartedAt ?? new Date().toISOString()))
  const [accepted, setAccepted] = useState(initial?.acceptedQuantity ? String(initial.acceptedQuantity) : '')
  const [rejected, setRejected] = useState(initial?.rejectedQuantity ? String(initial.rejectedQuantity) : '')
  const [locationId, setLocationId] = useState(initial?.acceptedConditionLocationId ?? '')
  const [reason, setReason] = useState('')
  const [draftSerials, setDraftSerials] = useState<DraftSerial[]>(() =>
    serials.map((serial) => {
      const prior = initial?.serialDispositions?.find((d) => d.InventorySerialId === serial.inventorySerialId)
      const disposition = prior?.Disposition === 'ACCEPTED' || prior?.Disposition === 'REJECTED' ? prior.Disposition : ''
      return { ...serial, disposition, reason: '' }
    }),
  )
  const [draftSamples, setDraftSamples] = useState<DraftSample[]>(() =>
    policies.flatMap((policy) =>
      Array.from({ length: policy.SampleSize }, (_, i) => ({
        policyId: policy.Id, sampleOrdinal: i + 1, observed: '', result: '' as const, remarks: '',
      })),
    ),
  )
  const [locations, setLocations] = useState<WarehouseConditionLocation[]>([])
  const [locationError, setLocationError] = useState<unknown>(null)
  const [error, setError] = useState<unknown>(null)

  // Idempotency: one key per distinct payload.
  const keyRef = useRef<{ fingerprint: string; key: string } | null>(null)

  useEffect(() => {
    listAvailableConditionLocations()
      .then((rows) => { setLocations(rows); setLocationError(null) })
      .catch((err) => setLocationError(err))
  }, [])

  // Fails closed on the server: without an effective policy nothing may be
  // accepted or rejected, the whole lot stays in QC_HOLD as discrepancy-pending.
  const policyMissing = !hasEffectivePolicy
  const acceptedNumber = policyMissing ? 0 : Number(accepted) || 0
  const rejectedNumber = policyMissing ? 0 : Number(rejected) || 0
  const discrepancy = Math.round((quantity - acceptedNumber - rejectedNumber) * 1e6) / 1e6
  const serialized = serials.length > 0

  const policiesUnreadable = hasEffectivePolicy && policies.length === 0
  const policyById = useMemo(() => new Map(policies.map((policy) => [policy.Id, policy])), [policies])
  const isNumeric = (policy: QcInspectionPolicy) => policy.LowerLimit !== null || policy.UpperLimit !== null

  const acceptedSerials = useMemo(() => draftSerials.filter((s) => s.disposition === 'ACCEPTED').length, [draftSerials])
  const rejectedSerials = useMemo(() => draftSerials.filter((s) => s.disposition === 'REJECTED').length, [draftSerials])

  const clientChecks = (): string | null => {
    if (!startedAt) return 'Inspection start time is required.'
    if (acceptedNumber < 0 || rejectedNumber < 0) return 'Quantities cannot be negative.'
    if (discrepancy < 0) return `Accepted (${acceptedNumber}) + rejected (${rejectedNumber}) exceeds the lot quantity of ${quantity}.`
    if (acceptedNumber + rejectedNumber + discrepancy <= 0) return 'The lot quantity is zero — nothing to inspect.'
    if (acceptedNumber > 0 && !locationId) return 'Pick the AVAILABLE location the accepted stock moves to.'
    if (policiesUnreadable) {
      return 'This item has an effective QC parameter policy but it could not be read, so the sample results the server requires cannot be captured. Check the qc.inspection-policies View permission.'
    }
    // Mirrors AddParameterResults: every sample needs an observation and a
    // PASS/FAIL, and a numeric observation must agree with the policy limits.
    for (const sample of draftSamples) {
      const policy = policyById.get(sample.policyId)
      if (!policy) continue
      const label = `${policy.ParameterCode} sample ${sample.sampleOrdinal}`
      if (!sample.result) return `${label}: pick PASS or FAIL.`
      if (!sample.observed.trim()) return `${label}: an observation is required.`
      if (isNumeric(policy)) {
        const value = Number(sample.observed)
        if (!Number.isFinite(value)) return `${label}: the observation must be a number (${policy.MeasurementUomCode}).`
        const within = (policy.LowerLimit === null || value >= policy.LowerLimit) && (policy.UpperLimit === null || value <= policy.UpperLimit)
        if (within && sample.result !== 'PASS') return `${label}: ${value} is within limits, so the result must be PASS.`
        if (!within && sample.result !== 'FAIL') return `${label}: ${value} is outside limits, so the result must be FAIL.`
      }
    }
    if (serialized) {
      if (!Number.isInteger(acceptedNumber) || !Number.isInteger(rejectedNumber)) return 'Serialized quantities must be whole units.'
      if (acceptedSerials !== acceptedNumber) return `${acceptedSerials} serial(s) marked ACCEPTED but accepted quantity is ${acceptedNumber}. They must match.`
      if (rejectedSerials !== rejectedNumber) return `${rejectedSerials} serial(s) marked REJECTED but rejected quantity is ${rejectedNumber}. They must match.`
    }
    if (correction && !reason.trim()) return 'A correction must state its reason — it reverses a posted inspection.'
    return null
  }

  const submit = async () => {
    const problem = clientChecks()
    if (problem) { setError(problem); return }
    setError(null)
    const values: QcDispositionValues = {
      inspectionStartedAt: new Date(startedAt).toISOString(),
      acceptedQuantity: acceptedNumber,
      rejectedQuantity: rejectedNumber,
      discrepancyPendingQuantity: discrepancy,
      acceptedConditionLocationId: acceptedNumber > 0 ? locationId : null,
      parameterResults: draftSamples.map((sample) => {
        const numeric = isNumeric(policyById.get(sample.policyId)!)
        return {
          QcInspectionPolicyId: sample.policyId,
          SampleOrdinal: sample.sampleOrdinal,
          ObservedNumericValue: numeric ? Number(sample.observed) : null,
          ObservedTextValue: numeric ? null : sample.observed.trim(),
          Result: sample.result as 'PASS' | 'FAIL',
          Remarks: sample.remarks.trim() || null,
        }
      }),
      serialDispositions: draftSerials
        .filter((s) => s.disposition !== '')
        .map((s) => ({
          InventorySerialId: s.inventorySerialId,
          Disposition: s.disposition as SerialDispositionValue,
          Reason: s.reason.trim() || null,
        })),
    }
    const fingerprint = JSON.stringify({ values, reason: reason.trim() })
    if (!keyRef.current || keyRef.current.fingerprint !== fingerprint) {
      keyRef.current = { fingerprint, key: newIdempotencyKey(correction ? 'qc-correct' : 'qc-finalize') }
    }
    try {
      await onSubmit(values, keyRef.current.key, reason.trim())
    } catch (err) {
      setError(err)
    }
  }

  const setSample = (index: number, patch: Partial<DraftSample>) =>
    setDraftSamples((prev) => prev.map((s, i) => (i === index ? { ...s, ...patch } : s)))

  const setSerial = (index: number, patch: Partial<DraftSerial>) =>
    setDraftSerials((prev) => prev.map((s, i) => (i === index ? { ...s, ...patch } : s)))

  return (
    <div className="card">
      {policyMissing && (
        <div className="alert alert-warn" role="status">
          <div className="alert-title">No effective QC policy for this item — the lot stays in QC hold</div>
          <p className="alert-body">
            The server fails closed: nothing can be accepted or rejected without an approved inspection policy for the
            item or its category. Finalizing records the whole quantity as discrepancy-pending. Once a policy is
            approved, the inspection is completed through a correction.
          </p>
        </div>
      )}
      {policiesUnreadable && (
        <div className="alert alert-warn" role="status">
          <div className="alert-title">Parameter results cannot be captured</div>
          <p className="alert-body">
            This item has an effective QC parameter policy, so finalize needs one PASS/FAIL result per required sample,
            but no policy could be read for it. Finalize is blocked until the policy list is readable.
          </p>
        </div>
      )}

      <ErrorAlert error={locationError} fallback="Could not load AVAILABLE locations." />
      <ErrorAlert error={error} fallback="The last action failed." />

      <h2 className="form-section-title">Disposition</h2>
      <div className="form-grid">
        <label className="field">
          <span className="field-label">Inspection started at *</span>
          <input className="input" type="datetime-local" value={startedAt} disabled={busy}
            onChange={(event) => setStartedAt(event.target.value)} />
        </label>
        <label className="field">
          <span className="field-label">Lot quantity</span>
          <input className="input" value={quantity} disabled readOnly />
        </label>
        <label className="field">
          <span className="field-label">Accepted quantity *</span>
          <input className="input" type="number" min="0" step="any" value={policyMissing ? '0' : accepted}
            disabled={busy || policyMissing} onChange={(event) => setAccepted(event.target.value)} />
        </label>
        <label className="field">
          <span className="field-label">Rejected quantity *</span>
          <input className="input" type="number" min="0" step="any" value={policyMissing ? '0' : rejected}
            disabled={busy || policyMissing} onChange={(event) => setRejected(event.target.value)} />
          <span className="field-hint">Rejected stock moves to the pending-return location on the QC rack.</span>
        </label>
        <label className="field">
          <span className="field-label">Discrepancy-pending quantity</span>
          <input className="input" value={discrepancy} disabled readOnly />
          <span className="field-hint">Lot − accepted − rejected. Units not presented stay in QC hold.</span>
        </label>
        <label className="field">
          <span className="field-label">Accepted stock location {acceptedNumber > 0 ? '*' : ''}</span>
          <select className="input" value={locationId} disabled={busy || policyMissing || acceptedNumber <= 0}
            onChange={(event) => setLocationId(event.target.value)}>
            <option value="">Select an AVAILABLE location…</option>
            {locations.map((location) => (
              <option key={location.Id} value={location.Id}>
                {location.WarehouseCode} / {location.BinCode}
              </option>
            ))}
          </select>
          {locations.length === 0 && !locationError && (
            <span className="field-hint">No effective AVAILABLE condition locations are configured.</span>
          )}
        </label>
      </div>

      {policies.length > 0 && (
        <>
          <h2 className="form-section-title">Parameter results ({draftSamples.length} samples)</h2>
          <p className="field-hint">
            One observation and PASS/FAIL per sample. A numeric observation must agree with the approved limits.
          </p>
          <div className="serial-list">
            {draftSamples.map((sample, index) => {
              const policy = policyById.get(sample.policyId)!
              const numeric = isNumeric(policy)
              const limits = numeric
                ? ` (${policy.LowerLimit ?? '-inf'} to ${policy.UpperLimit ?? 'inf'} ${policy.MeasurementUomCode})`
                : ''
              return (
                <div key={`${sample.policyId}-${sample.sampleOrdinal}`} className="serial-row">
                  <span className="mono serial-ordinal">{sample.sampleOrdinal}</span>
                  <span className="mono" title={policy.InspectionMethod}>{policy.ParameterCode}{limits}</span>
                  <input className="input" disabled={busy} type={numeric ? 'number' : 'text'} step="any"
                    placeholder={numeric ? `Observed ${policy.MeasurementUomCode}` : 'Observation'}
                    value={sample.observed} onChange={(event) => setSample(index, { observed: event.target.value })} />
                  <select className="input" disabled={busy} value={sample.result}
                    onChange={(event) => setSample(index, { result: event.target.value as DraftSample['result'] })}>
                    <option value="">Result...</option>
                    <option value="PASS">PASS</option>
                    <option value="FAIL">FAIL</option>
                  </select>
                </div>
              )
            })}
          </div>
        </>
      )}

      {serialized && (
        <>
          <h2 className="form-section-title">Serial dispositions ({draftSerials.length} units)</h2>
          <p className="field-hint">
            Mark exactly {acceptedNumber} ACCEPTED and {rejectedNumber} REJECTED; leave the discrepancy units undecided.
          </p>
          <div className="serial-list">
            {draftSerials.map((serial, index) => (
              <div key={serial.inventorySerialId} className="serial-row">
                <span className="mono serial-ordinal">{index + 1}</span>
                <span className="mono">{serial.serialNumber}</span>
                <select className="input" disabled={busy || policyMissing} value={serial.disposition}
                  onChange={(event) => setSerial(index, { disposition: event.target.value as SerialDispositionValue | '' })}>
                  <option value="">Undecided (discrepancy)</option>
                  <option value="ACCEPTED">ACCEPTED</option>
                  <option value="REJECTED">REJECTED</option>
                </select>
                {serial.disposition === 'REJECTED' ? (
                  <input className="input" placeholder="Rejection reason" disabled={busy}
                    value={serial.reason} onChange={(event) => setSerial(index, { reason: event.target.value })} />
                ) : <span />}
              </div>
            ))}
          </div>
        </>
      )}

      {correction && (
        <>
          <h2 className="form-section-title">Correction reason *</h2>
          <input className="input" placeholder="Why the finalized inspection is being corrected" disabled={busy}
            value={reason} onChange={(event) => setReason(event.target.value)} />
        </>
      )}

      <div className="modal-actions">
        <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void submit()}>
          {submitLabel}
        </button>
      </div>
    </div>
  )
}
