import { useNavigate } from 'react-router-dom'
import { getStoredIdentity, setStoredIdentity, setStoredToken } from '../api/client'

export function UserMenu() {
  const navigate = useNavigate()
  const identity = getStoredIdentity()

  const signOut = () => {
    setStoredToken('')
    setStoredIdentity(null)
    navigate('/login', { replace: true })
  }

  return (
    <div className="user-menu">
      {identity && (
        <span className="user-chip">
          <span className="mono">{identity.employeeCode}</span>
          <span className="user-org">{identity.organizationId.replaceAll('_', ' ')}</span>
        </span>
      )}
      <button type="button" className="btn btn-ghost" onClick={signOut}>Sign out</button>
    </div>
  )
}
