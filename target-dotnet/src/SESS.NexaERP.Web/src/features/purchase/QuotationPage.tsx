import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  getQuotation,
  getQuotationTaxContext,
  getRfq,
  listQuotations,
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
  QuotationListItem,
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
import { CopyId } from '../../components/CopyId'
import {
  OperationIntent,
  RequestGate,
  currentQuotationFor,
  draftLinesFor,
  invitationDrift,
  invitationLabel,
  isStaleConflict,
  quotationLineLabel,
  quotationOptionLabel,
  quotationScreenReads,
  revisionIntent,
  sessionScopeKey,
  sortInvitations,
  verifiableQuotations,
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
  const { me, can } = useSession()
  const [params] = useSearchParams()

  // GET /purchase/rfqs/{number} → purchase.rfq:view (only for the required dates).
  const canReadRfq = can(PAGE_KEYS.rfq, 'view')
  // GET /quotations and /quotations/{number} → purchase.vendor-quotations:view.
  const canReadQuotation = can(PAGE_KEYS.quotations, 'view')
  // GET /rfq-invitations and POST /rfq-invitations/{id}/quotations → purchase.vendor-quotations:create.
  const canRecordQuotation = can(PAGE_KEYS.quotations, 'create')
  // POST /quotations/{number}/technical-verifications →
  // purchase.technical-verification:verify.
  const canVerifyTechnically = can(PAGE_KEYS.technicalVerification, 'verify')
  // GET /quotations/{number}/attachment → purchase.vendor-quotations:download.
  const canDownloadAttachment = can(PAGE_KEYS.quotations, 'download')
  const reads = quotationScreenReads({ recordQuotation: canRecordQuotation, readQuotation: canReadQuotation, readRfq: canReadRfq })

  // Company / login scope: a change clears every list, selection and pending key.
  const scope = sessionScopeKey(me)
  const invitationGate = useRef(new RequestGate())
  const linesGate = useRef(new RequestGate())
  const pendingGate = useRef(new RequestGate())
  const verifyGate = useRef(new RequestGate())
  const submitIntent = useRef(new OperationIntent(() => newIdempotencyKey('quote-submit')))
  const verifyIntent = useRef(new OperationIntent(() => newIdempotencyKey('quote-verify')))

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
  const intent = invitation ? revisionIntent(invitation) : null

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

  // --- technical verification (list + detail from GET /quotations) ---
  const [quotationRows, setQuotationRows] = useState<QuotationListItem[]>([])
  const [quotationRowsError, setQuotationRowsError] = useState<unknown>(null)
  const pendingQuotations = useMemo(() => verifiableQuotations(quotationRows), [quotationRows])
  const [verifyQuotationNumber, setVerifyQuotationNumber] = useState(params.get('quotation') ?? '')
  const [verifyQuotation, setVerifyQuotation] = useState<QuotationDetail | null>(null)
  const [loadingVerifyQuotation, setLoadingVerifyQuotation] = useState(false)
  const [verifyLineId, setVerifyLineId] = useState('')
  const [verifyCompliant, setVerifyCompliant] = useState(true)
  const [verifyEvidence, setVerifyEvidence] = useState('{}')
  const [verifyRemarks, setVerifyRemarks] = useState('')
  const [verifying, setVerifying] = useState(false)

  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')

  const currentQuotation = invitation ? currentQuotationFor(invitation, quotationRows) : null

  const grandTotal = useMemo(
    () => lines.reduce((sum, line) => sum + lineTotal(line), 0) - num(headerDiscountValue),
    [lines, headerDiscountValue],
  )

  /** Reads invitations; returns the fresh rows, or null when the read failed or was superseded. */
  const loadInvitations = useCallback(async (): Promise<RfqInvitationCandidate[] | null> => {
    if (!reads.invitations) return null
    const ticket = invitationGate.current.begin()
    setInvitationsError(null)
    setLoadingInvitations(true)
    try {
      const rows = sortInvitations(await listRfqInvitations())
      if (!invitationGate.current.isCurrent(ticket)) return null
      setInvitations(rows)
      return rows
    } catch (err) {
      if (invitationGate.current.isCurrent(ticket)) {
        // Read unavailable: no list, no selection, nothing to submit against.
        setInvitations([])
        setInvitationsError(err)
      }
      return null
    } finally {
      if (invitationGate.current.isCurrent(ticket)) setLoadingInvitations(false)
    }
  }, [reads.invitations])

  const loadQuotationRows = useCallback(async () => {
    if (!reads.quotations) return
    const ticket = pendingGate.current.begin()
    setQuotationRowsError(null)
    try {
      const page = await listQuotations({ page: 1, pageSize: 100 })
      if (pendingGate.current.isCurrent(ticket)) setQuotationRows(page.Items)
    } catch (err) {
      if (pendingGate.current.isCurrent(ticket)) {
        setQuotationRows([])
        setQuotationRowsError(err)
      }
    }
  }, [reads.quotations])

  const loadVerifyQuotation = useCallback(async (number: string) => {
    const trimmed = number.trim()
    const ticket = verifyGate.current.begin()
    setVerifyQuotation(null)
    setVerifyLineId('')
    verifyIntent.current.clear()
    if (!trimmed || !reads.quotations) {
      setLoadingVerifyQuotation(false)
      return
    }
    setLoadingVerifyQuotation(true)
    try {
      const detail = await getQuotation(trimmed)
      if (!verifyGate.current.isCurrent(ticket)) return
      setVerifyQuotation(detail)
      if (detail.Lines.length === 1) setVerifyLineId(detail.Lines[0].Id)
    } catch (err) {
      if (verifyGate.current.isCurrent(ticket)) setError(err)
    } finally {
      if (verifyGate.current.isCurrent(ticket)) setLoadingVerifyQuotation(false)
    }
  }, [reads.quotations])

  // Scope change (company / login): drop everything, then read again.
  useEffect(() => {
    invitationGate.current.reset()
    linesGate.current.reset()
    pendingGate.current.reset()
    verifyGate.current.reset()
    submitIntent.current.clear()
    verifyIntent.current.clear()
    setInvitations([])
    setInvitationId('')
    setLines([])
    setQuotationRows([])
    setVerifyQuotation(null)
    setVerifyLineId('')
    setError(null)
    setNotice('')
    if (!scope) return
    void loadInvitations()
    void loadQuotationRows()
    const initial = params.get('quotation')
    if (initial) void loadVerifyQuotation(initial)
  }, [scope, loadInvitations, loadQuotationRows, loadVerifyQuotation, params])

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

  // Choosing an invitation resets the draft and fills lines/currency from the server.
  // Keyed on the id only, so a refresh of the same invitation keeps typed rates.
  const selectedRfqNumber = invitation?.RfqNumber ?? ''
  useEffect(() => {
    const ticket = linesGate.current.begin()
    submitIntent.current.clear()
    const selected = invitations.find((row) => row.InvitationId === invitationId)
    if (!selected) {
      setLines([])
      return
    }
    setCurrencyCode(selected.CurrencyCode)
    setLines(draftLinesFor(selected, null))
    if (!reads.rfqDetail) return
    getRfq(selected.RfqNumber)
      .then((detail) => { if (linesGate.current.isCurrent(ticket)) setLines(draftLinesFor(selected, detail)) })
      .catch(() => { /* required dates are a convenience; the lines are already filled */ })
    // A refresh of the same invitation must not wipe the typed draft, so the
    // list itself is deliberately not a dependency.
  }, [invitationId, selectedRfqNumber, reads.rfqDetail]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    setTaxContext(null)
    setTaxContextError(null)
    if (!invitationId || !reads.invitations) {
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
  }, [invitationId, reads.invitations, taxContextTick, scope])

  const chooseVerifyQuotation = (number: string) => {
    setVerifyQuotationNumber(number)
    void loadVerifyQuotation(number)
  }

  const setLine = (index: number, patch: Partial<DraftQuoteLine>) => {
    setLines((prev) => prev.map((line, i) => (i === index ? { ...line, ...patch } : line)))
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (saving) return
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

    setSaving(true)
    try {
      // Re-read the invitation before writing; any change stops for an explicit review.
      const fresh = await loadInvitations()
      if (!fresh) {
        setError('The invitation could not be re-read just now, so nothing was recorded. Try again.')
        return
      }
      const drift = invitationDrift(invitation, fresh.find((row) => row.InvitationId === invitation.InvitationId))
      if (drift) {
        setError(drift)
        return
      }
      const reviewed = revisionIntent(invitation)

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
      const body = {
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
        PreviousQuotationVersion: reviewed.previousQuotationVersion,
        Lines: payloadLines,
        HeaderDiscountValue: num(headerDiscountValue),
      }
      // Same key only for an identical retry of this exact operation.
      const key = submitIntent.current.keyFor([scope, invitation.InvitationId, body])
      const result = await submitQuotation(invitation.InvitationId, { ...body, IdempotencyKey: key })
      submitIntent.current.clear()
      rememberDoc('quotation', result.Number)
      setNotice(`Quotation ${result.Number} recorded (status ${result.Status}, version ${result.Version}).`)
      // The invitation now carries a current quotation version: reread before any
      // further revision, and load the new quotation for verification.
      void loadInvitations()
      void loadQuotationRows()
      if (reads.quotations) {
        setVerifyQuotationNumber(result.Number)
        void loadVerifyQuotation(result.Number)
      }
    } catch (err) {
      setError(err)
      if (isStaleConflict(err)) {
        // Stale: reread, show the current state, and wait for the user. No replay.
        submitIntent.current.clear()
        void loadInvitations()
        void loadQuotationRows()
        setNotice('The invitation or its current quotation changed on the server. The latest state is now shown — review it and submit again.')
      }
    } finally {
      setSaving(false)
    }
  }

  const runVerification = async () => {
    if (verifying) return
    setError(null)
    setNotice('')
    if (!verifyQuotation || !verifyLineId) {
      setError('Choose the quotation and the line being verified.')
      return
    }
    const number = verifyQuotation.QuotationNumber
    setVerifying(true)
    try {
      const body = {
        VendorQuotationLineId: verifyLineId,
        IsCompliant: verifyCompliant,
        ComplianceEvidenceJson: verifyEvidence.trim() || '{}',
        Remarks: verifyRemarks.trim(),
        QuotationVersion: verifyQuotation.Version,
      }
      const key = verifyIntent.current.keyFor([scope, number, body])
      const result = await verifyQuotationTechnically(number, { ...body, IdempotencyKey: key })
      verifyIntent.current.clear()
      setNotice(`Technical verification recorded. ${result.Number} is now ${result.Status}.`)
      setVerifyRemarks('')
      // Reread after every line decision: new version, new status, no stale selection.
      void loadVerifyQuotation(number)
      void loadQuotationRows()
    } catch (err) {
      setError(err)
      if (isStaleConflict(err)) {
        verifyIntent.current.clear()
        setNotice('The quotation changed on the server. It has been reloaded — choose the line again and review before recording.')
        void loadVerifyQuotation(number)
        void loadQuotationRows()
      }
    } finally {
      setVerifying(false)
    }
  }

  const downloadAttachment = async () => {
    const number = verifyQuotation?.QuotationNumber ?? verifyQuotationNumber.trim()
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
              <br />
              <CopyId label="Invitation id" value={invitation.InvitationId} /> · invitation version {invitation.InvitationVersion}
            </p>
          )}
          {invitation && intent && (
            <div className="alert" style={{ marginBottom: 0 }}>
              {intent.kind === 'new' ? (
                <><strong>New quotation</strong> — no quotation is on file for {invitation.VendorCode} on {invitation.RfqNumber}.</>
              ) : (
                <>
                  <strong>Revision</strong> of the current quotation
                  {currentQuotation ? <> {currentQuotation.QuotationNumber} (revision {currentQuotation.RevisionNumber})</> : null}
                  {' '}— previous quotation version {intent.previousQuotationVersion} is taken from the server.
                  {currentQuotation && <div><CopyId label="Current quotation id" value={currentQuotation.Id} /></div>}
                </>
              )}
            </div>
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
                      <CopyId label="RFQ line id" value={line.rfqLineId} />
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
          <label className="field field-wide">
            <span className="field-label">Quotation *</span>
            <select
              className="input"
              value={verifyQuotationNumber}
              disabled={!canReadQuotation || loadingVerifyQuotation}
              onChange={(e) => chooseVerifyQuotation(e.target.value)}
            >
              <option value="">
                {quotationRowsError != null
                  ? 'Quotations could not be read'
                  : pendingQuotations.length === 0 ? 'No quotations waiting for technical verification' : 'Choose a quotation to verify'}
              </option>
              {verifyQuotationNumber && !pendingQuotations.some((row) => row.QuotationNumber === verifyQuotationNumber) && (
                <option value={verifyQuotationNumber}>{verifyQuotationNumber}</option>
              )}
              {pendingQuotations.map((row) => (
                <option key={row.Id} value={row.QuotationNumber}>{quotationOptionLabel(row)}</option>
              ))}
            </select>
          </label>
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
            {quotationRowsError != null && (
              <ErrorAlert error={quotationRowsError} onReload={() => void loadQuotationRows()} fallback="Quotations could not be read." />
            )}
            {verifyQuotation && <CopyId label="Quotation id" value={verifyQuotation.Id} />}
            {verifyLineId && <CopyId label="Quotation line id" value={verifyLineId} />}
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
