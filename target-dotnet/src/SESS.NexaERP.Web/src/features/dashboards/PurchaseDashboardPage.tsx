import { DASHBOARD_MOCKS_ENABLED, getCompanyCode } from '../../api/dashboards'
import { useSession } from '../auth/SessionContext'
import { DASHBOARD_KEYS, PURCHASE_DASHBOARD_KEYS } from './dashboardAccess'
import { StateNotice } from './DashboardParts'
import { PurchaseObligationsSection } from './PurchaseObligationsSection'
import { PurchaseOpenOrdersSection } from './PurchaseOpenOrdersSection'
import { PurchaseSpendingSection } from './PurchaseSpendingSection'
import { PurchaseWorkloadSection } from './PurchaseWorkloadSection'

/**
 * Purchase dashboard: four permission-aware sections. Each section is shown
 * only when the session carries its page permission, and the server still
 * decides every figure — a hidden section is a convenience, not the guard.
 */
export function PurchaseDashboardPage() {
  const { can, loading } = useSession()
  const permitted = PURCHASE_DASHBOARD_KEYS.filter((key) => can(key))

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Purchase dashboard</h1>
          <p className="page-sub">
            Company <span className="mono">{getCompanyCode()}</span> · figures are in each document's own currency and are never converted or added across currencies.
          </p>
        </div>
      </div>

      {DASHBOARD_MOCKS_ENABLED && <MockBanner />}

      {loading ? (
        <StateNotice kind="loading" title="Loading your permissions…" />
      ) : permitted.length === 0 ? (
        <StateNotice kind="denied" title="The Purchase dashboard is not available to your role">
          It needs a Purchase Manager, Technical Director or Managing Director role in this company, with the dashboard
          permissions mapped. Ask the administrator if you should have access.
        </StateNotice>
      ) : (
        <div className="flex flex-col gap-4">
          {can(DASHBOARD_KEYS.purchaseWorkload) && <PurchaseWorkloadSection />}
          {can(DASHBOARD_KEYS.purchaseOpenOrders) && <PurchaseOpenOrdersSection />}
          {can(DASHBOARD_KEYS.purchaseObligations) && <PurchaseObligationsSection />}
          {can(DASHBOARD_KEYS.purchaseSpending) && <PurchaseSpendingSection />}
        </div>
      )}
    </div>
  )
}

/** Shown on every mock build so synthetic figures are never mistaken for real ones. */
function MockBanner() {
  const variant = new URLSearchParams(window.location.search).get('mock') ?? 'reference'
  return (
    <div className="alert alert-warn mb-3" data-dashboard-mock="on">
      <div className="alert-body">
        <strong>SYNTHETIC MOCK DATA — not company figures.</strong> Variant: <span className="mono">{variant}</span>.
        Every value comes from the contract's example bodies.
      </div>
    </div>
  )
}
