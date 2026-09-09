import { useEffect, useMemo, useState } from 'react'
import { listItems, listUoms } from '../../api/items'
import { listJobOrders } from '../../api/production'
import type { ItemSummary, ReferenceLookup } from '../../types/item'
import type { BomLineInput } from '../../types/bom'
import type { JobOrderSummary } from '../../types/production'
import { ErrorAlert } from '../../components/ErrorAlert'

export interface EditorLine {
  key: string
  itemId: string
  itemCode: string
  itemName: string
  uomId: string
  uomCode: string
  quantity: string
  remarks: string
}

export function editorLine(seed?: Partial<EditorLine>): EditorLine {
  return { key: crypto.randomUUID(), itemId: '', itemCode: '', itemName: '', uomId: '', uomCode: '', quantity: '', remarks: '', ...seed }
}

interface Props {
  title: string
  /** Shown when the job order must be chosen (Estimated BOM create). */
  pickJobOrder?: boolean
  initialLines: EditorLine[]
  reasonLabel: string
  submitLabel: string
  canSave: boolean
  onClose: () => void
  onSave: (payload: { jobOrderId: string; reason: string; lines: BomLineInput[] }) => Promise<void>
}

/**
 * Shared BOM line editor for an Estimated BOM (create / edit draft) and a
 * Production BOM draft. Both services accept the same line shape (ItemId,
 * UomId, Quantity, Remarks), require every item to be active and the UOM to
 * be the item's base UOM or one with an approved conversion.
 */
export function BomLinesEditorModal({ title, pickJobOrder, initialLines, reasonLabel, submitLabel, canSave, onClose, onSave }: Props) {
  const [jobs, setJobs] = useState<JobOrderSummary[]>([])
  const [jobOrderId, setJobOrderId] = useState('')
  const [reason, setReason] = useState('')
  const [lines, setLines] = useState<EditorLine[]>(initialLines.length > 0 ? initialLines : [editorLine()])
  const [uoms, setUoms] = useState<ReferenceLookup[]>([])
  const [itemSearch, setItemSearch] = useState('')
  const [itemOptions, setItemOptions] = useState<ItemSummary[]>([])
  const [searching, setSearching] = useState(false)
  const [lookupError, setLookupError] = useState<unknown>(null)
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    listUoms().then((page) => setUoms(page.Items ?? [])).catch(setLookupError)
    if (pickJobOrder) {
      listJobOrders({ page: 1, pageSize: 100, status: 'OPEN' }).then((page) => setJobs(page.Items ?? [])).catch(setLookupError)
    }
  }, [pickJobOrder])

  useEffect(() => {
    const term = itemSearch.trim()
    if (term.length < 2) {
      setItemOptions([])
      return
    }
    const handle = window.setTimeout(() => {
      setSearching(true)
      listItems({ page: 1, pageSize: 15, search: term })
        .then((page) => setItemOptions(page.Items ?? []))
        .catch(setLookupError)
        .finally(() => setSearching(false))
    }, 250)
    return () => window.clearTimeout(handle)
  }, [itemSearch])

  const uomByCode = useMemo(() => {
    const map = new Map<string, ReferenceLookup>()
    for (const uom of uoms) map.set(uom.Code.toUpperCase(), uom)
    return map
  }, [uoms])

  const setLine = (key: string, patch: Partial<EditorLine>) =>
    setLines((current) => current.map((line) => (line.key === key ? { ...line, ...patch } : line)))

  const pickItem = (key: string, item: ItemSummary) => {
    const uom = uomByCode.get((item.Uom ?? '').toUpperCase())
    setLine(key, { itemId: item.Id, itemCode: item.ItemCode, itemName: item.Name, uomId: uom?.Id ?? '', uomCode: item.Uom ?? '' })
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    if (pickJobOrder && !jobOrderId) { setError('Pick the OPEN job order this BOM belongs to.'); return }
    if (!reason.trim()) { setError(`${reasonLabel} is required — it is recorded on the revision.`); return }
    const filled = lines.filter((line) => line.itemId || line.quantity)
    if (filled.length === 0) { setError('Add at least one line.'); return }
    const payload: BomLineInput[] = []
    for (const line of filled) {
      const qty = Number(line.quantity)
      if (!line.itemId) { setError('Every line needs an item picked from the item master.'); return }
      if (!line.uomId) { setError(`${line.itemCode}: pick a UOM.`); return }
      if (!(qty > 0)) { setError(`${line.itemCode}: quantity must be greater than zero.`); return }
      payload.push({ ItemId: line.itemId, UomId: line.uomId, Quantity: qty, Remarks: line.remarks.trim() || null })
    }
    setSaving(true)
    try {
      await onSave({ jobOrderId, reason: reason.trim(), lines: payload })
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
          <h2>{title}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={submit} className="form-grid">
          {pickJobOrder && (
            <label className="field">
              <span className="field-label">Job order *</span>
              <select className="input" value={jobOrderId} onChange={(event) => setJobOrderId(event.target.value)}>
                <option value="">Pick an OPEN job order…</option>
                {jobs.map((job) => <option key={job.Id} value={job.Id}>{job.JobOrderNumber} · {job.CustomerName} · {job.MachineModel} #{job.MachineOrdinal}</option>)}
              </select>
              <span className="field-hint">Only Accounts-confirmed (OPEN) jobs are listed; a pending job is refused by the server.</span>
            </label>
          )}

          <label className={pickJobOrder ? 'field' : 'field field-wide'}>
            <span className="field-label">{reasonLabel} *</span>
            <input className="input" value={reason} onChange={(event) => setReason(event.target.value)} />
          </label>

          <div className="field-wide form-section-title">Lines ({lines.filter((line) => line.itemId).length})</div>

          <div className="field field-wide">
            <span className="field-label">Find item</span>
            <input
              className="input search"
              placeholder="Type at least 2 characters of the item code or name, then choose a line's item below…"
              value={itemSearch}
              onChange={(event) => setItemSearch(event.target.value)}
            />
            {searching && <span className="field-hint">Searching…</span>}
          </div>
          <ErrorAlert error={lookupError} className="field-wide" fallback="Item, UOM or job order lookup failed." />

          <div className="field-wide table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th style={{ width: 40 }}>#</th>
                  <th>Item *</th>
                  <th>UOM *</th>
                  <th className="text-right" style={{ width: 120 }}>Quantity *</th>
                  <th>Remarks</th>
                  <th style={{ width: 60 }} />
                </tr>
              </thead>
              <tbody>
                {lines.map((line, index) => (
                  <tr key={line.key}>
                    <td className="mono">{index + 1}</td>
                    <td>
                      {line.itemId ? (
                        <div>
                          <span className="mono">{line.itemCode}</span>{line.itemName ? ` — ${line.itemName}` : ''}
                          <button type="button" className="btn btn-ghost" style={{ marginLeft: 8 }} onClick={() => setLine(line.key, { itemId: '', itemCode: '', itemName: '', uomId: '', uomCode: '' })}>change</button>
                        </div>
                      ) : (
                        <select
                          className="input"
                          value=""
                          disabled={itemOptions.length === 0}
                          onChange={(event) => {
                            const item = itemOptions.find((option) => option.Id === event.target.value)
                            if (item) pickItem(line.key, item)
                          }}
                        >
                          <option value="">{itemOptions.length === 0 ? 'Search above, then pick…' : `Pick from ${itemOptions.length} matches…`}</option>
                          {itemOptions.map((item) => (
                            <option key={item.Id} value={item.Id}>{item.ItemCode} — {item.Name} ({item.Uom})</option>
                          ))}
                        </select>
                      )}
                    </td>
                    <td>
                      <select className="input" value={line.uomId} onChange={(event) => setLine(line.key, { uomId: event.target.value })}>
                        <option value="">—</option>
                        {uoms.map((uom) => <option key={uom.Id} value={uom.Id}>{uom.Code}</option>)}
                      </select>
                      {line.itemId && line.uomCode && uomByCode.get(line.uomCode.toUpperCase())?.Id !== line.uomId && (
                        <span className="field-hint">Item UOM is {line.uomCode}. Another UOM needs an approved conversion or the save is refused.</span>
                      )}
                    </td>
                    <td className="text-right">
                      <input className="input text-right mono" inputMode="decimal" value={line.quantity} onChange={(event) => setLine(line.key, { quantity: event.target.value })} />
                    </td>
                    <td>
                      <input className="input" value={line.remarks} onChange={(event) => setLine(line.key, { remarks: event.target.value })} />
                    </td>
                    <td>
                      <button type="button" className="btn btn-ghost" disabled={lines.length === 1} onClick={() => setLines((current) => current.filter((row) => row.key !== line.key))}>✕</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="field-wide">
            <button type="button" className="btn btn-ghost" onClick={() => setLines((current) => [...current, editorLine()])}>+ Add line</button>
          </div>

          <ErrorAlert error={error} className="field-wide" fallback="Could not save the BOM." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving || !canSave} title={canSave ? undefined : 'Your role cannot save this BOM.'}>
              {saving ? 'Saving…' : submitLabel}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
