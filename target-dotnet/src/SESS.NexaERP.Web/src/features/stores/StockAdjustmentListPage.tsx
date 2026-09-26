import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  STOCK_ADJUSTMENT_PAGE_KEY,
  formatDateOnly,
  formatRupees,
  listStockAdjustments,
} from '../../api/stockAdjustments'
import type { StockAdjustmentView } from '../../types/stockAdjustment'
import { STOCK_ADJUSTMENT_STATES, stockAdjustmentRoleWords } from '../../types/stockAdjustment'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession } from '../auth/SessionContext'
import { StockAdjustmentFormModal } from './StockAdjustmentFormModal'

const PAGE_SIZE = 25

/**
 * A2 stock adjustments — GET /api/v1/stores/stock-adjustments, newest first,
 * filtered by status. Corrections after the opening-stock ceremony go through
 * here: Stores records, the roles named by the server's approval snapshot
 * decide, and the last approval posts stock.
 */
export function StockAdjustmentListPage() {
  const navigate = useNavigate()
  const { can } = useSession()
  const [rows, setRows] = useState<StockAdjustmentView[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [showCreate, setShowCreate] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listStockAdjustments({ page, pageSize: PAGE_SIZE, status: status || undefined })
      setRows(data.Items)
      setTotal(data.Total)
    } catch (err) {
      setRows([])
      setTotal(0)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [page, status])

  useEffect(() => {
    void load()
  }, [load])

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const canCreate = can(STOCK_ADJUSTMENT_PAGE_KEY, 'create')

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Stock Adjustment</h1>
          <p className="page-sub">
            Correct stock after the opening-stock ceremony: count variances, damage or loss, and wrong entries. Stores records and submits; the approvers the server names decide, and the final approval posts the stock.
          </p>
        </div>
        <div className="action-row">
          {canCreate && (
            <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
              + New adjustment
            </button>
          )}
        </div>
      </div>

      <div className="toolbar">
        <select className="input" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="">All states</option>
          {STOCK_ADJUSTMENT_STATES.map((option) => <option key={option} value={option}>{option}</option>)}
        </select>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages} · {total} total</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load stock adjustments." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Adjustment</th>
              <th>Warehouse</th>
              <th>Reason</th>
              <th>Effective date</th>
              <th className="text-right">Lines</th>
              <th className="text-right">Value</th>
              <th>Waiting on</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={8} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={8} className="table-empty">No stock adjustments{status ? ` in ${status}` : ''}.{canCreate ? ' Record one with “+ New adjustment”.' : ''}</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr key={row.Id} className="row-click" onClick={() => navigate(`/stores/stock-adjustments/${row.Id}`)}>
                <td className="mono">
                  {row.AdjustmentNumber}
                  {row.ReversesStockAdjustmentId && <span className="field-hint" style={{ display: 'inline', marginLeft: 6 }}>reversal</span>}
                </td>
                <td className="mono">{row.WarehouseCode}</td>
                <td>{row.ReasonKind.replaceAll('_', ' ')}</td>
                <td>{formatDateOnly(row.EffectiveDate)}{row.DaysBackdated > 0 ? ` (${row.DaysBackdated} d back)` : ''}</td>
                <td className="text-right mono">{row.Lines?.length ?? 0}</td>
                <td className="text-right mono">{formatRupees(row.AbsoluteValue)}</td>
                <td>
                  {row.Status === 'SUBMITTED' && (row.OutstandingRoleCodes ?? []).length > 0
                    ? row.OutstandingRoleCodes.map(stockAdjustmentRoleWords).join(', ')
                    : '—'}
                </td>
                <td><StatusBadge value={row.Status} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <StockAdjustmentFormModal
          mode="create"
          onClose={() => setShowCreate(false)}
          onSaved={(result) => {
            setShowCreate(false)
            navigate(`/stores/stock-adjustments/${result.Id}`)
          }}
        />
      )}
    </div>
  )
}
