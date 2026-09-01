import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listPurchaseRequisitions } from '../../api/purchase'
import type { PurchaseRequisitionSummary } from '../../types/purchase'
import { PR_STATUSES } from '../../types/purchase'
import { StatusBadge } from '../employees/StatusBadge'
import { PurchaseRequisitionFormModal } from './PurchaseRequisitionFormModal'

const PAGE_SIZE = 20

export function formatAmount(value: number | null | undefined): string {
  if (value === null || value === undefined) return '—'
  return value.toLocaleString('en-IN', { maximumFractionDigits: 2 })
}

export function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  const [year, month, day] = value.split('-')
  return `${day}-${month}-${year}`
}

export function PurchaseRequisitionListPage() {
  const navigate = useNavigate()
  const [rows, setRows] = useState<PurchaseRequisitionSummary[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [showCreate, setShowCreate] = useState(false)

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await listPurchaseRequisitions({
        page,
        pageSize: PAGE_SIZE,
        search: appliedSearch,
        status,
      })
      setRows(data.Items)
      setTotalCount(data.TotalCount)
    } catch (err) {
      setRows([])
      setTotalCount(0)
      setError(err instanceof Error ? err.message : 'Failed to load purchase requisitions.')
    } finally {
      setLoading(false)
    }
  }, [page, appliedSearch, status])

  useEffect(() => {
    void load()
  }, [load])

  const applySearch = () => {
    setPage(1)
    setAppliedSearch(search.trim())
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Purchase Requisition</h1>
          <p className="page-sub">
            Step 1 of the purchase flow — demand is raised here, then routed for approval ({totalCount} total)
          </p>
        </div>
        <div className="action-row">
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Requisition
          </button>
        </div>
      </div>

      <div className="toolbar">
        <input
          className="input search"
          placeholder="Search PR number or purpose…"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onKeyDown={(event) => event.key === 'Enter' && applySearch()}
        />
        <button type="button" className="btn btn-ghost" onClick={applySearch}>Search</button>
        <select
          className="input"
          value={status}
          onChange={(event) => { setStatus(event.target.value); setPage(1) }}
        >
          <option value="">All statuses</option>
          {PR_STATUSES.map((option) => <option key={option} value={option}>{option}</option>)}
        </select>
        <div className="spacer" />
        <div className="pager">
          <button
            type="button"
            className="btn btn-ghost"
            disabled={page <= 1 || loading}
            onClick={() => setPage(page - 1)}
          >‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages}</span>
          <button
            type="button"
            className="btn btn-ghost"
            disabled={page >= totalPages || loading}
            onClick={() => setPage(page + 1)}
          >Next ›</button>
        </div>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>PR Number</th>
              <th>Department</th>
              <th>Requester</th>
              <th>Raised</th>
              <th>Required By</th>
              <th>Priority</th>
              <th className="text-right">Estimated (₹)</th>
              <th>Approval Route</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={9} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={9} className="table-empty">No purchase requisitions found.</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr
                key={row.Id}
                className="row-click"
                onClick={() => navigate(`/purchase/requisitions/${encodeURIComponent(row.PrNumber)}`)}
              >
                <td className="mono">{row.PrNumber}</td>
                <td>{row.RequestingDepartment || '—'}</td>
                <td className="mono">{row.RequesterEmployeeCode || '—'}</td>
                <td>{formatDate(row.RequestDate)}</td>
                <td>{formatDate(row.RequiredByDate)}</td>
                <td>{row.Priority}</td>
                <td className="text-right mono">{formatAmount(row.EstimatedTotal)}</td>
                <td>{row.ApprovalRoute || '—'}</td>
                <td><StatusBadge value={row.Status} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <PurchaseRequisitionFormModal
          mode="create"
          onClose={() => setShowCreate(false)}
          onSaved={(detail) => {
            setShowCreate(false)
            navigate(`/purchase/requisitions/${encodeURIComponent(detail.PrNumber)}`)
          }}
        />
      )}
    </div>
  )
}
