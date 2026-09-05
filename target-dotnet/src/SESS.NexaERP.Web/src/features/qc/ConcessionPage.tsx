import { useCallback, useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import {
  approveConcession,
  createConcession,
  getConcession,
  listAvailableConditionLocations,
  rejectConcession,
  reverseConcession,
} from '../../api/qc'
import { newIdempotencyKey } from '../../api/stores'
import type { InventoryConcessionResult, WarehouseConditionLocation } from '../../types/qc'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'

interface CreatePrefill {
  failedParameterResultId?: string
  failedParameter?: string
  measuredValue?: string
  inspectionNumber?: string
}

/**
 * Inventory concessions: the QC manager raises one against a rejected lot
 * disposition (a FAIL parameter result), the Technical Director approves it
 * into an AVAILABLE location, rejects it, or later reverses an approval.
 *
 * Route /qc/concessions opens by number or raises a new one;
 * /qc/concessions/:number shows the record and the TD decisions.
 */
export function ConcessionPage() {
  const { number = '' } = useParams()
  const location = useLocation()
  const navigate = useNavigate()
  const { can, hasRole, me } = useSession()
  const prefill = (location.state as { prefill?: CreatePrefill } | null)?.prefill

  const [lookup, setLookup] = useState('')
  const [concession, setConcession] = useState<InventoryConcessionResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)

  // Decision inputs (TD).
  const [locations, setLocations] = useState<WarehouseConditionLocation[]>([])
  const [locationId, setLocationId] = useState('')
  const [decisionReason, setDecisionReason] = useState('')

  // Create inputs (QC manager). The lot disposition id is not exposed by any
  // GET today, so it has to be supplied; reported as a backend gap.
  // POST /api/v1/qc/concessions → qc.inspection-policies:create. The service
  // (CreateConcessionAsync) enforces no role beyond the page grant.
  const canCreate = can(PAGE_KEYS.qc, 'create')
  const [showCreate, setShowCreate] = useState(Boolean(prefill) && canCreate)
  const [lotDispositionId, setLotDispositionId] = useState('')
  const [failedResultId, setFailedResultId] = useState(prefill?.failedParameterResultId ?? '')
  const [quantity, setQuantity] = useState('')
  const [failedParameter, setFailedParameter] = useState(prefill?.failedParameter ?? '')
  const [measuredValue, setMeasuredValue] = useState(prefill?.measuredValue ?? '')
  const [justification, setJustification] = useState('')
  const [intendedUse, setIntendedUse] = useState('')
  const [serialIds, setSerialIds] = useState('')
  const keyRef = useRef<{ fingerprint: string; key: string } | null>(null)

  // Approve/reject (qc.inspection-policies:approve) and reverse (…:cancel) both
  // need a direct TECHNICAL_DIRECTOR role (DecideConcession / ReverseConcessionAsync
  // → RequireTechnicalDirector). The creator may never decide their own concession.
  const isTd = hasRole('TECHNICAL_DIRECTOR')

  const load = useCallback(async (value: string) => {
    if (!value) return
    setLoading(true)
    setError(null)
    try {
      setConcession(await getConcession(value))
    } catch (err) {
      setConcession(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load(number)
  }, [number, load])

  useEffect(() => {
    listAvailableConditionLocations().then(setLocations).catch(() => setLocations([]))
  }, [])

  const keyFor = (prefix: string, payload: unknown): string => {
    const fingerprint = JSON.stringify(payload)
    if (!keyRef.current || keyRef.current.fingerprint !== fingerprint) {
      keyRef.current = { fingerprint, key: newIdempotencyKey(prefix) }
    }
    return keyRef.current.key
  }

  const run = async (label: string, action: () => Promise<InventoryConcessionResult>) => {
    setBusy(true)
    setError(null)
    setNotice('')
    try {
      const result = await action()
      setConcession(result)
      setNotice(result.Replayed ? `${label} was already recorded.` : `${label} done — concession is now ${result.Status}.`)
      setDecisionReason('')
      if (result.ConcessionNumber !== number) navigate(`/qc/concessions/${encodeURIComponent(result.ConcessionNumber)}`, { replace: true })
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const create = async () => {
    const qty = Number(quantity) || 0
    if (!lotDispositionId.trim() || !failedResultId.trim()) { setError('Lot disposition id and failed parameter result id are required.'); return }
    if (qty <= 0) { setError('Quantity must be more than zero.'); return }
    if (!failedParameter.trim() || !measuredValue.trim() || !justification.trim() || !intendedUse.trim()) {
      setError('Failed parameter, measured value, technical justification and intended use are all required.')
      return
    }
    const body = {
      QcInspectionLotDispositionId: lotDispositionId.trim(),
      FailedParameterResultId: failedResultId.trim(),
      Quantity: qty,
      FailedParameter: failedParameter.trim(),
      MeasuredValue: measuredValue.trim(),
      TechnicalJustification: justification.trim(),
      IntendedUse: intendedUse.trim(),
      InventorySerialIds: serialIds.split(/[\s,]+/).map((s) => s.trim()).filter(Boolean),
    }
    await run('Concession raised', () => createConcession(body, keyFor('concession-create', body)))
    setShowCreate(false)
  }

  const approve = () => {
    if (!concession) return
    if (!locationId) { setError('Pick the AVAILABLE location the accepted stock moves to.'); return }
    if (!decisionReason.trim()) { setError('A decision reason is required.'); return }
    const body = { Version: concession.Version, AvailableConditionLocationId: locationId, DecisionReason: decisionReason.trim() }
    return run('Approval', () => approveConcession(concession.ConcessionNumber, body, keyFor('concession-approve', body)))
  }

  const reject = () => {
    if (!concession) return
    if (!decisionReason.trim()) { setError('A decision reason is required.'); return }
    return run('Rejection', () => rejectConcession(concession.ConcessionNumber, { Version: concession.Version, DecisionReason: decisionReason.trim() }))
  }

  const reverse = () => {
    if (!concession) return
    if (!decisionReason.trim()) { setError('A reason is required to reverse an approved concession.'); return }
    const body = { Version: concession.Version, Reason: decisionReason.trim() }
    return run('Reversal', () => reverseConcession(concession.ConcessionNumber, body, keyFor('concession-reverse', body)))
  }

  // The creator is refused by the server (UnauthorizedAccessException); when the
  // session is not loaded yet the control stays visible, like can()/hasRole().
  const notCreator = !me || !concession || concession.CreatedByEmployeeId !== me.EmployeeId
  const canDecide = concession?.Status === 'DRAFT' && can(PAGE_KEYS.qc, 'approve') && isTd && notCreator
  const canReverse = concession?.Status === 'APPROVED' && can(PAGE_KEYS.qc, 'cancel') && isTd

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>{concession ? <span className="mono">{concession.ConcessionNumber}</span> : 'Concessions'}</h1>
          <p className="page-sub">
            Rejected QC stock accepted by the Technical Director for a stated use — approval moves it to an AVAILABLE location with a provenance annotation
          </p>
        </div>
        <div className="action-row">
          {concession && <StatusBadge value={concession.Status} />}
          {canCreate && (
            <button type="button" className="btn btn-primary" onClick={() => setShowCreate((value) => !value)}>
              {showCreate ? 'Close form' : '+ Raise concession'}
            </button>
          )}
        </div>
      </div>

      <div className="toolbar">
        <input className="input search" placeholder="Open concession by number, e.g. CON-2627-00001" value={lookup}
          onChange={(event) => setLookup(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter' && lookup.trim()) navigate(`/qc/concessions/${encodeURIComponent(lookup.trim().toUpperCase())}`)
          }} />
        <button type="button" className="btn btn-ghost" disabled={!lookup.trim()}
          onClick={() => navigate(`/qc/concessions/${encodeURIComponent(lookup.trim().toUpperCase())}`)}>Open</button>
      </div>

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load(number)} fallback="The last action failed." />

      {showCreate && canCreate && (
        <div className="card">
          <h2 className="form-section-title">Raise concession{prefill?.inspectionNumber ? <> for <span className="mono">{prefill.inspectionNumber}</span></> : null}</h2>
          <div className="alert alert-warn" role="status">
            <div className="alert-title">Lot disposition id must be supplied by hand</div>
            <p className="alert-body">
              The concession is raised against the rejected lot disposition of the finalized QC revision, but no read
              endpoint returns that id (GET /qc/inspections/{'{number}'} omits it). Reported as a backend gap; until it
              lands the id has to come from the database.
            </p>
          </div>
          <div className="form-grid">
            <label className="field">
              <span className="field-label">QC lot disposition id *</span>
              <input className="input mono" value={lotDispositionId} onChange={(event) => setLotDispositionId(event.target.value)} disabled={busy} />
            </label>
            <label className="field">
              <span className="field-label">Failed parameter result id *</span>
              <input className="input mono" value={failedResultId} onChange={(event) => setFailedResultId(event.target.value)} disabled={busy} />
            </label>
            <label className="field">
              <span className="field-label">Quantity *</span>
              <input className="input" type="number" min="0" step="any" value={quantity} onChange={(event) => setQuantity(event.target.value)} disabled={busy} />
              <span className="field-hint">Cannot exceed the rejected quantity of the lot disposition.</span>
            </label>
            <label className="field">
              <span className="field-label">Failed parameter *</span>
              <input className="input" value={failedParameter} onChange={(event) => setFailedParameter(event.target.value)} disabled={busy} />
            </label>
            <label className="field">
              <span className="field-label">Measured value *</span>
              <input className="input" value={measuredValue} onChange={(event) => setMeasuredValue(event.target.value)} disabled={busy} />
            </label>
            <label className="field">
              <span className="field-label">Serial ids (serialized items)</span>
              <input className="input mono" placeholder="comma separated InventorySerialId values" value={serialIds} onChange={(event) => setSerialIds(event.target.value)} disabled={busy} />
            </label>
            <label className="field field-wide">
              <span className="field-label">Technical justification *</span>
              <textarea className="input" rows={2} value={justification} onChange={(event) => setJustification(event.target.value)} disabled={busy} />
            </label>
            <label className="field field-wide">
              <span className="field-label">Intended use *</span>
              <textarea className="input" rows={2} value={intendedUse} onChange={(event) => setIntendedUse(event.target.value)} disabled={busy} />
            </label>
          </div>
          <div className="modal-actions">
            <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void create()}>Raise concession</button>
          </div>
        </div>
      )}

      {loading && <p>Loading…</p>}

      {concession && (
        <>
          <div className="card">
            <h2 className="form-section-title">Concession</h2>
            <div className="detail-grid">
              <div><span className="field-label">Quantity</span><div className="mono">{concession.Quantity}</div></div>
              <div><span className="field-label">Failed parameter</span><div>{concession.FailedParameter}</div></div>
              <div><span className="field-label">Measured value</span><div className="mono">{concession.MeasuredValue}</div></div>
              <div><span className="field-label">Serials</span><div className="mono">{concession.InventorySerialIds.length || '—'}</div></div>
              <div><span className="field-label">Decided by</span><div>{concession.DecidedRoleCode ?? '—'}</div></div>
              <div><span className="field-label">Stock posting batch</span><div className="mono">{concession.StockPostingBatchId ?? '—'}</div></div>
              <div className="field-wide"><span className="field-label">Technical justification</span><div>{concession.TechnicalJustification}</div></div>
              <div className="field-wide"><span className="field-label">Intended use</span><div>{concession.IntendedUse}</div></div>
              {concession.ProvenanceAnnotationJson && (
                <div className="field-wide"><span className="field-label">Provenance annotation</span><pre className="mono">{concession.ProvenanceAnnotationJson}</pre></div>
              )}
            </div>
          </div>

          {(canDecide || canReverse) && (
            <div className="reverse-panel">
              <h2 className="form-section-title">
                {concession.Status === 'DRAFT' ? 'Technical Director decision' : 'Reverse this approval'}
              </h2>
              <div className="form-grid">
                {concession.Status === 'DRAFT' && (
                  <label className="field">
                    <span className="field-label">AVAILABLE location for accepted stock *</span>
                    <select className="input" value={locationId} disabled={busy} onChange={(event) => setLocationId(event.target.value)}>
                      <option value="">Select…</option>
                      {locations.map((row) => <option key={row.Id} value={row.Id}>{row.WarehouseCode} / {row.BinCode}</option>)}
                    </select>
                  </label>
                )}
                <label className="field field-wide">
                  <span className="field-label">{concession.Status === 'DRAFT' ? 'Decision reason *' : 'Reversal reason *'}</span>
                  <input className="input" value={decisionReason} disabled={busy} onChange={(event) => setDecisionReason(event.target.value)} />
                </label>
              </div>
              <div className="modal-actions">
                {canDecide ? (
                  <>
                    <button type="button" className="btn btn-warn" disabled={busy} onClick={() => void reject()}>Reject</button>
                    <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void approve()}>Approve & post stock</button>
                  </>
                ) : (
                  <button type="button" className="btn btn-warn" disabled={busy} onClick={() => void reverse()}>Reverse approval</button>
                )}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}
