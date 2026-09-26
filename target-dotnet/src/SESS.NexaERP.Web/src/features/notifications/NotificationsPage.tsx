import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listNotifications, markNotificationRead, notificationLink } from '../../api/notifications'
import type { InAppNotification } from '../../types/notification'
import { ErrorAlert } from '../../components/ErrorAlert'

const PAGE_SIZE = 25

const TYPE_LABEL: Record<string, string> = {
  UNUSED_MATERIAL_OVERDUE: 'Material overdue',
  QC_AGEING_OVERDUE: 'QC overdue',
  MACHINE_NON_RETURNABLE_DISPATCH: 'Machine dispatched',
}

/** Server instants carry an offset; show them in the browser's local time. */
function localTime(value: string | null): string {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString()
}

/**
 * GET /api/v1/notifications: the signed-in employee's own in-app
 * notifications in the selected company, newest first. Opening one marks it
 * read (one-way) and follows it to its document; unknown types show text only.
 */
export function NotificationsPage() {
  const navigate = useNavigate()
  const [rows, setRows] = useState<InAppNotification[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [unreadOnly, setUnreadOnly] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await listNotifications({ unreadOnly, page, pageSize: PAGE_SIZE })
      setRows(result.Items)
      setTotal(result.TotalCount)
    } catch (err) {
      setRows([])
      setTotal(0)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [unreadOnly, page])

  useEffect(() => {
    void load()
  }, [load])

  const markRead = useCallback(async (row: InAppNotification): Promise<boolean> => {
    if (row.ReadAt) return true
    setBusyId(row.RecipientId)
    setError(null)
    try {
      await markNotificationRead(row.RecipientId)
      const now = new Date().toISOString()
      setRows((current) => current.map((item) => (item.RecipientId === row.RecipientId ? { ...item, ReadAt: now } : item)))
      return true
    } catch (err) {
      setError(err)
      return false
    } finally {
      setBusyId(null)
    }
  }, [])

  const open = useCallback(async (row: InAppNotification) => {
    const link = notificationLink(row)
    // Following the link does not depend on the acknowledgement succeeding.
    await markRead(row)
    if (link) navigate(link)
  }, [markRead, navigate])

  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Notifications</h1>
          <p className="page-sub">
            Your in-app notifications in this company. Opening one marks it read and takes you to the document. Marking read does not close the underlying task.
          </p>
        </div>
      </div>

      <div className="toolbar">
        <label className="pager-label">
          <input
            type="checkbox"
            checked={unreadOnly}
            onChange={(event) => { setUnreadOnly(event.target.checked); setPage(1) }}
          />{' '}
          Only unread
        </label>
        <button type="button" className="btn btn-ghost" onClick={() => void load()} disabled={loading}>↻ Refresh</button>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={loading || page <= 1} onClick={() => setPage((p) => p - 1)}>‹</button>
          <span className="pager-label">Page {page} of {pages} · {total} notification(s)</span>
          <button type="button" className="btn btn-ghost" disabled={loading || page >= pages} onClick={() => setPage((p) => p + 1)}>›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Notifications could not be loaded." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th></th><th>When</th><th>Notification</th><th>Document</th><th>State</th><th></th></tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={6} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={6} className="table-empty">{unreadOnly ? 'No unread notifications.' : 'No notifications.'}</td></tr>
            )}
            {!loading && rows.map((row) => {
              const link = notificationLink(row)
              const unread = !row.ReadAt
              return (
                <tr
                  key={row.RecipientId}
                  className={link ? 'row-click' : undefined}
                  onClick={link ? () => void open(row) : undefined}
                  style={unread ? { fontWeight: 600 } : undefined}
                >
                  <td aria-label={unread ? 'Unread' : 'Read'}>{unread ? '●' : ''}</td>
                  <td>{localTime(row.AvailableAt)}</td>
                  <td>
                    <div>{row.Title}</div>
                    <div className="page-sub" style={{ fontWeight: 400 }}>{row.Body}</div>
                    <div className="page-sub" style={{ fontWeight: 400 }}>{TYPE_LABEL[row.EventType] ?? row.EventType}</div>
                  </td>
                  <td className="mono">
                    {row.SourceReference || '—'}
                    {!link && <div className="page-sub" style={{ fontWeight: 400 }}>No screen for this notification</div>}
                  </td>
                  <td>
                    <span className={`badge ${row.Status === 'ACTIVE' ? 'badge-warn' : 'badge-muted'}`}>
                      {row.Status === 'ACTIVE' ? 'Open' : row.Status === 'COMPLETED' ? 'Resolved' : row.Status}
                    </span>
                    {row.ReadAt && <div className="page-sub" style={{ fontWeight: 400 }}>Read {localTime(row.ReadAt)}</div>}
                  </td>
                  <td className="text-right" onClick={(event) => event.stopPropagation()}>
                    {unread && (
                      <button
                        type="button"
                        className="btn btn-ghost"
                        disabled={busyId === row.RecipientId}
                        onClick={() => void markRead(row)}
                      >
                        Mark read
                      </button>
                    )}
                    {link && (
                      <button type="button" className="btn btn-ghost" onClick={() => void open(row)}>Open</button>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}
