import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createPurchaseOrder, getComparison, listPurchaseOrders, newIdempotencyKey, rememberDoc } from '../../api/purchase'
import type { PurchaseOrderListItem } from '../../types/purchase'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PurchaseDocumentRegister, formatDate, formatMoney, type RegisterColumn } from './PurchaseDocumentRegister'

const COLUMNS: RegisterColumn<PurchaseOrderListItem>[] = [
  { header: 'PO', sortKey: 'purchaseordernumber', className: 'mono', render: (row) => `${row.PurchaseOrderNumber}${row.RevisionNumber > 1 ? ` r${row.RevisionNumber}` : ''}` },
  { header: 'Vendor', render: (row) => `${row.VendorCode} — ${row.VendorName}` },
  { header: 'Total payable', className: 'text-right mono', render: (row) => formatMoney(row.TotalPayableValue) },
  { header: 'Created', sortKey: 'date', render: (row) => formatDate(row.CreatedAt) },
  { header: 'Issued', render: (row) => formatDate(row.IssuedAt) },
  { header: 'Status', sortKey: 'status', render: (row) => <StatusBadge value={row.Status} /> },
]

export function PurchaseOrderListPage() {
  const navigate = useNavigate()
  const { can, hasRole } = useSession()
  const [showCreate, setShowCreate] = useState(false)
  const [comparisonNumber, setComparisonNumber] = useState('')
  const [comparisonVersion, setComparisonVersion] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<unknown>(null)

  // POST /purchase/purchase-orders → purchase.po:create, and
  // CreatePurchaseOrderAsync additionally demands the PURCHASE_MANAGER role.
  const canCreatePo = can(PAGE_KEYS.purchaseOrders, 'create') && hasRole('PURCHASE_MANAGER')
  // The version lookup reads GET /purchase/comparisons/{number} →
  // purchase.commercial-comparisons:view.
  const canReadComparison = can(PAGE_KEYS.comparisons, 'view')

  const loadComparisonVersion = async () => {
    setError(null)
    try {
      const comparison = await getComparison(comparisonNumber)
      setComparisonVersion(String(comparison.Version))
    } catch (err) {
      setError(err)
    }
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
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
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <PurchaseDocumentRegister
      title="Purchase Order"
      subtitle="Step 5 of the purchase flow — an approved comparison becomes a PO, then it is approved and issued to the vendor"
      numberPlaceholder="PO number, e.g. PO-2627-00001"
      defaultSort={{ sortBy: 'date', sortDirection: 'desc' }}
      fetch={listPurchaseOrders}
      columns={COLUMNS}
      rowKey={(row) => row.Id}
      onOpen={(row) => navigate(`/purchase/purchase-orders/${encodeURIComponent(row.PurchaseOrderNumber)}`)}
      createLabel="+ New Purchase Order"
      onCreate={() => setShowCreate(true)}
      canCreate={canCreatePo}
    >
      {showCreate && canCreatePo && (
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
                {canReadComparison && (
                  <button type="button" className="link-button" onClick={() => void loadComparisonVersion()}>
                    Read current version from the comparison
                  </button>
                )}
              </label>
              <ErrorAlert error={error} className="field-wide" fallback="Could not create the purchase order." />
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
    </PurchaseDocumentRegister>
  )
}
