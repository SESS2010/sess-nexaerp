import { useEffect, useState } from 'react'
import { api, getStoredToken, setStoredToken } from '../api/client'

interface DevIdentity {
  employeeCode: string
  employeeName: string
  organizationId: string
}

interface DevTokenResponse {
  token: string
  employeeCode: string
  organizationId: string
  expiresInHours: number
}

// Development authentication control. Preferred path: pick an identity and the
// Debug-only API endpoint /api/v1/dev/token issues a short-lived JWT bound to a
// real employee identity mapping. Manual token paste remains as a fallback for
// tokens from a real OIDC dev authority.
export function DevTokenBox() {
  const [open, setOpen] = useState(false)
  const [identities, setIdentities] = useState<DevIdentity[]>([])
  const [selected, setSelected] = useState('')
  const [manualToken, setManualToken] = useState('')
  const [signedInAs, setSignedInAs] = useState(() => (getStoredToken() ? 'token set' : ''))
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!open) return
    api.get<DevIdentity[]>('/api/v1/dev/identities')
      .then((list) => {
        setIdentities(list)
        if (list.length > 0 && !selected) {
          setSelected(`${list[0].employeeCode}|${list[0].organizationId}`)
        }
        setError('')
      })
      .catch(() => {
        setIdentities([])
        setError('Dev login endpoint unavailable (API not running, or development authentication is off). You can still paste a token manually.')
      })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const devLogin = async () => {
    const [employeeCode, organizationId] = selected.split('|')
    if (!employeeCode) return
    setBusy(true)
    setError('')
    try {
      const result = await api.post<DevTokenResponse>('/api/v1/dev/token', { employeeCode, organizationId })
      setStoredToken(result.token)
      setSignedInAs(`${result.employeeCode} (${result.organizationId})`)
      setOpen(false)
      window.location.reload()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Dev login failed.')
    } finally {
      setBusy(false)
    }
  }

  const saveManual = () => {
    setStoredToken(manualToken.trim())
    setSignedInAs(manualToken.trim() ? 'token set' : '')
    setOpen(false)
    window.location.reload()
  }

  const clear = () => {
    setStoredToken('')
    setManualToken('')
    setSignedInAs('')
    setOpen(false)
    window.location.reload()
  }

  return (
    <div className="token-box">
      <button type="button" className={`btn ${signedInAs ? 'btn-ghost' : 'btn-warn'}`} onClick={() => setOpen(!open)}>
        {signedInAs ? `Dev: ${signedInAs}` : 'Not signed in'}
      </button>
      {open && (
        <div className="token-popover">
          <div className="field-label">Development sign-in</div>
          {identities.length > 0 && (
            <>
              <select className="input" style={{ width: '100%' }} value={selected} onChange={(event) => setSelected(event.target.value)}>
                {identities.map((identity) => (
                  <option key={`${identity.employeeCode}|${identity.organizationId}`} value={`${identity.employeeCode}|${identity.organizationId}`}>
                    {identity.employeeCode} — {identity.employeeName} ({identity.organizationId})
                  </option>
                ))}
              </select>
              <div className="token-actions">
                <button type="button" className="btn btn-primary" onClick={devLogin} disabled={busy}>
                  {busy ? 'Signing in…' : 'Sign in'}
                </button>
              </div>
              <hr style={{ border: 'none', borderTop: '1px solid var(--border)', margin: '10px 0' }} />
            </>
          )}
          <label className="field-label" htmlFor="dev-token">Or paste a bearer token (JWT)</label>
          <textarea
            id="dev-token"
            rows={3}
            value={manualToken}
            onChange={(event) => setManualToken(event.target.value)}
            placeholder="Paste JWT from the development authority"
          />
          {error && <div className="alert alert-error" style={{ marginTop: 8 }}>{error}</div>}
          <div className="token-actions">
            <button type="button" className="btn btn-ghost" onClick={clear}>Sign out</button>
            <button type="button" className="btn btn-primary" onClick={saveManual} disabled={!manualToken.trim()}>Use token</button>
          </div>
        </div>
      )}
    </div>
  )
}
