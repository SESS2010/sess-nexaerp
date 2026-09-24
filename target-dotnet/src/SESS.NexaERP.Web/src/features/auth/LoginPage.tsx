import { useState } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { SessLogo } from '../../components/SessLogo'
import { beginSignIn, clearNotice } from '../../auth/authSession'
import type { AuthNotice } from '../../auth/authSession'
import { REALMS, safeReturnPath } from '../../auth/oidcConfig'
import type { RealmKey } from '../../auth/oidcConfig'
import { useAuth } from '../../auth/useAuth'

function noticeText(notice: AuthNotice): { tone: 'info' | 'warn' | 'error'; text: string } {
  switch (notice.kind) {
    case 'expired':
      return { tone: 'warn', text: 'Your sign-in expired. Sign in again to continue.' }
    case 'signed-out':
      return { tone: 'info', text: 'You have signed out.' }
    case 'logout-incomplete':
      return { tone: 'warn', text: notice.detail }
    case 'callback-failed':
      return { tone: 'error', text: `Sign-in did not complete: ${notice.detail}` }
  }
}

// Production sign-in (server-frontend-oidc-contract.md). There is no password
// box here: the button sends the browser to the chosen Keycloak realm, which
// asks for the password (and, for Approvers, the authenticator code) itself.
export function LoginPage() {
  const auth = useAuth()
  const location = useLocation()
  const returnTo = safeReturnPath((location.state as { returnTo?: unknown } | null)?.returnTo)
  const [busy, setBusy] = useState<RealmKey | null>(null)
  const [error, setError] = useState('')

  if (auth.realm) {
    return <Navigate to={auth.company ? returnTo : '/select-company'} state={{ returnTo }} replace />
  }

  const start = async (key: RealmKey) => {
    setBusy(key)
    setError('')
    clearNotice()
    try {
      await beginSignIn(key, returnTo)
      // The browser is leaving for Keycloak; keep the button disabled.
    } catch (err) {
      setBusy(null)
      const reason = err instanceof Error ? err.message : String(err)
      setError(
        `The ${REALMS[key].label} sign-in server at ${REALMS[key].authority} could not be reached (${reason}). Check that it is running, then try again.`,
      )
    }
  }

  const notice = auth.notice ? noticeText(auth.notice) : null

  return (
    <div className="login-shell">
      <div className="login-card">
        <div className="login-brand">
          <SessLogo />
          <div>
            <div className="brand-name" style={{ color: 'inherit' }}>SESS NexaERP</div>
            <div className="login-brand-sub">Enterprise Resource Planning</div>
          </div>
        </div>

        <h1 className="login-title">Sign in</h1>
        <p className="login-note" style={{ marginTop: 0 }}>Choose how you sign in. You will enter your password on the next page.</p>

        {notice && (
          <div className={`alert ${notice.tone === 'error' ? 'alert-error' : notice.tone === 'warn' ? 'alert-warn' : 'alert-info'}`} role="status">
            {notice.text}
          </div>
        )}

        {(Object.keys(REALMS) as RealmKey[]).map((key) => (
          <button
            key={key}
            type="button"
            className={`btn ${key === 'staff' ? 'btn-primary' : 'btn-ghost'} login-submit login-realm`}
            disabled={busy !== null}
            onClick={() => void start(key)}
          >
            <span className="login-realm-label">{busy === key ? 'Opening sign-in…' : `Sign in as ${REALMS[key].label}`}</span>
            <span className="login-realm-hint">{REALMS[key].description}</span>
          </button>
        ))}

        {error && <div className="alert alert-error" role="alert">{error}</div>}

        <p className="login-note">
          SESS-01, SESS-02 and SESS-14, and anyone holding a Technical Director, Managing Director, Accounts Manager or CFO role, must use <strong>Approvers</strong>. The choice does not give you any role: the ERP checks your employee mapping, company and roles on every request.
        </p>
      </div>
    </div>
  )
}
