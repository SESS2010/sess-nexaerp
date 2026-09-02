import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import {
  createCustomer, getNextCustomerCode, parseCustomerAttachmentMetadata, updateCustomer, uploadCustomerAttachment,
} from '../../api/customers'
import type { CustomerAttachmentKind, CustomerAttachmentMetadata } from '../../api/customers'
import { parseBankMetadata } from '../../api/vendors'
import type { CustomerDetail, UpsertCustomerRequest } from '../../types/customer'
import { ErrorAlert } from '../../components/ErrorAlert'

interface Props {
  mode: 'create' | 'edit'
  existing?: CustomerDetail
  onClose: () => void
  onSaved: (customerCode: string) => void
}

const CUSTOMER_TYPES = ['STANDARD', 'KEY_ACCOUNT', 'GOVERNMENT', 'EXPORT']

const ATTACHMENT_FIELDS: { key: keyof CustomerAttachmentMetadata; kind: CustomerAttachmentKind; label: string }[] = [
  { key: 'gstCertificate', kind: 'GST_CERTIFICATE', label: 'GST certificate' },
  { key: 'bankLeaf', kind: 'BANK_LEAF', label: 'Bank cheque leaf' },
  { key: 'msmeCertificate', kind: 'MSME_CERTIFICATE', label: 'MSME certificate' },
  { key: 'panCard', kind: 'PAN_CARD', label: 'PAN copy' },
]

export function CustomerFormModal({ mode, existing, onClose, onSaved }: Props) {
  const bank = parseBankMetadata(existing?.BankMetadata)
  const [form, setForm] = useState({
    customerCode: existing?.CustomerCode ?? '',
    legalCustomerName: existing?.LegalCustomerName ?? '',
    tradeName: existing?.TradeName ?? '',
    customerType: existing?.CustomerType ?? 'STANDARD',
    gstNumber: existing?.GstNumber ?? '',
    panNumber: existing?.PanNumber ?? '',
    contactPerson: existing?.ContactPerson ?? '',
    phone: existing?.Phone ?? '',
    email: existing?.Email ?? '',
    industry: existing?.Industry ?? '',
    billingAddress: existing?.BillingAddress ?? '',
    shippingAddress: existing?.ShippingAddress ?? '',
    state: existing?.State ?? '',
    stateCode: existing?.StateCode ?? '',
    country: existing?.Country ?? 'India',
    paymentTerms: existing?.PaymentTerms ?? '',
    creditPeriodDays: existing?.CreditPeriodDays?.toString() ?? '',
    creditLimit: existing?.CreditLimit?.toString() ?? '',
    bankName: bank.bankName ?? '',
    accountHolder: bank.accountHolder ?? '',
    accountNumber: bank.accountNumber ?? '',
    ifsc: bank.ifsc ?? '',
    branch: bank.branch ?? '',
  })
  const [existingAttachments] = useState<CustomerAttachmentMetadata>(
    () => parseCustomerAttachmentMetadata(existing?.AttachmentMetadataJson ?? null),
  )
  const [files, setFiles] = useState<Partial<Record<keyof CustomerAttachmentMetadata, File | null>>>({})
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<unknown>(null)

  useEffect(() => {
    if (mode === 'create') {
      getNextCustomerCode()
        .then((next) => setForm((prev) => ({ ...prev, customerCode: next.CustomerCode })))
        .catch(() => setError('Could not fetch the next customer code. Reload and try again.'))
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const set = (key: keyof typeof form) => (event: { target: { value: string } }) =>
    setForm((prev) => ({ ...prev, [key]: event.target.value }))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setSaving(true)
    setError(null)
    const optional = (value: string) => (value.trim() ? value.trim() : null)
    try {
      const attachments: CustomerAttachmentMetadata = { ...existingAttachments }
      for (const field of ATTACHMENT_FIELDS) {
        const file = files[field.key]
        if (file) {
          const uploaded = await uploadCustomerAttachment(field.kind, file)
          attachments[field.key] = { id: uploaded.Id, fileName: uploaded.FileName }
        }
      }

      const bankDetails = {
        bankName: optional(form.bankName),
        accountHolder: optional(form.accountHolder),
        accountNumber: optional(form.accountNumber),
        ifsc: optional(form.ifsc)?.toUpperCase() ?? null,
        branch: optional(form.branch),
      }

      const body: UpsertCustomerRequest = {
        CustomerCode: form.customerCode,
        LegalCustomerName: form.legalCustomerName,
        TradeName: optional(form.tradeName),
        CustomerType: form.customerType,
        GstNumber: optional(form.gstNumber),
        PanNumber: optional(form.panNumber),
        BillingAddress: optional(form.billingAddress),
        ShippingAddress: optional(form.shippingAddress),
        State: optional(form.state),
        StateCode: optional(form.stateCode),
        Country: form.country.trim() || 'India',
        ContactPerson: optional(form.contactPerson),
        Phone: optional(form.phone),
        Email: optional(form.email),
        Industry: optional(form.industry),
        PaymentTerms: optional(form.paymentTerms),
        CreditPeriodDays: form.creditPeriodDays.trim() ? Number(form.creditPeriodDays) : null,
        CreditLimit: form.creditLimit.trim() ? Number(form.creditLimit) : null,
        PortalOrganizationId: existing?.PortalOrganizationId ?? form.customerCode,
        Version: mode === 'edit' ? existing!.Version : null,
        BankMetadataJson: Object.values(bankDetails).some((v) => v !== null) ? JSON.stringify(bankDetails) : null,
        AttachmentMetadataJson: Object.keys(attachments).length > 0 ? JSON.stringify(attachments) : null,
      }
      if (mode === 'create') {
        const summary = await createCustomer(body)
        onSaved(summary.CustomerCode)
      } else {
        const detail = await updateCustomer(existing!.CustomerCode, body)
        onSaved(detail.CustomerCode)
      }
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
          <h2>{mode === 'create' ? 'New Customer' : `Edit ${existing?.CustomerCode}`}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>
        <form onSubmit={submit} className="form-grid">
          <label className="field">
            <span className="field-label">Customer code (auto-generated)</span>
            <input className="input" required value={form.customerCode} readOnly disabled placeholder="CUST-…" />
          </label>
          <label className="field">
            <span className="field-label">Legal customer name *</span>
            <input className="input" required value={form.legalCustomerName} onChange={set('legalCustomerName')} />
          </label>
          <label className="field">
            <span className="field-label">Trade name</span>
            <input className="input" value={form.tradeName} onChange={set('tradeName')} />
          </label>
          <label className="field">
            <span className="field-label">Customer type *</span>
            <select className="input" value={form.customerType} onChange={set('customerType')}>
              {CUSTOMER_TYPES.map((type) => <option key={type}>{type}</option>)}
            </select>
          </label>
          <label className="field">
            <span className="field-label">GSTIN</span>
            <input className="input mono" value={form.gstNumber} onChange={set('gstNumber')} placeholder="15-character GSTIN" />
          </label>
          <label className="field">
            <span className="field-label">PAN</span>
            <input className="input mono" value={form.panNumber} onChange={set('panNumber')} placeholder="10-character PAN" />
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
            <span className="field-label">Industry</span>
            <input className="input" value={form.industry} onChange={set('industry')} />
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
          <label className="field">
            <span className="field-label">Payment terms</span>
            <input className="input" value={form.paymentTerms} onChange={set('paymentTerms')} />
          </label>
          <label className="field">
            <span className="field-label">Credit period (days)</span>
            <input className="input" type="number" min="0" value={form.creditPeriodDays} onChange={set('creditPeriodDays')} />
          </label>
          <label className="field">
            <span className="field-label">Credit limit</span>
            <input className="input" type="number" min="0" step="0.01" value={form.creditLimit} onChange={set('creditLimit')} />
          </label>
          <label className="field field-wide">
            <span className="field-label">Billing address</span>
            <textarea className="input" rows={2} value={form.billingAddress} onChange={set('billingAddress')} />
          </label>
          <label className="field field-wide">
            <span className="field-label">Shipping address</span>
            <textarea className="input" rows={2} value={form.shippingAddress} onChange={set('shippingAddress')} />
          </label>

          <div className="field-wide form-section-title">Bank details</div>
          <label className="field">
            <span className="field-label">Bank name</span>
            <input className="input" value={form.bankName} onChange={set('bankName')} />
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
            <input className="input mono" pattern="[A-Za-z]{4}0[A-Za-z0-9]{6}" title="e.g. HDFC0001234" value={form.ifsc} onChange={set('ifsc')} placeholder="HDFC0001234" />
          </label>
          <label className="field">
            <span className="field-label">Branch</span>
            <input className="input" value={form.branch} onChange={set('branch')} />
          </label>

          <div className="field-wide form-section-title">Documents (PDF/JPG/PNG, max 5 MB)</div>
          {ATTACHMENT_FIELDS.map((field) => (
            <label key={field.kind} className="field">
              <span className="field-label">{field.label}</span>
              <input
                className="input"
                type="file"
                accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
                onChange={(event) => setFiles((prev) => ({ ...prev, [field.key]: event.target.files?.[0] ?? null }))}
              />
              {!files[field.key] && existingAttachments[field.key] && (
                <span className="field-hint">Current: {existingAttachments[field.key]!.fileName}</span>
              )}
            </label>
          ))}

          <ErrorAlert error={error} className="field-wide" fallback="Could not save the customer." />
          <div className="modal-actions field-wide">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving…' : mode === 'create' ? 'Create customer' : 'Save changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
