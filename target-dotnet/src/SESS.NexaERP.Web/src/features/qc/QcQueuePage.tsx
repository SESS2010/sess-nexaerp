import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createQcInspection, getQcInspection, isQcMockMode, listQcQueue } from '../../api/qc'
import type { QcQueueItem } from '../../types/qc'
import { ErrorAlert } from '../../components/ErrorAlert'

const PAGE_SIZE = 25

/**
 * The QC work queue: every GRN line sitting in QC hold, oldest and overdue
 * first. Opening a row creates the inspection on first touch (INITIAL DRAFT
 * revision) or resumes the existing one.
 */
export function QcQueuePage() {
  const navigate = useNavigate()
  const [rows, setRows] = useState<QcQueueItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [overdueOnly, setOverdueOnly] = useState(false)
  const [loading, setLoading] = useState(false)
  const [opening, setOpening] = useState<string | null>(null)
  const [error, setError] = useState<unknown>(null)
  const [mock, setMock] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listQcQueue({ page, pageSize: PAGE_SIZE, overdue: overdueOnly || undefined })
      setRows(data.Items)
      setTotalCount(data.TotalCount)
      setMock(isQcMockMode())
    } catch (err) {
      setRows([])
      setTotalCount(0)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [page, overdueOnly])

  useEffect(() => {
    void load()
  }, [load])

  const open = async (item: QcQueueItem) => {
    setError(null)
    setOpening(item.GoodsReceiptLineId)
    try {
      const inspection = item.InspectionId
        ? await getQcInspection(item.InspectionId)
        : await createQcInspection(item.GoodsReceiptLineId)
      navigate(`/qc/inspections/${inspection.Id}`)
    } catch (err) {
      setError(err)
    } finally {
      setOpening(null)
    }
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>QC / Inspection</h1>
          <p className="page-sub">
            GRN lines in QC hold awaiting inspection ({totalCount} pending) — accept moves stock to the routed location, reject to pending-return
          </p>
        </div>
      </div>

      {mock && (
        <div className="alert alert-warn" role="status">
          <div className="alert-title">QC backend (Slice 3) is not live yet — showing local mock data</div>
          <p className="alert-body">
            The screen is fully working against a session-local mock so the inspection flow can be exercised and refined.
            Nothing here is saved to the server. The moment the QC endpoints ship, this banner disappears and the same screen runs live.
          </p>
        </div>
      )}

      <div className="toolbar">
        <label className="scan-toggle">
          <input type="checkbox" checked={overdueOnly} onChange={(event) => { setOverdueOnly(event.target.checked); setPage(1) }} />
          Overdue only
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
              <th>Source</th>
              <th>Item</th>
              <th>Category</th>
              <th className="text-right">Qty</th>
              <th>QC rack</th>
              <th>Received</th>
              <th>QC due</th>
              <th className="text-right">Age (h)</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={9} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={9} className="table-empty">Nothing waiting for inspection.</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr
                key={row.GoodsReceiptLineId}
                className="row-click"
                onClick={() => opening === null && void open(row)}
              >
                <td className="mono">{row.SourceNumber}</td>
                <td><span className="mono">{row.ItemCode}</span> — {row.ItemName}</td>
                <td className="mono">{row.CategoryCode}</td>
                <td className="text-right mono">{row.Quantity}</td>
                <td className="mono">{row.QcRackCode}</td>
                <td>{new Date(row.ReceivedAt).toLocaleString('en-IN')}</td>
                <td>{new Date(row.QcDueAt).toLocaleString('en-IN')}</td>
                <td className="text-right mono">{row.AgeHours.toFixed(0)}</td>
                <td>
                  {opening === row.GoodsReceiptLineId
                    ? <span className="badge badge-muted">Opening…</span>
                    : row.IsOverdue
                      ? <span className="badge badge-error">Overdue</span>
                      : row.InspectionId
                        ? <span className="badge badge-info">In progress</span>
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
