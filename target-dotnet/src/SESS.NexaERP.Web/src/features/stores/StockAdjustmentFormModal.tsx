import { useEffect, useMemo, useRef, useState } from 'react'
import { listItems } from '../../api/items'
import { listAvailableConditionLocations } from '../../api/qc'
import { lookupMaterialIssueRecipients } from '../../api/materialIssues'
import type { MaterialIssueRecipientLookup } from '../../api/materialIssues'
import { newIdempotencyKey } from '../../api/stores'
import {
  STOCK_ADJUSTMENT_PAGE_KEY,
  createStockAdjustment,
  daysBetween,
  formatDateOnly,
  formatRupees,
  listStockAdjustmentPeriods,
  reviseStockAdjustment,
  serverTodayUtc,
  stockAdjustmentRefusedDefinitively,
} from '../../api/stockAdjustments'
import type { ItemSummary } from '../../types/item'
import type { WarehouseConditionLocation } from '../../types/qc'
import type {
  StockAdjustmentLineInput,
  StockAdjustmentPeriodView,
  StockAdjustmentReasonKind,
  StockAdjustmentView,
} from '../../types/stockAdjustment'
import { STOCK_ADJUSTMENT_REASON_KINDS, STOCK_ADJUSTMENT_REASON_WORDS } from '../../types/stockAdjustment'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

type Direction = 'ADD' | 'REMOVE'

interface DraftLine {
  key: string
  itemId: string
  itemCode: string
  itemName: string
  locationId: string
  direction: Direction
  /** Magnitude as typed; the sign comes from direction. */
  quantity: string
  unitValue: string
  lotNumber: string
  serialNumber: string
  remarks: string
}

interface Props {
  /** create: a new DRAFT (optionally a reversal of `reverseOf`); edit: a new revision of `existing`. */
  mode: 'create' | 'edit'
  existing?: StockAdjustmentView
  /** A POSTED adjustment to reverse: lines are mirrored with the opposite sign, in order. */
  reverseOf?: StockAdjustmentView
  onClose: () => void
  onSaved: (result: StockAdjustmentView) => void
}

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
const SIX_DECIMALS = /^\d+(\.\d{1,6})?$/

function emptyLine(): DraftLine {
  return {
    key: crypto.randomUUID(), itemId: '', itemCode: '', itemName: '', locationId: '', direction: 'REMOVE',
    quantity: '', unitValue: '', lotNumber: '', serialNumber: '', remarks: '',
  }
}

function fromView(view: StockAdjustmentView, mirror: boolean): DraftLine[] {
  return view.Lines.map((line) => {
    const change = mirror ? -line.QuantityChange : line.QuantityChange
    const magnitude = Math.abs(change)
    // A mirrored removal becomes an addition, which must state its unit value;
    // the original's accepted value per unit is offered and stays editable.
    const unitValue = change > 0
      ? line.UnitValue ?? (magnitude > 0 ? Math.round((line.AcceptedLineValue / magnitude) * 1e6) / 1e6 : 0)
      : null
    return {
      key: mirror ? crypto.randomUUID() : line.Id,
      itemId: line.ItemId,
      itemCode: line.ItemCode,
      itemName: line.ItemName,
      locationId: line.WarehouseConditionLocationId,
      direction: change > 0 ? 'ADD' : 'REMOVE',
      quantity: String(magnitude),
      unitValue: unitValue === null ? '' : String(unitValue),
      lotNumber: line.LotNumber ?? '',
      serialNumber: line.SerialNumber ?? '',
      remarks: line.Remarks ?? '',
    }
  })
}

/**
 * A2 stock adjustment draft. POST /stores/stock-adjustments (create) or
 * PUT /stores/stock-adjustments/{id} (update = new revision, back to DRAFT).
 *
 * Warehouse, reason kind, effective date and inventory period are fixed at
 * creation; a revision replaces the lines, remarks and backdate evidence only.
 * Every line sits at an effective AVAILABLE condition location of the chosen
 * warehouse. An addition states its ex-tax unit value; a removal is valued by
 * the server from the FIFO layers it consumes, so it states none. Approval
 * authority is decided by the server at submission, never here.
 */
export function StockAdjustmentFormModal({ mode, existing, reverseOf, onClose, onSaved }: Props) {
  const { can } = useSession()
  const reversal = mode === 'create' && !!reverseOf
  const header = mode === 'edit' ? existing : reverseOf

  const [periods, setPeriods] = useState<StockAdjustmentPeriodView[]>([])
  const [periodId, setPeriodId] = useState(mode === 'edit' ? existing?.InventoryPeriodId ?? '' : '')
  const [effectiveDate, setEffectiveDate] = useState(mode === 'edit' ? existing?.EffectiveDate ?? '' : '')
  const [warehouseId, setWarehouseId] = useState(header?.WarehouseId ?? '')
  const [reasonKind, setReasonKind] = useState<StockAdjustmentReasonKind | ''>(
    mode === 'edit' ? (existing?.ReasonKind as StockAdjustmentReasonKind) : reversal ? 'CORRECTION' : '',
  )
  const [remarks, setRemarks] = useState(
    mode === 'edit' ? existing?.Remarks ?? '' : reversal ? `Reversal of ${reverseOf!.AdjustmentNumber}: ` : '',
  )
  const [backdateReason, setBackdateReason] = useState(mode === 'edit' ? existing?.BackdateReason ?? '' : '')
  const [backdateEvidenceId, setBackdateEvidenceId] = useState(mode === 'edit' ? existing?.BackdateEvidenceId ?? '' : '')
  const [revisionReason, setRevisionReason] = useState('')
  const [counterIds, setCounterIds] = useState<string[]>([])
  const [recipients, setRecipients] = useState<MaterialIssueRecipientLookup[]>([])
  const [lines, setLines] = useState<DraftLine[]>(() =>
    mode === 'edit' && existing ? fromView(existing, false) : reversal ? fromView(reverseOf!, true) : [emptyLine()],
  )

  const [locations, setLocations] = useState<WarehouseConditionLocation[]>([])
  const [locationError, setLocationError] = useState<unknown>(null)
  const [periodError, setPeriodError] = useState<unknown>(null)
  const [itemSearch, setItemSearch] = useState('')
  const [itemOptions, setItemOptions] = useState<ItemSummary[]>([])
  const [searching, setSearching] = useState(false)
  const [lookupError, setLookupError] = useState<unknown>(null)
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  // One key per submission, kept across retries whose outcome is unknown so a
  // replay returns the original record; renewed after a definitive refusal.
  const keyPrefix = mode === 'create' ? 'stock-adjustment-create' : 'stock-adjustment-revise'
  const keyRef = useRef(newIdempotencyKey(keyPrefix))

  useEffect(() => {
    listAvailableConditionLocations().then(setLocations).catch(setLocationError)
  }, [])

  useEffect(() => {
    if (mode !== 'create') return
    listStockAdjustmentPeriods()
      .then((rows) => {
        setPeriods(rows)
        if (rows.length === 1) setPeriodId(rows[0].Id)
      })
      .catch(setPeriodError)
  }, [mode])

  // Counters (who physically counted) are excluded from approval by the
  // server's snapshot. The only employee lookup Stores can read is the
  // material-issue recipient list, so it is offered only with that grant.
  const canPickCounters = mode === 'create' && can(PAGE_KEYS.materialIssues, 'issue')
  useEffect(() => {
    if (!canPickCounters) return
    lookupMaterialIssueRecipients().then(setRecipients).catch(() => setRecipients([]))
  }, [canPickCounters])

  // Item master search, debounced; hidden for a reversal, whose lines are fixed.
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

  const warehouses = useMemo(() => {
    const map = new Map<string, string>()
    for (const location of locations) map.set(location.WarehouseId, location.WarehouseCode)
    return [...map.entries()].map(([id, code]) => ({ id, code })).sort((a, b) => a.code.localeCompare(b.code))
  }, [locations])
  const warehouseLocations = useMemo(
    () => locations.filter((location) => location.WarehouseId === warehouseId),
    [locations, warehouseId],
  )
  const locationById = useMemo(() => new Map(locations.map((location) => [location.Id, location])), [locations])

  const period = periods.find((candidate) => candidate.Id === periodId)
  const today = serverTodayUtc()
  const maxDate = period ? (period.EndDate < today ? period.EndDate : today) : today
  const daysBack = effectiveDate ? daysBetween(effectiveDate, today) : 0
  const backdated = daysBack > 0

  const setLine = (key: string, patch: Partial<DraftLine>) =>
    setLines((current) => current.map((line) => (line.key === key ? { ...line, ...patch } : line)))

  const canSave = can(STOCK_ADJUSTMENT_PAGE_KEY, mode === 'create' ? 'create' : 'update')
  const lockedLines = reversal

  const addedValue = lines.reduce((sum, line) => {
    const quantity = Number(line.quantity)
    const unit = Number(line.unitValue)
    return line.direction === 'ADD' && quantity > 0 && unit >= 0 ? sum + quantity * unit : sum
  }, 0)

  const buildLines = (): StockAdjustmentLineInput[] | string => {
    const used = lines.filter((line) => line.itemId || line.quantity)
    if (used.length === 0) return 'Add at least one line: pick an item, a location and a quantity.'
    if (used.length > 500) return 'An adjustment carries at most 500 lines.'
    const serials = new Set<string>()
    const result: StockAdjustmentLineInput[] = []
    for (const [index, line] of used.entries()) {
      const label = `Line ${index + 1}${line.itemCode ? ` (${line.itemCode})` : ''}`
      if (!line.itemId) return `${label}: pick an item from the item master.`
      if (!line.locationId) return `${label}: pick the rack/bin (AVAILABLE location) in the warehouse.`
      const location = locationById.get(line.locationId)
      if (location && location.WarehouseId !== warehouseId) return `${label}: the location is not in the adjustment's warehouse.`
      if (!SIX_DECIMALS.test(line.quantity.trim()) || !(Number(line.quantity) > 0))
        return `${label}: enter a quantity greater than zero, at most six decimals.`
      const magnitude = Number(line.quantity)
      if (line.direction === 'ADD' && reasonKind === 'DAMAGE_LOSS')
        return `${label}: a damage/loss write-off removes stock only.`
      let unitValue: number | null = null
      if (line.direction === 'ADD') {
        if (!SIX_DECIMALS.test(line.unitValue.trim()))
          return `${label}: an addition needs its ex-tax unit value (zero or more, at most six decimals).`
        unitValue = Number(line.unitValue)
      }
      const serial = line.serialNumber.trim()
      if (serial) {
        if (magnitude !== 1) return `${label}: a serialized line changes exactly one unit.`
        const upper = serial.toUpperCase()
        if (serials.has(upper)) return `${label}: serial ${serial} is already on another line.`
        serials.add(upper)
      }
      result.push({
        ItemId: line.itemId,
        WarehouseConditionLocationId: line.locationId,
        QuantityChange: line.direction === 'ADD' ? magnitude : -magnitude,
        UnitValue: unitValue,
        LotNumber: line.lotNumber.trim() || null,
        SerialNumber: serial || null,
        Remarks: line.remarks.trim() || null,
      })
    }
    return result
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    if (!remarks.trim()) { setError('Remarks are required: say what was found and why stock is being adjusted.'); return }
    if (mode === 'create') {
      if (!warehouseId) { setError('Pick the warehouse.'); return }
      if (!reasonKind) { setError('Pick the reason kind.'); return }
      if (!periodId) { setError('Pick the open inventory period.'); return }
      if (!effectiveDate) { setError('Enter the effective date.'); return }
      if (period && (effectiveDate < period.StartDate || effectiveDate > period.EndDate)) {
        setError(`The effective date must be inside ${period.Code} (${formatDateOnly(period.StartDate)} – ${formatDateOnly(period.EndDate)}).`)
        return
      }
      if (effectiveDate > today) { setError('A stock adjustment cannot be dated in the future.'); return }
    } else if (!revisionReason.trim()) {
      setError('A reason for this revision is required; it is kept in the audit record.')
      return
    }
    if (backdated) {
      if (!backdateReason.trim()) { setError(`The effective date is ${daysBack} day(s) back: give the backdate reason.`); return }
      if (!GUID.test(backdateEvidenceId.trim())) { setError('A backdated adjustment needs the id of its retained evidence record.'); return }
    }
    const built = buildLines()
    if (typeof built === 'string') { setError(built); return }

    setSaving(true)
    try {
      const result = mode === 'create'
        ? await createStockAdjustment({
            WarehouseId: warehouseId,
            ReasonKind: reasonKind as StockAdjustmentReasonKind,
            EffectiveDate: effectiveDate,
            InventoryPeriodId: periodId,
            Remarks: remarks.trim(),
            Lines: built,
            IdempotencyKey: keyRef.current,
            CounterEmployeeIds: counterIds.length > 0 ? counterIds : null,
            BackdateReason: backdated ? backdateReason.trim() : null,
            BackdateEvidenceId: backdated ? backdateEvidenceId.trim() : null,
            ReversesStockAdjustmentId: reversal ? reverseOf!.Id : null,
          })
        : await reviseStockAdjustment(existing!.Id, {
            Version: existing!.Version,
            Remarks: remarks.trim(),
            Lines: built,
            Reason: revisionReason.trim(),
            IdempotencyKey: keyRef.current,
            BackdateReason: backdated ? backdateReason.trim() : null,
            BackdateEvidenceId: backdated ? backdateEvidenceId.trim() : null,
          })
      onSaved(result)
    } catch (err) {
      if (stockAdjustmentRefusedDefinitively(err)) keyRef.current = newIdempotencyKey(keyPrefix)
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  const title = mode === 'edit'
    ? `Revise ${existing?.AdjustmentNumber}`
    : reversal ? `Reverse ${reverseOf?.AdjustmentNumber}` : 'New stock adjustment'

  return (
    <div className="modal-backdrop">
      <div className="modal modal-wide" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{title}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>✕</button>
        </div>

        <form onSubmit={submit} className="form-grid" noValidate>
          <div className="field-wide form-section-title">Adjustment</div>

          {mode === 'edit' && existing?.Status !== 'DRAFT' && (
            <div className="field-wide alert alert-warn">
              This adjustment is {existing?.Status}. Saving makes a new revision and returns it to DRAFT: approvals already given stay with the old revision, and it must be submitted again.
            </div>
          )}
          {reversal && (
            <div className="field-wide alert">
              The lines mirror {reverseOf?.AdjustmentNumber} with the opposite quantity, in the same order, as the server requires. Stock put back needs its unit value; removals are valued from FIFO. A reversal needs at least the approvers of the original.
            </div>
          )}

          {mode === 'create' ? (
            <>
              <label className="field">
                <span className="field-label">Warehouse *</span>
                <select
                  className="input"
                  value={warehouseId}
                  disabled={reversal}
                  onChange={(event) => {
                    setWarehouseId(event.target.value)
                    setLines((current) => current.map((line) => ({ ...line, locationId: '' })))
                  }}
                >
                  <option value="">{warehouses.length === 0 ? 'No AVAILABLE locations readable…' : 'Pick the warehouse…'}</option>
                  {warehouses.map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{warehouse.code}</option>)}
                  {reversal && !warehouses.some((warehouse) => warehouse.id === warehouseId) && (
                    <option value={warehouseId}>{reverseOf?.WarehouseCode}</option>
                  )}
                </select>
                <span className="field-hint">Warehouses that have an effective AVAILABLE rack/bin.</span>
              </label>

              <label className="field">
                <span className="field-label">Reason kind *</span>
                <select className="input" value={reasonKind} onChange={(event) => setReasonKind(event.target.value as StockAdjustmentReasonKind | '')}>
                  <option value="">Pick the reason…</option>
                  {STOCK_ADJUSTMENT_REASON_KINDS.map((option) => (
                    <option key={option} value={option}>{STOCK_ADJUSTMENT_REASON_WORDS[option]}</option>
                  ))}
                </select>
                {reasonKind === 'DAMAGE_LOSS' && (
                  <span className="field-hint">A write-off needs the Technical Director with Accounts Manager concurrence.</span>
                )}
              </label>

              <label className="field">
                <span className="field-label">Inventory period *</span>
                <select className="input" value={periodId} onChange={(event) => setPeriodId(event.target.value)}>
                  <option value="">{periods.length === 0 ? 'No open inventory period…' : 'Pick the open period…'}</option>
                  {periods.map((candidate) => (
                    <option key={candidate.Id} value={candidate.Id}>
                      {candidate.Code} · {formatDateOnly(candidate.StartDate)} – {formatDateOnly(candidate.EndDate)}
                    </option>
                  ))}
                </select>
                {periods.length === 0 && !periodError && (
                  <span className="field-hint">Adjustments are dated into an OPEN inventory period. Ask the CFO to open one.</span>
                )}
              </label>

              <label className="field">
                <span className="field-label">Effective date *</span>
                <input
                  className="input"
                  type="date"
                  value={effectiveDate}
                  min={period?.StartDate}
                  max={maxDate}
                  onChange={(event) => setEffectiveDate(event.target.value)}
                />
                <span className="field-hint">
                  Inside the period and not in the future (server date {formatDateOnly(today)}).{' '}
                  <button type="button" className="btn btn-ghost" onClick={() => setEffectiveDate(today)}>Today</button>
                </span>
              </label>
            </>
          ) : (
            <div className="field-wide detail-grid">
              <div><span className="field-label">Warehouse</span> <span className="mono">{existing?.WarehouseCode}</span></div>
              <div><span className="field-label">Reason kind</span> {existing?.ReasonKind.replaceAll('_', ' ')}</div>
              <div><span className="field-label">Inventory period</span> <span className="mono">{existing?.InventoryPeriodCode}</span></div>
              <div><span className="field-label">Effective date</span> {formatDateOnly(existing?.EffectiveDate)}</div>
              <p className="field-hint">Warehouse, reason kind, period and effective date are fixed once recorded. To change them, reject this one and record a new adjustment.</p>
            </div>
          )}

          <label className="field field-wide">
            <span className="field-label">Remarks *</span>
            <textarea className="input" rows={2} maxLength={1000} value={remarks} onChange={(event) => setRemarks(event.target.value)} />
            <span className="field-hint">What was found, where, and why the book is wrong. {remarks.trim().length} of 1000.</span>
          </label>

          {mode === 'edit' && (
            <label className="field field-wide">
              <span className="field-label">Reason for this revision *</span>
              <input className="input" maxLength={1000} value={revisionReason} onChange={(event) => setRevisionReason(event.target.value)} />
            </label>
          )}

          {backdated && (
            <>
              <div className="field-wide alert alert-warn">
                The effective date is {daysBack} day{daysBack === 1 ? '' : 's'} before today. A backdate needs its reason and the retained evidence{daysBack > 7 ? '; beyond seven days the Technical Director must also approve' : ''}.
              </div>
              <label className="field">
                <span className="field-label">Backdate reason *</span>
                <input className="input" maxLength={1000} value={backdateReason} onChange={(event) => setBackdateReason(event.target.value)} />
              </label>
              <label className="field">
                <span className="field-label">Evidence record id *</span>
                <input className="input mono" value={backdateEvidenceId} placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" onChange={(event) => setBackdateEvidenceId(event.target.value)} />
                <span className="field-hint">The id of the retained evidence (count sheet, inspection record). There is no evidence upload for adjustments yet.</span>
              </label>
            </>
          )}

          {mode === 'create' && (
            <div className="field field-wide">
              <span className="field-label">Counted by (optional)</span>
              {canPickCounters ? (
                <>
                  <select
                    className="input"
                    value=""
                    onChange={(event) => {
                      const id = event.target.value
                      if (id && !counterIds.includes(id)) setCounterIds((current) => [...current, id])
                    }}
                  >
                    <option value="">{recipients.length === 0 ? 'No employees listed' : 'Add a counter…'}</option>
                    {recipients.filter((row) => !counterIds.includes(row.Id)).map((row) => (
                      <option key={row.Id} value={row.Id}>{row.EmployeeCode} — {row.EmployeeName}</option>
                    ))}
                  </select>
                  {counterIds.length > 0 && (
                    <div className="action-row" style={{ flexWrap: 'wrap', marginTop: 6 }}>
                      {counterIds.map((id) => {
                        const row = recipients.find((candidate) => candidate.Id === id)
                        return (
                          <button key={id} type="button" className="btn btn-ghost" onClick={() => setCounterIds((current) => current.filter((value) => value !== id))}>
                            {row ? `${row.EmployeeCode} — ${row.EmployeeName}` : id} ✕
                          </button>
                        )
                      })}
                    </div>
                  )}
                  <span className="field-hint">Counters, like you as recorder, cannot approve or reject this adjustment.</span>
                </>
              ) : (
                <span className="field-hint">No employee list is readable with your role; you are recorded as the recorder.</span>
              )}
            </div>
          )}

          <ErrorAlert error={periodError} className="field-wide" fallback="Could not load the open inventory periods." />
          {locationError != null && (
            <div className="field-wide">
              <ErrorAlert error={locationError} fallback="Could not load the AVAILABLE rack/bin locations." />
              <p className="field-hint">The location list comes from the warehouse condition-location master; your role needs view permission on it to record an adjustment.</p>
            </div>
          )}

          <div className="field-wide form-section-title">Lines ({lines.filter((line) => line.itemId).length})</div>

          {!lockedLines && (
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
          )}
          <ErrorAlert error={lookupError} className="field-wide" fallback="Item lookup failed." />

          <div className="field-wide table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th style={{ width: 36 }}>#</th>
                  <th>Item *</th>
                  <th>Rack/bin *</th>
                  <th style={{ width: 130 }}>Change *</th>
                  <th className="text-right" style={{ width: 110 }}>Quantity *</th>
                  <th className="text-right" style={{ width: 130 }}>Unit value ₹ (ex-tax)</th>
                  <th>Lot</th>
                  <th>Serial</th>
                  <th>Remarks</th>
                  <th style={{ width: 50 }} />
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
                          {!lockedLines && (
                            <button type="button" className="btn btn-ghost" style={{ marginLeft: 8 }} onClick={() => setLine(line.key, { itemId: '', itemCode: '', itemName: '' })}>change</button>
                          )}
                        </div>
                      ) : (
                        <select
                          className="input"
                          value=""
                          disabled={itemOptions.length === 0}
                          onChange={(event) => {
                            const item = itemOptions.find((option) => option.Id === event.target.value)
                            if (item) setLine(line.key, { itemId: item.Id, itemCode: item.ItemCode, itemName: item.Name })
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
                      <select
                        className="input"
                        value={line.locationId}
                        disabled={lockedLines || (!warehouseId && mode === 'create')}
                        onChange={(event) => setLine(line.key, { locationId: event.target.value })}
                      >
                        <option value="">{warehouseId ? 'Pick…' : 'Pick the warehouse first'}</option>
                        {warehouseLocations.map((location) => (
                          <option key={location.Id} value={location.Id}>{location.BinCode}</option>
                        ))}
                        {line.locationId && !warehouseLocations.some((location) => location.Id === line.locationId) && (
                          <option value={line.locationId}>
                            {(mode === 'edit' ? existing : reverseOf)?.Lines.find((row) => row.WarehouseConditionLocationId === line.locationId)?.RackBinCode ?? 'Current location'}
                          </option>
                        )}
                      </select>
                    </td>
                    <td>
                      <select
                        className="input"
                        value={line.direction}
                        disabled={lockedLines}
                        onChange={(event) => {
                          const direction = event.target.value as Direction
                          setLine(line.key, { direction, unitValue: direction === 'REMOVE' ? '' : line.unitValue })
                        }}
                      >
                        <option value="REMOVE">− Remove</option>
                        <option value="ADD" disabled={reasonKind === 'DAMAGE_LOSS'}>+ Add</option>
                      </select>
                    </td>
                    <td className="text-right">
                      <input
                        className="input text-right mono"
                        inputMode="decimal"
                        value={line.quantity}
                        disabled={lockedLines}
                        onChange={(event) => setLine(line.key, { quantity: event.target.value })}
                      />
                    </td>
                    <td className="text-right">
                      {line.direction === 'ADD' ? (
                        <input
                          className="input text-right mono"
                          inputMode="decimal"
                          value={line.unitValue}
                          onChange={(event) => setLine(line.key, { unitValue: event.target.value })}
                        />
                      ) : (
                        <span className="field-hint" title="A removal is valued by the server from the FIFO layers it consumes.">FIFO</span>
                      )}
                    </td>
                    <td>
                      <input className="input mono" maxLength={160} value={line.lotNumber} onChange={(event) => setLine(line.key, { lotNumber: event.target.value })} />
                    </td>
                    <td>
                      <input
                        className="input mono"
                        maxLength={300}
                        value={line.serialNumber}
                        disabled={lockedLines}
                        placeholder="if serialized"
                        onChange={(event) => setLine(line.key, { serialNumber: event.target.value })}
                      />
                    </td>
                    <td>
                      <input className="input" maxLength={500} value={line.remarks} onChange={(event) => setLine(line.key, { remarks: event.target.value })} />
                    </td>
                    <td>
                      {!lockedLines && (
                        <button type="button" className="btn btn-ghost" disabled={lines.length === 1} onClick={() => setLines((current) => current.filter((row) => row.key !== line.key))}>✕</button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {!lockedLines && (
            <div className="field-wide">
              <button type="button" className="btn btn-ghost" onClick={() => setLines((current) => [...current, emptyLine()])}>+ Add line</button>
            </div>
          )}
          <p className="field-wide field-hint">
            Stated value of additions: <span className="mono">{formatRupees(addedValue)}</span>. Removals are valued by the server from FIFO when saved; the approvers are named by the server when you submit, from the total value, the reason and the backdate. A serialized line changes exactly one unit; to correct a wrong serial, remove the wrong one and add the right one.
          </p>

          <ErrorAlert error={error} className="field-wide" fallback="Could not save the adjustment." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving || !canSave} title={canSave ? undefined : 'Your role cannot record stock adjustments.'}>
              {saving ? 'Saving…' : mode === 'create' ? 'Save draft' : 'Save new revision'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
