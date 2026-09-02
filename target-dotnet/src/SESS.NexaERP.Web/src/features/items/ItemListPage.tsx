import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listItems, listItemCategories } from '../../api/items'
import type { ItemSummary, ReferenceLookup } from '../../types/item'
import { StatusBadge } from '../employees/StatusBadge'
import { ItemFormModal } from './ItemFormModal'
import { ErrorAlert } from '../../components/ErrorAlert'
import { SortableHeader } from '../../components/SortableHeader'
import { useSort } from '../../hooks/useSort'

const PAGE_SIZE = 25

export function ItemListPage() {
  const navigate = useNavigate()
  const [rows, setRows] = useState<ItemSummary[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [category, setCategory] = useState('')
  const [categories, setCategories] = useState<ReferenceLookup[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [showCreate, setShowCreate] = useState(false)
  // The items endpoint sorts on code, name and status only.
  const { sort, toggleSort } = useSort({ sortBy: 'code', sortDirection: 'asc' }, () => setPage(1))

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  useEffect(() => {
    listItemCategories().then((data) => setCategories(data.Items)).catch(() => undefined)
  }, [])

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listItems({
        page, pageSize: PAGE_SIZE, search: appliedSearch, category,
        sortBy: sort.sortBy, sortDirection: sort.sortDirection,
      })
      setRows(data.Items)
      setTotalCount(data.TotalCount)
    } catch (err) {
      setRows([])
      setTotalCount(0)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [page, appliedSearch, category, sort])

  useEffect(() => {
    void load()
  }, [load])

  const applySearch = () => {
    setPage(1)
    setAppliedSearch(search.trim())
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Items</h1>
          <p className="page-sub">Item master with vendor linkage ({totalCount} total)</p>
        </div>
        <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
          + New Item
        </button>
      </div>

      <div className="toolbar">
        <input
          className="input search"
          placeholder="Search code, name, part no or barcode…"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onKeyDown={(event) => event.key === 'Enter' && applySearch()}
        />
        <button type="button" className="btn btn-ghost" onClick={applySearch}>Search</button>
        <select className="input" value={category} onChange={(event) => { setCategory(event.target.value); setPage(1) }}>
          <option value="">All categories</option>
          {categories.map((cat) => <option key={cat.Id} value={cat.Code}>{cat.Name}</option>)}
        </select>
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages}</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      <ErrorAlert error={error} onReload={() => void load()} fallback="Failed to load items." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <SortableHeader label="Code" sortKey="code" sort={sort} onSort={toggleSort} disabled={loading} />
              <SortableHeader label="Name" sortKey="name" sort={sort} onSort={toggleSort} disabled={loading} />
              <th>Category</th>
              <th>UOM</th>
              <th>Make</th>
              <th>Part number</th>
              <SortableHeader label="Status" sortKey="status" sort={sort} onSort={toggleSort} disabled={loading} />
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={7} className="table-empty">Loading…</td></tr>}
            {!loading && rows.length === 0 && !error && <tr><td colSpan={7} className="table-empty">No items found.</td></tr>}
            {!loading && rows.map((row) => (
              <tr key={row.Id} className="row-click" onClick={() => navigate(`/items/${encodeURIComponent(row.ItemCode)}`)}>
                <td className="mono">{row.ItemCode}</td>
                <td>{row.Name}</td>
                <td>{row.CategoryName ?? row.MaterialType}</td>
                <td>{row.Uom}</td>
                <td>{row.ManufacturerMake ?? '—'}</td>
                <td className="mono">{row.PartNumber ?? '—'}</td>
                <td><StatusBadge value={row.Status} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <ItemFormModal
          mode="create"
          onClose={() => setShowCreate(false)}
          onSaved={(itemCode) => {
            setShowCreate(false)
            navigate(`/items/${encodeURIComponent(itemCode)}`)
          }}
        />
      )}
    </div>
  )
}
