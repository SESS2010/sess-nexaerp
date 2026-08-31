import { useMemo, useRef, useState } from 'react'

export interface CustomerSearchOption {
  CustomerCode: string
  Name: string
}

interface Props {
  options: CustomerSearchOption[]
  /** Selected master customer code ('' when free text). */
  customerCode: string
  /** Display / free-text customer name. */
  customerName: string
  onSelect: (option: CustomerSearchOption) => void
  /** Called as the user types free text (clears any selected code). */
  onText: (name: string) => void
  placeholder?: string
}

const MAX_SUGGESTIONS = 12

/**
 * Single-field customer picker: type to search the customer master, pick a
 * suggestion to link the record, or keep typing for a free-text name.
 */
export function CustomerSearchSelect({ options, customerCode, customerName, onSelect, onText, placeholder }: Props) {
  const [open, setOpen] = useState(false)
  const [highlight, setHighlight] = useState(0)
  const blurTimer = useRef<number | undefined>(undefined)

  const suggestions = useMemo(() => {
    const term = customerName.trim().toUpperCase()
    if (!term) return options.slice(0, MAX_SUGGESTIONS)
    const starts = options.filter((c) => c.Name.toUpperCase().startsWith(term) || c.CustomerCode.toUpperCase().startsWith(term))
    const contains = options.filter((c) => !starts.includes(c) && (c.Name.toUpperCase().includes(term) || c.CustomerCode.toUpperCase().includes(term)))
    return [...starts, ...contains].slice(0, MAX_SUGGESTIONS)
  }, [options, customerName])

  const pick = (option: CustomerSearchOption) => {
    onSelect(option)
    setOpen(false)
  }

  return (
    <div className="relative w-full">
      <input
        className="input w-full"
        placeholder={placeholder ?? 'Type customer name or code to search…'}
        value={customerName}
        onChange={(event) => {
          onText(event.target.value)
          setOpen(true)
          setHighlight(0)
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => {
          blurTimer.current = window.setTimeout(() => setOpen(false), 150)
        }}
        onKeyDown={(event) => {
          if (!open && (event.key === 'ArrowDown' || event.key === 'ArrowUp')) { setOpen(true); return }
          if (event.key === 'ArrowDown') { event.preventDefault(); setHighlight((h) => Math.min(h + 1, suggestions.length - 1)) }
          if (event.key === 'ArrowUp') { event.preventDefault(); setHighlight((h) => Math.max(h - 1, 0)) }
          if (event.key === 'Enter' && open && suggestions[highlight]) { event.preventDefault(); pick(suggestions[highlight]) }
          if (event.key === 'Escape') setOpen(false)
        }}
      />
      {customerCode && (
        <span className="field-hint">
          Linked to customer master: <span className="mono">{customerCode}</span>
        </span>
      )}
      {!customerCode && customerName.trim() && (
        <span className="field-hint">Not linked to the customer master — will be saved as a free-text name.</span>
      )}
      {open && suggestions.length > 0 && (
        <div
          className="absolute left-0 right-0 z-20 mt-1 max-h-56 overflow-y-auto rounded-lg border border-line bg-white shadow-lg"
          style={{ top: '100%' }}
        >
          {suggestions.map((option, index) => (
            <button
              key={option.CustomerCode}
              type="button"
              className={`flex w-full items-center gap-2 px-3 py-2 text-left text-[13px] hover:bg-accent-soft ${index === highlight ? 'bg-accent-soft' : ''}`}
              onMouseDown={(event) => { event.preventDefault(); window.clearTimeout(blurTimer.current); pick(option) }}
              onMouseEnter={() => setHighlight(index)}
            >
              <span className="flex-1">{option.Name}</span>
              <span className="mono text-[11.5px] text-ink-faint">{option.CustomerCode}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
