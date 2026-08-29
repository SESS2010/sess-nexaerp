import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { fetchItemImageUrl, getItem, getItemVendors, runItemAction } from '../../api/items'
import type { ItemAction } from '../../api/items'
import type { ItemDetail, ItemVendorLink } from '../../types/item'
import { StatusBadge } from '../employees/StatusBadge'
import { ItemFormModal } from './ItemFormModal'

const ACTIONS: { action: ItemAction; label: string; from: string[] }[] = [
  { action: 'submit', label: 'Submit', from: ['Draft'] },
  { action: 'approve', label: 'Approve', from: ['Pending Approval', 'Clarification Requested'] },
  { action: 'reject', label: 'Reject', from: ['Pending Approval', 'Clarification Requested'] },
  { action: 'request-revision', label: 'Request revision', from: ['Pending Approval', 'Approved'] },
  { action: 'resubmit', label: 'Resubmit', from: ['Revision Requested', 'Rejected'] },
  { action: 'hold', label: 'Hold', from: ['Approved'] },
  { action: 'reactivate', label: 'Reactivate', from: ['Approved'] },
  { action: 'deactivate', label: 'Deactivate', from: ['Approved'] },
]

export function ItemDetailPage() {
  const { itemCode = '' } = useParams()
  const [detail, setDetail] = useState<ItemDetail | null>(null)
  const [vendors, setVendors] = useState<ItemVendorLink[]>([])
  const [imageUrl, setImageUrl] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)

  const load = useCallback(async () => {
    setError('')
    try {
      const item = await getItem(itemCode)
      setDetail(item)
      setVendors(await getItemVendors(itemCode).catch(() => []))
      if (item.ImageStorageKey) {
        setImageUrl(await fetchItemImageUrl(itemCode))
      } else {
        setImageUrl(null)
      }
    } catch (err) {
      setDetail(null)
      setError(err instanceof Error ? err.message : 'Failed to load item.')
    }
  }, [itemCode])

  useEffect(() => {
    void load()
  }, [load])

  const runAction = async (action: ItemAction, label: string) => {
    if (!detail) return
    const remarks = window.prompt(`${label} — enter remarks (required):`)
    if (!remarks || !remarks.trim()) return
    setBusy(true)
    try {
      await runItemAction(itemCode, action, remarks.trim(), detail.Version)
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="breadcrumbs">
            <Link to="/items">Items</Link> / <span className="mono">{itemCode}</span>
          </div>
          <h1>{detail ? detail.Name : itemCode}</h1>
          {detail && (
            <p className="page-sub">
              <StatusBadge value={detail.Status} /> <StatusBadge value={detail.ApprovalStatus} />{' '}
              {detail.ItemType} · v{detail.Version}
            </p>
          )}
        </div>
        {detail && (
          <div className="action-row">
            <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setEditing(true)}>Edit</button>
            {ACTIONS.filter((item) => item.from.includes(detail.ApprovalStatus)).map((item) => (
              <button key={item.action} type="button" className="btn btn-ghost" disabled={busy} onClick={() => runAction(item.action, item.label)}>
                {item.label}
              </button>
            ))}
          </div>
        )}
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {detail && (
        <div className="flex flex-col gap-5 lg:flex-row">
          <div className="card flex h-fit w-full shrink-0 flex-col items-center gap-3 lg:w-64">
            {imageUrl ? (
              <img src={imageUrl} alt={detail.Name} className="max-h-52 w-full rounded-lg object-contain" />
            ) : (
              <div className="grid h-40 w-full place-items-center rounded-lg bg-slate-100 text-[13px] text-ink-faint">
                No image
              </div>
            )}
            {detail.ImageFileName && <span className="field-hint">{detail.ImageFileName}</span>}
          </div>

          <div className="min-w-0 flex-1">
            <div className="card detail-grid mb-5">
              <Field label="Item code" value={detail.ItemCode} mono />
              <Field label="Name" value={detail.Name} />
              <Field label="Category" value={detail.CategoryName ?? '—'} />
              <Field label="Subcategory" value={detail.SubcategoryName ?? '—'} />
              <Field label="Material type" value={detail.MaterialType || '—'} />
              <Field label="UOM" value={detail.Uom} />
              <Field label="Make" value={detail.ManufacturerMake ?? '—'} />
              <Field label="Model" value={detail.Model ?? '—'} />
              <Field label="Part number" value={detail.PartNumber ?? '—'} mono />
              <Field label="HSN/SAC" value={detail.HsnSacCode ?? '—'} mono />
              <Field label="GST %" value={String(detail.GstPercentage)} mono />
              <Field label="Barcode" value={detail.Barcode ?? '—'} mono />
              <Field label="Min / Max stock" value={`${detail.MinimumStock} / ${detail.MaximumStock}`} mono />
              <Field label="Reorder level" value={String(detail.ReorderLevel)} mono />
              <Field label="Estimated price" value={detail.StandardEstimatedPrice != null ? String(detail.StandardEstimatedPrice) : '—'} mono />
              <Field label="Preferred vendor" value={detail.PreferredVendorCode ?? '—'} mono />
            </div>

            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr><th colSpan={3}>Vendors supplying this item ({vendors.length})</th></tr>
                </thead>
                <tbody>
                  {vendors.length === 0 && <tr><td colSpan={3} className="table-empty">No vendors linked yet — use Edit to select vendors.</td></tr>}
                  {vendors.map((vendor) => (
                    <tr key={vendor.VendorCode}>
                      <td className="mono"><Link to={`/vendors/${encodeURIComponent(vendor.VendorCode)}`} className="text-accent hover:underline">{vendor.VendorCode}</Link></td>
                      <td>{vendor.Name}</td>
                      <td><StatusBadge value={vendor.VendorStatus} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {editing && detail && (
        <ItemFormModal
          mode="edit"
          existing={detail}
          onClose={() => setEditing(false)}
          onSaved={() => { setEditing(false); void load() }}
        />
      )}
    </div>
  )
}

function Field({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="detail-field">
      <div className="field-label">{label}</div>
      <div className={mono ? 'mono' : undefined}>{value}</div>
    </div>
  )
}
