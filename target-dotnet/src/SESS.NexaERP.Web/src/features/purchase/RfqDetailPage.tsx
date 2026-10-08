import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  listVendorOptions,
  newIdempotencyKey,
  rememberDoc,
} from '../../api/purchase'
import type { VendorOption } from '../../api/purchase'
import type { RfqDetail, RfqInvitationCandidate } from '../../types/purchase'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { StatusBadge } from '../employees/StatusBadge'
import { formatAmount, formatDate } from './PurchaseRequisitionListPage'
import { ErrorAlert } from '../../components/ErrorAlert'
import { HistoryPanel } from '../../components/HistoryPanel'
import { CopyId } from '../../components/CopyId'
import { OperationIntent, RequestGate, invitationsForRfq, sessionScopeKey } from './quotationDraft'
import { inviteVendorFlow } from './quotationFlows'
import { quotationFlowApi } from './quotationFlowApi'
import { ScreenLifecycle, TARGET } from './screenLifecycle'


export function RfqDetailPage() {
  const { rfqNumber = '' } = useParams()
  const navigate = useNavigate()
  const { me, can } = useSession()

  // POST /purchase/rfqs/{number}/vendors → purchase.rfq:submit. The vendor
  // picker behind it reads masters.vendors:view.
  const canInviteVendor = can(PAGE_KEYS.rfq, 'submit') && can(PAGE_KEYS.vendors, 'view')
  // GET /purchase/rfq-invitations and the quotation entry both need purchase.vendor-quotations:create.
  const canRecordQuotation = can(PAGE_KEYS.quotations, 'create')

  const [rfq, setRfq] = useState<RfqDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')

  const [vendors, setVendors] = useState<VendorOption[]>([])
  const [vendorSearch, setVendorSearch] = useState('')
  const [vendorId, setVendorId] = useState('')
  const [inviteRemarks, setInviteRemarks] = useState('')
  const [inviting, setInviting] = useState(false)
  const [invitations, setInvitations] = useState<RfqInvitationCandidate[]>([])
  const [invitationsError, setInvitationsError] = useState<unknown>(null)
  const invitationGate = useRef(new RequestGate())
  const loadGate = useRef(new RequestGate())
  const vendorGate = useRef(new RequestGate())
  // Unmount, company/login/RFQ change, or another chosen vendor drops in-flight
  // reads and write continuations (screenLifecycle.ts).
  const [lifecycle] = useState(() =>
    new ScreenLifecycle().track(invitationGate.current, loadGate.current, vendorGate.current))

  useEffect(() => {
    lifecycle.mount()
    return () => lifecycle.unmount()
  }, [lifecycle])

  const chooseVendor = (id: string) => {
    lifecycle.setTarget(TARGET.invite, id)
    setVendorId(id)
  }
  // One logical invite keeps its key across an identical retry (lost answer).
  const inviteIntent = useRef(new OperationIntent(() => newIdempotencyKey('rfq-invite')))
  const scope = sessionScopeKey(me)

  const load = useCallback(async () => {
    const ticket = loadGate.current.begin()
    setLoading(true)
    setError(null)
    try {
      const detail = await quotationFlowApi.getRfq(rfqNumber)
      if (!loadGate.current.isCurrent(ticket)) return
      setRfq(detail)
      rememberDoc('rfq', detail.RfqNumber)
    } catch (err) {
      if (!loadGate.current.isCurrent(ticket)) return
      setRfq(null)
      setError(err)
    } finally {
      if (loadGate.current.isCurrent(ticket)) setLoading(false)
    }
  }, [rfqNumber])

  const loadInvitations = useCallback(async () => {
    // Only callers holding purchase.vendor-quotations:create read invitations;
    // an RFQ viewer sees the RFQ without this call or an error.
    if (!canRecordQuotation) return
    const ticket = invitationGate.current.begin()
    setInvitationsError(null)
    try {
      const rows = invitationsForRfq(await quotationFlowApi.listRfqInvitations(), rfqNumber)
      if (invitationGate.current.isCurrent(ticket)) setInvitations(rows)
    } catch (err) {
      if (invitationGate.current.isCurrent(ticket)) {
        setInvitations([])
        setInvitationsError(err)
      }
    }
  }, [rfqNumber, canRecordQuotation])

  useEffect(() => {
    // New company / login / RFQ: clear everything shown, picked or typed.
    lifecycle.changeScope()
    inviteIntent.current.clear()
    setRfq(null)
    setVendors([])
    setVendorSearch('')
    setVendorId('')
    setInviteRemarks('')
    setInviting(false)
    setNotice('')
    setInvitations([])
    setInvitationsError(null)
    void load()
    void loadInvitations()
  }, [load, loadInvitations, scope, lifecycle])

  useEffect(() => {
    if (!canInviteVendor) return
    const ticket = vendorGate.current.begin()
    const handle = window.setTimeout(() => {
      listVendorOptions(vendorSearch)
        .then((rows) => { if (vendorGate.current.isCurrent(ticket)) setVendors(rows) })
        .catch((err) => { if (vendorGate.current.isCurrent(ticket)) setError(err) })
    }, 250)
    return () => window.clearTimeout(handle)
  }, [vendorSearch, canInviteVendor, scope])

  const invite = async () => {
    if (!rfq || inviting) return
    if (!vendorId) {
      setError('Pick a vendor to invite.')
      return
    }
    setError(null)
    setNotice('')
    // Live until unmount, a company/login/RFQ change, or another vendor is chosen.
    lifecycle.setTarget(TARGET.invite, vendorId)
    const isLive = lifecycle.begin(TARGET.invite)
    const picked = vendors.find((vendor) => vendor.Id === vendorId)
    const pickedLabel = picked ? `${picked.VendorCode} — ${picked.Name}` : 'Vendor'
    setInviting(true)
    try {
      const outcome = await inviteVendorFlow({
        api: quotationFlowApi,
        rfqNumber: rfq.RfqNumber,
        rfqVersion: rfq.Version,
        vendorId,
        remarks: inviteRemarks,
        canReadInvitations: canRecordQuotation,
        intent: inviteIntent.current,
        scope,
        isLive,
      })
      if (outcome.kind === 'abandoned') return
      if (outcome.kind === 'unresolved') {
        // Only withdrawn/cancelled history is on file: not invited. Keep the
        // vendor and remarks; the same invite keeps its key for a retry.
        setError(outcome.error)
        setNotice(`${pickedLabel} is not confirmed as invited — the answer was lost, and the only invitation on file for this vendor is ${outcome.status}. Check the invited vendors below before trying again.`)
        void loadInvitations()
        return
      }
      if (outcome.kind === 'failed') {
        setError(outcome.error)
        if (outcome.stale) {
          setNotice('The RFQ changed on the server and has been reloaded. Review it and invite again.')
          void load()
          void loadInvitations()
        }
        return
      }
      setVendorId('')
      setInviteRemarks('')
      setNotice(outcome.kind === 'done'
        ? `${pickedLabel} invited (${outcome.result.Status}).`
        : `${pickedLabel} is invited — the answer was lost on the way back, but the invitation is on the server.`)
      void load()
      void loadInvitations()
    } finally {
      if (isLive()) setInviting(false)
    }
  }

  if (loading && !rfq) {
    return <div className="page"><p>Loading…</p></div>
  }

  if (!rfq) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="RFQ not found." />
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

      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />
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

      {canInviteVendor && (
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
            <select className="input" value={vendorId} onChange={(event) => chooseVendor(event.target.value)}>
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
      )}

      {canRecordQuotation && (
        <>
          <h2>Invited vendors ({invitations.length})</h2>
          {invitationsError != null && (
            <ErrorAlert error={invitationsError} onReload={() => void loadInvitations()} fallback="Invited vendors could not be read." />
          )}
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Vendor</th>
                  <th>Status</th>
                  <th className="text-right">Invitation version</th>
                  <th>Quotation</th>
                  <th>Quotes due</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {invitationsError == null && invitations.length === 0 && (
                  <tr><td colSpan={6} className="table-empty">No vendors invited yet.</td></tr>
                )}
                {invitationsError != null && (
                  <tr><td colSpan={6} className="table-empty">Invited vendors could not be read. Reload to try again.</td></tr>
                )}
                {invitations.map((invitation) => (
                  <tr key={invitation.InvitationId}>
                    <td>
                      {invitation.VendorCode} — {invitation.VendorName}
                      <div><CopyId label="Invitation id" value={invitation.InvitationId} /></div>
                    </td>
                    <td><StatusBadge value={invitation.Status} /></td>
                    <td className="text-right mono">{invitation.InvitationVersion}</td>
                    <td>
                      {invitation.CurrentQuotationVersion == null
                        ? 'Not yet received'
                        : `Received · current quotation version ${invitation.CurrentQuotationVersion}`}
                    </td>
                    <td>{new Date(invitation.QuoteDueAt).toLocaleString('en-IN')}</td>
                    <td>
                      <Link
                        to={`/purchase/quotations/new?rfq=${encodeURIComponent(invitation.RfqNumber)}&vendor=${encodeURIComponent(invitation.VendorCode)}`}
                      >
                        {invitation.CurrentQuotationVersion == null ? 'Record quotation' : 'Revise quotation'}
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      <HistoryPanel docType="RFQ" documentId={rfq.Id} />
    </div>
  )
}
