import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  actOnPurchaseOrder,
  amendPurchaseOrder,
  approvePurchaseOrder,
  cancelPurchaseOrder,
  getPurchaseOrder,
  newIdempotencyKey,
  rememberDoc,
} from '../../api/purchase'
import type { PurchaseOrderDetail } from '../../types/purchase'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { StatusBadge } from '../employees/StatusBadge'
import { formatAmount } from './PurchaseRequisitionListPage'
import { ErrorAlert } from '../../components/ErrorAlert'

type Pane = 'workflow' | 'amend' | 'cancel'

export function PurchaseOrderDetailPage() {
  const { poNumber = '' } = useParams()
  const navigate = useNavigate()
  const { can, hasRole, hasFullAuthorityRole } = useSession()

  // Every command below is on purchase.po, where full-control is not a
  // wildcard, so each action is checked on its own. The roles are the ones
  // EfRev869BPurchaseService demands on top of the page grant.
  const canSubmit = can(PAGE_KEYS.purchaseOrders, 'submit') && hasRole('PURCHASE_MANAGER')
  const canApprove = can(PAGE_KEYS.purchaseOrders, 'approve')
  const canReject = can(PAGE_KEYS.purchaseOrders, 'reject')
  const canIssue = can(PAGE_KEYS.purchaseOrders, 'issue') && hasRole('PURCHASE_MANAGER')
  const canAmend = can(PAGE_KEYS.purchaseOrders, 'update') && hasRole('PURCHASE_MANAGER')
  // Cancel is SUPPORT-denied, so the director role has to be held with full
  // authority; a SUPPORT-held directorship is refused by RequireRole.
  const canCancel =
    can(PAGE_KEYS.purchaseOrders, 'cancel') &&
    (hasFullAuthorityRole('TECHNICAL_DIRECTOR') || hasFullAuthorityRole('MANAGING_DIRECTOR'))
  const canWorkflow = canSubmit || canApprove || canReject || canIssue

  const [po, setPo] = useState<PurchaseOrderDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [pane, setPane] = useState<Pane>('workflow')
  const [busy, setBusy] = useState('')

  const [remarks, setRemarks] = useState('')
  const [amendmentReason, setAmendmentReason] = useState('')
  const [paymentTerms, setPaymentTerms] = useState('')
  const [deliveryTerms, setDeliveryTerms] = useState('')
  const [warrantyTerms, setWarrantyTerms] = useState('')
  const [cancelReason, setCancelReason] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const detail = await getPurchaseOrder(poNumber)
      setPo(detail)
      rememberDoc('purchase-order', detail.PoNumber)
      setPaymentTerms(detail.PaymentTermsSnapshot ?? '')
      setDeliveryTerms(detail.DeliveryTermsSnapshot ?? '')
      setWarrantyTerms(detail.WarrantyTermsSnapshot ?? '')
    } catch (err) {
      setPo(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [poNumber])

  useEffect(() => {
    void load()
  }, [load])

  const guardRemarks = (label: string): boolean => {
    if (!remarks.trim()) {
      setError(`Remarks are required to ${label.toLowerCase()}.`)
      return false
    }
    return true
  }

  const runSimple = async (action: 'submit' | 'issue', label: string) => {
    if (!po || !guardRemarks(label)) return
    setError(null); setNotice(''); setBusy(action)
    try {
      const result = await actOnPurchaseOrder(po.PoNumber, action, {
        Remarks: remarks.trim(),
        Version: po.Version,
        IdempotencyKey: newIdempotencyKey(`po-${action}`),
      })
      setRemarks('')
      setNotice(`${label} succeeded. Status is now ${result.Status}.`)
      void load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy('')
    }
  }

  const runApproval = async (action: 'approve' | 'reject', label: string) => {
    if (!po || !guardRemarks(label)) return
    setError(null); setNotice(''); setBusy(action)
    try {
      const result = await approvePurchaseOrder(po.PoNumber, action, {
        Remarks: remarks.trim(),
        Version: po.Version,
        ExpectedCurrentVersion: po.Version,
        IdempotencyKey: newIdempotencyKey(`po-${action}`),
      })
      setRemarks('')
      setNotice(`${label} succeeded. Status is now ${result.Status}.`)
      void load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy('')
    }
  }

  const runAmend = async () => {
    if (!po) return
    setError(null); setNotice('')
    if (!amendmentReason.trim()) {
      setError('An amendment must state its reason — the PO is a contract document.')
      return
    }
    setBusy('amend')
    try {
      const result = await amendPurchaseOrder(po.PoNumber, {
        AmendmentReason: amendmentReason.trim(),
        PaymentTerms: paymentTerms.trim(),
        DeliveryTerms: deliveryTerms.trim(),
        WarrantyTerms: warrantyTerms.trim(),
        Version: po.Version,
        IdempotencyKey: newIdempotencyKey('po-amend'),
      })
      setAmendmentReason('')
      setNotice(`Amendment recorded. ${result.Number} is now ${result.Status} at version ${result.Version}.`)
      void load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy('')
    }
  }

  const runCancel = async () => {
    if (!po) return
    setError(null); setNotice('')
    if (!cancelReason.trim()) {
      setError('Cancellation needs a written reason.')
      return
    }
    setBusy('cancel')
    try {
      const result = await cancelPurchaseOrder(po.PoNumber, {
        Reason: cancelReason.trim(),
        Version: po.Version,
        IdempotencyKey: newIdempotencyKey('po-cancel'),
      })
      setCancelReason('')
      setNotice(`Purchase order cancelled. Status is now ${result.Status}.`)
      void load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy('')
    }
  }

  if (loading && !po) return <div className="page"><p>Loading…</p></div>

  if (!po) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Purchase order not found." />
        <button type="button" className="btn btn-ghost" onClick={() => navigate('/purchase/purchase-orders')}>
          ‹ Back to purchase orders
        </button>
      </div>
    )
  }

  const lines = po.Lines ?? []
  const masked = po.TotalPayableValue === undefined

  // A pane is only offered when the session may act inside it; the selected
  // pane falls back to the first one that is still allowed.
  const paneAllowed: Record<Pane, boolean> = { workflow: canWorkflow, amend: canAmend, cancel: canCancel }
  const availablePanes = (['workflow', 'amend', 'cancel'] as Pane[]).filter((name) => paneAllowed[name])
  const activePane: Pane | null = paneAllowed[pane] ? pane : availablePanes[0] ?? null

  return (
    <div className="page">
      <div className="breadcrumbs">
        <Link to="/purchase/purchase-orders">Purchase Order</Link> /{' '}
        <span className="mono">{po.PoNumber}</span>
      </div>

      <div className="page-header">
        <div>
          <h1>{po.PoNumber}</h1>
          <p className="page-sub">
            Revision {po.RevisionNumber}
            {po.IsCurrentVersion ? ' (current)' : ' (superseded)'} · {po.CurrencyCode}
            {po.ApprovalRoute ? ` · route ${po.ApprovalRoute}` : ''}
            {po.RequiredApprovalStepCount !== undefined
              ? ` · ${po.CompletedApprovalStepCount ?? 0}/${po.RequiredApprovalStepCount} approvals`
              : ''}
          </p>
        </div>
        <div className="action-row"><StatusBadge value={po.Status} /></div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />
      {notice && <div className="alert">{notice}</div>}
      {masked && (
        <div className="alert">
          Commercial values are hidden — your role lacks{' '}
          <span className="mono">ViewCommercialValues</span> on <span className="mono">purchase.po</span>.
        </div>
      )}

      <div className="card">
        <div className="detail-grid">
          <div className="detail-field"><span className="field-label">Issued at</span>{po.IssuedAt ? new Date(po.IssuedAt).toLocaleString('en-IN') : '—'}</div>
          <div className="detail-field"><span className="field-label">Cancelled at</span>{po.CancelledAt ? new Date(po.CancelledAt).toLocaleString('en-IN') : '—'}</div>
          <div className="detail-field"><span className="field-label">Record version</span><span className="mono">{po.Version}</span></div>
          <div className="detail-field"><span className="field-label">Payment terms</span>{po.PaymentTermsSnapshot || '—'}</div>
          <div className="detail-field"><span className="field-label">Delivery terms</span>{po.DeliveryTermsSnapshot || '—'}</div>
          <div className="detail-field"><span className="field-label">Warranty terms</span>{po.WarrantyTermsSnapshot || '—'}</div>
          {po.AmendmentReason && (
            <div className="detail-field field-wide"><span className="field-label">Amendment reason</span>{po.AmendmentReason}</div>
          )}
          {po.CancellationReason && (
            <div className="detail-field field-wide"><span className="field-label">Cancellation reason</span>{po.CancellationReason}</div>
          )}
        </div>
      </div>

      {!masked && (
        <div className="card">
          <div className="form-section-title">Commercials</div>
          <div className="detail-grid">
            <div className="detail-field"><span className="field-label">Taxable value</span>₹{formatAmount(po.TaxableValue)}</div>
            <div className="detail-field"><span className="field-label">Discount</span>₹{formatAmount(po.DiscountValue)}</div>
            <div className="detail-field"><span className="field-label">Tax</span>₹{formatAmount(po.TaxValue)}</div>
            <div className="detail-field"><span className="field-label">Packing &amp; forwarding</span>₹{formatAmount(po.PackingForwarding)}</div>
            <div className="detail-field"><span className="field-label">Freight</span>₹{formatAmount(po.Freight)}</div>
            <div className="detail-field"><span className="field-label">Insurance</span>₹{formatAmount(po.Insurance)}</div>
            <div className="detail-field"><span className="field-label">Other charges</span>₹{formatAmount(po.OtherCharges)}</div>
            <div className="detail-field"><span className="field-label">Round off</span>₹{formatAmount(po.RoundOff)}</div>
            <div className="detail-field"><span className="field-label">Total payable</span><strong>₹{formatAmount(po.TotalPayableValue)}</strong></div>
          </div>
        </div>
      )}

      <h2>Lines ({lines.length})</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>#</th>
              <th>Item</th>
              <th>Description</th>
              <th>UOM</th>
              <th className="text-right">Ordered</th>
              <th className="text-right">Outstanding</th>
              <th className="text-right">Unit rate</th>
              <th className="text-right">Line total</th>
            </tr>
          </thead>
          <tbody>
            {lines.length === 0 && <tr><td colSpan={8} className="table-empty">No lines on this PO.</td></tr>}
            {lines.map((line) => (
              <tr key={line.Id}>
                <td className="mono">{line.LineNumber}</td>
                <td className="mono">{line.ItemCodeSnapshot}</td>
                <td>{line.ItemNameSnapshot}</td>
                <td>{line.UomSnapshot}</td>
                <td className="text-right mono">{formatAmount(line.OrderedQuantity)}</td>
                <td className="text-right mono">
                  {line.ApprovedOutstandingQuantitySnapshot !== undefined
                    ? formatAmount(line.ApprovedOutstandingQuantitySnapshot) : '—'}
                </td>
                <td className="text-right mono">{line.UnitRate !== undefined ? formatAmount(line.UnitRate) : 'Masked'}</td>
                <td className="text-right mono">{line.TotalPayableValue !== undefined ? formatAmount(line.TotalPayableValue) : 'Masked'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {availablePanes.length > 0 && (
        <div className="tabs" style={{ marginTop: 24 }}>
          {canWorkflow && (
            <button type="button" className={`tab${activePane === 'workflow' ? ' active' : ''}`} onClick={() => setPane('workflow')}>Workflow</button>
          )}
          {canAmend && (
            <button type="button" className={`tab${activePane === 'amend' ? ' active' : ''}`} onClick={() => setPane('amend')}>Amend</button>
          )}
          {canCancel && (
            <button type="button" className={`tab${activePane === 'cancel' ? ' active' : ''}`} onClick={() => setPane('cancel')}>Cancel</button>
          )}
        </div>
      )}

      {activePane === 'workflow' && (
        <div className="card">
          <label className="field field-wide">
            <span className="field-label">Remarks *</span>
            <textarea className="input" rows={2} value={remarks} onChange={(e) => setRemarks(e.target.value)} />
          </label>
          <div className="action-row">
            {canSubmit && (
              <button type="button" className="btn btn-ghost" disabled={!!busy} onClick={() => void runSimple('submit', 'Submit')}>
                {busy === 'submit' ? 'Working…' : 'Submit for approval'}
              </button>
            )}
            {canApprove && (
              <button type="button" className="btn btn-primary" disabled={!!busy} onClick={() => void runApproval('approve', 'Approve')}>
                {busy === 'approve' ? 'Working…' : 'Approve'}
              </button>
            )}
            {canReject && (
              <button type="button" className="btn btn-warn" disabled={!!busy} onClick={() => void runApproval('reject', 'Reject')}>
                {busy === 'reject' ? 'Working…' : 'Reject'}
              </button>
            )}
            {canIssue && (
              <button type="button" className="btn btn-primary" disabled={!!busy} onClick={() => void runSimple('issue', 'Issue')}>
                {busy === 'issue' ? 'Working…' : 'Issue to vendor'}
              </button>
            )}
          </div>
          <p className="field-hint">
            Sent with record version {po.Version}. Issue is the point the PO becomes a commitment to
            the vendor and the Gate Entry chain can begin.
          </p>
        </div>
      )}

      {activePane === 'amend' && (
        <div className="card">
          <div className="form-grid">
            <label className="field field-wide">
              <span className="field-label">Amendment reason *</span>
              <textarea className="input" rows={2} value={amendmentReason} onChange={(e) => setAmendmentReason(e.target.value)} />
            </label>
            <label className="field"><span className="field-label">Payment terms</span>
              <input className="input" value={paymentTerms} onChange={(e) => setPaymentTerms(e.target.value)} /></label>
            <label className="field"><span className="field-label">Delivery terms</span>
              <input className="input" value={deliveryTerms} onChange={(e) => setDeliveryTerms(e.target.value)} /></label>
            <label className="field"><span className="field-label">Warranty terms</span>
              <input className="input" value={warrantyTerms} onChange={(e) => setWarrantyTerms(e.target.value)} /></label>
            <div className="field-wide action-row">
              <button type="button" className="btn btn-primary" disabled={!!busy} onClick={() => void runAmend()}>
                {busy === 'amend' ? 'Working…' : 'Record amendment'}
              </button>
              <span className="field-hint">An amendment creates a new PO revision; the old one is retained.</span>
            </div>
          </div>
        </div>
      )}

      {activePane === 'cancel' && (
        <div className="card">
          <label className="field field-wide">
            <span className="field-label">Cancellation reason *</span>
            <textarea className="input" rows={2} value={cancelReason} onChange={(e) => setCancelReason(e.target.value)} />
          </label>
          <div className="action-row">
            <button type="button" className="btn btn-warn" disabled={!!busy} onClick={() => void runCancel()}>
              {busy === 'cancel' ? 'Working…' : 'Cancel purchase order'}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
