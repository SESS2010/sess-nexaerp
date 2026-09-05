import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { api } from '../../api/client'

/** GET /api/v1/session/me — SessionMe in SESS.NexaERP.Application.Identity. */
export interface SessionMe {
  EmployeeId: string
  EmployeeCode: string
  EmployeeName: string
  CompanyId: string
  OrganizationId: string
  DepartmentId: string
  DepartmentCode: string
  RoleCodes: string[]
  /** "pageKey:action" pairs resolved from the role-page permission grants, e.g. "purchase.po:view". */
  Permissions: string[]
  IdentityIssuer: string
  IdentitySubject: string
  /**
   * The subset of RoleCodes the employee holds with full authority — that is,
   * through a FULL or TEMPORARY assignment rather than a SUPPORT one. A role
   * held only as SUPPORT appears in RoleCodes but not here.
   */
  FullAuthorityRoleCodes: string[]
}

interface SessionState {
  me: SessionMe | null
  loading: boolean
  error: unknown
  reload: () => void
  /**
   * True when the session carries the permission, or when the session is not
   * loaded yet — the API is the authority, so an unknown session never hides
   * a screen the user may be allowed to open. Also true for FullControl on the
   * page, except on EXPLICIT_GRANT_PAGES where every action must be granted.
   */
  can: (pageKey: string, action?: string) => boolean
  /**
   * True when the session holds the role code (case-insensitive), or when the
   * session is not loaded yet. Use it for controls whose service enforces a
   * role on top of the page grant (e.g. QC_MANAGER, TECHNICAL_DIRECTOR).
   */
  hasRole: (role: string) => boolean
  /**
   * True when the session holds the role through a FULL or TEMPORARY
   * assignment. Use it instead of hasRole for the four actions a SUPPORT
   * assignment may never take — approve, reject, cancel and reverse — because
   * the server resolves those against the same list and answers 403 to a
   * SUPPORT holder.
   */
  hasFullAuthorityRole: (role: string) => boolean
}

/**
 * Actions a SUPPORT assignment can never take. Mirrors SupportDeniedActions in
 * SESS.NexaERP.Application/Common/RoleAuthorityResolution.cs — keep the two
 * lists identical, or a button appears that the server will refuse.
 */
export const SUPPORT_DENIED_ACTIONS: ReadonlySet<string> = new Set([
  'approve',
  'reject',
  'cancel',
  'reverse',
  'deactivate',
  'permission-configuration',
  'role-administration',
])

const SessionContext = createContext<SessionState | null>(null)

/**
 * Pages where "full-control" is NOT a wildcard: each action must be granted
 * explicitly. Mirrors `requiresExplicitGrant` in EfPagePermissionService
 * (SESS.NexaERP.Infrastructure/Authorization/EfPagePermissionService.cs); the
 * same list drives the session's Permissions expansion in EfSessionService.
 */
export const EXPLICIT_GRANT_PAGES: ReadonlySet<string> = new Set([
  'purchase.rfq',
  'purchase.vendor-quotations',
  'purchase.technical-verification',
  'purchase.commercial-comparisons',
  'purchase.po',
  'purchase.material-followup',
  'purchase.requisition-approvals',
  'inventory.grn',
])

export function SessionProvider({ children }: { children: React.ReactNode }) {
  const [me, setMe] = useState<SessionMe | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [tick, setTick] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    api.get<SessionMe>('/api/v1/session/me')
      .then((loaded) => { if (!cancelled) { setMe(loaded); setError(null) } })
      .catch((err) => { if (!cancelled) { setMe(null); setError(err) } })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [tick])

  const reload = useCallback(() => setTick((value) => value + 1), [])

  const permissions = useMemo(() => new Set(me?.Permissions ?? []), [me])

  // Action names follow PagePermissionActions in the API: lowercase kebab-case
  // ("view", "view-audit-history", "full-control"). Page key and action are
  // normalised the same way EfPagePermissionService normalises them.
  const can = useCallback(
    (pageKey: string, action = 'view') => {
      if (!me) return true
      const page = pageKey.trim().toLowerCase()
      const permission = action.trim().toLowerCase()
      if (permissions.has(`${page}:${permission}`)) return true
      return !EXPLICIT_GRANT_PAGES.has(page) && permissions.has(`${page}:full-control`)
    },
    [me, permissions],
  )

  const hasRole = useCallback(
    (role: string) => {
      if (!me) return true
      const wanted = role.trim().toUpperCase()
      return me.RoleCodes.some((code) => code.trim().toUpperCase() === wanted)
    },
    [me],
  )

  const hasFullAuthorityRole = useCallback(
    (role: string) => {
      if (!me) return true
      const wanted = role.trim().toUpperCase()
      // Older sessions predate the field; fall back to RoleCodes so the control
      // behaves exactly as it did before rather than vanishing.
      const codes = me.FullAuthorityRoleCodes ?? me.RoleCodes
      return codes.some((code) => code.trim().toUpperCase() === wanted)
    },
    [me],
  )

  const value = useMemo<SessionState>(
    () => ({ me, loading, error, reload, can, hasRole, hasFullAuthorityRole }),
    [me, loading, error, reload, can, hasRole, hasFullAuthorityRole],
  )
  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>
}

export function useSession(): SessionState {
  const context = useContext(SessionContext)
  if (!context) throw new Error('useSession must be used inside SessionProvider')
  return context
}

/** Page keys as declared by RequirePagePermission(...) in the API endpoints. */
export const PAGE_KEYS = {
  employees: 'employees.master',
  vendors: 'masters.vendors',
  customers: 'masters.customers',
  items: 'masters.items',
  customerPo: 'sales.customer-po',
  requisitions: 'purchase.requisitions',
  approvals: 'purchase.requisition-approvals',
  handoff: 'purchase.requirement-handoff',
  technicalVerification: 'purchase.technical-verification',
  rfq: 'purchase.rfq',
  quotations: 'purchase.vendor-quotations',
  comparisons: 'purchase.commercial-comparisons',
  purchaseOrders: 'purchase.po',
  materialFollowUp: 'purchase.material-followup',
  stockCheck: 'stores.stock-check',
  reservations: 'stores.reservations',
  gateEntry: 'inventory.grn',
  grn: 'inventory.grn',
  qc: 'qc.inspection-policies',
} as const
