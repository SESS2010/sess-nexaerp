import { ApiError } from '../api/client'

interface Props {
  error: unknown
  /** Re-runs the page's load. Shown for conflicts a refresh can actually resolve. */
  onReload?: () => void
  /** Fallback text when the thrown value carries no message. */
  fallback?: string
  /** Extra classes for the banner, e.g. a grid-span class inside a form. */
  className?: string
}

interface Conflict {
  title: string
  /** Plain-language next step for the user. */
  guidance: string
  /** A refresh can resolve this, so offer the button. */
  reloadable: boolean
  /** The server text is developer-facing; collapse it instead of leading with it. */
  technical: boolean
}

/**
 * Maps a 409 to something a storekeeper can act on.
 *
 * The API returns a single `message` string for every conflict, so the class of
 * conflict has to be recovered from its text. Matching is on lowercased
 * substrings of the messages the backend actually raises; anything unmatched
 * falls through to the generic case, and the server's own text is always kept
 * so nothing specific is lost.
 */
function classifyConflict(message: string): Conflict {
  const text = message.toLowerCase()

  if (text.includes('stale') || text.includes('version')) {
    return {
      title: 'Someone else changed this record',
      guidance:
        'Your copy is out of date, so the save was refused rather than overwriting their work. Reload to get the current version, then reapply your changes.',
      reloadable: true,
      technical: false,
    }
  }

  if (text.includes('approval configuration') || text.includes('approval_route_settings')) {
    return {
      title: 'Approval routing is not configured',
      guidance:
        'This company has no approval route covering this value, so the requisition cannot be submitted. This is a configuration gap, not a mistake in your entry — send the detail below to your system administrator.',
      reloadable: false,
      technical: true,
    }
  }

  if (text.includes('idempotency')) {
    return {
      title: 'This looks like a repeated submission',
      guidance:
        'The same submission reference arrived with different data. Reload to see whether the first attempt was already saved before entering it again.',
      reloadable: true,
      technical: true,
    }
  }

  if (text.includes('immutable') || text.includes('finalized') || text.includes('finalised')) {
    return {
      title: 'This document is finalized',
      guidance:
        'Finalized documents cannot be edited. Correct it by reversing the document and raising a new one.',
      reloadable: true,
      technical: false,
    }
  }

  return {
    title: 'The server refused this change',
    guidance:
      'The record is not in a state that allows this action. Reload to see its current state before trying again.',
    reloadable: true,
    technical: false,
  }
}

/**
 * Shared error banner. A plain failure renders as before; a 409 gets a
 * plain-language heading, a next step, and a reload action instead of raw
 * server text.
 */
export function ErrorAlert({ error, onReload, fallback = 'Something went wrong.', className = '' }: Props) {
  if (!error) return null

  // Pages use this same slot for client-side validation strings, so a plain
  // string is a valid error value, not just a thrown Error.
  const message =
    typeof error === 'string' ? error : error instanceof Error ? error.message : fallback
  const isConflict = error instanceof ApiError && error.status === 409

  if (!isConflict) {
    return (
      <div className={`alert alert-error ${className}`.trim()} role="alert">
        {message || fallback}
      </div>
    )
  }

  const conflict = classifyConflict(message)

  return (
    <div className={`alert alert-warn ${className}`.trim()} role="alert">
      <div className="alert-title">{conflict.title}</div>
      <p className="alert-body">{conflict.guidance}</p>

      {conflict.technical ? (
        <details className="alert-detail">
          <summary>Technical detail</summary>
          <p className="mono">{message}</p>
        </details>
      ) : (
        <p className="alert-detail mono">{message}</p>
      )}

      {conflict.reloadable && onReload && (
        <button type="button" className="btn btn-ghost mt-2" onClick={onReload}>
          ↻ Reload
        </button>
      )}
    </div>
  )
}
