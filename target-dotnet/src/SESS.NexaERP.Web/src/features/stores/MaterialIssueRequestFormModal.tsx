import { useEffect, useMemo, useState } from 'react'
import { listItems, listUoms } from '../../api/items'
import { createMaterialIssueRequest, updateMaterialIssueRequest } from '../../api/materialIssues'
import { newIdempotencyKey } from '../../api/stores'
import type { ItemSummary, ReferenceLookup } from '../../types/item'
import type { MaterialIssueRequestLineInput, MaterialIssueRequestView } from '../../types/materialIssue'
import { MIR_JOB_SITUATIONS, MIR_PURPOSES, MIR_SITUATIONS } from '../../types/materialIssue'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

interface DraftLine {
  key: string
  itemId: string
  itemCode: string
  itemName: string
  uomId: string
  /** The item master's UOM code; another UOM needs an approved conversion. */
  itemUom: string
  quantity: string
  remarks: string
}

interface Props {
  mode: 'create' | 'edit'
  existing?: MaterialIssueRequestView
  onClose: () => void
  onSaved: (result: MaterialIssueRequestView) => void
}

function todayPlus(days: number): string {
  const date = new Date()
  date.setDate(date.getDate() + days)
  return date.toISOString().slice(0, 10)
}

function emptyLine(): DraftLine {
  return { key: crypto.randomUUID(), itemId: '', itemCode: '', itemName: '', uomId: '', itemUom: '', quantity: '', remarks: '' }
}

/**
 * Material Issue Request (MIR). POST/PUT /stores/material-issue-requests.
 *
 * Only CONSUMABLE_OFFICE can be raised from this screen today: the three
 * customer-facing situations need a JobOrderId, and the Job Order API is not on
 * main yet. They are listed but disabled so the vocabulary is visible, and the
 * picker will be enabled the moment that endpoint ships.
 *
 * The requesting department is the signed-in employee's own department (the
 * session carries its id); the API has no department-id lookup for MIR.
 */
export function MaterialIssueRequestFormModal({ mode, existing, onClose, onSaved }: Props) {
  const { me, can } = useSession()
  const [purpose, setPurpose] = useState(existing?.Purpose ?? 'FACTORY_ASSEMBLY')
  const [situation, setSituation] = useState(existing?.Situation ?? 'CONSUMABLE_OFFICE')
  const [destinationType, setDestinationType] = useState(existing?.DestinationType ?? 'DEPARTMENT')
  const [destinationName, setDestinationName] = useState(existing?.DestinationName ?? '')
  const [requiredDate, setRequiredDate] = useState(existing?.RequiredDate ?? todayPlus(1))
  const [lines, setLines] = useState<DraftLine[]>(() =>
    existing
      ? existing.Lines.map((line) => ({
          key: line.Id,
          itemId: line.ItemId,
          itemCode: line.ItemCode,
          itemName: line.ItemName,
          uomId: line.UomId,
          itemUom: line.UomCode,
          quantity: String(line.RequestedQuantity),
          remarks: line.Remarks ?? '',
        }))
      : [emptyLine()],
  )
  const [uoms, setUoms] = useState<ReferenceLookup[]>([])
  const [itemSearch, setItemSearch] = useState('')
  const [itemOptions, setItemOptions] = useState<ItemSummary[]>([])
  const [searching, setSearching] = useState(false)
  const [lookupError, setLookupError] = useState<unknown>(null)
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    listUoms().then((page) => setUoms(page.Items ?? [])).catch(setLookupError)
  }, [])

  // Item master search, debounced. The list endpoint filters on code and name.
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

  useEffect(() => {
    if (destinationType === 'DEPARTMENT' && !destinationName && me?.DepartmentCode) {
      setDestinationName(me.DepartmentCode)
    }
  }, [destinationType, destinationName, me])

  const setLine = (key: string, patch: Partial<DraftLine>) =>
    setLines((current) => current.map((line) => (line.key === key ? { ...line, ...patch } : line)))

  const pickItem = (key: string, item: ItemSummary) => {
    const uom = uomByCode.get((item.Uom ?? '').toUpperCase())
    setLine(key, { itemId: item.Id, itemCode: item.ItemCode, itemName: item.Name, uomId: uom?.Id ?? '', itemUom: item.Uom ?? '' })
  }

  const jobSituation = MIR_JOB_SITUATIONS.includes(situation as (typeof MIR_JOB_SITUATIONS)[number])
  const canSave = can(PAGE_KEYS.materialIssueRequests, mode === 'create' ? 'create' : 'update')

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    if (!me?.DepartmentId) {
      setError('Your session has no department, so the requesting department cannot be set.')
      return
    }
    if (jobSituation) {
      setError('Customer-facing situations need a Job Order. The Job Order API is not available yet; raise a CONSUMABLE_OFFICE request or wait for that screen.')
      return
    }
    const complete = lines.filter((line) => line.itemId || line.quantity)
    if (complete.length === 0) {
      setError('Add at least one line — pick an item from the search and enter a quantity.')
      return
    }
    const payloadLines: MaterialIssueRequestLineInput[] = []
    for (const line of complete) {
      const quantity = Number(line.quantity)
      if (!line.itemId) { setError('Every line needs an item picked from the item master.'); return }
      if (!line.uomId) { setError(`${line.itemCode}: pick a UOM — the item's UOM code has no matching active UOM.`); return }
      if (!(quantity > 0)) { setError(`${line.itemCode}: quantity must be greater than zero.`); return }
      payloadLines.push({
        ItemId: line.itemId,
        UomId: line.uomId,
        Quantity: quantity,
        CustomerPurchaseOrderLineId: null,
        Remarks: line.remarks.trim() || null,
      })
    }
    const body = {
      Purpose: purpose,
      Situation: situation,
      DestinationType: destinationType,
      JobOrderId: null,
      CustomerId: null,
      VendorId: null,
      DestinationDepartmentId: destinationType === 'DEPARTMENT' ? me.DepartmentId : null,
      DestinationName: destinationName.trim() || (destinationType === 'DEPARTMENT' ? me.DepartmentCode : ''),
      RequestingDepartmentId: me.DepartmentId,
      RequiredDate: requiredDate,
      Lines: payloadLines,
      IdempotencyKey: newIdempotencyKey(mode === 'create' ? 'mir-create' : 'mir-update'),
    }
    if (!body.DestinationName) {
      setError('Destination name is required.')
      return
    }
    setSaving(true)
    try {
      const result = mode === 'create'
        ? await createMaterialIssueRequest(body)
        : await updateMaterialIssueRequest(existing!.Id, { ...body, Version: existing!.Version })
      onSaved(result)
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
          <h2>{mode === 'create' ? 'New Material Issue Request' : `Edit ${existing?.RequestNumber}`}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={submit} className="form-grid">
          <div className="field-wide form-section-title">Request</div>

          <label className="field">
            <span className="field-label">Situation *</span>
            <select className="input" value={situation} onChange={(event) => {
              const next = event.target.value
              setSituation(next)
              if (MIR_JOB_SITUATIONS.includes(next as (typeof MIR_JOB_SITUATIONS)[number])) setDestinationType('JOB_ORDER')
              else if (destinationType === 'JOB_ORDER') setDestinationType('DEPARTMENT')
            }}>
              {MIR_SITUATIONS.map((option) => (
                <option key={option} value={option} disabled={MIR_JOB_SITUATIONS.includes(option)}>
                  {option.replaceAll('_', ' ')}{MIR_JOB_SITUATIONS.includes(option) ? ' (needs Job Order — not yet available)' : ''}
                </option>
              ))}
            </select>
            <span className="field-hint">Chamber, service-PO and site-project requests are tied to a Job Order; consumables and office material are not.</span>
          </label>

          <label className="field">
            <span className="field-label">Purpose *</span>
            <select className="input" value={purpose} onChange={(event) => setPurpose(event.target.value)}>
              {MIR_PURPOSES.map((option) => <option key={option} value={option}>{option.replaceAll('_', ' ')}</option>)}
            </select>
          </label>

          <label className="field">
            <span className="field-label">Destination *</span>
            <select className="input" value={destinationType} disabled={jobSituation} onChange={(event) => setDestinationType(event.target.value)}>
              <option value="DEPARTMENT">DEPARTMENT — my department ({me?.DepartmentCode ?? '…'})</option>
              <option value="OTHER">OTHER — named below</option>
              {jobSituation && <option value="JOB_ORDER">JOB ORDER</option>}
            </select>
          </label>

          <label className="field">
            <span className="field-label">Destination name *</span>
            <input
              className="input"
              value={destinationName}
              placeholder={destinationType === 'OTHER' ? 'e.g. Site office, Demo bay' : ''}
              onChange={(event) => setDestinationName(event.target.value)}
            />
          </label>

          <label className="field">
            <span className="field-label">Required date *</span>
            <input className="input" type="date" value={requiredDate} onChange={(event) => setRequiredDate(event.target.value)} />
          </label>

          <div className="field">
            <span className="field-label">Requesting department</span>
            <div className="mono" style={{ paddingTop: 6 }}>{me?.DepartmentCode ?? '—'}</div>
            <span className="field-hint">Taken from your session; the requester cannot raise on behalf of another department.</span>
          </div>

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
          <ErrorAlert error={lookupError} className="field-wide" fallback="Item or UOM lookup failed." />

          <div className="field-wide table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th style={{ width: 40 }}>#</th>
                  <th>Item *</th>
                  <th>UOM * <span className="field-hint" style={{ display: 'inline' }}>(item's own)</span></th>
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
                          <span className="mono">{line.itemCode}</span> — {line.itemName}
                          <button type="button" className="btn btn-ghost" style={{ marginLeft: 8 }} onClick={() => setLine(line.key, { itemId: '', itemCode: '', itemName: '', uomId: '', itemUom: '' })}>change</button>
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
                      {line.itemId && line.itemUom && uomByCode.get(line.itemUom.toUpperCase())?.Id !== line.uomId && (
                        <span className="field-hint">Item UOM is {line.itemUom}. Another UOM needs an approved conversion or the save is refused.</span>
                      )}
                      {line.itemId && line.itemUom && !uomByCode.has(line.itemUom.toUpperCase()) && (
                        <span className="field-hint">Item UOM “{line.itemUom}” is not an active UOM in the master.</span>
                      )}
                    </td>
                    <td className="text-right">
                      <input
                        className="input text-right mono"
                        inputMode="decimal"
                        value={line.quantity}
                        onChange={(event) => setLine(line.key, { quantity: event.target.value })}
                      />
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
            <button type="button" className="btn btn-ghost" onClick={() => setLines((current) => [...current, emptyLine()])}>+ Add line</button>
          </div>

          <ErrorAlert error={error} className="field-wide" fallback="Could not save the request." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving || !canSave} title={canSave ? undefined : 'Your role cannot create material issue requests.'}>
              {saving ? 'Saving…' : mode === 'create' ? 'Create draft' : 'Save changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
