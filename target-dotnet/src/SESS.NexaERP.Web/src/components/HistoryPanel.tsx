// Tracking-lite (R1) "History" panel for document detail pages. One read of
// GET /api/v1/tracking/{docType}/{documentId}/history, rendered as a timeline
// newest first and grouped by Stage. The panel degrades quietly: a document
// outside the user's scope, a user without the Pending page, or a server that
// predates R1 each get one muted line instead of a red alert. The mock
// payloads live on integration/r1 (docs/installation/tracking-mocks) and are
// not committed here.

import { useCallback, useEffect, useState } from 'react'
import { getTrackingHistory, historyAvailability } from '../api/tracking'
import type { HistoryAvailability } from '../api/tracking'
import type { TrackingDocType, TrackingEvent, TrackingHistory } from '../types/tracking'
import { formatInstantIst } from '../print/format'
import { StatusBadge } from '../features/employees/StatusBadge'
import { ErrorAlert } from './ErrorAlert'

interface Props {
  docType: TrackingDocType
  /** The document's own Id; for 'QC' the GRN id. */
  documentId: string
  title?: string
}

const STAGE_WORDS: Record<string, string> = {
  PR: 'Purchase requisition',
  RFQ: 'RFQ',
  QUOTATION: 'Quotation',
  COMPARISON: 'Comparison',
  PO: 'Purchase order',
  GATE_ENTRY: 'Gate entry',
  GRN: 'Goods receipt',
  QC: 'Quality check',
  MIR: 'Material issue request',
  VENDOR_BILL: 'Vendor bill',
}

const ACTION_WORDS: Record<string, string> = {
  Create: 'Created',
  CREATED: 'Created',
  Submit: 'Submitted',
  SUBMITTED: 'Submitted',
  Approve: 'Approved',
  APPROVED: 'Approved',
  Reject: 'Rejected',
  REJECTED: 'Rejected',
  Issue: 'Issued',
  ISSUED: 'Issued',
  Recommend: 'Recommended',
  Verify: 'Verified',
  Amend: 'Amended',
  Cancel: 'Cancelled',
  CANCELLED: 'Cancelled',
  Finalize: 'Finalized',
  FINALIZED: 'Finalized',
  Reverse: 'Reversed',
  REVERSED: 'Reversed',
  Accept: 'Accepted',
  ACCEPTED: 'Accepted',
  QC_STARTED: 'QC started',
  QC_FINALIZED: 'QC finalized',
  QC_CORRECTED: 'QC corrected',
}

const ROLE_WORDS: Record<string, string> = {
  TECHNICAL_DIRECTOR: 'Technical Director',
  PURCHASE_MANAGER: 'Purchase Manager',
  PURCHASE_EXECUTIVE: 'Purchase Executive',
  STORES_EXECUTIVE: 'Stores Executive',
  STORES_ASSISTANT: 'Stores Assistant',
  QC_MANAGER: 'QC Manager',
  QC_INSPECTOR: 'QC Inspector',
  ACCOUNTS_MANAGER: 'Accounts Manager',
  ACCOUNTS_EXECUTIVE: 'Accounts Executive',
  DEPARTMENT_HEAD: 'Department Head',
}

/** TECHNICAL_DIRECTOR → "Technical Director"; keeps QC/PO/GRN/RFQ/MIR/PR upper-case. */
export function titleWords(code: string): string {
  const acronyms = new Set(['QC', 'PO', 'PR', 'GRN', 'RFQ', 'MIR', 'DC', 'TD', 'GST'])
  return code
    .split(/[_\s]+/)
    .filter(Boolean)
    .map((word) => {
      const upper = word.toUpperCase()
      if (acronyms.has(upper)) return upper
      return upper.charAt(0) + word.slice(1).toLowerCase()
    })
    .join(' ')
}

/** A tracking DocType (or history Stage) in words: GRN → "Goods receipt". Shared with the Pending page and home tiles. */
export function docTypeWords(docType: string): string {
  return STAGE_WORDS[docType] ?? titleWords(docType)
}

function stageWords(stage: string): string {
  return docTypeWords(stage)
}

function actionWords(action: string): string {
  return ACTION_WORDS[action] ?? titleWords(action)
}

/** A role code in words: QC_MANAGER → "QC Manager". Shared with the Pending page. */
export function roleWords(role: string): string {
  return ROLE_WORDS[role] ?? titleWords(role)
}

function who(event: TrackingEvent): string | null {
  if (event.EmployeeName && event.EmployeeCode) return `${event.EmployeeName} (${event.EmployeeCode})`
  if (event.EmployeeName) return event.EmployeeName
  if (event.EmployeeCode) return event.EmployeeCode
  if (event.LoginId) return event.LoginId
  return null
}

/** Consecutive events with the same Stage form one group; order is kept as the server sent it (newest first). */
function groupByStage(events: TrackingEvent[]): { stage: string; events: TrackingEvent[] }[] {
  const groups: { stage: string; events: TrackingEvent[] }[] = []
  for (const event of events) {
    const last = groups[groups.length - 1]
    if (last && last.stage === event.Stage) last.events.push(event)
    else groups.push({ stage: event.Stage, events: [event] })
  }
  return groups
}

const UNAVAILABLE_TEXT: Record<Exclude<HistoryAvailability, 'error'>, string> = {
  'out-of-scope': 'History is not available for this document in your scope.',
  'no-tracking-page': 'Your roles do not have the Pending/Tracking page.',
  'not-deployed': 'History is not available on this server yet.',
}

export function HistoryPanel({ docType, documentId, title = 'History' }: Props) {
  const [history, setHistory] = useState<TrackingHistory | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [open, setOpen] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setHistory(await getTrackingHistory(docType, documentId))
    } catch (err) {
      setHistory(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [docType, documentId])

  useEffect(() => {
    void load()
  }, [load])

  const availability = error ? historyAvailability(error) : null

  return (
    <section className="history-panel">
      <div className="history-heading">
        <button
          type="button"
          className="history-toggle"
          aria-expanded={open}
          onClick={() => setOpen((value) => !value)}
        >
          <span className="history-chevron" aria-hidden="true">{open ? '▾' : '▸'}</span>
          <span className="form-section-title history-title">{title}</span>
        </button>
        <a
          className="history-refresh"
          role="button"
          tabIndex={0}
          aria-disabled={loading}
          onClick={() => { if (!loading) void load() }}
          onKeyDown={(event) => { if ((event.key === 'Enter' || event.key === ' ') && !loading) { event.preventDefault(); void load() } }}
        >
          {loading ? 'Loading…' : 'Refresh'}
        </a>
      </div>

      {open && (
        <div className="history-body">
          {loading && !history && !error && <p className="field-hint">Loading history…</p>}

          {availability && availability !== 'error' && (
            <p className="field-hint history-unavailable">{UNAVAILABLE_TEXT[availability]}</p>
          )}
          {availability === 'error' && <ErrorAlert error={error} onReload={() => void load()} fallback="History could not be loaded." />}

          {history && (
            <>
              <div className="history-summary">
                <span className="mono history-number">{history.Number}</span>
                <StatusBadge value={history.CurrentStatus} />
                {history.PendingWithRole && (
                  <span className="history-waiting">
                    Waiting with {roleWords(history.PendingWithRole)}
                    {history.WaitingSince && <> since {formatInstantIst(history.WaitingSince)}</>}
                    {history.AgeDays !== null && history.AgeDays !== undefined && (
                      <> · {history.AgeDays} {history.AgeDays === 1 ? 'day' : 'days'}</>
                    )}
                    {history.IsOverdue && <span className="badge badge-error history-overdue">Overdue</span>}
                  </span>
                )}
              </div>

              {history.Events.length === 0 ? (
                <p className="field-hint">No history recorded yet.</p>
              ) : (
                <ol className="history-timeline">
                  {groupByStage(history.Events).map((group, groupIndex) => (
                    <li key={`${group.stage}-${groupIndex}`} className="history-stage">
                      <div className="history-stage-label">{stageWords(group.stage)}</div>
                      <ol className="history-events">
                        {group.events.map((event, index) => {
                          const actor = who(event)
                          return (
                            <li key={`${event.At}-${event.Action}-${index}`} className="history-event">
                              <div className="history-event-head">
                                <span className="history-action">{actionWords(event.Action)}</span>
                                {(event.FromStatus || event.ToStatus) && (
                                  <span className="history-transition">
                                    {event.FromStatus ? <StatusBadge value={event.FromStatus} /> : <span className="history-none">—</span>}
                                    <span className="history-arrow" aria-hidden="true">→</span>
                                    {event.ToStatus ? <StatusBadge value={event.ToStatus} /> : <span className="history-none">—</span>}
                                  </span>
                                )}
                                <span className="history-time mono">{formatInstantIst(event.At)}</span>
                              </div>
                              {(actor || event.RoleCode) && (
                                <div className="history-who">
                                  {actor}
                                  {actor && event.RoleCode && ' · '}
                                  {event.RoleCode && roleWords(event.RoleCode)}
                                </div>
                              )}
                              {event.Remarks && <div className="history-remarks">{event.Remarks}</div>}
                            </li>
                          )
                        })}
                      </ol>
                    </li>
                  ))}
                </ol>
              )}
            </>
          )}
        </div>
      )}
    </section>
  )
}
