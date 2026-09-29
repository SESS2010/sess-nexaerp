import { useNavigate } from 'react-router-dom'
import { selectCompany, signOut } from '../auth/authSession'
import { COMPANIES, REALMS, isCompanyCode } from '../auth/oidcConfig'
import { useAuth } from '../auth/useAuth'
import { useSession } from '../features/auth/SessionContext'

export function UserMenu() {
  const navigate = useNavigate()
  const auth = useAuth()
  const { me } = useSession()

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
      <select
        className="input"
        aria-label="Company"
        value={auth.company ?? ''}
        onChange={(event) => switchCompany(event.target.value)}
        style={{ width: 'auto' }}
      >
        {COMPANIES.map((company) => (
          <option key={company.code} value={company.code}>{company.code.replaceAll('_', ' ')}</option>
        ))}
      </select>
      <button type="button" className="btn btn-ghost" onClick={() => void signOut()}>Sign out</button>
    </div>
  )
}
