import { useState } from 'react'
import type { FormEvent } from 'react'
import { createVendor, updateVendor } from '../../api/vendors'
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
  })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const set = (key: keyof typeof form) => (event: { target: { value: string } }) =>
    setForm((prev) => ({ ...prev, [key]: event.target.value }))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setSaving(true)
    setError('')
    const optional = (value: string) => (value.trim() ? value.trim() : null)
    try {
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
        BankMetadataJson: null,
        AttachmentMetadataJson: existing?.AttachmentMetadataJson ?? null,
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
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{mode === 'create' ? 'New Vendor' : `Edit ${existing?.VendorCode}`}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>
        <form onSubmit={submit} className="form-grid">
          <label className="field">
            <span className="field-label">Vendor code *</span>
            <input
              className="input"
              required
              value={form.vendorCode}
              onChange={set('vendorCode')}
              placeholder="VEND-XXX"
              disabled={mode === 'edit'}
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
