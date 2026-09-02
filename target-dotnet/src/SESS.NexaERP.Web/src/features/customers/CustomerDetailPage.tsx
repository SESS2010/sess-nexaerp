import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  downloadCustomerAttachment, getCustomer, parseCustomerAttachmentMetadata, runCustomerAction,
} from '../../api/customers'
import type { CustomerAction } from '../../api/customers'
import { parseBankMetadata } from '../../api/vendors'
import type { CustomerDetail } from '../../types/customer'
import { StatusBadge } from '../employees/StatusBadge'
import { CustomerFormModal } from './CustomerFormModal'
import { ErrorAlert } from '../../components/ErrorAlert'

const ACTIONS: { action: CustomerAction; label: string; from: string[] }[] = [
  { action: 'submit', label: 'Submit', from: ['Draft'] },
  { action: 'approve', label: 'Approve', from: ['Pending Approval', 'Clarification Requested'] },
  { action: 'reject', label: 'Reject', from: ['Pending Approval', 'Clarification Requested'] },
  { action: 'request-clarification', label: 'Request clarification', from: ['Pending Approval'] },
  { action: 'request-revision', label: 'Request revision', from: ['Pending Approval', 'Approved'] },
  { action: 'resubmit', label: 'Resubmit', from: ['Revision Requested', 'Rejected'] },
  { action: 'hold', label: 'Hold', from: ['Approved'] },
  { action: 'reactivate', label: 'Reactivate', from: ['Approved'] },
  { action: 'deactivate', label: 'Deactivate', from: ['Approved'] },
]

const ATTACHMENT_LABELS: { key: 'gstCertificate' | 'bankLeaf' | 'msmeCertificate' | 'panCard'; label: string }[] = [
  { key: 'gstCertificate', label: 'GST certificate' },
  { key: 'bankLeaf', label: 'Bank cheque leaf' },
  { key: 'msmeCertificate', label: 'MSME certificate' },
  { key: 'panCard', label: 'PAN copy' },
]

export function CustomerDetailPage() {
  const { customerCode = '' } = useParams()
  const [detail, setDetail] = useState<CustomerDetail | null>(null)
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      setDetail(await getCustomer(customerCode))
    } catch (err) {
      setDetail(null)
      setError(err)
    }
  }, [customerCode])

  useEffect(() => {
    void load()
  }, [load])

  const runAction = async (action: CustomerAction, label: string) => {
    if (!detail) return
    const remarks = window.prompt(`${label} — enter remarks (required):`)
    if (!remarks || !remarks.trim()) return
    setBusy(true)
    setError(null)
    try {
      await runCustomerAction(customerCode, action, remarks.trim(), detail.Version)
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const bank = parseBankMetadata(detail?.BankMetadata)
  const attachments = parseCustomerAttachmentMetadata(detail?.AttachmentMetadataJson ?? null)

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="breadcrumbs">
            <Link to="/customers">Customers</Link> / <span className="mono">{customerCode}</span>
          </div>
          <h1>{detail ? detail.LegalCustomerName : customerCode}</h1>
          {detail && (
            <p className="page-sub">
              <StatusBadge value={detail.Status} /> <StatusBadge value={detail.ApprovalStatus} />{' '}
              {detail.CustomerType} · v{detail.Version}
            </p>
          )}
        </div>
        {detail && (
          <div className="action-row">
            <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setEditing(true)}>Edit</button>
            {ACTIONS.filter((item) => item.from.includes(detail.ApprovalStatus)).map((item) => (
              <button key={item.action} type="button" className="btn btn-ghost" disabled={busy} onClick={() => runAction(item.action, item.label)}>
                {item.label}
              </button>
            ))}
          </div>
        )}
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />

      {detail && (
        <div className="card detail-grid">
          <Field label="Customer code" value={detail.CustomerCode} mono />
          <Field label="Legal name" value={detail.LegalCustomerName} />
          <Field label="Trade name" value={detail.TradeName ?? '—'} />
          <Field label="Type" value={detail.CustomerType} />
          <Field label="GSTIN" value={detail.GstNumber ?? '—'} mono />
          <Field label="PAN" value={detail.PanNumber ?? '—'} mono />
          <Field label="Contact person" value={detail.ContactPerson ?? '—'} />
          <Field label="Phone" value={detail.Phone ?? '—'} />
          <Field label="Email" value={detail.Email ?? '—'} />
          <Field label="Industry" value={detail.Industry ?? '—'} />
          <Field label="State" value={detail.State ? `${detail.State}${detail.StateCode ? ` (${detail.StateCode})` : ''}` : '—'} />
          <Field label="Country" value={detail.Country} />
          <Field label="Billing address" value={detail.BillingAddress ?? '—'} />
          <Field label="Shipping address" value={detail.ShippingAddress ?? '—'} />
          <Field label="Payment terms" value={detail.PaymentTerms ?? '—'} />
          <Field label="Credit period" value={detail.CreditPeriodDays != null ? `${detail.CreditPeriodDays} days` : '—'} />
          <Field label="Credit limit" value={detail.CreditLimit != null ? String(detail.CreditLimit) : '—'} mono />
          <Field label="Bank name" value={bank.bankName ?? '—'} />
          <Field label="Account holder" value={bank.accountHolder ?? '—'} />
          <Field label="Account number" value={bank.accountNumber ?? '—'} mono />
          <Field label="IFSC" value={bank.ifsc ?? '—'} mono />
          <Field label="Branch" value={bank.branch ?? '—'} />
          {ATTACHMENT_LABELS.map(({ key, label }) => (
            <div key={key} className="detail-field">
              <div className="field-label">{label}</div>
              {attachments[key] ? (
                <button
                  type="button"
                  className="link-button"
                  onClick={() => void downloadCustomerAttachment(attachments[key]!.id, attachments[key]!.fileName).catch(() => window.alert('Download failed.'))}
                >
                  {attachments[key]!.fileName}
                </button>
              ) : (
                <div>—</div>
              )}
            </div>
          ))}
        </div>
      )}

      {editing && detail && (
        <CustomerFormModal
          mode="edit"
          existing={detail}
          onClose={() => setEditing(false)}
          onSaved={() => { setEditing(false); void load() }}
        />
      )}
    </div>
  )
}

function Field({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="detail-field">
      <div className="field-label">{label}</div>
      <div className={mono ? 'mono' : undefined}>{value}</div>
    </div>
  )
}
