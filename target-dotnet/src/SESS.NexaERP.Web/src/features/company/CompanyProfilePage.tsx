import { useCallback, useEffect, useState } from 'react'
import {
  canEditCompanyProfile,
  companyFieldErrorsOf,
  getCompanyProfile,
  listWarehouseStateCodes,
  saveCompanyProfile,
  saveWarehouseStateCode,
  STATE_CODE_PATTERN,
  toSaveCompanyProfileRequest,
  validateCompanyProfile,
} from '../../api/companyProfile'
import type { CompanyProfileFormValues } from '../../api/companyProfile'
import type { CompanyProfileView, WarehouseStateCodeView } from '../../types/companyProfile'
import { ErrorAlert } from '../../components/ErrorAlert'
import { StatusBadge } from '../employees/StatusBadge'
import { useSession } from '../auth/SessionContext'

type FieldErrors = Record<string, string[]>

function FieldError({ errors, field }: { errors: FieldErrors; field: string }) {
  const messages = errors[field]
  if (!messages?.length) return null
  return <span className="field-error scan-warning">{messages.join(' ')}</span>
}

const EMPTY_FORM: CompanyProfileFormValues = {
  LegalName: '', TradeName: '', Gstin: '', Pan: '', StateCode: '', State: '',
  AddressLine1: '', AddressLine2: '', City: '', PinCode: '', Phone: '', Email: '', Reason: '',
}

function formFrom(profile: CompanyProfileView): CompanyProfileFormValues {
  return {
    LegalName: profile.LegalName ?? '',
    TradeName: profile.TradeName ?? '',
    Gstin: profile.Gstin ?? '',
    Pan: profile.Pan ?? '',
    StateCode: profile.StateCode ?? '',
    State: profile.State ?? '',
    AddressLine1: profile.AddressLine1 ?? '',
    AddressLine2: profile.AddressLine2 ?? '',
    City: profile.City ?? '',
    PinCode: profile.PinCode ?? '',
    Phone: profile.Phone ?? '',
    Email: profile.Email ?? '',
    Reason: '',
  }
}

function formatInstant(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime()) || date.getFullYear() < 2000) return '—'
  return date.toLocaleString('en-IN', { dateStyle: 'medium', timeStyle: 'short' })
}

interface ProfileField {
  key: keyof Omit<CompanyProfileFormValues, 'Reason'>
  label: string
  required?: boolean
  mono?: boolean
  hint?: string
  maxLength?: number
  wide?: boolean
  type?: string
}

const PROFILE_FIELDS: ProfileField[] = [
  { key: 'LegalName', label: 'Legal name', required: true, maxLength: 200, wide: true, hint: 'Exactly as registered with the GST department; printed on every PO and delivery challan.' },
  { key: 'TradeName', label: 'Trade name', maxLength: 200, wide: true },
  { key: 'Gstin', label: 'GSTIN', required: true, mono: true, maxLength: 15, hint: '15 characters, e.g. 33ABCDE1234F1Z5. The first two digits are the state code and characters 3 to 12 are the PAN.' },
  { key: 'Pan', label: 'PAN', required: true, mono: true, maxLength: 10, hint: '10 characters, e.g. ABCDE1234F. Must match characters 3 to 12 of the GSTIN.' },
  { key: 'StateCode', label: 'GST state code', required: true, mono: true, maxLength: 2, hint: 'Two digits, e.g. 33 for Tamil Nadu. Must equal the first two digits of the GSTIN.' },
  { key: 'State', label: 'State', required: true, maxLength: 100 },
  { key: 'AddressLine1', label: 'Address line 1', required: true, maxLength: 200, wide: true },
  { key: 'AddressLine2', label: 'Address line 2', maxLength: 200, wide: true },
  { key: 'City', label: 'City', required: true, maxLength: 100 },
  { key: 'PinCode', label: 'PIN code', required: true, mono: true, maxLength: 6, hint: 'Six digits.' },
  { key: 'Phone', label: 'Phone', maxLength: 50 },
  { key: 'Email', label: 'Email', maxLength: 200, type: 'email' },
]

/**
 * R10: the company's legal identity (CompanyProfileEndpoints.cs). Any signed-in
 * employee reads it — it is what every print shows and what the quotation GST
 * state rule derives from. Only the Technical Director saves it, with a reason
 * that becomes the audit remark and the Version carried back for optimistic
 * locking. Below it, each warehouse's own GST state code, which the place of
 * supply follows before falling back to the company's state.
 */
export function CompanyProfilePage() {
  const { me } = useSession()
  const canEdit = canEditCompanyProfile(me)

  const [profile, setProfile] = useState<CompanyProfileView | null>(null)
  const [warehouses, setWarehouses] = useState<WarehouseStateCodeView[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [warehouseError, setWarehouseError] = useState<unknown>(null)
  const [notice, setNotice] = useState('')

  const [form, setForm] = useState<CompanyProfileFormValues>(EMPTY_FORM)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [saving, setSaving] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    setWarehouseError(null)
    try {
      const loaded = await getCompanyProfile()
      setProfile(loaded)
      setForm(formFrom(loaded))
      setFieldErrors({})
    } catch (err) {
      setProfile(null)
      setError(err)
    } finally {
      setLoading(false)
    }
    try {
      setWarehouses(await listWarehouseStateCodes())
    } catch (err) {
      setWarehouses([])
      setWarehouseError(err)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const setField = (key: keyof CompanyProfileFormValues, value: string) => {
    setForm((prev) => ({ ...prev, [key]: value }))
    setFieldErrors((prev) => {
      if (!prev[key]) return prev
      const next = { ...prev }
      delete next[key]
      return next
    })
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!canEdit || !profile) return
    setError(null)
    setNotice('')
    const clientErrors = validateCompanyProfile(form)
    setFieldErrors(clientErrors)
    if (Object.keys(clientErrors).length > 0) {
      setError('Some fields need attention before the profile can be saved; each one is marked below.')
      return
    }
    setSaving(true)
    try {
      // Version: the loaded one, 0 when no profile exists yet (the server insists on 0 for a first save).
      const saved = await saveCompanyProfile(toSaveCompanyProfileRequest(form, profile.IsComplete ? profile.Version : 0))
      setProfile(saved)
      setForm(formFrom(saved))
      setNotice(`Company profile saved (version ${saved.Version}). Prints and quotation GST states now use ${saved.LegalName}, state ${saved.StateCode}.`)
      // Effective warehouse states follow the company state, so re-read them.
      try { setWarehouses(await listWarehouseStateCodes()) } catch { /* the table keeps its last rows */ }
    } catch (err) {
      setFieldErrors(companyFieldErrorsOf(err))
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  const complete = profile?.IsComplete ?? false

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Company Profile</h1>
          <p className="page-sub">
            The legal identity printed on every purchase order and delivery challan, and the state the GST split is decided from
          </p>
        </div>
        <div className="action-row">
          {profile && <StatusBadge value={complete ? 'Completed' : 'Not Completed'} />}
          <button type="button" className="btn btn-ghost" disabled={loading || saving} onClick={() => void load()}>Refresh</button>
        </div>
      </div>

      {!canEdit && (
        <div className="alert">
          Only the Technical Director can change the company's legal identity. You can read the profile; the fields below are not editable.
        </div>
      )}

      {profile && !complete && (
        <div className="alert alert-warn">
          <div className="alert-title">The company profile has not been saved yet</div>
          <p className="alert-body">
            Until this profile is saved, purchase orders and delivery challans cannot be printed and quotation GST states cannot be derived.
            {canEdit ? ' Fill in every field below, give a reason, and save.' : ' Ask the Technical Director to complete it.'}
          </p>
        </div>
      )}

      {notice && <div className="alert">{notice}</div>}
      <ErrorAlert error={error} onReload={() => void load()} fallback="The company profile could not be loaded." />

      <form onSubmit={submit}>
        <div className="card" style={{ marginBottom: 16 }}>
          <div className="form-section-title">Legal identity — {profile?.CompanyCode ?? me?.OrganizationId ?? ''}</div>
          {loading && !profile && <p className="field-hint">Loading…</p>}
          <div className="form-grid">
            {PROFILE_FIELDS.map((field) => (
              <label key={field.key} className={`field${field.wide ? ' field-wide' : ''}`}>
                <span className="field-label">{field.label}{field.required ? ' *' : ''}</span>
                <input
                  className={`input${field.mono ? ' mono' : ''}`}
                  type={field.type ?? 'text'}
                  maxLength={field.maxLength}
                  value={form[field.key]}
                  readOnly={!canEdit}
                  disabled={loading || saving || !profile}
                  aria-invalid={fieldErrors[field.key] ? true : undefined}
                  onChange={(event) => setField(field.key, event.target.value)}
                />
                {field.hint && canEdit && <span className="field-hint">{field.hint}</span>}
                <FieldError errors={fieldErrors} field={field.key} />
              </label>
            ))}

            {canEdit && (
              <label className="field field-wide">
                <span className="field-label">Reason for this change *</span>
                <textarea
                  className="input"
                  rows={2}
                  value={form.Reason}
                  disabled={loading || saving || !profile}
                  placeholder={complete ? 'Why the legal identity is being changed — recorded in the audit trail' : 'e.g. Initial company profile for go-live'}
                  onChange={(event) => setField('Reason', event.target.value)}
                />
                <span className="field-hint">Mandatory. It is written as the remark on the audit row for this save.</span>
                <FieldError errors={fieldErrors} field="Reason" />
              </label>
            )}
          </div>

          <div className="action-row" style={{ marginTop: 16, alignItems: 'center' }}>
            {canEdit && (
              <button type="submit" className="btn btn-primary" disabled={loading || saving || !profile}>
                {saving ? 'Saving…' : complete ? 'Save changes' : 'Save company profile'}
              </button>
            )}
            <div className="spacer" />
            {profile && (
              <span className="field-hint" style={{ marginTop: 0 }}>
                {complete
                  ? <>Version <span className="mono">{profile.Version}</span> · last saved {formatInstant(profile.UpdatedAt)} by {profile.UpdatedBy || '—'}</>
                  : 'No version yet — this will be the first save.'}
              </span>
            )}
          </div>
        </div>
      </form>

      <WarehouseStateCodes
        rows={warehouses}
        canEdit={canEdit}
        companyStateCode={complete ? profile?.StateCode ?? '' : ''}
        error={warehouseError}
        onReload={() => void load()}
        onSaved={(saved) => {
          setWarehouses((prev) => prev.map((row) => (row.WarehouseId === saved.WarehouseId ? saved : row)))
          setNotice(`Warehouse ${saved.WarehouseCode} now uses GST state ${saved.EffectiveStateCode} (version ${saved.Version}).`)
        }}
      />
    </div>
  )
}

interface WarehouseStateCodesProps {
  rows: WarehouseStateCodeView[]
  canEdit: boolean
  /** The company's state (empty until the profile is saved), which a warehouse without its own code falls back to. */
  companyStateCode: string
  error: unknown
  onReload: () => void
  onSaved: (row: WarehouseStateCodeView) => void
}

function WarehouseStateCodes({ rows, canEdit, companyStateCode, error, onReload, onSaved }: WarehouseStateCodesProps) {
  // One row is edited at a time: its draft state code and reason live here.
  const [editing, setEditing] = useState<string | null>(null)
  const [draftCode, setDraftCode] = useState('')
  const [draftReason, setDraftReason] = useState('')
  const [rowErrors, setRowErrors] = useState<FieldErrors>({})
  const [rowError, setRowError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  const startEdit = (row: WarehouseStateCodeView) => {
    setEditing(row.WarehouseCode)
    setDraftCode(row.StateCode ?? row.EffectiveStateCode ?? '')
    setDraftReason('')
    setRowErrors({})
    setRowError(null)
  }

  const cancel = () => {
    setEditing(null)
    setRowErrors({})
    setRowError(null)
  }

  const save = async (row: WarehouseStateCodeView) => {
    const errors: FieldErrors = {}
    if (!STATE_CODE_PATTERN.test(draftCode.trim())) errors.StateCode = ['State code must be the two-digit GST state code, for example 33.']
    if (!draftReason.trim()) errors.Reason = ['Reason is required; it becomes the audit remark.']
    setRowErrors(errors)
    if (Object.keys(errors).length > 0) return
    setSaving(true)
    setRowError(null)
    try {
      const saved = await saveWarehouseStateCode(row.WarehouseCode, {
        StateCode: draftCode.trim(),
        Version: row.Version,
        Reason: draftReason.trim(),
      })
      setEditing(null)
      onSaved(saved)
    } catch (err) {
      setRowErrors(companyFieldErrorsOf(err))
      setRowError(err)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="card">
      <div className="form-section-title">Warehouse GST state codes</div>
      <p className="field-hint" style={{ marginBottom: 12 }}>
        The place of supply on a quotation is the delivery warehouse's state. A warehouse without its own code follows the company
        state{companyStateCode ? <> (<span className="mono">{companyStateCode}</span>)</> : ' — which is not set until the profile above is saved'}.
        {canEdit ? ' Set a code only for a warehouse in another state.' : ' Only the Technical Director can change these.'}
      </p>

      <ErrorAlert error={error} onReload={onReload} fallback="The warehouse state codes could not be loaded." />
      <ErrorAlert error={rowError} onReload={onReload} fallback="The warehouse state code could not be saved." />

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Warehouse</th>
              <th>Name</th>
              <th>Own state code</th>
              <th>Effective state</th>
              <th>Source</th>
              <th className="text-right">Version</th>
              {canEdit && <th></th>}
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 && (
              <tr><td colSpan={canEdit ? 7 : 6} className="table-empty">No active warehouses in this company.</td></tr>
            )}
            {rows.map((row) => {
              const isEditing = editing === row.WarehouseCode
              return (
                <tr key={row.WarehouseId}>
                  <td className="mono">{row.WarehouseCode}</td>
                  <td>{row.WarehouseName}</td>
                  <td>
                    {isEditing ? (
                      <div className="field">
                        <input
                          className="input mono"
                          maxLength={2}
                          value={draftCode}
                          autoFocus
                          disabled={saving}
                          aria-invalid={rowErrors.StateCode ? true : undefined}
                          onChange={(event) => { setDraftCode(event.target.value); setRowErrors((prev) => ({ ...prev, StateCode: [] })) }}
                        />
                        <FieldError errors={rowErrors} field="StateCode" />
                      </div>
                    ) : (
                      <span className="mono">{row.StateCode ?? '—'}</span>
                    )}
                  </td>
                  <td className="mono">{row.EffectiveStateCode || '—'}</td>
                  <td>
                    {row.StateCode
                      ? <StatusBadge value="Warehouse" />
                      : row.EffectiveStateCode
                        ? <StatusBadge value="Company profile" />
                        : <StatusBadge value="Not set" />}
                  </td>
                  <td className="text-right mono">{row.Version}</td>
                  {canEdit && (
                    <td>
                      {isEditing ? (
                        <div className="field" style={{ minWidth: 260 }}>
                          <input
                            className="input"
                            placeholder="Reason (audit remark) *"
                            value={draftReason}
                            disabled={saving}
                            aria-invalid={rowErrors.Reason ? true : undefined}
                            onChange={(event) => { setDraftReason(event.target.value); setRowErrors((prev) => ({ ...prev, Reason: [] })) }}
                          />
                          <FieldError errors={rowErrors} field="Reason" />
                          <div className="action-row" style={{ marginTop: 6 }}>
                            <button type="button" className="btn btn-primary" disabled={saving} onClick={() => void save(row)}>
                              {saving ? 'Saving…' : 'Save'}
                            </button>
                            <button type="button" className="btn btn-ghost" disabled={saving} onClick={cancel}>Cancel</button>
                          </div>
                        </div>
                      ) : (
                        <button type="button" className="btn btn-ghost" disabled={editing !== null} onClick={() => startEdit(row)}>
                          Set state code…
                        </button>
                      )}
                    </td>
                  )}
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}
