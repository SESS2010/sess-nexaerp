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

  // GRN QC: "The employee who recorded or finalised this GRN cannot inspect
  // it. Another QC inspector must." Must sit before the 'finalised' rule, which
  // would otherwise title it as an immutable-document refusal.
  if (text.includes('cannot inspect it')) {
    return {
      title: 'Wrong person for this step',
      guidance:
        'You recorded or finalised this GRN, so you cannot be its QC inspector. Another QC inspector must inspect it.',
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

  // Inventory period close: "This inventory period cannot be closed: N stock
  // adjustment(s) dated in it are not yet posted or rejected. Post or reject them first."
  if (text.includes('cannot be closed')) {
    return {
      title: 'Open stock adjustments block this period',
      guidance: 'Post or reject every stock adjustment dated in this period, then close it again.',
      reloadable: false,
      technical: false,
    }
  }

  // Material issue (EfMaterialIssueService.Issue.cs): a customer-facing excess
  // line with no decision, or a REJECTED one, refuses the issue.
  if (text.includes('customer-facing excess requires an approved technical director decision')) {
    return {
      title: 'The TD has not approved the excess',
      guidance:
        'At least one line on this MIR asks for more than the BOM or customer PO allows, and the Technical Director has not approved it — the decision is either still pending or was a rejection. A rejected excess means the MIR quantity must be edited back within the limit, or a new MIR raised. Check the TD decision column on the MIR.',
      reloadable: true,
      technical: false,
    }
  }

  // "Issue quantity exceeds MIR line N." — earlier partial issues count
  // towards the line, so the number entered can be over even when it is at or
  // below the requested quantity.
  if (text.includes('issue quantity exceeds mir line')) {
    return {
      title: 'More than the MIR line allows',
      guidance:
        'What was issued earlier against this line counts too: the total of every issue on the line cannot go above the quantity requested. Reduce the quantity to what is still open on the line (earlier partial issues are shown on the MIR), or raise a new MIR for the extra.',
      reloadable: true,
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

interface Failure {
  title: string
  guidance: string
  /** One plain line per rejected field, e.g. "Line 2 · Quantity: must be greater than 0". */
  fields: string[]
  reloadable: boolean
}

/** "Lines[1].RequiredDate" → "Line 2 · Required date". */
export function humanizeField(path: string): string {
  return path
    .split('.')
    .filter(Boolean)
    .map((part) => {
      const indexed = /^(\w+)\[(\d+)\]$/.exec(part)
      // "Lines[1]" names one line: singular, but leave "Address" alone.
      const word = (indexed ? indexed[1].replace(/([^s])s$/, '$1') : part).replace(/^\$/, '')
      const spaced = word
        .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
        .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
        .toLowerCase()
      const label = spaced.charAt(0).toUpperCase() + spaced.slice(1)
      return indexed ? `${label} ${Number(indexed[2]) + 1}` : label
    })
    .join(' · ')
}

/**
 * Plain-language wording for the failures that are not access refusals or
 * conflicts: field validation (400/422), a missing record (404), the server
 * failing (5xx) and the network being down. Returns null for a plain string or
 * an unrecognised error, which keep the old one-line banner.
 */
export function describeFailure(error: unknown): Failure | null {
  if (error instanceof TypeError && /fetch|network/i.test(error.message)) {
    return {
      title: 'The ERP response could not be confirmed',
      guidance: 'Check that this PC is on the office network. The result could not be confirmed. Check the record before retrying.',
      fields: [],
      reloadable: true,
    }
  }
  if (!(error instanceof ApiError)) return null

  if (error.status === 400 || error.status === 422) {
    const fields = Object.entries(error.errors ?? {}).map(
      ([field, messages]) => `${humanizeField(field)}: ${messages.join('; ')}`,
    )
    return {
      title: fields.length > 0 ? 'Please correct the highlighted entries' : 'This entry was not accepted',
      guidance:
        fields.length > 0
          ? 'Nothing was saved. Fix the items below and submit again.'
          : `Nothing was saved. ${error.envelope?.Detail || error.envelope?.Title || 'Check the values you entered and submit again.'}`,
      fields,
      reloadable: false,
    }
  }
  if (error.status === 404) {
    return {
      title: 'This record was not found',
      guidance:
        'It may belong to the other company, or it was removed. Check the company at the top, then open it again from the list.',
      fields: [],
      reloadable: true,
    }
  }
  if (error.status >= 500) {
    return {
      title: 'The ERP server could not complete this',
      guidance:
        'This is not a mistake in your entry. Reload to check whether it was saved before trying again. If it keeps happening, send a screenshot with the trace number to SURANTHER.',
      fields: [],
      reloadable: true,
    }
  }
  return null
}

/** Identity refusals from server-frontend-oidc-contract.md, branched on Code. */
const AUTH_CODES = new Set(['EMPLOYEE_ACCESS_NOT_CONFIGURED', 'MFA_REQUIRED', 'AUTHENTICATION_REQUIRED'])

function accessWording(error: ApiError): { title: string; guidance: string } {
  switch (error.code) {
    case 'EMPLOYEE_ACCESS_NOT_CONFIGURED':
      return {
        title: 'Your sign-in is not set up for this company',
        guidance:
          'You signed in, but the ERP has no active employee mapping for you in the chosen company, or your login is disabled. Try the other company, or ask the ERP administrator to map your login. Quote the trace number below.',
      }
    case 'MFA_REQUIRED':
      return {
        title: 'This role needs an authenticator code',
        guidance:
          'One of your roles may only be used after signing in with password and authenticator code. Sign out and sign in again as Approvers.',
      }
    default:
      return {
        title: 'You are not signed in',
        guidance: 'Your sign-in expired or was not accepted. Sign in again to continue.',
      }
  }
}

/** Full explanation of an identity refusal: plain words, then the server's Detail and TraceId. */
export function AccessProblem({ error, className = '' }: { error: unknown; className?: string }) {
  if (!(error instanceof ApiError)) {
    const text = error instanceof Error ? error.message : typeof error === 'string' ? error : 'The ERP could not be reached.'
    return <div className={`alert alert-error ${className}`.trim()} role="alert">{text}</div>
  }
  // A 5xx says nothing about the sign-in: the API is down or failed, so do not
  // tell the user their sign-in expired and send them round the login again.
  const wording =
    error.status >= 500
      ? {
          title: 'The ERP server did not answer',
          guidance:
            'Your sign-in is still valid; the server could not complete the request. Try again in a minute. If it keeps happening, tell the ERP administrator and quote the trace number if one is shown.',
        }
      : error.status === 403 && !AUTH_CODES.has(error.code ?? '')
        ? { title: 'You are not allowed to do this', guidance: "Your role or department does not have the permission this needs. The server's reason is below." }
        : accessWording(error)
  return (
    <div className={`alert alert-warn ${className}`.trim()} role="alert">
      <div className="alert-title">{wording.title}</div>
      <p className="alert-body">{wording.guidance}</p>
      <p className="alert-detail mono">{error.message}</p>
      {error.traceId && <p className="alert-detail mono">Trace {error.traceId}</p>}
    </div>
  )
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

  if (error instanceof ApiError && (error.status === 401 || AUTH_CODES.has(error.code ?? ''))) {
    return <AccessProblem error={error} className={className} />
  }

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
        {error.traceId && <p className="alert-detail mono">Trace {error.traceId}</p>}
      </div>
    )
  }

  if (!isConflict) {
    const failure = describeFailure(error)
    if (!failure) {
      return (
        <div className={`alert alert-error ${className}`.trim()} role="alert">
          {message || fallback}
        </div>
      )
    }
    return (
      <div className={`alert alert-error ${className}`.trim()} role="alert">
        <div className="alert-title">{failure.title}</div>
        <p className="alert-body">{failure.guidance}</p>
        {failure.fields.length > 0 && (
          <ul className="alert-body">
            {failure.fields.map((line) => (
              <li key={line}>{line}</li>
            ))}
          </ul>
        )}
        {error instanceof ApiError && error.traceId && <p className="alert-detail mono">Trace {error.traceId}</p>}
        <details className="alert-detail">
          <summary>Technical detail</summary>
          <p className="mono">{message}</p>
        </details>
        {failure.reloadable && onReload && (
          <button type="button" className="btn btn-ghost mt-2" onClick={onReload}>
            ↻ Try again
          </button>
        )}
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
