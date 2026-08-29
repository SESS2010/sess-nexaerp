import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { getStoredToken } from '../../api/client'

// Route guard: without a stored token, every app route redirects to /login.
// The backend still enforces real authorization on every call — this only
// keeps signed-out users from seeing empty screens.
export function RequireAuth({ children }: { children: ReactNode }) {
  if (!getStoredToken()) {
    return <Navigate to="/login" replace />
  }
  return <>{children}</>
}
