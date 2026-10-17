import { useCallback, useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import * as portal from '../../api/employeeMirs'
import type { PortalIssue, PortalItem, PortalReferences, PortalSpareLine } from '../../api/employeeMirs'
import { getCompany } from '../../auth/authSession'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { BarcodeScanField } from '../barcodes/BarcodeScanField'
import { barcodeSessionKey } from '../barcodes/barcodeSession'
import { addScanQuantity } from '../barcodes/barcodeScan'
import { ScreenLifecycle } from '../purchase/screenLifecycle'
import { OperationIntent, RequestGate } from '../purchase/quotationDraft'
import { businessDate } from '../../utils/businessDate'
import { MIR_PURPOSES, MIR_SITUATIONS, MIR_STATES } from '../../types/materialIssue'
import type { CreateMaterialIssueRequest, MaterialIssueRequestView, MaterialReturnView, OutstandingEngineerCustodyView } from '../../types/materialIssue'
import { addPortalItem, canEditOwnMir, canSubmitOwnMir, canDeclareOwnReturn, editableRemarks, jobReference, matchesOwnReturnScan } from './portalDraft'
import type { PortalLine } from './portalDraft'

interface Draft {
  purpose: string; situation: string; destinationType: string; job: string; customer: string
  destination: string; requiredDate: string; reference: string; lines: PortalLine[]
}
interface ReturnLine { quantity: string; consumed: string; held: string; scan: string }
interface ReturnDraft { data: PortalIssue; selected: string; declaredAt: string; lines: Record<string, ReturnLine> }
const blank = (department: string): Draft => ({ purpose: 'FACTORY_ASSEMBLY', situation: 'CONSUMABLE_OFFICE', destinationType: 'DEPARTMENT',
  job: '', customer: '', destination: department, requiredDate: businessDate(), reference: '', lines: [] })
const title = (value: string) => value.replaceAll('_', ' ')

export function EmployeeMirPortalPage() {
  const { me, can } = useSession()
  const scope = barcodeSessionKey(me)
  const [viewScope, setViewScope] = useState(scope)
  const [rows, setRows] = useState<MaterialIssueRequestView[]>([])
  const [total, setTotal] = useState(0), [page, setPage] = useState(1)
  const [filter, setFilter] = useState(''), [search, setSearch] = useState(''), [applied, setApplied] = useState('')
  const [references, setReferences] = useState<PortalReferences | null>(null)
  const [custody, setCustody] = useState<OutstandingEngineerCustodyView[]>([])
  const [returns, setReturns] = useState<MaterialReturnView[]>([])
  const [returnTotal, setReturnTotal] = useState(0), [returnPage, setReturnPage] = useState(1)
  const [selected, setSelected] = useState<MaterialIssueRequestView | null>(null)
  const [editor, setEditor] = useState(false)
  const [draft, setDraft] = useState<Draft>(() => blank(me?.DepartmentCode ?? ''))
  const draftRef = useRef(draft)
  const [itemSearch, setItemSearch] = useState(''), [choices, setChoices] = useState<PortalItem[]>([])
  const [spares, setSpares] = useState<PortalSpareLine[]>([])
  const [reason, setReason] = useState('')
  const [returnDraft, setReturnDraft] = useState<ReturnDraft | null>(null)
  const returnRef = useRef(returnDraft)
  const [error, setError] = useState<unknown>(null), [notice, setNotice] = useState('')
  const [loading, setLoading] = useState(false), [busy, setBusy] = useState(false), [scanPending, setScanPending] = useState(false)
  const pending = useRef(false), scanPendingRef = useRef(false)
  const reads = useRef(new RequestGate()), detailReads = useRef(new RequestGate()), itemReads = useRef(new RequestGate()), spareReads = useRef(new RequestGate())
  const lifecycle = useRef<ScreenLifecycle | null>(null)
  if (!lifecycle.current) lifecycle.current = new ScreenLifecycle().track(reads.current, detailReads.current, itemReads.current, spareReads.current)
  const life = lifecycle.current
  life.bindBusy('command', (value) => { pending.current = value; setBusy(value) })
  const intent = useRef(new OperationIntent(() => `mir-portal-${crypto.randomUUID()}`))
  const change = (update: (current: Draft) => Draft) => { const next = update(draftRef.current); draftRef.current = next; setDraft(next) }
  const changeReturn = (update: (current: ReturnDraft) => ReturnDraft) => {
    if (!returnRef.current) return
    const next = update(returnRef.current); returnRef.current = next; setReturnDraft(next)
  }
  const scanState = (value: boolean) => { scanPendingRef.current = value; setScanPending(value) }
  useEffect(() => { life.mount(); return () => life.unmount() }, [life])
  useEffect(() => {
    life.changeScope(); intent.current.clear(); setSelected(null); setEditor(false); setRows([]); setTotal(0)
    setCustody([]); setReturns([]); setReferences(null); setPage(1); setReturnPage(1); setFilter(''); setSearch(''); setApplied('')
    setChoices([]); setSpares([]); setError(null); setNotice(''); setReason(''); setItemSearch('')
    const next = blank(me?.DepartmentCode ?? ''); draftRef.current = next; setDraft(next)
    returnRef.current = null; setReturnDraft(null); setViewScope(scope)
  }, [scope, life])
  const load = useCallback(async () => {
    if (!scope) return
    const ticket = reads.current.begin(), live = life.begin()
    setLoading(true); setError(null)
    try {
      const [list, refs, held, declared] = await Promise.all([portal.listOwnMirs(page, applied, filter), portal.getPortalReferences(), portal.listOwnCustody(), portal.listOwnReturns(returnPage)])
      if (!live() || !reads.current.isCurrent(ticket)) return
      setRows(list.Items ?? []); setTotal(list.Total); setReferences(refs); setCustody(held); setReturns(declared.Items ?? []); setReturnTotal(declared.Total)
    } catch (failure) { if (live() && reads.current.isCurrent(ticket)) setError(failure) }
    finally { if (live() && reads.current.isCurrent(ticket)) setLoading(false) }
  }, [scope, page, applied, filter, returnPage, life])
  useEffect(() => { void load() }, [load])
  useEffect(() => {
    const ticket = spareReads.current.begin(), live = life.begin()
    setSpares([])
    if (draft.situation !== 'SPARE_SALE' || !draft.customer) return
    void portal.getPortalSpareLines(draft.customer).then((lines) => {
      if (live() && spareReads.current.isCurrent(ticket)) setSpares(lines)
    }).catch((failure) => { if (live() && spareReads.current.isCurrent(ticket)) setError(failure) })
  }, [scope, draft.situation, draft.customer, life])
  const resetDraft = (document: MaterialIssueRequestView | null) => {
    const next: Draft = document ? { purpose: document.Purpose, situation: document.Situation, destinationType: document.DestinationType,
      job: document.JobOrderId ?? '', customer: document.CustomerId ?? '', destination: document.DestinationName, requiredDate: document.RequiredDate,
      reference: jobReference(document), lines: document.Lines.map((line) => ({ item: { Id: line.ItemId, ItemCode: line.ItemCode, Name: line.ItemName, UomId: line.UomId, UomCode: line.UomCode },
        quantity: String(line.RequestedQuantity), remarks: editableRemarks(line.Remarks), customerPoLineId: line.CustomerPurchaseOrderLineId ?? '' })) } : blank(me?.DepartmentCode ?? '')
    draftRef.current = next; setDraft(next); setChoices([]); setItemSearch('')
  }
  const open = async (id: string) => {
    life.setTarget('command', id); intent.current.clear(); const ticket = detailReads.current.begin(), live = life.begin()
    setEditor(false); setSelected(null); setError(null); setReason(''); returnRef.current = null; setReturnDraft(null)
    try { const document = await portal.getOwnMir(id); if (live() && detailReads.current.isCurrent(ticket)) setSelected(document) }
    catch (failure) { if (live() && detailReads.current.isCurrent(ticket)) setError(failure) }
  }
  const newRequest = () => {
    life.setTarget('command', `new-${crypto.randomUUID()}`); detailReads.current.reset(); intent.current.clear()
    setSelected(null); resetDraft(null); setEditor(true); setError(null); setNotice(''); setReason(''); returnRef.current = null; setReturnDraft(null)
  }
  const addItem = (item: PortalItem) => {
    try {
      change((current) => ({ ...current, lines: addPortalItem(current.lines, item).map((line) => {
        const matching = spares.filter((po) => po.ItemId === line.item.Id)
        return current.situation === 'SPARE_SALE' && !line.customerPoLineId && matching.length === 1 ? { ...line, customerPoLineId: matching[0].Id } : line
      }) }))
      setChoices([]); setNotice('')
    } catch (failure) { setError(failure) }
  }
  const findItems = async () => {
    const ticket = itemReads.current.begin(), live = life.begin()
    try { const found = await portal.searchPortalItems(itemSearch); if (live() && itemReads.current.isCurrent(ticket)) { setChoices(found); setNotice(found.length ? '' : 'No approved item matches this search.') } }
    catch (failure) { if (live() && itemReads.current.isCurrent(ticket)) setError(failure) }
  }
  const write = async (actionName: string, body: unknown, action: (key: string) => Promise<MaterialIssueRequestView>) => {
    if (pending.current || scanPendingRef.current) return
    const actionOwner = life.startAction('command'), key = intent.current.keyFor([scope, life.targetOf('command'), actionName, body])
    setError(null); setNotice('')
    try {
      const document = await action(key)
      if (!actionOwner.isLive()) return
      intent.current.clear(); setSelected(document); setEditor(false); setNotice(`${document.RequestNumber}: ${title(document.Status)}.`); setReason(''); void load()
    } catch (failure) { if (actionOwner.isLive()) setError(failure) }
    finally { actionOwner.finish() }
  }
  const save = async (event: FormEvent) => {
    event.preventDefault()
    if (!references || pending.current || scanPendingRef.current) return
    const current = draftRef.current
    if (!current.reference.trim() || !current.destination.trim() || !current.lines.length || current.lines.some((line) => !Number.isFinite(Number(line.quantity)) || Number(line.quantity) <= 0)) {
      setError(new Error('Enter the job/machine, destination and at least one item with a positive quantity.')); return
    }
    const jobBacked = !['CONSUMABLE_OFFICE', 'SPARE_SALE'].includes(current.situation)
    const body: CreateMaterialIssueRequest = { Purpose: current.purpose, Situation: current.situation,
      DestinationType: jobBacked ? 'JOB_ORDER' : current.situation === 'SPARE_SALE' ? 'CUSTOMER' : current.destinationType,
      JobOrderId: jobBacked ? current.job || null : null, CustomerId: current.situation === 'SPARE_SALE' ? current.customer || null : null,
      VendorId: null, DestinationDepartmentId: !jobBacked && current.situation !== 'SPARE_SALE' && current.destinationType === 'DEPARTMENT' ? references.DepartmentId : null,
      DestinationName: current.destination.trim(), RequestingDepartmentId: references.DepartmentId, RequiredDate: current.requiredDate,
      Lines: current.lines.map((line) => ({ ItemId: line.item.Id, UomId: line.item.UomId, Quantity: Number(line.quantity),
        CustomerPurchaseOrderLineId: current.situation === 'SPARE_SALE' ? line.customerPoLineId || null : null, Remarks: line.remarks.trim() || null })), IdempotencyKey: '' }
    const document = selected
    const content = { JobOrMachine: current.reference.trim(), Request: body, Version: document?.Version }
    await write('save', content, (key) => document
      ? portal.updateOwnMir(document.Id, current.reference.trim(), { ...body, Version: document.Version, IdempotencyKey: key })
      : portal.createOwnMir(current.reference.trim(), { ...body, IdempotencyKey: key }))
  }
  const submit = async () => {
    if (!selected || !reason.trim()) { setError(new Error('A submit reason is required.')); return }
    const document = selected, body = { Version: document.Version, Reason: reason.trim() }
    await write('submit', body, (key) => portal.submitOwnMir(document.Id, { ...body, IdempotencyKey: key }))
  }
  const openReturn = async (id: string) => {
    life.setTarget('command', `return:${id}`); intent.current.clear(); const ticket = detailReads.current.begin(), live = life.begin()
    setSelected(null); setEditor(false); setError(null); returnRef.current = null; setReturnDraft(null)
    try {
      const data = await portal.getOwnIssue(id)
      if (!live() || !detailReads.current.isCurrent(ticket)) return
      const lines = Object.fromEntries(data.Issue.Lines.map((line) => [line.Id, { quantity: '0', consumed: '0', held: '0', scan: '' }]))
      const next: ReturnDraft = { data, lines, selected: data.Issue.Lines.length === 1 ? data.Issue.Lines[0].Id : '', declaredAt: new Date().toISOString() }
      returnRef.current = next; setReturnDraft(next)
    } catch (failure) { if (live() && detailReads.current.isCurrent(ticket)) setError(failure) }
  }
  const declare = async (event: FormEvent) => {
    event.preventDefault()
    const current = returnRef.current
    if (!current || pending.current || scanPendingRef.current) return
    const rows = current.data.Issue.Lines.filter((line) => Number(current.lines[line.Id].quantity) > 0)
    if (!rows.length) { setError(new Error('Enter at least one positive return quantity.')); return }
    if (rows.some((line) => { const value = current.lines[line.Id]; return !value.scan.trim() ||
      !Number.isFinite(Number(value.quantity)) || Number(value.quantity) > (current.data.ReturnableQuantities[line.Id] ?? 0) ||
      !Number.isFinite(Number(value.consumed)) || Number(value.consumed) < 0 || !Number.isFinite(Number(value.held)) || Number(value.held) < 0 })) {
      setError(new Error('Check the scan, current returnable quantity and non-negative consumed / still-held quantities.')); return
    }
    const body = { DeclaredAt: current.declaredAt, Lines: rows.map((line) => { const values = current.lines[line.Id]; return {
      MaterialIssueLineId: line.Id, ScanCode: values.scan.trim(), ReturnedQuantity: Number(values.quantity), ReportedConsumedQuantity: Number(values.consumed), ReportedStillHeldQuantity: Number(values.held) } }) }
    const owner = life.startAction('command'), key = intent.current.keyFor([scope, current.data.Issue.Id, 'return', body])
    setError(null)
    try {
      const result = await portal.declareOwnReturn(current.data.Issue.Id, { ...body, IdempotencyKey: key })
      if (!owner.isLive()) return
      intent.current.clear(); returnRef.current = null; setReturnDraft(null); setNotice(`${result.ReturnNumber}: submitted. Stores must accept the physical return before stock is credited.`); void load()
    } catch (failure) { if (owner.isLive()) setError(failure) }
    finally { owner.finish() }
  }
  const jobBacked = !['CONSUMABLE_OFFICE', 'SPARE_SALE'].includes(draft.situation)
  if (scope !== viewScope) return <p>Loading the selected company?</p>
  return <div className="page">
    <div className="page-header"><div><h1>My material requests</h1><h2>{me?.OrganizationId}</h2><p>{me?.EmployeeName} ? {me?.EmployeeCode} ? {me?.DepartmentCode}</p></div>
      <div className="action-row"><button className="btn btn-ghost" type="button" onClick={() => void load()}>Refresh</button>
        {can(PAGE_KEYS.ownMirs, 'create') && <button className="btn btn-primary" type="button" onClick={newRequest} disabled={!references || busy || scanPending}>+ New MIR</button>}</div></div>
    <p className="page-sub">Only your requests and custody are shown here. KARTHICK or SARATH approves; the TD is the fallback. Nobody approves their own MIR.</p>
    <ErrorAlert error={error} onReload={() => void load()} fallback="The action could not be confirmed. Refresh and check the list before trying again." />
    {notice && <p role="status">{notice}</p>}
    <div className="toolbar"><input className="input" aria-label="MIR number" value={search} onChange={(e) => setSearch(e.target.value)} />
      <button className="btn btn-ghost" type="button" onClick={() => { setPage(1); setApplied(search) }}>Search</button>
      <select className="input" aria-label="MIR status" value={filter} onChange={(e) => { setPage(1); setFilter(e.target.value) }}><option value="">All statuses</option>{MIR_STATES.map((state) => <option key={state} value={state}>{title(state)}</option>)}</select>
      <button type="button" className="btn btn-ghost" disabled={loading || page <= 1} onClick={() => setPage(page - 1)}>Previous</button><span>{page} / {Math.max(1, Math.ceil(total / 25))} ? {total} requests</span>
      <button type="button" className="btn btn-ghost" disabled={loading || page * 25 >= total} onClick={() => setPage(page + 1)}>Next</button></div>
    <div className="table-wrap"><table className="table"><thead><tr><th>Request</th><th>Required</th><th>Status</th><th>Open</th></tr></thead><tbody>{rows.map((row) => <tr key={row.Id}><td>{row.RequestNumber}</td><td>{row.RequiredDate}</td><td>{title(row.Status)}</td><td><button type="button" className="btn btn-ghost" onClick={() => void open(row.Id)}>Open</button></td></tr>)}</tbody></table></div>
    {selected && !editor && <section className="card"><h2>{selected.RequestNumber} ? {title(selected.Status)}</h2><p>Required {selected.RequiredDate} ? {selected.DestinationName}</p>
      <table className="table"><thead><tr><th>Item</th><th>Requested</th><th>Issued (base)</th><th>Job / remarks</th></tr></thead><tbody>{selected.Lines.map((line) => <tr key={line.Id}><td>{line.ItemCode} ? {line.ItemName}</td><td>{line.RequestedQuantity} {line.UomCode}</td><td>{line.IssuedBaseQuantity}</td><td style={{ whiteSpace: 'pre-wrap' }}>{line.Remarks}</td></tr>)}</tbody></table>
      {canEditOwnMir(selected.Status) && can(PAGE_KEYS.ownMirs, 'update') && <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => { resetDraft(selected); setEditor(true) }}>Edit draft</button>}
      {canSubmitOwnMir(selected.Status) && can(PAGE_KEYS.ownMirs, 'submit') && <div className="action-row"><label>Submit reason *<input className="input" value={reason} maxLength={1000} disabled={busy} onChange={(e) => setReason(e.target.value)} /></label><button type="button" className="btn btn-primary" disabled={busy || !reason.trim()} onClick={() => void submit()}>Submit</button></div>}</section>}
    {editor && <form className="card form-grid" onSubmit={(e) => void save(e)} onKeyDown={(e) => { if (e.key === 'Enter' && e.target instanceof HTMLInputElement) e.preventDefault() }}>
      <h2 className="field-wide">{selected ? `Edit ${selected.RequestNumber}` : 'New MIR'} ? {me?.OrganizationId}</h2>
      <fieldset disabled={busy} className="field-wide form-grid">
        <label className="field">Job / machine / work reference *<input className="input" required maxLength={200} value={draft.reference} onChange={(e) => change((d) => ({ ...d, reference: e.target.value }))} /></label>
        <label className="field">Required date *<input className="input" type="date" required value={draft.requiredDate} onChange={(e) => change((d) => ({ ...d, requiredDate: e.target.value }))} /></label>
        <label className="field">Purpose *<select className="input" value={draft.purpose} onChange={(e) => change((d) => ({ ...d, purpose: e.target.value }))}>{MIR_PURPOSES.map((value) => <option key={value} value={value}>{title(value)}</option>)}</select></label>
        <label className="field">Situation *<select className="input" value={draft.situation} onChange={(e) => change((d) => ({ ...d, situation: e.target.value, job: '', customer: '', lines: d.lines.map((line) => ({ ...line, customerPoLineId: '' })) }))}>{MIR_SITUATIONS.map((value) => <option key={value} value={value}>{title(value)}</option>)}</select></label>
        {jobBacked && <label className="field">Job order *<select className="input" required value={draft.job} onChange={(e) => change((d) => ({ ...d, job: e.target.value, destination: references?.Jobs.find((job) => job.Id === e.target.value)?.Code ?? '' }))}><option value="">Choose an Accounts-confirmed open job with a pinned Production BOM</option>{references?.Jobs.map((job) => <option key={job.Id} value={job.Id}>{job.Code} ? {job.Name}</option>)}</select></label>}
        {draft.situation === 'SPARE_SALE' && <label className="field">Customer *<select className="input" required value={draft.customer} onChange={(e) => change((d) => ({ ...d, customer: e.target.value, destination: references?.Customers.find((c) => c.Id === e.target.value)?.Name ?? '', lines: d.lines.map((line) => ({ ...line, customerPoLineId: '' })) }))}><option value="">Choose customer</option>{references?.Customers.map((customer) => <option key={customer.Id} value={customer.Id}>{customer.Code} ? {customer.Name}</option>)}</select></label>}
        {draft.situation === 'CONSUMABLE_OFFICE' && <label className="field">Destination type *<select className="input" value={draft.destinationType} onChange={(e) => change((d) => ({ ...d, destinationType: e.target.value }))}><option value="DEPARTMENT">DEPARTMENT</option><option value="OTHER">OTHER</option></select></label>}
        <label className="field">Destination *<input className="input" required maxLength={240} value={draft.destination} onChange={(e) => change((d) => ({ ...d, destination: e.target.value }))} /></label>
        <div className="field-wide"><BarcodeScanField target={`${scope}|${life.targetOf('command')}|${draft.situation}|${draft.job}|${draft.customer}`} expectedCompany={me?.OrganizationId} companyContext={getCompany}
          disabled={busy} onPendingChange={scanState} lookup={(text) => portal.searchPortalItems(text, true)}
          onResolved={(items) => { if (items.length !== 1) { setNotice(items.length ? 'Several items match. Choose the item explicitly; nothing was added.' : 'Unknown barcode / item code / serial, or item is not approved. Nothing was added.'); return } addItem(items[0]) }}
          focusQuantity={(items) => items.length === 1 ? `[data-portal-qty="${items[0].Id}"]` : ''} /></div>
        <div className="field-wide toolbar"><input className="input" aria-label="Find an approved item" value={itemSearch} onChange={(e) => setItemSearch(e.target.value)} /><button className="btn btn-ghost" type="button" onClick={() => void findItems()}>Find item</button></div>
        {choices.length > 0 && <div className="field-wide">{choices.map((item) => <button key={item.Id} type="button" className="btn btn-ghost" onClick={() => addItem(item)}>{item.ItemCode} ? {item.Name} ({item.UomCode})</button>)}</div>}
        {draft.lines.map((line) => <div className="field-wide form-grid" key={line.item.Id}><div>{line.item.ItemCode} ? {line.item.Name} ? {line.item.UomCode}</div>
          <label>Quantity *<input className="input" data-barcode-quantity data-portal-qty={line.item.Id} type="number" min="0.000001" step="any" required value={line.quantity} onChange={(e) => change((d) => ({ ...d, lines: d.lines.map((x) => x.item.Id === line.item.Id ? { ...x, quantity: e.target.value } : x) }))} /></label>
          <label>Remarks<input className="input" maxLength={750} value={line.remarks} onChange={(e) => change((d) => ({ ...d, lines: d.lines.map((x) => x.item.Id === line.item.Id ? { ...x, remarks: e.target.value } : x) }))} /></label>
          {draft.situation === 'SPARE_SALE' && <label>Customer spare PO line *<select className="input" required value={line.customerPoLineId} onChange={(e) => change((d) => ({ ...d, lines: d.lines.map((x) => x.item.Id === line.item.Id ? { ...x, customerPoLineId: e.target.value } : x) }))}><option value="">Choose current PO line</option>{spares.filter((po) => po.ItemId === line.item.Id).map((po) => <option key={po.Id} value={po.Id}>{po.PoNumber} ? line {po.LineNumber} ? {po.Description} ? {po.Quantity}</option>)}</select></label>}
          <button type="button" className="btn btn-ghost" onClick={() => change((d) => ({ ...d, lines: d.lines.filter((x) => x.item.Id !== line.item.Id) }))}>Remove line</button></div>)}
      </fieldset>
      <div className="field-wide action-row"><button className="btn btn-primary" type="submit" disabled={busy || scanPending || !draft.lines.length}>Save draft</button><button className="btn btn-ghost" type="button" disabled={busy} onClick={() => setEditor(false)}>Close</button></div>
    </form>}
    <section className="card"><h2>My outstanding custody</h2><table className="table"><thead><tr><th>Issue</th><th>Outstanding (base)</th><th>Return</th></tr></thead><tbody>{custody.map((row) => <tr key={row.MaterialIssueId}><td>{row.IssueNumber}</td><td>{row.QuantityBase}</td><td>{can(PAGE_KEYS.ownMirs, 'create') && <button className="btn btn-ghost" type="button" onClick={() => void openReturn(row.MaterialIssueId)}>Declare return</button>}</td></tr>)}</tbody></table></section>
    {returnDraft && can(PAGE_KEYS.ownMirs, 'create') && canDeclareOwnReturn(returnDraft.data.Issue.Status) && <form className="card" onSubmit={(e) => void declare(e)} onKeyDown={(e) => { if (e.key === 'Enter' && e.target instanceof HTMLInputElement) e.preventDefault() }}>
      <h2>Declare unused return ? {returnDraft.data.Issue.IssueNumber} ? {me?.OrganizationId}</h2><p>Stores must accept the physical material. No stock is credited by this declaration.</p>
      <BarcodeScanField target={`${scope}|return|${returnDraft.data.Issue.Id}|${returnDraft.selected}`} expectedCompany={me?.OrganizationId} companyContext={getCompany} disabled={busy} onPendingChange={scanState}
        lookup={async (scan) => { const current = returnRef.current; if (!current) return []; return current.data.Issue.Lines.filter((line) => {
          const item = current.data.Items.find((candidate) => candidate.Id === line.ItemId)
          return (!current.selected || current.selected === line.Id) && !!item && (current.data.ReturnableQuantities[line.Id] ?? 0) > 0 && matchesOwnReturnScan(item, line.StoredSerialNumber, scan)
        }).map((line) => ({ line, scan })) }}
        onResolved={(matches) => {
          if (matches.length !== 1) { setNotice(matches.length ? 'Several allocations match. Select the return line and scan again; nothing was added.' : 'This barcode / serial is not returnable in the selected custody. Nothing was added.'); return }
          const { line, scan } = matches[0]
          changeReturn((current) => { const old = current.lines[line.Id], result = addScanQuantity(old.quantity, current.data.ReturnableQuantities[line.Id] ?? 0, !!line.StoredSerialNumber)
            setNotice(result.warning); return result.changed ? { ...current, selected: line.Id, lines: { ...current.lines, [line.Id]: { ...old, quantity: result.quantity, scan } } } : current })
        }} focusQuantity={(matches) => matches.length === 1 ? `[data-portal-return-qty="${matches[0].line.Id}"]` : ''} />
      <fieldset disabled={busy}>{returnDraft.data.Issue.Lines.map((line) => { const item = returnDraft.data.Items.find((candidate) => candidate.Id === line.ItemId), values = returnDraft.lines[line.Id]; return <div className="form-grid" key={line.Id}>
        <label><input type="radio" name="return-line" checked={returnDraft.selected === line.Id} onChange={() => changeReturn((current) => ({ ...current, selected: line.Id }))} />{item?.ItemCode} ? {item?.Name} {line.StoredSerialNumber && `? serial ${line.StoredSerialNumber}`} ? returnable {returnDraft.data.ReturnableQuantities[line.Id] ?? 0} {item?.UomCode}</label>
        <label>Scan / item code<input className="input" type="text" value={values.scan} onFocus={(e) => e.target.select()} onChange={(e) => changeReturn((current) => ({ ...current, lines: { ...current.lines, [line.Id]: { ...current.lines[line.Id], scan: e.target.value } } }))} /></label>
        {(['quantity', 'consumed', 'held'] as const).map((field) => <label key={field}>{field === 'quantity' ? 'Returning' : field === 'consumed' ? 'Reported consumed' : 'Still held'}<input className="input" data-barcode-quantity data-portal-return-qty={field === 'quantity' ? line.Id : undefined} type="number" min="0" step="any" value={values[field]} onChange={(e) => changeReturn((current) => ({ ...current, lines: { ...current.lines, [line.Id]: { ...current.lines[line.Id], [field]: e.target.value } } }))} /></label>)}</div> })}</fieldset>
      <div className="action-row"><button className="btn btn-primary" type="submit" disabled={busy || scanPending}>Declare return</button><button className="btn btn-ghost" type="button" disabled={busy} onClick={() => { returnRef.current = null; setReturnDraft(null) }}>Close</button></div>
    </form>}
    <section className="card"><h2>My return declarations</h2><table className="table"><thead><tr><th>Return</th><th>Status</th><th>Stock</th></tr></thead><tbody>{returns.map((row) => <tr key={row.Id}><td>{row.ReturnNumber}</td><td>{title(row.Status)}</td><td>{row.Status === 'ACCEPTED' ? 'Accepted by Stores' : 'Waiting for physical acceptance'}</td></tr>)}</tbody></table>
      <div className="pager"><button className="btn btn-ghost" type="button" disabled={returnPage <= 1 || loading} onClick={() => setReturnPage(returnPage - 1)}>Previous</button><span>{returnPage} / {Math.max(1, Math.ceil(returnTotal / 25))}</span><button className="btn btn-ghost" type="button" disabled={returnPage * 25 >= returnTotal || loading} onClick={() => setReturnPage(returnPage + 1)}>Next</button></div></section>
  </div>
}
