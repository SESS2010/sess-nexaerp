import { useCallback, useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { correctQcInspection, getQcInspection } from '../../api/qc'
import { listGoodsReceipts } from '../../api/goodsReceipts'
import type { QcInspectionResult } from '../../types/qc'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'
import { QcDispositionForm, type QcDispositionValues, type QcSerialSource } from './QcDispositionForm'

/**
 * One finalized QC inspection, read by number. The current revision is
 * immutable; a correction reverses its stock posting and finalizes the next
 * revision with the corrected disposition.
 */
export function QcInspectionPage() {
  const { number = '' } = useParams()
  const location = useLocation()
  const navigate = useNavigate()
  const { can, hasRole } = useSession()
  const [inspection, setInspection] = useState<QcInspectionResult | null>(null)
  const [serials, setSerials] = useState<QcSerialSource[]>([])
  const [hasPolicy, setHasPolicy] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState<string>((location.state as { notice?: string } | null)?.notice ?? '')
  const [correcting, setCorrecting] = useState(false)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const loaded = await getQcInspection(number)
      setInspection(loaded)
      // Parameter results on the revision prove an effective policy existed
      // at inspection time; a correction must then supply them again.
      setHasPolicy(loaded.ParameterResults.length > 0)
      try {
        const receipts = await listGoodsReceipts({ page: 1, pageSize: 1, grnNumber: loaded.GrnNumber })
        const grn = receipts.Items?.[0]
        const line = grn?.Lines.find((candidate) => candidate.Lots.some((lot) => lot.Id === loaded.GoodsReceiptLineLotAllocationId))
        setSerials(
          (line?.Serials ?? [])
            .filter((serial) => serial.LotOrdinal === loaded.LotOrdinal && serial.InventorySerialId)
            .map((serial) => ({ inventorySerialId: serial.InventorySerialId as string, serialNumber: serial.StoredSerialNumber })),
        )
      } catch {
        // The GRN read only enriches the correction form with serial identities;
        // the inspection itself is still shown.
        setSerials(loaded.SerialDispositions.map((serial) => ({ inventorySerialId: serial.InventorySerialId, serialNumber: serial.SerialNumber })))
      }
    } catch (err) {
      setInspection(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [number])

  useEffect(() => {
    void load()
  }, [load])

  const correct = async (values: QcDispositionValues, idempotencyKey: string, reason: string) => {
    if (!inspection) return
    setBusy(true)
    setNotice('')
    try {
      const result = await correctQcInspection(
        inspection.InspectionNumber,
        {
          RevisesRevisionId: inspection.RevisionId,
          CorrectionReason: reason,
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
      setInspection(result)
      setCorrecting(false)
      setNotice(result.Replayed ? 'This correction was already recorded.' : `Correction posted — revision ${result.RevisionNumber} is now the finalized inspection.`)
    } finally {
      setBusy(false)
    }
  }

  // POST /qc/inspections/{number}/corrections: qc.inspection-policies:update plus a
  // direct QC_MANAGER role (CorrectAsync → FinalizeCore → RequireQcManager).
  const canCorrect = can(PAGE_KEYS.qc, 'update') && hasRole('QC_MANAGER')
  // The link only navigates, but it lands on the concession create form
  // (POST /api/v1/qc/concessions → qc.inspection-policies:create, no role check).
  const canRaiseConcession = can(PAGE_KEYS.qc, 'create')

  if (loading && !inspection) return <div className="page"><p>Loading…</p></div>
  if (!inspection) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Inspection not found." />
        <button type="button" className="btn btn-ghost" onClick={() => navigate('/qc/inspections')}>‹ Back to queue</button>
      </div>
    )
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="mono">{inspection.InspectionNumber}</h1>
          <p className="page-sub">
            <span className="mono">{inspection.GrnNumber}</span> lot {inspection.LotOrdinal} · <span className="mono">{inspection.ItemCode}</span>
            {' · '}{inspection.InspectedQuantity} inspected · revision {inspection.RevisionNumber}
          </p>
        </div>
        <div className="action-row">
          <StatusBadge value={inspection.Decision} />
          <StatusBadge value={inspection.Status} />
          <button type="button" className="btn btn-ghost" onClick={() => navigate('/qc/inspections')}>‹ Queue</button>
        </div>
      </div>

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />

      <div className="card">
        <h2 className="form-section-title">Disposition</h2>
        <div className="detail-grid">
          <div><span className="field-label">Accepted</span><div className="mono">{inspection.AcceptedQuantity}</div></div>
          <div><span className="field-label">Rejected</span><div className="mono">{inspection.RejectedQuantity}</div></div>
          <div><span className="field-label">Discrepancy pending</span><div className="mono">{inspection.DiscrepancyPendingQuantity}</div></div>
          <div><span className="field-label">Decision</span><div>{inspection.Decision}</div></div>
          <div><span className="field-label">Inspector basis</span><div>{inspection.InspectorBasis}</div></div>
          <div><span className="field-label">Stock posting batch</span><div className="mono">{inspection.StockPostingBatchId ?? '— (no stock moved)'}</div></div>
        </div>

        {inspection.ParameterResults.length > 0 && (
          <>
            <h2 className="form-section-title">Parameter results</h2>
            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>Parameter</th><th>Measured</th><th>Result</th></tr></thead>
                <tbody>
                  {inspection.ParameterResults.map((row) => (
                    <tr key={row.Id}>
                      <td className="mono">{row.ParameterCode}</td>
                      <td className="mono">{row.MeasuredValue}</td>
                      <td>
                        <StatusBadge value={row.Result} />
                        {row.Result === 'FAIL' && inspection.RejectedQuantity > 0 && canRaiseConcession && (
                          <button type="button" className="link-button" onClick={() => navigate('/qc/concessions', {
                            state: { prefill: { failedParameterResultId: row.Id, failedParameter: row.ParameterCode, measuredValue: row.MeasuredValue, inspectionNumber: inspection.InspectionNumber } },
                          })}>
                            Raise concession
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}

        {inspection.SerialDispositions.length > 0 && (
          <>
            <h2 className="form-section-title">Serial dispositions</h2>
            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>#</th><th>Serial</th><th>Disposition</th></tr></thead>
                <tbody>
                  {inspection.SerialDispositions.map((row, index) => (
                    <tr key={row.InventorySerialId}>
                      <td className="mono">{index + 1}</td>
                      <td className="mono">{row.SerialNumber}</td>
                      <td><StatusBadge value={row.Disposition} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>

      {canCorrect && (!correcting ? (
        <div className="reverse-panel">
          <h2 className="form-section-title">Correct this inspection</h2>
          <p className="field-hint">
            A finalized revision is immutable. A correction reverses its stock posting and finalizes a new revision
            with the corrected disposition; the reason is kept on the record.
          </p>
          <button type="button" className="btn btn-warn" disabled={busy} onClick={() => setCorrecting(true)}>Open correction</button>
        </div>
      ) : (
        <>
          <h2 className="form-section-title">Correction of revision {inspection.RevisionNumber}</h2>
          <QcDispositionForm
            quantity={inspection.InspectedQuantity}
            serials={serials}
            hasEffectivePolicy={hasPolicy}
            initial={{
              acceptedQuantity: inspection.AcceptedQuantity,
              rejectedQuantity: inspection.RejectedQuantity,
              serialDispositions: inspection.SerialDispositions,
            }}
            correction
            busy={busy}
            submitLabel="Post correction"
            onSubmit={correct}
          />
          <button type="button" className="btn btn-ghost mt-2" disabled={busy} onClick={() => setCorrecting(false)}>Cancel</button>
        </>
      ))}
    </div>
  )
}
