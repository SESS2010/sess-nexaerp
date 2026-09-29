import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import {
  createItem, createItemCategory, createItemSubcategory, createUom, getItemVendors,
  listItemCategories, listItemSubcategories, listUoms, setItemVendors, updateItem, uploadItemImage,
} from '../../api/items'
import { listVendors } from '../../api/vendors'
import { AddableSelect } from '../../components/AddableSelect'
import type { AddableOption } from '../../components/AddableSelect'
import type { ItemDetail, ReferenceLookup, SubcategoryLookup, UpsertItemRequest } from '../../types/item'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { ErrorAlert } from '../../components/ErrorAlert'

// Page keys for the quick-add masters behind the dropdowns; they are not in
// PAGE_KEYS. Backend constants: ReferenceMasterEndpoints.CategoriesPage,
// .SubcategoriesPage and .UomsPage.
const CATEGORIES_PAGE = 'masters.item-categories'
const SUBCATEGORIES_PAGE = 'masters.item-subcategories'
const UOMS_PAGE = 'masters.uoms'

interface Props {
  mode: 'create' | 'edit'
  existing?: ItemDetail
  onClose: () => void
  onSaved: (itemCode: string) => void
}

const ITEM_TYPES = ['RAW_MATERIAL', 'COMPONENT', 'CONSUMABLE', 'SPARE', 'FINISHED_MACHINE', 'TOOL', 'SERVICE_ITEM', 'NON_STOCK']

export function ItemFormModal({ mode, existing, onClose, onSaved }: Props) {
  const { can } = useSession()
  const [categories, setCategories] = useState<ReferenceLookup[]>([])
  const [subcategories, setSubcategories] = useState<SubcategoryLookup[]>([])
  const [uoms, setUoms] = useState<ReferenceLookup[]>([])
  const [vendorOptions, setVendorOptions] = useState<{ VendorCode: string; Name: string }[]>([])
  const [vendorFilter, setVendorFilter] = useState('')
  const [selectedVendors, setSelectedVendors] = useState<Set<string>>(new Set())
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<unknown>(null)

  const [form, setForm] = useState({
    itemCode: existing?.ItemCode ?? '',
    name: existing?.Name ?? '',
    categoryId: existing?.CategoryId ?? '',
    subcategoryId: existing?.SubcategoryId ?? '',
    materialType: existing?.MaterialType ?? '',
    itemType: existing?.ItemType ?? 'SPARE',
    uom: existing?.Uom ?? '',
    make: existing?.ManufacturerMake ?? '',
    model: existing?.Model ?? '',
    partNumber: existing?.PartNumber ?? '',
    hsn: existing?.HsnSacCode ?? '',
    gst: existing?.GstPercentage?.toString() ?? '18',
    barcode: existing?.Barcode ?? '',
    minStock: existing?.MinimumStock?.toString() ?? '0',
    maxStock: existing?.MaximumStock?.toString() ?? '0',
    reorder: existing?.ReorderLevel?.toString() ?? '0',
    price: existing?.StandardEstimatedPrice?.toString() ?? '',
    preferredVendor: existing?.PreferredVendorCode ?? '',
  })

  useEffect(() => {
    listItemCategories().then((d) => setCategories(d.Items)).catch(() => setError('Failed to load categories.'))
    listUoms().then((d) => setUoms(d.Items)).catch(() => undefined)
    listVendors({ page: 1, pageSize: 200 })
      .then((d) => setVendorOptions(d.Items.filter((v) => v.IsActive).map((v) => ({ VendorCode: v.VendorCode, Name: v.Name }))))
      .catch(() => undefined)
    if (mode === 'edit' && existing) {
      getItemVendors(existing.ItemCode)
        .then((links) => setSelectedVendors(new Set(links.map((l) => l.VendorCode))))
        .catch(() => undefined)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    if (form.categoryId) {
      listItemSubcategories(form.categoryId).then((d) => setSubcategories(d.Items)).catch(() => setSubcategories([]))
    } else {
      setSubcategories([])
    }
  }, [form.categoryId])

  // masters.items:update also covers the vendor-link PUT; the image POST needs
  // masters.items:upload-attachment.
  const canSave = can(PAGE_KEYS.items, mode === 'create' ? 'create' : 'update')
  const canLinkVendors = can(PAGE_KEYS.items, 'update')
  const canUploadImage = can(PAGE_KEYS.items, 'upload-attachment')

  const set = (key: keyof typeof form) => (event: { target: { value: string } }) =>
    setForm((prev) => ({ ...prev, [key]: event.target.value }))

  const filteredVendors = useMemo(() => {
    const term = vendorFilter.trim().toUpperCase()
    return term
      ? vendorOptions.filter((v) => v.VendorCode.includes(term) || v.Name.toUpperCase().includes(term))
      : vendorOptions
  }, [vendorOptions, vendorFilter])

  const toggleVendor = (code: string) => {
    setSelectedVendors((prev) => {
      const next = new Set(prev)
      if (next.has(code)) next.delete(code)
      else next.add(code)
      return next
    })
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setSaving(true)
    setError(null)
    const optional = (value: string) => (value.trim() ? value.trim() : null)
    try {
      const body: UpsertItemRequest = {
        ItemCode: form.itemCode.trim().toUpperCase(),
        Name: form.name.trim(),
        DetailedDescription: form.name.trim(),
        CategoryId: form.categoryId,
        SubcategoryId: form.subcategoryId || null,
        MaterialType: form.materialType.trim() || (categories.find((c) => c.Id === form.categoryId)?.Name ?? ''),
        ItemType: form.itemType,
        IsReturnable: existing?.IsReturnable ?? false,
        Uom: form.uom,
        ManufacturerMake: optional(form.make),
        Model: optional(form.model),
        PartNumber: optional(form.partNumber),
        HsnSacCode: optional(form.hsn),
        GstPercentage: Number(form.gst) || 0,
        TechnicalSpecification: existing?.TechnicalSpecification ?? null,
        DrawingDocumentReference: existing?.DrawingDocumentReference ?? null,
        QcRequired: existing?.QcRequired ?? false,
        SerialNumberTracking: existing?.SerialNumberTracking ?? false,
        BatchTracking: existing?.BatchTracking ?? false,
        ShelfLifeTracking: existing?.ShelfLifeTracking ?? false,
        MinimumStock: Number(form.minStock) || 0,
        MaximumStock: Number(form.maxStock) || 0,
        ReorderLevel: Number(form.reorder) || 0,
        PreferredVendorCode: optional(form.preferredVendor),
        StandardEstimatedPrice: form.price.trim() ? Number(form.price) : null,
        Barcode: optional(form.barcode),
        BarcodeSymbology: existing?.BarcodeSymbology ?? null,
        ImageStorageKey: existing?.ImageStorageKey ?? null,
        ImageFileName: existing?.ImageFileName ?? null,
        ImageContentType: existing?.ImageContentType ?? null,
        Version: mode === 'edit' ? existing!.Version : null,
      }
      const detail = mode === 'create' ? await createItem(body) : await updateItem(existing!.ItemCode, body)

      if (canLinkVendors) {
        await setItemVendors(detail.ItemCode, [...selectedVendors])
      }
      if (imageFile && canUploadImage) {
        await uploadItemImage(detail.ItemCode, imageFile)
      }
      onSaved(detail.ItemCode)
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{mode === 'create' ? 'New Item' : `Edit ${existing?.ItemCode}`}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>
        <form onSubmit={submit} className="form-grid">
          <label className="field">
            <span className="field-label">Item code *</span>
            <input className="input mono" required value={form.itemCode} onChange={set('itemCode')} placeholder="ITM-XX-000" disabled={mode === 'edit'} />
          </label>
          <label className="field">
            <span className="field-label">Item name *</span>
            <input className="input" required value={form.name} onChange={set('name')} />
          </label>
          <LookupSelect
            canCreate={can(CATEGORIES_PAGE, 'create')}
            label="Category"
            required
            value={form.categoryId}
            options={categories.map((cat) => ({ value: cat.Id, label: cat.Name }))}
            placeholder="Select category…"
            onChange={(categoryId) => setForm((prev) => ({ ...prev, categoryId, subcategoryId: '' }))}
            onCreate={async (name, code) => {
              const created = await createItemCategory(code, name)
              setCategories((prev) => [...prev, created].sort((a, b) => a.Name.localeCompare(b.Name)))
              return { value: created.Id, label: created.Name }
            }}
          />
          <LookupSelect
            canCreate={can(SUBCATEGORIES_PAGE, 'create')}
            label="Subcategory"
            value={form.subcategoryId}
            disabled={!form.categoryId}
            options={subcategories.map((sub) => ({ value: sub.Id, label: sub.Name }))}
            placeholder={subcategories.length === 0 ? 'None for category' : 'Select subcategory…'}
            onChange={(subcategoryId) => setForm((prev) => ({ ...prev, subcategoryId }))}
            addHint="Added under the selected category."
            onCreate={async (name, code) => {
              const created = await createItemSubcategory(form.categoryId, code, name)
              setSubcategories((prev) => [...prev, created].sort((a, b) => a.Name.localeCompare(b.Name)))
              return { value: created.Id, label: created.Name }
            }}
          />
          <label className="field">
            <span className="field-label">Item type *</span>
            <select className="input" value={form.itemType} onChange={set('itemType')}>
              {ITEM_TYPES.map((type) => <option key={type}>{type}</option>)}
            </select>
          </label>
          <LookupSelect
            canCreate={can(UOMS_PAGE, 'create')}
            label="UOM"
            required
            value={form.uom}
            options={uoms.map((u) => ({ value: u.Code, label: `${u.Name} (${u.Code})` }))}
            placeholder="Select UOM…"
            onChange={(uom) => setForm((prev) => ({ ...prev, uom }))}
            extraField={{ label: 'Dimension', options: ['COUNT', 'LENGTH', 'MASS', 'VOLUME', 'AREA', 'TIME'] }}
            onCreate={async (name, code, dimension) => {
              const created = await createUom(code, name, dimension)
              setUoms((prev) => [...prev, created].sort((a, b) => a.Name.localeCompare(b.Name)))
              return { value: created.Code, label: `${created.Name} (${created.Code})` }
            }}
          />
          <label className="field">
            <span className="field-label">Make / manufacturer</span>
            <input className="input" value={form.make} onChange={set('make')} />
          </label>
          <label className="field">
            <span className="field-label">Part number</span>
            <input className="input mono" value={form.partNumber} onChange={set('partNumber')} />
          </label>
          <label className="field">
            <span className="field-label">Model</span>
            <input className="input" value={form.model} onChange={set('model')} />
          </label>
          <label className="field">
            <span className="field-label">HSN/SAC code</span>
            <input className="input mono" value={form.hsn} onChange={set('hsn')} />
          </label>
          <label className="field">
            <span className="field-label">GST %</span>
            <input className="input" type="number" min="0" max="100" step="0.01" value={form.gst} onChange={set('gst')} />
          </label>
          <label className="field">
            <span className="field-label">Barcode</span>
            <input className="input mono" value={form.barcode} onChange={set('barcode')} />
          </label>
          <label className="field">
            <span className="field-label">Minimum stock</span>
            <input className="input" type="number" min="0" value={form.minStock} onChange={set('minStock')} />
          </label>
          <label className="field">
            <span className="field-label">Maximum stock</span>
            <input className="input" type="number" min="0" value={form.maxStock} onChange={set('maxStock')} />
          </label>
          <label className="field">
            <span className="field-label">Reorder level</span>
            <input className="input" type="number" min="0" value={form.reorder} onChange={set('reorder')} />
          </label>
          <label className="field">
            <span className="field-label">Estimated price</span>
            <input className="input" type="number" min="0" step="0.01" value={form.price} onChange={set('price')} />
          </label>
          {canUploadImage && (
            <label className="field">
              <span className="field-label">Item image (JPEG/PNG/WebP, max 5 MB)</span>
              <input
                className="input"
                type="file"
                accept="image/jpeg,image/png,image/webp"
                onChange={(event) => setImageFile(event.target.files?.[0] ?? null)}
              />
              {!imageFile && existing?.ImageFileName && <span className="field-hint">Current: {existing.ImageFileName}</span>}
            </label>
          )}
          <label className="field">
            <span className="field-label">Preferred vendor</span>
            <select className="input" value={form.preferredVendor} onChange={set('preferredVendor')}>
              <option value="">None</option>
              {[...selectedVendors].sort().map((code) => <option key={code} value={code}>{code}</option>)}
            </select>
            <span className="field-hint">Choose from the selected vendors below.</span>
          </label>

          {canLinkVendors && (
            <>
              <div className="field-wide form-section-title">Vendors supplying this item ({selectedVendors.size} selected)</div>
              <div className="field-wide">
                <input
                  className="input mb-2 w-full"
                  placeholder="Filter vendors…"
                  value={vendorFilter}
                  onChange={(event) => setVendorFilter(event.target.value)}
                />
                <div className="max-h-44 overflow-y-auto rounded-lg border border-line bg-white p-2">
                  {filteredVendors.length === 0 && <div className="p-2 text-[13px] text-ink-faint">No vendors match.</div>}
                  {filteredVendors.map((vendor) => (
                    <label key={vendor.VendorCode} className="flex cursor-pointer items-center gap-2.5 rounded-md px-2 py-1.5 hover:bg-accent-soft">
                      <input
                        type="checkbox"
                        className="size-4 accent-blue-700"
                        checked={selectedVendors.has(vendor.VendorCode)}
                        onChange={() => toggleVendor(vendor.VendorCode)}
                      />
                      <span className="mono text-[12.5px]">{vendor.VendorCode}</span>
                      <span className="text-[13px]">{vendor.Name}</span>
                    </label>
                  ))}
                </div>
              </div>
            </>
          )}

          <ErrorAlert error={error} className="field-wide" fallback="Could not save the item." />
          <div className="modal-actions field-wide">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            {canSave && (
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Saving…' : mode === 'create' ? 'Create item' : 'Save changes'}
              </button>
            )}
          </div>
        </form>
      </div>
    </div>
  )
}

interface LookupSelectProps {
  canCreate: boolean
  label: string
  required?: boolean
  disabled?: boolean
  value: string
  options: AddableOption[]
  placeholder?: string
  onChange: (value: string) => void
  onCreate: (name: string, code: string, extra: string) => Promise<AddableOption>
  extraField?: { label: string; options: string[] }
  addHint?: string
}

/**
 * AddableSelect when the caller may create the backing master record, otherwise
 * a plain dropdown — the quick-add "+" is hidden rather than disabled.
 */
function LookupSelect({ canCreate, ...props }: LookupSelectProps) {
  if (canCreate) return <AddableSelect {...props} />
  const { label, required, disabled, value, options, placeholder, onChange } = props
  return (
    <label className="field">
      <span className="field-label">{label}{required ? ' *' : ''}</span>
      <select
        className="input"
        required={required}
        disabled={disabled}
        value={value}
        onChange={(event) => onChange(event.target.value)}
      >
        <option value="">{placeholder ?? 'Select…'}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>{option.label}</option>
        ))}
      </select>
    </label>
  )
}
