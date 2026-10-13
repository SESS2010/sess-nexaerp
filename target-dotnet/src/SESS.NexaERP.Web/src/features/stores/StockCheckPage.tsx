import { getCompany } from '../../auth/authSession'
import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  getStockCheckPurchaseRequisition,
  listRackBins,
  listStockCheckRequisitions,
  newIdempotencyKey,
  stockCheckPurchaseRequisition,
} from '../../api/purchase'
import type {
  StockCheckPurchaseRequisitionDetail,
  PurchaseRequisitionSummary,
  RackBinSummary,
} from '../../types/purchase'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { formatDate } from '../purchase/PurchaseRequisitionListPage'
import { runStockCheckFlow } from './stockCheckFlow'
import { ScreenLifecycle } from '../purchase/screenLifecycle'
import { sessionScopeKey } from '../purchase/quotationDraft'
import { barcodeSessionKey } from '../barcodes/barcodeSession'
import { BarcodeScanField } from '../barcodes/BarcodeScanField'
import { resolveScan, addScanQuantity } from '../barcodes/barcodeScan'
import { BarcodeLabelDialog } from '../barcodes/BarcodeLabelDialog'
import { rackBinLabel } from '../barcodes/barcodeLabels'
import type { BarcodeLabel } from '../barcodes/barcodeLabels'
import { getRackBarcodeLabels, getStockCheckScanCandidates } from '../../api/barcodes'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

interface LineLocation {
  lineNumber: number
  warehouseCode: string
  rackBinCode: string
}

/**
 * Stores stock check on an approved PR. The storekeeper confirms, per line,
 * the warehouse and the physical rack/bin the stock would be reserved from;
 * the server compares on-hand against the requested quantity, reserves what
 * exists and hands the shortage to Purchase as a requirement handoff.
 *
 * /stores/stock-check lists PRs waiting; /stores/stock-check/:prNumber is one PR.
 */
export function StockCheckPage() {
  const { prNumber = '' } = useParams()
  const navigate = useNavigate()
  const { me, can } = useSession()
  const scope = sessionScopeKey(me)
  const barcodeScope = barcodeSessionKey(me)
  const lifecycle = useRef(new ScreenLifecycle()).current
  const detailOwner = useRef('')

  const [queue, setQueue] = useState<PurchaseRequisitionSummary[]>([])
  const [queueTotal, setQueueTotal] = useState(0)
  const [queueError, setQueueError] = useState<unknown>(null)

  const [detail, setDetail] = useState<StockCheckPurchaseRequisitionDetail | null>(null)
  const [locations, setLocations] = useState<LineLocation[]>([])
  const [bins, setBins] = useState<Record<string, RackBinSummary[]>>({})
  const [binsUnavailable, setBinsUnavailable] = useState(false)
  const [remarks, setRemarks] = useState('')
  const [loading, setLoading] = useState(false)
  const [busy, setBusy] = useState(false)
  const [barcodePending, setBarcodePending] = useState(false)
  const barcodePendingRef = useRef(false)
  const scanPendingChanged = (pending: boolean) => { barcodePendingRef.current = pending; setBarcodePending(pending) }
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [scanTally, setScanTally] = useState<Record<string, string>>({})
  const tallyRef = useRef<Record<string, string>>({})
  const countedSerials = useRef(new Set<string>())
  const [scanWarning, setScanWarning] = useState('')
  const [rackLabels, setRackLabels] = useState<BarcodeLabel[] | null>(null)
  const [labelBusy, setLabelBusy] = useState(false)
  const labelOwner = useRef(0)

  lifecycle.bindBusy('stock', setBusy)
  useEffect(() => { lifecycle.mount(); return () => lifecycle.unmount() }, [lifecycle])
  useEffect(() => {
    lifecycle.changeScope()
    lifecycle.setTarget('stock', scope + '|' + prNumber)
    detailOwner.current = ''
    labelOwner.current++; setRackLabels(null); setLabelBusy(false)
    countedSerials.current.clear(); tallyRef.current = {}; setScanTally({}); setScanWarning('')
    setDetail(null); setLocations([]); setBins({}); setBinsUnavailable(false); setRemarks('')
    setQueue([]); setQueueTotal(0); setError(null); setNotice('')
  }, [scope, prNumber, lifecycle])

  useEffect(() => {
    countedSerials.current.clear(); tallyRef.current = {}; setScanTally({}); setScanWarning('')
    labelOwner.current++; setRackLabels(null); setLabelBusy(false)
  }, [barcodeScope])
  const loadQueue = useCallback(async () => {
    const isLive = lifecycle.begin()
    setQueueError(null)
    try {
      // GET /stores/stock-check/requisitions → stores.stock-check:verify. Stores
      // has no purchase.requisitions:view, so the Purchase list endpoint 403s.
      const page = await listStockCheckRequisitions({ page: 1, pageSize: 50, status: 'StockCheckPending', sortBy: 'prnumber', sortDirection: 'asc' })
      if (!isLive()) return
      setQueue(page.Items)
      setQueueTotal(page.TotalCount)
    } catch (err) {
      if (!isLive()) return
      setQueue([])
      setQueueTotal(0)
      setQueueError(err)
    }
  }, [scope, prNumber, lifecycle])

  useEffect(() => {
    void loadQueue()
  }, [loadQueue])

  const loadBins = useCallback(async (warehouseCode: string) => {
    if (!warehouseCode || bins[warehouseCode]) return
    const isLive = lifecycle.begin()
    try {
      const page = await listRackBins(warehouseCode)
      if (!isLive()) return
      setBins((prev) => ({ ...prev, [warehouseCode]: page.Items.filter((bin) => bin.IsActive) }))
    } catch {
      if (!isLive()) return
      // No masters.rack-bins permission: fall back to typing the bin code.
      setBinsUnavailable(true)
    }
  }, [bins, scope, lifecycle])

  useEffect(() => {
    if (!prNumber) { setDetail(null); return }
    let cancelled = false
    const isLive = lifecycle.begin('stock')
    setLoading(true)
    setError(null)
    setNotice('')
    getStockCheckPurchaseRequisition(prNumber)
      .then((loaded) => {
        if (cancelled || !isLive()) return
        detailOwner.current = scope + '|' + prNumber
        setDetail(loaded)
        setLocations(loaded.Lines.map((line) => ({ lineNumber: line.LineNumber, warehouseCode: loaded.DeliveryWarehouseCode, rackBinCode: '' })))
        void loadBins(loaded.DeliveryWarehouseCode)
      })
      .catch((err) => { if (!cancelled && isLive()) { setDetail(null); setError(err) } })
      .finally(() => { if (!cancelled && isLive()) setLoading(false) })
    return () => { cancelled = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [prNumber, scope])

  const setLocation = (index: number, patch: Partial<LineLocation>) => {
    setLocations((prev) => prev.map((row, i) => (i === index ? { ...row, ...patch } : row)))
    if (patch.warehouseCode) void loadBins(patch.warehouseCode)
  }

  const submit = async () => {
    if (!me || me.OrganizationId !== getCompany()) { setError('Wait for the selected company session before saving.'); return }
    if (barcodePendingRef.current) { setError('Wait for the queued scans to finish before confirming stock check.'); return }
    if (!detail || detailOwner.current !== scope + '|' + prNumber || busy) return
    const missing = locations.filter((row) => !row.warehouseCode.trim() || !row.rackBinCode.trim())
    if (missing.length > 0) {
      setError(`Line ${missing.map((row) => row.lineNumber).join(', ')}: warehouse and a physical rack/bin are required — the reservation needs a location.`)
      return
    }
    if (!remarks.trim()) {
      setError('Remarks are required — say what was physically counted.')
      return
    }
    setError(null)
    const action = lifecycle.startAction('stock')
    try {
      const outcome = await runStockCheckFlow({ post: stockCheckPurchaseRequisition, read: getStockCheckPurchaseRequisition }, detail.PrNumber, {
        Remarks: remarks.trim(),
        Version: detail.Version,
        IdempotencyKey: newIdempotencyKey('stock-check'),
        Locations: locations.map((row) => ({
          LineNumber: row.lineNumber,
          WarehouseCode: row.warehouseCode.trim().toUpperCase(),
          RackBinCode: row.rackBinCode.trim().toUpperCase(),
        })),
      }, action.isLive)
      if (!outcome) return
      const { result, updated } = outcome
      setDetail(updated)
      setNotice(`${result.CheckNumber} (${result.ResultStatus}). ${describeOutcome(updated)}`)
      setRemarks('')
      void loadQueue()
    } catch (err) {
      if (action.isLive()) setError(err)
    } finally {
      action.finish()
    }
  }

  const checked = detail !== null && detail.Status !== 'StockCheckPending'
  // POST /purchase/requisitions/{prNumber}/stock-check → stores.stock-check:verify
  // Dedicated reads and the action share active stores/company/warehouse authority.
  const loadRackLabels = async () => {
    if (labelBusy || !(can('masters.rack-bins', 'view') && can('masters.rack-bins', 'print'))) return
    if (!me || getCompany() !== me.OrganizationId) { setError('Wait for the selected company session before reading labels.'); return }
    const company = me.OrganizationId
    const owner = ++labelOwner.current
    setLabelBusy(true)
    try {
      const rows = await getRackBarcodeLabels()
      if (labelOwner.current !== owner || getCompany() !== company) return
      if (!rows.length) { setError('No approved rack bins are printable in the selected company and scope.'); return }
      setRackLabels(rows.map(rackBinLabel))
    } catch (error) { if (labelOwner.current === owner) setError(error) }
    finally { if (labelOwner.current === owner) setLabelBusy(false) }
  }
  useEffect(() => () => { labelOwner.current++ }, [])
  const canStockCheck = can(PAGE_KEYS.stockCheck, 'verify')

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Stock Check</h1>
          <p className="page-sub">
            Approved requisitions waiting for Stores to confirm on-hand stock ({queueTotal} pending) — reserved from the rack/bin you name, shortage goes to Purchase
          </p>
        </div>
        {(can('masters.rack-bins', 'view') && can('masters.rack-bins', 'print')) && <button type="button" className="btn btn-ghost" disabled={labelBusy}
          onClick={() => void loadRackLabels()}>{labelBusy ? 'Loading labels…' : 'Print rack-bin labels'}</button>}        {detail && (
          <div className="action-row">
            <StatusBadge value={detail.Status} />
            {can('purchase.requisitions', 'view') && <Link to={`/purchase/requisitions/${encodeURIComponent(detail.PrNumber)}`} className="btn btn-ghost">Open PR</Link>}
          </div>
        )}
      </div>

      {rackLabels && (can('masters.rack-bins', 'view') && can('masters.rack-bins', 'print')) && <BarcodeLabelDialog canPrintNow={() => !!me && getCompany() === me.OrganizationId && can('masters.rack-bins', 'view') && can('masters.rack-bins', 'print')} labels={rackLabels} onClose={() => setRackLabels(null)} />}
      <ErrorAlert error={queueError} onReload={() => void loadQueue()} fallback="Could not load the stock-check queue." />

      {!prNumber && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr><th>PR number</th><th>Department</th><th>Requester</th><th>Required by</th><th>Priority</th><th className="text-right">Estimated (₹)</th><th>Status</th></tr>
            </thead>
            <tbody>
              {queue.length === 0 && !queueError && (
                <tr><td colSpan={7} className="table-empty">Nothing waiting for a stock check.</td></tr>
              )}
              {queue.map((row) => (
                <tr key={row.Id} className="row-click" onClick={() => navigate(`/stores/stock-check/${encodeURIComponent(row.PrNumber)}`)}>
                  <td className="mono">{row.PrNumber}</td>
                  <td>{row.RequestingDepartment}</td>
                  <td className="mono">{row.RequesterEmployeeCode}</td>
                  <td>{formatDate(row.RequiredByDate)}</td>
                  <td>{row.Priority}</td>
                  <td className="text-right mono">{row.EstimatedTotal.toLocaleString('en-IN')}</td>
                  <td><StatusBadge value={row.Status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {prNumber && loading && <p>Loading…</p>}
      {prNumber && !loading && !detail && (
        <>
          <ErrorAlert error={error} fallback="Requisition not found." />
          <button type="button" className="btn btn-ghost" onClick={() => navigate('/stores/stock-check')}>‹ Back to queue</button>
        </>
      )}

      {detail && (
        <div data-barcode-surface>
          <p className="page-sub">
            <span className="mono">{detail.PrNumber}</span> · {detail.RequestingDepartment} · raised by <span className="mono">{detail.RequesterEmployeeCode}</span>
            {' · '}required by {formatDate(detail.RequiredByDate)} · delivery warehouse <span className="mono">{detail.DeliveryWarehouseCode}</span>
          </p>
          {notice && <div className="alert" role="status">{notice}</div>}
          <ErrorAlert error={error} fallback="The stock check failed." />
          {binsUnavailable && canStockCheck && (
            <div className="alert alert-warn" role="status">
              Rack/bin list is not readable with your role (masters.rack-bins:view). Type the bin code exactly as printed on the rack.
            </div>
          )}

          {!checked && canStockCheck && <BarcodeScanField expectedCompany={me?.OrganizationId} companyContext={getCompany} onPendingChange={scanPendingChanged} target={barcodeScope + '|' + prNumber + '|' + detail.Version} disabled={busy}
            lookup={async (text, context) => resolveScan(await getStockCheckScanCandidates(prNumber, context), text, me?.OrganizationId)}
              focusQuantity={(result) => result.kind === 'match' ? '[data-stock-quantity="' + result.candidate.lineKey + '"]' : ''}
            onResolved={(result) => {
              if (result.kind !== 'match') { setScanWarning(result.message); return }
              if (result.candidate.inventorySerialId && countedSerials.current.has(result.candidate.inventorySerialId)) { setScanWarning('This physical serial is already in the local tally. Nothing was added.'); return }
              const key = result.candidate.lineKey
              const next = addScanQuantity(tallyRef.current[key] ?? '', Number.MAX_SAFE_INTEGER)
              setScanWarning(next.warning)
              if (!next.changed) return
              if (result.candidate.inventorySerialId) countedSerials.current.add(result.candidate.inventorySerialId)
              tallyRef.current = { ...tallyRef.current, [key]: next.quantity }
              setScanTally(tallyRef.current)
            }} />}
          {scanWarning && <span className="scan-warning" role="alert">{scanWarning}</span>}
          {!checked && <p className="field-hint">Scan tally is a local aid only. It does not change requested quantity, ERP on-hand stock or the stock-check posting.</p>}          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>#</th><th>Item</th><th>UOM</th><th className="text-right">Requested</th>
                  <th className="text-right">On hand</th><th className="text-right">Available</th>
                  <th>Scan tally (local)</th><th>Warehouse *</th><th>Rack / Bin *</th><th>Line status</th>
                </tr>
              </thead>
              <tbody>
                {detail.Lines.map((line, index) => {
                  const location = locations[index]
                  const options = location ? bins[location.warehouseCode] ?? [] : []
                  return (
                    <tr key={line.LineNumber}>
                      <td className="mono">{line.LineNumber}</td>
                      <td><span className="mono">{line.ItemCode}</span> — {line.ItemName}</td>
                      <td className="mono">{line.Uom}</td>
                      <td className="text-right mono">{line.RequestedQuantity}</td>
                      <td className="text-right mono">{line.OnHand}</td>
                      <td className="text-right mono">{line.Available}</td>
                      <td><input className="input text-right mono" inputMode="decimal" data-barcode-quantity data-stock-quantity={line.LineNumber}
                        disabled={busy || checked} value={scanTally[String(line.LineNumber)] ?? ''}
                        onChange={(event) => { tallyRef.current = { ...tallyRef.current, [String(line.LineNumber)]: event.target.value }; setScanTally(tallyRef.current) }} /></td>                      <td>
                        {canStockCheck ? (
                          <input className="input mono" aria-label={`Line ${line.LineNumber} warehouse`} disabled={checked || busy}
                            value={location?.warehouseCode ?? ''} onChange={(event) => setLocation(index, { warehouseCode: event.target.value })} />
                        ) : (
                          <span className="mono">{location?.warehouseCode || '—'}</span>
                        )}
                      </td>
                      <td>
                        {!canStockCheck ? (
                          <span className="mono">{location?.rackBinCode || '—'}</span>
                        ) : !binsUnavailable && options.length > 0 ? (
                          <select className="input" aria-label={`Line ${line.LineNumber} rack/bin`} disabled={checked || busy}
                            value={location?.rackBinCode ?? ''} onChange={(event) => setLocation(index, { rackBinCode: event.target.value })}>
                            <option value="">Select rack/bin…</option>
                            {options.map((bin) => (
                              <option key={bin.Id} value={bin.BinCode}>{bin.BinCode} — {bin.RackName} ({bin.MaterialCondition})</option>
                            ))}
                          </select>
                        ) : (
                          <input className="input mono" aria-label={`Line ${line.LineNumber} rack/bin`} placeholder="Bin code" disabled={checked || busy}
                            value={location?.rackBinCode ?? ''} onChange={(event) => setLocation(index, { rackBinCode: event.target.value })} />
                        )}
                      </td>
                      <td><StatusBadge value={line.LineStatus} /></td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          {!checked && (
            <div className="card">
              {canStockCheck && (
                <label className="field field-wide">
                  <span className="field-label">Remarks * (what was counted)</span>
                  <input className="input" value={remarks} disabled={busy} onChange={(event) => setRemarks(event.target.value)} />
                </label>
              )}
              <div className="modal-actions">
                <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => navigate('/stores/stock-check')}>‹ Queue</button>
                {canStockCheck && (
                  <button type="button" className="btn btn-primary" disabled={busy || barcodePending} onClick={() => void submit()}>
                    {busy ? 'Checking…' : 'Confirm stock check'}
                  </button>
                )}
              </div>
            </div>
          )}
          {checked && (
            <div className="modal-actions">
              <button type="button" className="btn btn-ghost" onClick={() => navigate('/stores/stock-check')}>‹ Back to queue</button>
            </div>
          )}
        </div>
      )}
    </div>
  )
}

function describeOutcome(pr: StockCheckPurchaseRequisitionDetail): string {
  const shortage = pr.Lines.reduce((sum, line) => sum + (line.ShortageQuantity || 0), 0)
  const reserved = pr.Lines.reduce((sum, line) => sum + (line.ReservedQuantity || 0), 0)
  switch (pr.Status) {
    case 'FullyAvailable':
    case 'Reserved':
      return `${pr.PrNumber}: stock available — ${reserved} reserved from stores. Nothing to buy.`
    case 'PartiallyAvailable':
      return `${pr.PrNumber}: ${reserved} reserved, ${shortage} short. The shortage has gone to Purchase as a requirement handoff.`
    case 'NotAvailable':
    case 'PurchaseHandoffCreated':
      return `${pr.PrNumber}: nothing on hand — ${shortage} handed to Purchase for RFQ.`
    default:
      return `${pr.PrNumber}: stock check recorded. Status is now ${pr.Status}.`
  }
}
