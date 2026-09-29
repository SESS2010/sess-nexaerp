import { useEffect, useState } from 'react'
import { createJobOrder, lookupJobOrderCustomerPoLines } from '../../api/production'
import { newIdempotencyKey } from '../../api/stores'
import type { JobOrderCustomerPoLineLookup, JobOrderView } from '../../types/production'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

interface Props {
  onClose: () => void
  onSaved: (result: JobOrderView) => void
}

function today(): string {
  return new Date().toISOString().slice(0, 10)
}

/**
 * POST /api/v1/production/job-orders. The line comes from the Job Order page's
 * own Customer PO line lookup (current revision, PO not completed) and must
 * carry a positive whole Quantity; MachineOrdinal picks which machine of that
 * quantity this job builds, and one ordinal cannot be used twice. A serial
 * already used in the company is refused.
 */
export function JobOrderFormModal({ onClose, onSaved }: Props) {
  const { can } = useSession()
  const [search, setSearch] = useState('')
  const [options, setOptions] = useState<JobOrderCustomerPoLineLookup[]>([])
  const [searching, setSearching] = useState(false)
  const [lineId, setLineId] = useState('')
  const [ordinal, setOrdinal] = useState('1')
  const [serial, setSerial] = useState('')
  const [jobOrderDate, setJobOrderDate] = useState(today())
  const [plannedCompletion, setPlannedCompletion] = useState('')
  const [lookupError, setLookupError] = useState<unknown>(null)
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    const term = search.trim()
    const handle = window.setTimeout(() => {
      setSearching(true)
      setLookupError(null)
      lookupJobOrderCustomerPoLines(term || undefined)
        .then(setOptions)
        .catch(setLookupError)
        .finally(() => setSearching(false))
    }, term ? 250 : 0)
    return () => window.clearTimeout(handle)
  }, [search])

  const line = options.find((item) => item.CustomerPurchaseOrderLineId === lineId)
  const maxOrdinal = line?.Quantity && Number.isInteger(line.Quantity) ? line.Quantity : 0
  const freeOrdinals = line ? Array.from({ length: maxOrdinal }, (_, index) => index + 1).filter((value) => !line.TakenOrdinals.includes(value)) : []
  const canSave = can(PAGE_KEYS.jobOrders, 'create')

  const pickLine = (id: string) => {
    setLineId(id)
    const picked = options.find((item) => item.CustomerPurchaseOrderLineId === id)
    const max = picked?.Quantity && Number.isInteger(picked.Quantity) ? picked.Quantity : 0
    const first = Array.from({ length: max }, (_, index) => index + 1).find((value) => !picked?.TakenOrdinals.includes(value))
    setOrdinal(first ? String(first) : '1')
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    if (!line) { setError('Pick the Customer PO machine line the job builds.'); return }
    if (!(maxOrdinal > 0)) { setError(`Line ${line.SlNo} has no positive whole quantity, so it cannot be a machine line.`); return }
    const machineOrdinal = Number(ordinal)
    if (!Number.isInteger(machineOrdinal) || machineOrdinal < 1 || machineOrdinal > maxOrdinal) {
      setError(`Machine ordinal must be between 1 and ${maxOrdinal} (the line quantity).`)
      return
    }
    if (line.TakenOrdinals.includes(machineOrdinal)) { setError(`Machine #${machineOrdinal} on this line already has a Job Order.`); return }
    if (!serial.trim()) { setError('Machine serial is required and must be unique in the company.'); return }
    if (!jobOrderDate) { setError('Job order date is required.'); return }
    setSaving(true)
    try {
      onSaved(await createJobOrder({
        CustomerPurchaseOrderLineId: line.CustomerPurchaseOrderLineId,
        MachineOrdinal: machineOrdinal,
        MachineSerial: serial.trim(),
        JobOrderDate: jobOrderDate,
        PlannedCompletionDate: plannedCompletion || null,
        IdempotencyKey: newIdempotencyKey('job-order-create'),
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
          <h2>New Job Order</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={submit} className="form-grid">
          <div className="field-wide form-section-title">Customer PO line</div>

          <label className="field field-wide">
            <span className="field-label">Find Customer PO line</span>
            <input
              className="input search"
              placeholder="PO number, customer, line description or item code… (open POs, current revision)"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
            {searching && <span className="field-hint">Searching…</span>}
          </label>

          <label className="field field-wide">
            <span className="field-label">Machine line *</span>
            <select className="input" value={lineId} onChange={(event) => pickLine(event.target.value)}>
              <option value="">{options.length === 0 ? (searching ? 'Loading…' : 'No open Customer PO lines match.') : `Pick from ${options.length} line(s)…`}</option>
              {options.map((item) => {
                const whole = item.Quantity !== null && item.Quantity > 0 && Number.isInteger(item.Quantity)
                const free = whole ? item.Quantity! - Math.max(item.TakenOrdinals.length, item.CreatedJobOrderCount) : 0
                return (
                  <option key={item.CustomerPurchaseOrderLineId} value={item.CustomerPurchaseOrderLineId} disabled={!whole || free === 0}>
                    {item.PoRecordNumber} · {item.CustomerPoNumber} · {item.CustomerName} — line {item.SlNo}: {item.Description} [{item.ItemCode}] qty {item.Quantity ?? '—'} {item.Uom ?? ''}
                    {!whole ? ' (no whole quantity)' : free === 0 ? ' (all machines have job orders)' : free < item.Quantity! ? ` (${free} of ${item.Quantity} free)` : ''}
                  </option>
                )
              })}
            </select>
            <span className="field-hint">Only lines of the current PO revision on POs that are not Completed are offered. Machine model comes from the line's item.</span>
          </label>

          <ErrorAlert error={lookupError} className="field-wide" fallback="Customer PO line lookup failed." />

          <div className="field-wide form-section-title">Machine</div>

          <label className="field">
            <span className="field-label">Machine ordinal *</span>
            <select className="input mono" value={ordinal} disabled={!line} onChange={(event) => setOrdinal(event.target.value)}>
              {!line && <option value="1">—</option>}
              {line && Array.from({ length: maxOrdinal }, (_, index) => index + 1).map((value) => (
                <option key={value} value={String(value)} disabled={line.TakenOrdinals.includes(value)}>
                  #{value}{line.TakenOrdinals.includes(value) ? ' — already has a Job Order' : ''}
                </option>
              ))}
            </select>
            <span className="field-hint">{line ? `Which of the ${maxOrdinal} machine(s) on this line. ${freeOrdinals.length} free.` : 'Pick a line first.'}</span>
          </label>

          <label className="field">
            <span className="field-label">Machine serial *</span>
            <input className="input mono" value={serial} onChange={(event) => setSerial(event.target.value)} placeholder="Unique in this company" />
          </label>

          <label className="field">
            <span className="field-label">Job order date *</span>
            <input className="input" type="date" value={jobOrderDate} onChange={(event) => setJobOrderDate(event.target.value)} />
          </label>

          <label className="field">
            <span className="field-label">Planned completion</span>
            <input className="input" type="date" value={plannedCompletion} onChange={(event) => setPlannedCompletion(event.target.value)} />
          </label>

          <ErrorAlert error={error} className="field-wide" fallback="Could not create the job order." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving || !canSave} title={canSave ? undefined : 'Your role cannot create job orders.'}>
              {saving ? 'Creating…' : 'Create (pending Accounts)'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
