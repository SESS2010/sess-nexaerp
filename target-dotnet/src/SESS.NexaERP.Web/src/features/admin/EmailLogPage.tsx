// E-mail log /admin/email (R1, email-lite). Reads GET /api/v1/email/outbox
// only; the fixtures in docs/installation/tracking-mocks/email-log/ are never
// imported here. A server without the route answers 404, which is shown as an
// error (the log is not "empty" then). Bodies and credentials are not part of
// the wire shape and are never requested.

import { useCallback, useEffect, useState } from 'react'
import { EMAIL_LOG_PAGE_KEY, canRetryStatus, listEmailOutbox, retryEmailOutboxItem } from '../../api/email'
import type { EmailOutboxItem } from '../../types/email'
import { EMAIL_OUTBOX_STATUSES } from '../../types/email'
import { useSession } from '../auth/SessionContext'
import { ErrorAlert } from '../../components/ErrorAlert'
import { formatTimestamp } from '../../utils/dashboardFormat'
import { EmailTestModal } from './EmailTestModal'

const PAGE_SIZE = 50
/** Contract: display times in Asia/Kolkata whatever the browser's zone. */
const TIME_ZONE = 'Asia/Kolkata'

const EVENT_LABEL: Record<string, string> = {
  PO_ISSUED: 'PO issued',
  TRACKING_DIGEST: 'Pending digest',
  TEST: 'Test message',
}

function statusTone(status: string): string {
  switch (status) {
    case 'SENT':
      return 'badge-ok'
    case 'QUEUED':
    case 'SENDING':
    case 'PENDING_COMPOSE':
      return 'badge-info'
    case 'FAILED':
    case 'BLOCKED_ALLOWLIST':
      return 'badge-warn'
    case 'DEAD':
      return 'badge-error'
    default:
      return 'badge-muted'
  }
}

function when(value: string | null): string {
  return value ? formatTimestamp(value, TIME_ZONE) : '—'
}

function recipients(row: EmailOutboxItem): string {
  const to = row.To.length ? row.To.join(', ') : '—'
  return row.Cc.length ? `${to} (cc ${row.Cc.join(', ')})` : to
}

export function EmailLogPage() {
  const { can } = useSession()
  // Update is TD-only; IT_MANAGER holds View alone and sees no actions.
  const canAct = can(EMAIL_LOG_PAGE_KEY, 'update')

  const [rows, setRows] = useState<EmailOutboxItem[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [actionError, setActionError] = useState<unknown>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [testOpen, setTestOpen] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await listEmailOutbox({ status: status || undefined, page, pageSize: PAGE_SIZE })
      setRows(result.Items)
      setTotal(result.Total)
    } catch (err) {
      setRows([])
      setTotal(0)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [status, page])

  useEffect(() => {
    void load()
  }, [load])

  const retry = async (row: EmailOutboxItem) => {
    setBusyId(row.Id)
    setActionError(null)
    setNotice(null)
    try {
      await retryEmailOutboxItem(row.Id)
      setNotice(`Retry queued for ${row.Subject ?? row.IdempotencyKey}.`)
      await load()
    } catch (err) {
      setActionError(err)
    } finally {
      setBusyId(null)
    }
  }

  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>E-mail log</h1>
          <p className="page-sub">
            Every e-mail the ERP queued for this company: PO issues to vendors and the pending digest. Times are
            Asia/Kolkata. Failed messages retry on their own (1, 5, 15 and 60 minutes, then DEAD after five
            attempts); a retry here re-queues a FAILED or DEAD message once.
          </p>
        </div>
        {canAct && (
          <div className="action-row">
            <button type="button" className="btn btn-primary" onClick={() => setTestOpen(true)} disabled={loading}>
              Send test e-mail
            </button>
          </div>
        )}
      </div>

      <div className="toolbar">
        <select
          className="input"
          value={status}
          onChange={(event) => { setStatus(event.target.value); setPage(1) }}
          aria-label="Status"
        >
          <option value="">All statuses</option>
          {EMAIL_OUTBOX_STATUSES.map((value) => <option key={value} value={value}>{value}</option>)}
        </select>
        <button type="button" className="btn btn-ghost" onClick={() => void load()} disabled={loading}>↻ Refresh</button>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={loading || page <= 1} onClick={() => setPage((p) => p - 1)}>‹</button>
          <span className="pager-label">Page {page} of {pages} · {total} message(s)</span>
          <button type="button" className="btn btn-ghost" disabled={loading || page >= pages} onClick={() => setPage((p) => p + 1)}>›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="The e-mail log could not be loaded." />
      <ErrorAlert error={actionError} fallback="The action was not accepted." />
      {notice && <div className="alert alert-info" role="status">{notice}</div>}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Event</th>
              <th>Subject</th>
              <th>Recipients</th>
              <th>Status</th>
              <th className="text-right">Attempts</th>
              <th>Created</th>
              <th>Next attempt</th>
              <th>Sent</th>
              <th>Error</th>
              {canAct && <th></th>}
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={canAct ? 10 : 9} className="table-empty">Loading…</td></tr>}
            {!loading && !error && rows.length === 0 && (
              <tr><td colSpan={canAct ? 10 : 9} className="table-empty">{status ? `No ${status} messages.` : 'No e-mails have been queued yet.'}</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr key={row.Id}>
                <td>
                  <div>{EVENT_LABEL[row.EventType] ?? row.EventType}</div>
                  <div className="page-sub mono">{row.SourceEntityType}</div>
                </td>
                <td>{row.Subject ?? <span className="page-sub">Not composed yet</span>}</td>
                <td className="mono">{recipients(row)}</td>
                <td><span className={`badge ${statusTone(row.Status)}`}>{row.Status}</span></td>
                <td className="text-right tabular-nums">{row.Attempts}</td>
                <td>{when(row.CreatedAt)}</td>
                <td>{when(row.NextAttemptAt)}</td>
                <td>{when(row.SentAt)}</td>
                <td>{row.LastError ?? '—'}</td>
                {canAct && (
                  <td className="text-right">
                    {canRetryStatus(row.Status) && (
                      <button
                        type="button"
                        className="btn btn-ghost"
                        disabled={busyId !== null}
                        onClick={() => void retry(row)}
                      >
                        {busyId === row.Id ? 'Retrying…' : 'Retry'}
                      </button>
                    )}
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {testOpen && (
        <EmailTestModal
          onClose={() => setTestOpen(false)}
          onSent={(to) => {
            setTestOpen(false)
            setNotice(`Test e-mail to ${to} was queued.`)
            void load()
          }}
        />
      )}
    </div>
  )
}
