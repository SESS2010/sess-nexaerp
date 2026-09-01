import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  getRfq,
  inviteVendorToRfq,
  listVendorOptions,
  newIdempotencyKey,
  rememberDoc,
} from '../../api/purchase'
import type { VendorOption } from '../../api/purchase'
import type { RfqDetail } from '../../types/purchase'
import { StatusBadge } from '../employees/StatusBadge'
import { formatAmount, formatDate } from './PurchaseRequisitionListPage'

/**
 * Vendors invited from this browser. GET /api/v1/purchase/rfqs/{number} includes
 * Lines but NOT Invitations, so an invitation id is visible exactly once — in the
 * invite response. Quotation submission needs that id, so it is kept locally
 * until the API exposes GET /api/v1/purchase/rfq-invitations.
 */
interface LocalInvitation {
  InvitationId: string
  VendorId: string
  VendorLabel: string
  Status: string
  Version: number
  InvitedAt: string
}

function invitationKey(rfqNumber: string): string {
  return `nexaerp.purchase.rfqInvitations.${rfqNumber}`
}

function readInvitations(rfqNumber: string): LocalInvitation[] {
  try {
    const raw = localStorage.getItem(invitationKey(rfqNumber))
    return raw ? (JSON.parse(raw) as LocalInvitation[]) : []
  } catch {
    return []
  }
}

function writeInvitation(rfqNumber: string, invitation: LocalInvitation): LocalInvitation[] {
  const next = [invitation, ...readInvitations(rfqNumber).filter((x) => x.InvitationId !== invitation.InvitationId)]
  try {
    localStorage.setItem(invitationKey(rfqNumber), JSON.stringify(next))
  } catch {
    // storage unavailable; the id is still shown once in the table below
  }
  return next
}

export function RfqDetailPage() {
  const { rfqNumber = '' } = useParams()
  const navigate = useNavigate()

  const [rfq, setRfq] = useState<RfqDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  const [vendors, setVendors] = useState<VendorOption[]>([])
  const [vendorSearch, setVendorSearch] = useState('')
  const [vendorId, setVendorId] = useState('')
  const [inviteRemarks, setInviteRemarks] = useState('')
  const [inviting, setInviting] = useState(false)
  const [invitations, setInvitations] = useState<LocalInvitation[]>([])

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const detail = await getRfq(rfqNumber)
      setRfq(detail)
      rememberDoc('rfq', detail.RfqNumber)
    } catch (err) {
      setRfq(null)
      setError(err instanceof Error ? err.message : 'Failed to load the RFQ.')
    } finally {
      setLoading(false)
    }
  }, [rfqNumber])

  useEffect(() => {
    void load()
    setInvitations(readInvitations(rfqNumber.trim().toUpperCase()))
  }, [load, rfqNumber])

  useEffect(() => {
    const handle = window.setTimeout(() => {
      listVendorOptions(vendorSearch).then(setVendors).catch(() => undefined)
    }, 250)
    return () => window.clearTimeout(handle)
  }, [vendorSearch])

  const invite = async () => {
    if (!rfq) return
    if (!vendorId) {
      setError('Pick a vendor to invite.')
      return
    }
    setError('')
    setNotice('')
    setInviting(true)
    try {
      const result = await inviteVendorToRfq(rfq.RfqNumber, {
        VendorId: vendorId,
        Remarks: inviteRemarks.trim(),
        RfqVersion: rfq.Version,
        IdempotencyKey: newIdempotencyKey('rfq-invite'),
      })
      const picked = vendors.find((vendor) => vendor.Id === vendorId)
      setInvitations(
        writeInvitation(rfq.RfqNumber, {
          InvitationId: result.Id,
          VendorId: vendorId,
          VendorLabel: picked ? `${picked.VendorCode} — ${picked.Name}` : vendorId,
          Status: result.Status,
          Version: result.Version,
          InvitedAt: new Date().toISOString(),
        }),
      )
      setVendorId('')
      setInviteRemarks('')
      setNotice(`Vendor invited. Invitation id ${result.Id} — keep it, the API cannot list it back.`)
      void load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to invite the vendor.')
    } finally {
      setInviting(false)
    }
  }

  if (loading && !rfq) {
    return <div className="page"><p>Loading…</p></div>
  }

  if (!rfq) {
    return (
      <div className="page">
        <div className="alert alert-error">{error || 'RFQ not found.'}</div>
        <button type="button" className="btn btn-ghost" onClick={() => navigate('/purchase/rfqs')}>
          ‹ Back to RFQ
        </button>
      </div>
    )
  }

  return (
    <div className="page">
      <div className="breadcrumbs">
        <Link to="/purchase/rfqs">RFQ</Link> / <span className="mono">{rfq.RfqNumber}</span>
      </div>

      <div className="page-header">
        <div>
          <h1>{rfq.RfqNumber}</h1>
          <p className="page-sub">
            FY {rfq.FinancialYear} · quotes due {new Date(rfq.QuoteDueAt).toLocaleString('en-IN')} ·{' '}
            {rfq.CurrencyCode}
          </p>
        </div>
        <div className="action-row">
          <StatusBadge value={rfq.Status} />
        </div>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {notice && <div className="alert">{notice}</div>}

      <div className="card">
        <div className="detail-grid">
          <div className="detail-field">
            <span className="field-label">Sourcing</span>
            {rfq.IsSingleSource ? 'Single source' : 'Competitive'}
          </div>
          <div className="detail-field">
            <span className="field-label">Issued at</span>
            {rfq.IssuedAt ? new Date(rfq.IssuedAt).toLocaleString('en-IN') : '—'}
          </div>
          <div className="detail-field">
            <span className="field-label">Record version</span>
            <span className="mono">{rfq.Version}</span>
          </div>
          {rfq.IsSingleSource && (
            <div className="detail-field field-wide">
              <span className="field-label">Single-source justification</span>
              {rfq.SingleSourceJustification ?? '—'}
            </div>
          )}
        </div>
      </div>

      <h2>Lines ({rfq.Lines?.length ?? 0})</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>#</th>
              <th>PR</th>
              <th className="text-right">PR line</th>
              <th>Item</th>
              <th>Description</th>
              <th>UOM</th>
              <th className="text-right">Approved</th>
              <th className="text-right">Already ordered</th>
              <th className="text-right">Outstanding</th>
              <th className="text-right">RFQ qty</th>
              <th>Required by</th>
            </tr>
          </thead>
          <tbody>
            {(rfq.Lines ?? []).length === 0 && (
              <tr><td colSpan={11} className="table-empty">No lines on this RFQ.</td></tr>
            )}
            {(rfq.Lines ?? []).map((line) => (
              <tr key={line.Id}>
                <td className="mono">{line.LineNumber}</td>
                <td className="mono">{line.PrNumberSnapshot}</td>
                <td className="text-right mono">{line.PrLineNumberSnapshot}</td>
                <td className="mono">{line.ItemCodeSnapshot}</td>
                <td>{line.ItemNameSnapshot}</td>
                <td>{line.UomSnapshot}</td>
                <td className="text-right mono">{formatAmount(line.ApprovedQuantitySnapshot)}</td>
                <td className="text-right mono">{formatAmount(line.AlreadyOrderedQuantitySnapshot)}</td>
                <td className="text-right mono">{formatAmount(line.OutstandingQuantitySnapshot)}</td>
                <td className="text-right mono">{formatAmount(line.RfqQuantity)}</td>
                <td>{formatDate(line.RequiredDateSnapshot)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="card">
        <div className="form-section-title">Invite a vendor</div>
        <div className="form-grid">
          <label className="field">
            <span className="field-label">Search vendors</span>
            <input
              className="input search"
              placeholder="Vendor code or name…"
              value={vendorSearch}
              onChange={(event) => setVendorSearch(event.target.value)}
            />
          </label>
          <label className="field">
            <span className="field-label">Vendor *</span>
            <select className="input" value={vendorId} onChange={(event) => setVendorId(event.target.value)}>
              <option value="">Select vendor…</option>
              {vendors.map((vendor) => (
                <option key={vendor.Id} value={vendor.Id}>{vendor.VendorCode} — {vendor.Name}</option>
              ))}
            </select>
          </label>
          <label className="field field-wide">
            <span className="field-label">Remarks</span>
            <input
              className="input"
              value={inviteRemarks}
              onChange={(event) => setInviteRemarks(event.target.value)}
            />
          </label>
          <div className="field-wide action-row">
            <button type="button" className="btn btn-primary" disabled={inviting} onClick={() => void invite()}>
              {inviting ? 'Inviting…' : 'Invite vendor'}
            </button>
            <span className="field-hint">
              Sent with RFQ version {rfq.Version}. A competitive RFQ needs at least two vendors.
            </span>
          </div>
        </div>
      </div>

      <h2>Invitations issued from this browser</h2>
      <div className="alert">
        <span className="mono">GET /rfqs/{'{number}'}</span> returns Lines but not Invitations, and there is
        no <span className="mono">GET /rfq-invitations</span>. An invitation id appears once, in the
        invite response — quotation submission needs it, so it is kept here locally.
      </div>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Invitation id</th>
              <th>Vendor</th>
              <th>Status</th>
              <th className="text-right">Version</th>
              <th>Invited at</th>
            </tr>
          </thead>
          <tbody>
            {invitations.length === 0 && (
              <tr><td colSpan={5} className="table-empty">No invitations issued from this browser.</td></tr>
            )}
            {invitations.map((invitation) => (
              <tr key={invitation.InvitationId}>
                <td className="mono">{invitation.InvitationId}</td>
                <td>{invitation.VendorLabel}</td>
                <td><StatusBadge value={invitation.Status} /></td>
                <td className="text-right mono">{invitation.Version}</td>
                <td>{new Date(invitation.InvitedAt).toLocaleString('en-IN')}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
