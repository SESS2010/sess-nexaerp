import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  getPurchaseRequisition,
  listPurchaseRequisitions,
  listRackBins,
  newIdempotencyKey,
  stockCheckPurchaseRequisition,
} from '../../api/purchase'
import type {
  PurchaseRequisitionDetail,
  PurchaseRequisitionSummary,
  RackBinSummary,
} from '../../types/purchase'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { formatDate } from '../purchase/PurchaseRequisitionListPage'
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
  const { can } = useSession()

  const [queue, setQueue] = useState<PurchaseRequisitionSummary[]>([])
  const [queueTotal, setQueueTotal] = useState(0)
  const [queueError, setQueueError] = useState<unknown>(null)

  const [detail, setDetail] = useState<PurchaseRequisitionDetail | null>(null)
  const [locations, setLocations] = useState<LineLocation[]>([])
  const [bins, setBins] = useState<Record<string, RackBinSummary[]>>({})
  const [binsUnavailable, setBinsUnavailable] = useState(false)
  const [remarks, setRemarks] = useState('')
  const [loading, setLoading] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')

  const loadQueue = useCallback(async () => {
    setQueueError(null)
    try {
      const page = await listPurchaseRequisitions({ page: 1, pageSize: 50, status: 'StockCheckPending', sortBy: 'prnumber', sortDirection: 'asc' })
      setQueue(page.Items)
      setQueueTotal(page.TotalCount)
    } catch (err) {
      setQueue([])
      setQueueTotal(0)
      setQueueError(err)
    }
  }, [])

  useEffect(() => {
    void loadQueue()
  }, [loadQueue])

  const loadBins = useCallback(async (warehouseCode: string) => {
    if (!warehouseCode || bins[warehouseCode]) return
    try {
      const page = await listRackBins(warehouseCode)
      setBins((prev) => ({ ...prev, [warehouseCode]: page.Items.filter((bin) => bin.IsActive) }))
    } catch {
      // No masters.rack-bins permission: fall back to typing the bin code.
      setBinsUnavailable(true)
    }
  }, [bins])

  useEffect(() => {
    if (!prNumber) { setDetail(null); return }
    let cancelled = false
    setLoading(true)
    setError(null)
    setNotice('')
    getPurchaseRequisition(prNumber)
      .then((loaded) => {
        if (cancelled) return
        setDetail(loaded)
        setLocations(loaded.Lines.map((line) => ({ lineNumber: line.LineNumber, warehouseCode: loaded.DeliveryWarehouseCode, rackBinCode: '' })))
        void loadBins(loaded.DeliveryWarehouseCode)
      })
      .catch((err) => { if (!cancelled) { setDetail(null); setError(err) } })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [prNumber])

  const setLocation = (index: number, patch: Partial<LineLocation>) => {
    setLocations((prev) => prev.map((row, i) => (i === index ? { ...row, ...patch } : row)))
    if (patch.warehouseCode) void loadBins(patch.warehouseCode)
  }

  const submit = async () => {
    if (!detail) return
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
    setBusy(true)
    try {
      const result = await stockCheckPurchaseRequisition(detail.PrNumber, {
        Remarks: remarks.trim(),
        Version: detail.Version,
        IdempotencyKey: newIdempotencyKey('stock-check'),
        Locations: locations.map((row) => ({
          LineNumber: row.lineNumber,
          WarehouseCode: row.warehouseCode.trim().toUpperCase(),
          RackBinCode: row.rackBinCode.trim().toUpperCase(),
        })),
      })
      // The POST answers with the check record only; the PR itself (status,
      // reserved and shortage per line) is re-read so the outcome is exact.
      const updated = await getPurchaseRequisition(detail.PrNumber)
      setDetail(updated)
      setNotice(`${result.CheckNumber} (${result.ResultStatus}). ${describeOutcome(updated)}`)
      setRemarks('')
      void loadQueue()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const checked = detail !== null && detail.Status !== 'StockCheckPending'
  // POST /purchase/requisitions/{prNumber}/stock-check → stores.stock-check:verify
  // (PageStockCheck in PurchaseRequisitionEndpoints.cs). No extra service role check;
  // stores.stock-check is not an explicit-grant page, so full-control covers it.
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
        {detail && (
          <div className="action-row">
            <StatusBadge value={detail.Status} />
            <Link to={`/purchase/requisitions/${encodeURIComponent(detail.PrNumber)}`} className="btn btn-ghost">Open PR</Link>
          </div>
        )}
      </div>

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
        <>
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

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>#</th><th>Item</th><th>UOM</th><th className="text-right">Requested</th>
                  <th className="text-right">On hand</th><th className="text-right">Available</th>
                  <th>Warehouse *</th><th>Rack / Bin *</th><th>Line status</th>
                </tr>
              </thead>
              <tbody>
                {detail.Lines.map((line, index) => {
                  const location = locations[index]
                  const options = location ? bins[location.warehouseCode] ?? [] : []
                  return (
                    <tr key={line.Id}>
                      <td className="mono">{line.LineNumber}</td>
                      <td><span className="mono">{line.ItemCode}</span> — {line.ItemName}</td>
                      <td className="mono">{line.Uom}</td>
                      <td className="text-right mono">{line.RequestedQuantity}</td>
                      <td className="text-right mono">{line.OnHand}</td>
                      <td className="text-right mono">{line.Available}</td>
                      <td>
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
                  <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void submit()}>
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
        </>
      )}
    </div>
  )
}

function describeOutcome(pr: PurchaseRequisitionDetail): string {
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
