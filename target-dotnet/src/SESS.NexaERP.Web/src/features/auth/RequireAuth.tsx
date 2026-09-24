import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../../auth/useAuth'

// Route guard: no OIDC session → sign-in; signed in but no company chosen →
// company selection. The return path travels in router state, not the URL.
// The backend still enforces real authorization on every call.
export function RequireAuth({ children }: { children: ReactNode }) {
  const auth = useAuth()
  const location = useLocation()
  const returnTo = `${location.pathname}${location.search}${location.hash}`
  if (!auth.realm) {
    return <Navigate to="/login" state={{ returnTo }} replace />
  }
  if (!auth.company) {
    return <Navigate to="/select-company" state={{ returnTo }} replace />
  }
  return <>{children}</>
}
