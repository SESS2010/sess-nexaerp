import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError } from '../../api/client'
import { verifyVendorCommercial } from '../../api/vendors'
import type { VendorDetail } from '../../types/vendor'
import { ErrorAlert } from '../../components/ErrorAlert'

// Accounts commercial verification, per
// docs/installation/vendor-commercial-verification-contract.md.
// POST /verify-commercial is not idempotent, so a lost or 5xx response is never
// retried from here: the user reloads the vendor and reads the approval history.
export function VerifyCommercialModal({ vendorCode, vendorName, version, onClose, onReload, onVerified }: {
  vendorCode: string
  vendorName: string
  version: number
  onClose: () => void
  onReload: () => void
  onVerified: (updated: VendorDetail) => void
}) {
  const [remarks, setRemarks] = useState('')
  const [confirmed, setConfirmed] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [refused, setRefused] = useState(false)
  const [uncertain, setUncertain] = useState(false)
  const inFlight = useRef(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (inFlight.current || uncertain || refused) return
    const trimmed = remarks.trim()
    if (!trimmed) { setError(new Error('Remarks are required: say which commercial, GST and bank evidence you checked.')); return }
    if (!confirmed) { setError(new Error('Confirm that you reviewed the evidence before verifying.')); return }
    inFlight.current = true
    setSaving(true)
    setError(null)
    try {
      onVerified(await verifyVendorCommercial(vendorCode, trimmed, version))
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setRefused(true)
      } else if (!(err instanceof ApiError) || err.status >= 500) {
        // Timeout, network drop or server error: the verification may or may not
        // have been saved. Read back before any new decision.
        setUncertain(true)
      } else {
        setError(err)
      }
    } finally {
      inFlight.current = false
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>Verify commercial details</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>✕</button>
        </div>
        <form onSubmit={submit} className="form-grid">
          <p className="field-wide">
            <span className="mono">{vendorCode}</span> · {vendorName} · v{version}
            <span className="field-hint">This records the Accounts check only. The vendor stays Pending Approval until the MD's final approval.</span>
          </p>
          <label className="field field-wide">
            <span className="field-label">Remarks *</span>
            <textarea className="input" rows={3} value={remarks} onChange={(event) => setRemarks(event.target.value)} placeholder="Commercial, GST and bank evidence checked; review reference …" disabled={saving || uncertain || refused} />
          </label>
          <label className="field field-wide">
            <span><input type="checkbox" checked={confirmed} onChange={(event) => setConfirmed(event.target.checked)} disabled={saving || uncertain || refused} /> I have reviewed the GST, PAN, bank and commercial evidence for this vendor.</span>
          </label>
          {refused && (
            <div className="alert alert-error field-wide">
              <div className="alert-title">Your login is not allowed to verify this vendor.</div>
              <div className="alert-body">Commercial verification needs the Accounts Manager role, signed in through the Approvers login with MFA, in the vendor's company. Do not retry under another person's login.</div>
            </div>
          )}
          {uncertain && (
            <div className="alert alert-warn field-wide">
              <div className="alert-title">The result is not known.</div>
              <div className="alert-body">The server did not confirm the verification. It may or may not have been saved. Reload the vendor and check the approval history before deciding again.</div>
              <button type="button" className="btn" onClick={onReload}>Reload vendor</button>
            </div>
          )}
          <div className="field-wide"><ErrorAlert error={error} onReload={onReload} fallback="The verification was not recorded." /></div>
          <div className="field-wide modal-actions">
            <button type="button" className="btn" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving || uncertain || refused}>{saving ? 'Verifying…' : 'Verify'}</button>
          </div>
        </form>
      </div>
    </div>
  )
}
