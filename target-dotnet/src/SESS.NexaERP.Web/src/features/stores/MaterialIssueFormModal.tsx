import { useEffect, useMemo, useState } from 'react'
import { issueMaterialFromRequest, lookupMaterialIssueRecipients } from '../../api/materialIssues'
import type { MaterialIssueRecipientLookup } from '../../api/materialIssues'
import { newIdempotencyKey } from '../../api/stores'
import type { MaterialIssueRequestLineView, MaterialIssueRequestView, MaterialIssueView } from '../../types/materialIssue'
import { mirLineBalanceQuantity, mirLineTdRejected } from '../../types/materialIssue'
import { ErrorAlert } from '../../components/ErrorAlert'
import { ScannerInput } from '../../components/ScannerInput'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

interface Props {
  mir: MaterialIssueRequestView
  onClose: () => void
  onIssued: (result: MaterialIssueView) => void
}

interface ScanLine {
  line: MaterialIssueRequestLineView
  scanCode: string
  quantity: string
}

/** Server rule (EfMaterialIssueService.Issue.cs NormalizeScan): letters and digits only, upper-cased. */
function normalizeScan(value: string): string {
  return value.replace(/[^A-Za-z0-9]/g, '').toUpperCase()
}

function nowLocal(): string {
  const date = new Date()
  date.setSeconds(0, 0)
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
}

/**
 * POST /api/v1/stores/material-issues/from-request/{mirId} (page action issue,
 * Stores roles only). Custody moves from Stores to the named engineer; a
 * return is due one day later. Each scan must match the MIR line's item code
 * (a serialized item needs its exact serial and quantity 1, which this screen
 * cannot resolve yet because no serial lookup is exposed). Quantity is
 * checked on the server against what the MIR line still allows (earlier
 * partial issues count: previouslyIssued + now <= RequestedBaseQuantity) and
 * against AVAILABLE Stores custody; customer-facing excess needs the TD's
 * APPROVED decision first — a REJECTED one blocks the issue.
 *
 * When the line view carries IssuedBaseQuantity the balance is shown and
 * enforced here too; when it does not, the requested quantity is the only
 * client-side cap and the server has the final say.
 */
export function MaterialIssueFormModal({ mir, onClose, onIssued }: Props) {
  const { can, me } = useSession()
  const [employeeSearch, setEmployeeSearch] = useState('')
  const [employees, setEmployees] = useState<MaterialIssueRecipientLookup[]>([])
  const [recipientId, setRecipientId] = useState('')
  const [issuedAt, setIssuedAt] = useState(nowLocal())
  const [scans, setScans] = useState<ScanLine[]>([])
  const [scanMode, setScanMode] = useState(true)
  const [warning, setWarning] = useState('')
  const [lookupError, setLookupError] = useState<unknown>(null)
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    const term = employeeSearch.trim()
    const handle = window.setTimeout(() => {
      lookupMaterialIssueRecipients(term || undefined).then(setEmployees).catch(setLookupError)
    }, term ? 250 : 0)
    return () => window.clearTimeout(handle)
  }, [employeeSearch])

  const linesByCode = useMemo(() => {
    const map = new Map<string, MaterialIssueRequestLineView>()
    for (const line of mir.Lines) map.set(normalizeScan(line.ItemCode), line)
    return map
  }, [mir])

  /** What the line still allows in its own UOM: the balance when the API reports it, else the requested quantity. */
  const lineCap = (line: MaterialIssueRequestLineView): number => mirLineBalanceQuantity(line) ?? line.RequestedQuantity
  const hasBalance = mir.Lines.some((line) => mirLineBalanceQuantity(line) !== null)

  const onScan = (raw: string) => {
    const code = normalizeScan(raw)
    const line = linesByCode.get(code)
    if (!line) {
      setWarning(`“${raw}” matches no item on ${mir.RequestNumber}. Scan the item code printed on the bin or pick the line below.`)
      return
    }
    if (lineCap(line) <= 0) {
      setWarning(`Line ${line.LineNumber} (${line.ItemCode}) is fully issued already — nothing is left on ${mir.RequestNumber} for it.`)
      return
    }
    setWarning('')
    addLine(line, raw)
  }

  const addLine = (line: MaterialIssueRequestLineView, scanCode: string) =>
    setScans((current) => {
      const existing = current.find((scan) => scan.line.Id === line.Id)
      if (existing) {
        return current.map((scan) => scan.line.Id === line.Id
          ? { ...scan, quantity: String(Math.min(lineCap(line), Number(scan.quantity || 0) + 1)) }
          : scan)
      }
      return [...current, { line, scanCode, quantity: String(Math.min(1, lineCap(line))) }]
    })

  const canIssue = can(PAGE_KEYS.materialIssues, 'issue')
  const recipient = employees.find((employee) => employee.Id === recipientId)
  const customerFacingExcess = mir.Lines.filter((line) => line.ExcessBaseQuantity > 0 && line.ExcessClassification !== 'INTERNAL')
  const blockedExcess = customerFacingExcess.filter((line) => !line.TdDecisionPresent && !line.TdDecision)
  const rejectedExcess = customerFacingExcess.filter(mirLineTdRejected)
  const blocked = blockedExcess.length > 0 || rejectedExcess.length > 0

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    if (!recipientId) { setError('Pick the engineer who takes custody.'); return }
    if (recipientId === me?.EmployeeId) { setError('Stores cannot issue to itself; pick the engineer who receives the material.'); return }
    if (scans.length === 0) { setError('Scan at least one item.'); return }
    for (const scan of scans) {
      const qty = Number(scan.quantity)
      if (!(qty > 0)) { setError(`${scan.line.ItemCode}: quantity must be greater than zero.`); return }
      const balance = mirLineBalanceQuantity(scan.line)
      if (balance !== null && qty > balance) {
        setError(`${scan.line.ItemCode}: ${qty} exceeds the balance ${balance} ${scan.line.UomCode} still open on the MIR line (${scan.line.RequestedQuantity} requested, the rest issued earlier).`)
        return
      }
      if (qty > scan.line.RequestedQuantity) { setError(`${scan.line.ItemCode}: ${qty} exceeds the MIR line quantity ${scan.line.RequestedQuantity}.`); return }
    }
    setSaving(true)
    try {
      onIssued(await issueMaterialFromRequest(mir.Id, {
        IdempotencyKey: newIdempotencyKey('material-issue'),
        IssuedToEmployeeId: recipientId,
        IssuedAt: new Date(issuedAt).toISOString(),
        Scans: scans.map((scan) => ({
          MaterialIssueRequestLineId: scan.line.Id,
          ScanCode: scan.scanCode,
          InventorySerialId: null,
          Quantity: Number(scan.quantity),
        })),
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
          <h2>Issue material against {mir.RequestNumber}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>

        <form
          onSubmit={submit}
          className="form-grid"
          onKeyDown={(event) => {
            // A USB scanner ends with Enter; it must never submit the form.
            const target = event.target as HTMLElement
            if (event.key === 'Enter' && !target.closest('button[type="submit"]')) event.preventDefault()
          }}
        >
          {blockedExcess.length > 0 && (
            <div className="alert alert-warn field-wide" role="alert">
              <div className="alert-title">{blockedExcess.length} line(s) wait for the Technical Director's excess decision</div>
              <p className="alert-body">The server refuses the issue until each customer-facing excess line is approved by the TD.</p>
            </div>
          )}
          {rejectedExcess.length > 0 && (
            <div className="alert alert-warn field-wide" role="alert">
              <div className="alert-title">The TD rejected the excess on line {rejectedExcess.map((line) => line.LineNumber).join(', ')}</div>
              <p className="alert-body">This MIR cannot be issued as it stands. Edit the MIR quantity back within the limit, or raise a new MIR.</p>
            </div>
          )}

          <label className="field">
            <span className="field-label">Find engineer</span>
            <input className="input search" placeholder="Employee code, name or department…" value={employeeSearch} onChange={(event) => setEmployeeSearch(event.target.value)} />
          </label>
          <label className="field">
            <span className="field-label">Issued to *</span>
            <select className="input" value={recipientId} onChange={(event) => setRecipientId(event.target.value)}>
              <option value="">{employees.length === 0 ? 'No eligible employee matches.' : `Pick from ${employees.length}…`}</option>
              {employees.map((employee) => <option key={employee.Id} value={employee.Id}>{employee.EmployeeCode} — {employee.EmployeeName} ({employee.Department})</option>)}
              {recipient === undefined && recipientId && <option value={recipientId}>{recipientId}</option>}
            </select>
            <span className="field-hint">Custody moves to this person; only they can declare the return, due one day after issue.</span>
          </label>

          <label className="field">
            <span className="field-label">Issued at *</span>
            <input className="input" type="datetime-local" value={issuedAt} onChange={(event) => setIssuedAt(event.target.value)} />
          </label>
          <div className="field">
            <span className="field-label">Destination</span>
            <div style={{ paddingTop: 6 }}>{mir.DestinationType.replaceAll('_', ' ')} · {mir.DestinationName}{mir.JobOrderId ? <span className="field-hint mono">Job order {mir.JobOrderId}</span> : null}</div>
          </div>

          <div className="field-wide">
            <ScannerInput
              label="Scan item"
              placeholder="Item code barcode…"
              scanMode={scanMode}
              onScan={onScan}
              warning={warning}
              hint={`${scans.length} of ${mir.Lines.length} MIR lines scanned. Each scan adds one; adjust the quantity below.`}
            />
            <label className="field-hint"><input type="checkbox" checked={!scanMode} onChange={(event) => setScanMode(!event.target.checked)} /> Keyboard mode (release scanner focus)</label>
          </div>

          <ErrorAlert error={lookupError} className="field-wide" fallback="Employee lookup failed." />

          <div className="field-wide table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Item</th>
                  <th className="text-right">Requested</th>
                  <th className="text-right" title={hasBalance ? 'Still open on the line after earlier issues' : 'This API does not report earlier issues; the server enforces the balance.'}>Balance</th>
                  <th className="text-right">Issue qty</th>
                  <th>Scan</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {mir.Lines.map((line) => {
                  const scan = scans.find((candidate) => candidate.line.Id === line.Id)
                  const balance = mirLineBalanceQuantity(line)
                  const exhausted = balance !== null && balance <= 0
                  return (
                    <tr key={line.Id}>
                      <td className="mono">{line.LineNumber}</td>
                      <td>
                        <span className="mono">{line.ItemCode}</span> — {line.ItemName}
                        {mirLineTdRejected(line) && <span className="field-hint">TD rejected the excess</span>}
                      </td>
                      <td className="text-right mono">{line.RequestedQuantity} {line.UomCode}</td>
                      <td className="text-right mono">
                        {balance === null
                          ? <span title="Not reported by this API; the server enforces the balance.">—</span>
                          : exhausted ? <span title="Fully issued">0 {line.UomCode}</span> : `${balance} ${line.UomCode}`}
                      </td>
                      <td className="text-right">
                        {scan ? (
                          <input className="input text-right mono" inputMode="decimal" value={scan.quantity} onChange={(event) => setScans((current) => current.map((row) => row.line.Id === line.Id ? { ...row, quantity: event.target.value } : row))} />
                        ) : '—'}
                      </td>
                      <td className="mono">{scan ? scan.scanCode : <button type="button" className="btn btn-ghost" disabled={exhausted} title={exhausted ? 'Fully issued already; nothing is left on this line.' : undefined} onClick={() => addLine(line, line.ItemCode)}>Add without scan</button>}</td>
                      <td>{scan && <button type="button" className="btn btn-ghost" onClick={() => setScans((current) => current.filter((row) => row.line.Id !== line.Id))}>✕</button>}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <ErrorAlert error={error} className="field-wide" fallback="Could not issue the material." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={saving || !canIssue || blocked}
              title={!canIssue ? 'Only Stores roles may issue material.'
                : rejectedExcess.length > 0 ? `The TD rejected the excess on line ${rejectedExcess.map((line) => line.LineNumber).join(', ')}; edit the MIR quantity or raise a new MIR.`
                : blockedExcess.length > 0 ? 'Customer-facing excess awaits the TD decision.'
                : undefined}
            >
              {saving ? 'Issuing…' : `Issue ${scans.length} line(s)`}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
