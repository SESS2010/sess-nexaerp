import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { completeSignIn, completeSignOut, reportCallbackFailure, reportLogoutIncomplete } from '../../auth/authSession'

// The authorization code is one-use. React StrictMode runs effects twice in
// development, so the exchange is started once per page load and shared.
let signInOnce: Promise<string> | null = null
let signOutOnce: Promise<void> | null = null

function reason(error: unknown): string {
  return error instanceof Error ? error.message : String(error)
}

/** /oidc/callback — exchanges the code, then asks for the company. */
export function OidcCallbackPage() {
  const navigate = useNavigate()
  const [status, setStatus] = useState('Completing sign-in…')

  useEffect(() => {
    signInOnce ??= completeSignIn()
    let active = true
    signInOnce
      .then((returnTo) => {
        if (active) navigate('/select-company', { replace: true, state: { returnTo } })
      })
      .catch((error: unknown) => {
        reportCallbackFailure(reason(error))
        if (active) {
          setStatus('Sign-in failed.')
          navigate('/login', { replace: true })
        }
      })
    return () => { active = false }
  }, [navigate])

  return <div className="login-shell"><div className="login-card"><p className="login-note">{status}</p></div></div>
}

/** /oidc/logout-callback — validates the state Keycloak returns after sign-out. */
export function OidcLogoutCallbackPage() {
  const navigate = useNavigate()

  useEffect(() => {
    signOutOnce ??= completeSignOut()
    let active = true
    signOutOnce
      .then(() => {
        if (active) navigate('/login', { replace: true })
      })
      .catch((error: unknown) => {
        reportLogoutIncomplete(
          `You are signed out of the ERP on this computer, but the sign-out reply from the identity server could not be verified (${reason(error)}). Close the browser before leaving this computer.`,
        )
        if (active) navigate('/login', { replace: true })
      })
    return () => { active = false }
  }, [navigate])

  return <div className="login-shell"><div className="login-card"><p className="login-note">Signing out…</p></div></div>
}
