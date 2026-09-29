import { useEffect, useRef, useState } from 'react'

interface Props {
  /** Called with the trimmed value on every completed scan (or manual Enter). */
  onScan: (value: string) => void
  label: string
  placeholder?: string
  /**
   * Scan mode keeps this input focused — clicking elsewhere refocuses it, so a
   * USB scanner (which types into whatever has focus) never fires into the void.
   * Turn it off for keyboard entry so normal tabbing works. Default on.
   */
  scanMode?: boolean
  disabled?: boolean
  /** Short status line under the box, e.g. "3 / 5 serials on line 2". */
  hint?: string
  /** Warning shown under the box (duplicate scan, unknown code, …). */
  warning?: string
}

/**
 * Scanner-first text capture. USB barcode scanners emulate a keyboard: they
 * type the code and send a trailing Enter. This box commits on Enter, clears
 * itself, and keeps focus, so consecutive scans need zero clicks or keys.
 * Manual typing works identically — type and press Enter.
 */
export function ScannerInput({ onScan, label, placeholder, scanMode = true, disabled, hint, warning }: Props) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [value, setValue] = useState('')
  // Read by the deferred refocus below, so it always sees the CURRENT props —
  // a timeout scheduled just before scan mode was switched off must not fire.
  const liveProps = useRef({ scanMode, disabled })
  liveProps.current = { scanMode, disabled }

  useEffect(() => {
    if (scanMode && !disabled) inputRef.current?.focus()
  }, [scanMode, disabled])

  const commit = () => {
    const scanned = value.trim()
    setValue('')
    if (scanned) onScan(scanned)
  }

  return (
    <div className="scan-field">
      <span className="field-label">{label}</span>
      <input
        ref={inputRef}
        className={scanMode ? 'input scan-box scan-armed' : 'input scan-box'}
        value={value}
        placeholder={placeholder ?? 'Scan barcode…'}
        disabled={disabled}
        autoComplete="off"
        spellCheck={false}
        onChange={(event) => setValue(event.target.value)}
        onKeyDown={(event) => {
          // Enter is the usual scanner suffix; keyCode 13 covers devices and
          // automation that omit `key`. Some scanners are configured with a Tab
          // suffix instead, so in scan mode a Tab on a non-empty box commits too.
          const isEnter = event.key === 'Enter' || event.keyCode === 13
          const isScanTab = scanMode && event.key === 'Tab' && value.trim() !== ''
          if (isEnter || isScanTab) {
            event.preventDefault()
            commit()
          }
        }}
        onBlur={() => {
          // Scan mode reclaims focus when it drifts to nothing (a click on the
          // page body), so the scanner always has a target — but never steals it
          // back from another control, or the rest of the form would be
          // untypeable while scan mode is on. Both checks run at FIRE time:
          // props via liveProps (the toggle may have flipped meanwhile), and
          // focus via document.activeElement (relatedTarget is unreliable —
          // Safari reports null for button and checkbox clicks).
          setTimeout(() => {
            if (!liveProps.current.scanMode || liveProps.current.disabled) return
            const active = document.activeElement
            if (active && active !== document.body) return
            inputRef.current?.focus()
          }, 120)
        }}
      />
      {warning ? (
        <span className="scan-warning" role="alert">{warning}</span>
      ) : hint ? (
        <span className="field-hint">{hint}</span>
      ) : null}
    </div>
  )
}
