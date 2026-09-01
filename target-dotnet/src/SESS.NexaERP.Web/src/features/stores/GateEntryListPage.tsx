import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listGateEntries } from '../../api/stores'
import type { GateEntryResult } from '../../types/stores'
import { GATE_ENTRY_STATES } from '../../types/stores'
import { StatusBadge } from '../employees/StatusBadge'
import { formatAmount } from '../purchase/PurchaseRequisitionListPage'
import { GateEntryFormModal } from './GateEntryFormModal'

const PAGE_SIZE = 25

export function GateEntryListPage() {
  const navigate = useNavigate()
  const [rows, setRows] = useState<GateEntryResult[]>([])
  const [page, setPage] = useState(1)
  const [poNumber, setPoNumber] = useState('')
  const [appliedPo, setAppliedPo] = useState('')
  const [state, setState] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [showCreate, setShowCreate] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await listGateEntries({
        page,
        pageSize: PAGE_SIZE,
        purchaseOrderNumber: appliedPo || undefined,
        state: state || undefined,
        from: from || undefined,
        to: to || undefined,
      })
      setRows(data.Items ?? [])
    } catch (err) {
      setRows([])
      setError(err instanceof Error ? err.message : 'Failed to load gate entries.')
    } finally {
      setLoading(false)
    }
  }, [page, appliedPo, state, from, to])

  useEffect(() => {
    void load()
  }, [load])

  // The API returns no total count, so "next" is only offered on a full page.
  const maybeMore = rows.length === PAGE_SIZE

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Gate Entry</h1>
          <p className="page-sub">
            Material arrival at the gate against an issued purchase order — proof of arrival only, not of acceptance
          </p>
        </div>
        <div className="action-row">
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Gate Entry
          </button>
        </div>
      </div>

      <div className="toolbar">
        <input
          className="input search"
          placeholder="Filter by PO number…"
          value={poNumber}
          onChange={(event) => setPoNumber(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') { setPage(1); setAppliedPo(poNumber.trim().toUpperCase()) }
          }}
        />
        <button
          type="button"
          className="btn btn-ghost"
          onClick={() => { setPage(1); setAppliedPo(poNumber.trim().toUpperCase()) }}
        >Search</button>
        <select className="input" value={state} onChange={(event) => { setState(event.target.value); setPage(1) }}>
          <option value="">All states</option>
          {GATE_ENTRY_STATES.map((option) => <option key={option} value={option}>{option}</option>)}
        </select>
        <input type="date" className="input" value={from} onChange={(event) => { setFrom(event.target.value); setPage(1) }} />
        <input type="date" className="input" value={to} onChange={(event) => { setTo(event.target.value); setPage(1) }} />
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page}</span>
          <button type="button" className="btn btn-ghost" disabled={!maybeMore || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Gate Entry</th>
              <th>PO number</th>
              <th>Vendor</th>
              <th>Vendor DC</th>
              <th>Vehicle</th>
              <th>Transport</th>
              <th>Arrived at</th>
              <th className="text-right">Lines</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={9} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={9} className="table-empty">No gate entries found.</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr key={row.Id} className="row-click" onClick={() => navigate(`/stores/gate-entries/${row.Id}`)}>
                <td className="mono">{row.GateEntryNumber}</td>
                <td className="mono">{row.PurchaseOrderNumber}</td>
                <td>{row.VendorName}</td>
                <td className="mono">{row.VendorDcNumber}</td>
                <td className="mono">{row.VehicleNumber ?? '—'}</td>
                <td>{row.ModeOfTransport}</td>
                <td>{new Date(row.ArrivedAt).toLocaleString('en-IN')}</td>
                <td className="text-right mono">{formatAmount(row.Lines?.length ?? 0)}</td>
                <td><StatusBadge value={row.Status} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <GateEntryFormModal
          mode="create"
          onClose={() => setShowCreate(false)}
          onSaved={(result) => {
            setShowCreate(false)
            navigate(`/stores/gate-entries/${result.Id}`)
          }}
        />
      )}
    </div>
  )
}
