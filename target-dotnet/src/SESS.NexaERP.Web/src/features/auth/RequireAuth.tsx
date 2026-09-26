import { useEffect, useState, type ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { resumableRealm, resumeSession } from '../../auth/authSession'
import { useAuth } from '../../auth/useAuth'

// Route guard: no OIDC session → resume the tab's previous sign-in if there
// was one (a reload), otherwise sign-in; signed in but no company chosen →
// company selection. The return path travels in router state, not the URL.
// The backend still enforces real authorization on every call.
export function RequireAuth({ children }: { children: ReactNode }) {
  const auth = useAuth()
  const location = useLocation()
  const returnTo = `${location.pathname}${location.search}${location.hash}`
  const [resume, setResume] = useState<'idle' | 'redirecting' | 'unavailable'>(() =>
    !auth.realm && resumableRealm() ? 'idle' : 'unavailable')

  useEffect(() => {
    if (auth.realm || resume !== 'idle') return
    let active = true
    setResume('redirecting')
    resumeSession(returnTo)
      .then((started) => { if (active && !started) setResume('unavailable') })
      .catch(() => { if (active) setResume('unavailable') })
    return () => { active = false }
    // Runs once per page load: a reload is the only way to lose the session here.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  if (!auth.realm) {
    if (resume !== 'unavailable') {
      return <div className="login-shell"><div className="login-card"><p className="login-note">Restoring your session…</p></div></div>
    }
    return <Navigate to="/login" state={{ returnTo }} replace />
  }
  if (!auth.company) {
    return <Navigate to="/select-company" state={{ returnTo }} replace />
  }
  return <>{children}</>
}
