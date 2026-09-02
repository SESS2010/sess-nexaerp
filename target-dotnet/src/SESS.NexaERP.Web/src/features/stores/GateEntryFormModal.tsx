import { useEffect, useState } from 'react'
import {
  createGateEntry,
  getSourcePurchaseOrder,
  newIdempotencyKey,
  updateGateEntry,
} from '../../api/stores'
import type {
  GateEntryLineRequest,
  GateEntryResult,
  IsoReceiptVerification,
  SourcePurchaseOrder,
} from '../../types/stores'
import { DEFAULT_ISO_VERIFICATION, TRANSPORT_MODES } from '../../types/stores'
import { formatAmount } from '../purchase/PurchaseRequisitionListPage'
import { ErrorAlert } from '../../components/ErrorAlert'

interface DraftLine {
  purchaseOrderLineId: string
  itemCode: string
  itemName: string
  uom: string
  orderedQuantity: number
  include: boolean
  deliveredQuantity: string
}

function nowLocal(): string {
  const now = new Date()
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}`
}

function toLocalInput(iso: string): string {
  const date = new Date(iso)
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function parseIso(json: string): IsoReceiptVerification {
  try {
    return { ...DEFAULT_ISO_VERIFICATION, ...(JSON.parse(json) as Partial<IsoReceiptVerification>) }
  } catch {
    return { ...DEFAULT_ISO_VERIFICATION }
  }
}

interface Props {
  mode: 'create' | 'edit'
  existing?: GateEntryResult
  onClose: () => void
  onSaved: (result: GateEntryResult) => void
}

export function GateEntryFormModal({ mode, existing, onClose, onSaved }: Props) {
  const [poNumber, setPoNumber] = useState(existing?.PurchaseOrderNumber ?? '')
  const [po, setPo] = useState<SourcePurchaseOrder | null>(null)
  const [loadingPo, setLoadingPo] = useState(false)

  const [vendorDcNumber, setVendorDcNumber] = useState(existing?.VendorDcNumber ?? '')
  const [vehicleNumber, setVehicleNumber] = useState(existing?.VehicleNumber ?? '')
  const [modeOfTransport, setModeOfTransport] = useState(existing?.ModeOfTransport ?? TRANSPORT_MODES[0])
  const [arrivedAt, setArrivedAt] = useState(existing ? toLocalInput(existing.ArrivedAt) : nowLocal())

  const [iso, setIso] = useState<IsoReceiptVerification>(
    existing ? parseIso(existing.IsoReceiptVerificationJson) : { ...DEFAULT_ISO_VERIFICATION },
  )
  const [lines, setLines] = useState<DraftLine[]>([])
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<unknown>(null)

  // On edit the PO is fixed; pull its lines straight away so quantities are editable.
  useEffect(() => {
    if (mode === 'edit' && existing) void loadPo(existing.PurchaseOrderNumber)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function loadPo(number: string) {
    setError(null)
    setLoadingPo(true)
    try {
      const loaded = await getSourcePurchaseOrder(number.trim().toUpperCase())
      setPo(loaded)
      const delivered = new Map(
        (existing?.Lines ?? []).map((line) => [line.PurchaseOrderLineId, line.DeliveredQuantity]),
      )
      setLines(
        (loaded.Lines ?? []).map((line) => ({
          purchaseOrderLineId: line.Id,
          itemCode: line.ItemCodeSnapshot,
          itemName: line.ItemNameSnapshot,
          uom: line.UomSnapshot,
          orderedQuantity: line.OrderedQuantity,
          include: delivered.has(line.Id),
          deliveredQuantity: String(delivered.get(line.Id) ?? line.OrderedQuantity),
        })),
      )
    } catch (err) {
      setPo(null)
      setLines([])
      setError(err)
    } finally {
      setLoadingPo(false)
    }
  }

  const setLine = (index: number, patch: Partial<DraftLine>) => {
    setLines((prev) => prev.map((line, i) => (i === index ? { ...line, ...patch } : line)))
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)

    const payloadLines: GateEntryLineRequest[] = lines
      .filter((line) => line.include)
      .map((line) => ({
        PurchaseOrderLineId: line.purchaseOrderLineId,
        DeliveredQuantity: Number(line.deliveredQuantity) || 0,
      }))

    if (mode === 'create' && !poNumber.trim()) {
      setError('Purchase order number is required.')
      return
    }
    if (payloadLines.length === 0) {
      setError('Tick at least one delivered PO line.')
      return
    }
    if (payloadLines.some((line) => line.DeliveredQuantity <= 0)) {
      setError('Delivered quantity must be greater than zero on every ticked line.')
      return
    }
    if (!vendorDcNumber.trim()) {
      setError('Vendor DC number is required — it is the vendor’s delivery document reference.')
      return
    }

    setSaving(true)
    try {
      const shared = {
        VendorDcNumber: vendorDcNumber.trim(),
        VehicleNumber: vehicleNumber.trim() || null,
        ModeOfTransport: modeOfTransport,
        ArrivedAt: new Date(arrivedAt).toISOString(),
        IsoReceiptVerificationJson: JSON.stringify(iso),
        Lines: payloadLines,
      }

      const saved =
        mode === 'create'
          ? await createGateEntry(
              { ...shared, PurchaseOrderNumber: poNumber.trim().toUpperCase() },
              newIdempotencyKey('gate-create'),
            )
          : await updateGateEntry(existing!.Id, { ...shared, Version: existing!.Version })

      onSaved(saved)
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  const poUsable = po && po.Status === 'Issued' && po.IsCurrentVersion

  return (
    <div className="modal-backdrop">
      <div className="modal" style={{ maxWidth: 1000 }} onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{mode === 'create' ? 'New Gate Entry' : `Edit ${existing?.GateEntryNumber}`}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={submit} className="form-grid">
          <div className="field-wide form-section-title">Purchase order</div>

          <label className="field">
            <span className="field-label">PO number *</span>
            <input
              className="input mono"
              placeholder="PO-2627-00001"
              value={poNumber}
              disabled={mode === 'edit'}
              onChange={(event) => setPoNumber(event.target.value)}
            />
          </label>
          {mode === 'create' && (
            <div className="field">
              <span className="field-label">&nbsp;</span>
              <button type="button" className="btn btn-ghost" disabled={loadingPo} onClick={() => void loadPo(poNumber)}>
                {loadingPo ? 'Loading…' : 'Load PO lines'}
              </button>
              <span className="field-hint">There is no PO list endpoint — type the number.</span>
            </div>
          )}

          {po && (
            <div className="field-wide">
              {poUsable ? (
                <div className="alert">
                  {po.PoNumber} · revision {po.RevisionNumber} · status {po.Status} ·{' '}
                  {po.Lines?.length ?? 0} line(s). Ready to receive.
                </div>
              ) : (
                <div className="alert alert-error">
                  {po.PoNumber} is <strong>{po.Status}</strong>
                  {po.IsCurrentVersion ? '' : ' and not the current revision'}. Gate Entry needs a
                  current <strong>Issued</strong> purchase order — the API will reject this.
                </div>
              )}
            </div>
          )}

          <div className="field-wide form-section-title">Arrival</div>

          <label className="field">
            <span className="field-label">Vendor DC number *</span>
            <input className="input mono" value={vendorDcNumber} onChange={(e) => setVendorDcNumber(e.target.value)} />
          </label>
          <label className="field">
            <span className="field-label">Vehicle number</span>
            <input className="input mono" value={vehicleNumber} onChange={(e) => setVehicleNumber(e.target.value)} />
          </label>
          <label className="field">
            <span className="field-label">Mode of transport *</span>
            <select className="input" value={modeOfTransport} onChange={(e) => setModeOfTransport(e.target.value)}>
              {TRANSPORT_MODES.map((option) => <option key={option} value={option}>{option}</option>)}
            </select>
          </label>
          <label className="field">
            <span className="field-label">Arrived at *</span>
            <input type="datetime-local" className="input" value={arrivedAt} onChange={(e) => setArrivedAt(e.target.value)} />
          </label>

          <div className="field-wide form-section-title">ISO receipt verification</div>
          <p className="field-wide field-hint">
            Stored on the gate entry as the incoming-inspection record for ISO 9001 clause 8.4.
            Gate Entry proves arrival only — it does not accept the goods or move stock.
          </p>

          <label className="field">
            <span className="field-label">Documents verified</span>
            <select className="input" value={iso.DocumentsVerified ? 'yes' : 'no'} onChange={(e) => setIso({ ...iso, DocumentsVerified: e.target.value === 'yes' })}>
              <option value="no">No</option><option value="yes">Yes</option>
            </select>
          </label>
          <label className="field">
            <span className="field-label">Packaging intact</span>
            <select className="input" value={iso.PackagingIntact ? 'yes' : 'no'} onChange={(e) => setIso({ ...iso, PackagingIntact: e.target.value === 'yes' })}>
              <option value="no">No</option><option value="yes">Yes</option>
            </select>
          </label>
          <label className="field">
            <span className="field-label">Quantity matches DC</span>
            <select className="input" value={iso.QuantityMatchesDc ? 'yes' : 'no'} onChange={(e) => setIso({ ...iso, QuantityMatchesDc: e.target.value === 'yes' })}>
              <option value="no">No</option><option value="yes">Yes</option>
            </select>
          </label>
          <label className="field">
            <span className="field-label">Visible damage observed</span>
            <select className="input" value={iso.VisualDamageObserved ? 'yes' : 'no'} onChange={(e) => setIso({ ...iso, VisualDamageObserved: e.target.value === 'yes' })}>
              <option value="no">No</option><option value="yes">Yes</option>
            </select>
          </label>
          <label className="field">
            <span className="field-label">Cold chain maintained</span>
            <select
              className="input"
              value={iso.ColdChainMaintained === null ? 'na' : iso.ColdChainMaintained ? 'yes' : 'no'}
              onChange={(e) => setIso({ ...iso, ColdChainMaintained: e.target.value === 'na' ? null : e.target.value === 'yes' })}
            >
              <option value="na">Not applicable</option>
              <option value="yes">Yes</option>
              <option value="no">No</option>
            </select>
          </label>
          <label className="field field-wide">
            <span className="field-label">Receipt remarks</span>
            <textarea className="input" rows={2} value={iso.Remarks} onChange={(e) => setIso({ ...iso, Remarks: e.target.value })} />
          </label>

          <div className="field-wide form-section-title">
            Delivered lines ({lines.filter((line) => line.include).length} of {lines.length})
          </div>

          <div className="field-wide table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th style={{ width: 40 }} />
                  <th className="text-right">PO line</th>
                  <th>Item</th>
                  <th>Description</th>
                  <th>UOM</th>
                  <th className="text-right">Ordered</th>
                  <th className="text-right">Delivered now *</th>
                </tr>
              </thead>
              <tbody>
                {lines.length === 0 && (
                  <tr>
                    <td colSpan={7} className="table-empty">
                      Load a purchase order above to list its lines.
                    </td>
                  </tr>
                )}
                {lines.map((line, index) => (
                  <tr key={line.purchaseOrderLineId} className={line.include ? 'row-selected' : undefined}>
                    <td>
                      <input
                        type="checkbox"
                        checked={line.include}
                        onChange={() => setLine(index, { include: !line.include })}
                      />
                    </td>
                    <td className="text-right mono">{line.orderedQuantity > 0 ? index + 1 : index + 1}</td>
                    <td className="mono">{line.itemCode}</td>
                    <td>{line.itemName}</td>
                    <td>{line.uom}</td>
                    <td className="text-right mono">{formatAmount(line.orderedQuantity)}</td>
                    <td className="text-right">
                      <input
                        className="input text-right mono"
                        disabled={!line.include}
                        value={line.deliveredQuantity}
                        onChange={(event) => setLine(index, { deliveredQuantity: event.target.value })}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <ErrorAlert error={error} className="field-wide" fallback="Could not save the gate entry." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving…' : mode === 'create' ? 'Create draft gate entry' : 'Save changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
