import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listGoodsReceipts } from '../../api/goodsReceipts'
import type { GoodsReceiptResult } from '../../types/goodsReceipt'
import { GOODS_RECEIPT_STATES } from '../../types/goodsReceipt'
import { StatusBadge } from '../employees/StatusBadge'
import { GoodsReceiptFormModal } from './GoodsReceiptFormModal'
import { ErrorAlert } from '../../components/ErrorAlert'

const PAGE_SIZE = 25

export function GoodsReceiptListPage() {
  const navigate = useNavigate()
  const [rows, setRows] = useState<GoodsReceiptResult[]>([])
  const [page, setPage] = useState(1)
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
      })
      setRows(data.Items ?? [])
    } catch (err) {
      setRows([])
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [page, appliedSearch, status])

  useEffect(() => {
    void load()
  }, [load])

  const applySearch = () => {
    setPage(1)
    setAppliedSearch(search.trim())
  }

  // The API returns no total count, so "next" is only offered on a full page.
  const maybeMore = rows.length === PAGE_SIZE

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
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New GRN
          </button>
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
          <span className="pager-label">Page {page}</span>
          <button type="button" className="btn btn-ghost" disabled={!maybeMore || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load goods receipts." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>GRN</th>
              <th>Gate Entry</th>
              <th>PO number</th>
              <th>Vendor</th>
              <th>Bill number</th>
              <th>Bill date</th>
              <th>Received at</th>
              <th className="text-right">Lines</th>
              <th>Kind</th>
              <th>Status</th>
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
