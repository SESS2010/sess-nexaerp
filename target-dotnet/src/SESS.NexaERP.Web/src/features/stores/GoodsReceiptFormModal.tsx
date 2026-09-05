import { useEffect, useMemo, useState } from 'react'
import { createGoodsReceipt, updateGoodsReceipt } from '../../api/goodsReceipts'
import { listGateEntries, newIdempotencyKey, getSourcePurchaseOrder } from '../../api/stores'
import type { GateEntryResult, SourcePurchaseOrder } from '../../types/stores'
import type {
  CreateGoodsReceiptRequest,
  GoodsReceiptLineRequest,
  GoodsReceiptResult,
  IsoGrnVerification,
} from '../../types/goodsReceipt'
import { DEFAULT_ISO_GRN_VERIFICATION, warrantyFromBillDate } from '../../types/goodsReceipt'
import { ErrorAlert } from '../../components/ErrorAlert'
import { ScannerInput } from '../../components/ScannerInput'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

interface DraftLot {
  lotOrdinal: number
  quantity: string
  supplierLotNumber: string
  manufacturerLotNumber: string
  manufactureDate: string
  expiryDate: string
}

interface DraftSerial {
  lotOrdinal: number
  entered: string
  stored: string
  acknowledged: boolean
  reason: string
}

interface DraftLine {
  gateEntryLineId: string
  lineNumber: number
  itemCode: string
  uom: string
  /** Gate-delivered quantity. The server sets ReceivedQuantity from the gate line; lots must sum to it. */
  quantity: number
  /** From the source PO, for the storekeeper's over-receipt awareness. Null while the PO is loading. */
  poOrderedQuantity: number | null
  /**
   * Known only in edit mode — the server computes it at create from the item
   * setting and the unit-rate threshold. Null means "not known yet".
   */
  serialCaptureMode: 'REQUIRED' | 'OPTIONAL' | null
  lots: DraftLot[]
  serials: DraftSerial[]
}

function newLot(ordinal: number, quantity: number | ''): DraftLot {
  return {
    lotOrdinal: ordinal,
    quantity: String(quantity),
    supplierLotNumber: '',
    manufacturerLotNumber: '',
    manufactureDate: '',
    expiryDate: '',
  }
}

function nowLocal(): string {
  const now = new Date()
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}`
}

function toLocalInput(iso: string): string {
  const date = new Date(iso)
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function parseIso(json: string): IsoGrnVerification {
  try {
    return { ...DEFAULT_ISO_GRN_VERIFICATION, ...(JSON.parse(json) as Partial<IsoGrnVerification>) }
  } catch {
    return { ...DEFAULT_ISO_GRN_VERIFICATION }
  }
}

interface Props {
  mode: 'create' | 'edit'
  existing?: GoodsReceiptResult
  onClose: () => void
  onSaved: (result: GoodsReceiptResult) => void
}

/**
 * GRN entry, scanner-first. The storekeeper scans the Gate Entry number to
 * start, then scans serials one after another into the always-focused scan box
 * — each scan lands on the active line and advances automatically when the
 * line is full. Manual typing is behind the "Keyboard entry" toggle.
 *
 * The server owns the hard rules (finalized gate only, over-receipt refusal,
 * lot totals, serial thresholds, 13-month warranty); this form makes them
 * visible before submit rather than re-implementing them.
 */
export function GoodsReceiptFormModal({ mode, existing, onClose, onSaved }: Props) {
  const { can, hasRole } = useSession()
  const [gates, setGates] = useState<GateEntryResult[]>([])
  const [gatesTotal, setGatesTotal] = useState(0)
  const [gatesLoading, setGatesLoading] = useState(false)
  const [gate, setGate] = useState<GateEntryResult | null>(null)
  const [gateWarning, setGateWarning] = useState('')

  const [vendorBillNumber, setVendorBillNumber] = useState(existing?.VendorBillNumber ?? '')
  const [vendorBillDate, setVendorBillDate] = useState(existing?.VendorBillDate ?? '')
  const [receivedAt, setReceivedAt] = useState(existing ? toLocalInput(existing.ReceivedAt) : nowLocal())
  const [iso, setIso] = useState<IsoGrnVerification>(
    existing ? parseIso(existing.IsoReceiptVerificationJson) : { ...DEFAULT_ISO_GRN_VERIFICATION },
  )

  const [lines, setLines] = useState<DraftLine[]>([])
  const [activeLine, setActiveLine] = useState(0)
  const [scanMode, setScanMode] = useState(true)
  const [serialWarning, setSerialWarning] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<unknown>(null)

  // Create: load the finalized gate entries the storekeeper can receive against.
  // Edit: the gate is fixed on the document; only bill/lots/serials change.
  useEffect(() => {
    if (mode === 'edit' && existing) {
      hydrateFromExisting(existing)
      return
    }
    setGatesLoading(true)
    // The dropdown is only the fallback for a storekeeper without a scanner,
    // so the most recent finalized gates are enough; a scan is resolved on the
    // server by exact number (gateEntryNumber filter), never against this list.
    listGateEntries({ page: 1, pageSize: 100, state: 'FINALIZED' })
      .then((result) => {
        setGates(result.Items ?? [])
        setGatesTotal(result.TotalCount)
      })
      .catch((err) => setError(err))
      .finally(() => setGatesLoading(false))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function hydrateFromExisting(grn: GoodsReceiptResult) {
    setLines(
      grn.Lines.map((line) => ({
        gateEntryLineId: line.GateEntryLineId,
        lineNumber: line.LineNumber,
        itemCode: line.ItemCode,
        uom: line.Uom,
        quantity: line.ReceivedQuantity,
        poOrderedQuantity: null,
        serialCaptureMode: line.SerialCaptureMode,
        lots: line.Lots.map((lot) => ({
          lotOrdinal: lot.LotOrdinal,
          quantity: String(lot.Quantity),
          supplierLotNumber: lot.SupplierLotNumber ?? '',
          manufacturerLotNumber: lot.ManufacturerLotNumber ?? '',
          manufactureDate: lot.ManufactureDate ?? '',
          expiryDate: lot.ExpiryDate ?? '',
        })),
        serials: line.Serials.map((serial) => ({
          lotOrdinal: serial.LotOrdinal,
          entered: serial.EnteredSerialNumber,
          stored: serial.StoredSerialNumber,
          acknowledged: serial.DuplicateWarningAcknowledged,
          reason: serial.DisambiguationReason ?? '',
        })),
      })),
    )
  }

  function selectGate(selected: GateEntryResult) {
    setGate(selected)
    setGateWarning('')
    setActiveLine(0)
    setLines(
      selected.Lines.map((line) => ({
        gateEntryLineId: line.Id,
        lineNumber: line.LineNumber,
        itemCode: line.ItemCode,
        uom: line.Uom,
        quantity: line.DeliveredQuantity,
        poOrderedQuantity: null,
        serialCaptureMode: null,
        lots: [newLot(1, line.DeliveredQuantity)],
        serials: [],
      })),
    )
    // The gate line has no PO ordered quantity; pull the source PO so the
    // storekeeper sees "receiving X of Y ordered" before the server rules on it.
    getSourcePurchaseOrder(selected.PurchaseOrderNumber)
      .then((po: SourcePurchaseOrder) => {
        const ordered = new Map(po.Lines.map((line) => [line.Id, line.OrderedQuantity]))
        setLines((prev) =>
          prev.map((line) => {
            const gateLine = selected.Lines.find((g) => g.Id === line.gateEntryLineId)
            const po1 = gateLine ? ordered.get(gateLine.PurchaseOrderLineId) : undefined
            return { ...line, poOrderedQuantity: po1 ?? null }
          }),
        )
      })
      .catch(() => undefined) // enrichment only — the server still enforces the cap
  }

  const onGateScan = (scanned: string) => {
    const wanted = scanned.trim().toUpperCase()
    if (!wanted) return
    // Exact-number lookup on the server: a scan must resolve regardless of how
    // many gates exist or which page the dropdown happened to load.
    setGateWarning('')
    setGatesLoading(true)
    listGateEntries({ page: 1, pageSize: 1, gateEntryNumber: wanted, state: 'FINALIZED' })
      .then((result) => {
        const found = result.Items?.[0]
        if (found) {
          selectGate(found)
        } else {
          setGateWarning(`No finalized Gate Entry '${wanted}' found. A GRN needs a finalized Gate Entry first.`)
        }
      })
      .catch((err) => setError(err))
      .finally(() => setGatesLoading(false))
  }

  const setLine = (index: number, patch: Partial<DraftLine>) =>
    setLines((prev) => prev.map((line, i) => (i === index ? { ...line, ...patch } : line)))

  /** All stored serials currently on the form, for instant duplicate warning. */
  const storedSerials = useMemo(() => {
    const seen = new Map<string, number>()
    lines.forEach((line) =>
      line.serials.forEach((serial) => {
        const key = serial.stored.trim().toUpperCase()
        seen.set(key, (seen.get(key) ?? 0) + 1)
      }),
    )
    return seen
  }, [lines])

  const onSerialScan = (scanned: string) => {
    setSerialWarning('')
    const line = lines[activeLine]
    if (!line) {
      setSerialWarning('Pick a line to scan serials into.')
      return
    }
    if (line.serials.length >= line.quantity) {
      setSerialWarning(`Line ${line.lineNumber} already has ${line.quantity} serials.`)
      return
    }
    if (storedSerials.has(scanned.toUpperCase())) {
      setSerialWarning(`'${scanned}' is already on this GRN. Every serial must be unique — check the item or disambiguate below.`)
      return
    }
    const defaultLot = line.lots[0]?.lotOrdinal ?? 1
    const next = [...line.serials, { lotOrdinal: defaultLot, entered: scanned, stored: scanned, acknowledged: false, reason: '' }]
    setLine(activeLine, { serials: next })
    // Line full → advance the scanner to the NEXT line still short of serials,
    // searching forward from the active line and wrapping — never backwards
    // past lines the storekeeper already dealt with.
    if (next.length >= line.quantity) {
      for (let step = 1; step < lines.length; step++) {
        const candidate = (activeLine + step) % lines.length
        if (lines[candidate].serials.length < lines[candidate].quantity) {
          setActiveLine(candidate)
          break
        }
      }
    }
  }

  const removeSerial = (lineIndex: number, serialIndex: number) => {
    const line = lines[lineIndex]
    setLine(lineIndex, { serials: line.serials.filter((_, i) => i !== serialIndex) })
  }

  const setSerial = (lineIndex: number, serialIndex: number, patch: Partial<DraftSerial>) => {
    const line = lines[lineIndex]
    setLine(lineIndex, {
      serials: line.serials.map((serial, i) => (i === serialIndex ? { ...serial, ...patch } : serial)),
    })
  }

  const addLot = (lineIndex: number) => {
    const line = lines[lineIndex]
    const ordinal = Math.max(0, ...line.lots.map((lot) => lot.lotOrdinal)) + 1
    setLine(lineIndex, { lots: [...line.lots, newLot(ordinal, '')] })
  }

  const removeLot = (lineIndex: number, lotIndex: number) => {
    const line = lines[lineIndex]
    if (line.lots.length <= 1) return
    const removed = line.lots[lotIndex]
    setLine(lineIndex, {
      lots: line.lots.filter((_, i) => i !== lotIndex),
      // Serials pointing at the removed lot fall back to the first remaining lot.
      serials: line.serials.map((serial) =>
        serial.lotOrdinal === removed.lotOrdinal
          ? { ...serial, lotOrdinal: line.lots.find((_, i) => i !== lotIndex)?.lotOrdinal ?? 1 }
          : serial,
      ),
    })
  }

  const setLot = (lineIndex: number, lotIndex: number, patch: Partial<DraftLot>) => {
    const line = lines[lineIndex]
    setLine(lineIndex, { lots: line.lots.map((lot, i) => (i === lotIndex ? { ...lot, ...patch } : lot)) })
  }

  const warranty = warrantyFromBillDate(vendorBillDate)

  // Quantities are 6-decimal-place decimals on the wire; comparing at that
  // resolution avoids IEEE float artifacts (0.1 + 0.2 must equal 0.3 here,
  // as it does in the server's decimal arithmetic).
  const asMicro = (value: number) => Math.round(value * 1e6)

  const clientChecks = (): string | null => {
    if (mode === 'create' && !gate) return 'Scan or pick a finalized Gate Entry — a GRN cannot exist without one.'
    if (!vendorBillNumber.trim()) return 'Vendor bill number is mandatory.'
    if (!vendorBillDate) return 'Vendor bill date is mandatory — warranty runs 13 months from it.'
    if (!receivedAt || Number.isNaN(new Date(receivedAt).getTime())) {
      return 'Received at is required.'
    }
    for (const line of lines) {
      for (const lot of line.lots) {
        if ((Number(lot.quantity) || 0) <= 0) {
          return `Line ${line.lineNumber} (${line.itemCode}): lot ${lot.lotOrdinal} has no quantity. Every lot needs a positive quantity — remove empty split rows.`
        }
      }
      const lotSum = line.lots.reduce((sum, lot) => sum + (Number(lot.quantity) || 0), 0)
      if (asMicro(lotSum) !== asMicro(line.quantity)) {
        return `Line ${line.lineNumber} (${line.itemCode}): lot quantities sum to ${lotSum}, but ${line.quantity} was received. They must match exactly.`
      }
      if (line.serialCaptureMode === 'REQUIRED' && line.serials.length !== line.quantity) {
        return `Line ${line.lineNumber} (${line.itemCode}) requires one serial per unit: ${line.serials.length} of ${line.quantity} scanned.`
      }
      if (line.serials.length > 0 && line.serials.length !== line.quantity) {
        return `Line ${line.lineNumber} (${line.itemCode}): ${line.serials.length} of ${line.quantity} serials scanned. Serials must be complete, or empty for non-serialized items.`
      }
      for (const lot of line.lots) {
        const serialsOnLot = line.serials.filter((serial) => serial.lotOrdinal === lot.lotOrdinal).length
        if (line.serials.length > 0 && serialsOnLot !== (Number(lot.quantity) || 0)) {
          return `Line ${line.lineNumber}: lot ${lot.lotOrdinal} has ${serialsOnLot} serials for ${lot.quantity} units. Serials must reconcile by lot.`
        }
      }
      for (const serial of line.serials) {
        if (!serial.stored.trim()) {
          return `Serial '${serial.entered}': the stored serial cannot be empty. Disambiguate by changing it, not by clearing it.`
        }
        if (serial.stored.trim() !== serial.entered.trim() && (!serial.acknowledged || !serial.reason.trim())) {
          return `Serial '${serial.entered}': a changed stored value needs the duplicate warning acknowledged and a reason recorded.`
        }
      }
    }
    return null
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    const problem = clientChecks()
    if (problem) {
      setError(problem)
      return
    }

    const payloadLines: GoodsReceiptLineRequest[] = lines.map((line) => ({
      GateEntryLineId: line.gateEntryLineId,
      Lots: line.lots.map((lot) => ({
        LotOrdinal: lot.lotOrdinal,
        Quantity: Number(lot.quantity) || 0,
        SupplierLotNumber: lot.supplierLotNumber.trim() || null,
        ManufacturerLotNumber: lot.manufacturerLotNumber.trim() || null,
        ManufactureDate: lot.manufactureDate || null,
        ExpiryDate: lot.expiryDate || null,
      })),
      Serials: line.serials.map((serial, index) => ({
        SerialOrdinal: index + 1,
        LotOrdinal: serial.lotOrdinal,
        EnteredSerialNumber: serial.entered.trim(),
        StoredSerialNumber: serial.stored.trim(),
        DuplicateWarningAcknowledged: serial.acknowledged,
        DisambiguationReason: serial.reason.trim() || null,
      })),
    }))

    setSaving(true)
    try {
      const shared = {
        VendorBillNumber: vendorBillNumber.trim(),
        VendorBillDate: vendorBillDate,
        ReceivedAt: new Date(receivedAt).toISOString(),
        IsoReceiptVerificationJson: JSON.stringify(iso),
        Lines: payloadLines,
      }
      const result =
        mode === 'create'
          ? await createGoodsReceipt(
              { GateEntryNumber: gate!.GateEntryNumber, ...shared } as CreateGoodsReceiptRequest,
              newIdempotencyKey('grn'),
            )
          : await updateGoodsReceipt(existing!.Id, { ...shared, Version: existing!.Version })
      onSaved(result)
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  const setIsoFlag = (key: keyof IsoGrnVerification) => (event: { target: { checked: boolean } }) =>
    setIso((prev) => ({ ...prev, [key]: event.target.checked }))

  const gateHeader = mode === 'edit' ? existing : gate
  const line = lines[activeLine]
  // POST/PUT /stores/goods-receipts → inventory.grn:create / :update (explicit-grant
  // page) plus EfGoodsReceiptService.ActorRole() = STORES_EXECUTIVE or STORES_ASSISTANT.
  const canSave =
    can(PAGE_KEYS.grn, mode === 'create' ? 'create' : 'update') &&
    (hasRole('STORES_EXECUTIVE') || hasRole('STORES_ASSISTANT'))

  return (
    <div className="modal-backdrop" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <div className="modal modal-wide">
        <div className="modal-header">
          <h2>{mode === 'create' ? 'New Goods Receipt' : `Edit ${existing?.GrnNumber}`}</h2>
          <label className="scan-toggle">
            <input type="checkbox" checked={!scanMode} onChange={(event) => setScanMode(!event.target.checked)} />
            Keyboard entry
          </label>
        </div>

        <form
          onSubmit={submit}
          onKeyDown={(event) => {
            // A USB scanner always ends with Enter. If a scan (or an Enter out
            // of habit) lands in an ordinary field, it must not submit the
            // form — only the explicit submit button, clicked or focused, may.
            const target = event.target as HTMLElement
            if (event.key === 'Enter' && !target.closest('button[type="submit"], textarea')) {
              event.preventDefault()
            }
          }}
        >
          {/* Step 1 — the gate. Scan its number; the dropdown is the fallback. */}
          {mode === 'create' && !gate && (
            <div className="form-grid">
              <div className="field-wide">
                <ScannerInput
                  label="Scan Gate Entry number"
                  placeholder="GE-2026-0001"
                  scanMode={scanMode}
                  onScan={onGateScan}
                  warning={gateWarning}
                  hint={gatesLoading ? 'Looking up gate entries…' : `${gatesTotal} finalized gate entries; a scan is matched on the server by exact number`}
                />
              </div>
              <label className="field field-wide">
                <span className="field-label">…or pick one</span>
                <select
                  className="input"
                  value=""
                  onChange={(event) => {
                    const found = gates.find((g) => g.Id === event.target.value)
                    if (found) selectGate(found)
                  }}
                >
                  <option value="">Select a finalized Gate Entry…</option>
                  {gates.map((g) => (
                    <option key={g.Id} value={g.Id}>
                      {g.GateEntryNumber} — {g.VendorName} — PO {g.PurchaseOrderNumber}
                    </option>
                  ))}
                </select>
              </label>
            </div>
          )}

          {gateHeader && (
            <>
              <div className="doc-context">
                <span><span className="field-label">Gate Entry</span> <span className="mono">{gateHeader.GateEntryNumber}</span></span>
                <span><span className="field-label">PO</span> <span className="mono">{gateHeader.PurchaseOrderNumber}</span></span>
                <span><span className="field-label">Vendor</span> {gateHeader.VendorName}</span>
                <span><span className="field-label">Vendor DC</span> <span className="mono">{gateHeader.VendorDcNumber}</span></span>
                <span><span className="field-label">Transport</span> {gateHeader.ModeOfTransport}</span>
                {mode === 'create' && (
                  <button type="button" className="btn btn-ghost" onClick={() => { setGate(null); setLines([]) }}>
                    Change gate
                  </button>
                )}
              </div>

              <div className="form-grid">
                <label className="field">
                  <span className="field-label">Vendor bill number *</span>
                  <input className="input" required value={vendorBillNumber} onChange={(event) => setVendorBillNumber(event.target.value)} />
                </label>
                <label className="field">
                  <span className="field-label">Vendor bill date *</span>
                  <input className="input" type="date" required value={vendorBillDate} onChange={(event) => setVendorBillDate(event.target.value)} />
                  {warranty && <span className="field-hint">Warranty until {warranty} (bill date + 13 months)</span>}
                </label>
                <label className="field">
                  <span className="field-label">Received at</span>
                  <input className="input" type="datetime-local" required value={receivedAt} onChange={(event) => setReceivedAt(event.target.value)} />
                </label>
                <div className="field">
                  <span className="field-label">ISO receipt verification</span>
                  <div className="check-grid">
                    <label><input type="checkbox" checked={iso.BillVerified} onChange={setIsoFlag('BillVerified')} /> Bill verified</label>
                    <label><input type="checkbox" checked={iso.QuantityVerified} onChange={setIsoFlag('QuantityVerified')} /> Quantity verified</label>
                    <label><input type="checkbox" checked={iso.CertificatesReceived} onChange={setIsoFlag('CertificatesReceived')} /> Certificates received</label>
                    <label><input type="checkbox" checked={iso.TestReportReceived} onChange={setIsoFlag('TestReportReceived')} /> Test report received</label>
                  </div>
                </div>
                <label className="field field-wide">
                  <span className="field-label">Verification remarks</span>
                  <input className="input" value={iso.Remarks} onChange={(event) => setIso((prev) => ({ ...prev, Remarks: event.target.value }))} />
                </label>
              </div>

              {/* Step 2 — serial scanning. One box, always focused; scans land on the active line. */}
              {lines.length > 0 && (
                <div className="scan-panel">
                  <ScannerInput
                    label={line ? `Scan serials → line ${line.lineNumber} · ${line.itemCode}` : 'Scan serials'}
                    placeholder="Scan item serial…"
                    scanMode={scanMode}
                    onScan={onSerialScan}
                    warning={serialWarning}
                    hint={line ? `${line.serials.length} / ${line.quantity} serials on this line. Serialized items need every unit scanned — the server refuses the draft otherwise.` : undefined}
                  />
                </div>
              )}

              {lines.map((draftLine, lineIndex) => (
                <div
                  key={draftLine.gateEntryLineId}
                  className={lineIndex === activeLine ? 'grn-line grn-line-active' : 'grn-line'}
                  onClick={() => setActiveLine(lineIndex)}
                >
                  <div className="grn-line-head">
                    <span className="mono">#{draftLine.lineNumber}</span>
                    <strong className="mono">{draftLine.itemCode}</strong>
                    <span>
                      Receiving <strong>{draftLine.quantity}</strong> {draftLine.uom}
                      {draftLine.poOrderedQuantity !== null && <> — PO quantity {draftLine.poOrderedQuantity}</>}
                    </span>
                    <span className="spacer" />
                    <span className={draftLine.serials.length === draftLine.quantity ? 'badge badge-ok' : draftLine.serialCaptureMode === 'REQUIRED' ? 'badge badge-warn' : 'badge badge-muted'}>
                      {draftLine.serials.length} / {draftLine.quantity} serials{draftLine.serialCaptureMode === 'REQUIRED' ? ' required' : ''}
                    </span>
                  </div>

                  {draftLine.lots.map((lot, lotIndex) => (
                    <div key={lot.lotOrdinal} className="lot-row">
                      <span className="field-label">Lot {lot.lotOrdinal}</span>
                      <input className="input" type="number" min="0" step="any" placeholder="Qty" title="Lot quantity"
                        value={lot.quantity} onChange={(event) => setLot(lineIndex, lotIndex, { quantity: event.target.value })} />
                      <input className="input" placeholder="Supplier lot no."
                        value={lot.supplierLotNumber} onChange={(event) => setLot(lineIndex, lotIndex, { supplierLotNumber: event.target.value })} />
                      <input className="input" placeholder="Mfr lot no."
                        value={lot.manufacturerLotNumber} onChange={(event) => setLot(lineIndex, lotIndex, { manufacturerLotNumber: event.target.value })} />
                      <input className="input" type="date" title="Manufacture date"
                        value={lot.manufactureDate} onChange={(event) => setLot(lineIndex, lotIndex, { manufactureDate: event.target.value })} />
                      <input className="input" type="date" title="Expiry date"
                        value={lot.expiryDate} onChange={(event) => setLot(lineIndex, lotIndex, { expiryDate: event.target.value })} />
                      {draftLine.lots.length > 1 && (
                        <button type="button" className="btn btn-ghost" onClick={() => removeLot(lineIndex, lotIndex)}>✕</button>
                      )}
                    </div>
                  ))}
                  <button type="button" className="btn btn-ghost lot-add" onClick={() => addLot(lineIndex)}>+ Split lot</button>

                  {draftLine.serials.length > 0 && (
                    <div className="serial-list">
                      {draftLine.serials.map((serial, serialIndex) => {
                        const duplicate = (storedSerials.get(serial.stored.trim().toUpperCase()) ?? 0) > 1
                        const changed = serial.stored.trim() !== serial.entered.trim()
                        return (
                          <div key={serialIndex} className={duplicate ? 'serial-row serial-dup' : 'serial-row'}>
                            <span className="mono serial-ordinal">{serialIndex + 1}</span>
                            <span className="mono" title="As scanned">{serial.entered}</span>
                            <input className="input mono" title="Stored serial — edit to disambiguate a duplicate"
                              value={serial.stored} onChange={(event) => setSerial(lineIndex, serialIndex, { stored: event.target.value })} />
                            {draftLine.lots.length > 1 && (
                              <select className="input" title="Lot" value={serial.lotOrdinal}
                                onChange={(event) => setSerial(lineIndex, serialIndex, { lotOrdinal: Number(event.target.value) })}>
                                {draftLine.lots.map((lot) => <option key={lot.lotOrdinal} value={lot.lotOrdinal}>Lot {lot.lotOrdinal}</option>)}
                              </select>
                            )}
                            {changed && (
                              <>
                                <label className="serial-ack">
                                  <input type="checkbox" checked={serial.acknowledged}
                                    onChange={(event) => setSerial(lineIndex, serialIndex, { acknowledged: event.target.checked })} />
                                  Duplicate acknowledged
                                </label>
                                <input className="input" placeholder="Reason for changed serial *"
                                  value={serial.reason} onChange={(event) => setSerial(lineIndex, serialIndex, { reason: event.target.value })} />
                              </>
                            )}
                            <button type="button" className="btn btn-ghost" title="Remove serial"
                              onClick={() => removeSerial(lineIndex, serialIndex)}>✕</button>
                          </div>
                        )
                      })}
                    </div>
                  )}
                </div>
              ))}
            </>
          )}

          <ErrorAlert error={error} fallback="Could not save the GRN." />

          <div className="modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            {canSave && (
              <button type="submit" className="btn btn-primary" disabled={saving || (mode === 'create' && !gate)}>
                {saving ? 'Saving…' : mode === 'create' ? 'Create draft GRN' : 'Save changes'}
              </button>
            )}
          </div>
        </form>
      </div>
    </div>
  )
}
