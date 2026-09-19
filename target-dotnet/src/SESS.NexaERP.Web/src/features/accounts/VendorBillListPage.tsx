import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listVendorBills } from '../../api/vendorBills'
import type { VendorBillView } from '../../types/vendorBill'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'
import { formatAmount } from '../purchase/PurchaseRequisitionListPage'
import { VendorBillFormModal } from './VendorBillFormModal'

const STATUSES = ['', 'DRAFT', 'ACCEPTED', 'REJECTED', 'REVERSED'] as const

/**
 * GET /api/v1/accounts/vendor-bills — the vendor bills raised against GRNs.
 * Accounts raises a bill from a finalized GRN and then accepts it; acceptance
 * is what values the receipt (FIFO layer, Actual BOM) at the landed rate.
 * Until 19 Sep this was API-only (grid 18 Sep, finding #13).
 */
export function VendorBillListPage() {
  const navigate = useNavigate()
  const { can } = useSession()
  const canCreate = can(PAGE_KEYS.vendorBills, 'create')

  const [rows, setRows] = useState<VendorBillView[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<(typeof STATUSES)[number]>('')
  const [billNumber, setBillNumber] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const pageSize = 25

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await listVendorBills({ page, pageSize, status: status || undefined, billNumber: billNumber.trim() || undefined })
      setRows(result.Items ?? [])
      setTotal(result.Total ?? 0)
    } catch (err) {
      setRows([])
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [page, status, billNumber])

  useEffect(() => {
    void load()
  }, [load])

  const pages = Math.max(1, Math.ceil(total / pageSize))

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Vendor Bills</h1>
          <p className="page-sub">
            Bills raised against finalized GRNs. Accept a matched bill to value the received stock at its landed rate; the Actual BOM and FIFO layers follow.
          </p>
        </div>
        {canCreate && <button className="btn btn-primary" onClick={() => setShowCreate(true)}>+ Raise from GRN</button>}
      </div>

      <div className="toolbar">
        <input className="input mono" placeholder="Bill number" value={billNumber} onChange={(event) => { setPage(1); setBillNumber(event.target.value) }} style={{ maxWidth: 220 }} />
        <select className="input" value={status} onChange={(event) => { setPage(1); setStatus(event.target.value as (typeof STATUSES)[number]) }} style={{ maxWidth: 180 }}>
          {STATUSES.map((value) => <option key={value} value={value}>{value || 'All statuses'}</option>)}
        </select>
        <div className="spacer" />
        <span className="pager-label">{total} bill(s)</span>
      </div>

      {notice && <div className="alert alert-success">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="Vendor bills could not be loaded (Accounts only)." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th>Bill</th><th>Bill date</th><th>Status</th><th>Match</th><th className="text-right">Payable</th><th className="text-right">Charges</th><th className="text-right">Landed</th><th>Decided</th></tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={8} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && <tr><td colSpan={8} className="table-empty">No vendor bills.</td></tr>}
            {!loading && rows.map((row) => (
              <tr key={row.Id} className="row-click" onClick={() => navigate(`/accounts/vendor-bills/${row.Id}`)}>
                <td className="mono">{row.BillNumber}</td>
                <td>{row.BillDate}</td>
                <td><StatusBadge value={row.Status} /></td>
                <td><StatusBadge value={row.MatchStatus} /></td>
                <td className="text-right mono">{formatAmount(row.TotalPayableValue)}</td>
                <td className="text-right mono">{formatAmount(row.TotalChargeValue)}</td>
                <td className="text-right mono">{formatAmount(row.TotalLandedValue)}</td>
                <td>{row.DecidedAt ? new Date(row.DecidedAt).toLocaleString() : '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="pager">
        <button className="btn" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>‹ Prev</button>
        <span className="pager-label">Page {page} of {pages}</span>
        <button className="btn" disabled={page >= pages} onClick={() => setPage((current) => current + 1)}>Next ›</button>
      </div>

      {showCreate && (
        <VendorBillFormModal
          onClose={() => setShowCreate(false)}
          onSaved={(bill) => {
            setShowCreate(false)
            setNotice(`${bill.BillNumber} raised as ${bill.Status} (${bill.MatchStatus}).`)
            navigate(`/accounts/vendor-bills/${bill.Id}`)
          }}
        />
      )}
    </div>
  )
}
