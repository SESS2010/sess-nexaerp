import { useMemo, useState } from 'react'
import {
  getRfq,
  newIdempotencyKey,
  quotationAttachmentUrl,
  rememberDoc,
  submitQuotation,
  verifyQuotationTechnically,
} from '../../api/purchase'
import { getStoredToken } from '../../api/client'
import type { QuotationLineRequest, RfqDetail, RfqLine } from '../../types/purchase'
import { QUOTATION_SUBMISSION_SOURCES, VENDOR_REGISTRATION_TYPES } from '../../types/purchase'
import { formatAmount } from './PurchaseRequisitionListPage'
import { ErrorAlert } from '../../components/ErrorAlert'

interface DraftQuoteLine {
  rfqLine: RfqLine
  quantity: string
  unitRate: string
  discountValue: string
  packingForwarding: string
  freight: string
  insurance: string
  otherCharges: string
  roundOff: string
  promisedDeliveryDate: string
  hsnSacCode: string
}

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
  // --- source RFQ + invitation ---
  const [rfqNumber, setRfqNumber] = useState('')
  const [invitationId, setInvitationId] = useState('')
  const [invitationVersion, setInvitationVersion] = useState('0')
  const [rfq, setRfq] = useState<RfqDetail | null>(null)
  const [lines, setLines] = useState<DraftQuoteLine[]>([])
  const [loadingRfq, setLoadingRfq] = useState(false)

  // --- quotation header ---
  const [vendorQuoteReference, setVendorQuoteReference] = useState('')
  const [currencyCode, setCurrencyCode] = useState('INR')
  const [paymentTerms, setPaymentTerms] = useState('')
  const [deliveryTerms, setDeliveryTerms] = useState('')
  const [warrantyTerms, setWarrantyTerms] = useState('')
  const [submissionSource, setSubmissionSource] = useState<string>(QUOTATION_SUBMISSION_SOURCES[0])
  const [receivedAt, setReceivedAt] = useState(todayLocal())
  const [attachmentObjectKey, setAttachmentObjectKey] = useState('')
  const [attachmentSha256, setAttachmentSha256] = useState('')
  const [vendorAttestation, setVendorAttestation] = useState('')
  const [supplierStateCode, setSupplierStateCode] = useState('33')
  const [placeOfSupplyStateCode, setPlaceOfSupplyStateCode] = useState('33')
  const [vendorRegistrationType, setVendorRegistrationType] = useState<string>(VENDOR_REGISTRATION_TYPES[0])
  const [headerDiscountValue, setHeaderDiscountValue] = useState('0')
  const [requestLateAuthorization, setRequestLateAuthorization] = useState(false)
  const [lateAuthorizationRemarks, setLateAuthorizationRemarks] = useState('')
  const [previousQuotationVersion, setPreviousQuotationVersion] = useState('')

  // --- technical verification ---
  const [verifyQuotationNumber, setVerifyQuotationNumber] = useState('')
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

  const loadRfq = async () => {
    setError(null)
    setNotice('')
    setLoadingRfq(true)
    try {
      const detail = await getRfq(rfqNumber)
      setRfq(detail)
      setCurrencyCode(detail.CurrencyCode)
      setLines(
        (detail.Lines ?? []).map((rfqLine) => ({
          rfqLine,
          quantity: String(rfqLine.RfqQuantity),
          unitRate: '0',
          discountValue: '0',
          packingForwarding: '0',
          freight: '0',
          insurance: '0',
          otherCharges: '0',
          roundOff: '0',
          promisedDeliveryDate: rfqLine.RequiredDateSnapshot,
          hsnSacCode: '',
        })),
      )
    } catch (err) {
      setRfq(null)
      setLines([])
      setError(err)
    } finally {
      setLoadingRfq(false)
    }
  }

  const setLine = (index: number, patch: Partial<DraftQuoteLine>) => {
    setLines((prev) => prev.map((line, i) => (i === index ? { ...line, ...patch } : line)))
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    setNotice('')

    if (!invitationId.trim()) {
      setError('Invitation id is required — it comes from the invite-vendor response on the RFQ page.')
      return
    }
    if (lines.length === 0) {
      setError('Load an RFQ first so the quotation has lines.')
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
      RequestForQuotationLineId: line.rfqLine.Id,
      Quantity: num(line.quantity),
      UnitRate: num(line.unitRate),
      DiscountValue: num(line.discountValue),
      PackingForwarding: num(line.packingForwarding),
      Freight: num(line.freight),
      Insurance: num(line.insurance),
      OtherCharges: num(line.otherCharges),
      PromisedDeliveryDate: line.promisedDeliveryDate,
      HsnSacCode: line.hsnSacCode.trim(),
      SupplierStateCode: supplierStateCode.trim(),
      PlaceOfSupplyStateCode: placeOfSupplyStateCode.trim(),
      VendorRegistrationType: vendorRegistrationType,
      RoundOff: num(line.roundOff),
    }))

    setSaving(true)
    try {
      const result = await submitQuotation(invitationId.trim(), {
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
        InvitationVersion: num(invitationVersion),
        PreviousQuotationVersion: previousQuotationVersion ? num(previousQuotationVersion) : null,
        IdempotencyKey: newIdempotencyKey('quote-submit'),
        Lines: payloadLines,
        HeaderDiscountValue: num(headerDiscountValue),
      })
      rememberDoc('quotation', result.Number)
      setVerifyQuotationNumber(result.Number)
      setVerifyVersion(String(result.Version))
      setNotice(`Quotation ${result.Number} recorded (status ${result.Status}, version ${result.Version}).`)
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  const runVerification = async () => {
    setError(null)
    setNotice('')
    if (!verifyQuotationNumber.trim() || !verifyLineId.trim()) {
      setError('Technical verification needs both a quotation number and a quotation line id.')
      return
    }
    setVerifying(true)
    try {
      const result = await verifyQuotationTechnically(verifyQuotationNumber.trim(), {
        VendorQuotationLineId: verifyLineId.trim(),
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
      const token = getStoredToken()
      const response = await fetch(quotationAttachmentUrl(number), {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      })
      if (!response.ok) throw new Error(`Download failed (${response.status})`)
      const blob = await response.blob()
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = `${number}-quotation`
      anchor.click()
      URL.revokeObjectURL(url)
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

      <div className="alert">
        <strong>Quotations cannot be listed or re-read.</strong> The API offers only{' '}
        <span className="mono">POST /rfq-invitations/{'{id}'}/quotations</span>,{' '}
        <span className="mono">POST /quotations/{'{number}'}/technical-verifications</span> and an
        attachment download. There is no <span className="mono">GET /quotations/{'{number}'}</span>,
        so a recorded quotation cannot be reopened here.
      </div>

      <ErrorAlert error={error} fallback="The last action failed." />
      {notice && <div className="alert">{notice}</div>}

      <div className="card">
        <div className="form-section-title">1 · Load the RFQ being quoted</div>
        <div className="form-grid">
          <label className="field">
            <span className="field-label">RFQ number *</span>
            <input
              className="input mono"
              placeholder="RFQ-2627-00001"
              value={rfqNumber}
              onChange={(event) => setRfqNumber(event.target.value)}
            />
          </label>
          <div className="field">
            <span className="field-label">&nbsp;</span>
            <button type="button" className="btn btn-ghost" disabled={loadingRfq} onClick={() => void loadRfq()}>
              {loadingRfq ? 'Loading…' : 'Load RFQ lines'}
            </button>
          </div>
          <label className="field">
            <span className="field-label">Invitation id (GUID) *</span>
            <input
              className="input mono"
              placeholder="from the RFQ page invite response"
              value={invitationId}
              onChange={(event) => setInvitationId(event.target.value)}
            />
            <span className="field-hint">
              Shown once on the RFQ detail page after inviting a vendor; the API cannot list it back.
            </span>
          </label>
          <label className="field">
            <span className="field-label">Invitation version *</span>
            <input
              className="input mono"
              value={invitationVersion}
              onChange={(event) => setInvitationVersion(event.target.value)}
            />
          </label>
        </div>
        {rfq && (
          <p className="field-hint">
            Loaded {rfq.RfqNumber} — {rfq.Lines?.length ?? 0} line(s), currency {rfq.CurrencyCode},
            quotes due {new Date(rfq.QuoteDueAt).toLocaleString('en-IN')}.
          </p>
        )}
      </div>

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
                {QUOTATION_SUBMISSION_SOURCES.map((option) => <option key={option} value={option}>{option}</option>)}
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
              <input className="input mono" value={supplierStateCode} onChange={(e) => setSupplierStateCode(e.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Place of supply state code *</span>
              <input className="input mono" value={placeOfSupplyStateCode} onChange={(e) => setPlaceOfSupplyStateCode(e.target.value)} />
              <span className="field-hint">Same code as supplier means CGST + SGST; different means IGST.</span>
            </label>

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
            <label className="field">
              <span className="field-label">Previous quotation version</span>
              <input
                className="input mono"
                placeholder="blank for first submission"
                value={previousQuotationVersion}
                onChange={(e) => setPreviousQuotationVersion(e.target.value)}
              />
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
                  <tr><td colSpan={12} className="table-empty">Load an RFQ above to pull its lines.</td></tr>
                )}
                {lines.map((line, index) => (
                  <tr key={line.rfqLine.Id}>
                    <td className="mono">
                      {line.rfqLine.ItemCodeSnapshot}
                      <div className="field-hint">{line.rfqLine.ItemNameSnapshot}</div>
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
            <button type="submit" className="btn btn-primary" disabled={saving || lines.length === 0}>
              {saving ? 'Recording…' : 'Record quotation'}
            </button>
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
            <input className="input mono" value={verifyQuotationNumber} onChange={(e) => setVerifyQuotationNumber(e.target.value)} />
          </label>
          <label className="field">
            <span className="field-label">Quotation version *</span>
            <input className="input mono" value={verifyVersion} onChange={(e) => setVerifyVersion(e.target.value)} />
          </label>
          <label className="field field-wide">
            <span className="field-label">Vendor quotation line id (GUID) *</span>
            <input className="input mono" value={verifyLineId} onChange={(e) => setVerifyLineId(e.target.value)} />
            <span className="field-hint">
              No endpoint returns quotation line ids. Until one exists this must be read from a
              comparison line or straight from the database.
            </span>
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
            <button type="button" className="btn btn-primary" disabled={verifying} onClick={() => void runVerification()}>
              {verifying ? 'Recording…' : 'Record verification'}
            </button>
            <button type="button" className="btn btn-ghost" onClick={() => void downloadAttachment()}>
              Download quotation attachment
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
