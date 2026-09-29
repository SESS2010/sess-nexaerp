import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listQcQueue } from '../../api/qc'
import type { QcQueueItem } from '../../types/qc'
import { ErrorAlert } from '../../components/ErrorAlert'

const PAGE_SIZE = 25

/**
 * The QC work queue: every GRN lot allocation sitting in QC_HOLD with no
 * inspection yet, oldest receipt first. Opening a row starts the inspection;
 * nothing is written until it is finalized.
 */
export function QcQueuePage() {
  const navigate = useNavigate()
  const [rows, setRows] = useState<QcQueueItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [overdueOnly, setOverdueOnly] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listQcQueue({ page, pageSize: PAGE_SIZE })
      setRows(data.Items)
      setTotalCount(data.TotalCount)
    } catch (err) {
      setRows([])
      setTotalCount(0)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [page])

  useEffect(() => {
    void load()
  }, [load])

  // The endpoint has no overdue filter; this narrows the loaded page only.
  const visible = overdueOnly ? rows.filter((row) => row.IsOverdue) : rows
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>QC / Inspection</h1>
          <p className="page-sub">
            GRN lots in QC hold awaiting inspection ({totalCount} pending) — accepted stock moves to an AVAILABLE
            location, rejected to pending-return, discrepancy stays in QC hold
          </p>
        </div>
        <div className="action-row">
          <input
            className="input search"
            placeholder="Open inspection by number, e.g. QCI-2627-00001"
            onKeyDown={(event) => {
              const value = (event.target as HTMLInputElement).value.trim().toUpperCase()
              if (event.key === 'Enter' && value) navigate(`/qc/inspections/${encodeURIComponent(value)}`)
            }}
          />
        </div>
      </div>

      <div className="toolbar">
        <label className="scan-toggle">
          <input type="checkbox" checked={overdueOnly} onChange={(event) => setOverdueOnly(event.target.checked)} />
          Overdue only (this page)
        </label>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages}</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load the QC queue." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>GRN</th>
              <th>Line / Lot</th>
              <th>Item</th>
              <th className="text-right">Qty</th>
              <th>Supplier lot</th>
              <th>Received</th>
              <th className="text-right">Age (days)</th>
              <th>Policy</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={9} className="table-empty">Loading…</td></tr>}
            {!loading && visible.length === 0 && !error && (
              <tr><td colSpan={9} className="table-empty">Nothing waiting for inspection.</td></tr>
            )}
            {!loading && visible.map((row) => (
              <tr
                key={row.GoodsReceiptLineLotAllocationId}
                className="row-click"
                onClick={() => navigate(`/qc/inspect/${row.GoodsReceiptLineLotAllocationId}`, { state: { item: row } })}
              >
                <td className="mono">{row.GrnNumber}</td>
                <td className="mono">{row.LineNumber} / {row.LotOrdinal}</td>
                <td><span className="mono">{row.ItemCode}</span> — {row.ItemName}</td>
                <td className="text-right mono">{row.Quantity}</td>
                <td className="mono">{row.SupplierLotNumber ?? '—'}</td>
                <td>{new Date(row.ReceivedAt).toLocaleString('en-IN')}</td>
                <td className="text-right mono">{row.AgeDays} / {row.CompletionLimitDays}</td>
                <td>
                  {row.HasEffectivePolicy
                    ? <span className="badge badge-info">Policy</span>
                    : <span className="badge badge-warn" title={row.PolicyResolution}>No policy</span>}
                </td>
                <td>
                  {row.IsOverdue
                    ? <span className="badge badge-error">Overdue</span>
                    : <span className="badge badge-muted">Pending</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
