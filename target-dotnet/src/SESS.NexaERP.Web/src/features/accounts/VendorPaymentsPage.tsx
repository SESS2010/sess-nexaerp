import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import {
  listAdvancePurchaseOrders, listVendorAdvances, listVendorPayables, listVendorPayments, listVendorPositions,
  recordVendorAdvance, recordVendorPayment, reverseVendorAdvance, uploadBankAdvice,
} from '../../api/vendorPayments'
import { newIdempotencyKey } from '../../api/stores'
import type {
  VendorAdvancePurchaseOrderOption, VendorAdvanceView, VendorPayableView, VendorPaymentView, VendorPositionView,
} from '../../types/vendorPayment'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'
import { formatAmount } from '../purchase/PurchaseRequisitionListPage'

type Tab = 'payables' | 'advances' | 'payments' | 'positions'

const today = () => new Date().toISOString().slice(0, 10)
const round2 = (value: number) => Math.round(value * 100) / 100

/**
 * Vendor advances and payments (manager's item 5). Payables are the accepted
 * vendor bills less advances adjusted and payments already made; a payment
 * is recorded against a vendor and allocated across those bills; an advance
 * is recorded against an issued PO and is adjusted automatically when that
 * PO's bill is accepted. Every advance, and every non-INR payment, cites an
 * uploaded bank advice (PDF / JPEG / PNG) as evidence. Reads, writes and the
 * upload all sit on accounts.vendor-financial-evidence.
 */
export function VendorPaymentsPage() {
  const { can } = useSession()
  const canRecord = can(PAGE_KEYS.vendorPayments, 'approve')
  const canReverse = can(PAGE_KEYS.vendorPayments, 'cancel')

  const [tab, setTab] = useState<Tab>('payables')
  const [payables, setPayables] = useState<VendorPayableView[]>([])
  const [advances, setAdvances] = useState<VendorAdvanceView[]>([])
  const [payments, setPayments] = useState<VendorPaymentView[]>([])
  const [positions, setPositions] = useState<VendorPositionView[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [overdueOnly, setOverdueOnly] = useState(false)
  const [showAdvance, setShowAdvance] = useState(false)
  const [showPayment, setShowPayment] = useState(false)
  const [reverseReason, setReverseReason] = useState('')
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [p, a, pay, pos] = await Promise.all([
        listVendorPayables({ overdueOnly: overdueOnly || undefined }),
        listVendorAdvances({ page: 1, pageSize: 100 }),
        listVendorPayments({ page: 1, pageSize: 100 }),
        listVendorPositions(),
      ])
      setPayables(p)
      setAdvances(a.Items ?? [])
      setPayments(pay.Items ?? [])
      setPositions(pos)
    } catch (err) {
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [overdueOnly])

  useEffect(() => {
    void load()
  }, [load])

  const reverse = async (row: VendorAdvanceView) => {
    if (!reverseReason.trim()) { setError('A reason is required to reverse an advance.'); return }
    setBusyId(row.Id)
    setError(null)
    try {
      const result = await reverseVendorAdvance(row.Id, { Reason: reverseReason.trim(), IdempotencyKey: newIdempotencyKey('advance-reverse') })
      setReverseReason('')
      setNotice(`${result.AdvanceNumber} reversed.`)
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusyId(null)
    }
  }

  const totals = useMemo(() => ({
    outstanding: round2(payables.reduce((sum, row) => sum + row.OutstandingValue, 0)),
    overdue: payables.filter((row) => row.IsOverdue).length,
  }), [payables])

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Vendor Payments</h1>
          <p className="page-sub">
            What is owed on accepted bills, advances paid against POs, and the payments that settle them. Each entry cites its bank advice.
          </p>
        </div>
        {canRecord && (
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn" onClick={() => setShowAdvance(true)}>+ Record advance</button>
            <button className="btn btn-primary" onClick={() => setShowPayment(true)}>+ Record payment</button>
          </div>
        )}
      </div>

      <div className="toolbar">
        {(['payables', 'advances', 'payments', 'positions'] as Tab[]).map((key) => (
          <button key={key} className={`btn ${tab === key ? 'btn-primary' : ''}`} onClick={() => setTab(key)}>
            {key === 'payables' ? `Payables (${payables.length})` : key === 'advances' ? `Advances (${advances.length})` : key === 'payments' ? `Payments (${payments.length})` : 'Vendor positions'}
          </button>
        ))}
        <div className="spacer" />
        {tab === 'payables' && <label className="pager-label"><input type="checkbox" checked={overdueOnly} onChange={(event) => setOverdueOnly(event.target.checked)} /> Overdue only</label>}
        {tab === 'payables' && <span className="pager-label">Outstanding {formatAmount(totals.outstanding)} · {totals.overdue} overdue</span>}
      </div>

      {notice && <div className="alert alert-success">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="Vendor payment data could not be loaded (Accounts only)." />

      {tab === 'payables' && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr><th>Bill</th><th>Vendor</th><th>PO</th><th>Bill date</th><th>Due</th><th>Terms</th><th className="text-right">Accepted</th><th className="text-right">Advance adj.</th><th className="text-right">Paid</th><th className="text-right">Outstanding</th></tr>
            </thead>
            <tbody>
              {loading && <tr><td colSpan={10} className="table-empty">Loading…</td></tr>}
              {!loading && payables.length === 0 && <tr><td colSpan={10} className="table-empty">Nothing outstanding.</td></tr>}
              {payables.map((row) => (
                <tr key={row.VendorBillId}>
                  <td><Link to={`/accounts/vendor-bills/${row.VendorBillId}`} className="mono">{row.BillNumber}</Link></td>
                  <td>{row.VendorName}</td>
                  <td className="mono">{row.PurchaseOrderNumber}</td>
                  <td>{row.BillDate}</td>
                  <td>{row.DueDate ?? '—'} {row.IsOverdue && <StatusBadge value="Overdue" />}</td>
                  <td>{row.PaymentTerms}</td>
                  <td className="text-right mono">{formatAmount(row.AcceptedValue)}</td>
                  <td className="text-right mono">{formatAmount(row.AdvanceAdjustedValue)}</td>
                  <td className="text-right mono">{formatAmount(row.PaidValue)}</td>
                  <td className="text-right mono"><strong>{formatAmount(row.OutstandingValue)}</strong> {row.CurrencyCode !== 'INR' && row.CurrencyCode}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {tab === 'advances' && (
        <>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr><th>Advance</th><th>Vendor</th><th>PO</th><th>Paid</th><th>Reference</th><th className="text-right">Amount</th><th className="text-right">Adjusted</th><th className="text-right">Outstanding</th><th>State</th>{canReverse && <th />}</tr>
              </thead>
              <tbody>
                {!loading && advances.length === 0 && <tr><td colSpan={10} className="table-empty">No advances recorded.</td></tr>}
                {advances.map((row) => (
                  <tr key={row.Id}>
                    <td className="mono">{row.AdvanceNumber}</td>
                    <td>{row.VendorName}</td>
                    <td className="mono">{row.PurchaseOrderNumber}{row.PurchaseOrderCancelled && <> <StatusBadge value="Cancelled" /></>}</td>
                    <td>{row.PaidDate}</td>
                    <td className="mono">{row.PaymentReference}</td>
                    <td className="text-right mono">{formatAmount(row.Amount)}</td>
                    <td className="text-right mono">{formatAmount(row.AdjustedAmount)}</td>
                    <td className="text-right mono">{formatAmount(row.OutstandingAmount)}</td>
                    <td><StatusBadge value={row.IsReversed ? 'Reversed' : 'Active'} /></td>
                    {canReverse && (
                      <td>
                        {!row.IsReversed && row.AdjustedAmount === 0 && (
                          <button className="btn" disabled={busyId !== null} onClick={() => void reverse(row)}>{busyId === row.Id ? 'Reversing…' : 'Reverse'}</button>
                        )}
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {canReverse && advances.some((row) => !row.IsReversed && row.AdjustedAmount === 0) && (
            <label className="field" style={{ maxWidth: 480 }}>
              <span className="field-label">Reason for reversal</span>
              <input className="input" value={reverseReason} onChange={(event) => setReverseReason(event.target.value)} placeholder="Required before Reverse" />
            </label>
          )}
        </>
      )}

      {tab === 'payments' && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr><th>Payment</th><th>Vendor</th><th>Paid</th><th>Reference</th><th className="text-right">Amount</th><th>Allocated to</th></tr>
            </thead>
            <tbody>
              {!loading && payments.length === 0 && <tr><td colSpan={6} className="table-empty">No payments recorded.</td></tr>}
              {payments.map((row) => (
                <tr key={row.Id}>
                  <td className="mono">{row.PaymentNumber}</td>
                  <td>{row.VendorName}</td>
                  <td>{row.PaidDate}</td>
                  <td className="mono">{row.PaymentReference}</td>
                  <td className="text-right mono">{formatAmount(row.Amount)} {row.CurrencyCode !== 'INR' && row.CurrencyCode}</td>
                  <td>{row.Allocations.map((a) => <span key={a.VendorBillId} className="mono" style={{ marginRight: 8 }}>{a.BillNumber} {formatAmount(a.Amount)}</span>)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {tab === 'positions' && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr><th>Vendor</th><th className="text-right">Outstanding advance</th><th className="text-right">Outstanding bills</th><th className="text-right">Net payable</th><th>Cancelled POs holding advance</th></tr>
            </thead>
            <tbody>
              {!loading && positions.length === 0 && <tr><td colSpan={5} className="table-empty">No vendor positions.</td></tr>}
              {positions.map((row) => (
                <tr key={row.VendorId}>
                  <td><span className="mono">{row.VendorCode}</span> — {row.VendorName}</td>
                  <td className="text-right mono">{formatAmount(row.OutstandingAdvance)}</td>
                  <td className="text-right mono">{formatAmount(row.OutstandingBills)}</td>
                  <td className="text-right mono"><strong>{formatAmount(row.NetPayable)}</strong> {row.CurrencyCode !== 'INR' && row.CurrencyCode}</td>
                  <td>{row.CancelledPurchaseOrdersWithOutstandingAdvance > 0 ? <StatusBadge value={`${row.CancelledPurchaseOrdersWithOutstandingAdvance} to recover`} /> : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showAdvance && <AdvanceModal onClose={() => setShowAdvance(false)} onSaved={(row) => { setShowAdvance(false); setNotice(`${row.AdvanceNumber} recorded against ${row.PurchaseOrderNumber}: ${formatAmount(row.Amount)}.`); setTab('advances'); void load() }} />}
      {showPayment && <PaymentModal payables={payables} onClose={() => setShowPayment(false)} onSaved={(row) => { setShowPayment(false); setNotice(`${row.PaymentNumber} recorded: ${formatAmount(row.Amount)} to ${row.VendorName}, ${row.Allocations.length} bill(s).`); setTab('payments'); void load() }} />}
    </div>
  )
}

// ---------------------------------------------------------------- bank advice

function BankAdviceField({ vendorId, required, value, onUploaded }: { vendorId: string; required: boolean; value: string; onUploaded: (key: string) => void }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const upload = async (file: File | undefined) => {
    if (!file) return
    if (!vendorId) { setError('Pick the vendor / PO first — the advice is filed against the vendor.'); return }
    setBusy(true)
    setError(null)
    try {
      const advice = await uploadBankAdvice(vendorId, file, newIdempotencyKey('bank-advice'))
      onUploaded(advice.EvidenceObjectKey)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }
  return (
    <div className="field-wide">
      <span className="field-label">Bank advice {required ? '*' : '(optional for INR)'}</span>
      <input className="input" type="file" accept="application/pdf,image/jpeg,image/png" disabled={busy || !vendorId} onChange={(event) => void upload(event.target.files?.[0])} />
      <span className="field-hint">{busy ? 'Uploading…' : value ? <>Evidence key <span className="mono">{value}</span></> : 'PDF, JPEG or PNG up to 5 MB. The upload returns the evidence key the entry cites.'}</span>
      <ErrorAlert error={error} fallback="The bank advice was not accepted." />
    </div>
  )
}

// ---------------------------------------------------------------- advance

function AdvanceModal({ onClose, onSaved }: { onClose: () => void; onSaved: (row: VendorAdvanceView) => void }) {
  const [options, setOptions] = useState<VendorAdvancePurchaseOrderOption[]>([])
  const [poId, setPoId] = useState('')
  const [amount, setAmount] = useState('')
  const [paidDate, setPaidDate] = useState(today())
  const [reference, setReference] = useState('')
  const [evidenceKey, setEvidenceKey] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    listAdvancePurchaseOrders().then(setOptions).catch(setError)
  }, [])

  const po = options.find((row) => row.PurchaseOrderId === poId)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!po) { setError('Pick the purchase order the advance is paid against.'); return }
    const value = Number(amount)
    if (!(value > 0)) { setError('Amount must be greater than zero.'); return }
    if (value > po.AvailableAdvanceAmount) { setError(`${formatAmount(value)} exceeds the ${formatAmount(po.AvailableAdvanceAmount)} still available against ${po.PurchaseOrderNumber}.`); return }
    if (!reference.trim()) { setError('The payment reference (UTR / cheque) is required.'); return }
    if (!evidenceKey) { setError('Upload the bank advice — an advance must cite its evidence.'); return }
    setSaving(true)
    setError(null)
    try {
      onSaved(await recordVendorAdvance({ PurchaseOrderId: po.PurchaseOrderId, PaidDate: paidDate, Amount: value, CurrencyCode: po.CurrencyCode, PaymentReference: reference.trim(), EvidenceObjectKey: evidenceKey, IdempotencyKey: newIdempotencyKey('advance') }))
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>Record vendor advance</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>
        <form onSubmit={submit} className="form-grid">
          <label className="field field-wide">
            <span className="field-label">Purchase order *</span>
            <select className="input" value={poId} onChange={(event) => setPoId(event.target.value)}>
              <option value="">— issued POs that can take an advance —</option>
              {options.map((row) => <option key={row.PurchaseOrderId} value={row.PurchaseOrderId}>{row.PurchaseOrderNumber} · {row.VendorName} · PO {formatAmount(row.PurchaseOrderValue)} · available {formatAmount(row.AvailableAdvanceAmount)}</option>)}
            </select>
          </label>
          <label className="field">
            <span className="field-label">Amount * {po && <span className="field-hint">({po.CurrencyCode})</span>}</span>
            <input className="input text-right mono" inputMode="decimal" value={amount} onChange={(event) => setAmount(event.target.value)} />
          </label>
          <label className="field">
            <span className="field-label">Paid on *</span>
            <input className="input" type="date" value={paidDate} onChange={(event) => setPaidDate(event.target.value)} />
          </label>
          <label className="field field-wide">
            <span className="field-label">Payment reference *</span>
            <input className="input mono" value={reference} onChange={(event) => setReference(event.target.value)} placeholder="UTR / cheque number" />
          </label>
          <BankAdviceField vendorId={po?.VendorId ?? ''} required value={evidenceKey} onUploaded={setEvidenceKey} />
          <div className="field-wide"><ErrorAlert error={error} fallback="The advance was not recorded." /></div>
          <div className="field-wide modal-actions">
            <button type="button" className="btn" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Recording…' : 'Record advance'}</button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------- payment

function PaymentModal({ payables, onClose, onSaved }: { payables: VendorPayableView[]; onClose: () => void; onSaved: (row: VendorPaymentView) => void }) {
  const vendors = useMemo(() => {
    const seen = new Map<string, { id: string; name: string; currency: string }>()
    for (const row of payables) if (!seen.has(row.VendorId)) seen.set(row.VendorId, { id: row.VendorId, name: `${row.VendorCode} — ${row.VendorName}`, currency: row.CurrencyCode })
    return [...seen.values()]
  }, [payables])
  const [vendorId, setVendorId] = useState(vendors.length === 1 ? vendors[0].id : '')
  const [paidDate, setPaidDate] = useState(today())
  const [reference, setReference] = useState('')
  const [evidenceKey, setEvidenceKey] = useState('')
  const [allocations, setAllocations] = useState<Record<string, string>>({})
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  const bills = payables.filter((row) => row.VendorId === vendorId && row.OutstandingValue > 0)
  const currency = vendors.find((v) => v.id === vendorId)?.currency ?? 'INR'
  const total = round2(Object.values(allocations).reduce((sum, value) => sum + (Number(value) || 0), 0))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!vendorId) { setError('Pick the vendor being paid.'); return }
    const lines = []
    for (const bill of bills) {
      const value = Number(allocations[bill.VendorBillId] || 0)
      if (value === 0) continue
      if (!(value > 0)) { setError(`${bill.BillNumber}: allocation must be positive.`); return }
      if (value > bill.OutstandingValue + 0.005) { setError(`${bill.BillNumber}: ${formatAmount(value)} exceeds the outstanding ${formatAmount(bill.OutstandingValue)}.`); return }
      lines.push({ VendorBillId: bill.VendorBillId, Amount: value })
    }
    if (lines.length === 0) { setError('Allocate the payment to at least one bill.'); return }
    if (!reference.trim()) { setError('The payment reference (UTR / cheque) is required.'); return }
    if (currency !== 'INR' && !evidenceKey) { setError('A bank advice is required for a foreign-currency payment.'); return }
    setSaving(true)
    setError(null)
    try {
      onSaved(await recordVendorPayment({ VendorId: vendorId, PaidDate: paidDate, Amount: total, CurrencyCode: currency, PaymentReference: reference.trim(), EvidenceObjectKey: evidenceKey, Allocations: lines, IdempotencyKey: newIdempotencyKey('payment') }))
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal modal-wide" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>Record vendor payment</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>
        <form onSubmit={submit} className="form-grid">
          <label className="field">
            <span className="field-label">Vendor *</span>
            <select className="input" value={vendorId} onChange={(event) => { setVendorId(event.target.value); setAllocations({}) }}>
              <option value="">— vendors with outstanding bills —</option>
              {vendors.map((v) => <option key={v.id} value={v.id}>{v.name}</option>)}
            </select>
          </label>
          <label className="field">
            <span className="field-label">Paid on *</span>
            <input className="input" type="date" value={paidDate} onChange={(event) => setPaidDate(event.target.value)} />
          </label>
          <label className="field field-wide">
            <span className="field-label">Payment reference *</span>
            <input className="input mono" value={reference} onChange={(event) => setReference(event.target.value)} placeholder="UTR / cheque number" />
          </label>

          <div className="field-wide table-wrap">
            <table className="table">
              <thead>
                <tr><th>Bill</th><th>PO</th><th>Due</th><th className="text-right">Outstanding</th><th className="text-right">Pay now</th><th /></tr>
              </thead>
              <tbody>
                {!vendorId && <tr><td colSpan={6} className="table-empty">Pick a vendor to see its outstanding bills.</td></tr>}
                {vendorId && bills.length === 0 && <tr><td colSpan={6} className="table-empty">Nothing outstanding for this vendor.</td></tr>}
                {bills.map((bill) => (
                  <tr key={bill.VendorBillId}>
                    <td className="mono">{bill.BillNumber}</td>
                    <td className="mono">{bill.PurchaseOrderNumber}</td>
                    <td>{bill.DueDate ?? '—'} {bill.IsOverdue && <StatusBadge value="Overdue" />}</td>
                    <td className="text-right mono">{formatAmount(bill.OutstandingValue)}</td>
                    <td className="text-right"><input className="input text-right mono" inputMode="decimal" value={allocations[bill.VendorBillId] ?? ''} placeholder="0.00" onChange={(event) => setAllocations((current) => ({ ...current, [bill.VendorBillId]: event.target.value }))} /></td>
                    <td><button type="button" className="btn btn-ghost" onClick={() => setAllocations((current) => ({ ...current, [bill.VendorBillId]: String(bill.OutstandingValue) }))}>Full</button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="field">
            <span className="field-label">Payment amount</span>
            <div className="mono" style={{ paddingTop: 6 }}>{formatAmount(total)} {currency}</div>
            <span className="field-hint">The sum of the allocations — the server records exactly this.</span>
          </div>
          <BankAdviceField vendorId={vendorId} required={currency !== 'INR'} value={evidenceKey} onUploaded={setEvidenceKey} />
          <div className="field-wide"><ErrorAlert error={error} fallback="The payment was not recorded." /></div>
          <div className="field-wide modal-actions">
            <button type="button" className="btn" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving || total <= 0}>{saving ? 'Recording…' : 'Record payment'}</button>
          </div>
        </form>
      </div>
    </div>
  )
}
