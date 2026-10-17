import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { listConcessions } from '../../api/qc'
import type { InventoryConcessionListItem } from '../../types/qc'
import { ErrorAlert } from '../../components/ErrorAlert'
import { StatusBadge } from '../employees/StatusBadge'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { sessionScopeKey } from '../purchase/quotationDraft'
import { formatInstantIst } from '../../print/format'

export function ConcessionListPage() {
  const { me, can } = useSession()
  const navigate = useNavigate()
  const scope = sessionScopeKey(me)
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('DRAFT')
  const [rows, setRows] = useState<InventoryConcessionListItem[]>([])
  const [total, setTotal] = useState(0)
  const [loadedScope, setLoadedScope] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const ticket = useRef(0)
  const canView = can(PAGE_KEYS.qc, 'view')
  const load = useCallback(async () => {
    const own = ++ticket.current
    setRows([]); setTotal(0); setError(null); setLoadedScope('')
    if (!canView) { setLoading(false); return }
    setLoading(true)
    try {
      const result = await listConcessions(page, status)
      if (ticket.current !== own) return
      setRows(result.Items); setTotal(result.TotalCount); setLoadedScope(scope)
    } catch (err) { if (ticket.current === own) setError(err) }
    finally { if (ticket.current === own) setLoading(false) }
  }, [page, status, scope, canView])
  useEffect(() => { void load(); return () => { ++ticket.current } }, [load])
  const visible = loadedScope === scope ? rows : []
  const count = loadedScope === scope ? total : 0
  return <div className="page">
    <div className="page-header"><div><h1>QC Concessions</h1><p className="page-sub">Concessions in the selected company</p></div>
      {can(PAGE_KEYS.qc, 'create') && <Link to="/qc/inspections" className="btn btn-ghost">Raise from an inspection</Link>}
    </div>
    <div className="toolbar"><label>Status <select className="input" value={status} onChange={e => { setPage(1); setStatus(e.target.value) }}>
      <option value="">All statuses</option><option value="DRAFT">Awaiting decision</option><option value="APPROVED">Approved</option><option value="REJECTED">Rejected</option><option value="REVERSED">Reversed</option>
    </select></label><button className="btn btn-ghost" disabled={loading || !canView} onClick={() => void load()}>Refresh</button></div>
    <ErrorAlert error={error} onReload={() => void load()} fallback="Concessions could not be read." />
    {loading && <p>Loading concessions...</p>}
    <div className="table-wrap"><table className="table"><thead><tr><th>Concession</th><th>Status</th><th>Quantity</th><th>Created</th><th /></tr></thead><tbody>
      {visible.map(row => <tr key={row.Id}><td>{row.ConcessionNumber}</td><td><StatusBadge value={row.Status} /></td><td>{row.Quantity}</td><td>{formatInstantIst(row.CreatedAt)}</td><td><button className="btn btn-ghost" onClick={() => navigate(`/qc/concessions/${encodeURIComponent(row.ConcessionNumber)}`)}>Open</button></td></tr>)}
      {!loading && !error && visible.length === 0 && <tr><td colSpan={5}>No concessions match this filter.</td></tr>}
    </tbody></table></div>
    <div className="action-row"><button className="btn btn-ghost" disabled={loading || page <= 1} onClick={() => setPage(page - 1)}>Previous</button><span>Page {page} - {count} concessions</span><button className="btn btn-ghost" disabled={loading || page * 25 >= count} onClick={() => setPage(page + 1)}>Next</button></div>
  </div>
}
