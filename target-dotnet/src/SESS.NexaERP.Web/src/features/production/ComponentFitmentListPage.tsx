import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { listComponentFitments, reverseComponentFitment } from '../../api/production'
import { newIdempotencyKey } from '../../api/stores'
import type { ComponentFitmentSummary } from '../../types/production'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { ComponentFitmentFormModal } from './ComponentFitmentFormModal'

const PAGE_SIZE = 25

/**
 * GET /api/v1/production/component-fitments. A fitment is the moment a
 * component issued to an engineer is built into the machine: CONSUMPTION_OUT
 * is posted here, never at issue, and the entry lands in the job's generated
 * Actual BOM. Reversal needs a reason and posts its own negative entry; a
 * reversed fitment can be re-verified by a new fitment that references it.
 */
export function ComponentFitmentListPage() {
  const { can, me } = useSession()
  const [params, setParams] = useSearchParams()
  const jobOrderId = params.get('jobOrderId') ?? ''
  const [rows, setRows] = useState<ComponentFitmentSummary[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [activeOnly, setActiveOnly] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [create, setCreate] = useState<{ reverifies?: ComponentFitmentSummary } | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listComponentFitments({ page, pageSize: PAGE_SIZE, jobOrderId: jobOrderId || undefined, activeOnly: activeOnly || undefined })
      setRows(data.Items ?? [])
      setTotal(data.TotalCount)
    } catch (err) {
      setRows([])
      setTotal(0)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [page, jobOrderId, activeOnly])

  useEffect(() => {
    void load()
  }, [load])

  const reverse = async (row: ComponentFitmentSummary) => {
    if (!reason.trim()) {
      setError('A reason is required to reverse a fitment — it is recorded on the reversal.')
      return
    }
    setError(null)
    setNotice('')
    setBusy(true)
    try {
      const result = await reverseComponentFitment(row.Id, { Reason: reason.trim(), IdempotencyKey: newIdempotencyKey('fitment-reverse') })
      setReason('')
      setNotice(`${result.FitmentNumber} reversed${result.IsSelfReversal ? ' (self-reversal, recorded as such)' : ''}. Custody is back with the engineer; re-verify with a new fitment when ready.`)
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const canCreate = can(PAGE_KEYS.componentFitments, 'create')
  const canReverse = can(PAGE_KEYS.componentFitments, 'cancel')

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Component Fitments</h1>
          <p className="page-sub">
            Confirm that an issued component is built into the machine. Consumption is posted at fitment, never at issue, and every fitment becomes an Actual BOM entry with its GRN and vendor-bill provenance.
          </p>
        </div>
        <div className="action-row">
          {canCreate && <button type="button" className="btn btn-primary" onClick={() => setCreate({})}>+ Confirm fitment</button>}
        </div>
      </div>

      <div className="toolbar">
        <input
          className="input search mono"
          placeholder="Job order id (GUID) — or open a job order and click Fitments"
          value={jobOrderId}
          onChange={(event) => { setPage(1); setParams(event.target.value ? { jobOrderId: event.target.value } : {}) }}
        />
        <label className="pager-label"><input type="checkbox" checked={activeOnly} onChange={(event) => { setActiveOnly(event.target.checked); setPage(1) }} /> Hide reversed</label>
        {canReverse && (
          <input className="input search" placeholder="Reason for a reversal (mandatory)" value={reason} onChange={(event) => setReason(event.target.value)} />
        )}
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages} · {total} total</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load fitments." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Fitment</th>
              <th>Job Order</th>
              <th>Item</th>
              <th className="text-right">Qty (base)</th>
              <th>Fitted at</th>
              <th>Confirmed by</th>
              <th>State</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={8} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={8} className="table-empty">No fitments{jobOrderId ? ' for this job order' : ''} yet.</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr key={row.Id}>
                <td className="mono">{row.FitmentNumber}{row.ReverifiesFitmentId ? <span className="field-hint">re-verifies an earlier fitment</span> : null}</td>
                <td className="mono">{row.JobOrderNumber}</td>
                <td className="mono">{row.ItemCode}</td>
                <td className="text-right mono">{row.QuantityBase}</td>
                <td>{new Date(row.FittedAt).toLocaleString()}</td>
                <td><span className="mono">{row.ConfirmedByEmployeeId === me?.EmployeeId ? `${me?.EmployeeCode} (you)` : row.ConfirmedByEmployeeId}</span> · {row.ActorRoleCode}</td>
                <td>
                  {row.IsReversed
                    ? <><StatusBadge value="REVERSED" />{row.IsSelfReversal ? <> <StatusBadge value="Self-reversal" /></> : null}<span className="field-hint">{row.ReversalReason}</span></>
                    : <StatusBadge value="CONFIRMED" />}
                </td>
                <td>
                  <span className="action-row">
                    {canReverse && !row.IsReversed && (
                      <button type="button" className="btn btn-warn" disabled={busy} onClick={() => void reverse(row)}>Reverse</button>
                    )}
                    {canCreate && row.IsReversed && (
                      <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setCreate({ reverifies: row })}>Re-verify</button>
                    )}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {create && (
        <ComponentFitmentFormModal
          jobOrderId={create.reverifies?.JobOrderId ?? jobOrderId}
          reverifies={create.reverifies}
          onClose={() => setCreate(null)}
          onSaved={(result) => {
            setCreate(null)
            setNotice(`${result.FitmentNumber} confirmed: ${result.QuantityBase} × ${result.ItemCode} fitted to ${result.JobOrderNumber}.`)
            if (!jobOrderId) setParams({ jobOrderId: result.JobOrderId })
            else void load()
          }}
        />
      )}
    </div>
  )
}
