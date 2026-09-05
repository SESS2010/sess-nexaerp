import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listGoodsReceipts } from '../../api/goodsReceipts'
import type { GoodsReceiptResult } from '../../types/goodsReceipt'
import { GOODS_RECEIPT_STATES } from '../../types/goodsReceipt'
import { StatusBadge } from '../employees/StatusBadge'
import { GoodsReceiptFormModal } from './GoodsReceiptFormModal'
import { ErrorAlert } from '../../components/ErrorAlert'
import { SortableHeader } from '../../components/SortableHeader'
import { useSort } from '../../hooks/useSort'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

const PAGE_SIZE = 25

export function GoodsReceiptListPage() {
  const navigate = useNavigate()
  const { can, hasRole } = useSession()
  const [rows, setRows] = useState<GoodsReceiptResult[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  // Newest receipt first — the register is read as a running arrival log. The
  // endpoint does not honour sortBy yet; the keys are the lowercase row-DTO
  // field names the backend will accept once it does.
  const { sort, toggleSort } = useSort({ sortBy: 'receivedat', sortDirection: 'desc' }, () => setPage(1))
  const [search, setSearch] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [showCreate, setShowCreate] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      // One search box serves both document numbers: GRN-… hits grnNumber,
      // anything else is tried as a Gate Entry number.
      const term = appliedSearch.toUpperCase()
      const data = await listGoodsReceipts({
        page,
        pageSize: PAGE_SIZE,
        grnNumber: term.startsWith('GRN') ? term : undefined,
        gateEntryNumber: term && !term.startsWith('GRN') ? term : undefined,
        status: status || undefined,
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
  }, [page, appliedSearch, status, sort])

  useEffect(() => {
    void load()
  }, [load])

  const applySearch = () => {
    setPage(1)
    setAppliedSearch(search.trim())
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  // POST /stores/goods-receipts → inventory.grn:create (explicit-grant page, so
  // full-control does not cover it) plus EfGoodsReceiptService.ActorRole(),
  // which accepts only STORES_EXECUTIVE / STORES_ASSISTANT.
  const canCreate =
    can(PAGE_KEYS.grn, 'create') && (hasRole('STORES_EXECUTIVE') || hasRole('STORES_ASSISTANT'))

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>GRN</h1>
          <p className="page-sub">
            Goods receipt against a finalized Gate Entry — vendor bill, lots and serials; stock moves to QC hold on finalize
          </p>
        </div>
        <div className="action-row">
          {canCreate && (
            <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
              + New GRN
            </button>
          )}
        </div>
      </div>

      <div className="toolbar">
        <input
          className="input search"
          placeholder="GRN or Gate Entry number…"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onKeyDown={(event) => event.key === 'Enter' && applySearch()}
        />
        <button type="button" className="btn btn-ghost" onClick={applySearch}>Search</button>
        <select className="input" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="">All states</option>
          {GOODS_RECEIPT_STATES.map((option) => <option key={option} value={option}>{option}</option>)}
        </select>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages} · {totalCount} total</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load goods receipts." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <SortableHeader label="GRN" sortKey="grnnumber" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Gate Entry" sortKey="gateentrynumber" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="PO number" sortKey="purchaseordernumber" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Vendor" sortKey="vendorname" sort={sort} onSort={toggleSort} disabled={loading} />
              <th>Bill number</th>
              <SortableHeader label="Bill date" sortKey="vendorbilldate" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Received at" sortKey="receivedat" sort={sort} onSort={toggleSort} disabled={loading} />
              <th className="text-right">Lines</th>
              <th>Kind</th>
              <SortableHeader label="Status" sortKey="status" sort={sort} onSort={toggleSort} disabled={loading} />
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={10} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan={10} className="table-empty">No goods receipts found.</td></tr>
            )}
            {!loading && rows.map((row) => (
              <tr key={row.Id} className="row-click" onClick={() => navigate(`/stores/goods-receipts/${row.Id}`)}>
                <td className="mono">{row.GrnNumber}</td>
                <td className="mono">{row.GateEntryNumber}</td>
                <td className="mono">{row.PurchaseOrderNumber}</td>
                <td>{row.VendorName}</td>
                <td className="mono">{row.VendorBillNumber}</td>
                <td>{row.VendorBillDate}</td>
                <td>{new Date(row.ReceivedAt).toLocaleString('en-IN')}</td>
                <td className="text-right mono">{row.Lines?.length ?? 0}</td>
                <td>{row.DocumentKind === 'NORMAL' ? '—' : <StatusBadge value={row.DocumentKind} />}</td>
                <td><StatusBadge value={row.Status} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <GoodsReceiptFormModal
          mode="create"
          onClose={() => setShowCreate(false)}
          onSaved={(result) => {
            setShowCreate(false)
            navigate(`/stores/goods-receipts/${result.Id}`)
          }}
        />
      )}
    </div>
  )
}
