import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { downloadVendorAttachment, getVendor, parseAttachmentMetadata, parseBankMetadata, runVendorAction } from '../../api/vendors'
import type { VendorAction } from '../../api/vendors'
import { getVendorItems } from '../../api/items'
import type { VendorSuppliedItem } from '../../types/item'
import type { VendorDetail } from '../../types/vendor'
import { StatusBadge } from '../employees/StatusBadge'
import { VendorFormModal } from './VendorFormModal'

// Which workflow actions make sense from each approval status. The backend is
// the authority (permission + state checks); this only trims the button row.
const ACTIONS: { action: VendorAction; label: string; from: string[] }[] = [
  { action: 'submit', label: 'Submit', from: ['Draft'] },
  { action: 'approve', label: 'Approve', from: ['Pending Approval', 'Clarification Requested', 'Verified'] },
  { action: 'reject', label: 'Reject', from: ['Pending Approval', 'Clarification Requested', 'Verified'] },
  { action: 'request-clarification', label: 'Request clarification', from: ['Pending Approval'] },
  { action: 'request-revision', label: 'Request revision', from: ['Pending Approval', 'Approved'] },
  { action: 'resubmit', label: 'Resubmit', from: ['Revision Requested', 'Rejected'] },
  { action: 'hold', label: 'Hold', from: ['Approved'] },
  { action: 'reactivate', label: 'Reactivate', from: ['Approved'] },
  { action: 'deactivate', label: 'Deactivate', from: ['Approved'] },
  { action: 'blacklist', label: 'Blacklist', from: ['Approved'] },
]

export function VendorDetailPage() {
  const { vendorCode = '' } = useParams()
  const [detail, setDetail] = useState<VendorDetail | null>(null)
  const [suppliedItems, setSuppliedItems] = useState<VendorSuppliedItem[]>([])
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)

  const load = useCallback(async () => {
    setError('')
    try {
      setDetail(await getVendor(vendorCode))
      setSuppliedItems(await getVendorItems(vendorCode).catch(() => []))
    } catch (err) {
      setDetail(null)
      setError(err instanceof Error ? err.message : 'Failed to load vendor.')
    }
  }, [vendorCode])

  useEffect(() => {
    void load()
  }, [load])

  const runAction = async (action: VendorAction, label: string) => {
    if (!detail) return
    const remarks = window.prompt(`${label} — enter remarks (required):`)
    if (!remarks || !remarks.trim()) return
    setBusy(true)
    setError('')
    try {
      await runVendorAction(vendorCode, action, remarks.trim(), detail.Version)
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="breadcrumbs">
            <Link to="/vendors">Vendors</Link> / <span className="mono">{vendorCode}</span>
          </div>
          <h1>{detail ? detail.LegalVendorName : vendorCode}</h1>
          {detail && (
            <p className="page-sub">
              <StatusBadge value={detail.VendorStatus} /> <StatusBadge value={detail.ApprovalStatus} />{' '}
              {detail.VendorType} · v{detail.Version}
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

      {error && <div className="alert alert-error">{error}</div>}

      {detail && (
        <div className="card detail-grid">
          <Field label="Vendor code" value={detail.VendorCode} mono />
          <Field label="Legal name" value={detail.LegalVendorName} />
          <Field label="Trade name" value={detail.TradeName ?? '—'} />
          <Field label="Type" value={detail.VendorType} />
          <Field label="GSTIN" value={detail.GstNumber ?? '—'} mono />
          <Field label="PAN" value={detail.PanNumber ?? '—'} mono />
          <Field label="MSME" value={detail.MsmeStatus ? (detail.MsmeNumber ?? 'Yes') : 'No'} />
          <Field label="Contact person" value={detail.ContactPerson ?? '—'} />
          <Field label="Phone" value={detail.Phone ?? '—'} />
          <Field label="Email" value={detail.Email ?? '—'} />
          <Field label="State" value={detail.State ? `${detail.State}${detail.StateCode ? ` (${detail.StateCode})` : ''}` : '—'} />
          <Field label="Country" value={detail.Country} />
          <Field label="Billing address" value={detail.BillingAddress ?? '—'} />
          <Field label="Shipping address" value={detail.ShippingAddress ?? '—'} />
          <Field label="Categories" value={detail.MaterialServiceCategories ?? '—'} />
          <Field label="Approved makes" value={detail.ApprovedMakes ?? '—'} />
          <Field label="Payment terms" value={detail.PaymentTerms ?? '—'} />
          <Field label="Delivery terms" value={detail.DeliveryTerms ?? '—'} />
          <Field label="Credit period" value={detail.CreditPeriodDays != null ? `${detail.CreditPeriodDays} days` : '—'} />
          <Field label="Bank name" value={parseBankMetadata(detail.BankMetadata).bankName ?? '—'} />
          <Field label="Account holder" value={parseBankMetadata(detail.BankMetadata).accountHolder ?? '—'} />
          <Field label="Account number" value={parseBankMetadata(detail.BankMetadata).accountNumber ?? '—'} mono />
          <Field label="IFSC" value={parseBankMetadata(detail.BankMetadata).ifsc ?? '—'} mono />
          <Field label="Branch" value={parseBankMetadata(detail.BankMetadata).branch ?? '—'} />
          <AttachmentField label="GST certificate" attachment={parseAttachmentMetadata(detail.AttachmentMetadataJson).gstCertificate} />
          <AttachmentField label="Bank cheque leaf" attachment={parseAttachmentMetadata(detail.AttachmentMetadataJson).bankLeaf} />
          <AttachmentField label="PAN card" attachment={parseAttachmentMetadata(detail.AttachmentMetadataJson).panCard} />
        </div>
      )}

      {detail && (
        <div className="table-wrap mt-5">
          <table className="table">
            <thead>
              <tr><th colSpan={4}>Items supplied by this vendor ({suppliedItems.length})</th></tr>
            </thead>
            <tbody>
              {suppliedItems.length === 0 && (
                <tr><td colSpan={4} className="table-empty">No items linked — link vendors from the Item Master edit form.</td></tr>
              )}
              {suppliedItems.map((item) => (
                <tr key={item.ItemCode}>
                  <td className="mono">
                    <Link to={`/items/${encodeURIComponent(item.ItemCode)}`} className="text-accent hover:underline">{item.ItemCode}</Link>
                  </td>
                  <td>{item.Name}</td>
                  <td>{item.Uom}</td>
                  <td>{item.Relationship === 'PREFERRED' ? <span className="badge badge-info">Preferred</span> : <span className="badge badge-muted">Supplier</span>}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {editing && detail && (
        <VendorFormModal
          mode="edit"
          existing={detail}
          onClose={() => setEditing(false)}
          onSaved={() => { setEditing(false); void load() }}
        />
      )}
    </div>
  )
}

function AttachmentField({ label, attachment }: { label: string; attachment?: { id: string; fileName: string } }) {
  return (
    <div className="detail-field">
      <div className="field-label">{label}</div>
      {attachment ? (
        <button
          type="button"
          className="link-button"
          onClick={() => void downloadVendorAttachment(attachment.id, attachment.fileName).catch(() => window.alert('Download failed.'))}
        >
          {attachment.fileName}
        </button>
      ) : (
        <div>—</div>
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
