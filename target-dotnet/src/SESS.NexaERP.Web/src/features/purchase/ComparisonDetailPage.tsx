import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  actOnComparison,
  getComparison,
  listQuotations,
  newIdempotencyKey,
  recommendComparison,
  rememberDoc,
} from '../../api/purchase'
import type { ComparisonAction } from '../../api/purchase'
import type { ComparisonDetail, QuotationListItem } from '../../types/purchase'
import { OperationIntent, comparisonQuotationChoices, isStaleConflict, sessionScopeKey } from './quotationDraft'
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
  const canListQuotations = can(PAGE_KEYS.quotations, 'view')
  const [recommendationRemarks, setRecommendationRemarks] = useState('')
  const [singleSourceJustification, setSingleSourceJustification] = useState('')
  const [recommending, setRecommending] = useState(false)

  const [remarks, setRemarks] = useState('')
  const [busy, setBusy] = useState<ComparisonAction | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const detail = await getComparison(comparisonNumber)
      setComparison(detail)
      rememberDoc('comparison', detail.ComparisonNumber)
    } catch (err) {
      setComparison(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [comparisonNumber])

  useEffect(() => {
    // A new comparison or company/login: forget the previous selection and key.
    setQuotationId('')
    setKnownQuotations([])
    recommendIntent.current.clear()
    void load()
  }, [load, scope])

  useEffect(() => {
    const lines = comparison?.Lines ?? []
    if (!canListQuotations || lines.length === 0) {
      setKnownQuotations([])
      return
    }
    let cancelled = false
    const vendorIds = [...new Set(lines.map((line) => line.VendorId).filter((id): id is string => !!id))]
    const queries = vendorIds.length > 0
      ? vendorIds.map((vendorId) => listQuotations({ page: 1, pageSize: 100, vendorId }))
      : [listQuotations({ page: 1, pageSize: 100 })]
    Promise.all(queries)
      .then((pages) => { if (!cancelled) setKnownQuotations(pages.flatMap((page) => page.Items)) })
      .catch(() => { /* labels fall back to a plain ordinal; the ids still travel */ })
    return () => { cancelled = true }
  }, [comparison, canListQuotations])

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
    setRecommending(true)
    try {
      // Freshly read the comparison: a changed version stops for review, never a silent retry.
      const fresh = await getComparison(comparison.ComparisonNumber)
      if (fresh.Version !== comparison.Version) {
        setComparison(fresh)
        recommendIntent.current.clear()
        setError(`Comparison ${fresh.ComparisonNumber} changed since you opened it (now version ${fresh.Version}, ${fresh.Status}). Review it and recommend again.`)
        return
      }
      const body = {
        VendorQuotationId: quotationId,
        RecommendationRemarks: recommendationRemarks.trim(),
        SingleSourceJustification: comparison.IsSingleSource
          ? singleSourceJustification.trim() || null
          : null,
        Version: fresh.Version,
      }
      const key = recommendIntent.current.keyFor([scope, comparison.ComparisonNumber, body])
      const result = await recommendComparison(comparison.ComparisonNumber, { ...body, IdempotencyKey: key })
      recommendIntent.current.clear()
      setNotice(`Recommendation recorded. ${result.Number} is now ${result.Status}.`)
      setQuotationId('')
      setRecommendationRemarks('')
      void load()
    } catch (err) {
      setError(err)
      if (isStaleConflict(err)) {
        recommendIntent.current.clear()
        setNotice('The comparison changed on the server and has been reloaded. Review it and recommend again.')
        void load()
      }
    } finally {
      setRecommending(false)
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
    setBusy(definition.action)
    try {
      const result = await actOnComparison(comparison.ComparisonNumber, definition.action, {
        Remarks: remarks.trim(),
        Version: comparison.Version,
        IdempotencyKey: newIdempotencyKey(`comparison-${definition.action}`),
      })
      setRemarks('')
      setNotice(`${definition.label} succeeded. Status is now ${result.Status}.`)
      void load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(null)
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
            <select className="input" value={quotationId} onChange={(e) => setQuotationId(e.target.value)}>
              <option value="">
                {quotationChoices.length === 0 ? 'No technically compliant quotation on this comparison' : 'Choose the quotation to recommend'}
              </option>
              {quotationChoices.map((choice) => (
                <option key={choice.quotationId} value={choice.quotationId} disabled={!choice.resolved}>{choice.label}</option>
              ))}
            </select>
            <CopyId label="Quotation id" value={quotationId} />
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
