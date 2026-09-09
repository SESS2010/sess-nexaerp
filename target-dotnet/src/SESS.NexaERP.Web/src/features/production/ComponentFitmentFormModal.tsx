import { useEffect, useState } from 'react'
import { getMaterialIssue, getMaterialIssueRequest, listOutstandingCustody } from '../../api/materialIssues'
import { confirmComponentFitment, listJobOrders } from '../../api/production'
import { newIdempotencyKey } from '../../api/stores'
import type { ComponentFitmentSummary, JobOrderSummary } from '../../types/production'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

interface Props {
  /** Preselected job order; the picker stays available when empty. */
  jobOrderId: string
  /** A reversed fitment being re-verified: the new fitment links to it. */
  reverifies?: ComponentFitmentSummary
  onClose: () => void
  onSaved: (result: ComponentFitmentSummary) => void
}

/** One material-issue line still in an engineer's custody for the job. */
interface CustodyLine {
  materialIssueLineId: string
  issueNumber: string
  itemCode: string
  itemName: string
  custodianCode: string
  quantityBase: number
}

function nowLocal(): string {
  const date = new Date()
  date.setSeconds(0, 0)
  const offset = date.getTimezoneOffset() * 60000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}

/**
 * POST /api/v1/production/component-fitments. The line must be a material
 * issue line of this job whose custody is still with the engineer. The
 * outstanding-custody view (Stores) lists exactly those, so it is offered as
 * the picker; a role without that page can still paste the line id.
 */
export function ComponentFitmentFormModal({ jobOrderId: initialJobOrderId, reverifies, onClose, onSaved }: Props) {
  const { can } = useSession()
  const [jobs, setJobs] = useState<JobOrderSummary[]>([])
  const [jobOrderId, setJobOrderId] = useState(initialJobOrderId)
  const [custody, setCustody] = useState<CustodyLine[]>([])
  const [custodyError, setCustodyError] = useState<unknown>(null)
  const [loadingCustody, setLoadingCustody] = useState(false)
  const [lineId, setLineId] = useState(reverifies?.MaterialIssueLineId ?? '')
  const [quantity, setQuantity] = useState(reverifies ? String(reverifies.QuantityBase) : '')
  const [fittedAt, setFittedAt] = useState(nowLocal())
  const [note, setNote] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    listJobOrders({ page: 1, pageSize: 100, status: 'OPEN' }).then((page) => setJobs(page.Items ?? [])).catch(setError)
  }, [])

  useEffect(() => {
    if (!jobOrderId) { setCustody([]); return }
    let cancelled = false
    setLoadingCustody(true)
    setCustodyError(null)
    ;(async () => {
      try {
        const outstanding = (await listOutstandingCustody()).filter((row) => row.JobOrderId === jobOrderId)
        const lines: CustodyLine[] = []
        for (const held of outstanding) {
          const issue = await getMaterialIssue(held.MaterialIssueId)
          const request = await getMaterialIssueRequest(issue.MaterialIssueRequestId)
          for (const line of issue.Lines) {
            const requestLine = request.Lines.find((candidate) => candidate.Id === line.MaterialIssueRequestLineId)
            lines.push({
              materialIssueLineId: line.Id,
              issueNumber: issue.IssueNumber,
              itemCode: requestLine?.ItemCode ?? line.ItemId,
              itemName: requestLine?.ItemName ?? '',
              custodianCode: held.EmployeeCode,
              quantityBase: line.QuantityBase,
            })
          }
        }
        if (!cancelled) setCustody(lines)
      } catch (err) {
        if (!cancelled) setCustodyError(err)
      } finally {
        if (!cancelled) setLoadingCustody(false)
      }
    })()
    return () => { cancelled = true }
  }, [jobOrderId])

  const canSave = can(PAGE_KEYS.componentFitments, 'create')
  const picked = custody.find((line) => line.materialIssueLineId === lineId)

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    if (!jobOrderId) { setError('Pick the job order the component is fitted to.'); return }
    if (!lineId.trim()) { setError('Pick the issued line (or paste its id).'); return }
    const qty = Number(quantity)
    if (!(qty > 0)) { setError('Quantity must be greater than zero.'); return }
    if (picked && qty > picked.quantityBase) { setError(`Only ${picked.quantityBase} base unit(s) were issued on that line.`); return }
    if (!fittedAt) { setError('Fitted-at time is required.'); return }
    if (!note.trim()) { setError('A confirmation note is required.'); return }
    setSaving(true)
    try {
      onSaved(await confirmComponentFitment({
        JobOrderId: jobOrderId,
        MaterialIssueLineId: lineId.trim(),
        QuantityBase: qty,
        FittedAt: new Date(fittedAt).toISOString(),
        ConfirmationNote: note.trim(),
        ReverifiesFitmentId: reverifies?.Id ?? null,
        IdempotencyKey: newIdempotencyKey('fitment-confirm'),
      }))
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal modal-wide" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{reverifies ? `Re-verify ${reverifies.FitmentNumber}` : 'Confirm component fitment'}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={submit} className="form-grid">
          <label className="field">
            <span className="field-label">Job order *</span>
            <select className="input" value={jobOrderId} disabled={Boolean(reverifies)} onChange={(event) => { setJobOrderId(event.target.value); setLineId('') }}>
              <option value="">Pick an OPEN job order…</option>
              {jobs.map((job) => <option key={job.Id} value={job.Id}>{job.JobOrderNumber} · {job.CustomerName} · {job.MachineSerial}</option>)}
              {jobOrderId && !jobs.some((job) => job.Id === jobOrderId) && <option value={jobOrderId}>{reverifies?.JobOrderNumber ?? jobOrderId}</option>}
            </select>
          </label>

          <label className="field">
            <span className="field-label">Fitted at *</span>
            <input className="input" type="datetime-local" value={fittedAt} onChange={(event) => setFittedAt(event.target.value)} />
          </label>

          <label className="field field-wide">
            <span className="field-label">Issued line in engineer custody *</span>
            {custody.length > 0 ? (
              <select className="input" value={lineId} onChange={(event) => { setLineId(event.target.value); const line = custody.find((candidate) => candidate.materialIssueLineId === event.target.value); if (line) setQuantity(String(line.quantityBase)) }}>
                <option value="">Pick the issued line…</option>
                {custody.map((line) => (
                  <option key={line.materialIssueLineId} value={line.materialIssueLineId}>
                    {line.issueNumber} · {line.itemCode}{line.itemName ? ` — ${line.itemName}` : ''} · {line.quantityBase} base · held by {line.custodianCode}
                  </option>
                ))}
              </select>
            ) : (
              <input className="input mono" placeholder="Material issue line id (GUID)" value={lineId} onChange={(event) => setLineId(event.target.value)} />
            )}
            {loadingCustody && <span className="field-hint">Loading outstanding custody for this job…</span>}
            {!loadingCustody && jobOrderId && custody.length === 0 && !custodyError && (
              <span className="field-hint">No outstanding engineer custody found for this job. Material must be issued against a job-backed MIR before it can be fitted.</span>
            )}
          </label>
          <ErrorAlert error={custodyError} className="field-wide" fallback="Outstanding custody could not be loaded; paste the line id instead." />

          <label className="field">
            <span className="field-label">Quantity (base) *</span>
            <input className="input mono text-right" inputMode="decimal" value={quantity} onChange={(event) => setQuantity(event.target.value)} />
            {picked && <span className="field-hint">Issued on this line: {picked.quantityBase}</span>}
          </label>

          <label className="field">
            <span className="field-label">Confirmation note *</span>
            <input className="input" value={note} onChange={(event) => setNote(event.target.value)} placeholder="e.g. Fitted to chamber door assembly, torque checked" />
          </label>

          <ErrorAlert error={error} className="field-wide" fallback="Could not confirm the fitment." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving || !canSave} title={canSave ? undefined : 'Your role cannot confirm fitments.'}>
              {saving ? 'Confirming…' : 'Confirm fitment'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
