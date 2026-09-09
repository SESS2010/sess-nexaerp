import { useMemo, useState } from 'react'
import { createMaterialReturn } from '../../api/materialIssues'
import { newIdempotencyKey } from '../../api/stores'
import type { MaterialIssueLineView, MaterialIssueView, MaterialReturnView } from '../../types/materialIssue'
import { ErrorAlert } from '../../components/ErrorAlert'
import { ScannerInput } from '../../components/ScannerInput'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

/** An issue line with the item code resolved from the MIR line it was issued against. */
export interface IssuedLine {
  line: MaterialIssueLineView
  itemCode: string
  itemName: string
  /** Base quantity still with the engineer after earlier accepted returns. */
  outstanding: number
}

interface Props {
  issue: MaterialIssueView
  lines: IssuedLine[]
  onClose: () => void
  onSaved: (result: MaterialReturnView) => void
}

interface Declaration {
  scanCode: string
  returned: string
  consumed: string
  stillHeld: string
}

function normalizeScan(value: string): string {
  return value.replace(/[^A-Za-z0-9]/g, '').toUpperCase()
}

function nowLocal(): string {
  const date = new Date()
  date.setSeconds(0, 0)
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
}

/**
 * POST /api/v1/stores/material-returns/from-issue/{issueId}. Only the named
 * custodian may declare, and every issue line declared must account for its
 * whole outstanding quantity: returned + reportedly consumed + still held.
 * Stores accepts the return afterwards; consumption is never recorded here,
 * only at fitment.
 */
export function MaterialReturnFormModal({ issue, lines, onClose, onSaved }: Props) {
  const { can } = useSession()
  const [declaredAt, setDeclaredAt] = useState(nowLocal())
  const [declarations, setDeclarations] = useState<Record<string, Declaration>>({})
  const [scanMode, setScanMode] = useState(true)
  const [warning, setWarning] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  const open = useMemo(() => lines.filter((row) => row.outstanding > 0), [lines])

  const declare = (row: IssuedLine, scanCode: string) =>
    setDeclarations((current) => current[row.line.Id]
      ? current
      : { ...current, [row.line.Id]: { scanCode, returned: String(row.outstanding), consumed: '0', stillHeld: '0' } })

  const onScan = (raw: string) => {
    const code = normalizeScan(raw)
    const row = open.find((candidate) => normalizeScan(candidate.itemCode) === code && !declarations[candidate.line.Id])
    if (!row) {
      setWarning(`“${raw}” matches no undeclared line on ${issue.IssueNumber}.`)
      return
    }
    setWarning('')
    declare(row, raw)
  }

  const patch = (lineId: string, change: Partial<Declaration>) =>
    setDeclarations((current) => ({ ...current, [lineId]: { ...current[lineId], ...change } }))

  const canCreate = can(PAGE_KEYS.materialReturns, 'create')

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    const entries = Object.entries(declarations)
    if (entries.length === 0) { setError('Scan at least one issued item to declare it.'); return }
    const payload = []
    for (const [lineId, declaration] of entries) {
      const row = open.find((candidate) => candidate.line.Id === lineId)!
      const returned = Number(declaration.returned)
      const consumed = Number(declaration.consumed || 0)
      const stillHeld = Number(declaration.stillHeld || 0)
      if (!(returned > 0)) { setError(`${row.itemCode}: returned quantity must be greater than zero.`); return }
      if (consumed < 0 || stillHeld < 0) { setError(`${row.itemCode}: consumed and still-held cannot be negative.`); return }
      if (returned + consumed + stillHeld !== row.outstanding) {
        setError(`${row.itemCode}: returned ${returned} + consumed ${consumed} + still held ${stillHeld} must equal the outstanding ${row.outstanding}.`)
        return
      }
      payload.push({
        MaterialIssueLineId: lineId,
        ScanCode: declaration.scanCode,
        ReturnedQuantity: returned,
        ReportedConsumedQuantity: consumed,
        ReportedStillHeldQuantity: stillHeld,
      })
    }
    setSaving(true)
    try {
      onSaved(await createMaterialReturn(issue.Id, {
        DeclaredAt: new Date(declaredAt).toISOString(),
        Lines: payload,
        IdempotencyKey: newIdempotencyKey('material-return'),
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
          <h2>Declare return of {issue.IssueNumber}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>

        <form
          onSubmit={submit}
          className="form-grid"
          onKeyDown={(event) => {
            const target = event.target as HTMLElement
            if (event.key === 'Enter' && !target.closest('button[type="submit"]')) event.preventDefault()
          }}
        >
          <label className="field">
            <span className="field-label">Declared at *</span>
            <input className="input" type="datetime-local" value={declaredAt} onChange={(event) => setDeclaredAt(event.target.value)} />
          </label>
          <div className="field">
            <span className="field-label">Return due</span>
            <div style={{ paddingTop: 6 }}>{new Date(issue.ReturnDueAt).toLocaleString()}</div>
          </div>

          <div className="field-wide">
            <ScannerInput
              label="Scan returned item"
              placeholder="Item code barcode…"
              scanMode={scanMode}
              onScan={onScan}
              warning={warning}
              hint={`${Object.keys(declarations).length} of ${open.length} outstanding lines declared. Each line must account for its whole outstanding quantity.`}
            />
            <label className="field-hint"><input type="checkbox" checked={!scanMode} onChange={(event) => setScanMode(!event.target.checked)} /> Keyboard mode</label>
          </div>

          <div className="field-wide table-wrap">
            <table className="table">
              <thead>
                <tr><th>#</th><th>Item</th><th className="text-right">Outstanding</th><th className="text-right">Returned</th><th className="text-right">Consumed</th><th className="text-right">Still held</th><th /></tr>
              </thead>
              <tbody>
                {open.length === 0 && <tr><td colSpan={7} className="table-empty">Nothing is outstanding on this issue.</td></tr>}
                {open.map((row) => {
                  const declaration = declarations[row.line.Id]
                  return (
                    <tr key={row.line.Id}>
                      <td className="mono">{row.line.LineNumber}</td>
                      <td><span className="mono">{row.itemCode}</span>{row.itemName ? ` — ${row.itemName}` : ''}{row.line.InventorySerialId ? <span className="field-hint">serialized: exact serial scan, full return of 1</span> : null}</td>
                      <td className="text-right mono">{row.outstanding}</td>
                      {declaration ? (
                        <>
                          <td className="text-right"><input className="input text-right mono" inputMode="decimal" value={declaration.returned} onChange={(event) => patch(row.line.Id, { returned: event.target.value })} /></td>
                          <td className="text-right"><input className="input text-right mono" inputMode="decimal" value={declaration.consumed} onChange={(event) => patch(row.line.Id, { consumed: event.target.value })} /></td>
                          <td className="text-right"><input className="input text-right mono" inputMode="decimal" value={declaration.stillHeld} onChange={(event) => patch(row.line.Id, { stillHeld: event.target.value })} /></td>
                          <td><button type="button" className="btn btn-ghost" onClick={() => setDeclarations((current) => { const next = { ...current }; delete next[row.line.Id]; return next })}>✕</button></td>
                        </>
                      ) : (
                        <>
                          <td colSpan={3} className="field-hint">Scan to declare</td>
                          <td><button type="button" className="btn btn-ghost" onClick={() => declare(row, row.itemCode)}>Declare without scan</button></td>
                        </>
                      )}
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <ErrorAlert error={error} className="field-wide" fallback="Could not declare the return." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving || !canCreate} title={canCreate ? undefined : 'Your role cannot declare returns.'}>
              {saving ? 'Declaring…' : 'Declare return'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
