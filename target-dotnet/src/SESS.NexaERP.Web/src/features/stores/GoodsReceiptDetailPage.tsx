import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { finalizeGoodsReceipt, getGoodsReceipt, reverseGoodsReceipt } from '../../api/goodsReceipts'
import { newIdempotencyKey } from '../../api/stores'
import type { GoodsReceiptResult } from '../../types/goodsReceipt'
import { StatusBadge } from '../employees/StatusBadge'
import { GoodsReceiptFormModal } from './GoodsReceiptFormModal'
import { ErrorAlert } from '../../components/ErrorAlert'

export function GoodsReceiptDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const [grn, setGrn] = useState<GoodsReceiptResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [reverseReason, setReverseReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setGrn(await getGoodsReceipt(id))
    } catch (err) {
      setGrn(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  const finalize = async () => {
    if (!grn) return
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      const result = await finalizeGoodsReceipt(grn.Id, {
        Version: grn.Version,
        IdempotencyKey: newIdempotencyKey('grn-finalize'),
      })
      setGrn(result)
      setNotice(`${result.GrnNumber} finalized. Stock moved to QC hold${result.StockPostingBatchId ? ` (posting batch ${result.StockPostingBatchId})` : ''}.`)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const reverse = async () => {
    if (!grn) return
    if (!reverseReason.trim()) {
      setError('A reversal must state its reason — it is the audit record for undoing a stock posting.')
      return
    }
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      const result = await reverseGoodsReceipt(grn.Id, {
        Version: grn.Version,
        Reason: reverseReason.trim(),
        IdempotencyKey: newIdempotencyKey('grn-reverse'),
      })
      setNotice(`Reversal ${result.GrnNumber} created.`)
      setReverseReason('')
      navigate(`/stores/goods-receipts/${result.Id}`)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  if (loading && !grn) {
    return <div className="page"><p>Loading…</p></div>
  }

  if (!grn) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="GRN not found." />
      </div>
    )
  }

  const isDraft = grn.Status === 'DRAFT'
  const hasWarnings = grn.Warnings.length > 0

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="mono">{grn.GrnNumber}</h1>
          <p className="page-sub">
            Gate Entry <span className="mono">{grn.GateEntryNumber}</span> · PO <span className="mono">{grn.PurchaseOrderNumber}</span> · {grn.VendorName}
          </p>
        </div>
        <div className="action-row">
          <StatusBadge value={grn.Status} />
          {grn.DocumentKind !== 'NORMAL' && <StatusBadge value={grn.DocumentKind} />}
          {isDraft && (
            <>
              <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setEditing(true)}>Edit</button>
              <button
                type="button"
                className="btn btn-primary"
                disabled={busy || hasWarnings}
                title={hasWarnings ? 'Resolve the duplicate serial warnings first.' : 'Finalize — the GRN becomes immutable and stock posts to QC hold.'}
                onClick={finalize}
              >
                Finalize
              </button>
            </>
          )}
        </div>
      </div>

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />

      {hasWarnings && (
        <div className="alert alert-warn" role="alert">
          <div className="alert-title">Duplicate serials must be made unique before finalize</div>
          {grn.Warnings.map((warning) => <p key={warning} className="alert-body mono">{warning}</p>)}
          {isDraft && <p className="alert-body">Open Edit, change the stored serial (e.g. append the financial year or make), acknowledge the duplicate and record the reason.</p>}
        </div>
      )}

      {grn.ReversesGoodsReceiptId && (
        <div className="alert alert-warn">
          This is a reversal document{grn.ReversalReason ? ` — ${grn.ReversalReason}` : ''}. It undoes{' '}
          <a className="row-click" onClick={() => navigate(`/stores/goods-receipts/${grn.ReversesGoodsReceiptId}`)}>the original GRN</a>.
        </div>
      )}

      <div className="detail-grid">
        <div><span className="field-label">Vendor bill</span> <span className="mono">{grn.VendorBillNumber}</span> dated {grn.VendorBillDate}</div>
        <div><span className="field-label">Vendor DC</span> <span className="mono">{grn.VendorDcNumber}</span></div>
        <div><span className="field-label">Transport</span> {grn.ModeOfTransport}</div>
        <div><span className="field-label">Received at</span> {new Date(grn.ReceivedAt).toLocaleString('en-IN')}</div>
        <div><span className="field-label">Version</span> <span className="mono">{grn.Version}</span></div>
        {grn.StockPostingBatchId && (
          <div><span className="field-label">Posting batch</span> <span className="mono">{grn.StockPostingBatchId}</span></div>
        )}
      </div>

      <h2 className="form-section-title">Lines</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>#</th>
              <th>Item</th>
              <th>Category</th>
              <th>HSN</th>
              <th className="text-right">Qty</th>
              <th className="text-right">Unit rate</th>
              <th>Serials</th>
              <th>Warranty until</th>
              <th>Lots</th>
            </tr>
          </thead>
          <tbody>
            {grn.Lines.map((line) => (
              <tr key={line.Id}>
                <td className="mono">{line.LineNumber}</td>
                <td>
                  <span className="mono">{line.ItemCode}</span> — {line.ItemName}
                  {line.Model && <span className="field-hint">Model {line.Model}{line.ManufacturerPartNumber ? ` · MPN ${line.ManufacturerPartNumber}` : ''}</span>}
                </td>
                <td className="mono">{line.ItemCategoryCode}</td>
                <td className="mono">{line.HsnSacCode}</td>
                <td className="text-right mono">{line.ReceivedQuantity} {line.Uom}</td>
                <td className="text-right mono">{line.UnitRate.toLocaleString('en-IN')}</td>
                <td>
                  {line.SerialCaptureMode === 'REQUIRED'
                    ? <StatusBadge value={`${line.Serials.length} required`} />
                    : line.Serials.length > 0 ? `${line.Serials.length} captured` : 'not serialized'}
                </td>
                <td>{line.WarrantyExpiryDate}</td>
                <td>
                  {line.Lots.map((lot) => (
                    <div key={lot.Id} className="mono lot-chip">
                      Lot {lot.LotOrdinal}: {lot.Quantity}
                      {lot.SupplierLotNumber ? ` · ${lot.SupplierLotNumber}` : ''}
                      {lot.ExpiryDate ? ` · exp ${lot.ExpiryDate}` : ''}
                    </div>
                  ))}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {grn.Lines.some((line) => line.Serials.length > 0) && (
        <>
          <h2 className="form-section-title">Serials</h2>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Line</th>
                  <th>#</th>
                  <th>Entered</th>
                  <th>Stored</th>
                  <th>Lot</th>
                  <th>Disambiguation</th>
                </tr>
              </thead>
              <tbody>
                {grn.Lines.flatMap((line) =>
                  line.Serials.map((serial) => (
                    <tr key={serial.Id}>
                      <td className="mono">{line.LineNumber}</td>
                      <td className="mono">{serial.SerialOrdinal}</td>
                      <td className="mono">{serial.EnteredSerialNumber}</td>
                      <td className="mono">{serial.StoredSerialNumber}</td>
                      <td className="mono">{serial.LotOrdinal}</td>
                      <td>{serial.DisambiguationReason ?? '—'}</td>
                    </tr>
                  )),
                )}
              </tbody>
            </table>
          </div>
        </>
      )}

      {grn.Status === 'FINALIZED' && grn.DocumentKind === 'NORMAL' && (
        <div className="reverse-panel">
          <h2 className="form-section-title">Reverse this GRN</h2>
          <p className="field-hint">
            A finalized GRN is immutable. Reversal creates a counter-document and undoes the stock posting — then receive again with a corrected GRN.
          </p>
          <div className="toolbar">
            <input
              className="input search"
              placeholder="Reason for reversal (mandatory)"
              value={reverseReason}
              onChange={(event) => setReverseReason(event.target.value)}
            />
            <button type="button" className="btn btn-warn" disabled={busy} onClick={reverse}>Reverse GRN</button>
          </div>
        </div>
      )}

      <h2 className="form-section-title">History</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th>When</th><th>Action</th><th>From</th><th>To</th><th>Role</th></tr>
          </thead>
          <tbody>
            {grn.History.length === 0 && <tr><td colSpan={5} className="table-empty">No history records.</td></tr>}
            {grn.History.map((event, index) => (
              <tr key={index}>
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

      {editing && grn && (
        <GoodsReceiptFormModal
          mode="edit"
          existing={grn}
          onClose={() => setEditing(false)}
          onSaved={(result) => {
            setEditing(false)
            setGrn(result)
          }}
        />
      )}
    </div>
  )
}
