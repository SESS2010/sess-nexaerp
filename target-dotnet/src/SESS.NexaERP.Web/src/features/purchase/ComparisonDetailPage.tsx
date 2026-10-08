import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  actOnComparison,
  newIdempotencyKey,
  rememberDoc,
} from '../../api/purchase'
import type { ComparisonAction } from '../../api/purchase'
import type { ComparisonDetail, QuotationListItem } from '../../types/purchase'
import { OperationIntent, RequestGate, comparisonQuotationChoices, isStaleConflict, sessionScopeKey } from './quotationDraft'
import { readComparisonQuotations, recommendFlow } from './quotationFlows'
import { quotationFlowApi } from './quotationFlowApi'
import { ScreenLifecycle, TARGET } from './screenLifecycle'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { StatusBadge } from '../employees/StatusBadge'
import { formatAmount } from './PurchaseRequisitionListPage'
import { ErrorAlert } from '../../components/ErrorAlert'
import { HistoryPanel } from '../../components/HistoryPanel'
import { CopyId } from '../../components/CopyId'

interface ActionDefinition {
  action: ComparisonAction
  label: string
  tone: 'btn-primary' | 'btn-ghost' | 'btn-warn'
  /** Action name on purchase.commercial-comparisons required by the endpoint. */
  permission: string
}

const ACTIONS: ActionDefinition[] = [
  { action: 'approve', label: 'Approve', tone: 'btn-primary', permission: 'approve' },
  { action: 'request-revision', label: 'Request revision', tone: 'btn-warn', permission: 'request-revision' },
  { action: 'reject', label: 'Reject', tone: 'btn-warn', permission: 'reject' },
  { action: 'resubmit', label: 'Resubmit', tone: 'btn-ghost', permission: 'resubmit' },
]

function prettyJson(value: string | undefined): string {
  if (!value) return '—'
  try {
    return JSON.stringify(JSON.parse(value), null, 1)
  } catch {
    return value
  }
}

export function ComparisonDetailPage() {
  const { comparisonNumber = '' } = useParams()
  const navigate = useNavigate()
  const { me, can } = useSession()
  const scope = sessionScopeKey(me)
  const recommendIntent = useRef(new OperationIntent(() => newIdempotencyKey('comparison-recommend')))
  // Reset on a company / login / comparison change: late reads and write
  // continuations from before are dropped.
  const loadGate = useRef(new RequestGate())
  const labelsGate = useRef(new RequestGate())
  // Unmount, company/login/comparison change, or another chosen winner drops
  // in-flight reads and write continuations (screenLifecycle.ts).
  const [lifecycle] = useState(() => new ScreenLifecycle().track(loadGate.current, labelsGate.current))

  useEffect(() => {
    lifecycle.mount()
    return () => lifecycle.unmount()
  }, [lifecycle])

  const chooseWinner = (id: string) => {
    lifecycle.setTarget(TARGET.winner, id)
    setQuotationId(id)
  }

  // POST /comparisons/{number}/recommend → purchase.commercial-comparisons:submit.
  const canRecommend = can(PAGE_KEYS.comparisons, 'submit')
  // Approve / reject / request-revision / resubmit each need their own action
  // grant on purchase.commercial-comparisons.
  const allowedActions = ACTIONS.filter((definition) => can(PAGE_KEYS.comparisons, definition.permission))

  const [comparison, setComparison] = useState<ComparisonDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')

  const [quotationId, setQuotationId] = useState('')
  // Quotation numbers / vendors for the comparison lines (GET /quotations, per vendor).
  const [knownQuotations, setKnownQuotations] = useState<QuotationListItem[]>([])
  // False when the quotation list was cut short or could not be read: unresolved
  // quotations then stay disabled, and the screen says why.
  const [labelsComplete, setLabelsComplete] = useState(true)
  const canListQuotations = can(PAGE_KEYS.quotations, 'view')
  const [recommendationRemarks, setRecommendationRemarks] = useState('')
  const [singleSourceJustification, setSingleSourceJustification] = useState('')
  const [recommending, setRecommending] = useState(false)

  const [remarks, setRemarks] = useState('')
  const [busy, setBusy] = useState<ComparisonAction | null>(null)
  // Busy flags belong to one action: another winner (recommend), a scope /
  // comparison change or unmount releases them, so the next action can start.
  // Approval actions keep one key across an identical retry (lost answer).
  lifecycle
    .bindBusy(TARGET.winner, setRecommending)
    .bindBusy(TARGET.approval, (isBusy) => { if (!isBusy) setBusy(null) })
  const approvalIntent = useRef(new OperationIntent(() => newIdempotencyKey('comparison-action')))

  const load = useCallback(async () => {
    const ticket = loadGate.current.begin()
    setLoading(true)
    setError(null)
    try {
      const detail = await quotationFlowApi.getComparison(comparisonNumber)
      if (!loadGate.current.isCurrent(ticket)) return
      setComparison(detail)
      rememberDoc('comparison', detail.ComparisonNumber)
    } catch (err) {
      if (!loadGate.current.isCurrent(ticket)) return
      setComparison(null)
      setError(err)
    } finally {
      if (loadGate.current.isCurrent(ticket)) setLoading(false)
    }
  }, [comparisonNumber])

  useEffect(() => {
    // A new comparison or company/login: forget everything shown or typed, and
    // make every in-flight read and write continuation moot.
    lifecycle.changeScope()
    recommendIntent.current.clear()
    approvalIntent.current.clear()
    setComparison(null)
    setQuotationId('')
    setKnownQuotations([])
    setLabelsComplete(true)
    setRecommendationRemarks('')
    setSingleSourceJustification('')
    setRemarks('')
    setRecommending(false)
    setBusy(null)
    setError(null)
    setNotice('')
    void load()
  }, [load, scope, lifecycle])

  useEffect(() => {
    const ticket = labelsGate.current.begin()
    const lines = comparison?.Lines ?? []
    if (!comparison || !canListQuotations || lines.length === 0) {
      setKnownQuotations([])
      setLabelsComplete(true)
      return
    }
    // Every page of each compared vendor's quotations, not just the first 100.
    readComparisonQuotations(quotationFlowApi, comparison)
      .then((list) => {
        if (!labelsGate.current.isCurrent(ticket)) return
        setKnownQuotations(list.items)
        setLabelsComplete(list.complete)
      })
      .catch(() => {
        // Labels fall back to a plain ordinal and recommend stays blocked; the ids still show.
        if (!labelsGate.current.isCurrent(ticket)) return
        setKnownQuotations([])
        setLabelsComplete(false)
      })
  }, [comparison, canListQuotations, scope])

  const quotationChoices = useMemo(
    () => comparisonQuotationChoices(comparison?.Lines ?? [], knownQuotations),
    [comparison, knownQuotations],
  )
  const quotationLabelById = useMemo(
    () => new Map(quotationChoices.map((choice) => [choice.quotationId, choice.label])),
    [quotationChoices],
  )

  const recommend = async () => {
    if (!comparison) return
    setError(null)
    setNotice('')
    if (!quotationId) {
      setError('Choose the winning vendor quotation to recommend.')
      return
    }
    if (!recommendationRemarks.trim()) {
      setError('A recommendation must say why that vendor was chosen — this is the audit record.')
      return
    }
    const choice = quotationChoices.find((row) => row.quotationId === quotationId)
    if (!choice?.resolved) {
      setError('This quotation could not be identified from the quotation list, so it cannot be recommended. Reload the page; if it persists, report it.')
      return
    }
    if (recommending) return
    // Owns "Recording…" and stays live until finish, unmount, a company/login/
    // comparison change, or another winner is chosen (which also frees the button).
    lifecycle.setTarget(TARGET.winner, quotationId)
    const action = lifecycle.startAction(TARGET.winner)
    const isLive = action.isLive
    try {
      // Fresh read of the comparison first: a changed version stops for review,
      // a changed company/login/target posts nothing.
      const outcome = await recommendFlow({
        api: quotationFlowApi,
        loaded: comparison,
        body: {
          VendorQuotationId: quotationId,
          RecommendationRemarks: recommendationRemarks.trim(),
          SingleSourceJustification: comparison.IsSingleSource
            ? singleSourceJustification.trim() || null
            : null,
        },
        intent: recommendIntent.current,
        scope,
        isLive,
      })
      switch (outcome.kind) {
        case 'abandoned':
          return
        case 'changed':
          setComparison(outcome.fresh)
          setError(`Comparison ${outcome.fresh.ComparisonNumber} changed since you opened it (now version ${outcome.fresh.Version}, ${outcome.fresh.Status}). Review it and recommend again.`)
          return
        case 'done':
          setNotice(`Recommendation recorded. ${outcome.result.Number} is now ${outcome.result.Status}.`)
          setQuotationId('')
          setRecommendationRemarks('')
          void load()
          return
        case 'failed':
          setError(outcome.error)
          if (outcome.stale) {
            setNotice('The comparison changed on the server and has been reloaded. Review it and recommend again.')
            void load()
          }
      }
    } finally {
      action.finish()
    }
  }

  const runAction = async (definition: ActionDefinition) => {
    if (!comparison) return
    setError(null)
    setNotice('')
    if (!remarks.trim()) {
      setError(`Remarks are required to ${definition.label.toLowerCase()}.`)
      return
    }
    if (busy) return
    const action = lifecycle.startAction(TARGET.approval)
    const isLive = action.isLive
    setBusy(definition.action)
    const body = { Remarks: remarks.trim(), Version: comparison.Version }
    // Same key only for an identical retry of this action on this version.
    const key = approvalIntent.current.keyFor([scope, comparison.ComparisonNumber, definition.action, body])
    try {
      const result = await actOnComparison(comparison.ComparisonNumber, definition.action, { ...body, IdempotencyKey: key })
      if (!isLive()) return
      approvalIntent.current.clear()
      setRemarks('')
      setNotice(`${definition.label} succeeded. Status is now ${result.Status}.`)
      void load()
    } catch (err) {
      if (!isLive()) return
      // A 409 means the comparison moved on: never replay that key.
      if (isStaleConflict(err)) approvalIntent.current.clear()
      setError(err)
    } finally {
      action.finish()
    }
  }

  if (loading && !comparison) return <div className="page"><p>Loading…</p></div>

  if (!comparison) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Comparison not found." />
        <button type="button" className="btn btn-ghost" onClick={() => navigate('/purchase/comparisons')}>
          ‹ Back to comparisons
        </button>
      </div>
    )
  }

  const lines = comparison.Lines ?? []
  const masked = lines.length > 0 && lines[0].TotalPayableValue === undefined

  return (
    <div className="page">
      <div className="breadcrumbs">
        <Link to="/purchase/comparisons">Commercial Comparison</Link> /{' '}
        <span className="mono">{comparison.ComparisonNumber}</span>
      </div>

      <div className="page-header">
        <div>
          <h1>{comparison.ComparisonNumber}</h1>
          <p className="page-sub">
            {comparison.CurrencyCode}
            {comparison.ApprovalRoute ? ` · route ${comparison.ApprovalRoute}` : ''}
            {comparison.RequiredApprovalStepCount !== undefined
              ? ` · ${comparison.CompletedApprovalStepCount ?? 0}/${comparison.RequiredApprovalStepCount} approvals`
              : ''}
          </p>
        </div>
        <div className="action-row"><StatusBadge value={comparison.Status} /></div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />
      {notice && <div className="alert">{notice}</div>}
      {masked && (
        <div className="alert">
          Commercial values are hidden — your role lacks the{' '}
          <span className="mono">ViewCommercialValues</span> permission on{' '}
          <span className="mono">purchase.commercial-comparisons</span>.
        </div>
      )}

      <div className="card">
        <div className="detail-grid">
          <div className="detail-field">
            <span className="field-label">Sourcing</span>
            {comparison.IsSingleSource ? 'Single source' : 'Competitive'}
          </div>
          <div className="detail-field">
            <span className="field-label">Total payable</span>
            {comparison.TotalPayableValue !== undefined ? `₹${formatAmount(comparison.TotalPayableValue)}` : 'Masked'}
          </div>
          <div className="detail-field">
            <span className="field-label">Record version</span>
            <span className="mono">{comparison.Version}</span>
          </div>
          <div className="detail-field">
            <span className="field-label">Recommended quotation</span>
            {comparison.RecommendedVendorQuotationId
              ? quotationLabelById.get(comparison.RecommendedVendorQuotationId) ?? 'Recorded'
              : '—'}
            <CopyId value={comparison.RecommendedVendorQuotationId} />
          </div>
          {comparison.RecommendationRemarks && (
            <div className="detail-field field-wide">
              <span className="field-label">Recommendation remarks</span>
              {comparison.RecommendationRemarks}
            </div>
          )}
          {comparison.IsSingleSource && (
            <div className="detail-field field-wide">
              <span className="field-label">Single-source justification</span>
              {comparison.SingleSourceJustification ?? '—'}
            </div>
          )}
        </div>
      </div>

      <h2>Compared lines ({lines.length})</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Quotation · vendor</th>
              <th>Technical compliance</th>
              <th>Delivery</th>
              <th>Warranty</th>
              <th>Payment terms</th>
              <th className="text-right">Total payable</th>
              <th>Recommended</th>
              <th>Reason</th>
            </tr>
          </thead>
          <tbody>
            {lines.length === 0 && (
              <tr><td colSpan={8} className="table-empty">No comparison lines.</td></tr>
            )}
            {lines.map((line) => (
              <tr key={line.Id} className={line.IsRecommended ? 'row-selected' : undefined}>
                <td>
                  {quotationLabelById.get(line.VendorQuotationId) ?? '—'}
                  <div><CopyId label="Quotation id" value={line.VendorQuotationId} /></div>
                  <div><CopyId label="Line id" value={line.VendorQuotationLineId} /></div>
                </td>
                <td><StatusBadge value={line.TechnicalComplianceSnapshot || 'Unknown'} /></td>
                <td>{line.DeliverySnapshot || '—'}</td>
                <td>{line.WarrantySnapshot ?? 'Masked'}</td>
                <td>{line.PaymentTermsSnapshot ?? 'Masked'}</td>
                <td className="text-right mono">
                  {line.TotalPayableValue !== undefined ? formatAmount(line.TotalPayableValue) : 'Masked'}
                </td>
                <td>{line.IsRecommended ? 'Yes' : '—'}</td>
                <td>{line.RecommendationReason ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {lines.length > 0 && lines[0].CommercialSnapshotJson && (
        <details className="card" style={{ marginTop: 16 }}>
          <summary className="form-section-title" style={{ cursor: 'pointer' }}>
            Commercial snapshot (first line)
          </summary>
          <pre className="mono" style={{ whiteSpace: 'pre-wrap', marginTop: 12 }}>
            {prettyJson(lines[0].CommercialSnapshotJson)}
          </pre>
        </details>
      )}

      {canRecommend && (
      <div className="card">
        <div className="form-section-title">Recommend a vendor</div>
        <div className="form-grid">
          <label className="field field-wide">
            <span className="field-label">Winning vendor quotation *</span>
            <select className="input" value={quotationId} onChange={(e) => chooseWinner(e.target.value)}>
              <option value="">
                {quotationChoices.length === 0 ? 'No technically compliant quotation on this comparison' : 'Choose the quotation to recommend'}
              </option>
              {quotationChoices.map((choice) => (
                <option key={choice.quotationId} value={choice.quotationId} disabled={!choice.resolved}>{choice.label}</option>
              ))}
            </select>
            <CopyId label="Quotation id" value={quotationId} />
            {!labelsComplete && (
              <span className="field-hint">
                The quotation list could not be read in full, so some quotations cannot be named and cannot be
                recommended. Reload the page; if it persists, report it.
              </span>
            )}
          </label>
          <label className="field field-wide">
            <span className="field-label">Recommendation remarks *</span>
            <textarea className="input" rows={2} value={recommendationRemarks} onChange={(e) => setRecommendationRemarks(e.target.value)} />
          </label>
          {comparison.IsSingleSource && (
            <label className="field field-wide">
              <span className="field-label">Single-source justification</span>
              <textarea className="input" rows={2} value={singleSourceJustification} onChange={(e) => setSingleSourceJustification(e.target.value)} />
            </label>
          )}
          <div className="field-wide action-row">
            <button type="button" className="btn btn-primary" disabled={recommending} onClick={() => void recommend()}>
              {recommending ? 'Recording…' : 'Record recommendation'}
            </button>
          </div>
        </div>
      </div>
      )}

      {allowedActions.length > 0 && (
      <div className="card">
        <div className="form-section-title">Approval</div>
        <label className="field field-wide">
          <span className="field-label">Remarks *</span>
          <textarea className="input" rows={2} value={remarks} onChange={(e) => setRemarks(e.target.value)} />
        </label>
        <div className="action-row">
          {allowedActions.map((definition) => (
            <button
              key={definition.action}
              type="button"
              className={`btn ${definition.tone}`}
              disabled={busy !== null}
              onClick={() => void runAction(definition)}
            >
              {busy === definition.action ? 'Working…' : definition.label}
            </button>
          ))}
        </div>
        <p className="field-hint">
          Sent with record version {comparison.Version}. The API decides which transition is legal
          and returns a conflict otherwise.
        </p>
      </div>
      )}

      <HistoryPanel docType="COMPARISON" documentId={comparison.Id} />
    </div>
  )
}
