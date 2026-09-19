import { useMemo, useState, type FormEvent } from 'react'
import { getGoodsReceipt, listGoodsReceipts } from '../../api/goodsReceipts'
import { createVendorBillFromGrn } from '../../api/vendorBills'
import { newIdempotencyKey } from '../../api/stores'
import type { GoodsReceiptResult } from '../../types/goodsReceipt'
import { VENDOR_BILL_CHARGE_TYPES, type VendorBillChargeType, type VendorBillView } from '../../types/vendorBill'
import { ErrorAlert } from '../../components/ErrorAlert'
import { formatAmount } from '../purchase/PurchaseRequisitionListPage'

interface Props {
  onClose: () => void
  onSaved: (bill: VendorBillView) => void
}

interface LineDraft {
  include: boolean
  quantity: string
  unitRate: string
  payable: string
  grossWeightKg: string
}

interface ChargeDraft {
  type: VendorBillChargeType
  value: string
}

const round2 = (value: number) => Math.round(value * 100) / 100

/**
 * Raise a vendor bill against a finalized GRN (POST /accounts/vendor-bills/
 * from-grn/{grnId}). Accounts picks the GRN by number — since main 04036dd
 * (#11) ACCOUNTS_MANAGER may read GRNs, so the line ids no longer have to come
 * from the database. Each line defaults to the received quantity, the PO rate
 * and a tax-inclusive payable (rate × qty × (1 + GST %)); every figure is
 * editable because the bill is what the vendor printed, not what we expected —
 * the server records the match status per line.
 */
export function VendorBillFormModal({ onClose, onSaved }: Props) {
  const [grnNumber, setGrnNumber] = useState('')
  const [grn, setGrn] = useState<GoodsReceiptResult | null>(null)
  const [lookupError, setLookupError] = useState<unknown>(null)
  const [looking, setLooking] = useState(false)

  const [billNumber, setBillNumber] = useState('')
  const [billDate, setBillDate] = useState(new Date().toISOString().slice(0, 10))
  const [lines, setLines] = useState<Record<string, LineDraft>>({})
  const [charges, setCharges] = useState<ChargeDraft[]>([])
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  const lookup = async () => {
    const number = grnNumber.trim()
    if (!number) return
    setLooking(true)
    setLookupError(null)
    setError(null)
    setGrn(null)
    try {
      const page = await listGoodsReceipts({ page: 1, pageSize: 5, grnNumber: number })
      const head = page.Items.find((row) => row.GrnNumber.toUpperCase() === number.toUpperCase()) ?? page.Items[0]
      if (!head) throw new Error(`No GRN ${number} is visible to you.`)
      const full = await getGoodsReceipt(head.Id)
      setGrn(full)
      setBillNumber(full.VendorBillNumber ?? '')
      if (full.VendorBillDate) setBillDate(full.VendorBillDate.slice(0, 10))
      setLines(Object.fromEntries(full.Lines.map((line) => [line.Id, {
        include: true,
        quantity: String(line.ReceivedQuantity),
        unitRate: String(line.UnitRate),
        payable: String(round2(line.ReceivedQuantity * line.UnitRate * (1 + (line.GstPercentage ?? 0) / 100))),
        grossWeightKg: '',
      } satisfies LineDraft])))
    } catch (err) {
      setLookupError(err)
    } finally {
      setLooking(false)
    }
  }

  const patchLine = (id: string, change: Partial<LineDraft>) =>
    setLines((current) => ({ ...current, [id]: { ...current[id], ...change } }))

  const totals = useMemo(() => {
    let payable = 0
    for (const draft of Object.values(lines)) if (draft.include) payable += Number(draft.payable) || 0
    const charge = charges.reduce((sum, row) => sum + (Number(row.value) || 0), 0)
    return { payable: round2(payable), charge: round2(charge) }
  }, [lines, charges])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!grn) { setError('Look up the GRN first.'); return }
    if (!billNumber.trim()) { setError("The vendor's bill number is required."); return }
    if (grn.Status !== 'FINALIZED') { setError(`${grn.GrnNumber} is ${grn.Status}; only a FINALIZED GRN can be billed.`); return }
    const payloadLines = []
    for (const line of grn.Lines) {
      const draft = lines[line.Id]
      if (!draft?.include) continue
      const quantity = Number(draft.quantity)
      const unitRate = Number(draft.unitRate)
      const payable = Number(draft.payable)
      if (!(quantity > 0)) { setError(`${line.ItemCode}: billed quantity must be greater than zero.`); return }
      if (quantity > line.ReceivedQuantity) { setError(`${line.ItemCode}: billed ${quantity} exceeds the received ${line.ReceivedQuantity}.`); return }
      if (unitRate < 0 || payable < 0) { setError(`${line.ItemCode}: rate and payable cannot be negative.`); return }
      const gross = draft.grossWeightKg.trim() === '' ? null : Number(draft.grossWeightKg)
      if (gross !== null && !(gross > 0)) { setError(`${line.ItemCode}: verified gross weight must be positive when given.`); return }
      payloadLines.push({ GoodsReceiptLineId: line.Id, Quantity: quantity, UnitRate: unitRate, TotalPayableValue: payable, VerifiedGrossWeightKg: gross })
    }
    if (payloadLines.length === 0) { setError('Include at least one GRN line on the bill.'); return }
    for (const row of charges) {
      if (!(Number(row.value) > 0)) { setError(`${row.type}: a charge needs a positive value.`); return }
    }
    setSaving(true)
    setError(null)
    try {
      onSaved(await createVendorBillFromGrn(grn.Id, {
        BillNumber: billNumber.trim(),
        BillDate: billDate,
        Lines: payloadLines,
        Charges: charges.length === 0 ? null : charges.map((row) => ({
          ChargeType: row.type,
          ChargeValue: Number(row.value),
          IsRecoverableTax: row.type === 'RECOVERABLE_GST',
        })),
        IdempotencyKey: newIdempotencyKey('vendor-bill'),
      }))
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
          <h2>Raise vendor bill from GRN</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>

        <form
          onSubmit={submit}
          className="form-grid"
          onKeyDown={(event) => {
            const target = event.target as HTMLElement
            if (event.key === 'Enter' && !target.closest('button[type="submit"]')) {
              event.preventDefault()
              if (target.id === 'vb-grn') void lookup()
            }
          }}
        >
          <label className="field">
            <span className="field-label">GRN number *</span>
            <div style={{ display: 'flex', gap: 8 }}>
              <input id="vb-grn" className="input mono" value={grnNumber} onChange={(event) => setGrnNumber(event.target.value)} placeholder="GRN-26-27-000011" />
              <button type="button" className="btn" onClick={() => void lookup()} disabled={looking || !grnNumber.trim()}>{looking ? 'Looking…' : 'Look up'}</button>
            </div>
          </label>
          <div className="field">
            <span className="field-label">Vendor / PO</span>
            <div style={{ paddingTop: 6 }}>{grn ? <>{grn.VendorName} · <span className="mono">{grn.PurchaseOrderNumber}</span> · {grn.Status}</> : '—'}</div>
          </div>
          <div className="field-wide"><ErrorAlert error={lookupError} fallback="The GRN could not be read." /></div>

          <label className="field">
            <span className="field-label">Bill number *</span>
            <input className="input mono" value={billNumber} onChange={(event) => setBillNumber(event.target.value)} placeholder="Vendor's invoice number" disabled={!grn} />
          </label>
          <label className="field">
            <span className="field-label">Bill date *</span>
            <input className="input" type="date" value={billDate} onChange={(event) => setBillDate(event.target.value)} disabled={!grn} />
          </label>

          <div className="field-wide table-wrap">
            <table className="table">
              <thead>
                <tr><th /><th>#</th><th>Item</th><th className="text-right">Received</th><th className="text-right">PO rate</th><th className="text-right">Billed qty</th><th className="text-right">Billed rate</th><th className="text-right">Payable (incl. tax)</th><th className="text-right">Gross kg</th></tr>
              </thead>
              <tbody>
                {!grn && <tr><td colSpan={9} className="table-empty">Look up a finalized GRN to load its lines.</td></tr>}
                {grn?.Lines.map((line) => {
                  const draft = lines[line.Id]
                  if (!draft) return null
                  return (
                    <tr key={line.Id}>
                      <td><input type="checkbox" checked={draft.include} onChange={(event) => patchLine(line.Id, { include: event.target.checked })} /></td>
                      <td className="mono">{line.LineNumber}</td>
                      <td><span className="mono">{line.ItemCode}</span> — {line.ItemName} <span className="field-hint">GST {line.GstPercentage}%</span></td>
                      <td className="text-right mono">{line.ReceivedQuantity} {line.Uom}</td>
                      <td className="text-right mono">{formatAmount(line.UnitRate)}</td>
                      <td className="text-right"><input className="input text-right mono" inputMode="decimal" value={draft.quantity} disabled={!draft.include} onChange={(event) => patchLine(line.Id, { quantity: event.target.value })} /></td>
                      <td className="text-right"><input className="input text-right mono" inputMode="decimal" value={draft.unitRate} disabled={!draft.include} onChange={(event) => patchLine(line.Id, { unitRate: event.target.value })} /></td>
                      <td className="text-right"><input className="input text-right mono" inputMode="decimal" value={draft.payable} disabled={!draft.include} onChange={(event) => patchLine(line.Id, { payable: event.target.value })} /></td>
                      <td className="text-right"><input className="input text-right mono" inputMode="decimal" value={draft.grossWeightKg} placeholder="—" disabled={!draft.include} onChange={(event) => patchLine(line.Id, { grossWeightKg: event.target.value })} /></td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <div className="field-wide">
            <span className="field-label">Charges on the bill</span>
            <p className="field-hint">Freight, duty and the like are capitalised into the landed rate; RECOVERABLE_GST is excluded from inventory cost. Weight-based charges allocate by verified gross weight when it is given.</p>
            {charges.map((row, index) => (
              <div key={index} style={{ display: 'flex', gap: 8, marginBottom: 6 }}>
                <select className="input" value={row.type} onChange={(event) => setCharges((current) => current.map((c, i) => i === index ? { ...c, type: event.target.value as VendorBillChargeType } : c))}>
                  {VENDOR_BILL_CHARGE_TYPES.map((type) => <option key={type} value={type}>{type}</option>)}
                </select>
                <input className="input text-right mono" inputMode="decimal" value={row.value} placeholder="0.00" onChange={(event) => setCharges((current) => current.map((c, i) => i === index ? { ...c, value: event.target.value } : c))} />
                <button type="button" className="btn btn-ghost" onClick={() => setCharges((current) => current.filter((_, i) => i !== index))}>✕</button>
              </div>
            ))}
            <button type="button" className="btn" disabled={!grn} onClick={() => setCharges((current) => [...current, { type: 'FREIGHT', value: '' }])}>+ Charge</button>
          </div>

          <div className="field">
            <span className="field-label">Payable on lines</span>
            <div className="mono" style={{ paddingTop: 6 }}>{formatAmount(totals.payable)}</div>
          </div>
          <div className="field">
            <span className="field-label">Charges</span>
            <div className="mono" style={{ paddingTop: 6 }}>{formatAmount(totals.charge)}</div>
          </div>

          <div className="field-wide"><ErrorAlert error={error} fallback="The bill was not raised." /></div>

          <div className="modal-actions field-wide">
            <button type="button" className="btn" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving || !grn}>{saving ? 'Raising…' : 'Raise bill'}</button>
          </div>
        </form>
      </div>
    </div>
  )
}
