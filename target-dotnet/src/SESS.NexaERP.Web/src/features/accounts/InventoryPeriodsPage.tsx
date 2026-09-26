import { Fragment, useCallback, useEffect, useMemo, useState } from 'react'
import {
  INVENTORY_PERIODS_PAGE_KEY,
  formatPeriodDate,
  formatPeriodInstant,
  inclusiveDays,
  listInventoryPeriods,
  todayIndia,
} from '../../api/inventoryPeriods'
import type { InventoryPeriodView } from '../../types/inventoryPeriod'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession } from '../auth/SessionContext'
import { InventoryPeriodOpenModal } from './InventoryPeriodOpenModal'
import { InventoryPeriodCloseModal } from './InventoryPeriodCloseModal'

type StatusFilter = 'ALL' | 'OPEN' | 'CLOSED'

/**
 * Inventory periods: the CFO-governed calendar that stock adjustments post
 * into (docs/installation/inventory-period-foundation.md). The CFO opens a
 * period with a code, name and inclusive dates, and later closes it. Periods
 * never overlap (closed ones included), dates and identity never change, and
 * a closed period never reopens. A stock adjustment needs an OPEN period
 * covering its date.
 */
export function InventoryPeriodsPage() {
  const { can } = useSession()
  const canView = can(INVENTORY_PERIODS_PAGE_KEY, 'view')
  const canDecide = can(INVENTORY_PERIODS_PAGE_KEY, 'approve')

  const [periods, setPeriods] = useState<InventoryPeriodView[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [status, setStatus] = useState<StatusFilter>('ALL')
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [opening, setOpening] = useState(false)
  const [closing, setClosing] = useState<InventoryPeriodView | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setPeriods(await listInventoryPeriods())
    } catch (err) {
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const sorted = useMemo(
    () => [...periods].sort((a, b) => (a.StartDate < b.StartDate ? 1 : a.StartDate > b.StartDate ? -1 : 0)),
    [periods],
  )
  const visible = sorted.filter((p) => status === 'ALL' || p.Status === status)
  const openCount = periods.filter((p) => p.Status === 'OPEN').length
  const today = todayIndia()
  const current = periods.find((p) => p.Status === 'OPEN' && p.StartDate <= today && today <= p.EndDate)

  const afterOpen = async (period: InventoryPeriodView) => {
    setOpening(false)
    setNotice(
      period.Replayed
        ? `Period ${period.Code} was already opened by this same request; nothing new was recorded.`
        : `Period ${period.Code} opened: ${formatPeriodDate(period.StartDate)} to ${formatPeriodDate(period.EndDate)}.`,
    )
    await load()
  }

  const afterClose = async (period: InventoryPeriodView) => {
    setClosing(null)
    setNotice(
      period.Replayed
        ? `Period ${period.Code} was already closed by this same request; nothing new was recorded.`
        : `Period ${period.Code} closed. It can never be reopened, and no stock can be adjusted into it.`,
    )
    await load()
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Inventory Periods</h1>
          <p className="page-sub">
            The dated windows stock adjustments post into — opened and closed only by the Chief Financial Officer
          </p>
        </div>
        <div className="action-row">
          {canDecide && (
            <button type="button" className="btn btn-primary" onClick={() => { setNotice(''); setOpening(true) }}>
              + Open period
            </button>
          )}
        </div>
      </div>

      {!canView && !loading && !error && (
        <div className="alert alert-warn">Your role does not have the Inventory Periods view permission.</div>
      )}

      <div className="card" style={{ marginBottom: 16 }}>
        {current ? (
          <p>
            Today ({formatPeriodDate(today)}) falls in open period <strong className="mono">{current.Code}</strong> — {current.Name}.
            Stock adjustments dated today can be posted.
          </p>
        ) : (
          <p>
            <strong>No open inventory period covers today ({formatPeriodDate(today)}).</strong> A stock adjustment dated today
            will be refused until the CFO opens one.
          </p>
        )}
        <p className="field-hint">
          Periods cannot overlap, even with a closed period. Code, name and dates are fixed once opened, and a closed period is
          never reopened.
        </p>
      </div>

      <div className="toolbar">
        <select className="input" value={status} onChange={(event) => setStatus(event.target.value as StatusFilter)}>
          <option value="ALL">All statuses</option>
          <option value="OPEN">Open{openCount ? ` (${openCount})` : ''}</option>
          <option value="CLOSED">Closed</option>
        </select>
        <button type="button" className="btn btn-ghost" disabled={loading} onClick={() => void load()}>Refresh</button>
      </div>

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="The inventory periods could not be loaded." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>From</th>
              <th>To</th>
              <th className="text-right">Days</th>
              <th>Status</th>
              <th>Opened</th>
              <th>Closed</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={9} className="table-empty">Loading…</td></tr>}
            {!loading && visible.length === 0 && (
              <tr><td colSpan={9} className="table-empty">No inventory periods{status !== 'ALL' ? ' in this status' : ''}.</td></tr>
            )}
            {!loading && visible.map((period) => {
              const expanded = expandedId === period.Id
              const decisions = Array.isArray(period.Decisions) ? period.Decisions : []
              return (
                <Fragment key={period.Id}>
                  <tr>
                    <td className="mono">{period.Code}</td>
                    <td>{period.Name}</td>
                    <td className="mono">{formatPeriodDate(period.StartDate)}</td>
                    <td className="mono">{formatPeriodDate(period.EndDate)}</td>
                    <td className="text-right mono">{inclusiveDays(period.StartDate, period.EndDate) ?? '—'}</td>
                    <td><StatusBadge value={period.Status} /></td>
                    <td>
                      {formatPeriodInstant(period.CreatedAt)}
                      <div className="text-ink-faint text-[11.5px]">{period.CreatedBy}</div>
                    </td>
                    <td>{period.ClosedAt ? formatPeriodInstant(period.ClosedAt) : '—'}</td>
                    <td>
                      <div className="action-row">
                        <button type="button" className="btn btn-ghost" onClick={() => setExpandedId(expanded ? null : period.Id)}>
                          {expanded ? 'Hide history' : `History (${decisions.length})`}
                        </button>
                        {canDecide && period.Status === 'OPEN' && (
                          <button type="button" className="btn btn-warn" onClick={() => { setNotice(''); setClosing(period) }}>
                            Close…
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                  {expanded && (
                    <tr>
                      <td colSpan={9}>
                        {decisions.length === 0 ? (
                          <p className="field-hint">No decisions recorded.</p>
                        ) : (
                          <table className="table">
                            <thead>
                              <tr>
                                <th>Decision</th>
                                <th>Recorded</th>
                                <th>By</th>
                                <th>Assignment</th>
                                <th className="text-right">Version</th>
                                <th>Reason</th>
                              </tr>
                            </thead>
                            <tbody>
                              {decisions.map((decision) => (
                                <tr key={decision.Id}>
                                  <td><StatusBadge value={decision.Action} /></td>
                                  <td>{formatPeriodInstant(decision.RecordedAt)}</td>
                                  <td>{decision.RecordedBy}</td>
                                  <td className="mono">{decision.RoleAssignmentType}</td>
                                  <td className="text-right mono">{decision.PeriodVersion}</td>
                                  <td>{decision.Reason}</td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        )}
                      </td>
                    </tr>
                  )}
                </Fragment>
              )
            })}
          </tbody>
        </table>
      </div>

      {opening && (
        <InventoryPeriodOpenModal existing={periods} onClose={() => setOpening(false)} onOpened={(p) => void afterOpen(p)} />
      )}
      {closing && (
        <InventoryPeriodCloseModal
          period={closing}
          onClose={() => setClosing(null)}
          onClosed={(p) => void afterClose(p)}
          onReload={() => { setClosing(null); void load() }}
        />
      )}
    </div>
  )
}
