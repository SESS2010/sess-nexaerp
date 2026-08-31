import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import {
  createVendor, getNextVendorCode, parseAttachmentMetadata, parseBankMetadata, updateVendor, uploadVendorAttachment,
} from '../../api/vendors'
import type { VendorAttachmentMetadata } from '../../api/vendors'
import type { UpsertVendorRequest, VendorDetail } from '../../types/vendor'

interface Props {
  mode: 'create' | 'edit'
  existing?: VendorDetail
  onClose: () => void
  onSaved: (vendorCode: string) => void
}

const VENDOR_TYPES = ['Material', 'Service', 'Material+Service', 'Transport', 'Contractor']

export function VendorFormModal({ mode, existing, onClose, onSaved }: Props) {
  const [form, setForm] = useState({
    vendorCode: existing?.VendorCode ?? '',
    legalVendorName: existing?.LegalVendorName ?? '',
    tradeName: existing?.TradeName ?? '',
    vendorType: existing?.VendorType ?? 'Material',
    gstNumber: existing?.GstNumber ?? '',
    panNumber: existing?.PanNumber ?? '',
    msmeStatus: existing?.MsmeStatus ?? false,
    msmeNumber: existing?.MsmeNumber ?? '',
    contactPerson: existing?.ContactPerson ?? '',
    phone: existing?.Phone ?? '',
    email: existing?.Email ?? '',
    billingAddress: existing?.BillingAddress ?? '',
    shippingAddress: existing?.ShippingAddress ?? '',
    state: existing?.State ?? '',
    stateCode: existing?.StateCode ?? '',
    country: existing?.Country ?? 'India',
    materialServiceCategories: existing?.MaterialServiceCategories ?? '',
    approvedMakes: existing?.ApprovedMakes ?? '',
    paymentTerms: existing?.PaymentTerms ?? '',
    deliveryTerms: existing?.DeliveryTerms ?? '',
    creditPeriodDays: existing?.CreditPeriodDays?.toString() ?? '',
    bankName: parseBankMetadata(existing?.BankMetadata).bankName ?? '',
    accountHolder: parseBankMetadata(existing?.BankMetadata).accountHolder ?? '',
    accountNumber: parseBankMetadata(existing?.BankMetadata).accountNumber ?? '',
    ifsc: parseBankMetadata(existing?.BankMetadata).ifsc ?? '',
    branch: parseBankMetadata(existing?.BankMetadata).branch ?? '',
  })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [existingAttachments] = useState<VendorAttachmentMetadata>(
    () => parseAttachmentMetadata(existing?.AttachmentMetadataJson ?? null),
  )
  const [gstFile, setGstFile] = useState<File | null>(null)
  const [bankLeafFile, setBankLeafFile] = useState<File | null>(null)
  const [panFile, setPanFile] = useState<File | null>(null)

  // New vendors get the next VEN-### code from the server; the field is
  // read-only so the series stays continuous.
  useEffect(() => {
    if (mode === 'create') {
      getNextVendorCode()
        .then((next) => setForm((prev) => ({ ...prev, vendorCode: next.VendorCode })))
        .catch(() => setError('Could not fetch the next vendor code. Reload and try again.'))
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const set = (key: keyof typeof form) => (event: { target: { value: string } }) =>
    setForm((prev) => ({ ...prev, [key]: event.target.value }))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setSaving(true)
    setError('')
    const optional = (value: string) => (value.trim() ? value.trim() : null)
    try {
      // Upload any newly selected files first, then reference them from the
      // vendor's attachment metadata. GST certificate is mandatory on create.
      const attachments: VendorAttachmentMetadata = { ...existingAttachments }
      if (gstFile) {
        const uploaded = await uploadVendorAttachment('GST_CERTIFICATE', gstFile)
        attachments.gstCertificate = { id: uploaded.Id, fileName: uploaded.FileName }
      }
      if (bankLeafFile) {
        const uploaded = await uploadVendorAttachment('BANK_LEAF', bankLeafFile)
        attachments.bankLeaf = { id: uploaded.Id, fileName: uploaded.FileName }
      }
      if (panFile) {
        const uploaded = await uploadVendorAttachment('PAN_CARD', panFile)
        attachments.panCard = { id: uploaded.Id, fileName: uploaded.FileName }
      }
      if (!attachments.gstCertificate) {
        setError('GST certificate attachment is required.')
        setSaving(false)
        return
      }

      const body: UpsertVendorRequest = {
        VendorCode: form.vendorCode,
        LegalVendorName: form.legalVendorName,
        TradeName: optional(form.tradeName),
        VendorType: form.vendorType,
        GstNumber: optional(form.gstNumber),
        PanNumber: optional(form.panNumber),
        MsmeStatus: form.msmeStatus,
        MsmeNumber: optional(form.msmeNumber),
        ContactPerson: optional(form.contactPerson),
        Phone: optional(form.phone),
        Email: optional(form.email),
        BillingAddress: optional(form.billingAddress),
        ShippingAddress: optional(form.shippingAddress),
        State: optional(form.state),
        StateCode: optional(form.stateCode),
        Country: form.country.trim() || 'India',
        MaterialServiceCategories: optional(form.materialServiceCategories),
        ApprovedMakes: optional(form.approvedMakes),
        PaymentTerms: optional(form.paymentTerms),
        DeliveryTerms: optional(form.deliveryTerms),
        CreditPeriodDays: form.creditPeriodDays.trim() ? Number(form.creditPeriodDays) : null,
        BankMetadataJson: (() => {
          const bank = {
            bankName: optional(form.bankName),
            accountHolder: optional(form.accountHolder),
            accountNumber: optional(form.accountNumber),
            ifsc: optional(form.ifsc)?.toUpperCase() ?? null,
            branch: optional(form.branch),
          }
          return Object.values(bank).some((v) => v !== null) ? JSON.stringify(bank) : null
        })(),
        AttachmentMetadataJson: JSON.stringify(attachments),
        Version: mode === 'edit' ? existing!.Version : null,
      }
      if (mode === 'create') {
        const summary = await createVendor(body)
        onSaved(summary.VendorCode)
      } else {
        const detail = await updateVendor(existing!.VendorCode, body)
        onSaved(detail.VendorCode)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{mode === 'create' ? 'New Vendor' : `Edit ${existing?.VendorCode}`}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>
        <form onSubmit={submit} className="form-grid">
          <label className="field">
            <span className="field-label">Vendor code (auto-generated)</span>
            <input
              className="input"
              required
              value={form.vendorCode}
              readOnly
              disabled
              placeholder="VEN-…"
            />
          </label>
          <label className="field">
            <span className="field-label">Legal vendor name *</span>
            <input className="input" required value={form.legalVendorName} onChange={set('legalVendorName')} />
          </label>
          <label className="field">
            <span className="field-label">Trade name</span>
            <input className="input" value={form.tradeName} onChange={set('tradeName')} />
          </label>
          <label className="field">
            <span className="field-label">Vendor type *</span>
            <select className="input" value={form.vendorType} onChange={set('vendorType')}>
              {VENDOR_TYPES.map((type) => <option key={type}>{type}</option>)}
            </select>
          </label>
          <label className="field">
            <span className="field-label">GSTIN</span>
            <input className="input" value={form.gstNumber} onChange={set('gstNumber')} placeholder="15-character GSTIN" />
          </label>
          <label className="field">
            <span className="field-label">PAN</span>
            <input className="input" value={form.panNumber} onChange={set('panNumber')} placeholder="10-character PAN" />
          </label>
          <label className="field">
            <span className="field-label">MSME</span>
            <select
              className="input"
              value={form.msmeStatus ? 'yes' : 'no'}
              onChange={(event) => setForm((prev) => ({ ...prev, msmeStatus: event.target.value === 'yes' }))}
            >
              <option value="no">No</option>
              <option value="yes">Yes</option>
            </select>
          </label>
          <label className="field">
            <span className="field-label">MSME number</span>
            <input className="input" value={form.msmeNumber} onChange={set('msmeNumber')} disabled={!form.msmeStatus} />
          </label>
          <label className="field">
            <span className="field-label">Contact person</span>
            <input className="input" value={form.contactPerson} onChange={set('contactPerson')} />
          </label>
          <label className="field">
            <span className="field-label">Phone</span>
            <input className="input" value={form.phone} onChange={set('phone')} />
          </label>
          <label className="field">
            <span className="field-label">Email</span>
            <input className="input" type="email" value={form.email} onChange={set('email')} />
          </label>
          <label className="field">
            <span className="field-label">State</span>
            <input className="input" value={form.state} onChange={set('state')} />
          </label>
          <label className="field">
            <span className="field-label">State code</span>
            <input className="input" value={form.stateCode} onChange={set('stateCode')} placeholder="e.g. 33" />
          </label>
          <label className="field">
            <span className="field-label">Country</span>
            <input className="input" value={form.country} onChange={set('country')} />
          </label>
          <label className="field field-wide">
            <span className="field-label">Billing address</span>
            <textarea className="input" rows={2} value={form.billingAddress} onChange={set('billingAddress')} />
          </label>
          <label className="field field-wide">
            <span className="field-label">Shipping address</span>
            <textarea className="input" rows={2} value={form.shippingAddress} onChange={set('shippingAddress')} />
          </label>
          <label className="field">
            <span className="field-label">Material/service categories</span>
            <input className="input" value={form.materialServiceCategories} onChange={set('materialServiceCategories')} />
          </label>
          <label className="field">
            <span className="field-label">Approved makes</span>
            <input className="input" value={form.approvedMakes} onChange={set('approvedMakes')} />
          </label>
          <label className="field">
            <span className="field-label">Payment terms</span>
            <input className="input" value={form.paymentTerms} onChange={set('paymentTerms')} />
          </label>
          <label className="field">
            <span className="field-label">Delivery terms</span>
            <input className="input" value={form.deliveryTerms} onChange={set('deliveryTerms')} />
          </label>
          <label className="field">
            <span className="field-label">Credit period (days)</span>
            <input className="input" type="number" min="0" value={form.creditPeriodDays} onChange={set('creditPeriodDays')} />
          </label>
          <div className="field-wide form-section-title">Bank details</div>
          <label className="field">
            <span className="field-label">Bank name</span>
            <input className="input" value={form.bankName} onChange={set('bankName')} placeholder="e.g. HDFC Bank" />
          </label>
          <label className="field">
            <span className="field-label">Account holder name</span>
            <input className="input" value={form.accountHolder} onChange={set('accountHolder')} />
          </label>
          <label className="field">
            <span className="field-label">Account number</span>
            <input className="input" inputMode="numeric" pattern="[0-9]{6,18}" title="6-18 digits" value={form.accountNumber} onChange={set('accountNumber')} />
          </label>
          <label className="field">
            <span className="field-label">IFSC code</span>
            <input
              className="input mono"
              pattern="[A-Za-z]{4}0[A-Za-z0-9]{6}"
              title="e.g. HDFC0001234"
              value={form.ifsc}
              onChange={set('ifsc')}
              placeholder="HDFC0001234"
            />
          </label>
          <label className="field">
            <span className="field-label">Branch</span>
            <input className="input" value={form.branch} onChange={set('branch')} />
          </label>
          <div className="field-wide form-section-title">Attachments</div>
          <label className="field">
            <span className="field-label">GST certificate * (PDF/JPG/PNG, max 5 MB)</span>
            <input
              className="input"
              type="file"
              accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
              onChange={(event) => setGstFile(event.target.files?.[0] ?? null)}
            />
            {!gstFile && existingAttachments.gstCertificate && (
              <span className="field-hint">Current: {existingAttachments.gstCertificate.fileName}</span>
            )}
          </label>
          <label className="field">
            <span className="field-label">Bank cheque leaf (PDF/JPG/PNG, max 5 MB)</span>
            <input
              className="input"
              type="file"
              accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
              onChange={(event) => setBankLeafFile(event.target.files?.[0] ?? null)}
            />
            {!bankLeafFile && existingAttachments.bankLeaf && (
              <span className="field-hint">Current: {existingAttachments.bankLeaf.fileName}</span>
            )}
          </label>
          <label className="field">
            <span className="field-label">PAN card (PDF/JPG/PNG, max 5 MB)</span>
            <input
              className="input"
              type="file"
              accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
              onChange={(event) => setPanFile(event.target.files?.[0] ?? null)}
            />
            {!panFile && existingAttachments.panCard && (
              <span className="field-hint">Current: {existingAttachments.panCard.fileName}</span>
            )}
          </label>
          {error && <div className="alert alert-error field-wide">{error}</div>}
          <div className="modal-actions field-wide">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving…' : mode === 'create' ? 'Create vendor' : 'Save changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
