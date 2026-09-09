import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { createProductionBom, listProductionBoms } from '../../api/boms'
import { listJobOrders } from '../../api/production'
import { newIdempotencyKey } from '../../api/stores'
import type { ProductionBomView } from '../../types/bom'
import type { JobOrderSummary } from '../../types/production'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

/**
 * GET /api/v1/production/boms (not paged). One Production BOM per Job Order,
 * created by copying the job's approved Estimated BOM revision; from then on
 * Production revises it. An APPROVED revision is pinned to the job so MIR
 * excess checks read one agreed list.
 */
export function ProductionBomListPage() {
  const navigate = useNavigate()
  const { can } = useSession()
  const [params] = useSearchParams()
  const [rows, setRows] = useState<ProductionBomView[]>([])
  const [search, setSearch] = useState(params.get('jobOrder') ?? '')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [creating, setCreating] = useState(false)
  const [jobs, setJobs] = useState<JobOrderSummary[]>([])
  const [jobOrderId, setJobOrderId] = useState('')
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setRows(await listProductionBoms())
    } catch (err) {
      setRows([])
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    if (!creating || jobs.length > 0) return
    listJobOrders({ page: 1, pageSize: 100, status: 'OPEN' }).then((page) => setJobs(page.Items ?? [])).catch(setError)
  }, [creating, jobs.length])

  const create = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!jobOrderId) { setError('Pick the OPEN job order.'); return }
    if (!reason.trim()) { setError('A revision reason is required.'); return }
    setError(null)
    setBusy(true)
    try {
      const result = await createProductionBom({ JobOrderId: jobOrderId, RevisionReason: reason.trim(), IdempotencyKey: newIdempotencyKey('pbom-create') })
      navigate(`/production/boms/${result.BomNumber}`)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const term = search.trim().toUpperCase()
  const visible = term ? rows.filter((row) => row.BomNumber.toUpperCase().includes(term) || row.JobOrderNumber.toUpperCase().includes(term)) : rows
  const canCreate = can(PAGE_KEYS.productionBom, 'create')

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Production BOM</h1>
          <p className="page-sub">
            Production's build list for one machine, seeded from the approved Estimated BOM. Pin an approved revision to the job so material requests are checked against it.
          </p>
        </div>
        <div className="action-row">
          {canCreate && !creating && <button type="button" className="btn btn-primary" onClick={() => setCreating(true)}>+ New from Estimated BOM</button>}
        </div>
      </div>

      {creating && (
        <form onSubmit={create} className="card" style={{ marginBottom: 16 }}>
          <div className="form-section-title">Create Production BOM</div>
          <div className="form-grid">
            <label className="field">
              <span className="field-label">Job order *</span>
              <select className="input" value={jobOrderId} onChange={(event) => setJobOrderId(event.target.value)}>
                <option value="">Pick an OPEN job order…</option>
                {jobs.map((job) => <option key={job.Id} value={job.Id}>{job.JobOrderNumber} · {job.CustomerName} · {job.MachineModel} #{job.MachineOrdinal}</option>)}
              </select>
              <span className="field-hint">The job's Estimated BOM must have an approved revision; its lines become revision 1 of this BOM.</span>
            </label>
            <label className="field">
              <span className="field-label">Revision reason *</span>
              <input className="input" value={reason} onChange={(event) => setReason(event.target.value)} />
            </label>
            <div className="field-wide modal-actions">
              <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setCreating(false)}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? 'Creating…' : 'Create draft'}</button>
            </div>
          </div>
        </form>
      )}

      <div className="toolbar">
        <input className="input search" placeholder="Filter by BOM or job order number…" value={search} onChange={(event) => setSearch(event.target.value)} />
        <div className="spacer" />
        <span className="pager-label">{visible.length} of {rows.length}</span>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load production BOMs." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>BOM</th>
              <th>Job Order</th>
              <th className="text-right">Revision</th>
              <th className="text-right">Lines</th>
              <th>Pinned</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={6} className="table-empty">Loading…</td></tr>}
            {!loading && visible.length === 0 && !error && (
              <tr><td colSpan={6} className="table-empty">No production BOMs{term ? ' match' : ' yet'}.</td></tr>
            )}
            {!loading && visible.map((row) => (
              <tr key={row.Id} className="row-click" onClick={() => navigate(`/production/boms/${row.BomNumber}`)}>
                <td className="mono">{row.BomNumber}</td>
                <td className="mono">{row.JobOrderNumber}</td>
                <td className="text-right mono">{row.CurrentRevisionNumber}</td>
                <td className="text-right mono">{row.CurrentRevision.Lines.length}</td>
                <td>{row.PinnedRevisionId ? <StatusBadge value="Pinned" /> : '—'}</td>
                <td><StatusBadge value={row.Status} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
