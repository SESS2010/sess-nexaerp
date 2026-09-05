import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { finalizeGateEntry, getGateEntry, newIdempotencyKey } from '../../api/stores'
import type { GateEntryResult, IsoReceiptVerification } from '../../types/stores'
import { StatusBadge } from '../employees/StatusBadge'
import { formatAmount } from '../purchase/PurchaseRequisitionListPage'
import { GateEntryFormModal } from './GateEntryFormModal'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

function yesNo(value: boolean | null | undefined): string {
  if (value === null || value === undefined) return 'Not applicable'
  return value ? 'Yes' : 'No'
}

function parseIso(json: string): IsoReceiptVerification | null {
  try {
    return JSON.parse(json) as IsoReceiptVerification
  } catch {
    return null
  }
}

export function GateEntryDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { can, hasRole } = useSession()

  const [gate, setGate] = useState<GateEntryResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [editing, setEditing] = useState(false)
  const [finalizing, setFinalizing] = useState(false)
  const [confirmFinalize, setConfirmFinalize] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setGate(await getGateEntry(id))
    } catch (err) {
      setGate(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  const finalize = async () => {
    if (!gate) return
    setError(null)
    setNotice('')
    setFinalizing(true)
    try {
      const result = await finalizeGateEntry(gate.Id, {
        Version: gate.Version,
        IdempotencyKey: newIdempotencyKey('gate-finalize'),
      })
      setGate(result)
      setConfirmFinalize(false)
      setNotice(`Gate entry finalized. It is now immutable at version ${result.Version}.`)
    } catch (err) {
      setError(err)
    } finally {
      setFinalizing(false)
    }
  }

  if (loading && !gate) return <div className="page"><p>Loading…</p></div>

  if (!gate) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Gate entry not found." />
        <button type="button" className="btn btn-ghost" onClick={() => navigate('/stores/gate-entries')}>
          ‹ Back to gate entries
        </button>
      </div>
    )
  }

  const iso = parseIso(gate.IsoReceiptVerificationJson)
  const isDraft = gate.Status === 'DRAFT'
  // Every Gate Entry mutation is inventory.grn (an explicit-grant page) plus
  // EfGateEntryService.ActorRole(), which accepts only STORES_EXECUTIVE / STORES_ASSISTANT.
  const storesOperator = hasRole('STORES_EXECUTIVE') || hasRole('STORES_ASSISTANT')
  // Editing re-reads the source PO (purchase.po:view) to list its lines; without that
  // grant the modal can load nothing and the draft can never be saved.
  const canEdit =
    can(PAGE_KEYS.gateEntry, 'update') && storesOperator && can(PAGE_KEYS.purchaseOrders, 'view')
  const canFinalize = can(PAGE_KEYS.gateEntry, 'submit') && storesOperator

  return (
    <div className="page">
      <div className="breadcrumbs">
        <Link to="/stores/gate-entries">Gate Entry</Link> /{' '}
        <span className="mono">{gate.GateEntryNumber}</span>
      </div>

      <div className="page-header">
        <div>
          <h1>{gate.GateEntryNumber}</h1>
          <p className="page-sub">
            {gate.VendorName} · PO <span className="mono">{gate.PurchaseOrderNumber}</span> ·
            arrived {new Date(gate.ArrivedAt).toLocaleString('en-IN')}
          </p>
        </div>
        <div className="action-row">
          <StatusBadge value={gate.Status} />
          {isDraft && canEdit && (
            <button type="button" className="btn btn-ghost" onClick={() => setEditing(true)}>Edit draft</button>
          )}
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />
      {notice && <div className="alert">{notice}</div>}
      {!isDraft && (
        <div className="alert">
          This gate entry is finalized and immutable. It records that material arrived — it does
          not accept the goods, move stock or create a GRN.
        </div>
      )}

      <div className="card">
        <div className="detail-grid">
          <div className="detail-field"><span className="field-label">Vendor DC number</span><span className="mono">{gate.VendorDcNumber}</span></div>
          <div className="detail-field"><span className="field-label">Vehicle number</span><span className="mono">{gate.VehicleNumber ?? '—'}</span></div>
          <div className="detail-field"><span className="field-label">Mode of transport</span>{gate.ModeOfTransport}</div>
          <div className="detail-field"><span className="field-label">Record version</span><span className="mono">{gate.Version}</span></div>
        </div>
      </div>

      <h2>ISO receipt verification</h2>
      <div className="card">
        {iso ? (
          <div className="detail-grid">
            <div className="detail-field"><span className="field-label">Documents verified</span>{yesNo(iso.DocumentsVerified)}</div>
            <div className="detail-field"><span className="field-label">Packaging intact</span>{yesNo(iso.PackagingIntact)}</div>
            <div className="detail-field"><span className="field-label">Quantity matches DC</span>{yesNo(iso.QuantityMatchesDc)}</div>
            <div className="detail-field"><span className="field-label">Visible damage observed</span>{yesNo(iso.VisualDamageObserved)}</div>
            <div className="detail-field"><span className="field-label">Cold chain maintained</span>{yesNo(iso.ColdChainMaintained)}</div>
            <div className="detail-field field-wide"><span className="field-label">Receipt remarks</span>{iso.Remarks || '—'}</div>
          </div>
        ) : (
          <pre className="mono" style={{ whiteSpace: 'pre-wrap' }}>{gate.IsoReceiptVerificationJson}</pre>
        )}
      </div>

      <h2>Delivered lines ({gate.Lines?.length ?? 0})</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>#</th>
              <th>Item</th>
              <th>UOM</th>
              <th className="text-right">Delivered</th>
              <th>PO line id</th>
            </tr>
          </thead>
          <tbody>
            {(gate.Lines ?? []).length === 0 && (
              <tr><td colSpan={5} className="table-empty">No delivered lines.</td></tr>
            )}
            {(gate.Lines ?? []).map((line) => (
              <tr key={line.Id}>
                <td className="mono">{line.LineNumber}</td>
                <td className="mono">{line.ItemCode}</td>
                <td>{line.Uom}</td>
                <td className="text-right mono">{formatAmount(line.DeliveredQuantity)}</td>
                <td className="mono">{line.PurchaseOrderLineId}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {isDraft && canFinalize && (
        <div className="card">
          <div className="form-section-title">Finalize</div>
          <p className="field-hint">
            Finalizing locks the arrival record permanently — quantities, DC number, vehicle and the
            ISO checks can never be edited again. Do it once the physical check at the gate is done.
          </p>
          {!confirmFinalize ? (
            <div className="action-row">
              <button type="button" className="btn btn-primary" onClick={() => setConfirmFinalize(true)}>
                Finalize gate entry
              </button>
            </div>
          ) : (
            <div className="action-row">
              <span className="field-hint" style={{ alignSelf: 'center' }}>
                This cannot be undone. Finalize {gate.GateEntryNumber}?
              </span>
              <button type="button" className="btn btn-ghost" disabled={finalizing} onClick={() => setConfirmFinalize(false)}>
                Cancel
              </button>
              <button type="button" className="btn btn-primary" disabled={finalizing} onClick={() => void finalize()}>
                {finalizing ? 'Finalizing…' : 'Yes, finalize'}
              </button>
            </div>
          )}
        </div>
      )}

      <h2>Status history</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>When</th>
              <th>Action</th>
              <th>From</th>
              <th>To</th>
              <th>Actor role</th>
            </tr>
          </thead>
          <tbody>
            {(gate.History ?? []).length === 0 && (
              <tr><td colSpan={5} className="table-empty">No status events.</td></tr>
            )}
            {(gate.History ?? []).map((event, index) => (
              <tr key={`${event.OccurredAt}-${index}`}>
                <td>{new Date(event.OccurredAt).toLocaleString('en-IN')}</td>
                <td>{event.Action}</td>
                <td>{event.FromStatus ?? '—'}</td>
                <td><StatusBadge value={event.ToStatus} /></td>
                <td className="mono">{event.ActorRoleCode}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {editing && (
        <GateEntryFormModal
          mode="edit"
          existing={gate}
          onClose={() => setEditing(false)}
          onSaved={(updated) => {
            setEditing(false)
            setGate(updated)
          }}
        />
      )}
    </div>
  )
}
