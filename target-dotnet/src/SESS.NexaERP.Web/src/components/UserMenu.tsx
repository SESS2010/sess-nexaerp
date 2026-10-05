import { useEffect, useRef, useState } from 'react'
import { Building2, ChevronDown, LogOut } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { selectCompany, signOut } from '../auth/authSession'
import { COMPANIES, REALMS, isCompanyCode } from '../auth/oidcConfig'
import { useAuth } from '../auth/useAuth'
import { useSession } from '../features/auth/SessionContext'

function initials(name: string): string {
  return name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase()
}

export function UserMenu() {
  const navigate = useNavigate()
  const auth = useAuth()
  const { me } = useSession()
  const [open, setOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return undefined
    const close = (event: MouseEvent) => { if (!menuRef.current?.contains(event.target as Node)) setOpen(false) }
    document.addEventListener('mousedown', close)
    return () => document.removeEventListener('mousedown', close)
  }, [open])

  // Switching company cancels pending requests and remounts the workspace, so
  // nothing from the old company survives; the home page reloads session/me.
  const switchCompany = (code: string) => {
    if (!isCompanyCode(code) || code === auth.company) return
    selectCompany(code)
    navigate('/', { replace: true })
  }

  return (
    <div className="user-menu">
      {me && (
        <span className="user-chip" title={me.EmployeeName}>
          <span className="mono">{me.EmployeeCode}</span>
          <span className="user-org">{me.EmployeeName}</span>
          {auth.realm && <span className="user-org">· {REALMS[auth.realm].label}</span>}
        </span>
      )}
      <label className="relative inline-flex items-center">
        <Building2 size={17} aria-hidden className="pointer-events-none absolute left-3 text-slate-500" />
        <select
          className="h-11 appearance-none rounded-2xl border border-slate-200 bg-white pl-9 pr-9 text-sm font-semibold text-slate-800 shadow-sm outline-none transition hover:border-slate-300 focus:border-blue-300 focus:ring-4 focus:ring-blue-100"
          aria-label="Company"
          value={auth.company ?? ''}
          onChange={(event) => switchCompany(event.target.value)}
        >
          {COMPANIES.map((company) => (
            <option key={company.code} value={company.code}>{company.code.replaceAll('_', ' ')}</option>
          ))}
        </select>
        <ChevronDown size={16} aria-hidden className="pointer-events-none absolute right-3 text-slate-500" />
      </label>
      <div className="relative" ref={menuRef}>
        <button type="button" onClick={() => setOpen(!open)} aria-haspopup="menu" aria-expanded={open} title="Account"
          className="flex items-center gap-1.5 rounded-full p-0.5 pr-1 text-slate-500 transition hover:bg-slate-100">
          <span className="grid h-11 w-11 place-items-center rounded-full bg-gradient-to-br from-blue-500 to-blue-700 text-sm font-bold text-white shadow-md shadow-blue-600/30 ring-2 ring-white">
            {me ? initials(me.EmployeeName) : '··'}
          </span>
          <ChevronDown size={16} aria-hidden />
        </button>
        {open && (
          <div role="menu" className="absolute right-0 top-full z-50 mt-2 w-64 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xl">
            {me && (
              <div className="border-b border-slate-100 px-4 py-3">
                <div className="truncate text-sm font-semibold text-slate-900">{me.EmployeeName}</div>
                <div className="truncate text-xs text-slate-500">
                  {me.EmployeeCode}{auth.realm ? ` · ${REALMS[auth.realm].label}` : ''}
                </div>
              </div>
            )}
            <button type="button" role="menuitem" onClick={() => void signOut()}
              className="flex w-full items-center gap-2.5 px-4 py-3 text-left text-sm font-medium text-rose-600 hover:bg-rose-50">
              <LogOut size={17} aria-hidden /> Sign out
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
