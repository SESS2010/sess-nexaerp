import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, setStoredIdentity, setStoredToken } from '../../api/client'
import { SessLogo } from '../../components/SessLogo'

interface DevIdentity {
  EmployeeCode: string
  EmployeeName: string
  OrganizationId: string
}

interface DevTokenResponse {
  Token: string
  EmployeeCode: string
  OrganizationId: string
  ExpiresInHours: number
}

// Sign-in page. Production authentication is OIDC-only (REV866); this page runs
// on the Debug-only /api/v1/dev pipeline using the employee's real identity
// mapping. When the production OIDC
// provider is selected, the submit handler becomes a redirect to the provider
// and the rest of the app is unchanged.
export function LoginPage() {
  const navigate = useNavigate()
  const [identities, setIdentities] = useState<DevIdentity[]>([])
  const [loginId, setLoginId] = useState('')
  const [organizationId, setOrganizationId] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [apiDown, setApiDown] = useState(false)

  useEffect(() => {
    api.get<DevIdentity[]>('/api/v1/dev/identities')
      .then((list) => {
        setIdentities(list)
        setApiDown(false)
      })
      .catch(() => setApiDown(true))
  }, [])

  const companies = useMemo(
    () => [...new Set(identities.map((identity) => identity.OrganizationId))],
    [identities],
  )

  useEffect(() => {
    if (companies.length > 0 && !companies.includes(organizationId)) {
      setOrganizationId(companies[0])
    }
  }, [companies, organizationId])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      const result = await api.post<DevTokenResponse>('/api/v1/dev/token', {
        LoginId: loginId.trim(),
        OrganizationId: organizationId || null,
      })
      setStoredToken(result.Token)
      setStoredIdentity({ employeeCode: result.EmployeeCode, organizationId: result.OrganizationId })
      // Land on the home page, which every role can see; Employee Master is
      // permission-gated and blanked the first screen for most users.
      navigate('/', { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Sign-in failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="login-shell">
      <form className="login-card" onSubmit={submit}>
        <div className="login-brand">
          <SessLogo />
          <div>
            <div className="brand-name" style={{ color: 'inherit' }}>SESS NexaERP</div>
            <div className="login-brand-sub">Enterprise Resource Planning</div>
          </div>
        </div>

        <h1 className="login-title">Sign in</h1>

        {apiDown && (
          <div className="alert alert-error">
            The API is not reachable, or development sign-in is disabled. Start the backend and reload.
          </div>
        )}

        <label className="field">
          <span className="field-label">Employee ID or email</span>
          <input
            className="input"
            required
            autoFocus
            autoComplete="username"
            value={loginId}
            onChange={(event) => setLoginId(event.target.value)}
            placeholder="e.g. TEST-01 or test.user@sess.local"
          />
        </label>


        <label className="field">
          <span className="field-label">Company</span>
          <select
            className="input"
            required
            value={organizationId}
            onChange={(event) => setOrganizationId(event.target.value)}
          >
            {companies.length === 0 && <option value="">Select company…</option>}
            {companies.map((company) => (
              <option key={company} value={company}>
                {company.replaceAll('_', ' ')}
              </option>
            ))}
          </select>
        </label>

        {error && <div className="alert alert-error">{error}</div>}

        <button type="submit" className="btn btn-primary login-submit" disabled={busy}>
          {busy ? 'Signing in…' : 'Sign in'}
        </button>

        <p className="login-note">
          Development sign-in uses an existing employee identity mapping. In production this is replaced by the organization's single sign-on (OIDC).
        </p>
      </form>
    </div>
  )
}
