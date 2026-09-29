import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useSession } from './SessionContext'

type Can = (pageKey: string, action?: string) => boolean

interface Props {
  /**
   * Decides from the session's "pageKey:action" grants whether the route may
   * open. Routes use the same page key and action as their sidebar link, so a
   * page reachable by URL is never one the menu would hide.
   */
  allow: (can: Can) => boolean
  /** The grant named in the refusal, e.g. "stores.stock-check:verify". */
  need: string
  children: ReactNode
}

/**
 * Route guard on the server's page grants. Mounted inside SessionGate, so the
 * session is loaded and `can` is authoritative. The backend still refuses the
 * calls; this only stops a page from opening by URL when its menu link would
 * not be shown.
 */
export function RequirePage({ allow, need, children }: Props) {
  const { can } = useSession()
  if (allow(can)) return <>{children}</>
  return (
    <div className="card">
      <div className="alert alert-warn" role="alert">
        <div className="alert-title">You are not allowed to open this page</div>
        <p className="alert-body">
          Your role or department does not hold the permission this page needs. Ask the ERP
          administrator if you should have it.
        </p>
        <p className="alert-detail mono">Needs {need}</p>
      </div>
      <Link to="/" className="btn btn-ghost">Back to Home</Link>
    </div>
  )
}

/** Route element that opens only with `pageKey:action` (default view). */
export function gated(pageKey: string, element: ReactNode, action = 'view'): ReactNode {
  return <RequirePage allow={(can) => can(pageKey, action)} need={`${pageKey}:${action}`}>{element}</RequirePage>
}
