import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createComparison, getRfq, listComparisons, newIdempotencyKey, rememberDoc } from '../../api/purchase'
import type { ComparisonListItem } from '../../types/purchase'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PurchaseDocumentRegister, formatDate, formatMoney, type RegisterColumn } from './PurchaseDocumentRegister'

const COLUMNS: RegisterColumn<ComparisonListItem>[] = [
  { header: 'Comparison', sortKey: 'comparisonnumber', className: 'mono', render: (row) => row.ComparisonNumber },
  { header: 'RFQ', className: 'mono', render: (row) => row.RfqNumber },
  { header: 'Selected vendor', render: (row) => row.SelectedVendorName ? `${row.SelectedVendorCode} — ${row.SelectedVendorName}` : '—' },
  { header: 'Total payable', className: 'text-right mono', render: (row) => formatMoney(row.TotalPayableValue) },
  { header: 'Created', sortKey: 'date', render: (row) => formatDate(row.CreatedAt) },
  { header: 'Status', sortKey: 'status', render: (row) => <StatusBadge value={row.Status} /> },
]

export function ComparisonListPage() {
  const navigate = useNavigate()
  const { can, hasRole } = useSession()
  const [showCreate, setShowCreate] = useState(false)
  const [rfqNumber, setRfqNumber] = useState('')
  const [rfqVersion, setRfqVersion] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<unknown>(null)

  // POST /purchase/comparisons → purchase.commercial-comparisons:create, and
  // CreateComparisonAsync additionally demands the PURCHASE_MANAGER role.
  const canCreateComparison =
    can(PAGE_KEYS.comparisons, 'create') && hasRole('PURCHASE_MANAGER')
  // The version lookup reads GET /purchase/rfqs/{number} → purchase.rfq:view.
  const canReadRfq = can(PAGE_KEYS.rfq, 'view')

  const loadRfqVersion = async () => {
    setError(null)
    try {
      const rfq = await getRfq(rfqNumber)
      setRfqVersion(String(rfq.Version))
    } catch (err) {
      setError(err)
    }
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
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
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <PurchaseDocumentRegister
      title="Commercial Comparison"
      subtitle="Step 4 of the purchase flow — quotations are compared and a vendor is recommended for approval"
      numberPlaceholder="Comparison number, e.g. CMP-2627-00001"
      defaultSort={{ sortBy: 'date', sortDirection: 'desc' }}
      fetch={listComparisons}
      columns={COLUMNS}
      rowKey={(row) => row.Id}
      onOpen={(row) => navigate(`/purchase/comparisons/${encodeURIComponent(row.ComparisonNumber)}`)}
      createLabel="+ New Comparison"
      onCreate={() => setShowCreate(true)}
      canCreate={canCreateComparison}
    >
      {showCreate && canCreateComparison && (
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
                {canReadRfq && (
                  <button type="button" className="link-button" onClick={() => void loadRfqVersion()}>
                    Read current version from the RFQ
                  </button>
                )}
              </label>
              <ErrorAlert error={error} className="field-wide" fallback="Could not create the comparison." />
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
    </PurchaseDocumentRegister>
  )
}
