import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { acceptVendorBill, getVendorBill, rejectVendorBill, reverseVendorBill } from '../../api/vendorBills'
import { getGoodsReceipt } from '../../api/goodsReceipts'
import { newIdempotencyKey } from '../../api/stores'
import type { VendorBillView } from '../../types/vendorBill'
import type { GoodsReceiptResult } from '../../types/goodsReceipt'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'
import { formatAmount } from '../purchase/PurchaseRequisitionListPage'

type Decision = 'accept' | 'reject' | 'reverse'

/**
 * One vendor bill: header, lines against the GRN, charges and the decision.
 * Accept / reject act on a DRAFT bill; reverse undoes an ACCEPTED one. Every
 * decision needs a reason and the current version, and is sent once with an
 * idempotency key. LandedUnitRate per line is what the stock is worth after
 * the governed ITC snapshot (recoverable GST out, blocked GST and capitalised
 * charges in) — the number the Actual BOM carries.
 */
export function VendorBillDetailPage() {
  const { id = '' } = useParams()
  const { can } = useSession()
  const [bill, setBill] = useState<VendorBillView | null>(null)
  const [grn, setGrn] = useState<GoodsReceiptResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState<Decision | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const loaded = await getVendorBill(id)
      setBill(loaded)
      // GRN read is Accounts-visible since main 04036dd (#11); item codes come from it.
      setGrn(await getGoodsReceipt(loaded.GoodsReceiptId).catch(() => null))
    } catch (err) {
      setBill(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  const decide = async (decision: Decision) => {
    if (!bill) return
    if (!reason.trim()) {
      setError(`A reason is required to ${decision} — it is recorded on the bill.`)
      return
    }
    setBusy(decision)
    setError(null)
    setNotice('')
    try {
      const request = { Version: bill.Version, Reason: reason.trim(), IdempotencyKey: newIdempotencyKey(`vendor-bill-${decision}`) }
      const result = decision === 'accept' ? await acceptVendorBill(bill.Id, request)
        : decision === 'reject' ? await rejectVendorBill(bill.Id, request)
          : await reverseVendorBill(bill.Id, request)
      setReason('')
      setNotice(`${result.BillNumber} is now ${result.Status}${decision === 'accept' ? ' — the receipt is valued at the landed rate.' : '.'}`)
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(null)
    }
  }

  if (loading && !bill) return <div className="page"><p>Loading…</p></div>
  if (!bill) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Vendor bill not found, or your role cannot read bills (Accounts only)." />
        <Link to="/accounts/vendor-bills">← Back to Vendor Bills</Link>
      </div>
    )
  }

  const grnLineById = new Map((grn?.Lines ?? []).map((line) => [line.Id, line]))
  const canAccept = bill.Status === 'DRAFT' && can(PAGE_KEYS.vendorBills, 'approve')
  const canReject = bill.Status === 'DRAFT' && can(PAGE_KEYS.vendorBills, 'reject')
  const canReverse = bill.Status === 'ACCEPTED' && can(PAGE_KEYS.vendorBills, 'cancel')

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="mono">{bill.BillNumber}</h1>
          <p className="page-sub">
            Bill dated {bill.BillDate} against {grn ? <Link to={`/stores/goods-receipts/${grn.Id}`} className="mono">{grn.GrnNumber}</Link> : <span className="mono">{bill.GoodsReceiptId.slice(0, 8)}…</span>}
            {grn && <> · {grn.VendorName} · PO <span className="mono">{grn.PurchaseOrderNumber}</span></>}
          </p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <StatusBadge value={bill.Status} />
          <StatusBadge value={bill.MatchStatus} />
        </div>
      </div>

      {notice && <div className="alert alert-success">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="The decision was not recorded." />

      <div className="card">
        <div className="detail-grid">
          <div className="detail-field"><span className="field-label">Payable (lines)</span><span className="mono">{formatAmount(bill.TotalPayableValue)}</span></div>
          <div className="detail-field"><span className="field-label">Charges</span><span className="mono">{formatAmount(bill.TotalChargeValue)}</span></div>
          <div className="detail-field"><span className="field-label">Landed value</span><span className="mono">{formatAmount(bill.TotalLandedValue)}</span></div>
          <div className="detail-field"><span className="field-label">Raised as</span>{bill.ActorRoleCode} · {bill.ResolvedRoleAssignmentType}</div>
          <div className="detail-field"><span className="field-label">Decided</span>{bill.DecidedAt ? <>{new Date(bill.DecidedAt).toLocaleString('en-IN')} — {bill.DecisionReason}</> : '—'}</div>
          <div className="detail-field"><span className="field-label">Reversed</span>{bill.ReversedAt ? <>{new Date(bill.ReversedAt).toLocaleString('en-IN')} — {bill.ReversalReason}</> : '—'}</div>
          <div className="detail-field"><span className="field-label">Version</span><span className="mono">{bill.Version}</span></div>
        </div>
      </div>

      <h2>Lines</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th>#</th><th>Item</th><th className="text-right">Billed qty</th><th className="text-right">Expected rate</th><th className="text-right">Billed rate</th><th className="text-right">Expected payable</th><th className="text-right">Billed payable</th><th>Match</th><th className="text-right">Allocated charges</th><th className="text-right">Landed rate</th></tr>
          </thead>
          <tbody>
            {bill.Lines.map((line) => {
              const grnLine = grnLineById.get(line.GoodsReceiptLineId)
              return (
                <tr key={line.Id}>
                  <td className="mono">{line.LineNumber}</td>
                  <td>{grnLine ? <><span className="mono">{grnLine.ItemCode}</span> — {grnLine.ItemName}</> : <span className="mono">{line.ItemId.slice(0, 8)}…</span>}</td>
                  <td className="text-right mono">{line.BilledQuantity}{grnLine ? ` ${grnLine.Uom}` : ''}</td>
                  <td className="text-right mono">{formatAmount(line.ExpectedUnitRate)}</td>
                  <td className="text-right mono">{formatAmount(line.BilledUnitRate)}</td>
                  <td className="text-right mono">{formatAmount(line.ExpectedPayableValue)}</td>
                  <td className="text-right mono">{formatAmount(line.BilledPayableValue)}</td>
                  <td><StatusBadge value={line.MatchStatus} /></td>
                  <td className="text-right mono">{formatAmount(line.AllocatedChargeValue)}</td>
                  <td className="text-right mono"><strong>{formatAmount(line.LandedUnitRate)}</strong></td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      <h2>Charges</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th>#</th><th>Type</th><th className="text-right">Value</th><th>Recoverable tax</th><th>In inventory cost</th><th>Allocation</th></tr>
          </thead>
          <tbody>
            {bill.Charges.length === 0 && <tr><td colSpan={6} className="table-empty">No charges on this bill.</td></tr>}
            {bill.Charges.map((charge) => (
              <tr key={charge.Id}>
                <td className="mono">{charge.ChargeNumber}</td>
                <td className="mono">{charge.ChargeType}</td>
                <td className="text-right mono">{formatAmount(charge.ChargeValue)}</td>
                <td>{charge.IsRecoverableTax ? 'Yes' : 'No'}</td>
                <td>{charge.IncludedInInventoryCost ? 'Yes' : 'No'}</td>
                <td className="mono">{charge.AllocationBasis}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {(canAccept || canReject || canReverse) && (
        <div className="card">
          <h2>Decision</h2>
          <label className="field">
            <span className="field-label">Reason *</span>
            <input className="input" value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Recorded on the bill" />
          </label>
          <div style={{ display: 'flex', gap: 8, marginTop: 8 }}>
            {canAccept && <button className="btn btn-primary" disabled={busy !== null} onClick={() => void decide('accept')}>{busy === 'accept' ? 'Accepting…' : 'Accept bill'}</button>}
            {canReject && <button className="btn" disabled={busy !== null} onClick={() => void decide('reject')}>{busy === 'reject' ? 'Rejecting…' : 'Reject bill'}</button>}
            {canReverse && <button className="btn" disabled={busy !== null} onClick={() => void decide('reverse')}>{busy === 'reverse' ? 'Reversing…' : 'Reverse acceptance'}</button>}
          </div>
        </div>
      )}

      <p><Link to="/accounts/vendor-bills">← Back to Vendor Bills</Link></p>
    </div>
  )
}
