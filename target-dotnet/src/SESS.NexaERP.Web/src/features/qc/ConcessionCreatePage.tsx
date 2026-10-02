import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { createConcession, getQcInspection } from '../../api/qc'
import { newIdempotencyKey } from '../../api/stores'
import type { QcInspectionResult } from '../../types/qc'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'
import {
  NO_INSPECTION_MESSAGE,
  concessionContext,
  concessionRequest,
  validateConcessionDraft,
} from './concessionDraft'

/**
 * Raise a concession against a finalized QC inspection
 * (/qc/concessions/new?inspection=QCI-…&parameter=<result id>).
 *
 * Everything that identifies the rejected stock — the lot disposition, the
 * failed parameter result, the measured value, the rejected quantity and the
 * rejected serials — is read from GET /qc/inspections/{number} and shown as
 * words; the ids travel to the API without the user ever seeing them. The
 * context lives in the address, so a refresh keeps it.
 */
export function ConcessionCreatePage() {
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const { can } = useSession()
  const inspectionNumber = (params.get('inspection') ?? '').trim().toUpperCase()
  const parameterResultId = params.get('parameter')

  const [inspection, setInspection] = useState<QcInspectionResult | null>(null)
  const [loading, setLoading] = useState(Boolean(inspectionNumber))
  const [loadError, setLoadError] = useState<unknown>(null)
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  const [quantity, setQuantity] = useState('')
  const [selectedSerialIds, setSelectedSerialIds] = useState<string[]>([])
  const [intendedUse, setIntendedUse] = useState('')
  const [technicalJustification, setTechnicalJustification] = useState('')
  const keyRef = useRef<{ fingerprint: string; key: string } | null>(null)

  const load = useCallback(async () => {
    if (!inspectionNumber) {
      setInspection(null)
      setLoading(false)
      return
    }
    setLoading(true)
    setLoadError(null)
    try {
      setInspection(await getQcInspection(inspectionNumber))
    } catch (err) {
      setInspection(null)
      setLoadError(err)
    } finally {
      setLoading(false)
    }
  }, [inspectionNumber])

  useEffect(() => {
    void load()
  }, [load])

  const result = useMemo(() => concessionContext(inspection, parameterResultId), [inspection, parameterResultId])

  // The quantity and the serial selection start from the inspection: the whole
  // rejected quantity, and every rejected serial.
  useEffect(() => {
    if (!result.ok) return
    setQuantity(String(result.context.rejectedQuantity))
    setSelectedSerialIds(result.context.rejectedSerials.map((row) => row.inventorySerialId))
  }, [result])

  const canCreate = can(PAGE_KEYS.qc, 'create')

  if (loading) return <div className="page"><p>Loading…</p></div>

  // No inspection, not finalized, nothing rejected, no FAIL parameter: a
  // sentence and a way back to the queue, never a form.
  if (!result.ok) {
    return (
      <div className="page">
        <div className="page-header">
          <div>
            <h1>Raise concession</h1>
            <p className="page-sub">A concession is raised from the inspection that rejected the stock</p>
          </div>
        </div>
        <ErrorAlert error={loadError} onReload={() => void load()} fallback={`Inspection ${inspectionNumber} could not be read.`} />
        <div className="alert alert-warn" role="status">
          <div className="alert-title">{NO_INSPECTION_MESSAGE}</div>
          <p className="alert-body">{result.detail}</p>
        </div>
        <div className="action-row">
          <Link className="btn btn-primary" to="/qc/inspections">Go to the QC queue</Link>
          <Link className="btn btn-ghost" to="/qc/concessions">Open a concession by number</Link>
        </div>
      </div>
    )
  }

  const context = result.context
  const serialized = context.rejectedSerials.length > 0
  const problem = validateConcessionDraft(context, { quantity, selectedSerialIds, technicalJustification, intendedUse })

  const toggleSerial = (inventorySerialId: string) => {
    setSelectedSerialIds((current) => {
      const next = current.includes(inventorySerialId)
        ? current.filter((id) => id !== inventorySerialId)
        : [...current, inventorySerialId]
      setQuantity(String(next.length))
      return next
    })
  }

  const submit = async () => {
    if (problem) { setError(problem); return }
    const body = concessionRequest(context, { quantity, selectedSerialIds, technicalJustification, intendedUse })
    const fingerprint = JSON.stringify(body)
    if (!keyRef.current || keyRef.current.fingerprint !== fingerprint) {
      keyRef.current = { fingerprint, key: newIdempotencyKey('concession-create') }
    }
    setBusy(true)
    setError(null)
    try {
      const created = await createConcession(body, keyRef.current.key)
      navigate(`/qc/concessions/${encodeURIComponent(created.ConcessionNumber)}`, {
        replace: true,
        state: { notice: created.Replayed ? 'This concession was already raised.' : 'Concession raised — it is now waiting for the Technical Director.' },
      })
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Raise concession</h1>
          <p className="page-sub">
            Rejected stock from <span className="mono">{context.inspectionNumber}</span>, offered to the Technical Director for a stated use
          </p>
        </div>
        <div className="action-row">
          <Link className="btn btn-ghost" to={`/qc/inspections/${encodeURIComponent(context.inspectionNumber)}`}>‹ Inspection</Link>
        </div>
      </div>

      <ErrorAlert error={error} fallback="The concession was not raised." />

      <div className="card">
        <h2 className="form-section-title">What is being conceded</h2>
        <div className="detail-grid">
          <div><span className="field-label">Inspection</span><div className="mono">{context.inspectionNumber}</div></div>
          <div><span className="field-label">GRN</span><div className="mono">{context.grnNumber}</div></div>
          <div><span className="field-label">Item</span><div className="mono">{context.itemCode}</div></div>
          <div><span className="field-label">Lot</span><div className="mono">{context.lotOrdinal}</div></div>
          <div><span className="field-label">Failed parameter</span><div>{context.failedParameter}</div></div>
          <div><span className="field-label">Measured value</span><div className="mono">{context.measuredValue}</div></div>
          <div><span className="field-label">Rejected quantity</span><div className="mono">{context.rejectedQuantity}</div></div>
        </div>
        <p className="field-hint">Read from the inspection — nothing here is typed by hand.</p>
      </div>

      <div className="card">
        <h2 className="form-section-title">Your request</h2>
        <div className="form-grid">
          <label className="field">
            <span className="field-label">Quantity *</span>
            <input
              className="input"
              type="number"
              min="0"
              step="any"
              value={quantity}
              disabled={busy || serialized}
              onChange={(event) => setQuantity(event.target.value)}
            />
            <span className="field-hint">
              {serialized
                ? 'Follows the serials you select below.'
                : `Cannot exceed ${context.rejectedQuantity}; a non-serialized concession must cover the whole rejected quantity.`}
            </span>
          </label>

          {serialized && (
            <div className="field field-wide">
              <span className="field-label">Rejected serials this concession covers *</span>
              <div className="serial-list">
                {context.rejectedSerials.map((serial) => (
                  <label key={serial.inventorySerialId} className="serial-row">
                    <input
                      type="checkbox"
                      checked={selectedSerialIds.includes(serial.inventorySerialId)}
                      disabled={busy}
                      onChange={() => toggleSerial(serial.inventorySerialId)}
                    />
                    <span className="mono">{serial.serialNumber}</span>
                  </label>
                ))}
              </div>
              <span className="field-hint">{selectedSerialIds.length} of {context.rejectedSerials.length} selected.</span>
            </div>
          )}

          <label className="field field-wide">
            <span className="field-label">Stated use *</span>
            <textarea className="input" rows={2} value={intendedUse} disabled={busy}
              placeholder="What this material will be used for if it is accepted"
              onChange={(event) => setIntendedUse(event.target.value)} />
          </label>
          <label className="field field-wide">
            <span className="field-label">Technical reason *</span>
            <textarea className="input" rows={2} value={technicalJustification} disabled={busy}
              placeholder="Why the failed parameter does not matter for that use"
              onChange={(event) => setTechnicalJustification(event.target.value)} />
          </label>
        </div>

        {problem && <p className="field-hint">{problem}</p>}

        <div className="modal-actions">
          <button type="button" className="btn btn-ghost" disabled={busy}
            onClick={() => navigate(`/qc/inspections/${encodeURIComponent(context.inspectionNumber)}`)}>Cancel</button>
          <button type="button" className="btn btn-primary" disabled={busy || !canCreate || Boolean(problem)}
            onClick={() => void submit()}>Raise concession</button>
        </div>
        {!canCreate && <p className="field-hint">Your role may not raise a concession.</p>}
      </div>
    </div>
  )
}
