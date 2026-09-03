import { useEffect, useMemo, useState } from 'react'
import { getStoredIdentity } from '../../api/client'
import {
  createPurchaseRequisition,
  listDepartments,
  listWarehouseOptions,
  searchItems,
  updatePurchaseRequisition,
} from '../../api/purchase'
import type {
  PurchaseLookupOption,
  PurchaseRequisitionDetail,
  PurchaseRequisitionLineRequest,
} from '../../types/purchase'
import { PR_PRIORITIES } from '../../types/purchase'

interface DraftLine {
  itemCode: string
  itemName: string
  quantity: string
  unitPrice: string
  requiredDate: string
  preferredWarehouseCode: string
}

function today(): string {
  return new Date().toISOString().slice(0, 10)
}

function emptyLine(requiredDate: string): DraftLine {
  return {
    itemCode: '',
    itemName: '',
    quantity: '1',
    unitPrice: '',
    requiredDate,
    preferredWarehouseCode: '',
  }
}

function blank(value: string): string | null {
  const trimmed = value.trim()
  return trimmed.length > 0 ? trimmed : null
}

interface Props {
  mode: 'create' | 'edit'
  existing?: PurchaseRequisitionDetail
  onClose: () => void
  onSaved: (detail: PurchaseRequisitionDetail) => void
}

export function PurchaseRequisitionFormModal({ mode, existing, onClose, onSaved }: Props) {
  const identity = getStoredIdentity()
  const defaultRequiredBy = existing?.RequiredByDate ?? today()

  const [departments, setDepartments] = useState<PurchaseLookupOption[]>([])
  const [warehouses, setWarehouses] = useState<PurchaseLookupOption[]>([])
  const [itemOptions, setItemOptions] = useState<PurchaseLookupOption[]>([])
  const [itemSearch, setItemSearch] = useState('')

  const [departmentCode, setDepartmentCode] = useState('')
  const [requiredByDate, setRequiredByDate] = useState(defaultRequiredBy)
  const [priority, setPriority] = useState(existing?.Priority ?? 'Normal')
  const [purpose, setPurpose] = useState(existing?.PurposeJustification ?? '')
  const [warehouseCode, setWarehouseCode] = useState(existing?.DeliveryWarehouseCode ?? '')
  const [costCentre, setCostCentre] = useState(existing?.CostCentre ?? '')
  const [projectReference, setProjectReference] = useState(existing?.ProjectReference ?? '')
  const [serviceReference, setServiceReference] = useState(existing?.ServiceReference ?? '')
  const [workOrderReference, setWorkOrderReference] = useState(existing?.WorkOrderReference ?? '')
  const [customerReference, setCustomerReference] = useState(existing?.CustomerReference ?? '')

  const [lines, setLines] = useState<DraftLine[]>(
    existing
      ? existing.Lines.map((line) => ({
          itemCode: line.ItemCode,
          itemName: line.ItemName,
          quantity: String(line.RequestedQuantity),
          unitPrice: String(line.EstimatedUnitPrice),
          requiredDate: defaultRequiredBy,
          preferredWarehouseCode: '',
        }))
      : [emptyLine(defaultRequiredBy)],
  )

  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    listDepartments().then(setDepartments).catch(() => undefined)
    listWarehouseOptions().then(setWarehouses).catch(() => undefined)
  }, [])

  useEffect(() => {
    const handle = window.setTimeout(() => {
      searchItems(itemSearch).then(setItemOptions).catch(() => undefined)
    }, 250)
    return () => window.clearTimeout(handle)
  }, [itemSearch])

  const estimatedTotal = useMemo(
    () =>
      lines.reduce(
        (sum, line) => sum + (Number(line.quantity) || 0) * (Number(line.unitPrice) || 0),
        0,
      ),
    [lines],
  )

  const setLine = (index: number, patch: Partial<DraftLine>) => {
    setLines((prev) => prev.map((line, i) => (i === index ? { ...line, ...patch } : line)))
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError('')

    const payloadLines: PurchaseRequisitionLineRequest[] = lines
      .filter((line) => line.itemCode.trim().length > 0)
      .map((line) => ({
        ItemCode: line.itemCode.trim(),
        RequestedQuantity: Number(line.quantity) || 0,
        EstimatedUnitPrice: Number(line.unitPrice) || 0,
        RequiredDate: line.requiredDate || requiredByDate,
        PreferredWarehouseCode: blank(line.preferredWarehouseCode),
        ProjectReference: null,
        MachineReference: null,
        ServiceReference: null,
      }))

    if (payloadLines.length === 0) {
      setError('Add at least one line with an item code.')
      return
    }
    if (payloadLines.some((line) => !Number.isFinite(line.EstimatedUnitPrice) || line.EstimatedUnitPrice <= 0)) {
      setError('Every line requires an estimated rate greater than zero.')
      return
    }
    if (!purpose.trim()) {
      setError('Purpose / justification is required.')
      return
    }
    if (!warehouseCode) {
      setError('Delivery warehouse is required.')
      return
    }
    if (mode === 'create' && !departmentCode) {
      setError('Requesting department is required.')
      return
    }
    if (mode === 'create' && !identity) {
      setError('Signed-in identity is unavailable. Sign in again before raising a requisition.')
      return
    }

    setSaving(true)
    try {
      const shared = {
        RequiredByDate: requiredByDate,
        Priority: priority,
        PurposeJustification: purpose.trim(),
        DeliveryWarehouseCode: warehouseCode,
        CostCentre: blank(costCentre),
        ProjectReference: blank(projectReference),
        ServiceReference: blank(serviceReference),
        WorkOrderReference: blank(workOrderReference),
        CustomerReference: blank(customerReference),
        Lines: payloadLines,
      }

      const saved =
        mode === 'create'
          ? await createPurchaseRequisition({
              ...shared,
              OrganizationId: identity!.organizationId,
              RequestingDepartmentCode: departmentCode,
              RequesterEmployeeCode: identity!.employeeCode,
            })
          : await updatePurchaseRequisition(existing!.PrNumber, {
              ...shared,
              Version: existing!.Version,
            })

      onSaved(saved)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save the requisition.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal" style={{ maxWidth: 1040 }} onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{mode === 'create' ? 'New Purchase Requisition' : `Edit ${existing?.PrNumber}`}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={submit} className="form-grid">
          <div className="field-wide form-section-title">Requisition</div>

          {mode === 'create' ? (
            <label className="field">
              <span className="field-label">Requesting department *</span>
              <select
                className="input"
                value={departmentCode}
                onChange={(event) => setDepartmentCode(event.target.value)}
              >
                <option value="">Select department…</option>
                {departments.map((option) => (
                  <option key={option.Code} value={option.Code}>{option.Name}</option>
                ))}
              </select>
            </label>
          ) : (
            <label className="field">
              <span className="field-label">Requesting department</span>
              <input className="input" value={existing?.RequestingDepartment ?? ''} disabled />
              <span className="field-hint">Department cannot change after the draft is created.</span>
            </label>
          )}

          <label className="field">
            <span className="field-label">Requester</span>
            <input
              className="input mono"
              value={existing?.RequesterEmployeeCode ?? identity?.employeeCode ?? ''}
              disabled
            />
            <span className="field-hint">Taken from your signed-in identity; the API rejects any other value.</span>
          </label>

          <label className="field">
            <span className="field-label">Required by *</span>
            <input
              type="date"
              className="input"
              value={requiredByDate}
              onChange={(event) => setRequiredByDate(event.target.value)}
            />
          </label>

          <label className="field">
            <span className="field-label">Priority *</span>
            <select className="input" value={priority} onChange={(event) => setPriority(event.target.value)}>
              {PR_PRIORITIES.map((option) => <option key={option} value={option}>{option}</option>)}
            </select>
          </label>

          <label className="field">
            <span className="field-label">Delivery warehouse *</span>
            <select
              className="input"
              value={warehouseCode}
              onChange={(event) => setWarehouseCode(event.target.value)}
            >
              <option value="">Select warehouse…</option>
              {warehouses.map((option) => (
                <option key={option.Code} value={option.Code}>{option.Code} — {option.Name}</option>
              ))}
            </select>
          </label>

          <label className="field">
            <span className="field-label">Cost centre</span>
            <input className="input" value={costCentre} onChange={(event) => setCostCentre(event.target.value)} />
          </label>

          <label className="field field-wide">
            <span className="field-label">Purpose / justification *</span>
            <textarea
              className="input"
              rows={2}
              value={purpose}
              onChange={(event) => setPurpose(event.target.value)}
            />
            <span className="field-hint">
              This is the audit answer to why the material was bought — write it properly.
            </span>
          </label>

          <div className="field-wide form-section-title">References</div>

          <label className="field">
            <span className="field-label">Project reference</span>
            <input className="input" value={projectReference} onChange={(event) => setProjectReference(event.target.value)} />
          </label>
          <label className="field">
            <span className="field-label">Service reference</span>
            <input className="input" value={serviceReference} onChange={(event) => setServiceReference(event.target.value)} />
          </label>
          <label className="field">
            <span className="field-label">Work order reference</span>
            <input className="input" value={workOrderReference} onChange={(event) => setWorkOrderReference(event.target.value)} />
          </label>
          <label className="field">
            <span className="field-label">Customer reference</span>
            <input className="input" value={customerReference} onChange={(event) => setCustomerReference(event.target.value)} />
          </label>

          <div className="field-wide form-section-title">Lines ({lines.length})</div>

          <div className="field-wide">
            <input
              className="input search"
              placeholder="Type to search the item master, then pick a code in a line below…"
              value={itemSearch}
              onChange={(event) => setItemSearch(event.target.value)}
            />
          </div>

          <div className="field-wide table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th style={{ width: '30%' }}>Item *</th>
                  <th className="text-right">Qty *</th>
                  <th className="text-right">Est. unit price *</th>
                  <th>Required date</th>
                  <th>Preferred warehouse</th>
                  <th className="text-right">Line total</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {lines.map((line, index) => (
                  <tr key={index}>
                    <td>
                      <select
                        className="input"
                        value={line.itemCode}
                        onChange={(event) => {
                          const picked = itemOptions.find((option) => option.Code === event.target.value)
                          setLine(index, {
                            itemCode: event.target.value,
                            itemName: picked?.Name ?? line.itemName,
                          })
                        }}
                      >
                        <option value="">Select item…</option>
                        {line.itemCode && !itemOptions.some((option) => option.Code === line.itemCode) && (
                          <option value={line.itemCode}>{line.itemCode} — {line.itemName}</option>
                        )}
                        {itemOptions.map((option) => (
                          <option key={option.Code} value={option.Code}>{option.Code} — {option.Name}</option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <input
                        type="number"
                        min="0.01"
                        step="0.01"
                        required
                        className="input text-right mono"
                        value={line.quantity}
                        onChange={(event) => setLine(index, { quantity: event.target.value })}
                      />
                    </td>
                    <td>
                      <input
                        type="number"
                        min="0.01"
                        step="0.01"
                        required
                        className="input text-right mono"
                        value={line.unitPrice}
                        onChange={(event) => setLine(index, { unitPrice: event.target.value })}
                      />
                    </td>
                    <td>
                      <input
                        type="date"
                        className="input"
                        value={line.requiredDate}
                        onChange={(event) => setLine(index, { requiredDate: event.target.value })}
                      />
                    </td>
                    <td>
                      <select
                        className="input"
                        value={line.preferredWarehouseCode}
                        onChange={(event) => setLine(index, { preferredWarehouseCode: event.target.value })}
                      >
                        <option value="">Same as delivery</option>
                        {warehouses.map((option) => (
                          <option key={option.Code} value={option.Code}>{option.Code}</option>
                        ))}
                      </select>
                    </td>
                    <td className="text-right mono">
                      {((Number(line.quantity) || 0) * (Number(line.unitPrice) || 0)).toLocaleString('en-IN', {
                        maximumFractionDigits: 2,
                      })}
                    </td>
                    <td>
                      <button
                        type="button"
                        className="btn btn-ghost"
                        disabled={lines.length <= 1}
                        onClick={() => setLines((prev) => prev.filter((_, i) => i !== index))}
                      >✕</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="field-wide action-row">
            <button
              type="button"
              className="btn btn-ghost"
              onClick={() => setLines((prev) => [...prev, emptyLine(requiredByDate)])}
            >+ Add line</button>
            <div className="spacer" />
            <strong>
              Estimated total: ₹{estimatedTotal.toLocaleString('en-IN', { maximumFractionDigits: 2 })}
            </strong>
          </div>

          {error && <div className="field-wide alert alert-error">{error}</div>}

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving…' : mode === 'create' ? 'Create draft' : 'Save changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
