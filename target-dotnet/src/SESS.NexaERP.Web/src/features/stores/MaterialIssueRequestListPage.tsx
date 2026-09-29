import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listMaterialIssueRequests } from '../../api/materialIssues'
import type { MaterialIssueRequestView } from '../../types/materialIssue'
import { MIR_STATES } from '../../types/materialIssue'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { MaterialIssueRequestFormModal } from './MaterialIssueRequestFormModal'

const PAGE_SIZE = 25

/**
 * GET /api/v1/stores/material-issue-requests — newest first (server orders by
 * CreatedAt desc and does not read a sort parameter yet), filtered by number
 * substring and status.
 */
export function MaterialIssueRequestListPage() {
  const navigate = useNavigate()
  const { can } = useSession()
  const [rows, setRows] = useState<MaterialIssueRequestView[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [showCreate, setShowCreate] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listMaterialIssueRequests({
        page,
        pageSize: PAGE_SIZE,
        number: appliedSearch || undefined,
        status: status || undefined,
      })
      setRows(data.Items ?? [])
      setTotal(data.Total)
    } catch (err) {
      setRows([])
      setTotal(0)
      setError(err)
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

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const canCreate = can(PAGE_KEYS.materialIssueRequests, 'create')

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Material Issue Request</h1>
          <p className="page-sub">
            Ask Stores for material — against a Job Order for chamber, service or site work; against a department for consumables. Stores or Production approves; Stores issues by scan.
          </p>
        </div>
        <div className="action-row">
          {canCreate && (
            <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
              + New MIR
            </button>
          )}
        </div>
      </div>

      <div className="toolbar">
        <input
          className="input search"
          placeholder="MIR number…"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onKeyDown={(event) => event.key === 'Enter' && applySearch()}
        />
        <button type="button" className="btn btn-ghost" onClick={applySearch}>Search</button>
        <select className="input" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="">All states</option>
          {MIR_STATES.map((option) => <option key={option} value={option}>{option.replaceAll('_', ' ')}</option>)}
        </select>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages} · {total} total</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load material issue requests." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>MIR</th>
              <th>Situation</th>
              <th>Purpose</th>
              <th>Destination</th>
              <th>Required by</th>
              <th className="text-right">Lines</th>
              <th className="text-right">Excess lines</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={8} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={8} className="table-empty">No material issue requests yet.{canCreate ? ' Raise one with “+ New MIR”.' : ''}</td></tr>
            )}
            {!loading && rows.map((row) => {
              const excess = row.Lines.filter((line) => line.ExcessBaseQuantity > 0).length
              return (
                <tr key={row.Id} className="row-click" onClick={() => navigate(`/stores/material-issue-requests/${row.Id}`)}>
                  <td className="mono">{row.RequestNumber}</td>
                  <td>{row.Situation.replaceAll('_', ' ')}</td>
                  <td>{row.Purpose.replaceAll('_', ' ')}</td>
                  <td>{row.DestinationType.replaceAll('_', ' ')} · {row.DestinationName}</td>
                  <td>{row.RequiredDate}</td>
                  <td className="text-right mono">{row.Lines.length}</td>
                  <td className="text-right mono">{excess > 0 ? <StatusBadge value={`${excess} excess`} /> : '—'}</td>
                  <td><StatusBadge value={row.Status} /></td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <MaterialIssueRequestFormModal
          mode="create"
          onClose={() => setShowCreate(false)}
          onSaved={(result) => {
            setShowCreate(false)
            navigate(`/stores/material-issue-requests/${result.Id}`)
          }}
        />
      )}
    </div>
  )
}
