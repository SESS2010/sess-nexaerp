import { useState } from 'react'

export interface AddableOption {
  value: string
  label: string
}

interface Props {
  label: string
  required?: boolean
  disabled?: boolean
  value: string
  options: AddableOption[]
  placeholder?: string
  onChange: (value: string) => void
  /** Creates the master record; returns the new option (which gets selected). */
  onCreate: (name: string, code: string, extra: string) => Promise<AddableOption>
  /** Optional extra choice shown in the quick-add row (e.g. UOM dimension). */
  extraField?: { label: string; options: string[] }
  addHint?: string
}

function deriveCode(name: string): string {
  return name.trim().toUpperCase().replace(/[^A-Z0-9]+/g, '_').replace(/^_+|_+$/g, '').slice(0, 40)
}

/**
 * A master-backed dropdown with an inline "+" quick-add: type a name, the code
 * is derived automatically, the record is created through the master API and
 * immediately selected.
 */
export function AddableSelect({ label, required, disabled, value, options, placeholder, onChange, onCreate, extraField, addHint }: Props) {
  const [adding, setAdding] = useState(false)
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [codeTouched, setCodeTouched] = useState(false)
  const [extra, setExtra] = useState(extraField?.options[0] ?? '')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const save = async () => {
    if (!name.trim() || !code.trim()) {
      setError('Name and code are required.')
      return
    }
    setBusy(true)
    setError('')
    try {
      const created = await onCreate(name.trim(), code.trim().toUpperCase(), extra)
      onChange(created.value)
      setAdding(false)
      setName('')
      setCode('')
      setCodeTouched(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not create the entry.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <label className="field">
      <span className="field-label">{label}{required ? ' *' : ''}</span>
      <div className="flex gap-1.5">
        <select
          className="input min-w-0 flex-1"
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
        <button
          type="button"
          className="btn btn-ghost px-2.5"
          title={`Add new ${label.toLowerCase()}`}
          disabled={disabled}
          onClick={() => { setAdding(!adding); setError('') }}
        >
          +
        </button>
      </div>
      {adding && (
        <div className="mt-1 flex flex-col gap-1.5 rounded-lg border border-line bg-slate-50 p-2.5">
          <input
            className="input"
            placeholder={`New ${label.toLowerCase()} name`}
            value={name}
            autoFocus
            onChange={(event) => {
              setName(event.target.value)
              if (!codeTouched) setCode(deriveCode(event.target.value))
            }}
          />
          <input
            className="input mono"
            placeholder="Code"
            value={code}
            onChange={(event) => { setCode(event.target.value); setCodeTouched(true) }}
          />
          {extraField && (
            <select className="input" value={extra} onChange={(event) => setExtra(event.target.value)}>
              {extraField.options.map((option) => <option key={option}>{option}</option>)}
            </select>
          )}
          {addHint && <span className="field-hint">{addHint}</span>}
          {error && <span className="text-[12px] text-red-700">{error}</span>}
          <div className="flex justify-end gap-1.5">
            <button type="button" className="btn btn-ghost" onClick={() => setAdding(false)} disabled={busy}>Cancel</button>
            <button type="button" className="btn btn-primary" onClick={() => void save()} disabled={busy}>
              {busy ? 'Saving…' : 'Save'}
            </button>
          </div>
        </div>
      )}
    </label>
  )
}
