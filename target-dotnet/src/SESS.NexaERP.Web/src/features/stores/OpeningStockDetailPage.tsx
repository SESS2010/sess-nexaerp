import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  authorizeOpeningStock,
  confirmOpeningStockValue,
  getOpeningStock,
  type OpeningStockActorView,
  type OpeningStockView,
} from '../../api/openingStock'
import { newIdempotencyKey } from '../../api/stores'
import { ErrorAlert } from '../../components/ErrorAlert'
import { StatusBadge } from '../employees/StatusBadge'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'

const money = new Intl.NumberFormat('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

const STAGES = [
  { status: 'COUNTED', label: 'Counted', role: 'Stores Manager', actor: (v: OpeningStockView) => v.CountedBy },
  { status: 'VALUED', label: 'Valued', role: 'Accounts Manager', actor: (v: OpeningStockView) => v.ValuedBy },
  { status: 'POSTED', label: 'Authorized & posted', role: 'Technical Director', actor: (v: OpeningStockView) => v.AuthorizedBy },
] as const

function Actor({ actor }: { actor: OpeningStockActorView | null }) {
  if (!actor) return <span className="text-ink-faint">— not yet —</span>
  return (
    <span>
      <span className="mono">{actor.EmployeeCode}</span> {actor.EmployeeName} · {actor.RoleCode} ({actor.AssignmentType})
      <div className="text-ink-faint text-[12px]">{new Date(actor.At).toLocaleString()} — {actor.Reason}</div>
    </span>
  )
}

/**
 * One opening stock ceremony. Stage 2 (VALUE, Accounts Manager) and stage 3
 * (AUTHORIZE, Technical Director) are taken here; each sends the ceremony
 * Version so a stale copy is refused, and its own idempotency key. Three
 * different employees are enforced by the server, not just hidden here.
 */
export function OpeningStockDetailPage() {
  const { id = '' } = useParams()
  const { can, me } = useSession()

  const [view, setView] = useState<OpeningStockView | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [actionError, setActionError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  const [reason, setReason] = useState('')
  const [keyState, setKeyState] = useState<{ fingerprint: string; key: string } | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setView(await getOpeningStock(id))
    } catch (err) {
      setView(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  const run = async (action: 'value' | 'authorize') => {
    if (!view) return
    setBusy(true)
    setActionError(null)
    setNotice('')
    const body = { Version: view.Version, Reason: reason.trim() }
    const fingerprint = JSON.stringify({ id, action, body })
    const key = keyState?.fingerprint === fingerprint ? keyState.key : newIdempotencyKey(`opening-stock-${action}`)
    setKeyState({ fingerprint, key })
    try {
      const next = action === 'value'
        ? await confirmOpeningStockValue(id, { ...body, IdempotencyKey: key })
        : await authorizeOpeningStock(id, { ...body, IdempotencyKey: key })
      setView(next)
      setReason('')
      setNotice(next.Replayed
        ? 'This step had already been recorded with the same key; the server replayed it.'
        : action === 'value'
          ? `Value confirmed at ${money.format(next.TotalValue)} — waiting for the Technical Director.`
          : `Authorized and posted: batch ${next.StockPostingBatchId?.slice(0, 8) ?? '?'} created the AVAILABLE stock and FIFO layers.`)
    } catch (err) {
      setActionError(err)
    } finally {
      setBusy(false)
    }
  }

  if (loading) return <div className="page"><p>Loading…</p></div>
  if (!view) {
    return (
      <div className="page">
        <ErrorAlert error={error} onReload={() => void load()} fallback="Opening stock could not be loaded." />
        <Link to="/stores/opening-stock">← Back to Opening Stock</Link>
      </div>
    )
  }

  // Grants decide what is offered; the server still checks the role, the
  // FULL/TEMPORARY assignment and the three-person rule.
  const canValue = view.Status === 'COUNTED' && can(PAGE_KEYS.openingStock, 'verify')
  const canAuthorize = view.Status === 'VALUED' && can(PAGE_KEYS.openingStock, 'approve')
  const isCounter = me?.EmployeeId === view.CountedBy.EmployeeId
  const isValuer = me?.EmployeeId === view.ValuedBy?.EmployeeId
  const stageIndex = STAGES.findIndex((s) => s.status === view.Status)

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="breadcrumbs"><Link to="/stores/opening-stock">Opening Stock</Link> / {view.PeriodStart} → {view.PeriodEnd}</div>
          <h1>Opening stock {view.PeriodStart} → {view.PeriodEnd}</h1>
          <p className="page-sub">
            Import batch <span className="mono">{view.ImportBatchId}</span> · version {view.Version}
            {view.StockPostingBatchId && <> · posting batch <span className="mono">{view.StockPostingBatchId}</span></>}
          </p>
        </div>
        <div className="action-row"><StatusBadge value={view.Status} /></div>
      </div>

      <div className="detail-grid" style={{ marginBottom: 16 }}>
        <div><span className="field-label">Lines</span> <span className="mono">{view.Lines.length}</span></div>
        <div><span className="field-label">Total quantity</span> <span className="mono">{view.TotalQuantity}</span></div>
        <div><span className="field-label">Total value</span> <span className="mono">{money.format(view.TotalValue)}</span></div>
      </div>

      <div className="card" style={{ marginBottom: 20 }}>
        <h2 className="form-section-title">Three-person ceremony</h2>
        <table className="table">
          <tbody>
            {STAGES.map((stage, index) => (
              <tr key={stage.status}>
                <td style={{ width: 28 }} className="mono">{index + 1}</td>
                <td style={{ width: 200 }}>
                  <strong>{stage.label}</strong>
                  <div className="text-ink-faint text-[12px]">{stage.role}</div>
                </td>
                <td>
                  {index <= stageIndex ? <Actor actor={stage.actor(view)} /> : <span className="text-ink-faint">— not yet —</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {(canValue || canAuthorize) && (
          <div style={{ marginTop: 16 }}>
            {(canValue && isCounter) && (
              <div className="alert alert-warn">You recorded the count, so the server will refuse your valuation — a different Accounts Manager must confirm the value.</div>
            )}
            {(canAuthorize && (isCounter || isValuer)) && (
              <div className="alert alert-warn">You already took one of the earlier steps, so the server will refuse your authorization — three separate people are required.</div>
            )}
            <div className="form-grid">
              <label className="field field-wide">
                <span className="field-label">{canValue ? 'Valuation reason *' : 'Authorization reason *'}</span>
                <input className="input" value={reason} onChange={(event) => setReason(event.target.value)}
                  placeholder={canValue ? 'e.g. Rates agreed with audited closing stock statement' : 'e.g. Opening balance authorized for go-live'} />
              </label>
            </div>
            <div className="action-row" style={{ marginTop: 12 }}>
              {canValue && (
                <button type="button" className="btn btn-primary" disabled={busy || !reason.trim()} onClick={() => void run('value')}>
                  Confirm value ({money.format(view.TotalValue)})
                </button>
              )}
              {canAuthorize && (
                <button type="button" className="btn btn-primary" disabled={busy || !reason.trim()} onClick={() => void run('authorize')}>
                  Authorize and post
                </button>
              )}
            </div>
          </div>
        )}
        {notice && <div className="alert" style={{ marginTop: 12 }}>{notice}</div>}
        <ErrorAlert error={actionError} onReload={() => void load()} fallback="The step was refused." className="mt-3" />
      </div>

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>#</th>
              <th>Reference</th>
              <th>Item</th>
              <th>Warehouse / bin</th>
              <th>Lot</th>
              <th>Serial</th>
              <th className="text-right">Quantity</th>
              <th className="text-right">Rate</th>
              <th className="text-right">Value</th>
              <th>Posted</th>
            </tr>
          </thead>
          <tbody>
            {view.Lines.map((line) => (
              <tr key={line.Id}>
                <td className="mono">{line.LineNumber}</td>
                <td className="mono">{line.LineReference}</td>
                <td><span className="mono">{line.ItemCode}</span> — {line.ItemName}</td>
                <td className="mono">{line.WarehouseCode} / {line.RackBinCode}</td>
                <td className="mono">{line.LotNumber ?? '—'}</td>
                <td className="mono">{line.SerialNumber ?? '—'}</td>
                <td className="text-right mono">{line.Quantity}</td>
                <td className="text-right mono">{money.format(line.UnitRate)}</td>
                <td className="text-right mono">{money.format(line.LineValue)}</td>
                <td className="mono text-[12px]">
                  {line.FifoInventoryCostLayerId
                    ? <>layer {line.FifoInventoryCostLayerId.slice(0, 8)}{line.InventorySerialId ? ` · serial ${line.InventorySerialId.slice(0, 8)}` : ''}{line.InventoryLotId ? ` · lot ${line.InventoryLotId.slice(0, 8)}` : ''}</>
                    : <span className="text-ink-faint">—</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <p style={{ marginTop: 16 }}><Link to="/stores/opening-stock">← Back to Opening Stock</Link></p>
    </div>
  )
}
