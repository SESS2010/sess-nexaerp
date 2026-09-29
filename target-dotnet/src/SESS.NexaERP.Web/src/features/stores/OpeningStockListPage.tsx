import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { listOpeningStock, recordOpeningStockCount, type OpeningStockView } from '../../api/openingStock'
import { newIdempotencyKey } from '../../api/stores'
import type { ImportResult } from '../../api/masterdata'
import { ImportExportBar } from '../../components/ImportExportBar'
import { ErrorAlert } from '../../components/ErrorAlert'
import { StatusBadge } from '../employees/StatusBadge'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'

const money = new Intl.NumberFormat('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

/**
 * Opening stock ceremonies for the signed-in company. Stage 1 (COUNT) starts
 * here: the Stores Manager uploads the opening-stock workbook (template →
 * fill → upload → errors.xlsx → re-upload), then records the count against
 * that import batch for a period. Stages 2 and 3 live on the detail page.
 *
 * The server refuses the count outright once the company has any stock
 * movement, so this is a one-time, per-company ceremony.
 */
export function OpeningStockListPage() {
  const { can, me } = useSession()
  const navigate = useNavigate()
  const canCount = can(PAGE_KEYS.openingStock, 'create')

  const [rows, setRows] = useState<OpeningStockView[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [status, setStatus] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setRows((await listOpeningStock(status || null)).Items)
    } catch (err) {
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [status])

  useEffect(() => {
    void load()
  }, [load])

  // ---- stage 1: count from import ----
  const [lastImport, setLastImport] = useState<ImportResult | null>(null)
  const [batchId, setBatchId] = useState('')
  const [periodStart, setPeriodStart] = useState('')
  const [periodEnd, setPeriodEnd] = useState('')
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [countError, setCountError] = useState<unknown>(null)
  const [keyState, setKeyState] = useState<{ fingerprint: string; key: string } | null>(null)

  const onImported = (result: ImportResult) => {
    setLastImport(result)
    if (result.RejectedRows === 0 && result.CreatedRows > 0) setBatchId(result.BatchId)
  }

  const submitCount = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setCountError(null)
    const body = { ImportBatchId: batchId.trim(), PeriodStart: periodStart, PeriodEnd: periodEnd, Reason: reason.trim() }
    // One key per distinct request so a retry replays instead of duplicating,
    // while a changed request gets a fresh key (the server refuses a reused
    // key with different content as an idempotency mismatch).
    const fingerprint = JSON.stringify(body)
    const key = keyState?.fingerprint === fingerprint ? keyState.key : newIdempotencyKey('opening-stock-count')
    setKeyState({ fingerprint, key })
    try {
      const created = await recordOpeningStockCount({ ...body, IdempotencyKey: key })
      navigate(`/stores/opening-stock/${created.Id}`)
    } catch (err) {
      setCountError(err)
    } finally {
      setBusy(false)
    }
  }

  const importUsable = lastImport !== null && lastImport.RejectedRows === 0 && lastImport.CreatedRows > 0

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Opening Stock</h1>
          <p className="page-sub">
            One-time opening balance for <span className="mono">{me?.OrganizationId ?? '…'}</span> — counted by Stores, valued by Accounts, authorized by the Technical Director, from one imported workbook
          </p>
        </div>
      </div>

      {/* The manager asked for this to be the loudest thing on the screen: the
          ceremony has no undo, and the company's first stock movement locks it. */}
      <div className="alert alert-warn" role="alert">
        <div className="alert-title" style={{ fontSize: 15 }}>⚠ OPENING STOCK IS DONE ONCE PER COMPANY. THERE IS NO UNDO.</div>
        <div className="alert-body">
          Once the Technical Director authorizes it, the quantities and values below become this company's stock and its first FIFO layers, and the ceremony can never be run again here —
          the server refuses it as soon as the company has a single stock movement. Corrections afterwards are ordinary movements, not a second opening balance.
        </div>
        <div className="alert-detail">
          Count only from a workbook with <strong>Rejected 0</strong>. Check every quantity, bin and rate against the physical count sheet before Record count.
          Practise in a trial company, never in the company going live.
        </div>
      </div>

      {canCount && (
        <div className="card" style={{ marginBottom: 20 }}>
          <h2 className="form-section-title">Stage 1 — Count from an imported workbook</h2>
          <p className="field-hint" style={{ marginBottom: 12 }}>
            Download the template, fill one row per item / rack-bin (serial-tracked items one row per serial, quantity 1), upload it,
            fix anything in errors.xlsx and upload again. The count can only use a batch with zero rejected rows.
          </p>
          <ImportExportBar masterKey="opening-stock" exportable={false} onImported={onImported} />
          {lastImport && !importUsable && (
            <div className="alert alert-warn" style={{ marginTop: 12 }}>
              Batch <span className="mono">{lastImport.BatchId.slice(0, 8)}</span> has {lastImport.RejectedRows} rejected row(s)
              {lastImport.CreatedRows === 0 ? ' and created nothing' : ''}, so the server will refuse it. Download errors.xlsx, correct, and upload again.
            </div>
          )}
          <form onSubmit={submitCount} style={{ marginTop: 16 }}>
            <div className="form-grid">
              <label className="field field-wide">
                <span className="field-label">Import batch id *</span>
                <input className="input mono" value={batchId} onChange={(event) => setBatchId(event.target.value)} placeholder="Filled in after a clean upload, or paste one" required />
                <span className="field-hint">The completed, error-free opening-stock import for this company.</span>
              </label>
              <label className="field">
                <span className="field-label">Period start *</span>
                <input className="input" type="date" value={periodStart} onChange={(event) => setPeriodStart(event.target.value)} required />
              </label>
              <label className="field">
                <span className="field-label">Period end *</span>
                <input className="input" type="date" value={periodEnd} onChange={(event) => setPeriodEnd(event.target.value)} required />
              </label>
              <label className="field field-wide">
                <span className="field-label">Count reason *</span>
                <input className="input" value={reason} onChange={(event) => setReason(event.target.value)} placeholder="e.g. Physical count 31 Mar 2026, go-live opening balance" required />
              </label>
            </div>
            <ErrorAlert error={countError} fallback="The count was refused." className="mt-4" />
            <div className="action-row" style={{ marginTop: 16 }}>
              <button type="submit" className="btn btn-primary" disabled={busy || !batchId.trim()}>Record count</button>
            </div>
          </form>
        </div>
      )}

      <div className="toolbar">
        <select className="input" value={status} onChange={(event) => setStatus(event.target.value)}>
          <option value="">All stages</option>
          <option value="COUNTED">Counted — awaiting Accounts</option>
          <option value="VALUED">Valued — awaiting Technical Director</option>
          <option value="POSTED">Posted</option>
        </select>
        <button type="button" className="btn btn-ghost" disabled={loading} onClick={() => void load()}>Refresh</button>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Opening stock could not be loaded." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Period</th>
              <th>Stage</th>
              <th className="text-right">Lines</th>
              <th className="text-right">Quantity</th>
              <th className="text-right">Value</th>
              <th>Counted</th>
              <th>Valued</th>
              <th>Authorized</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={8} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && <tr><td colSpan={8} className="table-empty">No opening stock ceremony in this company.</td></tr>}
            {!loading && rows.map((row) => (
              <tr key={row.Id}>
                <td><Link to={`/stores/opening-stock/${row.Id}`} className="mono">{row.PeriodStart} → {row.PeriodEnd}</Link></td>
                <td><StatusBadge value={row.Status} /></td>
                <td className="text-right mono">{row.Lines.length}</td>
                <td className="text-right mono">{row.TotalQuantity}</td>
                <td className="text-right mono">{money.format(row.TotalValue)}</td>
                <td>{row.CountedBy.EmployeeCode}</td>
                <td>{row.ValuedBy?.EmployeeCode ?? '—'}</td>
                <td>{row.AuthorizedBy?.EmployeeCode ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
