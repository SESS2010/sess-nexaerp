import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  JOB_SEARCH_MAX,
  formatInstant,
  listDispatchableJobOrders,
  recentMachineDeliveries,
  rememberMachineDelivery,
} from '../../api/machineDeliveries'
import type { MachineDeliveryJobOrderCandidate } from '../../types/machineDelivery'
import { MACHINE_DELIVERY_PAGE } from '../../types/machineDelivery'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession } from '../auth/SessionContext'
import { MachineDeliveryDispatchModal } from './MachineDeliveryDispatchModal'

/** Within the API's 1-200 clamp. */
const PAGE_SIZE = 25

export function MachineDeliveryListPage() {
  const navigate = useNavigate()
  const { can, loading: sessionLoading } = useSession()
  // The FAT-ready job list and both writes need Issue; View alone opens a DC.
  const canIssue = can(MACHINE_DELIVERY_PAGE, 'issue')

  const [rows, setRows] = useState<MachineDeliveryJobOrderCandidate[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [applied, setApplied] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [dispatching, setDispatching] = useState<MachineDeliveryJobOrderCandidate | null>(null)
  const [recent] = useState(() => recentMachineDeliveries())

  const load = useCallback(async () => {
    if (!canIssue) return
    setLoading(true)
    setError(null)
    try {
      const data = await listDispatchableJobOrders({ page, pageSize: PAGE_SIZE, search: applied || undefined })
      setRows(data.Items)
      setTotalCount(data.TotalCount)
    } catch (err) {
      setRows([])
      setTotalCount(0)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [canIssue, page, applied])

  useEffect(() => {
    void load()
  }, [load])

  const apply = () => {
    setPage(1)
    setApplied(search.trim().slice(0, JOB_SEARCH_MAX))
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))
  const knownByJob = new Map(recent.map((entry) => [entry.JobOrderId, entry]))

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Machine Delivery Challan</h1>
          <p className="page-sub">
            Dispatch a FAT-ready machine, then record the customer's signature. One DC per job order; a DC cannot be
            corrected, cancelled or re-dispatched.
          </p>
        </div>
      </div>

      {canIssue && (
        <>
          <h2>Machines ready to dispatch</h2>
          <div className="toolbar">
            <input
              className="input search"
              placeholder="Machine serial, job order or customer…"
              value={search}
              maxLength={JOB_SEARCH_MAX}
              onChange={(event) => setSearch(event.target.value)}
              onKeyDown={(event) => { if (event.key === 'Enter') apply() }}
            />
            <button type="button" className="btn btn-ghost" onClick={apply}>Search</button>
            <div className="spacer" />
            <div className="pager">
              <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
              <span className="pager-label">Page {page} of {totalPages} · {totalCount} total</span>
              <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
            </div>
          </div>

          <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load the FAT-ready machines." />

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Machine serial</th>
                  <th>Job order</th>
                  <th>Model</th>
                  <th>Customer</th>
                  <th>FAT</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {loading && <tr><td colSpan={6} className="table-empty">Loading…</td></tr>}
                {!loading && rows.length === 0 && !error && (
                  <tr><td colSpan={6} className="table-empty">No FAT-ready machine with a customer PO{applied ? ' matches this search' : ''}.</td></tr>
                )}
                {!loading && rows.map((row) => {
                  const known = knownByJob.get(row.JobOrderId)
                  return (
                    <tr key={row.JobOrderId}>
                      <td className="mono">{row.MachineSerial}</td>
                      <td className="mono">{row.JobOrderNumber}</td>
                      <td>{row.MachineModel}</td>
                      <td>{row.CustomerName}</td>
                      <td><span className="badge badge-ok">{row.FatReadinessStatus}</span></td>
                      <td className="text-right">
                        {known ? (
                          <Link to={`/stores/machine-deliveries/${known.Id}`} className="btn btn-ghost">
                            Open DC <span className="mono">{known.DcNumber}</span>
                          </Link>
                        ) : (
                          <button type="button" className="btn btn-primary" onClick={() => setDispatching(row)}>
                            Dispatch…
                          </button>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
          <p className="field-hint">
            The server lists every FAT-ready job with a customer PO, including a job that already has its DC. If the
            dispatch is refused with "This job already has a machine DC", the DC exists: open it from the notification
            link or from the list below on the browser it was dispatched from.
          </p>
        </>
      )}

      {!canIssue && !sessionLoading && (
        <div className="alert">
          Your role can view a machine DC but not dispatch or sign one. Open a DC from its link.
        </div>
      )}

      <h2>DCs opened on this browser</h2>
      <p className="field-hint">
        There is no DC register on the server yet. DCs dispatched or opened on this browser, in this company, are kept here.
      </p>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>DC number</th>
              <th>Machine serial</th>
              <th>Customer</th>
              <th>Last opened</th>
            </tr>
          </thead>
          <tbody>
            {recent.length === 0 && (
              <tr><td colSpan={4} className="table-empty">None yet.</td></tr>
            )}
            {recent.map((entry) => (
              <tr key={entry.Id} className="row-click" onClick={() => navigate(`/stores/machine-deliveries/${entry.Id}`)}>
                <td className="mono">{entry.DcNumber}</td>
                <td className="mono">{entry.MachineSerial}</td>
                <td>{entry.CustomerName}</td>
                <td>{formatInstant(entry.SeenAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {dispatching && (
        <MachineDeliveryDispatchModal
          job={dispatching}
          onClose={() => setDispatching(null)}
          onDispatched={(dc) => {
            rememberMachineDelivery(dc)
            setDispatching(null)
            navigate(`/stores/machine-deliveries/${dc.Id}`, { state: { dispatched: true } })
          }}
        />
      )}
    </div>
  )
}
