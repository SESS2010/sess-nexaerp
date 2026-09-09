import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { createEstimatedBom, downloadEstimatedBomTemplate, importEstimatedBomWorkbook, listEstimatedBoms } from '../../api/boms'
import { newIdempotencyKey } from '../../api/stores'
import type { EstimatedBomSummary } from '../../types/bom'
import { BOM_STATES } from '../../types/bom'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { BomLinesEditorModal } from './BomLinesEditorModal'

const PAGE_SIZE = 25

/**
 * GET /api/v1/design/estimated-boms. One Estimated BOM per Job Order,
 * prepared by Design after Accounts confirms the job. Revisions go
 * DRAFT → SUBMITTED → APPROVED; the approved revision is what a job-backed
 * MIR is checked against and what the Production BOM is copied from.
 */
export function EstimatedBomListPage() {
  const navigate = useNavigate()
  const { can } = useSession()
  const [params] = useSearchParams()
  const [rows, setRows] = useState<EstimatedBomSummary[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState(params.get('jobOrder') ?? '')
  const [appliedSearch, setAppliedSearch] = useState(params.get('jobOrder') ?? '')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [importing, setImporting] = useState(false)
  const fileInput = useRef<HTMLInputElement>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listEstimatedBoms({ page, pageSize: PAGE_SIZE, search: appliedSearch || undefined, status: status || undefined })
      setRows(data.Items ?? [])
      setTotal(data.TotalCount)
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

  const importWorkbook = async (file: File | undefined) => {
    if (!file) return
    setError(null)
    setNotice('')
    setImporting(true)
    try {
      const result = await importEstimatedBomWorkbook(file, newIdempotencyKey('ebom-import'))
      navigate(`/design/estimated-boms/${result.BomNumber}`)
    } catch (err) {
      setError(err)
    } finally {
      setImporting(false)
      if (fileInput.current) fileInput.current.value = ''
    }
  }

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const canCreate = can(PAGE_KEYS.estimatedBom, 'create')
  const canDownload = can(PAGE_KEYS.estimatedBom, 'download')

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Estimated BOM</h1>
          <p className="page-sub">
            Design's material estimate for one machine. Approved revisions bound job-backed material requests and seed the Production BOM.
          </p>
        </div>
        <div className="action-row">
          {canDownload && (
            <button type="button" className="btn btn-ghost" onClick={() => downloadEstimatedBomTemplate().catch(setError)}>Workbook template</button>
          )}
          {canCreate && (
            <>
              <input ref={fileInput} type="file" accept=".xlsx" hidden onChange={(event) => void importWorkbook(event.target.files?.[0])} />
              <button type="button" className="btn btn-ghost" disabled={importing} onClick={() => fileInput.current?.click()}>{importing ? 'Importing…' : 'Import workbook'}</button>
              <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>+ New Estimated BOM</button>
            </>
          )}
        </div>
      </div>

      <div className="toolbar">
        <input
          className="input search"
          placeholder="BOM or job order number…"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onKeyDown={(event) => event.key === 'Enter' && applySearch()}
        />
        <button type="button" className="btn btn-ghost" onClick={applySearch}>Search</button>
        <select className="input" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="">All states</option>
          {BOM_STATES.map((option) => <option key={option} value={option}>{option}</option>)}
        </select>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages} · {total} total</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load estimated BOMs." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>BOM</th>
              <th>Job Order</th>
              <th className="text-right">Revision</th>
              <th>Approved revision</th>
              <th>Commercial baseline</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={6} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={6} className="table-empty">No estimated BOMs{appliedSearch ? ' match' : ' yet'}.{canCreate ? ' Create one for an OPEN job order.' : ''}</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr key={row.Id} className="row-click" onClick={() => navigate(`/design/estimated-boms/${row.BomNumber}`)}>
                <td className="mono">{row.BomNumber}</td>
                <td className="mono">{row.JobOrderNumber}</td>
                <td className="text-right mono">{row.CurrentRevisionNumber}</td>
                <td>{row.ApprovedRevisionId ? <StatusBadge value="APPROVED" /> : '—'}</td>
                <td>{row.CommercialBaselineRevisionId ? <StatusBadge value="Pinned" /> : '—'}</td>
                <td><StatusBadge value={row.Status} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <BomLinesEditorModal
          title="New Estimated BOM"
          pickJobOrder
          initialLines={[]}
          reasonLabel="Revision reason"
          submitLabel="Create draft"
          canSave={canCreate}
          onClose={() => setShowCreate(false)}
          onSave={async ({ jobOrderId, reason, lines }) => {
            const result = await createEstimatedBom({ JobOrderId: jobOrderId, RevisionReason: reason, Lines: lines, IdempotencyKey: newIdempotencyKey('ebom-create') })
            setShowCreate(false)
            navigate(`/design/estimated-boms/${result.BomNumber}`)
          }}
        />
      )}
    </div>
  )
}
