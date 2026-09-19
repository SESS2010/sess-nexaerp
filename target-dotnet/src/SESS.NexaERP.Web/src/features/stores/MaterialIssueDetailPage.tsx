import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { acceptMaterialReturn, getMaterialIssue, getMaterialIssueRequest, listMaterialReturns } from '../../api/materialIssues'
import { newIdempotencyKey } from '../../api/stores'
import { listComponentFitments } from '../../api/production'
import type { MaterialIssueRequestView, MaterialIssueView, MaterialReturnView } from '../../types/materialIssue'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { MaterialReturnFormModal } from './MaterialReturnFormModal'
import type { IssuedLine } from './MaterialReturnFormModal'

/**
 * GET /api/v1/stores/material-issues/{id} plus the returns declared against
 * it. The issue is custody, not consumption: the engineer holds the material
 * until it is fitted (Production) or returned here and accepted by Stores.
 */
export function MaterialIssueDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { me, can } = useSession()
  const [issue, setIssue] = useState<MaterialIssueView | null>(null)
  const [mir, setMir] = useState<MaterialIssueRequestView | null>(null)
  const [returns, setReturns] = useState<MaterialReturnView[]>([])
  const [fittedByLine, setFittedByLine] = useState<Map<string, number>>(new Map())
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [declaring, setDeclaring] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const loaded = await getMaterialIssue(id)
      setIssue(loaded)
      const [request, page, fitments] = await Promise.all([
        getMaterialIssueRequest(loaded.MaterialIssueRequestId).catch(() => null),
        listMaterialReturns({ page: 1, pageSize: 50, materialIssueId: loaded.Id }).catch(() => ({ Items: [] as MaterialReturnView[] })),
        // Consumption is posted at fitment. Since 053a81e the line view carries
        // FittedQuantityBase; the fitment list is only the fallback for an API
        // that predates it (then non-Production roles may not read fitments and
        // outstanding shows issued minus returned only).
        loaded.JobOrderId && loaded.Lines.some((line) => line.FittedQuantityBase === undefined)
          ? listComponentFitments({ page: 1, pageSize: 200, jobOrderId: loaded.JobOrderId, activeOnly: true }).catch(() => ({ Items: [] }))
          : Promise.resolve({ Items: [] }),
      ])
      setMir(request)
      setReturns(page.Items ?? [])
      const fitted = new Map<string, number>()
      for (const fitment of fitments.Items ?? []) {
        if (fitment.IsReversed) continue
        fitted.set(fitment.MaterialIssueLineId, (fitted.get(fitment.MaterialIssueLineId) ?? 0) + fitment.QuantityBase)
      }
      setFittedByLine(fitted)
    } catch (err) {
      setIssue(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  const accept = async (row: MaterialReturnView) => {
    if (!reason.trim()) {
      setError('A reason is required to accept a return — it is recorded on the acceptance.')
      return
    }
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      const result = await acceptMaterialReturn(row.Id, {
        Version: row.Version,
        AcceptedAt: new Date().toISOString(),
        Reason: reason.trim(),
        IdempotencyKey: newIdempotencyKey('material-return-accept'),
      })
      setReason('')
      setNotice(`${result.ReturnNumber} accepted; the material is back in Stores custody.`)
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  if (loading && !issue) return <div className="page"><p>Loading…</p></div>

  if (!issue) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Material issue not found, or your role cannot read issues (Stores only)." />
      </div>
    )
  }

  const acceptedReturned = new Map<string, number>()
  for (const row of returns.filter((candidate) => candidate.Status === 'ACCEPTED')) {
    for (const line of row.Lines) acceptedReturned.set(line.MaterialIssueLineId, (acceptedReturned.get(line.MaterialIssueLineId) ?? 0) + line.ReturnedQuantityBase)
  }
  const lines: IssuedLine[] = issue.Lines.map((line) => {
    const requestLine = mir?.Lines.find((candidate) => candidate.Id === line.MaterialIssueRequestLineId)
    return {
      line,
      itemCode: requestLine?.ItemCode ?? line.ItemId,
      itemName: requestLine?.ItemName ?? '',
      outstanding: line.QuantityBase - (acceptedReturned.get(line.Id) ?? 0) - (line.FittedQuantityBase ?? fittedByLine.get(line.Id) ?? 0),
    }
  })
  const isCustodian = me?.EmployeeId === issue.IssuedToEmployeeId
  const outstanding = issue.Status === 'ISSUED' || issue.Status === 'PARTIALLY_RETURNED'
  const canDeclare = isCustodian && outstanding && can(PAGE_KEYS.materialReturns, 'create')
  const canAccept = can(PAGE_KEYS.materialReturns, 'approve')
  const pendingReturns = returns.filter((row) => row.Status === 'SUBMITTED')
  const overdue = outstanding && new Date(issue.ReturnDueAt).getTime() < Date.now()

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="mono">{issue.IssueNumber}</h1>
          <p className="page-sub">
            Against <a className="row-click mono" onClick={() => navigate(`/stores/material-issue-requests/${issue.MaterialIssueRequestId}`)}>{mir?.RequestNumber ?? issue.MaterialIssueRequestId}</a>
            {issue.JobOrderId ? <> · job order <a className="row-click mono" onClick={() => navigate(`/production/job-orders/${issue.JobOrderId}`)}>{issue.JobOrderId.slice(0, 8)}…</a></> : null}
          </p>
        </div>
        <div className="action-row">
          <StatusBadge value={issue.Status} />
          {overdue && <StatusBadge value="Overdue" />}
          {canDeclare && <button type="button" className="btn btn-primary" disabled={busy} onClick={() => setDeclaring(true)}>Declare return</button>}
        </div>
      </div>

      {canAccept && pendingReturns.length > 0 && (
        <div className="toolbar">
          <input className="input search" placeholder="Reason for accepting a return (mandatory)" value={reason} onChange={(event) => setReason(event.target.value)} />
        </div>
      )}

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />

      {outstanding && !isCustodian && (
        <div className="alert">Only the custodian can declare this return. Stores accepts it once declared.</div>
      )}

      <div className="detail-grid">
        <div><span className="field-label">Issued to</span> <span className="mono">{isCustodian ? `${me?.EmployeeCode} (you)` : issue.IssuedToEmployeeId}</span></div>
        <div><span className="field-label">Issued at</span> {new Date(issue.IssuedAt).toLocaleString()} · {issue.ActorRoleCode}</div>
        <div><span className="field-label">Return due</span> {new Date(issue.ReturnDueAt).toLocaleString()}</div>
        <div><span className="field-label">Stock posting</span> <span className="mono">{issue.StockPostingBatchId ?? '—'}</span></div>
        <div><span className="field-label">Version</span> <span className="mono">{issue.Version}</span></div>
      </div>

      <h2 className="form-section-title" style={{ marginTop: 24 }}>Issued lines</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th>#</th><th>Item</th><th className="text-right">Issued (base)</th><th className="text-right">Returned</th><th className="text-right">Fitted</th><th className="text-right">Outstanding</th><th>Serial</th><th>Location</th></tr>
          </thead>
          <tbody>
            {lines.map((row) => (
              <tr key={row.line.Id}>
                <td className="mono">{row.line.LineNumber}</td>
                <td><span className="mono">{row.itemCode}</span>{row.itemName ? ` — ${row.itemName}` : ''}</td>
                <td className="text-right mono">{row.line.QuantityBase}</td>
                <td className="text-right mono">{acceptedReturned.get(row.line.Id) ?? 0}</td>
                <td className="text-right mono">{fittedByLine.get(row.line.Id) ?? 0}</td>
                <td className="text-right mono">{row.outstanding}</td>
                <td className="mono">{row.line.StoredSerialNumber ?? (row.line.InventorySerialId ? row.line.InventorySerialId.slice(0, 8) + '…' : '—')}</td>
                <td className="mono">{row.line.WarehouseConditionLocationId.slice(0, 8)}…</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <h2 className="form-section-title" style={{ marginTop: 24 }}>Returns ({returns.length})</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th>Return</th><th>Declared</th><th>By</th><th className="text-right">Lines</th><th className="text-right">Returned</th><th>Status</th><th>Accepted</th><th /></tr>
          </thead>
          <tbody>
            {returns.length === 0 && <tr><td colSpan={8} className="table-empty">No returns declared yet.</td></tr>}
            {returns.map((row) => {
              const mine = row.ReturnedByEmployeeId === me?.EmployeeId
              return (
                <tr key={row.Id}>
                  <td className="mono">{row.ReturnNumber}</td>
                  <td>{new Date(row.DeclaredAt).toLocaleString()}</td>
                  <td className="mono">{mine ? `${me?.EmployeeCode} (you)` : row.ReturnedByEmployeeId}</td>
                  <td className="text-right mono">{row.Lines.length}</td>
                  <td className="text-right mono">{row.Lines.reduce((sum, line) => sum + line.ReturnedQuantityBase, 0)}</td>
                  <td><StatusBadge value={row.Status} /></td>
                  <td>{row.AcceptedAt ? `${new Date(row.AcceptedAt).toLocaleString()} · ${row.AcceptedActorRoleCode}` : '—'}</td>
                  <td>
                    {row.Status === 'SUBMITTED' && canAccept && !mine && (
                      <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void accept(row)}>Accept</button>
                    )}
                    {row.Status === 'SUBMITTED' && mine && <span className="field-hint">waiting for Stores</span>}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {declaring && (
        <MaterialReturnFormModal
          issue={issue}
          lines={lines}
          onClose={() => setDeclaring(false)}
          onSaved={(result) => {
            setDeclaring(false)
            setNotice(`${result.ReturnNumber} declared. Stores must accept it before custody moves back.`)
            void load()
          }}
        />
      )}

      <p className="field-hint" style={{ marginTop: 16 }}>
        <a className="row-click" onClick={() => navigate('/stores/material-issues')}>← Back to Material Issues</a>
      </p>
    </div>
  )
}
