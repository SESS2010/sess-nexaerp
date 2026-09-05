import { useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { finalizeQcInspection, findQcQueueItem } from '../../api/qc'
import { listGoodsReceipts } from '../../api/goodsReceipts'
import type { QcQueueItem } from '../../types/qc'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'
import { QcDispositionForm, type QcDispositionValues, type QcSerialSource } from './QcDispositionForm'

/**
 * Start-to-finalize inspection of one GRN lot allocation. The queue row is
 * passed through router state; on a refresh it is re-found by walking the
 * queue. Serialized units come from the GRN itself (the queue does not carry
 * them). One POST finalizes and posts stock — there is no draft on the server.
 */
export function QcInspectPage() {
  const { allocationId = '' } = useParams()
  const location = useLocation()
  const navigate = useNavigate()
  const { can, hasRole } = useSession()
  const [item, setItem] = useState<QcQueueItem | null>((location.state as { item?: QcQueueItem } | null)?.item ?? null)
  const [serials, setSerials] = useState<QcSerialSource[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      setLoading(true)
      setError(null)
      try {
        const row = item ?? (await findQcQueueItem(allocationId))
        if (!row) {
          throw new Error('This lot allocation is no longer in the QC queue — it may already be inspected. Open the inspection by its number instead.')
        }
        if (cancelled) return
        setItem(row)
        // The GRN carries the serial identities for this line and lot.
        const receipts = await listGoodsReceipts({ page: 1, pageSize: 1, grnNumber: row.GrnNumber })
        const grn = receipts.Items?.[0]
        const line = grn?.Lines.find((candidate) => candidate.Id === row.GoodsReceiptLineId)
        const units = (line?.Serials ?? [])
          .filter((serial) => serial.LotOrdinal === row.LotOrdinal && serial.InventorySerialId)
          .map((serial) => ({ inventorySerialId: serial.InventorySerialId as string, serialNumber: serial.StoredSerialNumber }))
        if (!cancelled) setSerials(units)
      } catch (err) {
        if (!cancelled) setError(err)
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => { cancelled = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [allocationId])

  const finalize = async (values: QcDispositionValues, idempotencyKey: string) => {
    if (!item) return
    setBusy(true)
    try {
      const result = await finalizeQcInspection(
        {
          GoodsReceiptLineLotAllocationId: item.GoodsReceiptLineLotAllocationId,
          InspectionStartedAt: values.inspectionStartedAt,
          AcceptedQuantity: values.acceptedQuantity,
          RejectedQuantity: values.rejectedQuantity,
          DiscrepancyPendingQuantity: values.discrepancyPendingQuantity,
          AcceptedConditionLocationId: values.acceptedConditionLocationId,
          ParameterResults: [],
          SerialDispositions: values.serialDispositions,
        },
        idempotencyKey,
      )
      navigate(`/qc/inspections/${encodeURIComponent(result.InspectionNumber)}`, {
        replace: true,
        state: { notice: result.Replayed ? 'This finalize was already recorded — nothing new was written.' : 'Inspection finalized and stock posted.' },
      })
    } finally {
      setBusy(false)
    }
  }

  const canFinalize = can(PAGE_KEYS.qc, 'create') && hasRole('QC_MANAGER')

  if (loading) return <div className="page"><p>Loading…</p></div>
  if (!item || serials === null) {
    return (
      <div className="page">
        <ErrorAlert error={error} fallback="Lot allocation not found." />
        <button type="button" className="btn btn-ghost" onClick={() => navigate('/qc/inspections')}>‹ Back to queue</button>
      </div>
    )
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Inspect <span className="mono">{item.GrnNumber}</span> line {item.LineNumber}, lot {item.LotOrdinal}</h1>
          <p className="page-sub">
            <span className="mono">{item.ItemCode}</span> {item.ItemName} · {item.Quantity} received
            {item.SupplierLotNumber && <> · supplier lot <span className="mono">{item.SupplierLotNumber}</span></>}
            {' · '}received {new Date(item.ReceivedAt).toLocaleString('en-IN')} · {item.AgeDays} of {item.CompletionLimitDays} days
          </p>
        </div>
        <div className="action-row">
          {item.IsOverdue && <span className="badge badge-error">Overdue</span>}
          <button type="button" className="btn btn-ghost" onClick={() => navigate('/qc/inspections')}>‹ Queue</button>
        </div>
      </div>

      <ErrorAlert error={error} fallback="Could not load the GRN for this lot." />

      {/* POST /api/v1/qc/inspections requires qc.inspection-policies:create plus a
          direct QC_MANAGER role (EfQcWorkflowService.FinalizeCore → RequireQcManager). */}
      {canFinalize && (
        <QcDispositionForm
          quantity={item.Quantity}
          serials={serials}
          hasEffectivePolicy={item.HasEffectivePolicy}
          busy={busy}
          submitLabel="Finalize & post stock"
          onSubmit={finalize}
        />
      )}
    </div>
  )
}
