import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createComparison, getRfq, newIdempotencyKey, rememberDoc } from '../../api/purchase'
import { DocumentRegister } from './DocumentRegister'

export function ComparisonListPage() {
  const navigate = useNavigate()
  const [showCreate, setShowCreate] = useState(false)
  const [rfqNumber, setRfqNumber] = useState('')
  const [rfqVersion, setRfqVersion] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const loadRfqVersion = async () => {
    setError('')
    try {
      const rfq = await getRfq(rfqNumber)
      setRfqVersion(String(rfq.Version))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to read the RFQ version.')
    }
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError('')
    if (!rfqNumber.trim()) {
      setError('RFQ number is required.')
      return
    }
    setBusy(true)
    try {
      const result = await createComparison({
        RfqNumber: rfqNumber.trim().toUpperCase(),
        RfqVersion: Number(rfqVersion) || 0,
        IdempotencyKey: newIdempotencyKey('comparison-create'),
      })
      rememberDoc('comparison', result.Number)
      navigate(`/purchase/comparisons/${encodeURIComponent(result.Number)}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create the comparison.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <DocumentRegister
      kind="comparison"
      title="Commercial Comparison"
      subtitle="Step 4 of the purchase flow — quotations are compared and a vendor is recommended for approval"
      missingEndpoint="GET /api/v1/purchase/comparisons"
      placeholder="Open comparison by number, e.g. CMP-2627-00001"
      routePrefix="/purchase/comparisons"
      createLabel="+ New Comparison"
      onCreate={() => setShowCreate(true)}
    >
      {showCreate && (
        <div className="modal-backdrop">
          <div className="modal" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h2>New Comparison</h2>
              <button type="button" className="btn btn-ghost" onClick={() => setShowCreate(false)}>✕</button>
            </div>
            <form onSubmit={submit} className="form-grid">
              <p className="field-wide field-hint">
                A comparison is generated from every quotation received against one RFQ. The API
                builds the lines; nothing is chosen here.
              </p>
              <label className="field">
                <span className="field-label">RFQ number *</span>
                <input
                  className="input mono"
                  placeholder="RFQ-2627-00001"
                  value={rfqNumber}
                  onChange={(event) => setRfqNumber(event.target.value)}
                />
              </label>
              <label className="field">
                <span className="field-label">RFQ version *</span>
                <input
                  className="input mono"
                  value={rfqVersion}
                  onChange={(event) => setRfqVersion(event.target.value)}
                />
                <button type="button" className="link-button" onClick={() => void loadRfqVersion()}>
                  Read current version from the RFQ
                </button>
              </label>
              {error && <div className="field-wide alert alert-error">{error}</div>}
              <div className="field-wide modal-actions">
                <button type="button" className="btn btn-ghost" onClick={() => setShowCreate(false)} disabled={busy}>
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary" disabled={busy}>
                  {busy ? 'Creating…' : 'Create comparison'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </DocumentRegister>
  )
}
