// Tracking-lite (R1) Pending page: GET /api/v1/tracking/pending, one table,
// overdue first then oldest first (the API's order; nothing is re-sorted
// here). Every filter lives in the URL (queue, docType, overdueOnly, mine,
// page) so a home tile can open the page already filtered and a reload keeps
// it; nothing is kept in localStorage. Row links go through trackingRowLink,
// never the API's Link field.

import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { getTrackingSummary, historyAvailability, listTrackingPending } from '../../api/tracking'
import type { HistoryAvailability } from '../../api/tracking'
import { ApiError } from '../../api/client'
import type { TrackingPendingPage as PendingPage, TrackingPendingRow } from '../../types/tracking'
import { ErrorAlert } from '../../components/ErrorAlert'
import { docTypeWords, roleWords } from '../../components/HistoryPanel'
import { StatusBadge } from '../employees/StatusBadge'
import { formatInstantIst } from '../../print/format'
import { TRACKING_DOC_TYPE_ORDER, TRACKING_QUEUES, groupQueuesByDocType } from './trackingQueues'
import type { TrackingQueueInfo } from './trackingQueues'
import { trackingRowLink } from './trackingLinks'

const PAGE_SIZE = 50

interface Filters {
  queue: string
  docType: string
  overdueOnly: boolean
  mine: boolean
  page: number
}

function readFilters(params: URLSearchParams): Filters {
  const page = Number.parseInt(params.get('page') ?? '1', 10)
  return {
    queue: params.get('queue') ?? '',
    docType: params.get('docType') ?? '',
    overdueOnly: params.get('overdueOnly') === '1' || params.get('overdueOnly') === 'true',
    mine: params.get('mine') === '1' || params.get('mine') === 'true',
    page: Number.isFinite(page) && page >= 1 ? page : 1,
  }
}

function writeFilters(filters: Filters): URLSearchParams {
  const params = new URLSearchParams()
  if (filters.queue) params.set('queue', filters.queue)
  if (filters.docType) params.set('docType', filters.docType)
  if (filters.overdueOnly) params.set('overdueOnly', '1')
  if (filters.mine) params.set('mine', '1')
  if (filters.page > 1) params.set('page', String(filters.page))
  return params
}

/** "Name (Code)" when one person is named, otherwise the role in words. */
function pendingWith(row: TrackingPendingRow): string {
  if (row.PendingWithEmployeeName || row.PendingWithEmployeeCode) {
    const name = row.PendingWithEmployeeName ?? ''
    const code = row.PendingWithEmployeeCode ?? ''
    return name && code ? `${name} (${code})` : name || code
  }
  if (row.PendingWithRoleName) return row.PendingWithRoleName
  if (row.PendingWithRole) return roleWords(row.PendingWithRole)
  return '—'
}

export function PendingPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const filters = useMemo(() => readFilters(searchParams), [searchParams])

  const [data, setData] = useState<PendingPage | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [reloadTick, setReloadTick] = useState(0)
  // The queue filter's options: the summary when the server gives one, else the static sixteen.
  const [queues, setQueues] = useState<TrackingQueueInfo[]>(TRACKING_QUEUES)

  useEffect(() => {
    let cancelled = false
    getTrackingSummary()
      .then((tiles) => {
        if (cancelled) return
        const ready = tiles.filter((tile) => tile.State === 'READY')
        if (ready.length > 0) {
          setQueues(ready.map((tile) => ({ DocType: tile.DocType as TrackingQueueInfo['DocType'], Queue: tile.Queue, Title: tile.Title })))
        }
      })
      .catch(() => { /* the static list stays; the pending call reports the real problem */ })
    return () => { cancelled = true }
  }, [])

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)
    listTrackingPending({
      docType: filters.docType || undefined,
      queue: filters.queue || undefined,
      overdueOnly: filters.overdueOnly,
      mine: filters.mine,
      page: filters.page,
      pageSize: PAGE_SIZE,
    })
      .then((page) => { if (!cancelled) setData(page) })
      .catch((err: unknown) => { if (!cancelled) { setData(null); setError(err) } })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [filters, reloadTick])

  const update = useCallback(
    (patch: Partial<Filters>) => {
      // Any filter change goes back to page 1 unless the change is the page itself.
      const next: Filters = { ...filters, page: 1, ...patch }
      setSearchParams(writeFilters(next))
    },
    [filters, setSearchParams],
  )

  const reload = useCallback(() => setReloadTick((tick) => tick + 1), [])

  // A queue belongs to one doc type: choosing a queue clears a conflicting doc type and vice versa.
  const onQueue = (queue: string) => update({ queue, docType: '' })
  const onDocType = (docType: string) => update({ docType, queue: '' })

  const grouped = useMemo(() => groupQueuesByDocType(queues), [queues])
  const total = data?.Total ?? 0
  const pages = Math.max(1, Math.ceil(total / (data?.PageSize ?? PAGE_SIZE)))
  const overdueOnPage = data?.Items.filter((row) => row.IsOverdue).length ?? 0
  const overdueLabel = data && total > data.Items.length ? `${overdueOnPage} overdue on this page` : `${overdueOnPage} overdue`

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Pending documents</h1>
          <p className="page-sub">
            {data
              ? `${total} pending · ${overdueLabel} · generated ${formatInstantIst(data.GeneratedAt)}`
              : loading
                ? 'Loading…'
                : 'Every open document waiting on someone, overdue first.'}
          </p>
        </div>
      </div>

      <div className="toolbar">
        <select className="input" value={filters.queue} onChange={(event) => onQueue(event.target.value)} aria-label="Queue">
          <option value="">All queues</option>
          {grouped.map((group) => (
            <optgroup key={group.docType} label={docTypeWords(group.docType)}>
              {group.queues.map((queue) => (
                <option key={queue.Queue} value={queue.Queue}>{queue.Title}</option>
              ))}
            </optgroup>
          ))}
        </select>
        <select className="input" value={filters.docType} onChange={(event) => onDocType(event.target.value)} aria-label="Document type">
          <option value="">All document types</option>
          {TRACKING_DOC_TYPE_ORDER.map((docType) => (
            <option key={docType} value={docType}>{docTypeWords(docType)}</option>
          ))}
        </select>
        <label className="tracking-check">
          <input type="checkbox" checked={filters.overdueOnly} onChange={(event) => update({ overdueOnly: event.target.checked })} />
          Overdue only
        </label>
        <label className="tracking-check">
          <input type="checkbox" checked={filters.mine} onChange={(event) => update({ mine: event.target.checked })} />
          Mine
        </label>
        <span className="spacer" />
        <button type="button" className="btn btn-ghost" onClick={reload} disabled={loading}>↻ Refresh</button>
      </div>

      <PendingError error={error} onReload={reload} />

      {data && (
        <>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Document</th>
                  <th>Status</th>
                  <th>Pending with</th>
                  <th>Waiting since</th>
                  <th>Age</th>
                </tr>
              </thead>
              <tbody>
                {data.Items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="tracking-empty">Nothing is pending for these filters.</td>
                  </tr>
                )}
                {data.Items.map((row) => (
                  <tr key={`${row.Queue}:${row.DocumentId}`} className={row.IsOverdue ? 'tracking-overdue' : undefined}>
                    <td>
                      <div className="tracking-doc-type">{docTypeWords(row.DocType)}</div>
                      <Link to={trackingRowLink(row)} className="tracking-doc-number mono">{row.Number}</Link>
                    </td>
                    <td>
                      <StatusBadge value={row.Status} />
                      {row.IsOverdue && <span className="badge badge-error tracking-overdue-badge">Overdue</span>}
                    </td>
                    <td>{pendingWith(row)}</td>
                    <td className="mono">{formatInstantIst(row.WaitingSince)}</td>
                    <td>
                      <span className="mono">{row.AgeDays} day{row.AgeDays === 1 ? '' : 's'}</span>
                      <span className="field-hint">overdue after {row.OverdueAfterDays}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="pager tracking-pager">
            <button type="button" className="btn btn-ghost" disabled={filters.page <= 1 || loading} onClick={() => update({ page: filters.page - 1 })}>‹ Prev</button>
            <span className="pager-label">Page {data.Page} of {pages} · {total} total · {data.PageSize} per page</span>
            <button type="button" className="btn btn-ghost" disabled={filters.page >= pages || loading} onClick={() => update({ page: filters.page + 1 })}>Next ›</button>
          </div>
        </>
      )}
    </div>
  )
}

const UNAVAILABLE: Record<Exclude<HistoryAvailability, 'error'>, string> = {
  'no-tracking-page': 'Pending documents are not permitted for your employee and selected company.',
  'not-deployed': 'This server does not have tracking yet.',
  // The pending list has no per-document scope, so a NOT_FOUND 404 here can only be a missing route.
  'out-of-scope': 'This server does not have tracking yet.',
}

function PendingError({ error, onReload }: { error: unknown; onReload: () => void }) {
  if (!error) return null
  const availability = historyAvailability(error)
  if (availability === 'error') return <ErrorAlert error={error} onReload={onReload} fallback="Could not load pending documents." />
  const serverMessage = error instanceof ApiError && availability === 'no-tracking-page' ? error.message : null
  return (
    <div className={`alert ${availability === 'no-tracking-page' ? 'alert-warn' : 'alert-info'} tracking-alert`} role="alert">
      <div className="alert-title">{UNAVAILABLE[availability]}</div>
      {serverMessage && serverMessage !== UNAVAILABLE[availability] && <p className="alert-detail mono">{serverMessage}</p>}
      {error instanceof ApiError && error.traceId && <p className="alert-detail mono">Trace {error.traceId}</p>}
    </div>
  )
}
