import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  addCustomerPoOption, createCustomerPo, downloadCustomerPoFile,
  getCustomerPoLookups, getNextCustomerPoNumber, updateCustomerPo, uploadCustomerPoFile,
} from '../../api/customerPos'
import type { CustomerPoOptionKind } from '../../api/customerPos'
import { listCustomers } from '../../api/customers'
import { AddableSelect } from '../../components/AddableSelect'
import { CustomerSearchSelect } from '../../components/CustomerSearchSelect'
import type { CustomerPoDetail, CustomerPoLookups, UpsertCustomerPoRequest } from '../../types/customerPo'

interface Props {
  mode: 'create' | 'edit'
  existing?: CustomerPoDetail
  onClose: () => void
  onSaved: (poRecordNumber: string) => void
}

interface CustomerOption {
  CustomerCode: string
  Name: string
}

interface LineDraft {
  description: string
  dueDate: string
  quantity: string
  uom: string
  rate: string
  discountPercent: string
}

const emptyLine = (): LineDraft => ({ description: '', dueDate: '', quantity: '', uom: '', rate: '', discountPercent: '' })

function lineAmount(line: LineDraft): number | null {
  const qty = parseFloat(line.quantity)
  const rate = parseFloat(line.rate)
  if (Number.isNaN(qty) || Number.isNaN(rate)) return null
  const disc = parseFloat(line.discountPercent)
  const gross = qty * rate
  return Math.round((gross - (Number.isNaN(disc) ? 0 : (gross * disc) / 100)) * 100) / 100
}

function inr(value: number | null): string {
  if (value === null) return '—'
  return value.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export function CustomerPoFormModal({ mode, existing, onClose, onSaved }: Props) {
  const navigate = useNavigate()
  const [lookups, setLookups] = useState<CustomerPoLookups | null>(null)
  const [customers, setCustomers] = useState<CustomerOption[]>([])
  const [poFile, setPoFile] = useState<File | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const [form, setForm] = useState({
    poRecordNumber: existing?.PoRecordNumber ?? '',
    customerPoNumber: existing?.CustomerPoNumber ?? '',
    customerPoDate: existing?.CustomerPoDate ?? '',
    quoteNumber: existing?.QuoteNumber ?? '',
    quoteDate: existing?.QuoteDate ?? '',
    customerCode: existing?.CustomerCode ?? '',
    customerName: existing?.CustomerName ?? '',
    serviceMode: existing?.ServiceMode ?? '',
    salesType: existing?.SalesType ?? '',
    description: existing?.Description ?? '',
    totalAmountWithGst: existing?.TotalAmountWithGst?.toString() ?? '',
    workStatus: existing?.WorkStatus ?? 'Not Completed',
    paymentTerms: existing?.PaymentTerms ?? '',
    modeOfDelivery: existing?.ModeOfDelivery ?? '',
    remarks: existing?.Remarks ?? '',
    deliveryTerms: existing?.DeliveryTerms ?? '',
    cgstPercent: existing?.CgstPercent?.toString() ?? '9',
    sgstPercent: existing?.SgstPercent?.toString() ?? '9',
    igstPercent: existing?.IgstPercent?.toString() ?? '',
    revisionReason: '',
  })

  const [lines, setLines] = useState<LineDraft[]>(
    existing?.Lines?.length
      ? existing.Lines.map((line) => ({
          description: line.Description,
          dueDate: line.DueDate ?? '',
          quantity: line.Quantity?.toString() ?? '',
          uom: line.Uom ?? '',
          rate: line.Rate?.toString() ?? '',
          discountPercent: line.DiscountPercent?.toString() ?? '',
        }))
      : [],
  )

  const set = (key: keyof typeof form) => (event: { target: { value: string } }) =>
    setForm((prev) => ({ ...prev, [key]: event.target.value }))

  const setLine = (index: number, key: keyof LineDraft) => (event: { target: { value: string } }) =>
    setLines((prev) => prev.map((line, i) => (i === index ? { ...line, [key]: event.target.value } : line)))

  useEffect(() => {
    getCustomerPoLookups().then(setLookups).catch(() => setError('Failed to load lookups.'))
    listCustomers({ page: 1, pageSize: 500 })
      .then((data) => setCustomers(data.Items.filter((c) => c.IsActive).map((c) => ({ CustomerCode: c.CustomerCode, Name: c.Name }))))
      .catch(() => undefined)
    if (mode === 'create') {
      getNextCustomerPoNumber()
        .then((next) => setForm((prev) => ({ ...prev, poRecordNumber: next.PoRecordNumber })))
        .catch(() => undefined)
    }
  }, [mode])

  const addOption = (kind: CustomerPoOptionKind, listKey: 'ServiceModes' | 'SalesTypes') => async (name: string) => {
    const created = await addCustomerPoOption(kind, name)
    setLookups((prev) => prev && ({
      ...prev,
      [listKey]: prev[listKey].includes(created.Value) ? prev[listKey] : [...prev[listKey], created.Value].sort(),
    }))
    return { value: created.Value, label: created.Value }
  }

  const totals = useMemo(() => {
    if (lines.length === 0) return null
    const taxable = Math.round(lines.reduce((sum, line) => sum + (lineAmount(line) ?? 0), 0) * 100) / 100
    const pct = (raw: string) => {
      const value = parseFloat(raw)
      return Number.isNaN(value) ? 0 : value
    }
    const cgst = Math.round(taxable * pct(form.cgstPercent)) / 100
    const sgst = Math.round(taxable * pct(form.sgstPercent)) / 100
    const igst = Math.round(taxable * pct(form.igstPercent)) / 100
    const before = taxable + cgst + sgst + igst
    const total = Math.round(before)
    return { taxable, cgst, sgst, igst, roundOff: Math.round((total - before) * 100) / 100, total }
  }, [lines, form.cgstPercent, form.sgstPercent, form.igstPercent])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError('')
    if (!form.customerPoNumber.trim()) { setError('Customer PO number is required.'); return }
    if (!form.customerCode) { setError('Select a customer from the Customer Master.'); return }
    if (mode === 'edit' && !form.revisionReason.trim()) { setError('Revision reason is required.'); return }
    for (const [index, line] of lines.entries()) {
      if (!line.description.trim()) { setError(`Line ${index + 1}: description is required.`); return }
    }
    setSaving(true)
    const body: UpsertCustomerPoRequest = {
      PoRecordNumber: form.poRecordNumber.trim() || null,
      CustomerPoNumber: form.customerPoNumber.trim(),
      CustomerPoDate: form.customerPoDate || null,
      QuoteNumber: form.quoteNumber.trim() || null,
      QuoteDate: form.quoteDate || null,
      CustomerCode: form.customerCode,
      ServiceMode: form.serviceMode || null,
      SalesType: form.salesType || null,
      Description: form.description.trim() || null,
      TotalAmountWithGst: form.totalAmountWithGst ? Number(form.totalAmountWithGst) : null,
      WorkStatus: form.workStatus || null,
      PaymentTerms: form.paymentTerms.trim() || null,
      ModeOfDelivery: form.modeOfDelivery.trim() || null,
      Remarks: form.remarks.trim() || null,
      DeliveryTerms: form.deliveryTerms.trim() || null,
      CgstPercent: lines.length && form.cgstPercent ? Number(form.cgstPercent) : null,
      SgstPercent: lines.length && form.sgstPercent ? Number(form.sgstPercent) : null,
      IgstPercent: lines.length && form.igstPercent ? Number(form.igstPercent) : null,
      Lines: lines.map((line, index) => ({
        SlNo: index + 1,
        Description: line.description.trim(),
        DueDate: line.dueDate || null,
        Quantity: line.quantity ? Number(line.quantity) : null,
        Uom: line.uom || null,
        Rate: line.rate ? Number(line.rate) : null,
        DiscountPercent: line.discountPercent ? Number(line.discountPercent) : null,
        Amount: lineAmount(line),
      })),
      Version: existing?.Version,
      RevisionReason: mode === 'edit' ? form.revisionReason.trim() : null,
    }
    try {
      let recordNumber: string
      let savedVersion: number
      if (mode === 'create') {
        const saved = await createCustomerPo(body)
        recordNumber = saved.PoRecordNumber
        savedVersion = saved.Version
      } else {
        const saved = await updateCustomerPo(existing!.PoRecordNumber, body)
        recordNumber = saved.PoRecordNumber
        savedVersion = saved.Version
      }
      if (poFile) {
        const fileReason = mode === 'create' ? 'Initial customer PO document upload' : form.revisionReason.trim()
        await uploadCustomerPoFile(recordNumber, poFile, savedVersion, fileReason)
      }
      onSaved(recordNumber)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed.')
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal" style={{ maxWidth: 1040 }} onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{mode === 'create' ? 'New Customer PO' : `Edit ${existing?.PoRecordNumber}`}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>
        <form onSubmit={submit} className="form-grid">
          <label className="field">
            <span className="field-label">PO record no *</span>
            <input className="input mono" value={form.poRecordNumber} onChange={set('poRecordNumber')} disabled={mode === 'edit'} />
            <span className="field-hint">Auto-generated; edit before saving if needed. Saved under the company you signed in with.</span>
          </label>

          <div className="field-wide form-section-title">Customer</div>
          <div className="field-wide">
            <div className="flex items-start gap-2">
              <CustomerSearchSelect
                options={customers}
                customerCode={form.customerCode}
                customerName={form.customerName}
                onSelect={(option) => setForm((prev) => ({ ...prev, customerCode: option.CustomerCode, customerName: option.Name }))}
                onText={(name) => setForm((prev) => ({ ...prev, customerCode: '', customerName: name }))}
              />
              <button
                type="button"
                className="btn btn-primary"
                style={{ whiteSpace: 'nowrap' }}
                onClick={() => navigate('/customers?create=1')}
              >
                + Add New Customer
              </button>
            </div>
          </div>

          <div className="field-wide form-section-title">PO details</div>
          <label className="field">
            <span className="field-label">Customer PO number *</span>
            <input className="input" value={form.customerPoNumber} onChange={set('customerPoNumber')} required />
          </label>
          <label className="field">
            <span className="field-label">Customer PO date</span>
            <input className="input" type="date" value={form.customerPoDate} onChange={set('customerPoDate')} />
          </label>
          <label className="field">
            <span className="field-label">Quote number</span>
            <input className="input" value={form.quoteNumber} onChange={set('quoteNumber')} />
          </label>
          <label className="field">
            <span className="field-label">Quote date</span>
            <input className="input" type="date" value={form.quoteDate} onChange={set('quoteDate')} />
          </label>
          <AddableSelect
            label="Mode of service"
            value={form.serviceMode}
            options={(lookups?.ServiceModes ?? []).map((m) => ({ value: m, label: m }))}
            onChange={(value) => setForm((prev) => ({ ...prev, serviceMode: value }))}
            onCreate={(name) => addOption('SERVICE_MODE', 'ServiceModes')(name)}
            addHint="Adds the new mode of service to the dropdown for every PO."
          />
          <AddableSelect
            label="Sales type"
            value={form.salesType}
            options={(lookups?.SalesTypes ?? []).map((t) => ({ value: t, label: t }))}
            onChange={(value) => setForm((prev) => ({ ...prev, salesType: value }))}
            onCreate={(name) => addOption('SALES_TYPE', 'SalesTypes')(name)}
            addHint="Adds the new sales type to the dropdown for every PO."
          />
          <label className="field">
            <span className="field-label">Work status</span>
            <select className="input" value={form.workStatus} onChange={set('workStatus')}>
              {lookups?.WorkStatuses.map((s) => <option key={s} value={s}>{s}</option>) ?? <option>Not Completed</option>}
            </select>
          </label>
          <label className="field field-wide">
            <span className="field-label">Description / scope</span>
            <textarea className="input" rows={2} value={form.description} onChange={set('description')} />
          </label>

          <div className="field-wide form-section-title">Goods / services lines ({lines.length})</div>
          <div className="field-wide">
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>#</th>
                    <th style={{ minWidth: 220 }}>Description</th>
                    <th>Due on</th>
                    <th>Qty</th>
                    <th>UOM</th>
                    <th>Rate</th>
                    <th>Disc %</th>
                    <th className="text-right">Amount</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {lines.length === 0 && (
                    <tr><td colSpan={9} className="table-empty">No lines — add rows, or use the total field below for a value-only entry.</td></tr>
                  )}
                  {lines.map((line, index) => (
                    <tr key={index}>
                      <td className="mono">{index + 1}</td>
                      <td><textarea className="input" rows={2} value={line.description} onChange={setLine(index, 'description')} /></td>
                      <td><input className="input" type="date" value={line.dueDate} onChange={setLine(index, 'dueDate')} /></td>
                      <td><input className="input" type="number" min="0" step="0.001" style={{ width: 90 }} value={line.quantity} onChange={setLine(index, 'quantity')} /></td>
                      <td>
                        <select className="input" value={line.uom} onChange={setLine(index, 'uom')}>
                          <option value="">—</option>
                          {lookups?.Uoms.map((uom) => <option key={uom} value={uom}>{uom}</option>)}
                        </select>
                      </td>
                      <td><input className="input" type="number" min="0" step="0.01" style={{ width: 110 }} value={line.rate} onChange={setLine(index, 'rate')} /></td>
                      <td><input className="input" type="number" min="0" max="100" step="0.01" style={{ width: 80 }} value={line.discountPercent} onChange={setLine(index, 'discountPercent')} /></td>
                      <td className="text-right mono">{inr(lineAmount(line))}</td>
                      <td>
                        <button type="button" className="btn btn-ghost" onClick={() => setLines((prev) => prev.filter((_, i) => i !== index))}>✕</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <button type="button" className="btn btn-ghost mt-2" onClick={() => setLines((prev) => [...prev, emptyLine()])}>
              + Add line
            </button>
          </div>

          {lines.length > 0 && (
            <>
              <div className="field-wide form-section-title">Tax & totals</div>
              <label className="field">
                <span className="field-label">CGST %</span>
                <input className="input" type="number" min="0" step="0.01" value={form.cgstPercent} onChange={set('cgstPercent')} />
              </label>
              <label className="field">
                <span className="field-label">SGST %</span>
                <input className="input" type="number" min="0" step="0.01" value={form.sgstPercent} onChange={set('sgstPercent')} />
              </label>
              <label className="field">
                <span className="field-label">IGST %</span>
                <input className="input" type="number" min="0" step="0.01" value={form.igstPercent} onChange={set('igstPercent')} />
                <span className="field-hint">Use IGST alone for inter-state; clear CGST/SGST.</span>
              </label>
              {totals && (
                <div className="field">
                  <span className="field-label">Computed totals</span>
                  <div className="mono" style={{ fontSize: 13, lineHeight: 1.7 }}>
                    Taxable: ₹{inr(totals.taxable)}<br />
                    {totals.cgst > 0 && <>CGST: ₹{inr(totals.cgst)}<br /></>}
                    {totals.sgst > 0 && <>SGST: ₹{inr(totals.sgst)}<br /></>}
                    {totals.igst > 0 && <>IGST: ₹{inr(totals.igst)}<br /></>}
                    Round off: ₹{inr(totals.roundOff)}<br />
                    <strong>Total: ₹{inr(totals.total)}</strong>
                  </div>
                  <span className="field-hint">Amount in words is generated on save.</span>
                </div>
              )}
            </>
          )}
          {lines.length === 0 && (
            <label className="field">
              <span className="field-label">Total amount (incl. GST)</span>
              <input className="input" type="number" min="0" step="0.01" value={form.totalAmountWithGst} onChange={set('totalAmountWithGst')} />
              <span className="field-hint">Used only when no lines are entered.</span>
            </label>
          )}

          <div className="field-wide form-section-title">Terms and customer PO document</div>
          <label className="field">
            <span className="field-label">Delivery terms</span>
            <input className="input" value={form.deliveryTerms} onChange={set('deliveryTerms')} />
          </label>
          <label className="field">
            <span className="field-label">Payment terms</span>
            <input className="input" value={form.paymentTerms} onChange={set('paymentTerms')} />
          </label>
          <label className="field">
            <span className="field-label">Dispatched through / delivery mode</span>
            <input className="input" value={form.modeOfDelivery} onChange={set('modeOfDelivery')} placeholder="e.g. Courier & Mail" />
          </label>
          <label className="field">
            <span className="field-label">PO copy (PDF, max 10 MB)</span>
            <input
              className="input"
              type="file"
              accept="application/pdf"
              onChange={(event) => setPoFile(event.target.files?.[0] ?? null)}
            />
            {!poFile && existing?.PoFileName && (
              <span className="field-hint">
                Current: {existing.PoFileName}{' '}
                <a href="#" onClick={(event) => { event.preventDefault(); void downloadCustomerPoFile(existing.PoRecordNumber, existing.PoFileName ?? '') }}>
                  Download
                </a>
              </span>
            )}
          </label>
          {mode === 'edit' && (
            <label className="field field-wide">
              <span className="field-label">Revision reason *</span>
              <input className="input" required value={form.revisionReason} onChange={set('revisionReason')} />
              <span className="field-hint">Every edit creates an immutable revision; earlier lines and snapshots remain unchanged.</span>
            </label>
          )}

          <label className="field field-wide">
            <span className="field-label">Remarks</span>
            <textarea className="input" rows={2} value={form.remarks} onChange={set('remarks')} />
          </label>

          {mode === 'edit' && existing?.AmountInWords && (
            <div className="field-wide field-hint">Amount in words: {existing.AmountInWords}</div>
          )}

          {error && <div className="alert alert-error field-wide">{error}</div>}
          <div className="modal-actions field-wide">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving…' : mode === 'create' ? 'Save Customer PO' : 'Save changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
