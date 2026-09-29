import { useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { SessLogo } from '../../components/SessLogo'
import { selectCompany, signOut } from '../../auth/authSession'
import { COMPANIES, REALMS, isCompanyCode, safeReturnPath } from '../../auth/oidcConfig'
import { useAuth } from '../../auth/useAuth'

// Picked explicitly after every sign-in. These are requested codes, not granted
// memberships: session/me under the chosen header decides whether the
// workspace opens.
export function CompanySelectPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const returnTo = safeReturnPath((location.state as { returnTo?: unknown } | null)?.returnTo)
  const [choice, setChoice] = useState<string>(auth.company ?? '')

  if (!auth.realm) return <Navigate to="/login" state={{ returnTo }} replace />

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!isCompanyCode(choice)) return
    selectCompany(choice)
    navigate(returnTo, { replace: true })
  }

  return (
    <div className="login-shell">
      <form className="login-card" onSubmit={submit}>
        <div className="login-brand">
          <SessLogo />
          <div>
            <div className="brand-name" style={{ color: 'inherit' }}>SESS NexaERP</div>
            <div className="login-brand-sub">Signed in through {REALMS[auth.realm].label}</div>
          </div>
        </div>

        <h1 className="login-title">Choose company</h1>

        <fieldset className="login-companies">
          <legend className="field-label">Company</legend>
          {COMPANIES.map((company) => (
            <label key={company.code} className={`login-company${choice === company.code ? ' selected' : ''}`}>
              <input
                type="radio"
                name="company"
                value={company.code}
                checked={choice === company.code}
                onChange={() => setChoice(company.code)}
              />
              <span>
                <span className="login-company-name">{company.name}</span>
                <span className="login-company-code mono">{company.code}</span>
              </span>
            </label>
          ))}
        </fieldset>

        <button type="submit" className="btn btn-primary login-submit" disabled={!isCompanyCode(choice)}>
          Continue
        </button>
        <button type="button" className="btn btn-ghost" onClick={() => void signOut()}>
          Sign out
        </button>
      </form>
    </div>
  )
}
