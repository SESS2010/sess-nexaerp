import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { createQcPolicy, decideQcPolicy, listQcPolicies } from '../../api/qc'
import { listItemCategories, listUoms } from '../../api/items'
import { newIdempotencyKey } from '../../api/stores'
import type { QcInspectionPolicy } from '../../types/qc'
import type { ReferenceLookup } from '../../types/item'
import { StatusBadge } from '../employees/StatusBadge'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession, PAGE_KEYS } from '../auth/SessionContext'

type StatusFilter = 'ALL' | typeof PENDING | 'Approved' | 'Rejected'

/** MasterApprovalStatuses.PendingApproval — the value carries a space. */
const PENDING = 'Pending Approval'

const today = () => new Date().toISOString().slice(0, 10)

/**
 * QC inspection policies: what the inspector must measure on a lot before it
 * can be finalized. The QC manager prepares one bound to an item or to a
 * category (never both); the Technical Director approves or rejects it; only
 * approved, active, in-date policies are demanded at inspection time
 * (EfQcWorkflowService.EffectivePolicies).
 *
 * Until today approval was API-only (grid 17 Sep, finding #10) — a customer
 * site could not configure QC at all.
 */
export function QcPolicyPage() {
  const { can, me } = useSession()
  const canCreate = can(PAGE_KEYS.qc, 'create')
  const canApprove = can(PAGE_KEYS.qc, 'approve')
  const canReject = can(PAGE_KEYS.qc, 'reject')

  const [policies, setPolicies] = useState<QcInspectionPolicy[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState<StatusFilter>('ALL')

  const [categories, setCategories] = useState<ReferenceLookup[]>([])
  const [uoms, setUoms] = useState<ReferenceLookup[]>([])

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setPolicies(await listQcPolicies())
    } catch (err) {
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    if (!canCreate) return
    listItemCategories().then((page) => setCategories(page.Items)).catch(() => setCategories([]))
    listUoms().then((page) => setUoms(page.Items)).catch(() => setUoms([]))
  }, [canCreate])

  const categoryById = useMemo(() => new Map(categories.map((c) => [c.Id, c])), [categories])

  // ---- create (QC manager) ----
  const [showCreate, setShowCreate] = useState(false)
  const [binding, setBinding] = useState<'item' | 'category'>('item')
  const [itemCode, setItemCode] = useState('')
  const [categoryCode, setCategoryCode] = useState('')
  const [parameterCode, setParameterCode] = useState('')
  const [uomCode, setUomCode] = useState('')
  const [lowerLimit, setLowerLimit] = useState('')
  const [upperLimit, setUpperLimit] = useState('')
  const [method, setMethod] = useState('')
  const [sampleSize, setSampleSize] = useState('1')
  const [effectiveFrom, setEffectiveFrom] = useState(today())
  const [effectiveTo, setEffectiveTo] = useState('')
  const [remarks, setRemarks] = useState('')

  const submitCreate = async (event: FormEvent) => {
    event.preventDefault()
    if (!me) return
    setBusy(true)
    setError(null)
    setNotice('')
    try {
      const result = await createQcPolicy({
        OrganizationId: me.OrganizationId,
        ItemCode: binding === 'item' ? itemCode.trim().toUpperCase() : null,
        ItemCategoryCode: binding === 'category' ? categoryCode.trim().toUpperCase() : null,
        ParameterCode: parameterCode.trim().toUpperCase(),
        MeasurementUomCode: uomCode.trim().toUpperCase(),
        LowerLimit: lowerLimit.trim() ? Number(lowerLimit) : null,
        UpperLimit: upperLimit.trim() ? Number(upperLimit) : null,
        InspectionMethod: method.trim(),
        SampleSize: Number(sampleSize) || 0,
        EffectiveFrom: effectiveFrom,
        EffectiveTo: effectiveTo || null,
        Remarks: remarks.trim(),
      })
      setNotice(`Policy ${result.Id.slice(0, 8)} created — ${result.ApprovalStatus}. It applies only after the Technical Director approves it.`)
      setShowCreate(false)
      setParameterCode('')
      setLowerLimit('')
      setUpperLimit('')
      setMethod('')
      setRemarks('')
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  // ---- decide (TD) ----
  const [decidingId, setDecidingId] = useState<string | null>(null)
  const [decisionRemarks, setDecisionRemarks] = useState('')
  const keyRef = useRef<{ fingerprint: string; key: string } | null>(null)
  const keyFor = (payload: unknown): string => {
    const fingerprint = JSON.stringify(payload)
    if (!keyRef.current || keyRef.current.fingerprint !== fingerprint) {
      keyRef.current = { fingerprint, key: newIdempotencyKey('qc-policy') }
    }
    return keyRef.current.key
  }

  const decide = async (policy: QcInspectionPolicy, approve: boolean) => {
    setBusy(true)
    setError(null)
    setNotice('')
    const body = { Remarks: decisionRemarks.trim(), Version: policy.Version }
    try {
      const result = await decideQcPolicy(policy.Id, approve, body, keyFor({ id: policy.Id, approve, body }))
      setNotice(`Policy ${policy.ParameterCode} ${result.ApprovalStatus} (v${result.Version}).`)
      setDecidingId(null)
      setDecisionRemarks('')
      keyRef.current = null
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const visible = policies.filter((p) => status === 'ALL' || p.ApprovalStatus === status)
  const pendingCount = policies.filter((p) => p.ApprovalStatus === PENDING).length

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>QC Inspection Policies</h1>
          <p className="page-sub">
            Parameters an inspector must record per sample before a lot can be finalized — prepared by QC, approved by the Technical Director
          </p>
        </div>
        <div className="action-row">
          {canCreate && (
            <button type="button" className="btn btn-primary" onClick={() => setShowCreate((value) => !value)}>
              {showCreate ? 'Close form' : '+ New policy'}
            </button>
          )}
        </div>
      </div>

      <div className="toolbar">
        <select className="input" value={status} onChange={(event) => setStatus(event.target.value as StatusFilter)}>
          <option value="ALL">All statuses</option>
          <option value={PENDING}>Pending approval{pendingCount ? ` (${pendingCount})` : ''}</option>
          <option value="Approved">Approved</option>
          <option value="Rejected">Rejected</option>
        </select>
        <button type="button" className="btn btn-ghost" disabled={loading} onClick={() => void load()}>Refresh</button>
      </div>

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." />

      {showCreate && canCreate && (
        <form className="card" onSubmit={submitCreate} style={{ marginBottom: 20 }}>
          <h2 className="form-section-title">New policy</h2>
          <div className="form-grid">
            <label className="field">
              <span className="field-label">Bind to *</span>
              <select className="input" value={binding} onChange={(event) => setBinding(event.target.value as 'item' | 'category')}>
                <option value="item">One item (by item code)</option>
                <option value="category">Every item in a category</option>
              </select>
              <span className="field-hint">Exactly one binding. A category policy applies to every item in it that has no item-level policy for the same parameter.</span>
            </label>
            {binding === 'item' ? (
              <label className="field">
                <span className="field-label">Item code *</span>
                <input className="input mono" value={itemCode} onChange={(event) => setItemCode(event.target.value)} placeholder="e.g. 1SAM250000R1004" required />
              </label>
            ) : (
              <label className="field">
                <span className="field-label">Item category *</span>
                <select className="input" value={categoryCode} onChange={(event) => setCategoryCode(event.target.value)} required>
                  <option value="">Select category…</option>
                  {categories.map((c) => <option key={c.Id} value={c.Code}>{c.Code} — {c.Name}</option>)}
                </select>
              </label>
            )}
            <label className="field">
              <span className="field-label">Parameter code *</span>
              <input className="input mono" value={parameterCode} onChange={(event) => setParameterCode(event.target.value)} placeholder="e.g. VISUAL, TRIP_CURRENT" required />
            </label>
            <label className="field">
              <span className="field-label">Measurement UOM *</span>
              <select className="input" value={uomCode} onChange={(event) => setUomCode(event.target.value)} required>
                <option value="">Select UOM…</option>
                {uoms.map((u) => <option key={u.Id} value={u.Code}>{u.Code} — {u.Name}</option>)}
              </select>
            </label>
            <label className="field">
              <span className="field-label">Lower limit</span>
              <input className="input" type="number" step="any" value={lowerLimit} onChange={(event) => setLowerLimit(event.target.value)} />
              <span className="field-hint">Leave both limits empty for a PASS/FAIL-only parameter.</span>
            </label>
            <label className="field">
              <span className="field-label">Upper limit</span>
              <input className="input" type="number" step="any" value={upperLimit} onChange={(event) => setUpperLimit(event.target.value)} />
            </label>
            <label className="field">
              <span className="field-label">Sample size *</span>
              <input className="input" type="number" min={1} step={1} value={sampleSize} onChange={(event) => setSampleSize(event.target.value)} required />
              <span className="field-hint">Results required per lot: one per sample.</span>
            </label>
            <label className="field">
              <span className="field-label">Inspection method *</span>
              <input className="input" value={method} onChange={(event) => setMethod(event.target.value)} placeholder="How the parameter is checked" required />
            </label>
            <label className="field">
              <span className="field-label">Effective from *</span>
              <input className="input" type="date" value={effectiveFrom} onChange={(event) => setEffectiveFrom(event.target.value)} required />
            </label>
            <label className="field">
              <span className="field-label">Effective to</span>
              <input className="input" type="date" value={effectiveTo} onChange={(event) => setEffectiveTo(event.target.value)} />
            </label>
            <label className="field field-wide">
              <span className="field-label">Preparation remarks *</span>
              <textarea className="input" rows={2} value={remarks} onChange={(event) => setRemarks(event.target.value)} required />
            </label>
          </div>
          <div className="action-row" style={{ marginTop: 16 }}>
            <button type="submit" className="btn btn-primary" disabled={busy}>Submit for approval</button>
            <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setShowCreate(false)}>Cancel</button>
          </div>
        </form>
      )}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Binding</th>
              <th>Parameter</th>
              <th>UOM</th>
              <th className="text-right">Limits</th>
              <th>Method</th>
              <th className="text-right">Samples</th>
              <th>Effective</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={9} className="table-empty">Loading…</td></tr>}
            {!loading && visible.length === 0 && <tr><td colSpan={9} className="table-empty">No policies{status !== 'ALL' ? ' in this status' : ''}.</td></tr>}
            {!loading && visible.map((policy) => {
              const pending = policy.ApprovalStatus === PENDING && policy.IsActive
              const category = policy.ItemCategoryId ? categoryById.get(policy.ItemCategoryId) : null
              return (
                <tr key={policy.Id}>
                  <td>
                    {policy.ItemId
                      ? <><span className="field-label">Item</span> <span className="mono">{policy.ItemId.slice(0, 8)}…</span></>
                      : <><span className="field-label">Category</span> {category ? `${category.Code} — ${category.Name}` : <span className="mono">{policy.ItemCategoryId?.slice(0, 8)}…</span>}</>}
                  </td>
                  <td className="mono">{policy.ParameterCode}</td>
                  <td className="mono">{policy.MeasurementUomCode}</td>
                  <td className="text-right mono">
                    {policy.LowerLimit === null && policy.UpperLimit === null
                      ? 'PASS / FAIL'
                      : `${policy.LowerLimit ?? '—'} … ${policy.UpperLimit ?? '—'}`}
                  </td>
                  <td>{policy.InspectionMethod}</td>
                  <td className="text-right mono">{policy.SampleSize}</td>
                  <td className="mono">{policy.EffectiveFrom}{policy.EffectiveTo ? ` → ${policy.EffectiveTo}` : ''}</td>
                  <td>
                    <StatusBadge value={policy.ApprovalStatus} />
                    {!policy.IsActive && policy.ApprovalStatus !== 'Rejected' && <div className="text-ink-faint text-[11.5px]">inactive</div>}
                  </td>
                  <td>
                    {pending && (canApprove || canReject) && decidingId !== policy.Id && (
                      <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => { setDecidingId(policy.Id); setDecisionRemarks('') }}>Decide</button>
                    )}
                    {pending && decidingId === policy.Id && (
                      <div className="field" style={{ minWidth: 260 }}>
                        <input className="input" placeholder="Decision remarks *" value={decisionRemarks} onChange={(event) => setDecisionRemarks(event.target.value)} />
                        <div className="action-row">
                          {canApprove && <button type="button" className="btn btn-primary" disabled={busy || !decisionRemarks.trim()} onClick={() => void decide(policy, true)}>Approve</button>}
                          {canReject && <button type="button" className="btn btn-warn" disabled={busy || !decisionRemarks.trim()} onClick={() => void decide(policy, false)}>Reject</button>}
                          <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setDecidingId(null)}>Cancel</button>
                        </div>
                        <span className="field-hint">Technical Director only; the preparer cannot decide their own policy.</span>
                      </div>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}
