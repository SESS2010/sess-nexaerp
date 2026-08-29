import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, setStoredIdentity, setStoredToken } from '../../api/client'

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

// Sign-in page. Production authentication is OIDC-only (REV866): there is no
// password verification anywhere in the backend, so this page has no password
// field. In development it signs in through the Debug-only /api/v1/dev pipeline
// using the employee's real identity mapping; when the production OIDC provider
// is selected, the submit handler becomes a redirect to the provider and the
// rest of the app is unchanged.
export function LoginPage() {
  const navigate = useNavigate()
  const [identities, setIdentities] = useState<DevIdentity[]>([])
  const [employeeCode, setEmployeeCode] = useState('')
  const [organizationId, setOrganizationId] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [apiDown, setApiDown] = useState(false)

  useEffect(() => {
    api.get<DevIdentity[]>('/api/v1/dev/identities')
      .then((list) => {
        setIdentities(list)
        setApiDown(false)
        if (list.length > 0) {
          setEmployeeCode(list[0].EmployeeCode)
          setOrganizationId(list[0].OrganizationId)
        }
      })
      .catch(() => setApiDown(true))
  }, [])

  const companies = useMemo(
    () => identities.filter((identity) => identity.EmployeeCode === employeeCode.trim().toUpperCase()),
    [identities, employeeCode],
  )

  useEffect(() => {
    if (companies.length > 0 && !companies.some((c) => c.OrganizationId === organizationId)) {
      setOrganizationId(companies[0].OrganizationId)
    }
  }, [companies, organizationId])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      const result = await api.post<DevTokenResponse>('/api/v1/dev/token', {
        EmployeeCode: employeeCode.trim(),
        OrganizationId: organizationId || null,
      })
      setStoredToken(result.Token)
      setStoredIdentity({ employeeCode: result.EmployeeCode, organizationId: result.OrganizationId })
      navigate('/employees', { replace: true })
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
          <span className="brand-mark">N</span>
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
          <span className="field-label">Employee ID</span>
          <input
            className="input"
            required
            autoFocus
            value={employeeCode}
            onChange={(event) => setEmployeeCode(event.target.value)}
            placeholder="e.g. SESS-12"
            list="known-identities"
          />
          <datalist id="known-identities">
            {[...new Set(identities.map((identity) => identity.EmployeeCode))].map((code) => (
              <option key={code} value={code}>
                {identities.find((identity) => identity.EmployeeCode === code)?.EmployeeName}
              </option>
            ))}
          </datalist>
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
            {companies.map((identity) => (
              <option key={identity.OrganizationId} value={identity.OrganizationId}>
                {identity.OrganizationId.replaceAll('_', ' ')}
              </option>
            ))}
          </select>
        </label>

        {error && <div className="alert alert-error">{error}</div>}

        <button type="submit" className="btn btn-primary login-submit" disabled={busy}>
          {busy ? 'Signing in…' : 'Sign in'}
        </button>

        <p className="login-note">
          Development sign-in — verifies your employee identity mapping. Password login is
          replaced by the organization's single sign-on (OIDC) in production.
        </p>
      </form>
    </div>
  )
}
