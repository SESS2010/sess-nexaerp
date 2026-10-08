import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  getQuotationTaxContext,
  newIdempotencyKey,
  quotationAttachmentUrl,
  rememberDoc,
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
  invitationLabel,
  quotationLineLabel,
  quotationOptionLabel,
  quotationScreenReads,
  revisionIntent,
  sessionScopeKey,
  verifiableQuotations,
} from './quotationDraft'
import type { DraftQuoteLine } from './quotationDraft'
import {
  emptyQuotationHeader,
  emptyVerificationForm,
  mergeRequiredDates,
  readInvitations,
  readQuotationDetail,
  readQuotationRows,
  submitQuotationFlow,
  verifyQuotationFlow,
} from './quotationFlows'
import type { QuotationHeaderForm, VerificationForm } from './quotationFlows'
import { quotationFlowApi } from './quotationFlowApi'
import { QuotationScreenController, ScreenLifecycle, TARGET } from './screenLifecycle'

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

function blankHeader(): QuotationHeaderForm {
  return emptyQuotationHeader({
    submissionSource: QUOTATION_SUBMISSION_SOURCES[0].value,
    vendorRegistrationType: VENDOR_REGISTRATION_TYPES[0],
    receivedAt: todayLocal(),
  })
}

export function QuotationPage() {
  const { me, can } = useSession()
  const [params, setParams] = useSearchParams()
  const urlQuotation = params.get('quotation') ?? ''
  const urlRfq = params.get('rfq') ?? ''
  const urlVendor = params.get('vendor') ?? ''

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
  const reads = useMemo(
    () => quotationScreenReads({ recordQuotation: canRecordQuotation, readQuotation: canReadQuotation, readRfq: canReadRfq }),
    [canRecordQuotation, canReadQuotation, canReadRfq],
  )

  // Company / login scope: a change clears every list, form value, selection,
  // pending key and busy flag, and makes every in-flight read or write moot.
  // Unmount (navigation, logout) and a change of invitation / quotation / line
  // do the same for the actions bound to them (screenLifecycle.ts).
  const scope = sessionScopeKey(me)
  const lastScope = useRef('')
  const invitationGate = useRef(new RequestGate())
  const linesGate = useRef(new RequestGate())
  const pendingGate = useRef(new RequestGate())
  const verifyGate = useRef(new RequestGate())
  const [screen] = useState(() => new QuotationScreenController(
    new ScreenLifecycle().track(invitationGate.current, linesGate.current, pendingGate.current, verifyGate.current),
  ))
  const submitIntent = useRef(new OperationIntent(() => newIdempotencyKey('quote-submit')))
  const verifyIntent = useRef(new OperationIntent(() => newIdempotencyKey('quote-verify')))

  useEffect(() => {
    screen.lifecycle.mount()
    return () => screen.lifecycle.unmount()
  }, [screen])

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

  // --- quotation header + evidence (one object so a scope change clears all of it) ---
  const [header, setHeader] = useState<QuotationHeaderForm>(blankHeader)
  const setHeaderField = <K extends keyof QuotationHeaderForm>(key: K, value: QuotationHeaderForm[K]) =>
    setHeader((prev) => ({ ...prev, [key]: value }))
  // R2: both GST state codes come from the server for the selected invitation
  // (vendor GSTIN / state, delivery warehouse / company profile). They are shown
  // read-only and sent back exactly as derived; submit is refused until they load.
  const [taxContext, setTaxContext] = useState<QuotationTaxContext | null>(null)
  const [taxContextError, setTaxContextError] = useState<unknown>(null)
  const [loadingTaxContext, setLoadingTaxContext] = useState(false)
  const [taxContextTick, setTaxContextTick] = useState(0)

  // --- technical verification (list + detail from GET /quotations) ---
  const [quotationRows, setQuotationRows] = useState<QuotationListItem[]>([])
  const [quotationRowsComplete, setQuotationRowsComplete] = useState(true)
  const [quotationRowsTotal, setQuotationRowsTotal] = useState(0)
  const [quotationRowsError, setQuotationRowsError] = useState<unknown>(null)
  const pendingQuotations = useMemo(() => verifiableQuotations(quotationRows), [quotationRows])
  const [verifyForm, setVerifyForm] = useState<VerificationForm>(emptyVerificationForm)
  const [verifyQuotation, setVerifyQuotation] = useState<QuotationDetail | null>(null)
  const [loadingVerifyQuotation, setLoadingVerifyQuotation] = useState(false)
  const [verifying, setVerifying] = useState(false)

  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')

  const currentQuotation = invitation ? currentQuotationFor(invitation, quotationRows) : null

  const grandTotal = useMemo(
    () => lines.reduce((sum, line) => sum + lineTotal(line), 0) - num(header.headerDiscountValue),
    [lines, header.headerDiscountValue],
  )

  /** Reads invitations; returns the fresh rows, or null when the read failed or was superseded. */
  const loadInvitations = useCallback(async (): Promise<RfqInvitationCandidate[] | null> => {
    if (!reads.invitations) return null
    const ticket = invitationGate.current.begin()
    setInvitationsError(null)
    setLoadingInvitations(true)
    try {
      const rows = await readInvitations(quotationFlowApi, reads)
      if (!invitationGate.current.isCurrent(ticket) || !rows) return null
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
  }, [reads])

  const loadQuotationRows = useCallback(async () => {
    if (!reads.quotations) return
    const ticket = pendingGate.current.begin()
    setQuotationRowsError(null)
    try {
      // Every page at the server's page size; Submitted is filtered here (Defect #3).
      const list = await readQuotationRows(quotationFlowApi, reads)
      if (!pendingGate.current.isCurrent(ticket) || !list) return
      setQuotationRows(list.items)
      setQuotationRowsComplete(list.complete)
      setQuotationRowsTotal(list.total)
    } catch (err) {
      if (pendingGate.current.isCurrent(ticket)) {
        setQuotationRows([])
        setQuotationRowsError(err)
      }
    }
  }, [reads])

  const loadVerifyQuotation = useCallback(async (number: string) => {
    const ticket = verifyGate.current.begin()
    setVerifyQuotation(null)
    setVerifyForm((prev) => ({ ...prev, lineId: '' }))
    verifyIntent.current.clear()
    if (!number.trim() || !reads.quotations) {
      setLoadingVerifyQuotation(false)
      return
    }
    setLoadingVerifyQuotation(true)
    try {
      const detail = await readQuotationDetail(quotationFlowApi, reads, number)
      if (!verifyGate.current.isCurrent(ticket) || !detail) return
      setVerifyQuotation(detail)
      if (detail.Lines.length === 1) setVerifyForm((prev) => ({ ...prev, lineId: detail.Lines[0].Id }))
    } catch (err) {
      if (verifyGate.current.isCurrent(ticket)) setError(err)
    } finally {
      if (verifyGate.current.isCurrent(ticket)) setLoadingVerifyQuotation(false)
    }
  }, [reads])

  // Scope change (company / login) or a new link: drop everything, then read again.
  useEffect(() => {
    const previous = lastScope.current
    lastScope.current = scope
    // Drops in-flight actions and reads (all four gates) and the evidence owner.
    screen.changeScope()
    submitIntent.current.clear()
    verifyIntent.current.clear()
    setInvitations([])
    setInvitationsError(null)
    setLoadingInvitations(false)
    setInvitationId('')
    setLines([])
    setHeader(blankHeader())
    setQuotationRows([])
    setQuotationRowsComplete(true)
    setQuotationRowsTotal(0)
    setQuotationRowsError(null)
    setVerifyQuotation(null)
    setLoadingVerifyQuotation(false)
    setVerifyForm(emptyVerificationForm())
    setSaving(false)
    setVerifying(false)
    setError(null)
    setNotice('')
    if (!scope) return
    if (previous && previous !== scope && (urlQuotation || urlRfq || urlVendor)) {
      // A link opened for the previous company / login is not carried over.
      // Clearing it re-runs this effect, which then reads afresh.
      setParams(new URLSearchParams(), { replace: true })
      return
    }
    void loadInvitations()
    void loadQuotationRows()
    if (urlQuotation) {
      setVerifyForm({ ...emptyVerificationForm(), quotationNumber: urlQuotation })
      void loadVerifyQuotation(urlQuotation)
    }
  }, [scope, urlQuotation, urlRfq, urlVendor, setParams, loadInvitations, loadQuotationRows, loadVerifyQuotation, screen])

  /**
   * Choosing an invitation. A different invitation (vendor) resets every
   * vendor-bound header/evidence value and the notices; re-choosing the same
   * one keeps the typed draft.
   */
  const chooseInvitation = (id: string) => {
    if (screen.pickInvitation(id)) {
      setHeader(blankHeader())
      setNotice('')
      setError(null)
      submitIntent.current.clear()
    }
    setInvitationId(id)
  }

  // Catch-all for selections not made through chooseInvitation (scope reset);
  // pickInvitation is idempotent for the same id.
  useEffect(() => {
    if (screen.pickInvitation(invitationId)) {
      setHeader(blankHeader())
      setNotice('')
      submitIntent.current.clear()
    }
  }, [invitationId, screen])

  // The verification target is the quotation and the line; changing either
  // drops an in-flight verification and its result reload.
  useEffect(() => {
    screen.pickVerification(verifyForm.quotationNumber, verifyForm.lineId)
  }, [verifyForm.quotationNumber, verifyForm.lineId, screen])

  // ?rfq=RFQ-… preselects that RFQ's only invitation; ?vendor=CODE narrows to one vendor.
  useEffect(() => {
    if (invitationId || invitations.length === 0) return
    const rfqParam = urlRfq.trim().toUpperCase()
    const vendorParam = urlVendor.trim().toUpperCase()
    if (!rfqParam) return
    const matches = invitations.filter((row) =>
      row.RfqNumber.toUpperCase() === rfqParam && (!vendorParam || row.VendorCode.toUpperCase() === vendorParam))
    if (matches.length === 1) chooseInvitation(matches[0].InvitationId)
  }, [invitations, invitationId, urlRfq, urlVendor]) // eslint-disable-line react-hooks/exhaustive-deps

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
    setHeaderField('currencyCode', selected.CurrencyCode)
    setLines(draftLinesFor(selected, null))
    if (!reads.rfqDetail) return
    quotationFlowApi.getRfq(selected.RfqNumber)
      // A late answer only fills required dates still blank; typed values stay.
      .then((detail) => { if (linesGate.current.isCurrent(ticket)) setLines((current) => mergeRequiredDates(current, detail)) })
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
    // A different quotation drops an in-flight verification of the previous one.
    screen.pickVerification(number, '')
    // Each quotation starts with a blank result, evidence and remarks.
    setVerifyForm({ ...emptyVerificationForm(), quotationNumber: number })
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
    if (header.requestLateAuthorization && !header.lateAuthorizationRemarks.trim()) {
      setError('Late-submission authorization needs a written reason.')
      return
    }

    // Live until unmount, a company/login change, or another invitation is chosen.
    const isLive = screen.submitIsLive()
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
      VendorRegistrationType: header.vendorRegistrationType,
      RoundOff: num(line.roundOff),
    }))
    const body = {
      VendorQuoteReference: header.vendorQuoteReference.trim(),
      CurrencyCode: header.currencyCode.trim().toUpperCase(),
      PaymentTerms: header.paymentTerms.trim(),
      DeliveryTerms: header.deliveryTerms.trim(),
      WarrantyTerms: header.warrantyTerms.trim(),
      RequestLateAuthorization: header.requestLateAuthorization,
      LateAuthorizationRemarks: header.requestLateAuthorization ? header.lateAuthorizationRemarks.trim() : null,
      SubmissionSource: header.submissionSource,
      ReceivedAt: new Date(header.receivedAt).toISOString(),
      AttachmentObjectKey: header.attachmentObjectKey.trim(),
      AttachmentSha256: header.attachmentSha256.trim(),
      VendorAttestation: header.vendorAttestation.trim(),
      InvitationVersion: invitation.InvitationVersion,
      PreviousQuotationVersion: reviewed.previousQuotationVersion,
      Lines: payloadLines,
      HeaderDiscountValue: num(header.headerDiscountValue),
    }

    setSaving(true)
    try {
      // Re-reads the invitation first; any change stops for an explicit review.
      const outcome = await submitQuotationFlow({
        api: quotationFlowApi,
        reviewed: invitation,
        body,
        intent: submitIntent.current,
        scope,
        isLive,
      })
      if (outcome.kind === 'abandoned') return
      if (outcome.fresh) setInvitations(outcome.fresh)
      switch (outcome.kind) {
        case 'reread-failed':
          setError('The invitation could not be re-read just now, so nothing was recorded. Try again.')
          break
        case 'drift':
          setError(outcome.message)
          break
        case 'done': {
          const result = outcome.result
          rememberDoc('quotation', result.Number)
          setNotice(`Quotation ${result.Number} recorded (status ${result.Status}, version ${result.Version}).`)
          // The invitation now carries a current quotation version: reread before any
          // further revision, and load the new quotation for verification.
          void loadInvitations()
          void loadQuotationRows()
          if (reads.quotations) chooseVerifyQuotation(result.Number)
          break
        }
        case 'failed':
          setError(outcome.error)
          if (outcome.stale) {
            // Stale: reread, show the current state, and wait for the user. No replay.
            void loadInvitations()
            void loadQuotationRows()
            setNotice('The invitation or its current quotation changed on the server. The latest state is now shown — review it and submit again.')
          }
          break
      }
    } finally {
      if (isLive()) setSaving(false)
    }
  }

  const runVerification = async () => {
    if (verifying) return
    setError(null)
    setNotice('')
    if (!verifyQuotation || !verifyForm.lineId) {
      setError('Choose the quotation and the line being verified.')
      return
    }
    // Live until unmount, a company/login change, or another quotation or line is chosen.
    screen.pickVerification(verifyForm.quotationNumber, verifyForm.lineId)
    const isLive = screen.verifyIsLive()
    const number = verifyQuotation.QuotationNumber
    setVerifying(true)
    try {
      const outcome = await verifyQuotationFlow({
        api: quotationFlowApi,
        quotation: verifyQuotation,
        form: verifyForm,
        intent: verifyIntent.current,
        scope,
        isLive,
      })
      if (outcome.kind === 'abandoned') return
      if (outcome.kind === 'done') {
        setNotice(`Technical verification recorded. ${outcome.result.Number} is now ${outcome.result.Status}.`)
        setVerifyForm((prev) => ({ ...prev, remarks: '' }))
        // Reread after every line decision: new version, new status, no stale selection.
        void loadVerifyQuotation(number)
        void loadQuotationRows()
        return
      }
      setError(outcome.error)
      if (outcome.stale) {
        setNotice('The quotation changed on the server. It has been reloaded — choose the line again and review before recording.')
        void loadVerifyQuotation(number)
        void loadQuotationRows()
      }
    } finally {
      if (isLive()) setVerifying(false)
    }
  }

  const downloadAttachment = async () => {
    const number = verifyQuotation?.QuotationNumber ?? verifyForm.quotationNumber.trim()
    if (!number) return
    const isLive = screen.lifecycle.begin(TARGET.verification)
    try {
      const response = await authorizedFetch(quotationAttachmentUrl(number))
      if (!isLive()) return
      await saveResponseAsFile(response, `${number}-quotation`)
    } catch (err) {
      if (isLive()) setError(err)
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
                onChange={(event) => chooseInvitation(event.target.value)}
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
              <input className="input" value={header.vendorQuoteReference} onChange={(e) => setHeaderField('vendorQuoteReference', e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Currency *</span>
              <input className="input mono" value={header.currencyCode} onChange={(e) => setHeaderField('currencyCode', e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Received at *</span>
              <input type="datetime-local" className="input" value={header.receivedAt} onChange={(e) => setHeaderField('receivedAt', e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Submission source *</span>
              <select className="input" value={header.submissionSource} onChange={(e) => setHeaderField('submissionSource', e.target.value)}>
                {QUOTATION_SUBMISSION_SOURCES.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
              </select>
            </label>
            <label className="field">
              <span className="field-label">Payment terms *</span>
              <input className="input" value={header.paymentTerms} onChange={(e) => setHeaderField('paymentTerms', e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Delivery terms *</span>
              <input className="input" value={header.deliveryTerms} onChange={(e) => setHeaderField('deliveryTerms', e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Warranty terms *</span>
              <input className="input" value={header.warrantyTerms} onChange={(e) => setHeaderField('warrantyTerms', e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Header discount</span>
              <input className="input text-right mono" value={header.headerDiscountValue} onChange={(e) => setHeaderField('headerDiscountValue', e.target.value)} />
            </label>

            <div className="field-wide form-section-title">Tax identity</div>
            <label className="field">
              <span className="field-label">Vendor registration type *</span>
              <select className="input" value={header.vendorRegistrationType} onChange={(e) => setHeaderField('vendorRegistrationType', e.target.value)}>
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
              <input className="input mono" value={header.attachmentObjectKey} onChange={(e) => setHeaderField('attachmentObjectKey', e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Attachment SHA-256 *</span>
              <input className="input mono" value={header.attachmentSha256} onChange={(e) => setHeaderField('attachmentSha256', e.target.value)} />
              <span className="field-hint">
                There is no upload endpoint for quotation files — the key and hash must be supplied.
              </span>
            </label>
            <label className="field field-wide">
              <span className="field-label">Vendor attestation *</span>
              <textarea className="input" rows={2} value={header.vendorAttestation} onChange={(e) => setHeaderField('vendorAttestation', e.target.value)} />
            </label>

            <label className="field">
              <span className="field-label">Late submission</span>
              <select
                className="input"
                value={header.requestLateAuthorization ? 'yes' : 'no'}
                onChange={(e) => setHeaderField('requestLateAuthorization', e.target.value === 'yes')}
              >
                <option value="no">On time</option>
                <option value="yes">Late — request authorization</option>
              </select>
            </label>
            {header.requestLateAuthorization && (
              <label className="field field-wide">
                <span className="field-label">Late authorization remarks *</span>
                <textarea className="input" rows={2} value={header.lateAuthorizationRemarks} onChange={(e) => setHeaderField('lateAuthorizationRemarks', e.target.value)} />
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
              value={verifyForm.quotationNumber}
              disabled={!canReadQuotation || loadingVerifyQuotation}
              onChange={(e) => chooseVerifyQuotation(e.target.value)}
            >
              <option value="">
                {quotationRowsError != null
                  ? 'Quotations could not be read'
                  : pendingQuotations.length === 0 ? 'No quotations waiting for technical verification' : 'Choose a quotation to verify'}
              </option>
              {verifyForm.quotationNumber && !pendingQuotations.some((row) => row.QuotationNumber === verifyForm.quotationNumber) && (
                <option value={verifyForm.quotationNumber}>{verifyForm.quotationNumber}</option>
              )}
              {pendingQuotations.map((row) => (
                <option key={row.Id} value={row.QuotationNumber}>{quotationOptionLabel(row)}</option>
              ))}
            </select>
            {!quotationRowsComplete && (
              <span className="field-hint">
                Only {quotationRows.length} of {quotationRowsTotal} quotations could be read, so this list may be incomplete.
              </span>
            )}
          </label>
          <label className="field field-wide">
            <span className="field-label">Quotation line *</span>
            <select
              className="input"
              value={verifyForm.lineId}
              disabled={!verifyQuotation}
              onChange={(e) => {
                screen.pickVerification(verifyForm.quotationNumber, e.target.value)
                setVerifyForm((prev) => ({ ...prev, lineId: e.target.value }))
              }}
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
            {verifyForm.lineId && <CopyId label="Quotation line id" value={verifyForm.lineId} />}
            {!canReadQuotation && (
              <span className="field-hint">
                Choosing a line needs view access to vendor quotations (purchase.vendor-quotations:view).
              </span>
            )}
          </label>
          <label className="field">
            <span className="field-label">Result *</span>
            <select
              className="input"
              value={verifyForm.compliant ? 'yes' : 'no'}
              onChange={(e) => setVerifyForm((prev) => ({ ...prev, compliant: e.target.value === 'yes' }))}
            >
              <option value="yes">Technically compliant</option>
              <option value="no">Not compliant</option>
            </select>
          </label>
          <label className="field">
            <span className="field-label">Compliance evidence (JSON)</span>
            <input
              className="input mono"
              value={verifyForm.evidence}
              onChange={(e) => setVerifyForm((prev) => ({ ...prev, evidence: e.target.value }))}
            />
          </label>
          <label className="field field-wide">
            <span className="field-label">Remarks</span>
            <textarea
              className="input"
              rows={2}
              value={verifyForm.remarks}
              onChange={(e) => setVerifyForm((prev) => ({ ...prev, remarks: e.target.value }))}
            />
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
