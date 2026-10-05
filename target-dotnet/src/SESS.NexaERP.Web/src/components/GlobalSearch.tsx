import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { CornerDownLeft, Search } from 'lucide-react'
import { useSession } from '../features/auth/SessionContext'
import { shortcutGroups } from '../features/home/pageCatalog'

/**
 * Top-bar search: finds any screen the role may open (the same catalogue as
 * Home and the sidebar). Ctrl+K / Cmd+K focuses it; arrows move, Enter opens.
 */
export function GlobalSearch() {
  const { can, me } = useSession()
  const navigate = useNavigate()
  const inputRef = useRef<HTMLInputElement>(null)
  const [query, setQuery] = useState('')
  const [open, setOpen] = useState(false)
  const [active, setActive] = useState(0)

  const entries = useMemo(
    () => shortcutGroups(can, me !== null).flatMap((group) => group.entries.map((entry) => ({ ...entry, group: group.label }))),
    [can, me],
  )
  const q = query.trim().toLowerCase()
  const results = (q ? entries.filter((e) => `${e.label} ${e.hint} ${e.group}`.toLowerCase().includes(q)) : entries).slice(0, 8)

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault()
        inputRef.current?.focus()
        setOpen(true)
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  const go = (to: string) => {
    setOpen(false)
    setQuery('')
    inputRef.current?.blur()
    navigate(to)
  }

  return (
    <div className="relative hidden w-full max-w-xl md:block">
      <Search size={18} aria-hidden className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" />
      <input
        ref={inputRef}
        value={query}
        onChange={(e) => { setQuery(e.target.value); setActive(0); setOpen(true) }}
        onFocus={() => setOpen(true)}
        onBlur={() => window.setTimeout(() => setOpen(false), 120)}
        onKeyDown={(e) => {
          if (e.key === 'ArrowDown') { e.preventDefault(); setActive((i) => Math.min(i + 1, results.length - 1)) }
          else if (e.key === 'ArrowUp') { e.preventDefault(); setActive((i) => Math.max(i - 1, 0)) }
          else if (e.key === 'Enter' && results[active]) { e.preventDefault(); go(results[active].to) }
          else if (e.key === 'Escape') { setOpen(false); inputRef.current?.blur() }
        }}
        placeholder="Search anything in NexaERP..."
        aria-label="Search screens"
        className="w-full rounded-2xl border border-slate-200 bg-white py-2.5 pl-11 pr-20 text-[15px] text-slate-800 shadow-sm outline-none transition placeholder:text-slate-400 focus:border-blue-300 focus:ring-4 focus:ring-blue-100"
      />
      <kbd className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 rounded-lg border border-slate-200 bg-slate-50 px-2 py-0.5 font-sans text-xs text-slate-500">Ctrl + K</kbd>
      {open && results.length > 0 && (
        <ul role="listbox" className="absolute left-0 right-0 top-full z-50 mt-2 overflow-hidden rounded-2xl border border-slate-200 bg-white p-1.5 shadow-xl">
          {results.map((r, i) => (
            <li key={r.to} role="option" aria-selected={i === active}>
              <button type="button" onMouseDown={(e) => e.preventDefault()} onClick={() => go(r.to)} onMouseEnter={() => setActive(i)}
                className={`flex w-full items-center gap-3 rounded-xl px-3 py-2 text-left ${i === active ? 'bg-blue-50' : ''}`}>
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-sm font-semibold text-slate-800">{r.label}</span>
                  <span className="block truncate text-xs text-slate-500">{r.group} · {r.hint}</span>
                </span>
                {i === active && <CornerDownLeft size={15} className="text-blue-500" aria-hidden />}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
