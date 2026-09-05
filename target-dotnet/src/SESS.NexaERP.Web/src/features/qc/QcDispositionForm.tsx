import { useEffect, useMemo, useRef, useState } from 'react'
import { listAvailableConditionLocations } from '../../api/qc'
import { newIdempotencyKey } from '../../api/stores'
import type {
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
  serialDispositions: QcSerialDispositionRequest[]
}

interface Props {
  /** The lot allocation quantity — accepted + rejected + discrepancy must equal it. */
  quantity: number
  /** Serialized units on this lot allocation; empty for non-serialized items. */
  serials: QcSerialSource[]
  hasEffectivePolicy: boolean
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

  const acceptedSerials = useMemo(() => draftSerials.filter((s) => s.disposition === 'ACCEPTED').length, [draftSerials])
  const rejectedSerials = useMemo(() => draftSerials.filter((s) => s.disposition === 'REJECTED').length, [draftSerials])

  const clientChecks = (): string | null => {
    if (!startedAt) return 'Inspection start time is required.'
    if (acceptedNumber < 0 || rejectedNumber < 0) return 'Quantities cannot be negative.'
    if (discrepancy < 0) return `Accepted (${acceptedNumber}) + rejected (${rejectedNumber}) exceeds the lot quantity of ${quantity}.`
    if (acceptedNumber + rejectedNumber + discrepancy <= 0) return 'The lot quantity is zero — nothing to inspect.'
    if (acceptedNumber > 0 && !locationId) return 'Pick the AVAILABLE location the accepted stock moves to.'
    if (hasEffectivePolicy) {
      return 'This item has an effective QC parameter policy, so the server requires parameter sample results. The API has no endpoint to read the policy (parameter codes, sample sizes, limits), so this screen cannot capture them yet — reported as a backend gap.'
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
      {hasEffectivePolicy && (
        <div className="alert alert-warn" role="status">
          <div className="alert-title">Parameter results cannot be captured yet</div>
          <p className="alert-body">
            This item has an effective QC parameter policy, so finalize needs one PASS/FAIL result per required sample.
            The API exposes no read endpoint for QC inspection policies, so the parameter codes and sample sizes are
            unknown to this screen. Reported to the backend team; finalize is blocked until it lands.
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
