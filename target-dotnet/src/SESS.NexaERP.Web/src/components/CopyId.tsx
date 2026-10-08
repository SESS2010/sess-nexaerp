import { useState } from 'react'

/**
 * A record id shown in full with a Copy button (UAT defect #2: ids are picked
 * from lists, never typed, but stay visible for support and audit).
 */
export function CopyId({ label, value }: { label?: string; value: string | null | undefined }) {
  const [copied, setCopied] = useState(false)
  if (!value) return null
  const copy = async () => {
    try {
      await navigator.clipboard.writeText(value)
      setCopied(true)
      window.setTimeout(() => setCopied(false), 1500)
    } catch {
      // Clipboard blocked (plain-HTTP LAN origin): the id is still selectable on screen.
      setCopied(false)
    }
  }
  return (
    <span className="field-hint" style={{ display: 'inline-flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
      {label && <span>{label}</span>}
      <span className="mono" style={{ userSelect: 'all', wordBreak: 'break-all' }}>{value}</span>
      <button
        type="button"
        className="btn btn-ghost"
        style={{ padding: '0 6px', fontSize: 12, lineHeight: '18px' }}
        onClick={() => void copy()}
        aria-label={`Copy ${label ?? 'id'}`}
      >
        {copied ? 'Copied' : 'Copy'}
      </button>
    </span>
  )
}
