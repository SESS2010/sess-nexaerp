import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  actOnPurchaseRequisition,
  getPurchaseRequisition,
  getPurchaseRequisitionApprovalHistory,
  getPurchaseRequisitionStatusHistory,
} from '../../api/purchase'
import type { PurchaseRequisitionAction } from '../../api/purchase'
import type {
  PurchaseRequisitionDetail,
  PurchaseRequisitionHistorySummary,
} from '../../types/purchase'
import { StatusBadge } from '../employees/StatusBadge'
import { PurchaseRequisitionFormModal } from './PurchaseRequisitionFormModal'
import { formatAmount, formatDate } from './PurchaseRequisitionListPage'
import { ErrorAlert } from '../../components/ErrorAlert'

interface ActionDefinition {
  action: PurchaseRequisitionAction
  label: string
  tone: 'btn-primary' | 'btn-ghost' | 'btn-warn'
  requiresRemarks: boolean
}

// The API is the authority on which transition is legal; this list only keeps
// obviously-wrong buttons off the screen. An unexpected transition still comes
// back as a 409 with a readable message.
const ACTIONS_BY_STATUS: Record<string, ActionDefinition[]> = {
  Draft: [
    { action: 'submit', label: 'Submit for verification', tone: 'btn-primary', requiresRemarks: false },
    { action: 'cancel', label: 'Cancel', tone: 'btn-warn', requiresRemarks: true },
  ],
  Submitted: [
    { action: 'verify', label: 'Department verify', tone: 'btn-primary', requiresRemarks: false },
    { action: 'request-revision', label: 'Request revision', tone: 'btn-warn', requiresRemarks: true },
    { action: 'reject', label: 'Reject', tone: 'btn-warn', requiresRemarks: true },
    { action: 'hold', label: 'Hold', tone: 'btn-ghost', requiresRemarks: true },
  ],
  DepartmentVerified: [
    { action: 'approve', label: 'Approve', tone: 'btn-primary', requiresRemarks: false },
    { action: 'request-revision', label: 'Request revision', tone: 'btn-warn', requiresRemarks: true },
    { action: 'reject', label: 'Reject', tone: 'btn-warn', requiresRemarks: true },
    { action: 'hold', label: 'Hold', tone: 'btn-ghost', requiresRemarks: true },
  ],
  PendingApproval: [
    { action: 'approve', label: 'Approve', tone: 'btn-primary', requiresRemarks: false },
    { action: 'request-revision', label: 'Request revision', tone: 'btn-warn', requiresRemarks: true },
    { action: 'reject', label: 'Reject', tone: 'btn-warn', requiresRemarks: true },
    { action: 'hold', label: 'Hold', tone: 'btn-ghost', requiresRemarks: true },
  ],
  RevisionRequested: [
    { action: 'resubmit', label: 'Resubmit', tone: 'btn-primary', requiresRemarks: true },
    { action: 'cancel', label: 'Cancel', tone: 'btn-warn', requiresRemarks: true },
  ],
}

const EDITABLE_STATUSES = new Set(['Draft', 'RevisionRequested'])

function HistoryTable({ rows, empty }: { rows: PurchaseRequisitionHistorySummary[]; empty: string }) {
  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th>When</th>
            <th>Action</th>
            <th>From</th>
            <th>To</th>
            <th>By</th>
            <th>Role</th>
            <th>Remarks</th>
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 && <tr><td colSpan={7} className="table-empty">{empty}</td></tr>}
          {rows.map((row) => (
            <tr key={row.Id}>
              <td>{new Date(row.CreatedAt).toLocaleString('en-IN')}</td>
              <td>{row.Action}</td>
              <td>{row.PreviousStatus ?? '—'}</td>
              <td><StatusBadge value={row.NewStatus} /></td>
              <td className="mono">{row.ActorLoginId}</td>
              <td className="mono">{row.ActorRoleCode}</td>
              <td>{row.Remarks || '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export function PurchaseRequisitionDetailPage() {
  const { prNumber = '' } = useParams()
  const navigate = useNavigate()

  const [detail, setDetail] = useState<PurchaseRequisitionDetail | null>(null)
  const [statusHistory, setStatusHistory] = useState<PurchaseRequisitionHistorySummary[]>([])
  const [approvalHistory, setApprovalHistory] = useState<PurchaseRequisitionHistorySummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [remarks, setRemarks] = useState('')
  const [busy, setBusy] = useState<PurchaseRequisitionAction | null>(null)
  const [editing, setEditing] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setDetail(await getPurchaseRequisition(prNumber))
    } catch (err) {
      setDetail(null)
      setError(err)
    } finally {
      setLoading(false)
    }
    // History endpoints sit behind a separate ViewAuditHistory permission, so a
    // failure here must not blank the page.
    getPurchaseRequisitionStatusHistory(prNumber).then(setStatusHistory).catch(() => setStatusHistory([]))
    getPurchaseRequisitionApprovalHistory(prNumber).then(setApprovalHistory).catch(() => setApprovalHistory([]))
  }, [prNumber])

  useEffect(() => {
    void load()
  }, [load])

  const runAction = async (definition: ActionDefinition) => {
    if (!detail) return
    if (definition.requiresRemarks && !remarks.trim()) {
      setError(`Remarks are required to ${definition.label.toLowerCase()}.`)
      return
    }
    setError(null)
    setNotice('')
    setBusy(definition.action)
    try {
      const updated = await actOnPurchaseRequisition(detail.PrNumber, definition.action, {
        Remarks: remarks.trim(),
        Version: detail.Version,
      })
      setDetail(updated)
      setRemarks('')
      setNotice(`${definition.label} succeeded. Status is now ${updated.Status}.`)
      void load()
    } catch (err) {
      // A 409 here is the fail-closed approval-configuration refusal or a stale
      // Version; ErrorAlert turns both into something the requester can act on.
      setError(err)
    } finally {
      setBusy(null)
    }
  }

  if (loading && !detail) {
    return <div className="page"><p>Loading…</p></div>
  }

  if (!detail) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Requisition not found." />
        <button type="button" className="btn btn-ghost" onClick={() => navigate('/purchase/requisitions')}>
          ‹ Back to list
        </button>
      </div>
    )
  }

  const actions = ACTIONS_BY_STATUS[detail.Status] ?? []
  const canEdit = EDITABLE_STATUSES.has(detail.Status)

  return (
    <div className="page">
      <div className="breadcrumbs">
        <Link to="/purchase/requisitions">Purchase Requisition</Link> / <span className="mono">{detail.PrNumber}</span>
      </div>

      <div className="page-header">
        <div>
          <h1>{detail.PrNumber}</h1>
          <p className="page-sub">
            {detail.RequestingDepartment} · raised {formatDate(detail.RequestDate)} by{' '}
            <span className="mono">{detail.RequesterEmployeeCode}</span> · route {detail.ApprovalRoute || '—'}
          </p>
        </div>
        <div className="action-row">
          <StatusBadge value={detail.Status} />
          {canEdit && (
            <button type="button" className="btn btn-ghost" onClick={() => setEditing(true)}>Edit draft</button>
          )}
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />
      {notice && <div className="alert">{notice}</div>}

      <div className="card">
        <div className="detail-grid">
          <div className="detail-field"><span className="field-label">Required by</span>{formatDate(detail.RequiredByDate)}</div>
          <div className="detail-field"><span className="field-label">Priority</span>{detail.Priority}</div>
          <div className="detail-field"><span className="field-label">Delivery warehouse</span><span className="mono">{detail.DeliveryWarehouseCode || '—'}</span></div>
          <div className="detail-field"><span className="field-label">Estimated total</span>₹{formatAmount(detail.EstimatedTotal)}</div>
          <div className="detail-field"><span className="field-label">Cost centre</span>{detail.CostCentre ?? '—'}</div>
          <div className="detail-field"><span className="field-label">Project ref</span>{detail.ProjectReference ?? '—'}</div>
          <div className="detail-field"><span className="field-label">Service ref</span>{detail.ServiceReference ?? '—'}</div>
          <div className="detail-field"><span className="field-label">Work order ref</span>{detail.WorkOrderReference ?? '—'}</div>
          <div className="detail-field"><span className="field-label">Customer ref</span>{detail.CustomerReference ?? '—'}</div>
          <div className="detail-field">
            <span className="field-label">Customer PO</span>
            {detail.CustomerPoRecordNumber ? (
              <Link className="mono" to="/sales/customer-po">{detail.CustomerPoRecordNumber}</Link>
            ) : '—'}
          </div>
          <div className="detail-field field-wide">
            <span className="field-label">Purpose / justification</span>
            {detail.PurposeJustification}
          </div>
        </div>
      </div>

      <h2>Lines ({detail.Lines.length})</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>#</th>
              <th>Item</th>
              <th>Description</th>
              <th>UOM</th>
              <th className="text-right">Requested</th>
              <th className="text-right">Est. rate</th>
              <th className="text-right">Line total</th>
              <th className="text-right">On hand</th>
              <th className="text-right">Available</th>
              <th className="text-right">Reserved</th>
              <th className="text-right">Shortage</th>
              <th>Line status</th>
            </tr>
          </thead>
          <tbody>
            {detail.Lines.map((line) => (
              <tr key={line.Id}>
                <td className="mono">{line.LineNumber}</td>
                <td className="mono">{line.ItemCode}</td>
                <td>{line.ItemName}</td>
                <td>{line.Uom}</td>
                <td className="text-right mono">{formatAmount(line.RequestedQuantity)}</td>
                <td className="text-right mono">{formatAmount(line.EstimatedUnitPrice)}</td>
                <td className="text-right mono">{formatAmount(line.EstimatedLineTotal)}</td>
                <td className="text-right mono">{formatAmount(line.OnHand)}</td>
                <td className="text-right mono">{formatAmount(line.Available)}</td>
                <td className="text-right mono">{formatAmount(line.ReservedQuantity)}</td>
                <td className="text-right mono">{formatAmount(line.ShortageQuantity)}</td>
                <td><StatusBadge value={line.LineStatus} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {actions.length > 0 && (
        <div className="card">
          <div className="form-section-title">Workflow</div>
          <label className="field field-wide">
            <span className="field-label">Remarks</span>
            <textarea
              className="input"
              rows={2}
              value={remarks}
              placeholder="Required for reject, revision, hold, cancel and resubmit."
              onChange={(event) => setRemarks(event.target.value)}
            />
          </label>
          <div className="action-row">
            {actions.map((definition) => (
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
            Sent with record version {detail.Version}. If someone else changed this PR meanwhile the API
            returns a conflict — reload and retry.
          </p>
        </div>
      )}

      <h2>Approval history</h2>
      <HistoryTable rows={approvalHistory} empty="No approval events, or you lack the audit-history permission." />

      <h2>Status history</h2>
      <HistoryTable rows={statusHistory} empty="No status events, or you lack the audit-history permission." />

      {editing && (
        <PurchaseRequisitionFormModal
          mode="edit"
          existing={detail}
          onClose={() => setEditing(false)}
          onSaved={(updated) => {
            setEditing(false)
            setDetail(updated)
            void load()
          }}
        />
      )}
    </div>
  )
}
