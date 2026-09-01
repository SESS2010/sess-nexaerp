import { useEffect, useState } from 'react'

const STORAGE_KEY = 'nexaerp.nav.collapsed'

function readCollapsed(): Record<string, boolean> {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as Record<string, boolean>) : {}
  } catch {
    return {}
  }
}

function writeCollapsed(next: Record<string, boolean>): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(next))
  } catch {
    // storage unavailable; sections simply reopen on next load
  }
}

interface Props {
  /** Stable key used to remember the open/closed state. */
  id: string
  label: string
  /** Sections holding the active route stay open on first render. */
  defaultOpen?: boolean
  children: React.ReactNode
}

/**
 * Collapsible sidebar group. The open/closed state is per-browser so a user who
 * only works in one module is not scrolling past the others every day.
 */
export function NavSection({ id, label, defaultOpen = true, children }: Props) {
  const [open, setOpen] = useState(defaultOpen)

  useEffect(() => {
    const stored = readCollapsed()
    if (stored[id] !== undefined) setOpen(!stored[id])
  }, [id])

  const toggle = () => {
    const next = !open
    setOpen(next)
    writeCollapsed({ ...readCollapsed(), [id]: !next })
  }

  return (
    <div className="nav-group">
      <button
        type="button"
        className="nav-section-toggle"
        aria-expanded={open}
        aria-controls={`nav-group-${id}`}
        onClick={toggle}
      >
        <span>{label}</span>
        <span className={`nav-chevron${open ? ' open' : ''}`} aria-hidden="true">
          <svg viewBox="0 0 12 12" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="1.8">
            <path d="M3 4.5 6 7.5 9 4.5" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </span>
      </button>
      <div id={`nav-group-${id}`} className="nav-group-body" hidden={!open}>
        {children}
      </div>
    </div>
  )
}
