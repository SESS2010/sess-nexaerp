import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createPurchaseOrder, getComparison, newIdempotencyKey, rememberDoc } from '../../api/purchase'
import { DocumentRegister } from './DocumentRegister'

export function PurchaseOrderListPage() {
  const navigate = useNavigate()
  const [showCreate, setShowCreate] = useState(false)
  const [comparisonNumber, setComparisonNumber] = useState('')
  const [comparisonVersion, setComparisonVersion] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const loadComparisonVersion = async () => {
    setError('')
    try {
      const comparison = await getComparison(comparisonNumber)
      setComparisonVersion(String(comparison.Version))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to read the comparison version.')
    }
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError('')
    if (!comparisonNumber.trim()) {
      setError('Comparison number is required.')
      return
    }
    setBusy(true)
    try {
      const result = await createPurchaseOrder({
        ComparisonNumber: comparisonNumber.trim().toUpperCase(),
        ComparisonVersion: Number(comparisonVersion) || 0,
        IdempotencyKey: newIdempotencyKey('po-create'),
      })
      rememberDoc('purchase-order', result.Number)
      navigate(`/purchase/purchase-orders/${encodeURIComponent(result.Number)}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create the purchase order.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <DocumentRegister
      kind="purchase-order"
      title="Purchase Order"
      subtitle="Step 5 of the purchase flow — an approved comparison becomes a PO, then it is approved and issued to the vendor"
      missingEndpoint="GET /api/v1/purchase/purchase-orders"
      placeholder="Open PO by number, e.g. PO-2627-00001"
      routePrefix="/purchase/purchase-orders"
      createLabel="+ New Purchase Order"
      onCreate={() => setShowCreate(true)}
    >
      {showCreate && (
        <div className="modal-backdrop">
          <div className="modal" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h2>New Purchase Order</h2>
              <button type="button" className="btn btn-ghost" onClick={() => setShowCreate(false)}>✕</button>
            </div>
            <form onSubmit={submit} className="form-grid">
              <p className="field-wide field-hint">
                A PO is generated from an approved comparison. Vendor, lines, rates and tax all come
                from the comparison snapshot — nothing is typed here.
              </p>
              <label className="field">
                <span className="field-label">Comparison number *</span>
                <input
                  className="input mono"
                  placeholder="CMP-2627-00001"
                  value={comparisonNumber}
                  onChange={(event) => setComparisonNumber(event.target.value)}
                />
              </label>
              <label className="field">
                <span className="field-label">Comparison version *</span>
                <input
                  className="input mono"
                  value={comparisonVersion}
                  onChange={(event) => setComparisonVersion(event.target.value)}
                />
                <button type="button" className="link-button" onClick={() => void loadComparisonVersion()}>
                  Read current version from the comparison
                </button>
              </label>
              {error && <div className="field-wide alert alert-error">{error}</div>}
              <div className="field-wide modal-actions">
                <button type="button" className="btn btn-ghost" onClick={() => setShowCreate(false)} disabled={busy}>
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary" disabled={busy}>
                  {busy ? 'Creating…' : 'Create purchase order'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </DocumentRegister>
  )
}
