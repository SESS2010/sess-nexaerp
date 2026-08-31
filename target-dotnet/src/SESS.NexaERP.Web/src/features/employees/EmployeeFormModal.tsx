import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { createEmployee, createEmployeeLookup, getEmployeeLookups, updateEmployee } from '../../api/employees'
import { AddableSelect } from '../../components/AddableSelect'
import type { EmployeeDetail, EmployeeMasterLookups } from '../../types/employee'

interface Props {
  mode: 'create' | 'edit'
  existing?: EmployeeDetail
  onClose: () => void
  onSaved: (detail: EmployeeDetail) => void
}

const EMPLOYEE_TYPES = ['Permanent', 'Contract', 'Trainee', 'Consultant']

export function EmployeeFormModal({ mode, existing, onClose, onSaved }: Props) {
  const [lookups, setLookups] = useState<EmployeeMasterLookups | null>(null)
  const [form, setForm] = useState({
    employeeCode: existing?.EmployeeCode ?? '',
    employeeName: existing?.EmployeeName ?? '',
    employeeType: existing?.EmployeeType ?? 'Permanent',
    grade: existing?.Grade ?? '',
    departmentCode: '',
    skillCode: '',
    designationCode: '',
    dateOfJoining: existing?.DateOfJoining ?? '',
    officialEmail: existing?.OfficialEmail ?? '',
    mobileNumber: existing?.MobileNumber ?? '',
    remarks: '',
  })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    getEmployeeLookups()
      .then((data) => {
        setLookups(data)
        // In edit mode the detail carries display names; preselect the codes
        // whose names match the current values.
        if (existing) {
          setForm((prev) => ({
            ...prev,
            departmentCode: data.Departments.find((x) => x.Name === existing.Department)?.Code ?? '',
            skillCode: data.Skills.find((x) => existing.SkillCategories.includes(x.Name))?.Code ?? '',
            designationCode: data.Designations.find((x) => x.Name === existing.JobDesignation)?.Code ?? '',
          }))
        }
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Failed to load master lookups.'))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const set = (key: keyof typeof form) => (event: { target: { value: string } }) =>
    setForm((prev) => ({ ...prev, [key]: event.target.value }))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      const shared = {
        EmployeeName: form.employeeName,
        EmployeeType: form.employeeType,
        Grade: form.grade,
        DepartmentCode: form.departmentCode,
        SkillCode: form.skillCode,
        DesignationCode: form.designationCode,
        DateOfJoining: form.dateOfJoining || null,
        OfficialEmail: form.officialEmail || null,
        MobileNumber: form.mobileNumber || null,
      }
      const detail = mode === 'create'
        ? await createEmployee({ ...shared, EmployeeCode: form.employeeCode, Remarks: form.remarks })
        : await updateEmployee(existing!.EmployeeCode, { ...shared, Reason: form.remarks })
      onSaved(detail)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{mode === 'create' ? 'New Employee' : `Edit ${existing?.EmployeeCode}`}</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>
        <form onSubmit={submit} className="form-grid">
          {mode === 'create' && (
            <label className="field">
              <span className="field-label">Employee code *</span>
              <input className="input" required value={form.employeeCode} onChange={set('employeeCode')} placeholder="SESS-XXX" />
            </label>
          )}
          <label className="field">
            <span className="field-label">Employee name *</span>
            <input className="input" required value={form.employeeName} onChange={set('employeeName')} />
          </label>
          <label className="field">
            <span className="field-label">Employee type *</span>
            <select className="input" value={form.employeeType} onChange={set('employeeType')}>
              {EMPLOYEE_TYPES.map((type) => <option key={type}>{type}</option>)}
            </select>
          </label>
          <label className="field">
            <span className="field-label">Grade *</span>
            <input className="input" required value={form.grade} onChange={set('grade')} placeholder="e.g. Executive" />
          </label>
          <AddableSelect
            label="Department"
            required
            value={form.departmentCode}
            options={(lookups?.Departments ?? []).map((item) => ({ value: item.Code, label: item.Name }))}
            placeholder="Select department…"
            onChange={(departmentCode) => setForm((prev) => ({ ...prev, departmentCode }))}
            onCreate={async (name, code) => {
              const created = await createEmployeeLookup('departments', code, name)
              setLookups((prev) => prev && ({ ...prev, Departments: [...prev.Departments, created].sort((a, b) => a.Name.localeCompare(b.Name)) }))
              return { value: created.Code, label: created.Name }
            }}
          />
          <AddableSelect
            label="Skill category"
            required
            value={form.skillCode}
            options={(lookups?.Skills ?? []).map((item) => ({ value: item.Code, label: item.Name }))}
            placeholder="Select skill…"
            onChange={(skillCode) => setForm((prev) => ({ ...prev, skillCode }))}
            onCreate={async (name, code) => {
              const created = await createEmployeeLookup('skills', code, name)
              setLookups((prev) => prev && ({ ...prev, Skills: [...prev.Skills, created].sort((a, b) => a.Name.localeCompare(b.Name)) }))
              return { value: created.Code, label: created.Name }
            }}
          />
          <AddableSelect
            label="Designation"
            required
            value={form.designationCode}
            options={(lookups?.Designations ?? []).map((item) => ({ value: item.Code, label: item.Name }))}
            placeholder="Select designation…"
            onChange={(designationCode) => setForm((prev) => ({ ...prev, designationCode }))}
            onCreate={async (name, code) => {
              const created = await createEmployeeLookup('designations', code, name)
              setLookups((prev) => prev && ({ ...prev, Designations: [...prev.Designations, created].sort((a, b) => a.Name.localeCompare(b.Name)) }))
              return { value: created.Code, label: created.Name }
            }}
          />
          <label className="field">
            <span className="field-label">Date of joining</span>
            <input className="input" type="date" value={form.dateOfJoining} onChange={set('dateOfJoining')} />
          </label>
          <label className="field">
            <span className="field-label">Official email</span>
            <input className="input" type="email" value={form.officialEmail} onChange={set('officialEmail')} />
          </label>
          <label className="field">
            <span className="field-label">Mobile number</span>
            <input className="input" value={form.mobileNumber} onChange={set('mobileNumber')} />
          </label>
          <label className="field field-wide">
            <span className="field-label">{mode === 'create' ? 'Remarks *' : 'Reason for update *'}</span>
            <textarea className="input" required rows={2} value={form.remarks} onChange={set('remarks')} />
          </label>
          {error && <div className="alert alert-error field-wide">{error}</div>}
          <div className="modal-actions field-wide">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={saving || !lookups}>
              {saving ? 'Saving…' : mode === 'create' ? 'Create employee' : 'Save changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
