import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listGateEntries } from '../../api/stores'
import type { GateEntryResult } from '../../types/stores'
import { GATE_ENTRY_STATES } from '../../types/stores'
import { StatusBadge } from '../employees/StatusBadge'
import { formatAmount } from '../purchase/PurchaseRequisitionListPage'
import { GateEntryFormModal } from './GateEntryFormModal'
import { ErrorAlert } from '../../components/ErrorAlert'
import { SortableHeader } from '../../components/SortableHeader'
import { useSort } from '../../hooks/useSort'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

/**
 * Server-side sorting for this register is not available yet: the
 * /api/v1/stores/gate-entries list endpoint accepts no sortBy or
 * sortDirection. Set to true once the backend takes them.
 */
const SORTABLE = false

const PAGE_SIZE = 25

export function GateEntryListPage() {
  const navigate = useNavigate()
  const { can, hasRole } = useSession()
  const [rows, setRows] = useState<GateEntryResult[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  // Arrival order is what the register reads as "current" — newest gate entry
  // on top, which is the order the endpoint already returns.
  //
  // GET /api/v1/stores/gate-entries takes no sortBy/sortDirection, so the
  // columns are rendered as plain headers until it does. The keys below are the
  // lowercase row-DTO field names the backend will accept once the parameters
  // are added; flip SORTABLE to true then and nothing else needs to change.
  // The list is paged server-side, so sorting the fetched page in the browser
  // would reorder 50 rows out of a much longer register and read as a bug.
  const { sort, toggleSort } = useSort({ sortBy: 'arrivedat', sortDirection: 'desc' }, () => setPage(1))
  const [gateNumber, setGateNumber] = useState('')
  const [appliedGate, setAppliedGate] = useState('')
  const [poNumber, setPoNumber] = useState('')
  const [appliedPo, setAppliedPo] = useState('')
  const [state, setState] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [showCreate, setShowCreate] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listGateEntries({
        page,
        pageSize: PAGE_SIZE,
        gateEntryNumber: appliedGate || undefined,
        purchaseOrderNumber: appliedPo || undefined,
        state: state || undefined,
        from: from || undefined,
        to: to || undefined,
        sortBy: sort.sortBy,
        sortDirection: sort.sortDirection,
      })
      setRows(data.Items ?? [])
      setTotalCount(data.TotalCount)
    } catch (err) {
      setRows([])
      setTotalCount(0)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [page, appliedGate, appliedPo, state, from, to, sort])

  useEffect(() => {
    void load()
  }, [load])

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  // POST /stores/gate-entries → inventory.grn:create, plus EfGateEntryService.ActorRole()
  // which accepts only STORES_EXECUTIVE / STORES_ASSISTANT. The form also has to read the
  // source PO (purchase.po:view) to list its lines and refuses to save with none, so
  // without that grant the create flow can never complete.
  const canCreate =
    can(PAGE_KEYS.gateEntry, 'create') &&
    (hasRole('STORES_EXECUTIVE') || hasRole('STORES_ASSISTANT')) &&
    can(PAGE_KEYS.purchaseOrders, 'view')

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
          {canCreate && (
            <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
              + New Gate Entry
            </button>
          )}
        </div>
      </div>

      <div className="toolbar">
        <input
          className="input search"
          placeholder="Gate Entry number…"
          value={gateNumber}
          onChange={(event) => setGateNumber(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') { setPage(1); setAppliedGate(gateNumber.trim().toUpperCase()) }
          }}
        />
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
          onClick={() => { setPage(1); setAppliedGate(gateNumber.trim().toUpperCase()); setAppliedPo(poNumber.trim().toUpperCase()) }}
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
          <span className="pager-label">Page {page} of {totalPages} · {totalCount} total</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load gate entries." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <SortableHeader label="Gate Entry" sortKey="gateentrynumber" sort={sort} onSort={toggleSort} disabled={loading} sortable={SORTABLE} />
              <SortableHeader label="PO number" sortKey="purchaseordernumber" sort={sort} onSort={toggleSort} disabled={loading} sortable={SORTABLE} />
              <SortableHeader label="Vendor" sortKey="vendorname" sort={sort} onSort={toggleSort} disabled={loading} sortable={SORTABLE} />
              <th>Vendor DC</th>
              <th>Vehicle</th>
              <th>Transport</th>
              <SortableHeader label="Arrived at" sortKey="arrivedat" sort={sort} onSort={toggleSort} disabled={loading} sortable={SORTABLE} />
              <th className="text-right">Lines</th>
              <SortableHeader label="Status" sortKey="status" sort={sort} onSort={toggleSort} disabled={loading} sortable={SORTABLE} />
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
