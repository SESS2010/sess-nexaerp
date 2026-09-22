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
function classifyConflict(message: string, code?: string): Conflict {
  // The envelope's Code is authoritative when present. CONCURRENCY_CONFLICT is
  // the optimistic-lock loser (two people saved the same record at once, e.g.
  // two Accounts users recording a payment): its Detail ("The record changed
  // after it was loaded. Refresh and retry.") contains neither "stale" nor
  // "version", so it must not fall through to the business-rule wording.
  if (code === 'CONCURRENCY_CONFLICT') {
    return {
      title: 'Someone else changed this record',
      guidance:
        'Your copy is out of date, so the save was refused rather than overwriting their work. Reload to get the current version, then reapply your changes.',
      reloadable: true,
      technical: false,
    }
  }

  const text = message.toLowerCase()

  if (text.includes('uom conversion')) {
    return {
      title: "The chosen UOM cannot be converted to the item's base UOM",
      guidance:
        'Request the item in its own UOM, or have an approved UOM conversion set up in the master first.',
      reloadable: false,
      technical: false,
    }
  }

  if (text.includes('insufficient available')) {
    return {
      title: 'Stores has no AVAILABLE stock of this item',
      guidance:
        'Only QC-accepted stock in Stores custody can be issued. Receive and clear the material through GRN and QC first, or issue a smaller quantity.',
      reloadable: false,
      technical: false,
    }
  }

  if (text.includes('only the named issue custodian') || text.includes('own material return') || text.includes('accept their own')) {
    return {
      title: 'Wrong person for this step',
      guidance:
        'The engineer who holds the material declares the return, and someone else in Stores accepts it. Ask the right person to take this step.',
      reloadable: false,
      technical: false,
    }
  }

  // Opening stock ceremony (OpeningStockSql.cs). Each refusal is a different
  // person's problem, so each gets its own sentence rather than one generic one.
  if (text.includes('already has stock movements')) {
    return {
      title: 'This company already has stock movements',
      guidance:
        'Opening stock can only be loaded into an empty company. Every GRN, issue or return that has already been posted makes an opening balance meaningless, so the ceremony is refused here for good.',
      reloadable: false,
      technical: false,
    }
  }

  if (text.includes('already exists for this company and period')) {
    return {
      title: 'This period has already been loaded',
      guidance:
        'An opening stock ceremony for exactly this period exists in this company. Open it from the list instead of starting another; a different period needs a different import.',
      reloadable: false,
      technical: false,
    }
  }

  if (text.includes('may not count and value') || text.includes('three separate employees')) {
    return {
      title: 'The same person cannot take two of the three steps',
      guidance:
        'Count, value and authorization must each be done by a different employee — Stores Manager, Accounts Manager and Technical Director. Ask the next person in the chain to take this step.',
      reloadable: false,
      technical: false,
    }
  }

  if (text.includes('full or temporary') && text.includes('authority')) {
    return {
      title: 'Your role assignment does not carry this authority',
      guidance:
        'The step needs a current FULL or TEMPORARY assignment of the named role in this company. A SUPPORT assignment, an expired one, or one still awaiting approval is refused.',
      reloadable: false,
      technical: false,
    }
  }

  if (text.includes('error-free import for this company')) {
    return {
      title: 'No usable import batch for this company',
      guidance:
        'The import batch must be an opening-stock workbook uploaded in this company, completed with zero invalid rows and at least one row created. Check the company you are signed into, and re-upload a corrected workbook if any row was rejected.',
      reloadable: false,
      technical: false,
    }
  }

  if (text.includes('stale version or invalid status')) {
    return {
      title: 'The ceremony has moved on',
      guidance:
        'Either someone else already took this step, or this ceremony is not at the stage this step expects (count → value → authorize). Reload to see its current stage.',
      reloadable: true,
      technical: false,
    }
  }

  if (text.includes('idempotency mismatch') || text.includes('command ledger') || text.includes('registered current company-scoped import command')) {
    return {
      title: 'The command context did not match',
      guidance:
        'The same idempotency key was reused with different content, or the request was not registered in the ordinary command ledger for this company. Start the step again so a fresh key is issued.',
      reloadable: true,
      technical: true,
    }
  }

  // Word match: "conversion" also contains "version" and is a different conflict.
  if (text.includes('stale') || /\bversion\b/.test(text)) {
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
  const isForbidden = error instanceof ApiError && error.status === 403

  // A 403 must never be silent: the server text names the permission or the
  // approver the document is waiting on, which is exactly what the user needs.
  if (isForbidden) {
    return (
      <div className={`alert alert-warn ${className}`.trim()} role="alert">
        <div className="alert-title">You are not allowed to do this</div>
        <p className="alert-body">
          Your role or department does not have the permission this action needs. The server's reason is below; if it names another person, the document is waiting on them, not on you.
        </p>
        <p className="alert-detail mono">{message}</p>
      </div>
    )
  }

  if (!isConflict) {
    return (
      <div className={`alert alert-error ${className}`.trim()} role="alert">
        {message || fallback}
      </div>
    )
  }

  const conflict = classifyConflict(message, error instanceof ApiError ? error.code : undefined)

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
