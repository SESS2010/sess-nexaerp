import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  actOnComparison,
  getComparison,
  newIdempotencyKey,
  recommendComparison,
  rememberDoc,
} from '../../api/purchase'
import type { ComparisonAction } from '../../api/purchase'
import type { ComparisonDetail } from '../../types/purchase'
import { StatusBadge } from '../employees/StatusBadge'
import { formatAmount } from './PurchaseRequisitionListPage'

interface ActionDefinition {
  action: ComparisonAction
  label: string
  tone: 'btn-primary' | 'btn-ghost' | 'btn-warn'
}

const ACTIONS: ActionDefinition[] = [
  { action: 'approve', label: 'Approve', tone: 'btn-primary' },
  { action: 'request-revision', label: 'Request revision', tone: 'btn-warn' },
  { action: 'reject', label: 'Reject', tone: 'btn-warn' },
  { action: 'resubmit', label: 'Resubmit', tone: 'btn-ghost' },
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

  const [comparison, setComparison] = useState<ComparisonDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  const [quotationId, setQuotationId] = useState('')
  const [recommendationRemarks, setRecommendationRemarks] = useState('')
  const [singleSourceJustification, setSingleSourceJustification] = useState('')
  const [recommending, setRecommending] = useState(false)

  const [remarks, setRemarks] = useState('')
  const [busy, setBusy] = useState<ComparisonAction | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const detail = await getComparison(comparisonNumber)
      setComparison(detail)
      rememberDoc('comparison', detail.ComparisonNumber)
    } catch (err) {
      setComparison(null)
      setError(err instanceof Error ? err.message : 'Failed to load the comparison.')
    } finally {
      setLoading(false)
    }
  }, [comparisonNumber])

  useEffect(() => {
    void load()
  }, [load])

  const recommend = async () => {
    if (!comparison) return
    setError('')
    setNotice('')
    if (!quotationId.trim()) {
      setError('Pick the winning vendor quotation id to recommend.')
      return
    }
    if (!recommendationRemarks.trim()) {
      setError('A recommendation must say why that vendor was chosen — this is the audit record.')
      return
    }
    setRecommending(true)
    try {
      const result = await recommendComparison(comparison.ComparisonNumber, {
        VendorQuotationId: quotationId.trim(),
        RecommendationRemarks: recommendationRemarks.trim(),
        SingleSourceJustification: comparison.IsSingleSource
          ? singleSourceJustification.trim() || null
          : null,
        Version: comparison.Version,
        IdempotencyKey: newIdempotencyKey('comparison-recommend'),
      })
      setNotice(`Recommendation recorded. ${result.Number} is now ${result.Status}.`)
      setQuotationId('')
      setRecommendationRemarks('')
      void load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to record the recommendation.')
    } finally {
      setRecommending(false)
    }
  }

  const runAction = async (definition: ActionDefinition) => {
    if (!comparison) return
    setError('')
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
      setError(err instanceof Error ? err.message : `${definition.label} failed.`)
    } finally {
      setBusy(null)
    }
  }

  if (loading && !comparison) return <div className="page"><p>Loading…</p></div>

  if (!comparison) {
    return (
      <div className="page">
        <div className="alert alert-error">{error || 'Comparison not found.'}</div>
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

      {error && <div className="alert alert-error">{error}</div>}
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
            <span className="mono">{comparison.RecommendedVendorQuotationId ?? '—'}</span>
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
              <th>Quotation line id</th>
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
                <td className="mono">{line.VendorQuotationLineId}</td>
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

      <div className="card">
        <div className="form-section-title">Recommend a vendor</div>
        <div className="form-grid">
          <label className="field field-wide">
            <span className="field-label">Winning vendor quotation id (GUID) *</span>
            <input className="input mono" value={quotationId} onChange={(e) => setQuotationId(e.target.value)} />
            <span className="field-hint">
              The comparison lines above carry quotation <em>line</em> ids; this field needs the
              parent quotation id. No endpoint returns it yet.
            </span>
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

      <div className="card">
        <div className="form-section-title">Approval</div>
        <label className="field field-wide">
          <span className="field-label">Remarks *</span>
          <textarea className="input" rows={2} value={remarks} onChange={(e) => setRemarks(e.target.value)} />
        </label>
        <div className="action-row">
          {ACTIONS.map((definition) => (
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
    </div>
  )
}
