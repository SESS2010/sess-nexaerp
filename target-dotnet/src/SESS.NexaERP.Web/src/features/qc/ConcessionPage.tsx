import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import {
  approveConcession,
  getConcession,
  listAvailableConditionLocations,
  rejectConcession,
  reverseConcession,
} from '../../api/qc'
import { newIdempotencyKey } from '../../api/stores'
import type { InventoryConcessionResult, WarehouseConditionLocation } from '../../types/qc'
import { StatusBadge } from '../employees/StatusBadge'
import { CopyId } from '../../components/CopyId'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'

/**
 * Inventory concessions: the QC manager raises one against a rejected lot
 * disposition (a FAIL parameter result), the Technical Director approves it
 * into an AVAILABLE location, rejects it, or later reverses an approval.
 *
 * Route /qc/concessions opens a concession by number;
 * /qc/concessions/:number shows the record and the TD decisions. A new
 * concession is raised only from a finalized inspection, on
 * /qc/concessions/new?inspection=… (ConcessionCreatePage).
 */
export function ConcessionPage() {
  const { number = '' } = useParams()
  const location = useLocation()
  const navigate = useNavigate()
  const { can, me } = useSession()

  const [lookup, setLookup] = useState('')
  const [concession, setConcession] = useState<InventoryConcessionResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState((location.state as { notice?: string } | null)?.notice ?? '')
  const [busy, setBusy] = useState(false)

  // Decision inputs (TD).
  const [locations, setLocations] = useState<WarehouseConditionLocation[]>([])
  const [locationId, setLocationId] = useState('')
  const [decisionReason, setDecisionReason] = useState('')

  // Nothing is created here: POST /api/v1/qc/concessions needs the QC lot
  // disposition id and the FAIL parameter result id, which only the inspection
  // read supplies. The link lives on the inspection page (field review 1 Oct).
  const canCreate = can(PAGE_KEYS.qc, 'create')
  const keyRef = useRef<{ fingerprint: string; key: string } | null>(null)

  // Approve/reject are qc.inspection-policies:approve and reverse is …:cancel;
  // the session's Permissions already exclude both for anyone the service
  // would refuse. The creator may never decide their own concession.

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
  // session is not loaded yet nothing is offered, like can().
  const notCreator = !me || !concession || concession.CreatedByEmployeeId !== me.EmployeeId
  const canDecide = concession?.Status === 'DRAFT' && can(PAGE_KEYS.qc, 'approve') && notCreator
  const canReverse = concession?.Status === 'APPROVED' && can(PAGE_KEYS.qc, 'cancel')

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
          {canCreate && <Link className="btn btn-ghost" to="/qc/inspections">Raise from an inspection ›</Link>}
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
              <div><span className="field-label">Stock posting</span><div>{concession.StockPostingBatchId ? <>Posted <CopyId label="Posting batch id" value={concession.StockPostingBatchId} /></> : 'Not posted'}</div></div>
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
