import { useCallback, useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  getQuotation,
  getQuotationTaxContext,
  getRfq,
  listRfqInvitations,
  newIdempotencyKey,
  quotationAttachmentUrl,
  rememberDoc,
  submitQuotation,
  verifyQuotationTechnically,
} from '../../api/purchase'
import { authorizedFetch, saveResponseAsFile } from '../../api/client'
import type {
  QuotationDetail,
  QuotationLineRequest,
  QuotationTaxContext,
  RfqInvitationCandidate,
} from '../../types/purchase'
import {
  QUOTATION_SUBMISSION_SOURCES,
  VENDOR_REGISTRATION_TYPES,
  quotationStateSourceWords,
  supplyTypeWords,
} from '../../types/purchase'
import { formatAmount } from './PurchaseRequisitionListPage'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import {
  draftLinesFor,
  invitationLabel,
  previousVersionFor,
  quotationLineLabel,
  sortInvitations,
} from './quotationDraft'
import type { DraftQuoteLine } from './quotationDraft'

function num(value: string): number {
  return Number(value) || 0
}

function lineTotal(line: DraftQuoteLine): number {
  return (
    num(line.quantity) * num(line.unitRate) -
    num(line.discountValue) +
    num(line.packingForwarding) +
    num(line.freight) +
    num(line.insurance) +
    num(line.otherCharges) +
    num(line.roundOff)
  )
}

function todayLocal(): string {
  const now = new Date()
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}`
}

export function QuotationPage() {
  const { can } = useSession()
  const [params] = useSearchParams()

  // GET /purchase/rfqs/{number} → purchase.rfq:view (only for the required dates).
  const canReadRfq = can(PAGE_KEYS.rfq, 'view')
  // GET /quotations/{number} → purchase.vendor-quotations:view (lines to verify).
  const canReadQuotation = can(PAGE_KEYS.quotations, 'view')
  // POST /rfq-invitations/{id}/quotations → purchase.vendor-quotations:create.
  const canRecordQuotation = can(PAGE_KEYS.quotations, 'create')
  // POST /quotations/{number}/technical-verifications →
  // purchase.technical-verification:verify.
  const canVerifyTechnically = can(PAGE_KEYS.technicalVerification, 'verify')
  // GET /quotations/{number}/attachment → purchase.vendor-quotations:download.
  const canDownloadAttachment = can(PAGE_KEYS.quotations, 'download')

  // --- source invitation (GET /rfq-invitations; no ids are typed) ---
  const [invitations, setInvitations] = useState<RfqInvitationCandidate[]>([])
  const [invitationsError, setInvitationsError] = useState<unknown>(null)
  const [loadingInvitations, setLoadingInvitations] = useState(false)
  const [invitationId, setInvitationId] = useState('')
  const [lines, setLines] = useState<DraftQuoteLine[]>([])
  const invitation = useMemo(
    () => invitations.find((row) => row.InvitationId === invitationId) ?? null,
    [invitations, invitationId],
  )

  // --- quotation header ---
  const [vendorQuoteReference, setVendorQuoteReference] = useState('')
  const [currencyCode, setCurrencyCode] = useState('INR')
  const [paymentTerms, setPaymentTerms] = useState('')
  const [deliveryTerms, setDeliveryTerms] = useState('')
  const [warrantyTerms, setWarrantyTerms] = useState('')
  const [submissionSource, setSubmissionSource] = useState<string>(QUOTATION_SUBMISSION_SOURCES[0].value)
  const [receivedAt, setReceivedAt] = useState(todayLocal())
  const [attachmentObjectKey, setAttachmentObjectKey] = useState('')
  const [attachmentSha256, setAttachmentSha256] = useState('')
  const [vendorAttestation, setVendorAttestation] = useState('')
  // R2: both GST state codes come from the server for the selected invitation
  // (vendor GSTIN / state, delivery warehouse / company profile). They are shown
  // read-only and sent back exactly as derived; submit is refused until they load.
  const [taxContext, setTaxContext] = useState<QuotationTaxContext | null>(null)
  const [taxContextError, setTaxContextError] = useState<unknown>(null)
  const [loadingTaxContext, setLoadingTaxContext] = useState(false)
  const [taxContextTick, setTaxContextTick] = useState(0)
  const [vendorRegistrationType, setVendorRegistrationType] = useState<string>(VENDOR_REGISTRATION_TYPES[0])
  const [headerDiscountValue, setHeaderDiscountValue] = useState('0')
  const [requestLateAuthorization, setRequestLateAuthorization] = useState(false)
  const [lateAuthorizationRemarks, setLateAuthorizationRemarks] = useState('')

  // --- technical verification (lines from GET /quotations/{number}) ---
  const [verifyQuotationNumber, setVerifyQuotationNumber] = useState(params.get('quotation') ?? '')
  const [verifyQuotation, setVerifyQuotation] = useState<QuotationDetail | null>(null)
  const [loadingVerifyQuotation, setLoadingVerifyQuotation] = useState(false)
  const [verifyLineId, setVerifyLineId] = useState('')
  const [verifyCompliant, setVerifyCompliant] = useState(true)
  const [verifyEvidence, setVerifyEvidence] = useState('{}')
  const [verifyRemarks, setVerifyRemarks] = useState('')
  const [verifyVersion, setVerifyVersion] = useState('0')
  const [verifying, setVerifying] = useState(false)

  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')

  const grandTotal = useMemo(
    () => lines.reduce((sum, line) => sum + lineTotal(line), 0) - num(headerDiscountValue),
    [lines, headerDiscountValue],
  )

  const loadInvitations = useCallback(async () => {
    if (!canRecordQuotation) return
    setInvitationsError(null)
    setLoadingInvitations(true)
    try {
      setInvitations(sortInvitations(await listRfqInvitations()))
    } catch (err) {
      setInvitations([])
      setInvitationsError(err)
    } finally {
      setLoadingInvitations(false)
    }
  }, [canRecordQuotation])

  useEffect(() => {
    void loadInvitations()
  }, [loadInvitations])

  // ?rfq=RFQ-… preselects that RFQ's only invitation; ?vendor=CODE narrows to one vendor.
  useEffect(() => {
    if (invitationId || invitations.length === 0) return
    const rfqParam = params.get('rfq')?.trim().toUpperCase()
    const vendorParam = params.get('vendor')?.trim().toUpperCase()
    if (!rfqParam) return
    const matches = invitations.filter((row) =>
      row.RfqNumber.toUpperCase() === rfqParam && (!vendorParam || row.VendorCode.toUpperCase() === vendorParam))
    if (matches.length === 1) setInvitationId(matches[0].InvitationId)
  }, [invitations, invitationId, params])

  // Choosing an invitation fills the lines, currency and revision from the server.
  useEffect(() => {
    if (!invitation) {
      setLines([])
      return
    }
    setCurrencyCode(invitation.CurrencyCode)
    setLines(draftLinesFor(invitation, null))
    if (!canReadRfq) return
    let cancelled = false
    getRfq(invitation.RfqNumber)
      .then((detail) => { if (!cancelled) setLines(draftLinesFor(invitation, detail)) })
      .catch(() => { /* required dates are a convenience; the lines are already filled */ })
    return () => { cancelled = true }
  }, [invitation, canReadRfq])

  useEffect(() => {
    setTaxContext(null)
    setTaxContextError(null)
    if (!invitationId || !canRecordQuotation) {
      setLoadingTaxContext(false)
      return
    }
    let cancelled = false
    setLoadingTaxContext(true)
    getQuotationTaxContext(invitationId)
      .then((context) => { if (!cancelled) setTaxContext(context) })
      .catch((err) => { if (!cancelled) setTaxContextError(err) })
      .finally(() => { if (!cancelled) setLoadingTaxContext(false) })
    return () => { cancelled = true }
  }, [invitationId, canRecordQuotation, taxContextTick])

  const loadVerifyQuotation = useCallback(async (number: string) => {
    const trimmed = number.trim()
    setVerifyQuotation(null)
    setVerifyLineId('')
    if (!trimmed) return
    setLoadingVerifyQuotation(true)
    try {
      const detail = await getQuotation(trimmed)
      setVerifyQuotation(detail)
      setVerifyVersion(String(detail.Version))
      if (detail.Lines.length === 1) setVerifyLineId(detail.Lines[0].Id)
    } catch (err) {
      setError(err)
    } finally {
      setLoadingVerifyQuotation(false)
    }
  }, [])

  useEffect(() => {
    const initial = params.get('quotation')
    if (initial && canReadQuotation) void loadVerifyQuotation(initial)
  }, [params, canReadQuotation, loadVerifyQuotation])

  const setLine = (index: number, patch: Partial<DraftQuoteLine>) => {
    setLines((prev) => prev.map((line, i) => (i === index ? { ...line, ...patch } : line)))
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    setNotice('')

    if (!invitation) {
      setError('Choose the RFQ and vendor being quoted.')
      return
    }
    if (lines.length === 0) {
      setError('The chosen RFQ has no lines to quote.')
      return
    }
    if (!taxContext) {
      setError(
        loadingTaxContext
          ? 'The GST state codes for this invitation are still being looked up. Wait a moment and try again.'
          : 'The GST state codes for this invitation could not be derived, so the quotation cannot be recorded. See the Tax identity section.',
      )
      return
    }
    if (lines.some((line) => !line.hsnSacCode.trim())) {
      setError('Every line needs an HSN/SAC code for the tax computation.')
      return
    }
    if (requestLateAuthorization && !lateAuthorizationRemarks.trim()) {
      setError('Late-submission authorization needs a written reason.')
      return
    }

    const payloadLines: QuotationLineRequest[] = lines.map((line) => ({
      RequestForQuotationLineId: line.rfqLineId,
      Quantity: num(line.quantity),
      UnitRate: num(line.unitRate),
      DiscountValue: num(line.discountValue),
      PackingForwarding: num(line.packingForwarding),
      Freight: num(line.freight),
      Insurance: num(line.insurance),
      OtherCharges: num(line.otherCharges),
      PromisedDeliveryDate: line.promisedDeliveryDate,
      HsnSacCode: line.hsnSacCode.trim(),
      SupplierStateCode: taxContext.SupplierStateCode,
      PlaceOfSupplyStateCode: taxContext.PlaceOfSupplyStateCode,
      VendorRegistrationType: vendorRegistrationType,
      RoundOff: num(line.roundOff),
    }))

    setSaving(true)
    try {
      const previousVersion = previousVersionFor(invitation)
      const result = await submitQuotation(invitation.InvitationId, {
        VendorQuoteReference: vendorQuoteReference.trim(),
        CurrencyCode: currencyCode.trim().toUpperCase(),
        PaymentTerms: paymentTerms.trim(),
        DeliveryTerms: deliveryTerms.trim(),
        WarrantyTerms: warrantyTerms.trim(),
        RequestLateAuthorization: requestLateAuthorization,
        LateAuthorizationRemarks: requestLateAuthorization ? lateAuthorizationRemarks.trim() : null,
        SubmissionSource: submissionSource,
        ReceivedAt: new Date(receivedAt).toISOString(),
        AttachmentObjectKey: attachmentObjectKey.trim(),
        AttachmentSha256: attachmentSha256.trim(),
        VendorAttestation: vendorAttestation.trim(),
        InvitationVersion: invitation.InvitationVersion,
        PreviousQuotationVersion: previousVersion,
        IdempotencyKey: newIdempotencyKey('quote-submit'),
        Lines: payloadLines,
        HeaderDiscountValue: num(headerDiscountValue),
      })
      rememberDoc('quotation', result.Number)
      setVerifyQuotationNumber(result.Number)
      setVerifyVersion(String(result.Version))
      setNotice(`Quotation ${result.Number} recorded (status ${result.Status}, version ${result.Version}).`)
      // The invitation now carries a current quotation version; refresh so a
      // revision sends the right previous version, and load lines to verify.
      void loadInvitations()
      if (canReadQuotation) void loadVerifyQuotation(result.Number)
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  const runVerification = async () => {
    setError(null)
    setNotice('')
    if (!verifyQuotationNumber.trim() || !verifyLineId) {
      setError('Load the quotation and choose the line being verified.')
      return
    }
    setVerifying(true)
    try {
      const result = await verifyQuotationTechnically(verifyQuotationNumber.trim(), {
        VendorQuotationLineId: verifyLineId,
        IsCompliant: verifyCompliant,
        ComplianceEvidenceJson: verifyEvidence.trim() || '{}',
        Remarks: verifyRemarks.trim(),
        QuotationVersion: num(verifyVersion),
        IdempotencyKey: newIdempotencyKey('quote-verify'),
      })
      setVerifyVersion(String(result.Version))
      setNotice(`Technical verification recorded. ${result.Number} is now ${result.Status}.`)
    } catch (err) {
      setError(err)
    } finally {
      setVerifying(false)
    }
  }

  const downloadAttachment = async () => {
    const number = verifyQuotationNumber.trim()
    if (!number) return
    try {
      const response = await authorizedFetch(quotationAttachmentUrl(number))
      await saveResponseAsFile(response, `${number}-quotation`)
    } catch (err) {
      setError(err)
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Vendor Quotations</h1>
          <p className="page-sub">
            Step 3 of the purchase flow — record what each invited vendor quoted, then verify it technically
          </p>
        </div>
      </div>

      <ErrorAlert error={error} fallback="The last action failed." />
      {notice && <div className="alert">{notice}</div>}

      {canRecordQuotation && (
        <div className="card">
          <div className="form-section-title">1 · Choose the RFQ and vendor being quoted</div>
          <div className="form-grid">
            <label className="field field-wide">
              <span className="field-label">RFQ · vendor *</span>
              <select
                className="input"
                value={invitationId}
                disabled={loadingInvitations}
                onChange={(event) => setInvitationId(event.target.value)}
              >
                <option value="">
                  {loadingInvitations
                    ? 'Loading invited vendors…'
                    : invitations.length === 0
                      ? 'No invited vendors yet — invite one from the RFQ page'
                      : 'Choose an invited vendor'}
                </option>
                {invitations.map((row) => (
                  <option key={row.InvitationId} value={row.InvitationId}>{invitationLabel(row)}</option>
                ))}
              </select>
            </label>
          </div>
          {invitationsError != null && (
            <ErrorAlert
              error={invitationsError}
              onReload={() => void loadInvitations()}
              fallback="The invited vendors could not be loaded."
            />
          )}
          {invitation && (
            <p className="field-hint">
              {invitation.RfqNumber} — {invitation.Lines.length} line(s), currency {invitation.CurrencyCode},
              quotes due {new Date(invitation.QuoteDueAt).toLocaleString('en-IN')}, invitation {invitation.Status}.
              {invitation.CurrentQuotationVersion != null && ' This vendor already quoted; recording again creates a revision.'}
            </p>
          )}
        </div>
      )}

      <form onSubmit={submit}>
        <div className="card">
          <div className="form-section-title">2 · Quotation header</div>
          <div className="form-grid">
            <label className="field">
              <span className="field-label">Vendor quote reference *</span>
              <input className="input" value={vendorQuoteReference} onChange={(e) => setVendorQuoteReference(e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Currency *</span>
              <input className="input mono" value={currencyCode} onChange={(e) => setCurrencyCode(e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Received at *</span>
              <input type="datetime-local" className="input" value={receivedAt} onChange={(e) => setReceivedAt(e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Submission source *</span>
              <select className="input" value={submissionSource} onChange={(e) => setSubmissionSource(e.target.value)}>
                {QUOTATION_SUBMISSION_SOURCES.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
              </select>
            </label>
            <label className="field">
              <span className="field-label">Payment terms *</span>
              <input className="input" value={paymentTerms} onChange={(e) => setPaymentTerms(e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Delivery terms *</span>
              <input className="input" value={deliveryTerms} onChange={(e) => setDeliveryTerms(e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Warranty terms *</span>
              <input className="input" value={warrantyTerms} onChange={(e) => setWarrantyTerms(e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Header discount</span>
              <input className="input text-right mono" value={headerDiscountValue} onChange={(e) => setHeaderDiscountValue(e.target.value)} />
            </label>

            <div className="field-wide form-section-title">Tax identity</div>
            <label className="field">
              <span className="field-label">Vendor registration type *</span>
              <select className="input" value={vendorRegistrationType} onChange={(e) => setVendorRegistrationType(e.target.value)}>
                {VENDOR_REGISTRATION_TYPES.map((option) => <option key={option} value={option}>{option}</option>)}
              </select>
            </label>
            <label className="field">
              <span className="field-label">Supplier state code *</span>
              <input
                className="input mono"
                value={taxContext?.SupplierStateCode ?? ''}
                readOnly
                placeholder={loadingTaxContext ? 'Looking up…' : '—'}
                title="Derived by the server from the vendor; cannot be typed."
              />
              {taxContext && (
                <span className="field-hint">
                  Supplier state {taxContext.SupplierStateCode} {quotationStateSourceWords(taxContext.SupplierStateSource)} (vendor {taxContext.VendorCode}).
                </span>
              )}
            </label>
            <label className="field">
              <span className="field-label">Place of supply state code *</span>
              <input
                className="input mono"
                value={taxContext?.PlaceOfSupplyStateCode ?? ''}
                readOnly
                placeholder={loadingTaxContext ? 'Looking up…' : '—'}
                title="Derived by the server from the delivery location; cannot be typed."
              />
              {taxContext && (
                <span className="field-hint">
                  Place of supply {taxContext.PlaceOfSupplyStateCode} {quotationStateSourceWords(taxContext.PlaceOfSupplySource)}.
                </span>
              )}
            </label>
            <div className="field-wide">
              {taxContext ? (
                <div className="alert" style={{ marginBottom: 0 }}>
                  <strong>{supplyTypeWords(taxContext.SupplyType)}</strong> — supplier state {taxContext.SupplierStateCode}{' '}
                  {quotationStateSourceWords(taxContext.SupplierStateSource)} · place of supply {taxContext.PlaceOfSupplyStateCode}{' '}
                  {quotationStateSourceWords(taxContext.PlaceOfSupplySource)} · RFQ {taxContext.RfqNumber}, vendor {taxContext.VendorCode}.
                  These codes are decided by the server and sent as shown; the quotation is refused if they differ.
                </div>
              ) : loadingTaxContext ? (
                <p className="field-hint">Looking up the GST state codes for {invitation?.VendorCode ?? 'the chosen vendor'}…</p>
              ) : taxContextError ? (
                <>
                  <ErrorAlert
                    error={taxContextError}
                    onReload={() => setTaxContextTick((value) => value + 1)}
                    fallback="The GST state codes for this invitation could not be derived."
                  />
                  <p className="field-hint">
                    The quotation cannot be recorded until this is fixed: a vendor needs a GSTIN or a two-digit state code in the Vendor Master,
                    and the company profile needs its state code (Technical Director).
                  </p>
                </>
              ) : (
                <p className="field-hint">
                  Choose the RFQ and vendor above; the supplier state and place of supply are then derived from the vendor and the delivery location.
                </p>
              )}
            </div>

            <div className="field-wide form-section-title">Evidence</div>
            <label className="field">
              <span className="field-label">Attachment object key *</span>
              <input className="input mono" value={attachmentObjectKey} onChange={(e) => setAttachmentObjectKey(e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Attachment SHA-256 *</span>
              <input className="input mono" value={attachmentSha256} onChange={(e) => setAttachmentSha256(e.target.value)} />
              <span className="field-hint">
                There is no upload endpoint for quotation files — the key and hash must be supplied.
              </span>
            </label>
            <label className="field field-wide">
              <span className="field-label">Vendor attestation *</span>
              <textarea className="input" rows={2} value={vendorAttestation} onChange={(e) => setVendorAttestation(e.target.value)} />
            </label>

            <label className="field">
              <span className="field-label">Late submission</span>
              <select
                className="input"
                value={requestLateAuthorization ? 'yes' : 'no'}
                onChange={(e) => setRequestLateAuthorization(e.target.value === 'yes')}
              >
                <option value="no">On time</option>
                <option value="yes">Late — request authorization</option>
              </select>
            </label>
            {requestLateAuthorization && (
              <label className="field field-wide">
                <span className="field-label">Late authorization remarks *</span>
                <textarea className="input" rows={2} value={lateAuthorizationRemarks} onChange={(e) => setLateAuthorizationRemarks(e.target.value)} />
              </label>
            )}
          </div>
        </div>

        <div className="card">
          <div className="form-section-title">3 · Quoted lines ({lines.length})</div>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Item</th>
                  <th className="text-right">Qty</th>
                  <th className="text-right">Unit rate</th>
                  <th className="text-right">Discount</th>
                  <th className="text-right">P&amp;F</th>
                  <th className="text-right">Freight</th>
                  <th className="text-right">Insurance</th>
                  <th className="text-right">Other</th>
                  <th className="text-right">Round off</th>
                  <th>HSN/SAC</th>
                  <th>Promised</th>
                  <th className="text-right">Line total</th>
                </tr>
              </thead>
              <tbody>
                {lines.length === 0 && (
                  <tr><td colSpan={12} className="table-empty">Choose the RFQ and vendor above to pull the lines.</td></tr>
                )}
                {lines.map((line, index) => (
                  <tr key={line.rfqLineId}>
                    <td className="mono">
                      {line.itemCode}
                      <div className="field-hint">{line.itemName} · {line.uom}</div>
                    </td>
                    <td><input className="input text-right mono" value={line.quantity} onChange={(e) => setLine(index, { quantity: e.target.value })} /></td>
                    <td><input className="input text-right mono" value={line.unitRate} onChange={(e) => setLine(index, { unitRate: e.target.value })} /></td>
                    <td><input className="input text-right mono" value={line.discountValue} onChange={(e) => setLine(index, { discountValue: e.target.value })} /></td>
                    <td><input className="input text-right mono" value={line.packingForwarding} onChange={(e) => setLine(index, { packingForwarding: e.target.value })} /></td>
                    <td><input className="input text-right mono" value={line.freight} onChange={(e) => setLine(index, { freight: e.target.value })} /></td>
                    <td><input className="input text-right mono" value={line.insurance} onChange={(e) => setLine(index, { insurance: e.target.value })} /></td>
                    <td><input className="input text-right mono" value={line.otherCharges} onChange={(e) => setLine(index, { otherCharges: e.target.value })} /></td>
                    <td><input className="input text-right mono" value={line.roundOff} onChange={(e) => setLine(index, { roundOff: e.target.value })} /></td>
                    <td><input className="input mono" value={line.hsnSacCode} onChange={(e) => setLine(index, { hsnSacCode: e.target.value })} /></td>
                    <td><input type="date" className="input" value={line.promisedDeliveryDate} onChange={(e) => setLine(index, { promisedDeliveryDate: e.target.value })} /></td>
                    <td className="text-right mono">{formatAmount(lineTotal(line))}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="action-row" style={{ marginTop: 16 }}>
            {canRecordQuotation && (
              <button
                type="submit"
                className="btn btn-primary"
                disabled={saving || lines.length === 0 || !taxContext}
                title={!taxContext ? 'Waiting for the GST state codes of the selected invitation.' : undefined}
              >
                {saving ? 'Recording…' : 'Record quotation'}
              </button>
            )}
            <div className="spacer" />
            <strong>Quoted total: ₹{formatAmount(grandTotal)}</strong>
          </div>
        </div>
      </form>

      <div className="card">
        <div className="form-section-title">4 · Technical verification</div>
        <div className="form-grid">
          <label className="field">
            <span className="field-label">Quotation number *</span>
            <input
              className="input mono"
              placeholder="VQ-2627-00001"
              value={verifyQuotationNumber}
              onChange={(e) => { setVerifyQuotationNumber(e.target.value); setVerifyQuotation(null); setVerifyLineId('') }}
              onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); void loadVerifyQuotation(verifyQuotationNumber) } }}
            />
          </label>
          <div className="field">
            <span className="field-label">&nbsp;</span>
            <button
              type="button"
              className="btn btn-ghost"
              disabled={!canReadQuotation || loadingVerifyQuotation || !verifyQuotationNumber.trim()}
              title={!canReadQuotation ? 'Needs view access to vendor quotations.' : undefined}
              onClick={() => void loadVerifyQuotation(verifyQuotationNumber)}
            >
              {loadingVerifyQuotation ? 'Loading…' : 'Load quotation lines'}
            </button>
          </div>
          <label className="field field-wide">
            <span className="field-label">Quotation line *</span>
            <select
              className="input"
              value={verifyLineId}
              disabled={!verifyQuotation}
              onChange={(e) => setVerifyLineId(e.target.value)}
            >
              <option value="">{verifyQuotation ? 'Choose the line being verified' : 'Load the quotation first'}</option>
              {verifyQuotation?.Lines.map((line) => (
                <option key={line.Id} value={line.Id}>{quotationLineLabel(line)}</option>
              ))}
            </select>
            {verifyQuotation && (
              <span className="field-hint">
                {verifyQuotation.QuotationNumber} · {verifyQuotation.VendorCode} — {verifyQuotation.VendorName} ·
                RFQ {verifyQuotation.RfqNumber} · {verifyQuotation.Status} · version {verifyQuotation.Version}
              </span>
            )}
            {!canReadQuotation && (
              <span className="field-hint">
                Choosing a line needs view access to vendor quotations (purchase.vendor-quotations:view).
              </span>
            )}
          </label>
          <label className="field">
            <span className="field-label">Result *</span>
            <select className="input" value={verifyCompliant ? 'yes' : 'no'} onChange={(e) => setVerifyCompliant(e.target.value === 'yes')}>
              <option value="yes">Technically compliant</option>
              <option value="no">Not compliant</option>
            </select>
          </label>
          <label className="field">
            <span className="field-label">Compliance evidence (JSON)</span>
            <input className="input mono" value={verifyEvidence} onChange={(e) => setVerifyEvidence(e.target.value)} />
          </label>
          <label className="field field-wide">
            <span className="field-label">Remarks</span>
            <textarea className="input" rows={2} value={verifyRemarks} onChange={(e) => setVerifyRemarks(e.target.value)} />
          </label>
          <div className="field-wide action-row">
            {canVerifyTechnically && (
              <button type="button" className="btn btn-primary" disabled={verifying} onClick={() => void runVerification()}>
                {verifying ? 'Recording…' : 'Record verification'}
              </button>
            )}
            {canDownloadAttachment && (
              <button type="button" className="btn btn-ghost" onClick={() => void downloadAttachment()}>
                Download quotation attachment
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
