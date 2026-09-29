import { useEffect, useMemo, useState } from 'react'
import { listItems, listUoms } from '../../api/items'
import { createMaterialIssueRequest, updateMaterialIssueRequest } from '../../api/materialIssues'
import { listJobOrders } from '../../api/production'
import { listCustomers } from '../../api/customers'
import { getCustomerPo, listCustomerPos } from '../../api/customerPos'
import type { JobOrderSummary } from '../../types/production'
import type { CustomerSummary } from '../../types/customer'
import { newIdempotencyKey } from '../../api/stores'
import type { ItemSummary, ReferenceLookup } from '../../types/item'
import type { MaterialIssueRequestLineInput, MaterialIssueRequestView } from '../../types/materialIssue'
import { MIR_JOB_SITUATIONS, MIR_PURPOSES, MIR_SITUATIONS, MIR_SPARE_SALE_SITUATION } from '../../types/materialIssue'
import { CustomerSearchSelect } from '../../components/CustomerSearchSelect'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

interface DraftLine {
  key: string
  itemId: string
  itemCode: string
  itemName: string
  uomId: string
  /** The item master's UOM code; another UOM needs an approved conversion. */
  itemUom: string
  quantity: string
  remarks: string
  /** SPARE_SALE only: the customer's current spare Customer PO line for this item. */
  customerPoLineId: string
}

/** A spare Customer PO line of the picked customer, flattened for the per-line picker. */
interface SparePoLine {
  lineId: string
  poRecordNumber: string
  customerPoNumber: string
  slNo: number
  itemId: string
  description: string
  quantity: number | null
  uom: string | null
}

interface Props {
  mode: 'create' | 'edit'
  existing?: MaterialIssueRequestView
  onClose: () => void
  onSaved: (result: MaterialIssueRequestView) => void
}

/** EfMaterialIssueService.CustomerPoQuantityAsync accepts only these sales types for a spare line. */
const SPARE_SALES_TYPES = new Set(['spares', 'spares & service'])

function todayPlus(days: number): string {
  const date = new Date()
  date.setDate(date.getDate() + days)
  return date.toISOString().slice(0, 10)
}

function emptyLine(): DraftLine {
  return { key: crypto.randomUUID(), itemId: '', itemCode: '', itemName: '', uomId: '', itemUom: '', quantity: '', remarks: '', customerPoLineId: '' }
}

/**
 * Material Issue Request (MIR). POST/PUT /stores/material-issue-requests.
 *
 * The three job-backed situations need a JobOrderId and DestinationType
 * JOB_ORDER; only an OPEN (Accounts-confirmed) job is offered, because the
 * service refuses a PENDING_ACCOUNTS one with a 409. CONSUMABLE_OFFICE must
 * target a department or a named destination instead.
 *
 * SPARE_SALE is spare parts sold to a customer without a Job Order: the
 * server wants DestinationType CUSTOMER, a CustomerId, no JobOrderId, and on
 * every line the customer's current spare Customer PO line for that item
 * (CustomerPurchaseOrderLineId). There is no BOM; the line is measured
 * against the PO line quantity and anything above it is customer-facing
 * excess for the TD.
 *
 * The requesting department is the signed-in employee's own department (the
 * session carries its id); the API has no department-id lookup for MIR.
 */
export function MaterialIssueRequestFormModal({ mode, existing, onClose, onSaved }: Props) {
  const { me, can } = useSession()
  const [purpose, setPurpose] = useState(existing?.Purpose ?? 'FACTORY_ASSEMBLY')
  const [situation, setSituation] = useState(existing?.Situation ?? 'CONSUMABLE_OFFICE')
  const [destinationType, setDestinationType] = useState(existing?.DestinationType ?? 'DEPARTMENT')
  const [destinationName, setDestinationName] = useState(existing?.DestinationName ?? '')
  const [requiredDate, setRequiredDate] = useState(existing?.RequiredDate ?? todayPlus(1))
  const [jobOrderId, setJobOrderId] = useState(existing?.JobOrderId ?? '')
  const [jobs, setJobs] = useState<JobOrderSummary[]>([])
  // SPARE_SALE: the customer master list (active only) and the picked customer.
  const [customers, setCustomers] = useState<CustomerSummary[]>([])
  const [customersLoaded, setCustomersLoaded] = useState(false)
  const [customerId, setCustomerId] = useState(existing?.CustomerId ?? '')
  const [customerCode, setCustomerCode] = useState('')
  const [customerName, setCustomerName] = useState('')
  const [sparePoLines, setSparePoLines] = useState<SparePoLine[]>([])
  const [sparePoLoading, setSparePoLoading] = useState(false)
  const [lines, setLines] = useState<DraftLine[]>(() =>
    existing
      ? existing.Lines.map((line) => ({
          key: line.Id,
          itemId: line.ItemId,
          itemCode: line.ItemCode,
          itemName: line.ItemName,
          uomId: line.UomId,
          itemUom: line.UomCode,
          quantity: String(line.RequestedQuantity),
          remarks: line.Remarks ?? '',
          customerPoLineId: line.CustomerPurchaseOrderLineId ?? '',
        }))
      : [emptyLine()],
  )
  const [uoms, setUoms] = useState<ReferenceLookup[]>([])
  const [itemSearch, setItemSearch] = useState('')
  const [itemOptions, setItemOptions] = useState<ItemSummary[]>([])
  const [searching, setSearching] = useState(false)
  const [lookupError, setLookupError] = useState<unknown>(null)
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    // Optional: the picked item's BaseUomId sets the line UOM, so a role without
    // masters.uoms:view (Production, Stores executives) must not see an alert.
    listUoms().then((page) => setUoms(page.Items ?? [])).catch(() => setUoms([]))
  }, [])

  const jobSituation = MIR_JOB_SITUATIONS.includes(situation as (typeof MIR_JOB_SITUATIONS)[number])
  const spareSale = situation === MIR_SPARE_SALE_SITUATION

  // Job orders are only needed for the job-backed situations, and only
  // OPEN ones are usable; the list is fetched the first time one is chosen.
  useEffect(() => {
    if (!jobSituation || jobs.length > 0) return
    listJobOrders({ page: 1, pageSize: 100, status: 'OPEN' }).then((page) => setJobs(page.Items ?? [])).catch(setLookupError)
  }, [jobSituation, jobs.length])

  const job = jobs.find((candidate) => candidate.Id === jobOrderId)
  useEffect(() => {
    if (jobSituation && job) setDestinationName(`${job.JobOrderNumber} · ${job.MachineSerial}`)
  }, [jobSituation, job])

  // Customers are only needed for SPARE_SALE; same lookup as the Customer PO
  // form (active customers from the master, searched client-side).
  useEffect(() => {
    if (!spareSale || customersLoaded) return
    listCustomers({ page: 1, pageSize: 500 })
      .then((page) => {
        setCustomers((page.Items ?? []).filter((customer) => customer.IsActive))
        setCustomersLoaded(true)
      })
      .catch(setLookupError)
  }, [spareSale, customersLoaded])

  // Edit mode round-trip: the view carries CustomerId, so the code and name
  // are recovered from the master list once it arrives.
  useEffect(() => {
    if (!spareSale || !customerId || customerCode) return
    const found = customers.find((customer) => customer.Id === customerId)
    if (found) {
      setCustomerCode(found.CustomerCode)
      setCustomerName(found.Name)
    }
  }, [spareSale, customerId, customerCode, customers])

  // The picked customer's spare Customer POs (SalesType Spares / Spares &
  // Service), flattened to their current-revision lines, so each MIR line can
  // name the PO line the server checks it against.
  useEffect(() => {
    if (!spareSale || !customerCode) {
      setSparePoLines([])
      return
    }
    let cancelled = false
    setSparePoLoading(true)
    listCustomerPos({ page: 1, pageSize: 200, search: customerName || customerCode })
      .then(async (page) => {
        const spares = (page.Items ?? []).filter((po) =>
          po.CustomerCode === customerCode && SPARE_SALES_TYPES.has((po.SalesType ?? '').trim().toLowerCase()))
        const details = await Promise.all(spares.map((po) => getCustomerPo(po.PoRecordNumber)))
        if (cancelled) return
        const flat: SparePoLine[] = []
        for (const po of details) {
          for (const line of po.Lines ?? []) {
            if (!line.Id || !line.ItemId) continue
            flat.push({
              lineId: line.Id,
              poRecordNumber: po.PoRecordNumber,
              customerPoNumber: po.CustomerPoNumber,
              slNo: line.SlNo,
              itemId: line.ItemId,
              description: line.Description,
              quantity: line.Quantity,
              uom: line.Uom,
            })
          }
        }
        setSparePoLines(flat)
      })
      .catch((err) => { if (!cancelled) setLookupError(err) })
      .finally(() => { if (!cancelled) setSparePoLoading(false) })
    return () => { cancelled = true }
  }, [spareSale, customerCode, customerName])

  // Item master search, debounced. The list endpoint filters on code and name.
  useEffect(() => {
    const term = itemSearch.trim()
    if (term.length < 2) {
      setItemOptions([])
      return
    }
    const handle = window.setTimeout(() => {
      setSearching(true)
      listItems({ page: 1, pageSize: 15, search: term })
        .then((page) => setItemOptions(page.Items ?? []))
        .catch(setLookupError)
        .finally(() => setSearching(false))
    }, 250)
    return () => window.clearTimeout(handle)
  }, [itemSearch])

  const uomByCode = useMemo(() => {
    const map = new Map<string, ReferenceLookup>()
    for (const uom of uoms) map.set(uom.Code.toUpperCase(), uom)
    return map
  }, [uoms])

  useEffect(() => {
    if (destinationType === 'DEPARTMENT' && !destinationName && me?.DepartmentCode) {
      setDestinationName(me.DepartmentCode)
    }
  }, [destinationType, destinationName, me])

  const setLine = (key: string, patch: Partial<DraftLine>) =>
    setLines((current) => current.map((line) => (line.key === key ? { ...line, ...patch } : line)))

  const pickItem = (key: string, item: ItemSummary) => {
    const uom = uomByCode.get((item.Uom ?? '').toUpperCase())
    const poLinesForItem = sparePoLines.filter((poLine) => poLine.itemId === item.Id)
    setLine(key, {
      itemId: item.Id,
      itemCode: item.ItemCode,
      itemName: item.Name,
      uomId: item.BaseUomId ?? uom?.Id ?? '',
      itemUom: item.Uom ?? '',
      // One matching spare PO line is picked for the user; several need a choice.
      customerPoLineId: spareSale && poLinesForItem.length === 1 ? poLinesForItem[0].lineId : '',
    })
  }

  const pickCustomer = (option: { CustomerCode: string; Name: string }) => {
    const found = customers.find((customer) => customer.CustomerCode === option.CustomerCode)
    setCustomerCode(option.CustomerCode)
    setCustomerName(option.Name)
    setCustomerId(found?.Id ?? '')
    setDestinationName(option.Name)
    // PO lines belong to the customer; a change of customer invalidates them.
    setLines((current) => current.map((line) => ({ ...line, customerPoLineId: '' })))
  }

  const canSave = can(PAGE_KEYS.materialIssueRequests, mode === 'create' ? 'create' : 'update')

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    if (!me?.DepartmentId) {
      setError('Your session has no department, so the requesting department cannot be set.')
      return
    }
    if (jobSituation && !jobOrderId) {
      setError('Job-backed situations need a Job Order. Pick an OPEN job order, or raise a CONSUMABLE_OFFICE request instead.')
      return
    }
    if (spareSale && !customerId) {
      setError('A spare sale needs a customer. Pick one from the customer master (free text is not enough — the server needs the customer id).')
      return
    }
    const complete = lines.filter((line) => line.itemId || line.quantity)
    if (complete.length === 0) {
      setError('Add at least one line — pick an item from the search and enter a quantity.')
      return
    }
    // A job-backed MIR is customer-facing: the server requires the Customer PO
    // line behind the Job Order on every line (CustomerPurchaseOrderLineId).
    // JobOrderSummary carries it, so it is taken from the picked job order.
    const jobCustomerPoLineId = jobSituation ? (jobs.find((job) => job.Id === jobOrderId)?.CustomerPurchaseOrderLineId ?? null) : null
    if (jobSituation && !jobCustomerPoLineId) {
      setError('The picked Job Order does not expose its Customer PO line, which the server requires for a customer-facing request.')
      return
    }
    const payloadLines: MaterialIssueRequestLineInput[] = []
    for (const line of complete) {
      const quantity = Number(line.quantity)
      if (!line.itemId) { setError('Every line needs an item picked from the item master.'); return }
      if (!line.uomId) { setError(`${line.itemCode}: pick a UOM — the item's UOM code has no matching active UOM.`); return }
      if (!(quantity > 0)) { setError(`${line.itemCode}: quantity must be greater than zero.`); return }
      if (spareSale && !line.customerPoLineId) {
        setError(`${line.itemCode}: pick the customer's spare PO line for this item. The server refuses a SPARE_SALE line without one.`)
        return
      }
      payloadLines.push({
        ItemId: line.itemId,
        UomId: line.uomId,
        Quantity: quantity,
        CustomerPurchaseOrderLineId: jobSituation ? jobCustomerPoLineId : spareSale ? line.customerPoLineId : null,
        Remarks: line.remarks.trim() || null,
      })
    }
    const body = {
      Purpose: purpose,
      Situation: situation,
      DestinationType: destinationType,
      JobOrderId: jobSituation ? jobOrderId : null,
      CustomerId: spareSale ? customerId : null,
      VendorId: null,
      DestinationDepartmentId: destinationType === 'DEPARTMENT' ? me.DepartmentId : null,
      DestinationName: destinationName.trim() || (destinationType === 'DEPARTMENT' ? me.DepartmentCode : spareSale ? customerName.trim() : ''),
      RequestingDepartmentId: me.DepartmentId,
      RequiredDate: requiredDate,
      Lines: payloadLines,
      IdempotencyKey: newIdempotencyKey(mode === 'create' ? 'mir-create' : 'mir-update'),
    }
    if (!body.DestinationName) {
      setError('Destination name is required.')
      return
    }
    setSaving(true)
    try {
      const result = mode === 'create'
        ? await createMaterialIssueRequest(body)
        : await updateMaterialIssueRequest(existing!.Id, { ...body, Version: existing!.Version })
      onSaved(result)
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  const situationLabel = (option: string) => {
    if (MIR_JOB_SITUATIONS.includes(option as (typeof MIR_JOB_SITUATIONS)[number])) return `${option.replaceAll('_', ' ')} (against a Job Order)`
    if (option === MIR_SPARE_SALE_SITUATION) return 'SPARE SALE (to a customer, no Job Order)'
    return option.replaceAll('_', ' ')
  }

  return (
    <div className="modal-backdrop">
      <div className="modal modal-wide" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{mode === 'create' ? 'New Material Issue Request' : `Edit ${existing?.RequestNumber}`}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={submit} className="form-grid">
          <div className="field-wide form-section-title">Request</div>

          <label className="field">
            <span className="field-label">Situation *</span>
            <select className="input" value={situation} onChange={(event) => {
              const next = event.target.value
              setSituation(next)
              if (MIR_JOB_SITUATIONS.includes(next as (typeof MIR_JOB_SITUATIONS)[number])) {
                setDestinationType('JOB_ORDER')
              } else if (next === MIR_SPARE_SALE_SITUATION) {
                setDestinationType('CUSTOMER')
                if (customerName) setDestinationName(customerName)
              } else if (destinationType === 'JOB_ORDER' || destinationType === 'CUSTOMER') {
                setDestinationType('DEPARTMENT')
                setDestinationName('')
              }
            }}>
              {MIR_SITUATIONS.map((option) => (
                <option key={option} value={option}>{situationLabel(option)}</option>
              ))}
            </select>
            <span className="field-hint">
              Chamber, service-PO and site-project requests are tied to a Job Order; a spare sale goes to a customer against their spare PO with no Job Order; consumables and office material are tied to neither.
            </span>
          </label>

          {jobSituation && (
            <label className="field">
              <span className="field-label">Job order *</span>
              <select className="input" value={jobOrderId} onChange={(event) => setJobOrderId(event.target.value)}>
                <option value="">Pick an OPEN job order…</option>
                {jobs.map((candidate) => (
                  <option key={candidate.Id} value={candidate.Id}>{candidate.JobOrderNumber} · {candidate.CustomerName} · {candidate.MachineModel} #{candidate.MachineOrdinal}</option>
                ))}
                {jobOrderId && !jobs.some((candidate) => candidate.Id === jobOrderId) && <option value={jobOrderId}>{jobOrderId}</option>}
              </select>
              <span className="field-hint">Only Accounts-confirmed jobs are listed. Lines are checked against its Estimated BOM, pinned Production BOM and customer PO; excess needs a TD decision.</span>
            </label>
          )}

          {spareSale && (
            <div className="field">
              <span className="field-label">Customer *</span>
              <CustomerSearchSelect
                options={customers.map((customer) => ({ CustomerCode: customer.CustomerCode, Name: customer.Name }))}
                customerCode={customerCode}
                customerName={customerName}
                onSelect={pickCustomer}
                onText={(name) => {
                  setCustomerName(name)
                  setCustomerCode('')
                  setCustomerId('')
                }}
                placeholder={customersLoaded ? 'Type the customer name or code…' : 'Loading customers…'}
              />
              <span className="field-hint">
                Spare parts sold to this customer without a Job Order. There is no BOM to check against; each line is measured against the customer's spare PO line, and anything above it is excess for the TD.
              </span>
              {existing?.CustomerId && !customerCode && customersLoaded && (
                <span className="field-hint">The saved customer (<span className="mono">{existing.CustomerId}</span>) is not an active customer in the master any more; pick another.</span>
              )}
            </div>
          )}

          <label className="field">
            <span className="field-label">Purpose *</span>
            <select className="input" value={purpose} onChange={(event) => setPurpose(event.target.value)}>
              {MIR_PURPOSES.map((option) => <option key={option} value={option}>{option.replaceAll('_', ' ')}</option>)}
            </select>
          </label>

          <label className="field">
            <span className="field-label">Destination *</span>
            <select className="input" value={destinationType} disabled={jobSituation || spareSale} onChange={(event) => setDestinationType(event.target.value)}>
              <option value="DEPARTMENT">DEPARTMENT — my department ({me?.DepartmentCode ?? '…'})</option>
              <option value="OTHER">OTHER — named below</option>
              {jobSituation && <option value="JOB_ORDER">JOB ORDER</option>}
              {spareSale && <option value="CUSTOMER">CUSTOMER</option>}
            </select>
            {spareSale && <span className="field-hint">A spare sale always goes to the customer.</span>}
          </label>

          <label className="field">
            <span className="field-label">Destination name *</span>
            <input
              className="input"
              value={destinationName}
              placeholder={destinationType === 'OTHER' ? 'e.g. Site office, Demo bay' : spareSale ? 'Customer name (filled from the picked customer)' : ''}
              onChange={(event) => setDestinationName(event.target.value)}
            />
          </label>

          <label className="field">
            <span className="field-label">Required date *</span>
            <input className="input" type="date" value={requiredDate} onChange={(event) => setRequiredDate(event.target.value)} />
          </label>

          <div className="field">
            <span className="field-label">Requesting department</span>
            <div className="mono" style={{ paddingTop: 6 }}>{me?.DepartmentCode ?? '—'}</div>
            <span className="field-hint">Taken from your session; the requester cannot raise on behalf of another department.</span>
          </div>

          <div className="field-wide form-section-title">Lines ({lines.filter((line) => line.itemId).length})</div>

          <div className="field field-wide">
            <span className="field-label">Find item</span>
            <input
              className="input search"
              placeholder="Type at least 2 characters of the item code or name, then choose a line's item below…"
              value={itemSearch}
              onChange={(event) => setItemSearch(event.target.value)}
            />
            {searching && <span className="field-hint">Searching…</span>}
          </div>
          <ErrorAlert error={lookupError} className="field-wide" fallback="Item, UOM, customer or customer PO lookup failed." />

          {spareSale && customerCode && !sparePoLoading && sparePoLines.length === 0 && (
            <div className="alert alert-warn field-wide" role="alert">
              <div className="alert-title">{customerName} has no spare Customer PO with item lines</div>
              <p className="alert-body">A SPARE_SALE line must name a current line of a Customer PO whose sales type is Spares or Spares &amp; Service. Record that PO in Sales first.</p>
            </div>
          )}

          <div className="field-wide table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th style={{ width: 40 }}>#</th>
                  <th>Item *</th>
                  <th>UOM * <span className="field-hint" style={{ display: 'inline' }}>(item's own)</span></th>
                  <th className="text-right" style={{ width: 120 }}>Quantity *</th>
                  {spareSale && <th>Customer PO line *</th>}
                  <th>Remarks</th>
                  <th style={{ width: 60 }} />
                </tr>
              </thead>
              <tbody>
                {lines.map((line, index) => {
                  const poLinesForItem = spareSale && line.itemId ? sparePoLines.filter((poLine) => poLine.itemId === line.itemId) : []
                  const poLineKnown = !line.customerPoLineId || poLinesForItem.some((poLine) => poLine.lineId === line.customerPoLineId)
                  return (
                  <tr key={line.key}>
                    <td className="mono">{index + 1}</td>
                    <td>
                      {line.itemId ? (
                        <div>
                          <span className="mono">{line.itemCode}</span> — {line.itemName}
                          <button type="button" className="btn btn-ghost" style={{ marginLeft: 8 }} onClick={() => setLine(line.key, { itemId: '', itemCode: '', itemName: '', uomId: '', itemUom: '', customerPoLineId: '' })}>change</button>
                        </div>
                      ) : (
                        <select
                          className="input"
                          value=""
                          disabled={itemOptions.length === 0}
                          onChange={(event) => {
                            const item = itemOptions.find((option) => option.Id === event.target.value)
                            if (item) pickItem(line.key, item)
                          }}
                        >
                          <option value="">{itemOptions.length === 0 ? 'Search above, then pick…' : `Pick from ${itemOptions.length} matches…`}</option>
                          {itemOptions.map((item) => (
                            <option key={item.Id} value={item.Id}>{item.ItemCode} — {item.Name} ({item.Uom})</option>
                          ))}
                        </select>
                      )}
                    </td>
                    <td>
                      {uoms.length === 0 && line.uomId ? (
                        <span className="mono" title="Item base UOM (the UOM master is not readable by your role)">{line.itemUom || '—'}</span>
                      ) : (
                        <select className="input" value={line.uomId} onChange={(event) => setLine(line.key, { uomId: event.target.value })}>
                          <option value="">—</option>
                          {uoms.map((uom) => <option key={uom.Id} value={uom.Id}>{uom.Code}</option>)}
                        </select>
                      )}
                      {uoms.length > 0 && line.itemId && line.itemUom && uomByCode.get(line.itemUom.toUpperCase())?.Id !== line.uomId && (
                        <span className="field-hint">Item UOM is {line.itemUom}. Another UOM needs an approved conversion or the save is refused.</span>
                      )}
                      {uoms.length > 0 && line.itemId && line.itemUom && !uomByCode.has(line.itemUom.toUpperCase()) && (
                        <span className="field-hint">Item UOM “{line.itemUom}” is not an active UOM in the master.</span>
                      )}
                    </td>
                    <td className="text-right">
                      <input
                        className="input text-right mono"
                        inputMode="decimal"
                        value={line.quantity}
                        onChange={(event) => setLine(line.key, { quantity: event.target.value })}
                      />
                    </td>
                    {spareSale && (
                      <td>
                        {!line.itemId ? (
                          <span className="field-hint">Pick the item first.</span>
                        ) : (
                          <select className="input" value={line.customerPoLineId} onChange={(event) => setLine(line.key, { customerPoLineId: event.target.value })}>
                            <option value="">{sparePoLoading ? 'Loading spare POs…' : poLinesForItem.length === 0 ? 'No spare PO line for this item' : 'Pick the PO line…'}</option>
                            {poLinesForItem.map((poLine) => (
                              <option key={poLine.lineId} value={poLine.lineId}>
                                {poLine.poRecordNumber} · {poLine.customerPoNumber} · line {poLine.slNo} · {poLine.quantity ?? '?'} {poLine.uom ?? ''}
                              </option>
                            ))}
                            {!poLineKnown && <option value={line.customerPoLineId}>{line.customerPoLineId} (saved)</option>}
                          </select>
                        )}
                        {line.itemId && line.customerPoLineId && (() => {
                          const poLine = poLinesForItem.find((candidate) => candidate.lineId === line.customerPoLineId)
                          const quantity = Number(line.quantity)
                          return poLine && poLine.quantity !== null && quantity > poLine.quantity
                            ? <span className="field-hint">Above the PO line quantity ({poLine.quantity} {poLine.uom ?? ''}): the difference is customer-facing excess and needs the TD.</span>
                            : null
                        })()}
                      </td>
                    )}
                    <td>
                      <input className="input" value={line.remarks} onChange={(event) => setLine(line.key, { remarks: event.target.value })} />
                    </td>
                    <td>
                      <button type="button" className="btn btn-ghost" disabled={lines.length === 1} onClick={() => setLines((current) => current.filter((row) => row.key !== line.key))}>✕</button>
                    </td>
                  </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
          <div className="field-wide">
            <button type="button" className="btn btn-ghost" onClick={() => setLines((current) => [...current, emptyLine()])}>+ Add line</button>
          </div>

          <ErrorAlert error={error} className="field-wide" fallback="Could not save the request." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving || !canSave} title={canSave ? undefined : 'Your role cannot create material issue requests.'}>
              {saving ? 'Saving…' : mode === 'create' ? 'Create draft' : 'Save changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
