import { getCompany } from '../../auth/authSession'
import { useEffect, useRef, useState } from 'react'
import { issueMaterialFromRequest, lookupMaterialIssueRecipients } from '../../api/materialIssues'
import type { MaterialIssueRecipientLookup } from '../../api/materialIssues'
import { newIdempotencyKey } from '../../api/stores'
import type { MaterialIssueRequestLineView, MaterialIssueRequestView, MaterialIssueView } from '../../types/materialIssue'
import { mirLineTdRejected } from '../../types/materialIssue'
import { ErrorAlert } from '../../components/ErrorAlert'
import { BarcodeScanField } from '../barcodes/BarcodeScanField'
import { resolveScan } from '../barcodes/barcodeScan'
import type { ScanCandidate } from '../barcodes/barcodeScan'
import { appendIssueScan, issueBaseBalance } from '../barcodes/issueScanDraft'
import { getIssueScanCandidates } from '../../api/barcodes'
import { barcodeSessionKey } from '../barcodes/barcodeSession'
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
  inventorySerialId: string | null
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
 * resolves through the authorized barcode/serial lookup). Quantity is
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
  const scansRef = useRef<ScanLine[]>([])
  const updateScans = (change: (current: ScanLine[]) => ScanLine[]) => {
    const next = change(scansRef.current); scansRef.current = next; setScans(next)
  }
  const [warning, setWarning] = useState('')
  const [serialChoices, setSerialChoices] = useState<ScanCandidate[]>([])
  const [selectedLineId, setSelectedLineId] = useState('')
  const [lookupError, setLookupError] = useState<unknown>(null)
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)
  const [barcodePending, setBarcodePending] = useState(false)
  const barcodePendingRef = useRef(false)
  const scanPendingChanged = (pending: boolean) => { barcodePendingRef.current = pending; setBarcodePending(pending) }

  const scanContext = barcodeSessionKey(me) + '|' + mir.Id + '|' + mir.Version
  useEffect(() => { scansRef.current = []; setScans([]); setWarning(''); setSerialChoices([]); setSelectedLineId('') }, [scanContext])

  useEffect(() => {
    const term = employeeSearch.trim()
    const handle = window.setTimeout(() => {
      lookupMaterialIssueRecipients(term || undefined).then(setEmployees).catch(setLookupError)
    }, term ? 250 : 0)
    return () => window.clearTimeout(handle)
  }, [employeeSearch])

  const lineCap = (line: MaterialIssueRequestLineView): number => issueBaseBalance(line) ?? line.RequestedBaseQuantity
  const hasBalance = mir.Lines.some((line) => issueBaseBalance(line) !== null)
  const addLine = (line: MaterialIssueRequestLineView, scanCode: string, inventorySerialId: string | null = null) => {
    const result = appendIssueScan(scansRef.current.map(row => ({ lineId: row.line.Id, scanCode: row.scanCode,
      inventorySerialId: row.inventorySerialId, quantity: row.quantity })), { lineId: line.Id, scanCode, inventorySerialId }, lineCap(line))
    setWarning(result.warning)
    const next = result.rows.map(row => ({ line: mir.Lines.find(item => item.Id === row.lineId)!,
      scanCode: row.scanCode, inventorySerialId: row.inventorySerialId, quantity: row.quantity }))
    updateScans(() => next)
  }
  const canIssue = can(PAGE_KEYS.materialIssues, 'issue')
  const recipient = employees.find((employee) => employee.Id === recipientId)
  const customerFacingExcess = mir.Lines.filter((line) => line.ExcessBaseQuantity > 0 && line.ExcessClassification !== 'INTERNAL')
  const blockedExcess = customerFacingExcess.filter((line) => !line.TdDecisionPresent && !line.TdDecision)
  const rejectedExcess = customerFacingExcess.filter(mirLineTdRejected)
  const blocked = blockedExcess.length > 0 || rejectedExcess.length > 0

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!me || me.OrganizationId !== getCompany()) { setError('Wait for the selected company session before saving.'); return }
    if (barcodePendingRef.current) { setError('Wait for the queued scans to finish before saving.'); return }
    setError(null)
    if (!recipientId) { setError('Pick the engineer who takes custody.'); return }
    if (recipientId === me?.EmployeeId) { setError('Stores cannot issue to itself; pick the engineer who receives the material.'); return }
    if (scans.length === 0) { setError('Scan at least one item.'); return }
    for (const scan of scans) {
      const qty = Number(scan.quantity)
      if (!(qty > 0)) { setError(`${scan.line.ItemCode}: quantity must be greater than zero.`); return }
      const balance = issueBaseBalance(scan.line)
      if (balance !== null && qty > balance) {
        setError(`${scan.line.ItemCode}: ${qty} exceeds the balance ${balance} base units still open on the MIR line (${scan.line.RequestedQuantity} requested, the rest issued earlier).`)
        return
      }
      if (qty > scan.line.RequestedBaseQuantity) { setError(`${scan.line.ItemCode}: ${qty} exceeds the MIR line quantity ${scan.line.RequestedBaseQuantity} base units.`); return }
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
          InventorySerialId: scan.inventorySerialId,
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
            <BarcodeScanField expectedCompany={me?.OrganizationId} companyContext={getCompany} onPendingChange={scanPendingChanged} target={barcodeSessionKey(me) + '|' + mir.Id + '|' + mir.Version} disabled={saving || !canIssue || blocked}
              lookup={async (text, context) => {
                const candidates = await getIssueScanCandidates(mir.Id, context)
                return { result: resolveScan(candidates, text, me?.OrganizationId), candidates }
              }}
              focusQuantity={({ result }) => result.kind === 'match' ? '[data-mir-quantity="' + result.candidate.lineKey + '"]' : ''}
              onResolved={({ result, candidates }) => {
                setSerialChoices(candidates)
                if (result.kind !== 'match') { setWarning(result.message); return }
                const line = mir.Lines.find(row => row.Id === result.candidate.lineKey)
                if (!line) { setWarning('MIR line changed. Reload before scanning.'); return }
                setSelectedLineId(line.Id)
                if (result.candidate.requiresSerial) { setWarning('Serialized item selected. Scan or select its exact available physical serial; no quantity was added.'); return }
                addLine(line, result.candidate.storedSerialNumber ?? line.ItemCode, result.candidate.inventorySerialId)
              }} />
            {warning && <span className="scan-warning" role="alert">{warning}</span>}          </div>

          <ErrorAlert error={lookupError} className="field-wide" fallback="Employee lookup failed." />

          <div className="field-wide table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Item</th>
                  <th className="text-right">Requested</th>
                  <th className="text-right" title={hasBalance ? 'Still open on the line after earlier issues' : 'This API does not report earlier issues; the server enforces the balance.'}>Balance</th>
                  <th className="text-right">Issue qty (base units)</th>
                  <th>Scan</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {mir.Lines.map((line) => {
                  const scan = scans.find((candidate) => candidate.line.Id === line.Id)
                  const balance = issueBaseBalance(line)
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
                          : exhausted ? <span title="Fully issued">0 base units</span> : `${balance} base units`}
                      </td>
                      <td className="text-right">
                        {scan ? (
                          <input className="input text-right mono" inputMode="decimal" data-barcode-quantity data-mir-quantity={line.Id} readOnly={scan.inventorySerialId !== null} value={scan.inventorySerialId ? String(scans.filter(row => row.line.Id === line.Id).length) : scan.quantity} onChange={(event) => updateScans((current) => current.map((row) => row.line.Id === line.Id ? { ...row, quantity: event.target.value } : row))} />
                        ) : selectedLineId === line.Id ? <input className="input text-right mono" readOnly data-barcode-quantity data-mir-quantity={line.Id} value="" placeholder="Scan serial" /> : '—'}
                      </td>
                      <td className="mono">{scan ? scans.filter(row => row.line.Id === line.Id).map(row => row.scanCode).join(', ') : <button type="button" className="btn btn-ghost" disabled={exhausted} title={exhausted ? 'Fully issued already; nothing is left on this line.' : undefined} onClick={() => { setSelectedLineId(line.Id); if (serialChoices.some(row => row.lineKey === line.Id && row.requiresSerial)) setWarning('Scan or select an available physical serial for this item.'); else addLine(line, line.ItemCode) }}>Add without scan</button>}</td>
                      <td>{scan && <button type="button" className="btn btn-ghost" onClick={() => updateScans((current) => current.filter((row) => row.line.Id !== line.Id))}>✕</button>}</td>
                    </tr>
                  )
                })}
                {selectedLineId && serialChoices.some(row => row.lineKey === selectedLineId && row.inventorySerialId) && <tr>
                  <td colSpan={7}><label>Available physical serial <select className="input" value="" disabled={saving || barcodePending}
                    onChange={(event) => {
                      const candidate = serialChoices.find(row => row.lineKey === selectedLineId && row.inventorySerialId === event.target.value)
                      const line = mir.Lines.find(row => row.Id === selectedLineId)
                      if (candidate && line && candidate.inventorySerialId && candidate.storedSerialNumber) addLine(line, candidate.storedSerialNumber, candidate.inventorySerialId)
                    }}><option value="">Select the serial on the physical item…</option>
                    {serialChoices.filter(row => row.lineKey === selectedLineId && row.inventorySerialId).map(row => <option key={row.inventorySerialId} value={row.inventorySerialId!}>{row.storedSerialNumber}</option>)}
                  </select></label></td>
                </tr>}
              </tbody>
            </table>
          </div>

          <ErrorAlert error={error} className="field-wide" fallback="Could not issue the material." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={saving || barcodePending || !canIssue || blocked}
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
