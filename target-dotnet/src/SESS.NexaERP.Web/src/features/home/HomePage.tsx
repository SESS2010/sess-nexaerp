import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/ErrorAlert'
import { useSession } from '../auth/SessionContext'
import { TrackingTiles } from '../tracking/TrackingTiles'
import { catalogSize, shortcutGroups } from './pageCatalog'

// Every module screen is page-permission gated, so an approver, a
// storekeeper and a requester each see a different subset of the app. This
// page is the one place every signed-in role can land: it needs only
// /api/v1/session/me, which any authenticated employee may call. The session
// carries the resolved permissions, so tiles the role cannot open are hidden
// rather than opening onto a 403.
//
// The tiles come from PAGE_CATALOG, which lists every page the sidebar can
// show, in the sidebar's own groups and order. Home therefore offers a
// shortcut for every page the role may View — previously it filtered a fixed
// list of its own, so screens added to the menu never appeared here and the
// Accounts Manager landed on a near-empty Home.

export function HomePage() {
  const { me, error, can } = useSession()
  const groups = shortcutGroups(can, me !== null)
  const shown = groups.reduce((total, group) => total + group.entries.length, 0)
  const total = catalogSize()

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Welcome{me ? `, ${me.EmployeeName}` : ''}</h1>
          <p className="page-sub">
            <span className="mono">{me?.EmployeeCode ?? ''}</span>
            {me?.DepartmentCode ? ` · ${me.DepartmentCode}` : ''}
            {me?.RoleCodes?.length ? ` · ${me.RoleCodes.join(', ')}` : ''}
            {' · '}
            {(me?.OrganizationId ?? '').replaceAll('_', ' ')}
          </p>
        </div>
      </div>

      <ErrorAlert error={error} fallback="Could not load your session." />

      {me && <TrackingTiles />}

      <div className="card">
        <p className="page-sub" style={{ marginBottom: 12 }}>
          {me
            ? `Screens your role can open (${shown} of ${total}). Screens without View permission are hidden.`
            : 'Pick a screen. If one opens with a permission message, your role is not mapped to it; the message names what is missing.'}
        </p>
        {groups.map((group) => (
          <section key={group.id} className="home-group">
            <h2 className="home-group-title">{group.label}</h2>
            <div className="detail-grid">
              {group.entries.map((entry) => (
                <Link key={entry.to} to={entry.to} className="home-tile">
                  <div className="home-tile-title">{entry.label}</div>
                  <div className="field-hint">{entry.hint}</div>
                </Link>
              ))}
            </div>
          </section>
        ))}
        {me && shown === 0 && (
          <p className="field-hint">Your role has no page permissions mapped yet. Ask the administrator to map your role.</p>
        )}
      </div>
    </div>
  )
}
