import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { ApiError } from '../../api/client'
import { DASHBOARD_ERROR_CODES } from '../../types/dashboard'
import { formatAmount, formatTimestamp } from '../../utils/dashboardFormat'
import type { DashboardQueryState } from './useDashboardQuery'

// ---------- money: null is never zero ----------

export type NullReason = 'withheld' | 'unknown' | 'none'

const NULL_TEXT: Record<NullReason, { label: string; title: string }> = {
  withheld: { label: 'Withheld', title: 'Not zero: this value is withheld from your role.' },
  unknown: { label: 'Unknown', title: 'Not zero: the source is incomplete, so this value cannot be stated.' },
  none: { label: '—', title: 'No value recorded. This is not a zero amount.' },
}

/** A visibly different marker for a missing value, so it can never be read as 0. */
export function NullValue({ reason }: { reason: NullReason }) {
  const { label, title } = NULL_TEXT[reason]
  return (
    <span
      title={title}
      className="inline-block rounded border border-dashed px-1.5 text-xs italic"
      style={{ color: 'var(--color-ink-faint)', borderColor: 'var(--color-line)' }}
    >
      {label}
    </span>
  )
}

/** One amount in its own currency. A null value or currency renders as a NullValue. */
export function Money({ value, currency, nullReason = 'unknown' }: { value: number | null | undefined; currency: string | null | undefined; nullReason?: NullReason }) {
  if (value === null || value === undefined || !currency) return <NullValue reason={nullReason} />
  return <span className={`mono${value < 0 ? ' text-red-700' : ''}`}>{formatAmount(value, currency)}</span>
}

/**
 * Separate native-currency lines; currencies are never added together.
 * An empty list is not ₹0: it says there is no value in any currency.
 */
export function AmountList({ amounts, emptyReason = 'none' }: { amounts: { Currency: string | null; value: number | null }[] | null; emptyReason?: NullReason }) {
  if (amounts === null) return <NullValue reason="unknown" />
  if (amounts.length === 0) return <NullValue reason={emptyReason} />
  return (
    <span className="inline-flex flex-col">
      {amounts.map((amount, index) => (
        <Money key={`${amount.Currency ?? 'redacted'}-${index}`} value={amount.value} currency={amount.Currency} nullReason="withheld" />
      ))}
    </span>
  )
}

// ---------- the five states, each visibly different ----------

type StateKind = 'loading' | 'empty' | 'incomplete' | 'denied' | 'failed'

const STATE_STYLE: Record<StateKind, { badge: string; label: string; box: string }> = {
  loading: { badge: 'badge badge-muted', label: 'Loading', box: 'alert border-dashed' },
  empty: { badge: 'badge badge-info', label: 'Empty', box: 'alert' },
  incomplete: { badge: 'badge badge-warn', label: 'Incomplete source', box: 'alert alert-warn' },
  denied: { badge: 'badge badge-muted', label: 'Permission denied', box: 'alert' },
  failed: { badge: 'badge badge-error', label: 'Request failed', box: 'alert alert-error' },
}

export function StateNotice({ kind, title, children, action }: { kind: StateKind; title: string; children?: ReactNode; action?: ReactNode }) {
  const style = STATE_STYLE[kind]
  return (
    <div className={style.box} role={kind === 'failed' ? 'alert' : 'status'} data-dashboard-state={kind}>
      <div className="alert-title flex items-center gap-2">
        <span className={style.badge}>{style.label}</span>
        <span>{title}</span>
      </div>
      {children && <div className="alert-body">{children}</div>}
      {action && <div className="mt-2">{action}</div>}
    </div>
  )
}

function TraceLine({ traceId }: { traceId?: string }) {
  if (!traceId) return null
  return <div className="alert-detail">Support reference (TraceId): <span className="mono">{traceId}</span></div>
}

/**
 * Error and discard states for one section. The 409 source-inconsistency is
 * not an ordinary failure: no figures are shown and an administrator must act.
 */
export function QueryProblem({ state, onRetry }: { state: Exclude<DashboardQueryState<unknown>, { kind: 'ready' } | { kind: 'loading' }>; onRetry: () => void }) {
  if (state.kind === 'company-mismatch') {
    return (
      <StateNotice kind="failed" title="Response discarded: it belongs to a different company"
        action={<button type="button" className="btn btn-ghost" onClick={onRetry}>Load again</button>}>
        The server answered for <span className="mono">{state.received}</span> while the selected company is{' '}
        <span className="mono">{state.expected}</span>. Nothing from that response is shown.
      </StateNotice>
    )
  }

  const error = state.error
  const apiError = error instanceof ApiError ? error : null
  const status = apiError?.status
  const code = apiError?.code

  if (status === 409 && code === DASHBOARD_ERROR_CODES.sourceInconsistent) {
    return (
      <div className="alert alert-error" role="alert" data-dashboard-state="source-inconsistent">
        <div className="alert-title flex items-center gap-2">
          <span className="badge badge-error">Figures do not reconcile</span>
          <span>Report source needs administrator action</span>
        </div>
        <div className="alert-body">
          <p><strong>These balances are inconsistent, so no figures are shown.</strong> Do not rely on any earlier copy of this report.</p>
          <p>{apiError?.message}</p>
          <p>When you report it, quote the reference below.</p>
        </div>
        <TraceLine traceId={apiError?.traceId} />
      </div>
    )
  }

  if (status === 403) {
    return (
      <StateNotice kind="denied" title="This section is not available to you">
        {apiError?.message}
        <TraceLine traceId={apiError?.traceId} />
      </StateNotice>
    )
  }

  if (status === 401) {
    return (
      <StateNotice kind="failed" title="Not signed in"
        action={<Link to="/login" className="btn btn-primary">Sign in again</Link>}>
        Your session has ended. Sign in again to load this section.
        <TraceLine traceId={apiError?.traceId} />
      </StateNotice>
    )
  }

  if (status === 400) {
    return (
      <StateNotice kind="failed" title="The filter was not accepted"
        action={<button type="button" className="btn btn-ghost" onClick={onRetry}>Try again</button>}>
        {apiError?.message} Clear the detail filters and try again.
        <TraceLine traceId={apiError?.traceId} />
      </StateNotice>
    )
  }

  // 500 and anything unexpected: never show exception text, keep the reference.
  return (
    <StateNotice kind="failed" title="Could not load this section"
      action={<button type="button" className="btn btn-ghost" onClick={onRetry}>Try again</button>}>
      {status === 500 || !apiError ? 'The server could not produce this report.' : apiError.message}
      <TraceLine traceId={apiError?.traceId} />
    </StateNotice>
  )
}

// ---------- section frame ----------

export function SectionFrame({ id, title, subtitle, generatedAt, timeZone, children }: {
  id: string
  title: string
  subtitle?: ReactNode
  generatedAt?: string
  timeZone?: string
  children: ReactNode
}) {
  return (
    <section className="card" aria-labelledby={`${id}-title`} id={id}>
      <div className="flex flex-wrap items-baseline justify-between gap-2 mb-3">
        <div>
          <h2 id={`${id}-title`} className="text-lg font-semibold">{title}</h2>
          {subtitle && <p className="page-sub">{subtitle}</p>}
        </div>
        {generatedAt && timeZone && (
          <span className="field-hint">Last refreshed {formatTimestamp(generatedAt, timeZone)} ({timeZone})</span>
        )}
      </div>
      {children}
    </section>
  )
}

/** The server's own explanation of what an amount means, shown beside the figures. */
export function BasisNote({ children }: { children: ReactNode }) {
  return <p className="field-hint mb-2"><strong>Basis:</strong> {children}</p>
}

/** Makes clear that a filter narrows the detail rows only, never the overview. */
export function DetailFilterNote({ active, onClear }: { active: string[]; onClear: () => void }) {
  if (active.length === 0) return null
  return (
    <div className="alert mb-2" data-dashboard-filter="active">
      <div className="alert-body flex flex-wrap items-center gap-2">
        <span className="badge badge-info">Detail filtered</span>
        <span>Rows below show only: {active.join(' · ')}. The overview above is not filtered.</span>
        <button type="button" className="link-button" onClick={onClear}>Clear detail filter</button>
      </div>
    </div>
  )
}

export function Pager({ page, pageSize, totalRows, onPage }: { page: number; pageSize: number; totalRows: number; onPage: (page: number) => void }) {
  const pages = Math.max(1, Math.ceil(totalRows / pageSize))
  if (totalRows <= pageSize) return <p className="field-hint mt-2">{totalRows} detail row{totalRows === 1 ? '' : 's'}</p>
  return (
    <div className="pager">
      <button type="button" className="btn btn-ghost" disabled={page <= 1} onClick={() => onPage(page - 1)}>Previous</button>
      <span className="pager-label">Page {page} of {pages} · {totalRows} detail rows</span>
      <button type="button" className="btn btn-ghost" disabled={page >= pages} onClick={() => onPage(page + 1)}>Next</button>
    </div>
  )
}

/**
 * A stock dimension the contract supplies only as a GUID (warehouse, rack/bin,
 * ownership, custody, provenance, serial). No names or codes are available
 * and there is no warehouse lookup screen, so this shows a short, clearly
 * labelled ID with the full value on hover. PENDING a decision on how
 * storekeepers should see these; change only this component.
 */
export function DimensionId({ label, id }: { label: string; id: string | null }) {
  if (!id) {
    return <span className="field-hint whitespace-nowrap">{label}: <NullValue reason="none" /></span>
  }
  return (
    <span className="field-hint whitespace-nowrap" title={`${label} ID ${id}`}>
      {label} ID <span className="mono">…{id.slice(-6)}</span>
    </span>
  )
}

/** A link when the target page is permitted; plain text otherwise. */
export function MaybeLink({ to, children }: { to: string | null; children: ReactNode }) {
  return to ? <Link to={to}>{children}</Link> : <>{children}</>
}
