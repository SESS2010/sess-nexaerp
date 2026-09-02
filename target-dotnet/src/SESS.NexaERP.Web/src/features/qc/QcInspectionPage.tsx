import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  correctQcInspection,
  finalizeQcRevision,
  getQcInspection,
  isQcMockMode,
  updateQcRevision,
} from '../../api/qc'
import { newIdempotencyKey } from '../../api/stores'
import type { QcDecision, QcInspectionResult, SerialDispositionValue } from '../../types/qc'
import { QC_DECISIONS } from '../../types/qc'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'

interface DraftSerialDisposition {
  inventorySerialId: string
  storedSerialNumber: string
  disposition: SerialDispositionValue | ''
  reason: string
}

/**
 * One QC inspection. The current revision is edited while DRAFT;
 * finalize-and-post makes it immutable and moves the stock (accepted quantity
 * to the routed location, rejected to pending-return). A finalized inspection
 * is only changed by opening a correction, which starts the next revision.
 */
export function QcInspectionPage() {
  const { id = '' } = useParams()
  const [inspection, setInspection] = useState<QcInspectionResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)

  const [inspected, setInspected] = useState('')
  const [accepted, setAccepted] = useState('')
  const [rejected, setRejected] = useState('')
  const [decision, setDecision] = useState<QcDecision | ''>('')
  const [serials, setSerials] = useState<DraftSerialDisposition[]>([])
  const [correctionReason, setCorrectionReason] = useState('')

  const hydrate = useCallback((loaded: QcInspectionResult) => {
    setInspection(loaded)
    const revision = loaded.CurrentRevision
    setInspected(revision.InspectedQuantity ? String(revision.InspectedQuantity) : '')
    setAccepted(revision.AcceptedQuantity ? String(revision.AcceptedQuantity) : '')
    setRejected(revision.RejectedQuantity ? String(revision.RejectedQuantity) : '')
    setDecision(revision.Decision)
    setSerials(
      revision.SerialDispositions.map((serial) => ({
        inventorySerialId: serial.InventorySerialId,
        storedSerialNumber: serial.StoredSerialNumber,
        disposition: serial.Disposition,
        reason: serial.Reason ?? '',
      })),
    )
  }, [])

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      hydrate(await getQcInspection(id))
    } catch (err) {
      setInspection(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [id, hydrate])

  useEffect(() => {
    void load()
  }, [load])

  if (loading && !inspection) return <div className="page"><p>Loading…</p></div>
  if (!inspection) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Inspection not found." />
      </div>
    )
  }

  const revision = inspection.CurrentRevision
  const isDraft = revision.Status === 'DRAFT'
  const quantity = inspection.Quantity
  const inspectedNumber = Number(inspected) || 0
  const acceptedNumber = Number(accepted) || 0
  const rejectedNumber = Number(rejected) || 0
  // Units received but never presented for inspection are auto-rejected.
  const shortfall = Math.max(0, quantity - inspectedNumber)
  const serialized = serials.length > 0

  const clientChecks = (): string | null => {
    if (inspectedNumber <= 0) return 'Inspected quantity is required.'
    if (inspectedNumber > quantity) return `Inspected quantity cannot exceed the received quantity of ${quantity}.`
    if (Math.round((acceptedNumber + rejectedNumber) * 1e6) !== Math.round(inspectedNumber * 1e6)) {
      return `Accepted (${acceptedNumber}) + rejected (${rejectedNumber}) must equal the inspected quantity (${inspectedNumber}).`
    }
    if (!decision) return 'Pick a decision.'
    if (decision === 'ACCEPTED' && rejectedNumber > 0) return 'ACCEPTED means nothing was rejected — use PARTIALLY_ACCEPTED.'
    if (decision === 'REJECTED' && acceptedNumber > 0) return 'REJECTED means nothing was accepted — use PARTIALLY_ACCEPTED.'
    if (serialized) {
      const undecided = serials.filter((serial) => !serial.disposition)
      if (undecided.length > 0) return `${undecided.length} serial(s) still need an accept/reject disposition.`
      const acceptedSerials = serials.filter((serial) => serial.disposition === 'ACCEPTED').length
      if (acceptedSerials !== acceptedNumber) {
        return `${acceptedSerials} serials accepted but accepted quantity is ${acceptedNumber}. They must reconcile.`
      }
      const missingReasons = serials.filter((serial) => serial.disposition === 'REJECTED' && !serial.reason.trim())
      if (missingReasons.length > 0) return 'Every rejected serial needs a reason.'
    }
    return null
  }

  const save = async (): Promise<QcInspectionResult | null> => {
    const problem = clientChecks()
    if (problem) {
      setError(problem)
      return null
    }
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      const result = await updateQcRevision(revision.Id, {
        InspectedQuantity: inspectedNumber,
        AcceptedQuantity: acceptedNumber,
        RejectedQuantity: rejectedNumber,
        InspectionShortfallRejectedQuantity: shortfall,
        Decision: decision as QcDecision,
        AcceptedConditionLocationId: revision.AcceptedConditionLocationId,
        ParameterResults: [],
        SerialDispositions: serials.map((serial) => ({
          InventorySerialId: serial.inventorySerialId,
          Disposition: serial.disposition as SerialDispositionValue,
          Reason: serial.reason.trim() || null,
        })),
        Version: revision.Version,
      })
      hydrate(result)
      setNotice('Revision saved.')
      return result
    } catch (err) {
      setError(err)
      return null
    } finally {
      setBusy(false)
    }
  }

  const finalize = async () => {
    // Save first so what gets finalized is exactly what is on screen.
    const saved = await save()
    if (!saved) return
    setBusy(true)
    try {
      const result = await finalizeQcRevision(saved.CurrentRevision.Id, {
        Version: saved.CurrentRevision.Version,
        IdempotencyKey: newIdempotencyKey('qc-finalize'),
      })
      hydrate(result)
      setNotice(
        `Inspection finalized. Accepted stock posted${result.StockPostingBatchId ? ` (batch ${result.StockPostingBatchId})` : ''}.`,
      )
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const correct = async () => {
    if (!correctionReason.trim()) {
      setError('A correction must state its reason — it reopens a posted inspection.')
      return
    }
    setError(null)
    setBusy(true)
    try {
      const result = await correctQcInspection(inspection.Id, {
        Reason: correctionReason.trim(),
        Version: revision.Version,
      })
      hydrate(result)
      setCorrectionReason('')
      setNotice(`Correction opened — revision ${result.CurrentRevision.RevisionNumber} is now editable.`)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const setSerial = (index: number, patch: Partial<DraftSerialDisposition>) =>
    setSerials((prev) => prev.map((serial, i) => (i === index ? { ...serial, ...patch } : serial)))

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="mono">{inspection.InspectionNumber}</h1>
          <p className="page-sub">
            <span className="mono">{inspection.SourceNumber}</span> · <span className="mono">{inspection.ItemCode}</span> {inspection.ItemName} · {quantity} received
            {inspection.QcDueAt && <> · due {new Date(inspection.QcDueAt).toLocaleString('en-IN')}</>}
          </p>
        </div>
        <div className="action-row">
          <StatusBadge value={revision.Status} />
          {revision.RevisionKind === 'CORRECTION' && <span className="badge badge-warn">Correction #{revision.RevisionNumber}</span>}
        </div>
      </div>

      {isQcMockMode() && (
        <div className="alert alert-warn" role="status">
          <span className="alert-title">Mock data — QC backend not live.</span> Nothing on this page reaches the server.
        </div>
      )}

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />

      <div className="card">
        <h2 className="form-section-title">Inspection result</h2>
        <div className="form-grid">
          <label className="field">
            <span className="field-label">Inspected quantity *</span>
            <input className="input" type="number" min="0" step="any" disabled={!isDraft}
              value={inspected} onChange={(event) => setInspected(event.target.value)} />
            {shortfall > 0 && (
              <span className="field-hint">{shortfall} unit(s) not presented — auto-rejected as inspection shortfall on finalize.</span>
            )}
          </label>
          <label className="field">
            <span className="field-label">Decision *</span>
            <select className="input" disabled={!isDraft} value={decision}
              onChange={(event) => setDecision(event.target.value as QcDecision | '')}>
              <option value="">Select…</option>
              {QC_DECISIONS.map((option) => <option key={option} value={option}>{option}</option>)}
            </select>
          </label>
          <label className="field">
            <span className="field-label">Accepted quantity *</span>
            <input className="input" type="number" min="0" step="any" disabled={!isDraft}
              value={accepted} onChange={(event) => setAccepted(event.target.value)} />
          </label>
          <label className="field">
            <span className="field-label">Rejected quantity *</span>
            <input className="input" type="number" min="0" step="any" disabled={!isDraft}
              value={rejected} onChange={(event) => setRejected(event.target.value)} />
            <span className="field-hint">Accepted + rejected must equal inspected. Rejected stock moves to pending-return.</span>
          </label>
        </div>

        {serialized && (
          <>
            <h2 className="form-section-title">Serial dispositions</h2>
            <div className="serial-list">
              {serials.map((serial, index) => (
                <div key={serial.inventorySerialId} className="serial-row">
                  <span className="mono serial-ordinal">{index + 1}</span>
                  <span className="mono">{serial.storedSerialNumber}</span>
                  <select className="input" disabled={!isDraft} value={serial.disposition}
                    onChange={(event) => setSerial(index, { disposition: event.target.value as SerialDispositionValue | '' })}>
                    <option value="">Decide…</option>
                    <option value="ACCEPTED">ACCEPTED</option>
                    <option value="REJECTED">REJECTED</option>
                  </select>
                  {serial.disposition === 'REJECTED' ? (
                    <input className="input" placeholder="Rejection reason *" disabled={!isDraft}
                      value={serial.reason} onChange={(event) => setSerial(index, { reason: event.target.value })} />
                  ) : <span />}
                </div>
              ))}
            </div>
          </>
        )}

        {revision.ParameterResults.length === 0 && (
          <p className="field-hint">
            No effective QC parameter policy for this item — parameter results are not required and never block finalization.
          </p>
        )}

        {isDraft && (
          <div className="modal-actions">
            <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => void save()}>Save draft</button>
            <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void finalize()}>
              Finalize &amp; post stock
            </button>
          </div>
        )}
      </div>

      {!isDraft && (
        <div className="reverse-panel">
          <h2 className="form-section-title">Correct this inspection</h2>
          <p className="field-hint">
            A finalized inspection is immutable — a correction opens the next revision and re-posts the stock difference.
          </p>
          <div className="toolbar">
            <input className="input search" placeholder="Reason for correction (mandatory)"
              value={correctionReason} onChange={(event) => setCorrectionReason(event.target.value)} />
            <button type="button" className="btn btn-warn" disabled={busy} onClick={() => void correct()}>Open correction</button>
          </div>
        </div>
      )}

      {inspection.PriorRevisions.length > 0 && (
        <>
          <h2 className="form-section-title">Revision history</h2>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr><th>#</th><th>Kind</th><th>Inspected</th><th>Accepted</th><th>Rejected</th><th>Decision</th><th>Reason</th></tr>
              </thead>
              <tbody>
                {inspection.PriorRevisions.map((prior) => (
                  <tr key={prior.Id}>
                    <td className="mono">{prior.RevisionNumber}</td>
                    <td>{prior.RevisionKind}</td>
                    <td className="text-right mono">{prior.InspectedQuantity}</td>
                    <td className="text-right mono">{prior.AcceptedQuantity}</td>
                    <td className="text-right mono">{prior.RejectedQuantity}</td>
                    <td><StatusBadge value={prior.Decision || '—'} /></td>
                    <td>{prior.CorrectionReason ?? '—'}</td>
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
