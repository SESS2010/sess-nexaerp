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
  /** RoleCodes held through a FULL or TEMPORARY assignment. Informational only; see `can`. */
  FullAuthorityRoleCodes: string[]
}

interface SessionState {
  me: SessionMe | null
  loading: boolean
  error: unknown
  reload: () => void
  /**
   * True when /session/me.Permissions carries "pageKey:action". That list is
   * the sole authority for what the UI shows: EfSessionService has already
   * expanded full-control, dropped the actions a SUPPORT assignment may not
   * take, and only lists actions the role's services accept — so there is no
   * role check, no full-control wildcard and no SUPPORT list on this side.
   * False until the session has loaded, so nothing is offered that the
   * server would refuse.
   */
  can: (pageKey: string, action?: string) => boolean
}

const SessionContext = createContext<SessionState | null>(null)

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
  // ("view", "view-audit-history"). Page key and action are normalised the
  // same way EfPagePermissionService normalises them.
  const can = useCallback(
    (pageKey: string, action = 'view') =>
      permissions.has(`${pageKey.trim().toLowerCase()}:${action.trim().toLowerCase()}`),
    [permissions],
  )

  const value = useMemo<SessionState>(
    () => ({ me, loading, error, reload, can }),
    [me, loading, error, reload, can],
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
  materialIssueRequests: 'stores.material-issue-requests',
  materialIssues: 'stores.material-issues',
  materialIssueExcess: 'stores.material-issue-excess',
  materialReturns: 'stores.material-returns',
  openingStock: 'stores.opening-stock',
  vendorBills: 'accounts.vendor-bills',
  jobOrders: 'production.job-orders',
  componentFitments: 'production.component-fitments',
  fatReadiness: 'production.fat-readiness',
  productionBom: 'production.production-bom',
  estimatedBom: 'design.estimated-bom',
} as const
